namespace Application.DTOs.User
{
    public class GetUserDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
