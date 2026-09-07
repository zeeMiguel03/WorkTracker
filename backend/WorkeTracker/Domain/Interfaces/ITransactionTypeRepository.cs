using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITransactionTypeRepository
    {
        Task AddAsync(TransactionType transactionType, CancellationToken cancellationToken = default);

        Task<TransactionType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<TransactionType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TransactionType>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<bool> HasAssociatedEntriesAsync(int id, CancellationToken cancellationToken = default);

        void Update(TransactionType transactionType);

        void Remove(TransactionType transactionType);
    }
}
