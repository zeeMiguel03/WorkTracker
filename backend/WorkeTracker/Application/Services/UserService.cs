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
        private readonly IUserRepository _userRepo;
        private readonly IUploadService _uploadService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepo,
            IUploadService uploadService, 
            IPasswordHasher<User> passwordHasher,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _uploadService = uploadService;
            _passwordHasher = passwordHasher;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CreateUserAsync(CreateUserDTO user, CancellationToken cancellationToken = default)
        {
            var createdByUserId = _currentUserService.GetUserIdOrNull();

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                throw new DomainException("PASSWORD_REQUIRED", "Password is required.");
            }

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

            ValidateCurrentPassword(myUser, password.CurrentPassword);

            if (string.IsNullOrWhiteSpace(password.NewPassword))
            {
                throw new DomainException("PASSWORD_REQUIRED", "New password is required.");
            }

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

            return MapToGetUserDTO(user);
        }

        public async Task<GetUserDTO?> GetUserByIdAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.GetUserId();

            var user = await _userRepo.GetByIdAsync(currentUser, cancellationToken);

            if (user is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            return MapToGetUserDTO(user);
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
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("INVALID_PASSWORD", "Password doesn't match");
            }
        }

        private static GetUserDTO MapToGetUserDTO(User user)
        {
            return new GetUserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                UtCreation = user.UtCreation
            };
        }
    }
}
