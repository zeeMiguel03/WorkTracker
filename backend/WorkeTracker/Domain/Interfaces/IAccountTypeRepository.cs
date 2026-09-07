using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAccountTypeRepository
    {
        Task AddAsync(AccountType accountType, CancellationToken cancellationToken = default);

        Task<AccountType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AccountType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AccountType>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<bool> HasAssociatedAccountsAsync(int id, CancellationToken cancellationToken = default);

        void Update(AccountType accountType);

        void Remove(AccountType accountType);
    }
}
