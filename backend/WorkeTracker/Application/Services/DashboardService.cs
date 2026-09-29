using Application.DTOs.Dashboard;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Exceptions;

namespace Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardQueries _queries;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(IDashboardQueries queries, ICurrentUserService currentUser)
    {
        _queries = queries;
        _currentUser = currentUser;
    }

    public Task<DashboardResponseDTO> GetAsync(DateOnly from, DateOnly toExclusive, CancellationToken cancellationToken = default)
    {
        if (from == default || toExclusive == default ||
            toExclusive <= from ||
            (from.Year <= 9994 && toExclusive > from.AddYears(5)))
        {
            throw new DomainException("INVALID_DASHBOARD_RANGE", "Choose a valid date range of up to five years.");
        }

        return _queries.GetAsync(_currentUser.GetUserId(), from, toExclusive, cancellationToken);
    }
}
