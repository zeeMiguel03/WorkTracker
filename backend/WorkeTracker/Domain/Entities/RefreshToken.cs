using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        private const int TOKEN_HASH_LENGTH = 64;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Required]
        [Column("token_hash")]
        [MaxLength(TOKEN_HASH_LENGTH)]
        public string TokenHash { get; private set; } = string.Empty;

        [Required]
        [Column("token_version")]
        public int TokenVersion { get; private set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Timestamp]
        [Column("row_version")]
        public byte[] RowVersion { get; private set; } = [];

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; private set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        private RefreshToken() { }

        public static RefreshToken Create(int userId, string tokenHash, int tokenVersion, DateTime expiresAt)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != TOKEN_HASH_LENGTH)
            {
                throw new DomainException("INVALID_REFRESH_TOKEN_HASH", "Refresh token hash is invalid.");
            }

            if (tokenVersion < 0)
            {
                throw new DomainException("INVALID_TOKEN_VERSION", "Token version is invalid.");
            }

            if (expiresAt <= DateTime.UtcNow)
            {
                throw new DomainException("INVALID_REFRESH_TOKEN_EXPIRATION", "Refresh token expiration is invalid.");
            }

            return new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                TokenVersion = tokenVersion,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
        }

        public bool IsActive(DateTime utcNow)
        {
            return RevokedAt is null && ExpiresAt > utcNow;
        }

        public void Revoke(DateTime utcNow)
        {
            RevokedAt ??= utcNow;
        }
    }
}
