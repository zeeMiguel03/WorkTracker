namespace Application.DTOs.PurchaseOrder;

public class UpdatePurchaseOrderDTO
{
    public int Id { get; set; }

    public int? SourceId { get; set; }
    public string? TrackingNumber { get; set; }

    public decimal ShippingCost { get; set; }
    public decimal? OtherCosts { get; set; }

    public string? Notes { get; set; }
}