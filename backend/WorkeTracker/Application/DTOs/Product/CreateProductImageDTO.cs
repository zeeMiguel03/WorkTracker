using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Product;

public class CreateProductImageDTO
{
    public int ProductId { get; set; }
    public IFormFile Image { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }
}
