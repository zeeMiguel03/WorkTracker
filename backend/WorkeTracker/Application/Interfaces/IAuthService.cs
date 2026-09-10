using Application.DTOs.Auth;
using Application.DTOs.User;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthenticatedUserDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken = default);

        Task<AuthenticatedUserDTO> RegisterAsync(CreateUserDTO dto, CancellationToken cancellationToken = default);
    }
}
