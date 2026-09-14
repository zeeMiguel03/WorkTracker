using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    [Table("users")]
    public class User
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_EMAIL = 255;
        private const int MAX_LENGTH_PASSWORD_HASH = 500;
        private const int MAX_LENGTH_PROFILE_IMAGE = 500;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Column("name")]
        [Required]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("email")]
        [Required]
        [MaxLength(MAX_LENGTH_EMAIL)]
        public string Email { get; private set; } = string.Empty;

        [Column("password_hash")]
        [Required]
        [MaxLength(MAX_LENGTH_PASSWORD_HASH)]
        public string PasswordHash { get; private set; } = string.Empty;

        [Column("profile_image_url")]
        [MaxLength(MAX_LENGTH_PROFILE_IMAGE)]
        public string? ProfileImageUrl { get; private set; } 

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }

        [Required]
        [Column("token_version")]
        public int TokenVersion { get; private set; }

        public ICollection<TasksStatus> TasksStatus { get; private set; } = new List<TasksStatus>();
        public ICollection<Tasks> Tasks { get; private set; } = new List<Tasks>();
        public ICollection<Account> Accounts { get; private set; } = new List<Account>();
        public ICollection<Source> Sources { get; private set; } = new List<Source>();
        public ICollection<TransactionType> TransactionTypes { get; private set; } = new List<TransactionType>();
        public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

        private User() { }

        public static User Create(string name, string email, string? profileImageUrl, int? utCreation)
        {
            var user = new User
            {
                Name = NormalizeAndValidateName(name),
                Email = NormalizeAndValidateEmail(email),
                ProfileImageUrl = NormalizeAndValidateProfileImageUrl(profileImageUrl),
                TokenVersion = 0,
                UtCreation = utCreation,
                CreatedAt = DateTime.UtcNow
            };

            return user;
        }

        public void ChangeEmail(string email)
        {
            Email = NormalizeAndValidateEmail(email);
        }

        public void Edit(string name, string? profileImageUrl)
        {
            Name = NormalizeAndValidateName(name);
            ProfileImageUrl = NormalizeAndValidateProfileImageUrl(profileImageUrl);
        }

        public void SetPasswordHash(string passwordHash)
        {
            ValidatePasswordHash(passwordHash);

            PasswordHash = passwordHash;
        }

        public void InvalidateTokens()
        {
            TokenVersion = checked(TokenVersion + 1);
        }

        private static string NormalizeAndValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new DomainException("EMAIL_REQUIRED", "Email is required.");
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (normalizedEmail.Length > MAX_LENGTH_EMAIL)
            {
                throw new DomainException("EMAIL_TOO_LONG", "Email exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_EMAIL });
            }

            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            if (!regex.IsMatch(normalizedEmail))
            {
                throw new DomainException("INVALID_EMAIL_FORMAT", "Email format is invalid.");
            }

            return normalizedEmail;
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

        private static void ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainException("PASSWORD_HASH_REQUIRED", "Password hash is required.");
            }

            if (passwordHash.Length > MAX_LENGTH_PASSWORD_HASH)
            {
                throw new DomainException("PASSWORD_HASH_TOO_LONG", "Password hash exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_PASSWORD_HASH });
            }
        }

        private static string? NormalizeAndValidateProfileImageUrl(string? profileImageUrl)
        {
            if (string.IsNullOrWhiteSpace(profileImageUrl))
            {
                return string.Empty;
            }

            var normalizedUrl = profileImageUrl.Trim();

            if (normalizedUrl.Length > MAX_LENGTH_PROFILE_IMAGE)
            {
                throw new DomainException("PROFILE_IMAGE_URL_TOO_LONG", "Profile image URL exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_PROFILE_IMAGE });
            }

            return normalizedUrl;
        }
    }
}
