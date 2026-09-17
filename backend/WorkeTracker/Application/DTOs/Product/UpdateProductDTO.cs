using Domain.Enums;

namespace Application.DTOs.Product;

public class UpdateProductDTO
{
    public int Id { get; set; }

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

    public string? Notes { get; set; }
}