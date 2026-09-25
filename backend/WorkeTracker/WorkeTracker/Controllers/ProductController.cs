using Application.DTOs.Common;
using Application.DTOs.Product;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<ListProductDTO>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ProductStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListProductsByUserAsync(
            page,
            pageSize,
            status,
            search,
            ct);

        foreach (var product in result.Items)
        {
            AddCoverImageUrl(product);
        }

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetProductDTO>> GetById(int id, CancellationToken ct)
    {
        var product = await _service.GetProductByIdAsync(id, ct);
        AddImageUrls(product);

        return Ok(product);
    }

    [HttpGet("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> GetImage(int productId, int imageId, CancellationToken ct)
    {
        var stream = await _service.GetProductImageAsync(productId, imageId, ct);

        return stream is null
            ? NotFound()
            : File(stream, "image/webp");
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("uploads")]
    public async Task<IActionResult> Create([FromForm] CreateProductDTO dto, CancellationToken ct)
    {
        var product = await _service.CreateProductAsync(dto, ct);

        AddImageUrls(product);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDTO dto, CancellationToken ct)
    {
        dto.Id = id;

        await _service.UpdateProductAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteProductAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/status/{status}")]
    public async Task<IActionResult> ChangeStatus(int id, ProductStatus status, CancellationToken ct)
    {
        await _service.ChangeProductStatusAsync(id, status, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/sell")]
    public async Task<IActionResult> Sell(int id, [FromBody] SellProductDTO dto, CancellationToken ct)
    {
        dto.ProductId = id;

        await _service.SellProductAsync(dto, ct);

        return NoContent();
    }

    [HttpPost("{productId:int}/images")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("uploads")]
    public async Task<IActionResult> AddImage(int productId, [FromForm] CreateProductImageDTO dto, CancellationToken ct)
    {
        dto.ProductId = productId;

        await _service.AddProductImageAsync(dto, ct);

        return NoContent();
    }

    [HttpPut("{productId:int}/images/{imageId:int}")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("uploads")]
    public async Task<IActionResult> UpdateImage(int productId, int imageId, [FromForm] UpdateProductImageDTO dto, CancellationToken ct)
    {
        dto.ProductId = productId;

        dto.ImageId = imageId;

        await _service.UpdateProductImageAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int productId, int imageId, CancellationToken ct)
    {
        await _service.DeleteProductImageAsync(productId, imageId, ct);

        return NoContent();
    }

    [HttpPatch("{productId:int}/images/{imageId:int}/cover")]
    public async Task<IActionResult> SetCoverImage(int productId, int imageId, CancellationToken ct)
    {
        await _service.SetCoverImageAsync(productId, imageId, ct);

        return NoContent();
    }

    [HttpPatch("{productId:int}/images/order")]
    public async Task<IActionResult> ReorderImages(int productId, [FromBody] ReorderProductImagesDTO dto,  CancellationToken ct)
    {
        dto.ProductId = productId;

        await _service.ReorderProductImagesAsync(dto, ct);

        return NoContent();
    }

    private void AddImageUrls(GetProductDTO product)
    {
        foreach (var image in product.Images)
        {
            image.ImageUrl = Url.ActionLink(
                nameof(GetImage),
                values: new { productId = product.Id, imageId = image.Id })
                ?? image.ImageUrl;
        }
    }

    private void AddCoverImageUrl(ListProductDTO product)
    {
        if (product.CoverImageId.HasValue)
        {
            product.CoverImageUrl = Url.ActionLink(
                nameof(GetImage),
                values: new
                {
                    productId = product.Id,
                    imageId = product.CoverImageId.Value
                });
        }
    }
}
