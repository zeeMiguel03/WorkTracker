using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITasksRepository
    {
        Task AddAsync(Tasks tasks, CancellationToken cancellationToken = default);

        Task<Tasks?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Tasks>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Tasks> Items, int TotalCount)> GetPageByUserIdAsync(
            int userId,
            int page,
            int pageSize,
            int? taskStatusId,
            string? search,
            CancellationToken cancellationToken = default);

        void Update(Tasks tasks);

        void Remove(Tasks tasks);
    }
}
