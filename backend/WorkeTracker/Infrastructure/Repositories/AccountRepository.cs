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

        public Task<List<Account>> GetUserAccountsAsync(int idUser, CancellationToken cancellationToken = default)
        {
            return _context.accounts
                .Where(account => account.UserId == idUser)
                .OrderBy(account => account.Name)
                .ToListAsync(cancellationToken);
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
