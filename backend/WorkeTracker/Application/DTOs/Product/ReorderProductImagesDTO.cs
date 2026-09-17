namespace Application.DTOs.Product;

public class ReorderProductImagesDTO
{
    public int ProductId { get; set; }
    public IReadOnlyList<int> ImageIds { get; set; } = [];
}