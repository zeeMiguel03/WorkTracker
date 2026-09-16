using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TasksRepository : ITasksRepository
    {
        private readonly AppDbContext _context;

        public TasksRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Tasks tasks, CancellationToken cancellationToken = default)
        {
            await _context.tasks.AddAsync(tasks, cancellationToken);
        }

        public Task<Tasks?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.tasks.FirstOrDefaultAsync(
                tasks => tasks.Id == id,
                cancellationToken);
        }

        public async Task<IReadOnlyList<Tasks>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.tasks
                .AsNoTracking()
                .Where(tasks => tasks.UserId == userId)
                .OrderBy(tasks => tasks.CompletedAt.HasValue)
                .ThenBy(tasks => tasks.SortOrder)
                .ThenBy(tasks => tasks.DueDate)
                .ThenBy(tasks => tasks.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Tasks> Items, int TotalCount)> GetPageByUserIdAsync(
            int userId,
            int page,
            int pageSize,
            int? taskStatusId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var query = _context.tasks
                .AsNoTracking()
                .Where(tasks => tasks.UserId == userId);

            if (taskStatusId.HasValue)
            {
                query = query.Where(tasks => tasks.TaskStatusId == taskStatusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();

                query = query.Where(tasks =>
                    tasks.Title.Contains(normalizedSearch) ||
                    (tasks.Description != null && tasks.Description.Contains(normalizedSearch)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(tasks => tasks.CompletedAt.HasValue)
                .ThenBy(tasks => tasks.SortOrder)
                .ThenBy(tasks => tasks.DueDate)
                .ThenBy(tasks => tasks.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public void Remove(Tasks tasks)
        {
            _context.tasks.Remove(tasks);
        }

        public void Update(Tasks tasks)
        {
            _context.tasks.Update(tasks);
        }
    }
}
