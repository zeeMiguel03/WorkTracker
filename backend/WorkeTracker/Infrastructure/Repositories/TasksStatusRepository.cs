using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TasksStatusRepository : ITasksStatusRepository
    {
        private readonly AppDbContext _context;

        public TasksStatusRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TasksStatus tasksStatus, CancellationToken cancellationToken = default)
        {
            await _context.task_status.AddAsync(tasksStatus, cancellationToken);
        }

        public async Task<IReadOnlyList<TasksStatus>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.task_status
                .AsNoTracking()
                .Where(tasksStatus => tasksStatus.UserId == userId)
                .OrderBy(tasksStatus => tasksStatus.SortOrder)
                .ToListAsync(cancellationToken);
        }

        public Task<TasksStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.task_status.FirstOrDefaultAsync(
                tasksStatus => tasksStatus.Id == id,
                cancellationToken);
        }

        public Task<TasksStatus?> GetByNameAsync(int userId, string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult<TasksStatus?>(null);
            }

            var normalizedName = name.Trim();

            return _context.task_status.FirstOrDefaultAsync(
                tasksStatus => tasksStatus.UserId == userId && tasksStatus.Name == normalizedName,
                cancellationToken);
        }

        public Task<bool> HasAssociatedTasksAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.tasks.AnyAsync(
                tasks => tasks.TaskStatusId == id,
                cancellationToken);
        }

        public void Remove(TasksStatus tasksStatus)
        {
            _context.task_status.Remove(tasksStatus);
        }

        public void Update(TasksStatus tasksStatus)
        {
            _context.task_status.Update(tasksStatus);
        }
    }
}
