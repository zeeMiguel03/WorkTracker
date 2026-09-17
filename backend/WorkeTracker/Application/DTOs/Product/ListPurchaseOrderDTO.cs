using Domain.Enums;

namespace Application.DTOs.PurchaseOrder;

public class ListPurchaseOrderDTO
{
    public int Id { get; set; }

    public int? SourceId { get; set; }
    public string? SourceName { get; set; }

    public string? TrackingNumber { get; set; }
    public PurchaseOrderStatus Status { get; set; }

    public decimal ShippingCost { get; set; }
    public decimal? OtherCosts { get; set; }

    public DateTime? OrderedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }
}