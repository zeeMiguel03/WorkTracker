using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        int productId,
        int userId,
        bool includeImages = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPageByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        ProductStatus? status,
        string? search,
        bool includeCoverImage = true,
        CancellationToken cancellationToken = default);

    void Update(Product product);

    void Remove(Product product);

    void RemoveImage(ProductImage image);
}
