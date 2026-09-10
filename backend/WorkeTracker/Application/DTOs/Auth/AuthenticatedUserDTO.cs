using Application.DTOs.User;

namespace Application.DTOs.Auth
{
    public class AuthenticatedUserDTO
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public GetUserDTO User { get; set; } = new();
    }
}
