using Application.DTOs.User;
using System.Text.Json.Serialization;

namespace Application.DTOs.Auth
{
    public class AuthenticatedUserDTO
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime RefreshTokenExpiresAt { get; set; }

        public GetUserDTO User { get; set; } = new();
    }
}
