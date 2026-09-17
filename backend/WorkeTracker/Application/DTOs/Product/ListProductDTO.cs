using Domain.Enums;

namespace Application.DTOs.Product;

public class ListProductDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }

    public ProductCondition Condition { get; set; }
    public ProductStatus Status { get; set; }

    public decimal? ListingPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
}
