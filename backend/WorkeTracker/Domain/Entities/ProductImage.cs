using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("product_images")]
    public class ProductImage
    {
        private const int MAX_LENGTH_IMAGE_URL = 500;

        [Key]
        [Column("id")]
        public int Id { get; private set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; private set; }

        [Required]
        [Column("image_url")]
        [MaxLength(MAX_LENGTH_IMAGE_URL)]
        public string ImageUrl { get; private set; } = string.Empty;

        [Required]
        [Column("display_order")]
        public int DisplayOrder { get; private set; }

        [Required]
        [Column("is_cover")]
        public bool IsCover { get; private set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }


        [ForeignKey(nameof(ProductId))]
        public Product Product { get; private set; } = null!;

        private ProductImage() { }

        public static ProductImage Create(int productId, string imageUrl, int displayOrder, bool isCover)
        {
            if (productId <= 0)
            {
                throw new DomainException("INVALID_PRODUCT_ID", "Product id is invalid.");
            }

            return new ProductImage
            {
                ProductId = productId,
                ImageUrl = NormalizeImageUrl(imageUrl),
                DisplayOrder = ValidateDisplayOrder(displayOrder),
                IsCover = isCover,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Update(string imageUrl, int displayOrder, bool isCover)
        {
            ImageUrl = NormalizeImageUrl(imageUrl);
            DisplayOrder = ValidateDisplayOrder(displayOrder);
            IsCover = isCover;
        }

        private static string NormalizeImageUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException("IMAGE_URL_REQUIRED", "Image URL is required.");
            }

            var normalized = value.Trim();

            if (normalized.Length > MAX_LENGTH_IMAGE_URL)
            {
                throw new DomainException("IMAGE_URL_TOO_LONG", $"Image URL cannot exceed {MAX_LENGTH_IMAGE_URL} characters.");
            }

            return normalized;
        }

        private static int ValidateDisplayOrder(int displayOrder)
        {
            if (displayOrder < 0)
            {
                throw new DomainException("INVALID_DISPLAY_ORDER", "Display order cannot be negative.");
            }

            return displayOrder;
        }
    }
}
