using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISourceRepository
    {
        Task AddAsync(Source source, CancellationToken cancellationToken = default);

        Task<Source?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Source?> GetByNameAndUserIdAsync(string name, int userId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Source> Items, int TotalCount)> GetPageByUserIdAsync(
            int userId,
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default);

        void Update(Source source);

        void Remove(Source source);
    }
}
