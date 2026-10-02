using Application.DTOs.User;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<Domain.Entities.User> AddExternalUserAsync(string name, string email, CancellationToken cancellationToken = default);

        Task CreateUserAsync(CreateUserDTO user, CancellationToken cancellationToken = default);

        Task UpdateUserAsync(UpdateUserDTO user, CancellationToken cancellationToken = default);

        Task ChangeEmailAsync(ChangeUserEmailDTO email, CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(ChangeUserPasswordDTO password, CancellationToken cancellationToken = default);

        Task DeleteUserAsync(CancellationToken cancellationToken = default);

        Task<GetUserDTO?> GetUserByIdAsync(CancellationToken cancellationToken = default);

        Task<Stream?> GetProfileImageAsync(CancellationToken cancellationToken = default);

        Task<GetUserDTO?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
