using Application.DTOs.TaskStatus;

namespace Application.Interfaces
{
    public interface ITasksStatusService
    {
        Task<GetTaskStatusDTO> CreateTaskStatusAsync(CreateTaskStatusDTO dto, CancellationToken cancellationToken = default);

        Task UpdateTaskStatusAsync(UpdateTaskStatusDTO dto, CancellationToken cancellationToken = default);

        Task DeleteTaskStatusAsync(int idTaskStatus, CancellationToken cancellationToken = default);

        Task<GetTaskStatusDTO> ListTaskStatusByIdAsync(int idTaskStatus, CancellationToken cancellationToken = default);

        Task<List<GetTaskStatusDTO>> ListTaskStatusesByUserAsync(CancellationToken cancellationToken = default);
    }
}
