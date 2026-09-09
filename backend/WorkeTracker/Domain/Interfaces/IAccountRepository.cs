using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task AddAsync(Account account, CancellationToken cancellationToken = default);

        Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Account>> GetUserAccountsAsync(int idUser, CancellationToken cancellationToken = default);

        void Update(Account account);   

        void Remove(Account account);
    }
}
