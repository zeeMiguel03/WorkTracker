using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("task")]
    public class Task
    {
        private const int MAX_LENGTH_TITLE = 150;
        private const int MAX_LENGTH_DESCRIPTION = 500;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Column("user_id")]
        [Required]
        public int UserId { get; private set; }

        [Column("source_id")]
        public int? SourceId { get; private set; }

        [Column("task_status_id")]
        [Required]
        public int TaskStatusId { get; private set; }

        [Column("title")]
        [Required]
        [MaxLength(MAX_LENGTH_TITLE)]
        public string Title { get; private set; } = string.Empty;

        [Column("description")]
        [MaxLength(MAX_LENGTH_DESCRIPTION)]
        public string? Description { get; private set; }

        [Column("priority")]
        [Required]
        public int Priority { get; private set; }

        [Column("sort_order")]
        [Required]
        public int SortOrder { get; private set; }

        [Column("due_date")]
        public DateTime? DueDate { get; private set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; private set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }


        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        [ForeignKey(nameof(TaskStatusId))]
        public TaskStatus TaskStatus { get; private set; } = null!;

        [ForeignKey(nameof(SourceId))]
        public Source? Source { get; private set; }

        private Task() { }

        public static Task Create(int userId, int? sourceId, int taskStatusId, string title,
            string? description, int priority, int sortOrder, DateTime? dueDate, int? utCreation)
        {
            var task = new Task
            {
                UserId = ValidateUserId(userId),
                SourceId = ValidateSourceId(sourceId),
                TaskStatusId = ValidateTaskStatusId(taskStatusId),
                Title = NormalizeAndValidateTitle(title),
                Description = NormalizeAndValidateDescription(description),
                Priority = ValidatePriority(priority),
                SortOrder = ValidateSortOrder(sortOrder),
                DueDate = dueDate,
                CompletedAt = null,
                CreatedAt = DateTime.UtcNow,
                UtCreation = utCreation
            };

            return task;
        }

        public void Update(int taskStatusId, string title, string? description, int priority, int sortOrder, DateTime? dueDate)
        {
            TaskStatusId = ValidateTaskStatusId(taskStatusId);
            Title = NormalizeAndValidateTitle(title);
            Description = NormalizeAndValidateDescription(description);
            Priority = ValidatePriority(priority);
            SortOrder = ValidateSortOrder(sortOrder);
            DueDate = dueDate;
        }

        public void UpdateStatus(int taskStatusId)
        {
            TaskStatusId = ValidateTaskStatusId(taskStatusId);
        }

        public void UpdateSortOrder(int sortOrder)
        {
            SortOrder = ValidateSortOrder(sortOrder);
        }

        public void Complete()
        {
            CompletedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            CompletedAt = null;
        }

        private static string NormalizeAndValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("TITLE_REQUIRED", "Title is required.");
            }

            var normalizedTitle = title.Trim();

            if (normalizedTitle.Length > MAX_LENGTH_TITLE)
            {
                throw new DomainException("TITLE_TOO_LONG", "Title exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_TITLE });
            }

            return normalizedTitle;
        }

        private static string? NormalizeAndValidateDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            var normalizedDescription = description.Trim();

            if (normalizedDescription.Length > MAX_LENGTH_DESCRIPTION)
            {
                throw new DomainException("DESCRIPTION_TOO_LONG", "Description exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_DESCRIPTION });
            }

            return normalizedDescription;
        }

        private static int ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            return userId;
        }

        private static int? ValidateSourceId(int? sourceId)
        {
            if (sourceId.HasValue && sourceId.Value <= 0)
            {
                throw new DomainException("INVALID_SOURCE_ID", "Source id is invalid.");
            }

            return sourceId;
        }

        private static int ValidateTaskStatusId(int taskStatusId)
        {
            if (taskStatusId <= 0)
            {
                throw new DomainException("INVALID_TASK_STATUS_ID", "Task status id is invalid.");
            }

            return taskStatusId;
        }

        private static int ValidatePriority(int priority)
        {
            if (priority < 0)
            {
                throw new DomainException("INVALID_PRIORITY", "Priority cannot be negative.");
            }

            return priority;
        }

        private static int ValidateSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
            {
                throw new DomainException("INVALID_SORT_ORDER", "Sort order cannot be negative.");
            }

            return sortOrder;
        }
    }
}