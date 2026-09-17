using Domain.Enums;

namespace Application.DTOs.PurchaseOrder;

public class GetPurchaseOrderDTO
{
    public int Id { get; set; }

    public int? EntryId { get; set; }
    public int? SourceId { get; set; }

    public string? SourceName { get; set; }
    public string? TrackingNumber { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public decimal ShippingCost { get; set; }
    public decimal? OtherCosts { get; set; }

    public DateTime? OrderedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<PurchaseOrderProductDTO> Products { get; set; } = [];
}