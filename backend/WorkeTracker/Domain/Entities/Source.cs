using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("sources")]
    public class Source
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_IMAGE_URL = 500;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Required]
        [Column("name")]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("image_url")]
        [MaxLength(MAX_LENGTH_IMAGE_URL)]
        public string? ImageUrl { get; private set; }

        [Column("link")]
        public string? Link { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }


        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        public ICollection<Tasks> Tasks { get; private set; } = new List<Tasks>();

        private Source() { }

        public static Source Create(int userId, string name, string? imageUrl, string? link, bool isActive, int? utCreation)
        {
            var source = new Source
            {
                UserId = ValidateUserId(userId),
                Name = NormalizeAndValidateName(name),
                ImageUrl = NormalizeAndValidateImageUrl(imageUrl),
                Link = link,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                UtCreation = utCreation
            };

            return source;
        }

        public void Update(string name, string? imageUrl, string? link, bool isActive)
        {
            Name = NormalizeAndValidateName(name);
            ImageUrl = NormalizeAndValidateImageUrl(imageUrl);
            Link = link;
            IsActive = isActive;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
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

        private static string? NormalizeAndValidateImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var normalizedImageUrl = imageUrl.Trim();

            if (normalizedImageUrl.Length > MAX_LENGTH_IMAGE_URL)
            {
                throw new DomainException("IMAGE_URL_TOO_LONG", "Image URL exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_IMAGE_URL });
            }

            return normalizedImageUrl;
        }

        private static int ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            return userId;
        }
    }
}
