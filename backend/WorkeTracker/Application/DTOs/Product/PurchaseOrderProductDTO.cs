using Domain.Enums;

namespace Application.DTOs.PurchaseOrder;

public class PurchaseOrderProductDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ProductCondition Condition { get; set; }
    public ProductStatus Status { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal? ListingPrice { get; set; }

    public string? CoverImageUrl { get; set; }
}