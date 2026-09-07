using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISourceRepository
    {
        Task AddAsync(Source source, CancellationToken cancellationToken = default);

        Task<Source?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        void Update(Source source);

        void Remove(Source source);
    }
}
