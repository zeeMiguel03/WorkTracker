using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Product;

public class CreateProductDTO
{
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

    public string? Notes { get; set; }

    public List<IFormFile> Images { get; set; } = [];

    public int CoverImageIndex { get; set; }
}
