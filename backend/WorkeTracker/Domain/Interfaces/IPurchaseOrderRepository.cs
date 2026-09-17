using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IPurchaseOrderRepository
{
    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByIdAsync(
        int purchaseOrderId,
        int userId,
        bool includeProducts = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> GetPageByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        PurchaseOrderStatus? status,
        string? search,
        CancellationToken cancellationToken = default);

    void Update(PurchaseOrder purchaseOrder);

    void Remove(PurchaseOrder purchaseOrder);
}
