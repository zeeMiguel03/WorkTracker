using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ExternalLoginRepository(AppDbContext context) : IExternalLoginRepository
{
    public Task<ExternalLogin?> GetByProviderSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default)
    {
        return context.external_logins
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.Provider == provider && x.ProviderSubject == subject,
                cancellationToken);
    }

    public Task<ExternalLogin?> GetByUserAndProviderAsync(int userId, string provider, CancellationToken cancellationToken = default)
    {
        return context.external_logins.SingleOrDefaultAsync(
            login => login.UserId == userId && login.Provider == provider,
            cancellationToken);
    }

    public Task<bool> HasProviderAsync(int userId, string provider, CancellationToken cancellationToken = default)
    {
        return context.external_logins.AnyAsync(
            login => login.UserId == userId && login.Provider == provider,
            cancellationToken);
    }

    public async Task AddAsync(ExternalLogin login, CancellationToken cancellationToken = default)
    {
        await context.external_logins.AddAsync(login, cancellationToken);
    }
}
