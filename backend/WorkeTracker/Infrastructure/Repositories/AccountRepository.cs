using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            await _context.accounts.AddAsync(account, cancellationToken);
        }

        public Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.accounts.FirstOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
        }

        public async Task<(IReadOnlyList<Account> Items, int TotalCount)> GetPageByUserIdAsync(
            int idUser,
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var query = _context.accounts
                .AsNoTracking()
                .Where(account => account.UserId == idUser);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();

                query = query.Where(account =>
                    account.Name.Contains(normalizedSearch) ||
                    (account.BankName != null && account.BankName.Contains(normalizedSearch)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(account => account.Name)
                .ThenBy(account => account.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public void Remove(Account account)
        {
            _context.accounts.Remove(account);
        }

        public void Update(Account account)
        {
            _context.accounts.Update(account);
        }
    }
}
