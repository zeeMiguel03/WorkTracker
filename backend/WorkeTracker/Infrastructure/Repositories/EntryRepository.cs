using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class EntryRepository : IEntryRepository
    {
        private readonly AppDbContext _context;

        public EntryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Entry entry, CancellationToken cancellationToken = default)
        {
            await _context.entries.AddAsync(entry, cancellationToken);
        }

        public Task<Entry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.entries.FirstOrDefaultAsync(
                entry => entry.Id == id,
                cancellationToken);
        }

        public void Remove(Entry entry)
        {
            _context.entries.Remove(entry);
        }

        public void Update(Entry entry)
        {
            _context.entries.Update(entry);
        }
    }
}
