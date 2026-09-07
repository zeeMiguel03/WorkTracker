namespace Application.DTOs.User
{
    public class GetUserDTO
    {
        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string? ProfileImageUrl { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public int? UtCreation { get; private set; }
    }
}
