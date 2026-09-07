using Application.DTOs.User;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<GetUserDTO> CreateUserAsync(CreateUserDTO user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<GetUserDTO> UpdateUserAsync(int id, UpdateUserDTO user, CancellationToken cancellationToken = default)
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
    }
}
