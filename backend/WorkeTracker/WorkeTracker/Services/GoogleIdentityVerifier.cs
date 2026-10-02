using API.Options;
using Application.DTOs.Auth;
using Application.Exceptions;
using Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace API.Services;

public sealed class GoogleIdentityVerifier(IOptions<GoogleOptions> options) : IGoogleIdentityVerifier
{
    public async Task<GoogleIdentity> VerifyAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ClientId))
        {
            throw new InvalidOperationException("Google:ClientId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(credential) || credential.Length > 10_000)
        {
            throw new UnauthorizedException("INVALID_GOOGLE_TOKEN", "Google credential is invalid.");
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [options.Value.ClientId]
                });

            if (!payload.EmailVerified ||
                string.IsNullOrWhiteSpace(payload.Subject) ||
                string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new UnauthorizedException("INVALID_GOOGLE_TOKEN", "Google account is not verified.");
            }

            return new GoogleIdentity
            {
                Subject = payload.Subject,
                Email = payload.Email,
                Name = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name
            };
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedException("INVALID_GOOGLE_TOKEN", "Google credential is invalid or expired.");
        }
    }
}
