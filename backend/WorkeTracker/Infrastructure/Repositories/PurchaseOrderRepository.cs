using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private const int MaxPageSize = 100;
    private readonly AppDbContext _context;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        await _context.purchase_orders.AddAsync(purchaseOrder, cancellationToken);
    }

    public Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId, int userId, bool includeProducts = false, CancellationToken cancellationToken = default)
    {
        var query = _context.purchase_orders
            .AsNoTracking()
            .Include(order => order.Source)
            .Where(order => order.Id == purchaseOrderId && order.UserId == userId);

        if (includeProducts)
        {
            query = query
                .Include(order => order.Products)
                .ThenInclude(product => product.Images.Where(image => image.IsCover));
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> GetPageByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        PurchaseOrderStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.purchase_orders
            .AsNoTracking()
            .Where(order => order.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(order =>
                (order.TrackingNumber != null && order.TrackingNumber.Contains(normalizedSearch)) ||
                (order.Notes != null && order.Notes.Contains(normalizedSearch)) ||
                (order.Source != null && order.Source.Name.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(order => order.Source)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Update(PurchaseOrder purchaseOrder)
    {
        _context.purchase_orders.Update(purchaseOrder);
    }

    public void Remove(PurchaseOrder purchaseOrder)
    {
        _context.purchase_orders.Remove(purchaseOrder);
    }
}
