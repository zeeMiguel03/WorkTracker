using Application.DTOs.Common;
using Application.DTOs.PurchaseOrder;
using Domain.Enums;

namespace Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<GetPurchaseOrderDTO> CreatePurchaseOrderAsync(CreatePurchaseOrderDTO dto, CancellationToken cancellationToken = default);

    Task UpdatePurchaseOrderAsync(UpdatePurchaseOrderDTO dto, CancellationToken cancellationToken = default);

    Task DeletePurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    Task<GetPurchaseOrderDTO> GetPurchaseOrderByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    Task<PagedResultDTO<ListPurchaseOrderDTO>> ListPurchaseOrdersByUserAsync(int page, int pageSize, PurchaseOrderStatus? status, string? search, CancellationToken cancellationToken = default);

    Task ChangePurchaseOrderStatusAsync(int purchaseOrderId, PurchaseOrderStatus status, CancellationToken cancellationToken = default);

    Task MarkPurchaseOrderAsOrderedAsync(int purchaseOrderId, DateTime orderedAt, CancellationToken cancellationToken = default);

    Task MarkPurchaseOrderAsDeliveredAsync(int purchaseOrderId, DateTime deliveredAt, CancellationToken cancellationToken = default);
}
