using Application.DTOs.Auth;
using Application.DTOs.User;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthenticatedUserDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken = default);

        Task<AuthenticatedUserDTO> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);

        Task LinkGoogleAsync(string credential, CancellationToken cancellationToken = default);

        Task<AuthenticatedUserDTO> RegisterAsync(CreateUserDTO dto, CancellationToken cancellationToken = default);

        Task<AuthenticatedUserDTO> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

        Task LogoutAllAsync(CancellationToken cancellationToken = default);
    }
}
