using Application.DTOs.Auth;

namespace Application.Interfaces
{
    public interface IGoogleIdentityVerifier
    {
        Task<GoogleIdentity> VerifyAsync(string credential, CancellationToken cancellationToken = default);
    }
}
