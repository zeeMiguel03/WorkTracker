using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    [Table("task_status")]
    public class TaskStatus
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_COLOR = 7;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Column("name")]
        [Required]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("color")]
        [Required]
        [MaxLength(MAX_LENGTH_COLOR)]
        public string Color { get; private set; } = string.Empty;

        [Column("sort_order")]
        [Required]
        public int SortOrder { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }


        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        public ICollection<Task> Tasks { get; private set; } = new List<Task>();

        private TaskStatus() { }

        public static TaskStatus Create(string name, string color, int sortOrder, int userId, int? utCreation)
        {
            var taskStatus = new TaskStatus
            {
                Name = NormalizeAndValidateName(name),
                Color = NormalizeAndValidateColor(color),
                SortOrder = ValidateSortOrder(sortOrder),
                UserId = userId,
                UtCreation = utCreation,
                CreatedAt = DateTime.UtcNow
            };

            return taskStatus;
        }

        public void Update(string name, string color, int sortOrder)
        {
            Name = NormalizeAndValidateName(name);
            Color = NormalizeAndValidateColor(color);
            SortOrder = ValidateSortOrder(sortOrder);
        }

        private static string NormalizeAndValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("NAME_REQUIRED", "Name is required.");
            }

            var normalizedName = name.Trim();

            if (normalizedName.Length > MAX_LENGTH_NAME)
            {
                throw new DomainException("NAME_TOO_LONG", "Name exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_NAME });
            }

            return normalizedName;
        }

        private static string NormalizeAndValidateColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                throw new DomainException("COLOR_REQUIRED", "Color is required.");
            }

            var normalizedColor = color.Trim();

            if (normalizedColor.Length > MAX_LENGTH_COLOR)
            {
                throw new DomainException("COLOR_TOO_LONG", "Color exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_COLOR });
            }

            if (!Regex.IsMatch(normalizedColor, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"))
            {
                throw new DomainException("INVALID_COLOR", "Color is invalid.");
            }

            return normalizedColor.ToUpperInvariant();
        }

        private static int ValidateSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
            {
                throw new DomainException("INVALID_SORT_ORDER", "Sort order cannot be negative."
                );
            }

            return sortOrder;
        }
    }
}
