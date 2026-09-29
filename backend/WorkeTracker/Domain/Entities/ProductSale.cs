using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

[Table("product_sales")]
public sealed class ProductSale
{
    [Key]
    [Column("id")]
    public int Id { get; private set; }

    [Column("user_id")]
    public int UserId { get; private set; }

    [Column("product_id")]
    public int? ProductId { get; private set; }

    [Column("product_name")]
    [MaxLength(150)]
    public string ProductName { get; private set; } = string.Empty;

    [Column("sale_source_id")]
    public int? SaleSourceId { get; private set; }

    [Column("sale_source_name")]
    [MaxLength(150)]
    public string? SaleSourceName { get; private set; }

    [Column("purchase_price", TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; private set; }

    [Column("allocated_shipping_cost", TypeName = "decimal(18,2)")]
    public decimal AllocatedShippingCost { get; private set; }

    [Column("allocated_other_costs", TypeName = "decimal(18,2)")]
    public decimal AllocatedOtherCosts { get; private set; }

    [Column("sale_price", TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; private set; }

    [Column("sale_other_costs", TypeName = "decimal(18,2)")]
    public decimal SaleOtherCosts { get; private set; }

    [Column("sold_at", TypeName = "datetime2")]
    public DateTime SoldAt { get; private set; }

    [Column("sale_date", TypeName = "date")]
    public DateOnly SaleDate { get; private set; }

    private ProductSale() { }

    public static ProductSale FromProduct(Product product, DateOnly saleDate, string? saleSourceName)
    {
        if (product.Status != ProductStatus.Sold ||
            product.SalePrice is null ||
            product.SoldAt is null)
        {
            throw new DomainException("SALE_NOT_COMPLETE", "The product does not contain a completed sale.");
        }

        return new ProductSale
        {
            UserId = product.UserId,
            ProductId = product.Id,
            ProductName = product.Name,
            SaleSourceId = product.SaleSourceId,
            SaleSourceName = saleSourceName,
            PurchasePrice = product.PurchasePrice,
            AllocatedShippingCost = product.AllocatedShippingCost,
            AllocatedOtherCosts = product.AllocatedOtherCosts,
            SalePrice = product.SalePrice.Value,
            SaleOtherCosts = product.SaleOtherCosts ?? 0m,
            SoldAt = product.SoldAt.Value,
            SaleDate = saleDate
        };
    }
}