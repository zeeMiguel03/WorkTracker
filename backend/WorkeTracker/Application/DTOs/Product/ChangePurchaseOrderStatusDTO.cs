using Domain.Enums;

namespace Application.DTOs.PurchaseOrder;

public class ChangePurchaseOrderStatusDTO
{
    public PurchaseOrderStatus Status { get; set; }
}