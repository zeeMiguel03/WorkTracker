using Domain.Enums;
using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    [Table("accounts")]
    public class Account
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_BANK_NAME = 150;
        private const int MAX_LENGTH_CARD_BRAND = 50;
        private const int MAX_LENGTH_LAST4 = 4;
        private const int MAX_LENGTH_ICON_KEY = 100;
        private const int MAX_LENGTH_COLOR = 50;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Required]
        [Column("account_type")]
        public AccountType AccountType { get; private set; }

        [Required]
        [Column("name")]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("bank_name")]
        [MaxLength(MAX_LENGTH_BANK_NAME)]
        public string? BankName { get; private set; }

        [Column("card_brand")]
        [MaxLength(MAX_LENGTH_CARD_BRAND)]
        public string? CardBrand { get; private set; }

        [Column("last4")]
        [MaxLength(MAX_LENGTH_LAST4)]
        public string? Last4 { get; private set; }

        [Column("icon_key")]
        [MaxLength(MAX_LENGTH_ICON_KEY)]
        public string? IconKey { get; private set; }

        [Column("color")]
        [MaxLength(MAX_LENGTH_COLOR)]
        public string? Color { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Column("ut_creation")]
        public int? UtCreation { get; private set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        public ICollection<Entry> Entries { get; private set; } = new List<Entry>();

        private Account() { }

        public static Account Create(
            int userId,
            AccountType accountType,
            string name,
            string? bankName,
            string? cardBrand,
            string? last4,
            string? iconKey,
            string? color,
            int? utCreation)
        {
            var account = new Account
            {
                UserId = ValidateUserId(userId),
                AccountType = ValidateAccountType(accountType),
                Name = NormalizeAndValidateName(name),
                BankName = NormalizeAndValidateBankName(bankName),
                CardBrand = NormalizeAndValidateCardBrand(cardBrand),
                Last4 = NormalizeAndValidateLast4(last4),
                IconKey = NormalizeAndValidateIconKey(iconKey),
                Color = NormalizeAndValidateColor(color),
                CreatedAt = DateTime.UtcNow,
                UtCreation = utCreation
            };

            return account;
        }

        public void Update(
            AccountType accountType,
            string name,
            string? bankName,
            string? cardBrand,
            string? last4,
            string? iconKey,
            string? color)
        {
            AccountType = ValidateAccountType(accountType);
            Name = NormalizeAndValidateName(name);
            BankName = NormalizeAndValidateBankName(bankName);
            CardBrand = NormalizeAndValidateCardBrand(cardBrand);
            Last4 = NormalizeAndValidateLast4(last4);
            IconKey = NormalizeAndValidateIconKey(iconKey);
            Color = NormalizeAndValidateColor(color);
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

        private static string? NormalizeAndValidateBankName(string? bankName)
        {
            if (string.IsNullOrWhiteSpace(bankName))
            {
                return null;
            }

            var normalizedBankName = bankName.Trim();

            if (normalizedBankName.Length > MAX_LENGTH_BANK_NAME)
            {
                throw new DomainException("BANK_NAME_TOO_LONG", "Bank name exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_BANK_NAME });
            }

            return normalizedBankName;
        }

        private static string? NormalizeAndValidateCardBrand(string? cardBrand)
        {
            if (string.IsNullOrWhiteSpace(cardBrand))
            {
                return null;
            }

            var normalizedCardBrand = cardBrand.Trim();

            if (normalizedCardBrand.Length > MAX_LENGTH_CARD_BRAND)
            {
                throw new DomainException("CARD_BRAND_TOO_LONG", "Card brand exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_CARD_BRAND });
            }

            return normalizedCardBrand;
        }

        private static string? NormalizeAndValidateLast4(string? last4)
        {
            if (string.IsNullOrWhiteSpace(last4))
            {
                return null;
            }

            var normalizedLast4 = last4.Trim();

            if (normalizedLast4.Length != MAX_LENGTH_LAST4)
            {
                throw new DomainException("INVALID_LAST4", "Last4 must contain exactly 4 digits.");
            }

            if (!Regex.IsMatch(normalizedLast4, @"^\d{4}$"))
            {
                throw new DomainException("INVALID_LAST4", "Last4 must contain exactly 4 digits.");
            }

            return normalizedLast4;
        }

        private static string? NormalizeAndValidateIconKey(string? iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
            {
                return null;
            }

            var normalizedIconKey = iconKey.Trim();

            if (normalizedIconKey.Length > MAX_LENGTH_ICON_KEY)
            {
                throw new DomainException("ICON_KEY_TOO_LONG", "Icon key exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_ICON_KEY });
            }

            return normalizedIconKey;
        }

        private static string? NormalizeAndValidateColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return null;
            }

            var normalizedColor = color.Trim();

            if (normalizedColor.Length > MAX_LENGTH_COLOR)
            {
                throw new DomainException("COLOR_TOO_LONG", "Color exceeds the maximum allowed length.", new { maxLength = MAX_LENGTH_COLOR });
            }

            return normalizedColor;
        }

        private static int ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            return userId;
        }

        private static AccountType ValidateAccountType(AccountType accountType)
        {
            if (!Enum.IsDefined(accountType))
            {
                throw new DomainException("INVALID_ACCOUNT_TYPE", "Account type is invalid.");
            }

            return accountType;
        }
    }
}
