using Application.DTOs.Common;
using Application.DTOs.Product;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private const int MaxProductImages = 5;
        private const int MaxConcurrentProductImageUploads = MaxProductImages;

        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IEntryRepository _entryRepository;
        private readonly ISourceRepository _sourceRepository;
        private readonly IUploadService _uploadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ICurrentUserService currentUserService,
            IPurchaseOrderRepository purchaseOrderRepository,
            IEntryRepository entryRepository,
            ISourceRepository sourceRepository,
            IUploadService uploadService, 
            IUnitOfWork unitOfWork,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _currentUserService = currentUserService;
            _purchaseOrderRepository = purchaseOrderRepository;
            _entryRepository = entryRepository;
            _sourceRepository = sourceRepository;
            _uploadService = uploadService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GetProductDTO> CreateProductAsync(CreateProductDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            if (dto.PurchaseOrderId.HasValue)
            {
                await VerifyPurchaseOrder(dto.PurchaseOrderId.Value, userId, cancellationToken);
            }

            if (dto.Images is null)
            {
                throw new DomainException("INVALID_PRODUCT_IMAGES", "The product images collection cannot be null.");
            }

            if (dto.Images.Count > MaxProductImages)
            {
                throw new DomainException("TOO_MANY_PRODUCT_IMAGES", $"A product can have a maximum of {MaxProductImages} images.");
            }

            if (dto.Images.Count > 0 && (dto.CoverImageIndex < 0 || dto.CoverImageIndex >= dto.Images.Count))
            {
                throw new DomainException("INVALID_COVER_IMAGE_INDEX", "The cover image index is invalid.");
            }

            var product = Product.Create(
                userId,
                dto.PurchaseOrderId,
                dto.Name,
                dto.Description,
                dto.Category,
                dto.Brand,
                dto.Size,
                dto.Color,
                dto.Condition,
                dto.Status,
                dto.PurchasePrice,
                dto.AllocatedShippingCost,
                dto.AllocatedOtherCosts,
                dto.ListingPrice,
                dto.MinimumPrice,
                dto.Notes);

            await _productRepository.AddAsync(product, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (dto.Images.Count == 0)
            {
                return MapToGetProductDTO(product);
            }

            var uploadedImagePaths = new List<string>(dto.Images.Count);
            using var uploadGate = new SemaphoreSlim(
                Math.Min(MaxConcurrentProductImageUploads, dto.Images.Count));

            var imageUploadTasks = dto.Images
                .Select(image => UploadProductImageAsync(image, uploadGate, cancellationToken))
                .ToArray();

            try
            {
                // Image validation and processing is CPU-bound. Run a small,
                // bounded number of uploads in parallel, then attach the
                // resulting paths to the product in their original order.
                var imagePaths = await Task.WhenAll(imageUploadTasks);
                uploadedImagePaths.AddRange(imagePaths);

                for (var index = 0; index < imagePaths.Length; index++)
                {
                    if (index == dto.CoverImageIndex)
                    {
                        RemoveCurrentCover(product);
                    }

                    product.Images.Add(ProductImage.Create(
                        product.Id,
                        imagePaths[index],
                        index,
                        index == dto.CoverImageIndex));
                }

                _productRepository.Update(product);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                var completedImagePaths = uploadedImagePaths.Count > 0
                    ? uploadedImagePaths
                    : imageUploadTasks
                        .Where(task => task.IsCompletedSuccessfully)
                        .Select(task => task.Result)
                        .ToList();

                foreach (var imagePath in completedImagePaths)
                {
                    await TryDeleteProductImageAsync(imagePath);
                }

                throw;
            }

            return MapToGetProductDTO(product);
        }

        public async Task UpdateProductAsync(UpdateProductDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(dto.Id, userId, includeImages: false, cancellationToken);

            product.Update(
                dto.Name,
                dto.Description,
                dto.Category,
                dto.Brand,
                dto.Size,
                dto.Color,
                dto.Condition,
                dto.PurchasePrice,
                dto.AllocatedShippingCost,
                dto.AllocatedOtherCosts,
                dto.ListingPrice,
                dto.MinimumPrice,
                dto.Notes);

            if (dto.Status.HasValue)
            {
                product.ChangeStatus(dto.Status.Value);
            }

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(productId, userId, includeImages: true, cancellationToken);

            var imagePaths = product.Images
                .Select(image => image.ImageUrl)
                .ToList();

            _productRepository.Remove(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var imagePath in imagePaths)
            {
                await TryDeleteProductImageAsync(imagePath);
            }
        }

        public  async Task ChangeProductStatusAsync(int productId, ProductStatus status, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(productId, userId, includeImages: false, cancellationToken);

            product.ChangeStatus(status);

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetProductDTO> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(productId, userId, includeImages: true, cancellationToken);

            return MapToGetProductDTO(product);
        }

        public async Task<PagedResultDTO<ListProductDTO>> ListProductsByUserAsync(int page, int pageSize, ProductStatus? status, string? search, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var result = await _productRepository.GetPageByUserIdAsync(
                userId,
                page,
                pageSize,
                status,
                search,
                includeCoverImage: true,
                cancellationToken);

            return new PagedResultDTO<ListProductDTO>
            {
                Items = result.Items
                    .Select(product => new ListProductDTO
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Category = product.Category,
                        Brand = product.Brand,
                        Size = product.Size,
                        Color = product.Color,
                        Condition = product.Condition,
                        Status = product.Status,
                        ListingPrice = product.ListingPrice,
                        CreatedAt = product.CreatedAt,
                        CoverImageId = product.Images
                            .FirstOrDefault(image => image.IsCover)
                            ?.Id,
                        CoverImageUrl = product.Images
                            .FirstOrDefault(image => image.IsCover)
                            ?.ImageUrl
                    })
                    .ToList(),

                Page = page,
                PageSize = pageSize,
                TotalItems = result.TotalCount
            };
        }

        public async Task<Stream?> GetProductImageAsync(int productId, int imageId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(
                productId,
                userId,
                includeImages: true,
                cancellationToken);

            var image = product.Images
                .SingleOrDefault(x => x.Id == imageId);

            if (image is null)
            {
                return null;
            }

            return await _uploadService.ReadUploadAsync(
                image.ImageUrl,
                cancellationToken);
        }

        public async Task AddProductImageAsync(CreateProductImageDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(dto.ProductId, userId, includeImages: true, cancellationToken);

            if (product.Images.Count >= MaxProductImages)
            {
                throw new DomainException("TOO_MANY_PRODUCT_IMAGES", $"A product can have a maximum of {MaxProductImages} images.");
            }

            var imagePath = await UploadAndAttachProductImageAsync(
                product,
                dto.Image,
                dto.DisplayOrder,
                dto.IsCover,
                cancellationToken);

            try
            {
                _productRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await TryDeleteProductImageAsync(imagePath);
                throw;
            }
        }

        public async Task UpdateProductImageAsync(UpdateProductImageDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(dto.ProductId, userId, includeImages: true, cancellationToken);

            var image = product.Images.SingleOrDefault(image => image.Id == dto.ImageId);

            if (image is null)
            {
                throw new DomainException("PRODUCT_IMAGE_NOT_FOUND", "Product image was not found.");
            }

            var oldImagePath = image.ImageUrl;
            string? newImagePath = null;

            try
            {
                if (dto.Image is not null)
                {
                    newImagePath = await _uploadService.UploadProductImageAsync(
                        dto.Image,
                        cancellationToken);
                }

                if (dto.IsCover)
                {
                    RemoveCurrentCover(product, image.Id);
                }

                image.Update(
                    newImagePath ?? oldImagePath,
                    dto.DisplayOrder,
                    dto.IsCover);

                _productRepository.Update(product);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(newImagePath))
                {
                    await TryDeleteProductImageAsync(oldImagePath);
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImagePath))
                {
                    await TryDeleteProductImageAsync(newImagePath);
                }

                throw;
            }
        }

        public async Task DeleteProductImageAsync(int productId, int imageId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(productId, userId, includeImages: true, cancellationToken);

            var image = product.Images
                .SingleOrDefault(image => image.Id == imageId);

            if (image is null)
            {
                throw new DomainException("PRODUCT_IMAGE_NOT_FOUND", "Product image was not found.");
            }

            var imagePath = image.ImageUrl;

            _productRepository.RemoveImage(image);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await TryDeleteProductImageAsync(imagePath);
        }

        public async Task ReorderProductImagesAsync(ReorderProductImagesDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(dto.ProductId, userId, includeImages: true, cancellationToken);

            var images = product.Images.ToList();

            if (dto.ImageIds.Count != images.Count ||
                dto.ImageIds.Distinct().Count() != dto.ImageIds.Count ||
                images.Any(image => !dto.ImageIds.Contains(image.Id)))
            {
                throw new DomainException("INVALID_IMAGE_ORDER", "The image order is invalid.");
            }

            for (var index = 0; index < dto.ImageIds.Count; index++)
            {
                var image = images.Single(image => image.Id == dto.ImageIds[index]);

                image.Update(image.ImageUrl, index, image.IsCover);
            }

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task SellProductAsync(SellProductDTO dto, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(
                dto.ProductId,
                userId,
                includeImages: false,
                cancellationToken);

            if (dto.SaleEntryId is int saleEntryId)
            {
                await ValidateSaleEntryAsync(saleEntryId, userId, cancellationToken);
            }

            if (dto.SaleSourceId is int saleSourceId)
            {
                await ValidateSaleSourceAsync(saleSourceId, userId, cancellationToken);
            }

            product.RegisterSale(
                dto.SaleEntryId,
                dto.SaleSourceId,
                dto.SalePrice,
                dto.SaleOtherCosts,
                dto.SoldAt);

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task SetCoverImageAsync(int productId, int imageId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserId();

            var product = await GetOwnedProductAsync(productId, userId, includeImages: true, cancellationToken);

            var image = product.Images
                .SingleOrDefault(image => image.Id == imageId);

            if (image is null)
            {
                throw new DomainException("PRODUCT_IMAGE_NOT_FOUND", "Product image was not found.");
            }

            RemoveCurrentCover(product, image.Id);

            image.Update(image.ImageUrl, image.DisplayOrder, isCover: true);

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Product> GetOwnedProductAsync(int productId, int userId, bool includeImages, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(
                productId,
                userId,
                includeImages,
                cancellationToken);

            if (product is null)
            {
                throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found.");
            }

            return product;
        }

        private static void RemoveCurrentCover(Product product, int? exceptImageId = null)
        {
            foreach (var image in product.Images
                .Where(image =>
                    image.IsCover &&
                    image.Id != exceptImageId))
            {
                image.Update(
                    image.ImageUrl,
                    image.DisplayOrder,
                    isCover: false);
            }
        }

        private async Task<string> UploadAndAttachProductImageAsync(Product product, IFormFile imageFile, int displayOrder, bool isCover, CancellationToken cancellationToken)
        {
            var imagePath = await _uploadService.UploadProductImageAsync(
                imageFile,
                cancellationToken);

            try
            {
                if (isCover)
                {
                    RemoveCurrentCover(product);
                }

                product.Images.Add(ProductImage.Create(
                    product.Id,
                    imagePath,
                    displayOrder,
                    isCover));

                return imagePath;
            }
            catch
            {
                await TryDeleteProductImageAsync(imagePath);

                throw;
            }
        }

        private async Task<string> UploadProductImageAsync(IFormFile imageFile, SemaphoreSlim uploadGate, CancellationToken cancellationToken)
        {
            await uploadGate.WaitAsync(cancellationToken);

            try
            {
                return await _uploadService.UploadProductImageAsync(
                    imageFile,
                    cancellationToken);
            }
            finally
            {
                uploadGate.Release();
            }
        }

        private static GetProductDTO MapToGetProductDTO(Product product)
        {
            return new GetProductDTO
            {
                Id = product.Id,
                PurchaseOrderId = product.PurchaseOrderId,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Brand = product.Brand,
                Size = product.Size,
                Color = product.Color,
                Condition = product.Condition,
                PurchasePrice = product.PurchasePrice,
                AllocatedShippingCost = product.AllocatedShippingCost,
                AllocatedOtherCosts = product.AllocatedOtherCosts,
                ListingPrice = product.ListingPrice,
                MinimumPrice = product.MinimumPrice,
                Status = product.Status,
                SaleEntryId = product.SaleEntryId,
                SaleSourceId = product.SaleSourceId,
                SalePrice = product.SalePrice,
                SaleOtherCosts = product.SaleOtherCosts,
                SoldAt = product.SoldAt,
                Notes = product.Notes,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,

                Images = product.Images
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => new GetProductImageDTO
                    {
                        Id = image.Id,
                        ProductId = image.ProductId,
                        ImageUrl = image.ImageUrl,
                        DisplayOrder = image.DisplayOrder,
                        IsCover = image.IsCover,
                        CreatedAt = image.CreatedAt
                    })
                    .ToList()
            };
        }

        private async Task TryDeleteProductImageAsync(string imagePath)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await _uploadService.DeleteImageAsync(imagePath, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to delete product image {ImagePath}.", imagePath);
            }
        }

        private async Task VerifyPurchaseOrder(int purchaseOrderId, int userId, CancellationToken cancellationToken)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, userId, false, cancellationToken);

            if (purchaseOrder is null || purchaseOrder.UserId != userId)
            {
                throw new DomainException("PURCHASE_ORDER_NOT_FOUND", "Purchase order was not found.");
            }
        }

        private async Task ValidateSaleEntryAsync(int entryId, int userId, CancellationToken cancellationToken)
        {
            var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);

            if (entry is null || entry.UserId != userId)
            {
                throw new DomainException("ENTRY_NOT_FOUND", "Entry was not found.");
            }
        }

        private async Task ValidateSaleSourceAsync(int sourceId, int userId, CancellationToken cancellationToken)
        {
            var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);

            if (source is null || source.UserId != userId)
            {
                throw new DomainException("SOURCE_NOT_FOUND", "Source was not found.");
            }
        }
    }
}
