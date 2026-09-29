using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductSaleRepository : IProductSaleRepository
{
    private readonly AppDbContext _context;

    public ProductSaleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(ProductSale sale, CancellationToken cancellationToken = default)
    {
        return _context.product_sales.AddAsync(sale, cancellationToken).AsTask();
    }

    public async Task RemoveByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sales = await _context.product_sales
            .Where(sale => sale.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.product_sales.RemoveRange(sales);
    }
}
