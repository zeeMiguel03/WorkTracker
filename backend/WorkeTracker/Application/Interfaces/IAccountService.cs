using Application.DTOs.Account;

namespace Application.Interfaces
{
    public interface IAccountService
    {
        Task CreateAccountAsync(CreateAccountDTO accountDTO, CancellationToken cancellationToken = default);

        Task UpdateAccountAsync(UpdateAccountDTO accountDTO, CancellationToken cancellationToken = default);

        Task<List<ListAccountDTO>> ListAllAccountsByUserAsync(CancellationToken cancellationToken = default);

        Task<ListAccountDTO> ListAccountByIdAsync(int idAccount, CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(int idAccount, CancellationToken cancellation = default);
    }
}
