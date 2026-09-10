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
