using Application.DTOs.Common;
using Application.DTOs.Task;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services
{
    /// <summary>
    /// Provides application services for managing tasks, including creation,
    /// updates, deletion, retrieval, status changes, ordering, and completion.
    /// Ensures that tasks and their related entities belong to the authenticated user.
    /// </summary>
    public class TasksService : ITasksService
    {
        private readonly ITasksRepository _tasksRepo;
        private readonly ITasksStatusRepository _tasksStatusRepo;
        private readonly ISourceRepository _sourceRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="TasksService"/> class.
        /// </summary>
        /// <param name="tasksRepo">Repository responsible for task persistence and retrieval.</param>
        /// <param name="tasksStatusRepo">Repository responsible for task status retrieval.</param>
        /// <param name="sourceRepo">Repository responsible for source retrieval.</param>
        /// <param name="currentUserService">Service used to identify the authenticated user.</param>
        /// <param name="unitOfWork">Unit of work responsible for persisting changes.</param>
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

        /// <summary>
        /// Creates a new task for the authenticated user.
        /// Validates the associated source and task status before persisting the task.
        /// </summary>
        /// <param name="dto">Data required to create the task.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A DTO containing the newly created task information.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the associated source or task status does not exist
        /// or does not belong to the authenticated user.
        /// </exception>
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

        /// <summary>
        /// Updates an existing task belonging to the authenticated user.
        /// Validates ownership and the associated entities before applying changes.
        /// </summary>
        /// <param name="dto">Data containing the task identifier and updated information.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task, source, or task status does not exist
        /// or the authenticated user does not have permission to access it.
        /// </exception>
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

        /// <summary>
        /// Deletes an existing task belonging to the authenticated user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to delete.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task does not exist or does not belong to the authenticated user.
        /// </exception>
        public async Task DeleteTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to delete this task.", cancellationToken);

            _tasksRepo.Remove(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a specific task by its identifier.
        /// Ensures that the requested task belongs to the authenticated user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to retrieve.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A DTO containing the requested task information.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task does not exist or the authenticated user
        /// does not have permission to access it.
        /// </exception>
        public async Task<GetTaskDTO> ListTaskByIdAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to access this task.", cancellationToken);

            return MapToGetTaskDTO(tasks);
        }

        /// <summary>
        /// Retrieves a paginated list of tasks belonging to the authenticated user.
        /// Supports filtering by task status and searching by a text query.
        /// </summary>
        /// <param name="page">Page number, normalized to a minimum value of 1.</param>
        /// <param name="pageSize">Number of items per page, limited to a range of 1 to 50.</param>
        /// <param name="taskStatusId">
        /// Optional task status identifier used to filter results.
        /// Values less than or equal to zero are ignored.
        /// </param>
        /// <param name="search">Optional search text used to filter tasks.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A paginated result containing task DTOs, pagination information,
        /// and the total number of matching tasks.
        /// </returns>
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

        /// <summary>
        /// Changes the status of an existing task belonging to the authenticated user.
        /// Ensures that the new task status also belongs to the same user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to update.</param>
        /// <param name="idTaskStatus">Identifier of the new task status.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task or task status does not exist,
        /// or when the authenticated user does not have permission to access it.
        /// </exception>
        public async Task UpdateTaskStatusAsync(int idTask, int idTaskStatus, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to update this task.", cancellationToken);

            await ValidateTaskStatusAsync(idTaskStatus, tasks.UserId, cancellationToken);

            tasks.UpdateStatus(idTaskStatus);

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the sort order of an existing task belonging to the authenticated user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to reorder.</param>
        /// <param name="sortOrder">New sorting position assigned to the task.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task does not exist or does not belong to the authenticated user.
        /// </exception>
        public async Task UpdateTaskSortOrderAsync(int idTask, int sortOrder, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to reorder this task.", cancellationToken);

            tasks.UpdateSortOrder(sortOrder);

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Marks an existing task as completed.
        /// Ensures that the task belongs to the authenticated user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to complete.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task does not exist or does not belong to the authenticated user.
        /// </exception>
        public async Task CompleteTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to complete this task.", cancellationToken);

            tasks.Complete();

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Reopens a previously completed task belonging to the authenticated user.
        /// </summary>
        /// <param name="idTask">Unique identifier of the task to reopen.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the task does not exist or does not belong to the authenticated user.
        /// </exception>
        public async Task ReopenTaskAsync(int idTask, CancellationToken cancellationToken = default)
        {
            var tasks = await GetOwnedTaskAsync(idTask, "You do not have permission to reopen this task.", cancellationToken);

            tasks.Reopen();

            _tasksRepo.Update(tasks);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a task and verifies that it belongs to the authenticated user.
        /// </summary>
        /// <param name="taskId">Unique identifier of the task to retrieve.</param>
        /// <param name="accessDeniedMessage">
        /// Error message used when the authenticated user does not own the task.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>The requested task entity if it exists and belongs to the authenticated user.</returns>
        /// <exception cref="DomainException">
        /// Thrown with TASK_NOT_FOUND when the task does not exist,
        /// or TASK_ACCESS_DENIED when the authenticated user does not own the task.
        /// </exception>
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

        /// <summary>
        /// Validates that the associated task status and optional source exist
        /// and belong to the specified user.
        /// </summary>
        /// <param name="sourceId">Optional identifier of the associated source.</param>
        /// <param name="taskStatusId">Identifier of the associated task status.</param>
        /// <param name="userId">Identifier of the user who must own the related entities.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous validation operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the source or task status does not exist
        /// or does not belong to the specified user.
        /// </exception>
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

        /// <summary>
        /// Validates that a task status exists and belongs to the specified user.
        /// </summary>
        /// <param name="taskStatusId">Identifier of the task status to validate.</param>
        /// <param name="userId">Identifier of the user who must own the task status.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous validation operation.</returns>
        /// <exception cref="DomainException">
        /// Thrown with TASK_STATUS_NOT_FOUND when the status does not exist,
        /// or TASK_STATUS_ACCESS_DENIED when it does not belong to the specified user.
        /// </exception>
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

        /// <summary>
        /// Maps a task domain entity to its corresponding data transfer object.
        /// </summary>
        /// <param name="tasks">Task entity containing the data to map.</param>
        /// <returns>A DTO containing the task information.</returns>
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
