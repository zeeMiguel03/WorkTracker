using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("entries")]
    public class Entry
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_IMAGE_URL = 500;
        private const int MAX_LENGTH_DESCRIPTION = 500;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Required]
        [Column("source_id")]
        public int SourceId { get; private set; }

        [Required]
        [Column("transaction_type_id")]
        public int TransactionTypeId { get; private set; }

        [Required]
        [Column("account_id")]
        public int AccountId { get; private set; }

        [Required]
        [Column("name")]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("image_url")]
        [MaxLength(MAX_LENGTH_IMAGE_URL)]
        public string? ImageUrl { get; private set; }

        [Column("description")]
        [MaxLength(MAX_LENGTH_DESCRIPTION)]
        public string? Description { get; private set; }

        [Required]
        [Column("quantity", TypeName = "decimal(10,2)")]
        public decimal Quantity { get; private set; }

        [Required]
        [Column("value", TypeName = "decimal(10,2)")]
        public decimal Value { get; private set; }

        [Required]
        [Column("date")]
        public DateTime Date { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }


        [ForeignKey(nameof(SourceId))]
        public Source Source { get; private set; } = null!;

        [ForeignKey(nameof(TransactionTypeId))]
        public TransactionType TransactionType { get; private set; } = null!;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; private set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        private Entry() { }

        public static Entry Create(int userId, int sourceId, int transactionTypeId, int accountId, string name, string? imageUrl,
            string? description, decimal quantity, decimal value, DateTime date, int? utCreation)
        {
            var entry = new Entry
            {
                UserId = ValidateUserId(userId),
                SourceId = ValidateSourceId(sourceId),
                TransactionTypeId = ValidateTransactionTypeId(transactionTypeId),
                AccountId = ValidateAccountId(accountId),
                Name = NormalizeAndValidateName(name),
                ImageUrl = NormalizeAndValidateImageUrl(imageUrl),
                Description = NormalizeAndValidateDescription(description),
                Quantity = ValidateQuantity(quantity),
                Value = ValidateValue(value),
                Date = ValidateDate(date),
                CreatedAt = DateTime.UtcNow,
                UtCreation = utCreation
            };

            return entry;
        }

        public void Update(int sourceId, int transactionTypeId, int accountId, string name,
            string? imageUrl, string? description, decimal quantity, decimal value, DateTime date)
        {
            SourceId = ValidateSourceId(sourceId);
            TransactionTypeId = ValidateTransactionTypeId(transactionTypeId);
            AccountId = ValidateAccountId(accountId);
            Name = NormalizeAndValidateName(name);
            ImageUrl = NormalizeAndValidateImageUrl(imageUrl);
            Description = NormalizeAndValidateDescription(description);
            Quantity = ValidateQuantity(quantity);
            Value = ValidateValue(value);
            Date = ValidateDate(date);
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

        private static int ValidateSourceId(int sourceId)
        {
            if (sourceId <= 0)
            {
                throw new DomainException("INVALID_SOURCE_ID", "Source id is invalid.");
            }

            return sourceId;
        }

        private static int ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            return userId;
        }

        private static int ValidateTransactionTypeId(int transactionTypeId)
        {
            if (transactionTypeId <= 0)
            {
                throw new DomainException("INVALID_TRANSACTION_TYPE_ID", "Transaction type id is invalid.");
            }

            return transactionTypeId;
        }

        private static int ValidateAccountId(int accountId)
        {
            if (accountId <= 0)
            {
                throw new DomainException("INVALID_ACCOUNT_ID", "Account id is invalid.");
            }

            return accountId;
        }

        private static decimal ValidateQuantity(decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new DomainException("INVALID_QUANTITY", "Quantity must be greater than zero.");
            }

            return quantity;
        }

        private static decimal ValidateValue(decimal value)
        {
            if (value < 0)
            {
                throw new DomainException("INVALID_VALUE", "Value cannot be negative.");
            }

            return value;
        }

        private static DateTime ValidateDate(DateTime date)
        {
            if (date == default)
            {
                throw new DomainException("INVALID_DATE", "Date is invalid.");
            }

            return date;
        }
    }
}
