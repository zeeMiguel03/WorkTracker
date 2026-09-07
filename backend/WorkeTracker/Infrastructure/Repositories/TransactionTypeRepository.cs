using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TransactionTypeRepository : ITransactionTypeRepository
    {
        private readonly AppDbContext _context;

        public TransactionTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TransactionType transactionType, CancellationToken cancellationToken = default)
        {
            await _context.transaction_types.AddAsync(transactionType, cancellationToken);
        }

        public async Task<IReadOnlyList<TransactionType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.transaction_types
                .AsNoTracking()
                .OrderBy(transactionType => transactionType.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<TransactionType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.transaction_types.FirstOrDefaultAsync(
                transactionType => transactionType.Id == id,
                cancellationToken);
        }

        public Task<TransactionType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim();

            return _context.transaction_types.FirstOrDefaultAsync(
                transactionType => transactionType.Name == normalizedName,
                cancellationToken);
        }

        public Task<bool> HasAssociatedEntriesAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.entries.AnyAsync(
               entry => entry.TransactionTypeId == id,
               cancellationToken);
        }

        public void Remove(TransactionType transactionType)
        {
            _context.transaction_types.Remove(transactionType);
        }

        public void Update(TransactionType transactionType)
        {
            _context.transaction_types.Update(transactionType);
        }
    }
}
