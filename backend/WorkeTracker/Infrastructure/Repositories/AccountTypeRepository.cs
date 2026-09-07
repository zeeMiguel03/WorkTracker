using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AccountTypeRepository : IAccountTypeRepository
    {
        private readonly AppDbContext _context;

        public AccountTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AccountType accountType, CancellationToken cancellationToken = default)
        {
            await _context.accountsType.AddAsync(accountType, cancellationToken);
        }

        public async Task<IReadOnlyList<AccountType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.accountsType
                .AsNoTracking()
                .OrderBy(accountType => accountType.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<AccountType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.accountsType.FirstOrDefaultAsync(
                accountType => accountType.Id == id,
                cancellationToken);
        }

        public Task<AccountType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim();

            return _context.accountsType.FirstOrDefaultAsync(
                accountType => accountType.Name == normalizedName,
                cancellationToken);
        }

        public Task<bool> HasAssociatedAccountsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.accounts.AnyAsync(
                account => account.AccountTypeId == id,
                cancellationToken);
        }

        public void Remove(AccountType accountType)
        {
            _context.accountsType.Remove(accountType);
        }

        public void Update(AccountType accountType)
        {
            _context.accountsType.Update(accountType);
        }
    }
}
