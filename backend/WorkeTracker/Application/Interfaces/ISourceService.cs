using Application.DTOs.Source;

namespace Application.Interfaces
{
    public interface ISourceService
    {
        Task<GetSourceDTO> CreateSourceAsync(CreateSourceDTO dto, CancellationToken cancellationToken = default);

        Task<Stream?> GetSourceImageAsync(int idSource, CancellationToken cancellationToken = default);

        Task UpdateSourceAsync(UpdateSourceDTO dto, CancellationToken cancellationToken = default);

        Task DeleteSourceAsync(int idSource, CancellationToken cancellationToken = default);

        Task<GetSourceDTO> ListSourceByIdAsync(int idSource, CancellationToken cancellationToken = default);

        Task<List<GetSourceDTO>> ListSourcesByUserAsync(CancellationToken cancellationToken = default);
    }
}
