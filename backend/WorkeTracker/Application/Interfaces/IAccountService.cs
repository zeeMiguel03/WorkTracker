using Application.DTOs.Account;
using Application.DTOs.Common;

namespace Application.Interfaces
{
    public interface IAccountService
    {
        Task<ListAccountDTO> CreateAccountAsync(CreateAccountDTO accountDTO, CancellationToken cancellationToken = default);

        Task UpdateAccountAsync(UpdateAccountDTO accountDTO, CancellationToken cancellationToken = default);

        Task<PagedResultDTO<ListAccountDTO>> ListAccountsByUserAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default);

        Task<ListAccountDTO> ListAccountByIdAsync(int idAccount, CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(int idAccount, CancellationToken cancellation = default);
    }
}
