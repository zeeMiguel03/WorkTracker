using Application.DTOs.TaskStatus;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services
{
    public class TasksStatusService : ITasksStatusService
    {
        private readonly ITasksStatusRepository _tasksStatusRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public TasksStatusService(
            ITasksStatusRepository tasksStatusRepo,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _tasksStatusRepo = tasksStatusRepo;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task CreateTaskStatusAsync(CreateTaskStatusDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            await ValidateNameAvailableAsync(dto.Name, currentUserId, null, cancellationToken);

            var taskStatus = TasksStatus.Create(
                dto.Name,
                dto.Color,
                dto.SortOrder,
                currentUserId,
                currentUserId);

            await _tasksStatusRepo.AddAsync(taskStatus, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateTaskStatusAsync(UpdateTaskStatusDTO dto, CancellationToken cancellationToken = default)
        {
            var taskStatus = await GetOwnedTaskStatusAsync(dto.Id, "You do not have permission to update this task status.", cancellationToken);

            await ValidateNameAvailableAsync(dto.Name, taskStatus.UserId, taskStatus.Id, cancellationToken);

            taskStatus.Update(dto.Name, dto.Color, dto.SortOrder);

            _tasksStatusRepo.Update(taskStatus);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteTaskStatusAsync(int idTaskStatus, CancellationToken cancellationToken = default)
        {
            var taskStatus = await GetOwnedTaskStatusAsync(idTaskStatus, "You do not have permission to delete this task status.", cancellationToken);

            if (await _tasksStatusRepo.HasAssociatedTasksAsync(taskStatus.Id, cancellationToken))
            {
                throw new DomainException("TASK_STATUS_IN_USE", "The task status cannot be deleted because it is associated with tasks.");
            }

            _tasksStatusRepo.Remove(taskStatus);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetTaskStatusDTO> ListTaskStatusByIdAsync(int idTaskStatus, CancellationToken cancellationToken = default)
        {
            var taskStatus = await GetOwnedTaskStatusAsync(idTaskStatus, "You do not have permission to access this task status.", cancellationToken);

            return MapToGetTaskStatusDTO(taskStatus);
        }

        public async Task<List<GetTaskStatusDTO>> ListTaskStatusesByUserAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            var taskStatuses = await _tasksStatusRepo.GetByUserIdAsync(currentUserId, cancellationToken);

            return taskStatuses
                .Select(MapToGetTaskStatusDTO)
                .ToList();
        }

        private async Task<TasksStatus> GetOwnedTaskStatusAsync(int taskStatusId, string accessDeniedMessage, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();

            var taskStatus = await _tasksStatusRepo.GetByIdAsync(taskStatusId, cancellationToken);

            if (taskStatus is null)
            {
                throw new DomainException("TASK_STATUS_NOT_FOUND", "Task status was not found.");
            }

            if (taskStatus.UserId != currentUserId)
            {
                throw new DomainException("TASK_STATUS_ACCESS_DENIED", accessDeniedMessage);
            }

            return taskStatus;
        }

        private async Task ValidateNameAvailableAsync(string name, int userId, int? taskStatusId, CancellationToken cancellationToken)
        {
            var taskStatusWithSameName = await _tasksStatusRepo.GetByNameAsync(userId, name, cancellationToken);

            if (taskStatusWithSameName is not null && (taskStatusId is null || taskStatusWithSameName.Id != taskStatusId.Value))
            {
                throw new DomainException("TASK_STATUS_NAME_ALREADY_EXISTS", "A task status with this name already exists.");
            }
        }

        private static GetTaskStatusDTO MapToGetTaskStatusDTO(TasksStatus taskStatus)
        {
            return new GetTaskStatusDTO
            {
                Id = taskStatus.Id,
                UserId = taskStatus.UserId,
                Name = taskStatus.Name,
                Color = taskStatus.Color,
                SortOrder = taskStatus.SortOrder,
                CreatedAt = taskStatus.CreatedAt,
                UtCreation = taskStatus.UtCreation
            };
        }
    }
}
