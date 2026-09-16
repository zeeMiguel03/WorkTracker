using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SourceRepository : ISourceRepository
    {
        private readonly AppDbContext _context;

        public SourceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Source source, CancellationToken cancellationToken = default)
        {
            await _context.sources.AddAsync(source, cancellationToken);
        }

        public Task<Source?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.sources.FirstOrDefaultAsync(
                source => source.Id == id,
                cancellationToken);
        }

        public Task<Source?> GetByNameAndUserIdAsync(string name, int userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult<Source?>(null);
            }

            var normalizedName = name.Trim();

            return _context.sources.FirstOrDefaultAsync(
                source => source.Name == normalizedName && source.UserId == userId,
                cancellationToken);
        }

        public async Task<(IReadOnlyList<Source> Items, int TotalCount)> GetPageByUserIdAsync(
            int userId,
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var query = _context.sources
                .AsNoTracking()
                .Where(source => source.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();

                query = query.Where(source =>
                    source.Name.Contains(normalizedSearch));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(source => source.Name)
                .ThenBy(source => source.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public void Remove(Source source)
        {
            _context.sources.Remove(source);
        }

        public void Update(Source source)
        {
            _context.sources.Update(source);
        }
    }
}
