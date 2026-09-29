using Domain.Enums;
using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("purchase_orders")]
    public class PurchaseOrder
    {
        private const int MAX_LENGTH_TRACKING_NUMBER = 60;
        private const int MAX_LENGTH_NOTES = 1000;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Column("user_id")]
        [Required]
        public int UserId { get; private set; }

        [Column("source_id")]
        public int? SourceId { get; private set; }

        [Column("tracking_number")]
        [MaxLength(MAX_LENGTH_TRACKING_NUMBER)]
        public string? TrackingNumber { get; private set; } = string.Empty;

        [Column("status")]
        [Required]
        public PurchaseOrderStatus Status { get; private set; }

        [Required]
        [Column("shipping_cost", TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; private set; }

        [Column("other_costs", TypeName = "decimal(18,2)")]
        public decimal? OtherCosts { get; private set; }

        [Column("ordered_at")]
        public DateTime? OrderedAt { get; private set; }

        [Column("delivered_at")]
        public DateTime? DeliveredAt { get; private set; }

        [Column("notes")]
        [MaxLength(MAX_LENGTH_NOTES)]
        public string? Notes { get; private set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; private set; }


        [ForeignKey(nameof(UserId))]
        public User User { get; private set; } = null!;

        [ForeignKey(nameof(SourceId))]
        public Source? Source { get; private set; } = null!;

        public ICollection<Product> Products { get; private set; } = new List<Product>();

        private PurchaseOrder() { }

        public static PurchaseOrder Create(
            int userId,
            int? sourceId,
            string? trackingNumber,
            decimal shippingCost,
            decimal otherCosts,
            string? notes)
        {
            var now = DateTime.UtcNow;

            return new PurchaseOrder
            {
                UserId = ValidateUserId(userId),
                SourceId = ValidateOptionalId(sourceId, "source"),
                TrackingNumber = NormalizeTrackingNumber(trackingNumber),
                Status = PurchaseOrderStatus.Draft,
                ShippingCost = ValidateCost(shippingCost, "shipping cost"),
                OtherCosts = ValidateCost(otherCosts, "other costs"),
                OrderedAt = null,
                DeliveredAt = null,
                Notes = NormalizeNotes(notes),
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public void Update(int? sourceId, string? trackingNumber, decimal shippingCost, decimal otherCosts, string? notes)
        {
            SourceId = ValidateOptionalId(sourceId, "source");
            TrackingNumber = NormalizeTrackingNumber(trackingNumber);
            ShippingCost = ValidateCost(shippingCost, "shipping cost");
            OtherCosts = ValidateCost(otherCosts, "other costs");
            Notes = NormalizeNotes(notes);

            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(PurchaseOrderStatus newStatus)
        {
            if (Status == newStatus)
            {
                return;
            }

            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsOrdered(DateTime orderedAt)
        {
            OrderedAt = ValidateDate(orderedAt);
            Status = PurchaseOrderStatus.Ordered;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsDelivered(DateTime deliveredAt)
        {
            var normalizedDate = ValidateDate(deliveredAt);

            if (OrderedAt.HasValue && normalizedDate < OrderedAt.Value)
            {
                throw new DomainException("INVALID_DELIVERED_DATE", "Delivered date cannot be before the ordered date.");
            }

            DeliveredAt = normalizedDate;
            Status = PurchaseOrderStatus.Received;
            UpdatedAt = DateTime.UtcNow;
        }

        private static int ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new DomainException("INVALID_USER_ID", "User id is invalid.");
            }

            return userId;
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

        private static DateTime ValidateDate(DateTime date)
        {
            if (date == default)
            {
                throw new DomainException("INVALID_DATE", "Date is invalid.");
            }

            return date;
        }

        private static string? NormalizeTrackingNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();

            if (normalized.Length > MAX_LENGTH_TRACKING_NUMBER)
            {
                throw new DomainException(
                    "TRACKING_NUMBER_TOO_LONG",
                    $"Tracking number cannot exceed {MAX_LENGTH_TRACKING_NUMBER} characters.");
            }

            return normalized;
        }

        private static string? NormalizeNotes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();

            if (normalized.Length > MAX_LENGTH_NOTES)
            {
                throw new DomainException("NOTES_TOO_LONG", $"Notes cannot exceed {MAX_LENGTH_NOTES} characters.");
            }

            return normalized;
        }
    }
}
