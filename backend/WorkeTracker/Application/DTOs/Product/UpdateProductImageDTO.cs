using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Product;

public class UpdateProductImageDTO
{
    public int ProductId { get; set; }
    public int ImageId { get; set; }
    public IFormFile? Image { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }
}
