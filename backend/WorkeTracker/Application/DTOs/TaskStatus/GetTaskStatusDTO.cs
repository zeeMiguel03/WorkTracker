namespace Application.DTOs.TaskStatus
{
    public class GetTaskStatusDTO
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
