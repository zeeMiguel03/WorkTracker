using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private const int REFRESH_TOKEN_LIFETIME_DAYS = 10;
        private const int MAX_LENGTH_REFRESH_TOKEN = 512;

        private readonly IUserRepository _userRepo;
        private readonly IUserService _userService;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAccessTokenService _accessTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository userRepo,
            IUserService userService,
            IRefreshTokenRepository refreshTokenRepo,
            IPasswordHasher<User> passwordHasher,
            IAccessTokenService accessTokenService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _userService = userService;
            _refreshTokenRepo = refreshTokenRepo;
            _passwordHasher = passwordHasher;
            _accessTokenService = accessTokenService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
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

            if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.SetPasswordHash(_passwordHasher.HashPassword(user, dto.Password));
                _userRepo.Update(user);
            }

            return await CreateAuthenticatedUserAsync(user, null, cancellationToken);
        }

        public async Task<AuthenticatedUserDTO> RegisterAsync(CreateUserDTO dto, CancellationToken cancellationToken = default)
        {
            await _userService.CreateUserAsync(dto, cancellationToken);

            var user = await _userRepo.GetByEmailAsync(dto.Email, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("The registered user could not be loaded.");
            }

            return await CreateAuthenticatedUserAsync(user, null, cancellationToken);
        }

        public async Task<AuthenticatedUserDTO> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            ValidateRefreshToken(refreshToken);

            var utcNow = DateTime.UtcNow;
            var tokenHash = HashRefreshToken(refreshToken);
            var storedToken = await _refreshTokenRepo.GetByHashAsync(tokenHash, cancellationToken);

            if (storedToken is null || !storedToken.IsActive(utcNow))
            {
                throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
            }

            var user = await _userRepo.GetByIdAsync(storedToken.UserId, cancellationToken);

            if (user is null || user.TokenVersion != storedToken.TokenVersion)
            {
                throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
            }

            storedToken.Revoke(utcNow);
            _refreshTokenRepo.Update(storedToken);

            return await CreateAuthenticatedUserAsync(user, storedToken.ExpiresAt, cancellationToken);
        }

        public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > MAX_LENGTH_REFRESH_TOKEN)
            {
                return;
            }

            var tokenHash = HashRefreshToken(refreshToken);
            var storedToken = await _refreshTokenRepo.GetByHashAsync(tokenHash, cancellationToken);

            if (storedToken is null)
            {
                return;
            }

            storedToken.Revoke(DateTime.UtcNow);
            _refreshTokenRepo.Update(storedToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task LogoutAllAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            var user = await _userRepo.GetByIdAsync(currentUserId, cancellationToken);

            if (user is null)
            {
                throw new DomainException("USER_NOT_FOUND", "User was not found.");
            }

            user.InvalidateTokens();
            _userRepo.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<AuthenticatedUserDTO> CreateAuthenticatedUserAsync(
            User user,
            DateTime? sessionExpiresAt,
            CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var refreshTokenExpiresAt = sessionExpiresAt ?? utcNow.AddDays(REFRESH_TOKEN_LIFETIME_DAYS);

            if (refreshTokenExpiresAt <= utcNow)
            {
                throw new UnauthorizedException("REFRESH_TOKEN_EXPIRED", "The authentication session has expired.");
            }

            var refreshTokenValue = GenerateRefreshToken();
            var refreshTokenHash = HashRefreshToken(refreshTokenValue);
            var refreshToken = RefreshToken.Create(
                user.Id,
                refreshTokenHash,
                user.TokenVersion,
                refreshTokenExpiresAt);

            await _refreshTokenRepo.AddAsync(refreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _accessTokenService.Create(user);

            return new AuthenticatedUserDTO
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
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

        private static string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);

            return Convert
                .ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }

        private static void ValidateRefreshToken(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedException("REFRESH_TOKEN_REQUIRED", "Refresh token is required.");
            }

            if (refreshToken.Length > MAX_LENGTH_REFRESH_TOKEN)
            {
                throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
            }
        }
    }
}
