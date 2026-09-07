using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITasksStatusRepository
    {
        Task AddAsync(TasksStatus tasksStatus, CancellationToken cancellationToken = default);

        Task<TasksStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<TasksStatus?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TasksStatus>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<bool> HasAssociatedTasksAsync(int id, CancellationToken cancellationToken = default);

        void Update(TasksStatus tasksStatus);

        void Remove(TasksStatus tasksStatus);
    }
}
