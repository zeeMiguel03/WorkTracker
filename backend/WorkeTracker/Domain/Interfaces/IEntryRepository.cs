using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IEntryRepository
    {
        Task AddAsync(Entry entry, CancellationToken cancellationToken = default);

        Task<Entry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        void Update(Entry entry);

        void Remove(Entry entry);
    }
}
