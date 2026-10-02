using Domain.Entities;

namespace Domain.Interfaces;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByProviderSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default);

    Task<ExternalLogin?> GetByUserAndProviderAsync(int userId, string provider, CancellationToken cancellationToken = default);

    Task<bool> HasProviderAsync(int userId, string provider, CancellationToken cancellationToken = default);

    Task AddAsync(ExternalLogin login, CancellationToken cancellationToken = default);
}
