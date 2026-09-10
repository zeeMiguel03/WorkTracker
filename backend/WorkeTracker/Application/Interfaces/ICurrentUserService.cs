namespace Application.Interfaces.Services
{
    /// <summary>
    /// Provides information about the currently authenticated user.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the unique identifier of the currently authenticated user.
        /// </summary>
        /// <returns>The authenticated user's unique identifier.</returns>
        /// <exception cref="Application.Exceptions.UnauthorizedException">
        /// Thrown when the user is not authenticated or when the user identifier
        /// claim is missing or invalid.
        /// </exception>
        int GetUserId();

        int? GetUserIdOrNull();

        /// <summary>
        /// Gets the name of the currently authenticated user.
        /// </summary>
        /// <returns>
        /// The user's name, or <see langword="null"/> when the corresponding
        /// claim is not available.
        /// </returns>
        string? GetUserName();

        /// <summary>
        /// Gets the email address of the currently authenticated user.
        /// </summary>
        /// <returns>
        /// The user's email address, or <see langword="null"/> when the corresponding
        /// claim is not available.
        /// </returns>
        string? GetUserEmail();
    }
}
