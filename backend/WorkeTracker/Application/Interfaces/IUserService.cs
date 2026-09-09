using Application.DTOs.User;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task CreateUserAsync(CreateUserDTO user, CancellationToken cancellationToken = default);

        Task UpdateUserAsync(int id, UpdateUserDTO user, CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(int id, ChangeUserPasswordDTO password, CancellationToken cancellationToken = default);

        Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);

        Task<List<GetUserDTO>> GetAllUsersAsync(CancellationToken cancellationToken = default);

        Task<GetUserDTO?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<GetUserDTO?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}