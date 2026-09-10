namespace Application.DTOs.TaskStatus
{
    public class CreateTaskStatusDTO
    {
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
