using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.products.AddAsync(product, cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, int userId, bool includeImages = false, CancellationToken cancellationToken = default)
    {
        var query = _context.products
            .AsNoTracking()
            .Where(product => product.Id == productId && product.UserId == userId);

        if (includeImages)
        {
            query = query.Include(product => product.Images.OrderBy(image => image.DisplayOrder));
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPageByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        ProductStatus? status,
        string? search,
        bool includeCoverImage = true,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.products
            .AsNoTracking()
            .Where(product => product.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(product => product.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(product =>
                product.Name.Contains(normalizedSearch) ||
                (product.Brand != null && product.Brand.Contains(normalizedSearch)) ||
                (product.Category != null && product.Category.Contains(normalizedSearch)) ||
                (product.Color != null && product.Color.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (includeCoverImage)
        {
            query = query.Include(product => product.Images.Where(image => image.IsCover));
        }

        var items = await query
            .OrderByDescending(product => product.CreatedAt)
            .ThenByDescending(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Update(Product product)
    {
        _context.products.Update(product);
    }

    public void Remove(Product product)
    {
        _context.products.Remove(product);
    }

    public void RemoveImage(ProductImage image)
    {
        _context.product_images.Remove(image);
    }
}
