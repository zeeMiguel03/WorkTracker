using Domain.Enums;

namespace Application.DTOs.Product;

public class GetProductDTO
{
    public int Id { get; set; }
    public int? PurchaseOrderId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }

    public ProductCondition Condition { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal AllocatedShippingCost { get; set; }
    public decimal AllocatedOtherCosts { get; set; }

    public decimal? ListingPrice { get; set; }
    public decimal? MinimumPrice { get; set; }

    public ProductStatus Status { get; set; }

    public int? SaleEntryId { get; set; }
    public int? SaleSourceId { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? SaleOtherCosts { get; set; }
    public DateTime? SoldAt { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<GetProductImageDTO> Images { get; set; } = [];
}