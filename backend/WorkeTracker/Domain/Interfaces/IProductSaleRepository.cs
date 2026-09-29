using Domain.Entities;

namespace Domain.Interfaces;

public interface IProductSaleRepository
{
    Task AddAsync(ProductSale sale, CancellationToken cancellationToken = default);

    Task RemoveByUserAsync(int userId, CancellationToken cancellationToken = default);
}
