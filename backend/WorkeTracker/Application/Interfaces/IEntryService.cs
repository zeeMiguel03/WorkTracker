using Application.DTOs.Entry;

namespace Application.Interfaces
{
    public interface IEntryService
    {
        Task CreateEntryAsync(CreateEntryDTO dto, CancellationToken cancellationToken = default);

        Task UpdateEntryAsync(UpdateEntryDTO dto, CancellationToken cancellationToken = default);

        Task DeleteEntryAsync(int idEntry, CancellationToken cancellationToken = default);

        Task<GetEntryDTO> ListEntryByIdAsync(int idEntry, CancellationToken cancellationToken = default);

        Task<List<GetEntryDTO>> ListEntriesByUserAsync(CancellationToken cancellationToken = default);
    }
}
