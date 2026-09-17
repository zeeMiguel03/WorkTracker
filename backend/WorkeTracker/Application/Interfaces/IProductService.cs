using Application.DTOs.Common;
using Application.DTOs.Product;
using Domain.Enums;

namespace Application.Interfaces;

public interface IProductService
{
    Task<GetProductDTO> CreateProductAsync(CreateProductDTO dto, CancellationToken cancellationToken = default);

    Task UpdateProductAsync(UpdateProductDTO dto, CancellationToken cancellationToken = default);

    Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<GetProductDTO> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<Stream?> GetProductImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDTO<ListProductDTO>> ListProductsByUserAsync(int page, int pageSize, ProductStatus? status, string? search, CancellationToken cancellationToken = default);

    Task ChangeProductStatusAsync(int productId, ProductStatus status, CancellationToken cancellationToken = default);

    Task SellProductAsync(SellProductDTO dto, CancellationToken cancellationToken = default);

    Task AddProductImageAsync(CreateProductImageDTO dto, CancellationToken cancellationToken = default);

    Task UpdateProductImageAsync(UpdateProductImageDTO dto, CancellationToken cancellationToken = default);

    Task DeleteProductImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);

    Task SetCoverImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);

    Task ReorderProductImagesAsync(ReorderProductImagesDTO dto, CancellationToken cancellationToken = default);
}
