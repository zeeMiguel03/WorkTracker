namespace Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft = 1,
    Ordered = 2,
    InTransit = 3,
    PartiallyReceived = 4,
    Received = 5,
    Cancelled = 6
}