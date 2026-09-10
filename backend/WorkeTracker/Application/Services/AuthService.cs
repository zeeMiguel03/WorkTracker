using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IUserService _userService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAccessTokenService _accessTokenService;

        public AuthService(
            IUserRepository userRepo,
            IUserService userService,
            IPasswordHasher<User> passwordHasher,
            IAccessTokenService accessTokenService)
        {
            _userRepo = userRepo;
            _userService = userService;
            _passwordHasher = passwordHasher;
            _accessTokenService = accessTokenService;
        }

        public async Task<AuthenticatedUserDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new UnauthorizedException("INVALID_CREDENTIALS", "Email or password is invalid.");
            }

            var user = await _userRepo.GetByEmailAsync(dto.Email, cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedException("INVALID_CREDENTIALS", "Email or password is invalid.");
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("INVALID_CREDENTIALS", "Email or password is invalid.");
            }

            return CreateAuthenticatedUser(user);
        }

        public async Task<AuthenticatedUserDTO> RegisterAsync(CreateUserDTO dto, CancellationToken cancellationToken = default)
        {
            await _userService.CreateUserAsync(dto, cancellationToken);

            var user = await _userRepo.GetByEmailAsync(dto.Email, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("The registered user could not be loaded.");
            }

            return CreateAuthenticatedUser(user);
        }

        private AuthenticatedUserDTO CreateAuthenticatedUser(User user)
        {
            var accessToken = _accessTokenService.Create(user);

            return new AuthenticatedUserDTO
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                User = new GetUserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    ProfileImageUrl = user.ProfileImageUrl,
                    CreatedAt = user.CreatedAt,
                    UtCreation = user.UtCreation
                }
            };
        }
    }
}
