namespace Application.DTOs.Task
{
    public class GetTaskDTO
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? SourceId { get; set; }

        public int TaskStatusId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Priority { get; set; }

        public int SortOrder { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
