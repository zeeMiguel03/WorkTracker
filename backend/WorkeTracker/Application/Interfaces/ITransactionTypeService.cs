using Application.DTOs.Transaction;

namespace Application.Interfaces
{
    public interface ITransactionTypeService
    {
        Task CreateTransactionTypeAsync(CreateTransactionTypeDTO dto, CancellationToken cancellationToken = default);

        Task UpdateTransactionTypeAsync(UpdateTransactionTypeDTO dto, CancellationToken cancellationToken = default);

        Task<GetTransactionTypeDTO> ListTransactionTypeByIdAsync(int idTransactionType, CancellationToken cancellationToken = default);

        Task<List<GetTransactionTypeDTO>> ListTransactionTypesByUserAsync(CancellationToken cancellationToken = default);
    }
}
