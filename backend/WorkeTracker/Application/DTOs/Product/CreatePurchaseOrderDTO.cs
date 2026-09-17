namespace Application.DTOs.PurchaseOrder;

public class CreatePurchaseOrderDTO
{
    public int? EntryId { get; set; }
    public int? SourceId { get; set; }

    public string? TrackingNumber { get; set; }

    public decimal ShippingCost { get; set; }
    public decimal? OtherCosts { get; set; }

    public string? Notes { get; set; }
}