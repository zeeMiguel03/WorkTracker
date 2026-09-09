using Application.DTOs.User;
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
            var createdByUserId =  _currentUserService.GetUserId();

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

                // Generate the password hash using the newly created entity.
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

        public Task UpdateUserAsync(int id, UpdateUserDTO user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ChangePasswordAsync(int id, ChangeUserPasswordDTO password, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }


        public Task<List<GetUserDTO>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GetUserDTO?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GetUserDTO?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private async Task ValidateUserDoesNotExistAsync(string email, CancellationToken cancellationToken = default)
        {
            var userExists = await _userRepo.EmailExistsAsync(email, cancellationToken);

            if (userExists)
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
                    "Failed to delete profile image {ProfileImagePath} after user creation failed.",
                    profileImagePath);
            }
        }
    }
}
