using Application.DTOs.Dashboard;

namespace Application.Interfaces;

public interface IDashboardQueries
{
    Task<DashboardResponseDTO> GetAsync(
        int userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken = default);
}
