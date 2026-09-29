using Application.DTOs.Dashboard;

namespace Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDTO> GetAsync(DateOnly from, DateOnly toExclusive, CancellationToken cancellationToken = default);
}
