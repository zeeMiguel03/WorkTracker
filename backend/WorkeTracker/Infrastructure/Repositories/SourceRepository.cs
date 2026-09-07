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
