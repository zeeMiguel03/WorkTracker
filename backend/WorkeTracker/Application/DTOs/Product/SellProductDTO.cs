namespace Application.DTOs.Product;

public class SellProductDTO
{
    public int ProductId { get; set; }
    public int? SaleSourceId { get; set; }

    public decimal SalePrice { get; set; }
    public decimal? SaleOtherCosts { get; set; }
    public DateTime SoldAt { get; set; }
}
