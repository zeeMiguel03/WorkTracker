using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Domain.Entities
{
    [Table("account_types")]
    public class AccountType
    {
        private const int MAX_LENGTH_NAME = 100;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Column("name")]
        [Required]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }

        private AccountType() { }

        public ICollection<Account> Accounts { get; private set; } = new List<Account>();

        public static AccountType Create(string name, int? utCreation)
        {
            var accountType = new AccountType
            {
                Name = NormalizeAndValidateName(name),
                UtCreation = utCreation,
                CreatedAt = DateTime.UtcNow
            };

            return accountType;
        }

        public void Update(string name)
        {
            Name = NormalizeAndValidateName(name);
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
    }
}
