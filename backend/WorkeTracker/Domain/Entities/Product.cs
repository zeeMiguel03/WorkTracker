using Domain.Enums;
using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("products")]
    public class Product
    {
        private const int MAX_LENGTH_NAME = 150;
        private const int MAX_LENGTH_DESCRIPTION = 2000;
        private const int MAX_LENGTH_CATEGORY = 100;
        private const int MAX_LENGTH_BRAND = 100;
        private const int MAX_LENGTH_SIZE = 50;
        private const int MAX_LENGTH_COLOR = 50;
        private const int MAX_LENGTH_NOTES = 1000;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; private set; }

        [Column("purchase_order_id")]
        public int? PurchaseOrderId { get; private set; }

        [Required]
        [Column("name")]
        [MaxLength(MAX_LENGTH_NAME)]
        public string Name { get; private set; } = string.Empty;

        [Column("description")]
        [MaxLength(MAX_LENGTH_DESCRIPTION)]
        public string? Description { get; private set; }

        [Column("category")]
        [MaxLength(MAX_LENGTH_CATEGORY)]
        public string? Category { get; private set; }

        [Column("brand")]
        [MaxLength(MAX_LENGTH_BRAND)]
        public string? Brand { get; private set; }

        [Column("size")]
        [MaxLength(MAX_LENGTH_SIZE)]
        public string? Size { get; private set; }

        [Column("color")]
        [MaxLength(MAX_LENGTH_COLOR)]
        public string? Color { get; private set; }

        [Required]
        [Column("condition")]
        public ProductCondition Condition { get; private set; }

        [Required]
        [Column("purchase_price", TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; private set; }

        [Required]
        [Column("allocated_shipping_cost", TypeName = "decimal(18,2)")]
        public decimal AllocatedShippingCost { get; private set; }

        [Required]
        [Column("allocated_other_costs", TypeName = "decimal(18,2)")]
        public decimal AllocatedOtherCosts { get; private set; }

        [Column("listing_price", TypeName = "decimal(18,2)")]
        public decimal? ListingPrice { get; private set; }

        [Column("minimum_price", TypeName = "decimal(18,2)")]
        public decimal? MinimumPrice { get; private set; }

        [Required]
        [Column("status")]
        public ProductStatus Status { get; private set; }

        [Column("sale_source_id")]
        public int? SaleSourceId { get; private set; }

        [Column("sale_price", TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; private set; }

        [Column("sale_other_costs", TypeName = "decimal(18,2)")]
        public decimal? SaleOtherCosts { get; private set; }

        [Column("sold_at")]
        public DateTime? SoldAt { get; private set; }

        [Column("notes")]
        [MaxLength(MAX_LENGTH_NOTES)]
        public string? Notes { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; private set; }

        [Timestamp]
        [Column("row_version")]
        public byte[] RowVersion { get; private set; } = [];


        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        [ForeignKey(nameof(PurchaseOrderId))]
        public PurchaseOrder? PurchaseOrder { get; private set; }

        [ForeignKey(nameof(SaleSourceId))]
        public Source? SaleSource { get; private set; }

        public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

        private Product() { }

        public static Product Create(
            int userId,
            int? purchaseOrderId,
            string name,
            string? description,
            string? category,
            string? brand,
            string? size,
            string? color,
            ProductCondition condition,
            ProductStatus status,
            decimal purchasePrice,
            decimal allocatedShippingCost,
            decimal allocatedOtherCosts,
            decimal? listingPrice,
            decimal? minimumPrice,
            string? notes)
        {
            ValidatePriceRange(listingPrice, minimumPrice);
            if (status == ProductStatus.Sold)
            {
                throw new DomainException("SALE_REQUIRED", "Register the sale to mark a product as sold.");
            }
            var now = DateTime.UtcNow;

            return new Product
            {
                UserId = ValidateRequiredId(userId, "user"),
                PurchaseOrderId = ValidateOptionalId(purchaseOrderId, "purchase order"),
                Name = NormalizeRequired(name, MAX_LENGTH_NAME, "name"),
                Description = NormalizeOptional(description, MAX_LENGTH_DESCRIPTION, "description"),
                Category = NormalizeOptional(category, MAX_LENGTH_CATEGORY, "category"),
                Brand = NormalizeOptional(brand, MAX_LENGTH_BRAND, "brand"),
                Size = NormalizeOptional(size, MAX_LENGTH_SIZE, "size"),
                Color = NormalizeOptional(color, MAX_LENGTH_COLOR, "color"),
                Condition = ValidateCondition(condition),
                Status = ValidateStatus(status),
                PurchasePrice = ValidateCost(purchasePrice, "purchase price"),
                AllocatedShippingCost = ValidateCost(allocatedShippingCost, "allocated shipping cost"),
                AllocatedOtherCosts = ValidateCost(allocatedOtherCosts, "allocated other costs"),
                ListingPrice = ValidateOptionalCost(listingPrice, "listing price"),
                MinimumPrice = ValidateOptionalCost(minimumPrice, "minimum price"),
                Notes = NormalizeOptional(notes, MAX_LENGTH_NOTES, "notes"),
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public void Update(
            string name,
            string? description,
            string? category,
            string? brand,
            string? size,
            string? color,
            ProductCondition condition,
            decimal purchasePrice,
            decimal allocatedShippingCost,
            decimal allocatedOtherCosts,
            decimal? listingPrice,
            decimal? minimumPrice,
            string? notes)
        {
            if (Status == ProductStatus.Sold && (PurchasePrice != purchasePrice ||
                 AllocatedShippingCost != allocatedShippingCost ||
                 AllocatedOtherCosts != allocatedOtherCosts))
            {
                throw new DomainException("SOLD_PRODUCT_COSTS", "The acquisition costs of a sold product cannot be changed.");
            }

            ValidatePriceRange(listingPrice, minimumPrice);
            Name = NormalizeRequired(name, MAX_LENGTH_NAME, "name");
            Description = NormalizeOptional(description, MAX_LENGTH_DESCRIPTION, "description");
            Category = NormalizeOptional(category, MAX_LENGTH_CATEGORY, "category");
            Brand = NormalizeOptional(brand, MAX_LENGTH_BRAND, "brand");
            Size = NormalizeOptional(size, MAX_LENGTH_SIZE, "size");
            Color = NormalizeOptional(color, MAX_LENGTH_COLOR, "color");
            Condition = ValidateCondition(condition);
            PurchasePrice = ValidateCost(purchasePrice, "purchase price");
            AllocatedShippingCost = ValidateCost(allocatedShippingCost, "allocated shipping cost");
            AllocatedOtherCosts = ValidateCost(allocatedOtherCosts, "allocated other costs");
            ListingPrice = ValidateOptionalCost(listingPrice, "listing price");
            MinimumPrice = ValidateOptionalCost(minimumPrice, "minimum price");
            Notes = NormalizeOptional(notes, MAX_LENGTH_NOTES, "notes");
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(ProductStatus status)
        {
            status = ValidateStatus(status);

            if (status == ProductStatus.Sold && Status != ProductStatus.Sold)
            {
                throw new DomainException("SALE_REQUIRED", "Register the sale to mark a product as sold.");
            }

            if (Status == ProductStatus.Sold && status != ProductStatus.Sold)
            {
                throw new DomainException("SOLD_PRODUCT_STATUS", "The status of a sold product cannot be changed.");
            }

            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RegisterSale(int? sourceId, decimal salePrice, decimal? otherCosts, DateTime soldAt)
        {
            if (Status == ProductStatus.Sold)
            {
                throw new DomainException("PRODUCT_ALREADY_SOLD", "Product is already sold.");
            }

            SaleSourceId = ValidateOptionalId(sourceId, "sale source");
            SalePrice = ValidateCost(salePrice, "sale price");
            SaleOtherCosts = ValidateOptionalCost(otherCosts, "sale other costs");
            SoldAt = ValidateDate(soldAt, "sold date");
            Status = ProductStatus.Sold;
            UpdatedAt = DateTime.UtcNow;
        }

        public decimal GetAcquisitionCost() => PurchasePrice + AllocatedShippingCost + AllocatedOtherCosts;

        private static int ValidateRequiredId(int id, string field)
        {
            if (id <= 0)
            {
                throw new DomainException("INVALID_ID", $"{field} id is invalid.");
            }

            return id;
        }

        private static int? ValidateOptionalId(int? id, string field)
        {
            if (id.HasValue && id.Value <= 0)
            {
                throw new DomainException("INVALID_ID", $"{field} id is invalid.");
            }

            return id;
        }

        private static decimal ValidateCost(decimal value, string field)
        {
            if (value < 0)
            {
                throw new DomainException("INVALID_COST", $"{field} cannot be negative.");
            }

            return decimal.Round(value, 2, MidpointRounding.ToEven);
        }

        private static decimal? ValidateOptionalCost(decimal? value, string field)
        {
            return value.HasValue ? ValidateCost(value.Value, field) : null;
        }

        private static ProductCondition ValidateCondition(ProductCondition condition)
        {
            if (!Enum.IsDefined(condition))
            {
                throw new DomainException("INVALID_PRODUCT_CONDITION", "Product condition is invalid.");
            }

            return condition;
        }

        private static ProductStatus ValidateStatus(ProductStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                throw new DomainException("INVALID_PRODUCT_STATUS", "Product status is invalid.");
            }

            return status;
        }

        private static DateTime ValidateDate(DateTime value, string field)
        {
            if (value == default)
            {
                throw new DomainException("INVALID_DATE", $"{field} is invalid.");
            }

            return value;
        }

        private static string NormalizeRequired(string value, int maxLength, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException("FIELD_REQUIRED", $"{field} is required.");
            }

            return NormalizeOptional(value, maxLength, field)!;
        }

        private static string? NormalizeOptional(string? value, int maxLength, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();

            if (normalized.Length > maxLength)
            {
                throw new DomainException("FIELD_TOO_LONG", $"{field} cannot exceed {maxLength} characters.");
            }

            return normalized;
        }

        private static void ValidatePriceRange(decimal? listingPrice, decimal? minimumPrice)
        {
            ValidateOptionalCost(listingPrice, "listing price");
            ValidateOptionalCost(minimumPrice, "minimum price");

            if (listingPrice.HasValue && minimumPrice.HasValue && minimumPrice.Value > listingPrice.Value)
            {
                throw new DomainException("INVALID_MINIMUM_PRICE", "Minimum price cannot exceed listing price.");
            }
        }
    }
}
