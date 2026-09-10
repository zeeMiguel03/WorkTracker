namespace Application.DTOs.Task
{
    public class CreateTaskDTO
    {
        public int? SourceId { get; set; }

        public int TaskStatusId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Priority { get; set; }

        public int SortOrder { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
