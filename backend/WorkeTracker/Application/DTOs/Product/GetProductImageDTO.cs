namespace Application.DTOs.Product;

public class GetProductImageDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }

    public DateTime CreatedAt { get; set; }
}