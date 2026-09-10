namespace Application.DTOs.Source
{
    public class GetSourceDTO
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }

        public int? UtCreation { get; set; }
    }
}
