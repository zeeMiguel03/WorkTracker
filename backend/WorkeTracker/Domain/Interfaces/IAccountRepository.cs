using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task AddAsync(Account account, CancellationToken cancellationToken = default);

        Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Account> Items, int TotalCount)> GetPageByUserIdAsync(
            int idUser,
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default);

        void Update(Account account);   

        void Remove(Account account);
    }
}
