using Application.DTOs.Common;
using Application.DTOs.Task;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services
{
    public class TasksService : ITasksService
    {
        private readonly ITasksRepository _tasksRepo;
        private readonly ITasksStatusRepository _tasksStatusRepo;
        private readonly ISourceRepository _sourceRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public TasksService(
            ITasksRepository tasksRepo,
            ITasksStatusRepository tasksStatusRepo,
            ISourceRepository sourceRepo,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _tasksRepo = tasksRepo;
            _tasksStatusRepo = tasksStatusRepo;
            _sourceRepo = sourceRepo;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<GetTaskDTO> CreateTaskAsync(CreateTaskDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            await ValidateRelationsAsync(dto.SourceId, dto.TaskStatusId, currentUserId, cancellationToken);

            var tasks = Tasks.Create(
                currentUserId,
                dto.SourceId,
                dto.TaskStatusId,
                dto.Title,
                dto.Description,
                dto.Priority,
                dto.SortOrder,
                dto.DueDate,
                currentUserId);

            await _tasksRepo.AddAsync(tasks, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToGetTaskDTO(tasks);
        }

        public async Task UpdateTaskAsync(UpdateTaskDTO dto, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(dto.Id, "You do not have permission to update this task.", cancellationToken);

            await ValidateRelationsAsync(dto.SourceId, dto.TaskStatusId, tasks.UserId, cancellationToken);

            tasks.Update(
                dto.SourceId,
                dto.TaskStatusId,
                dto.Title,
                dto.Description,
                dto.Priority,
                dto.SortOrder,
                dto.DueDate);

            _tasksRepo.Update(tasks);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to delete this task.", cancellationToken);

            _tasksRepo.Remove(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetTaskDTO> ListTaskByIdAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to access this task.", cancellationToken);

            return MapToGetTaskDTO(tasks);
        }

        public async Task<PagedResultDTO<GetTaskDTO>> ListTasksByUserAsync(
            int page,
            int pageSize,
            int? taskStatusId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            if (taskStatusId is <= 0)
            {
                taskStatusId = null;
            }

            var (tasks, totalCount) = await _tasksRepo.GetPageByUserIdAsync(
                currentUserId,
                page,
                pageSize,
                taskStatusId,
                search,
                cancellationToken);

            return new PagedResultDTO<GetTaskDTO>
            {
                Items = tasks.Select(MapToGetTaskDTO).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount
            };
        }

        public async Task UpdateTaskStatusAsync(int idTask, int idTaskStatus, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to update this task.", cancellationToken);

            await ValidateTaskStatusAsync(idTaskStatus, tasks.UserId, cancellationToken);

            tasks.UpdateStatus(idTaskStatus);

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateTaskSortOrderAsync(int idTask, int sortOrder, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to reorder this task.", cancellationToken);

            tasks.UpdateSortOrder(sortOrder);

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task CompleteTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to complete this task.", cancellationToken);

            tasks.Complete();

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ReopenTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to reopen this task.", cancellationToken);

            tasks.Reopen();

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Tasks> GetOwnedTaskAsync(int taskId, string accessDeniedMessage, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();

            var tasks = await _tasksRepo.GetByIdAsync(taskId, cancellationToken);

            if (tasks is null)
            {
                throw new DomainException("TASK_NOT_FOUND", "Task was not found.");
            }

            if (tasks.UserId != currentUserId)
            {
                throw new DomainException("TASK_ACCESS_DENIED", accessDeniedMessage);
            }

            return tasks;
        }

        private async Task ValidateRelationsAsync(int? sourceId, int taskStatusId, int userId, CancellationToken cancellationToken)
        {
            await ValidateTaskStatusAsync(taskStatusId, userId, cancellationToken);

            if (sourceId is null)
            {
                return;
            }

            var source = await _sourceRepo.GetByIdAsync(sourceId.Value, cancellationToken);

            if (source is null)
            {
                throw new DomainException("SOURCE_NOT_FOUND", "Source was not found.");
            }

            if (source.UserId != userId)
            {
                throw new DomainException("SOURCE_ACCESS_DENIED", "You do not have permission to use this source.");
            }
        }

        private async Task ValidateTaskStatusAsync(int taskStatusId, int userId, CancellationToken cancellationToken)
        {
            var taskStatus = await _tasksStatusRepo.GetByIdAsync(taskStatusId, cancellationToken);

            if (taskStatus is null)
            {
                throw new DomainException("TASK_STATUS_NOT_FOUND", "Task status was not found.");
            }

            if (taskStatus.UserId != userId)
            {
                throw new DomainException("TASK_STATUS_ACCESS_DENIED", "You do not have permission to use this task status.");
            }
        }

        private static GetTaskDTO MapToGetTaskDTO(Tasks tasks)
        {
            return new GetTaskDTO
            {
                Id = tasks.Id,
                UserId = tasks.UserId,
                SourceId = tasks.SourceId,
                TaskStatusId = tasks.TaskStatusId,
                Title = tasks.Title,
                Description = tasks.Description,
                Priority = tasks.Priority,
                SortOrder = tasks.SortOrder,
                DueDate = tasks.DueDate,
                CompletedAt = tasks.CompletedAt,
                CreatedAt = tasks.CreatedAt,
                UtCreation = tasks.UtCreation
            };
        }
    }
}
