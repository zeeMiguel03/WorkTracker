using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITasksRepository
    {
        Task AddAsync(Tasks tasks, CancellationToken cancellationToken = default);

        Task<Tasks?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        void Update(Tasks tasks);

        void Remove(Tasks tasks);
    }
}
