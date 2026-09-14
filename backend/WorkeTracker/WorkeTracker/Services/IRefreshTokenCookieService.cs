namespace API.Services
{
    public interface IRefreshTokenCookieService
    {
        string? Get();

        void Set(string refreshToken, DateTime expiresAt);

        void Delete();
    }
}
