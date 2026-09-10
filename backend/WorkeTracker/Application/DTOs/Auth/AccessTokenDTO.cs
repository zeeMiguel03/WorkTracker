namespace Application.DTOs.Auth
{
    public class AccessTokenDTO
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
