using Application.DTOs.User;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private static readonly (string Name, string Color)[] DefaultTaskStatuses =
        [
            ("A fazer", "#98A2B3"),
            ("Em progresso", "#7592FF"),
            ("Em revisão", "#F79009"),
            ("Concluídas", "#12B76A")
        ];

        private readonly IUserRepository _userRepo;
        private readonly IExternalLoginRepository _externalLoginRepository;
        private readonly IProductSaleRepository _productSaleRepository;
        private readonly IUploadService _uploadService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepo,
            IExternalLoginRepository externalLoginRepository,
            IProductSaleRepository productSaleRepository,
            IUploadService uploadService, 
            IPasswordHasher<User> passwordHasher,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _externalLoginRepository = externalLoginRepository;
            _productSaleRepository = productSaleRepository;
            _uploadService = uploadService;
            _passwordHasher = passwordHasher;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CreateUserAsync(CreateUserDTO user, CancellationToken cancellationToken = default)
        {
            var createdByUserId = _currentUserService.GetUserIdOrNull();

            ValidatePasswordStrength(user.Password);

            await ValidateUserDoesNotExistAsync(user.Email, cancellationToken);

            string? profileImagePath = null;

            try
            {
                if (user.ProfileImageUrl is not null)
                {
                    profileImagePath = await _uploadService.UploadUserImageAsync(user.ProfileImageUrl, cancellationToken);
                }

                var newUser = User.Create(
                    user.Name,
                    user.Email,
                    profileImagePath,
                    createdByUserId);

                for (var index = 0; index < DefaultTaskStatuses.Length; index++)
                {
                    var (name, color) = DefaultTaskStatuses[index];
                    newUser.TasksStatus.Add(
                        TasksStatus.CreateForUser(newUser, name, color, index));
                }

                var passwordHash = _passwordHasher.HashPassword(newUser, user.Password);

                newUser.SetPasswordHash(passwordHash);

                await _userRepo.AddAsync(newUser, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(profileImagePath))
                {
                    await TryDeleteProfileImageAsync(profileImagePath);
                }

                throw;
            }
        }

        public async Task<User> AddExternalUserAsync(string name, string email, CancellationToken cancellationToken = default)
        {
            await ValidateUserDoesNotExistAsync(email, cancellationToken);

            var normalizedName = string.IsNullOrWhiteSpace(name)
                ? email.Split('@', 2)[0]
                : name.Trim();

            if (normalizedName.Length > 150)
            {
                normalizedName = normalizedName[..150];
            }

            var newUser = User.Create(normalizedName, email, null, null);

            for (var index = 0; index < DefaultTaskStatuses.Length; index++)
            {
                var (statusName, color) = DefaultTaskStatuses[index];

                newUser.TasksStatus.Add(TasksStatus.CreateForUser(newUser, statusName, color, index));
            }

            await _userRepo.AddAsync(newUser, cancellationToken);

            return newUser;
        }

        public async Task UpdateUserAsync(UpdateUserDTO user, CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var existingUser = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (existingUser is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            var oldProfileImagePath = existingUser.ProfileImageUrl;

            string? newProfileImagePath = null;

            try
            {
                if (user.ProfileImageUrl is not null)
                {
                    newProfileImagePath = await _uploadService.UploadUserImageAsync(user.ProfileImageUrl, cancellationToken);
                }

                var profileImagePath = newProfileImagePath ?? oldProfileImagePath;

                existingUser.Edit(
                    user.Name,
                    profileImagePath);

                _userRepo.Update(existingUser);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                if (!string.IsNullOrWhiteSpace(newProfileImagePath) &&
                    !string.IsNullOrWhiteSpace(oldProfileImagePath))
                {
                    await TryDeleteProfileImageAsync(oldProfileImagePath);
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newProfileImagePath))
                {
                    await TryDeleteProfileImageAsync(newProfileImagePath);
                }

                throw;
            }
        }

        public async Task ChangeEmailAsync(ChangeUserEmailDTO email, CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var myUser = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (myUser is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            ValidateCurrentPassword(myUser, email.currentPassword);

            await ValidateEmailDoesNotBelongToAnotherUserAsync(currentUser, email.newEmail, cancellationToken);

            myUser.ChangeEmail(email.newEmail);
            myUser.InvalidateTokens();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ChangePasswordAsync(ChangeUserPasswordDTO password, CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var myUser = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (myUser is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            if (!string.IsNullOrWhiteSpace(myUser.PasswordHash))
            {
                ValidateCurrentPassword(myUser, password.CurrentPassword);
            }
            else if (!string.IsNullOrWhiteSpace(password.CurrentPassword))
            {
                throw new UnauthorizedException("INVALID_PASSWORD", "Password doesn't match");
            }

            if (string.IsNullOrWhiteSpace(password.NewPassword))
            {
                throw new DomainException("PASSWORD_REQUIRED", "New password is required.");
            }

            ValidatePasswordStrength(password.NewPassword);

            var passwordHash = _passwordHasher.HashPassword(myUser, password.NewPassword);

            myUser.SetPasswordHash(passwordHash);
            myUser.InvalidateTokens();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteUserAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var myUser = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (myUser is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            var profileImagePath = myUser.ProfileImageUrl;

            await _productSaleRepository.RemoveByUserAsync(currentUser, cancellationToken);
            _userRepo.Remove(myUser);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(profileImagePath))
            {
                await TryDeleteProfileImageAsync(profileImagePath);
            }
        }

        public async Task<GetUserDTO?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var user = await _userRepo.GetByEmailAsync(email, cancellationToken);

            if (user is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            if (currentUserId != user.Id)
            {
                throw new DomainException("FORBIDDEN", "You are not allowed to access this user's information.");
            }

            return await MapToGetUserDTOAsync(user, cancellationToken);
        }

        public async Task<GetUserDTO?> GetUserByIdAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var user = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (user is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            return await MapToGetUserDTOAsync(user, cancellationToken);
        }

        public async Task<Stream?> GetProfileImageAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            var user = await _userRepo.GetByIdAsync(currentUserId, cancellationToken);

            if (user is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            return string.IsNullOrWhiteSpace(user.ProfileImageUrl)
                ? null
                : await _uploadService.ReadUploadAsync(user.ProfileImageUrl, cancellationToken);
        }

        private async Task ValidateUserDoesNotExistAsync(string email, CancellationToken cancellationToken = default)
        {
            var userExists = await _userRepo.EmailExistsAsync(email, cancellationToken);

            if (userExists)
            {
                throw new DomainException("EMAIL_ALREADY_EXISTS", "A user with this email already exists.");
            }
        }

        private async Task ValidateEmailDoesNotBelongToAnotherUserAsync(int id, string email, CancellationToken cancellationToken = default)
        {
            var userExists = await _userRepo.GetByEmailAsync(email, cancellationToken);

            if (userExists != null && userExists.Id != id)
            {
                throw new DomainException("EMAIL_ALREADY_EXISTS", "A user with this email already exists.");
            }
        }

        private async Task TryDeleteProfileImageAsync(string profileImagePath)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await _uploadService.DeleteImageAsync(profileImagePath, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete profile image {ProfileImagePath}.",
                    profileImagePath);
            }
        }

        private void ValidateCurrentPassword(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                throw new UnauthorizedException("INVALID_PASSWORD", "Password doesn't match");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("INVALID_PASSWORD", "Password doesn't match");
            }
        }

        private static void ValidatePasswordStrength(string password)
        {
            const int minimumLength = 12;
            const int maximumLength = 128;

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new DomainException("PASSWORD_REQUIRED", "Password is required.");
            }

            if (password.Length < minimumLength)
            {
                throw new DomainException("PASSWORD_TOO_SHORT", $"Password must have at least {minimumLength} characters.");
            }

            if (password.Length > maximumLength)
            {
                throw new DomainException("PASSWORD_TOO_LONG", $"Password cannot exceed {maximumLength} characters.");
            }

            if (!password.Any(char.IsLower))
            {
                throw new DomainException("PASSWORD_LOWERCASE_REQUIRED", "Password must include at least one lowercase letter.");
            }

            if (!password.Any(char.IsUpper))
            {
                throw new DomainException("PASSWORD_UPPERCASE_REQUIRED", "Password must include at least one uppercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                throw new DomainException("PASSWORD_DIGIT_REQUIRED", "Password must include at least one number.");
            }

            if (!password.Any(character => !char.IsLetterOrDigit(character)))
            {
                throw new DomainException("PASSWORD_SYMBOL_REQUIRED", "Password must include at least one symbol.");
            }
        }

        private async Task<GetUserDTO> MapToGetUserDTOAsync(User user, CancellationToken cancellationToken)
        {
            return new GetUserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                UtCreation = user.UtCreation,
                HasLocalPassword = !string.IsNullOrWhiteSpace(user.PasswordHash),
                HasGoogleLogin = await _externalLoginRepository.HasProviderAsync(user.Id, "Google", cancellationToken)
            };
        }
    }
}
