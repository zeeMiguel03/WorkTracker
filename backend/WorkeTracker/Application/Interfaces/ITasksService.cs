using Application.DTOs.Task;

namespace Application.Interfaces
{
    public interface ITasksService
    {
        Task<GetTaskDTO> CreateTaskAsync(CreateTaskDTO dto, CancellationToken cancellationToken = default);

        Task UpdateTaskAsync(UpdateTaskDTO dto, CancellationToken cancellationToken = default);

        Task DeleteTaskAsync(int idTask, CancellationToken cancellationToken = default);

        Task<GetTaskDTO> ListTaskByIdAsync(int idTask, CancellationToken cancellationToken = default);

        Task<List<GetTaskDTO>> ListTasksByUserAsync(CancellationToken cancellationToken = default);

        Task UpdateTaskStatusAsync(int idTask, int idTaskStatus, CancellationToken cancellationToken = default);

        Task UpdateTaskSortOrderAsync(int idTask, int sortOrder, CancellationToken cancellationToken = default);

        Task CompleteTaskAsync(int idTask, CancellationToken cancellationToken = default);

        Task ReopenTaskAsync(int idTask, CancellationToken cancellationToken = default);
    }
}
