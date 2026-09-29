using Application.DTOs.Dashboard;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

public sealed class DashboardQueries : IDashboardQueries
{
    private readonly AppDbContext _context;

    public DashboardQueries(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponseDTO> GetAsync(int userId, DateOnly from, DateOnly toExclusive, CancellationToken cancellationToken = default)
    {
        var sales = _context.product_sales
            .AsNoTracking()
            .Where(s => s.UserId == userId &&
                        s.SaleDate >= from && s.SaleDate < toExclusive);

        var dailyRows = await sales
            .GroupBy(s => s.SaleDate)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(s => s.SalePrice),
                Profit = g.Sum(s => s.SalePrice - s.SaleOtherCosts -
                    s.PurchasePrice - s.AllocatedShippingCost - s.AllocatedOtherCosts),
                SoldCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var channelRows = await sales
            .GroupBy(s => s.SaleSourceId)
            .Select(g => new
            {
                Name = g.OrderByDescending(s => s.SoldAt)
                    .ThenByDescending(s => s.Id)
                    .Select(s => s.SaleSourceName)
                    .FirstOrDefault(),
                Profit = g.Sum(s => s.SalePrice - s.SaleOtherCosts -
                    s.PurchasePrice - s.AllocatedShippingCost - s.AllocatedOtherCosts)
            })
            .OrderByDescending(x => x.Profit)
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentRows = await sales
            .OrderByDescending(s => s.SoldAt)
            .ThenByDescending(s => s.Id)
            .Take(6)
            .Select(s => new
            {
                s.Id,
                s.ProductId,
                Name = s.ProductName,
                s.SaleDate,
                SourceName = s.SaleSourceName,
                s.SalePrice,
                Profit = s.SalePrice - s.SaleOtherCosts -
                    s.PurchasePrice - s.AllocatedShippingCost - s.AllocatedOtherCosts
            })
            .ToListAsync(cancellationToken);

        var stockProducts = _context.products
            .AsNoTracking()
            .Where(p => p.UserId == userId &&
                (p.Status == ProductStatus.Purchased ||
                 p.Status == ProductStatus.Active));

        var stockTotals = await stockProducts
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Invested = g.Sum(p => p.PurchasePrice + p.AllocatedShippingCost + p.AllocatedOtherCosts)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var stock = await stockProducts
            .Where(p => p.ListingPrice != null)
            .OrderByDescending(p => p.ListingPrice!.Value - p.PurchasePrice -
                p.AllocatedShippingCost - p.AllocatedOtherCosts)
            .ThenBy(p => p.Id)
            .Take(5)
            .Select(p => new DashboardStockDTO(
                p.Id,
                p.Name,
                p.ListingPrice!.Value,
                p.PurchasePrice + p.AllocatedShippingCost + p.AllocatedOtherCosts,
                p.ListingPrice!.Value - p.PurchasePrice -
                    p.AllocatedShippingCost - p.AllocatedOtherCosts))
            .ToListAsync(cancellationToken);

        return new DashboardResponseDTO(
            dailyRows.Sum(x => x.Revenue),
            dailyRows.Sum(x => x.Profit),
            dailyRows.Sum(x => x.SoldCount),
            stockTotals?.Invested ?? 0m,
            stockTotals?.Count ?? 0,
            dailyRows.Select(x => new DashboardDailyDTO(
                x.Date.ToString("yyyy-MM-dd"), x.Revenue, x.Profit)).ToList(),
            channelRows.Select(x => new DashboardChannelDTO(
                x.Name ?? "Canal não definido", x.Profit)).ToList(),
            recentRows.Select(x => new DashboardSaleDTO(
                x.Id, x.ProductId, x.Name, x.SaleDate.ToString("yyyy-MM-dd"),
                x.SourceName ?? "Canal não definido", x.SalePrice, x.Profit)).ToList(),
            stock);
    }
}
