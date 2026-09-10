using Application.Exceptions;
using Application.Interfaces.Services;
using System.Security.Claims;

namespace API.Services;

/// <summary>
/// Provides information about the currently authenticated user by reading
/// claims from the current HTTP context.
/// </summary>
/// <remarks>
/// This service uses <see cref="IHttpContextAccessor"/> to access the
/// <see cref="ClaimsPrincipal"/> associated with the current HTTP request.
/// </remarks>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// The accessor used to obtain the current HTTP context.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContextAccessor"/> is
    /// <see langword="null"/>.
    /// </exception>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the principal associated with the current HTTP request.
    /// </summary>
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public int GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("USER_NOT_AUTHENTICATED", "User is not authenticated.");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("INVALID_USER_ID", "The authenticated user ID is invalid.");
        }

        return userId;
    }

    /// <inheritdoc />
    public int? GetUserIdOrNull()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    /// <inheritdoc />
    public string? GetUserName()
    {
        return User?.FindFirstValue(ClaimTypes.Name);
    }
    
    /// <inheritdoc />
    public string? GetUserEmail()
    {
        return User?.FindFirstValue(ClaimTypes.Email);
    }
}
