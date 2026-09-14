namespace API.Services
{
    public class RefreshTokenCookieService : IRefreshTokenCookieService
    {
        private const string COOKIE_NAME = "refresh_token";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;

        public RefreshTokenCookieService(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }

        public string? Get()
        {
            return GetHttpContext().Request.Cookies[COOKIE_NAME];
        }

        public void Set(string refreshToken, DateTime expiresAt)
        {
            GetHttpContext().Response.Cookies.Append(
                COOKIE_NAME,
                refreshToken,
                CreateCookieOptions(expiresAt));
        }

        public void Delete()
        {
            GetHttpContext().Response.Cookies.Delete(
                COOKIE_NAME,
                CreateCookieOptions(null));
        }

        private HttpContext GetHttpContext()
        {
            return _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context is not available.");
        }

        private CookieOptions CreateCookieOptions(DateTime? expiresAt)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = expiresAt,
                IsEssential = true
            };
        }
    }
}
