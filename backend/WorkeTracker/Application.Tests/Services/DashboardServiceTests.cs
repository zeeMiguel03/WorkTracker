using Application.DTOs.Dashboard;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Exceptions;
using NSubstitute;

namespace Application.Tests.Services;

public class DashboardServiceTests
{
    private const int UserId = 42;

    private readonly IDashboardQueries _queries = Substitute.For<IDashboardQueries>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _currentUser.GetUserId().Returns(UserId);
        _sut = new DashboardService(_queries, _currentUser);
    }

    [Fact]
    public async Task GetAsync_WhenRangeIsValid_ForwardsUserRangeAndCancellationToken()
    {
        var from = new DateOnly(2026, 1, 1);
        var toExclusive = new DateOnly(2026, 2, 1);
        using var cancellation = new CancellationTokenSource();
        var response = CreateResponse();

        _queries.GetAsync(UserId, from, toExclusive, cancellation.Token)
            .Returns(Task.FromResult(response));

        var result = await _sut.GetAsync(from, toExclusive, cancellation.Token);

        Assert.Same(response, result);

        _currentUser.Received(1).GetUserId();

        await _queries.Received(1).GetAsync(UserId, from, toExclusive, cancellation.Token);
    }

    [Fact]
    public async Task GetAsync_WhenRangeIsExactlyFiveYears_IsAccepted()
    {
        var from = new DateOnly(2020, 1, 1);
        var toExclusive = new DateOnly(2025, 1, 1);
        var response = CreateResponse();

        _queries.GetAsync(UserId, from, toExclusive, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var result = await _sut.GetAsync(from, toExclusive);

        Assert.Same(response, result);

        await _queries.Received(1).GetAsync(UserId, from, toExclusive, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenRangeStartsNearMaximumDate_DoesNotOverflowAndForwardsRequest()
    {
        var from = new DateOnly(9995, 1, 1);
        var toExclusive = new DateOnly(9999, 1, 1);
        var response = CreateResponse();

        _queries.GetAsync(UserId, from, toExclusive, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var result = await _sut.GetAsync(from, toExclusive);

        Assert.Same(response, result);

        await _queries.Received(1).GetAsync(UserId, from, toExclusive, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenFromIsDefault_ThrowsAndDoesNotCallQueries()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetAsync(default, new DateOnly(2026, 1, 1)));

        Assert.Equal("INVALID_DASHBOARD_RANGE", exception.Code);

        _currentUser.DidNotReceive().GetUserId();

        await _queries.DidNotReceive().GetAsync(
            Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenToExclusiveIsDefault_ThrowsAndDoesNotCallQueries()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetAsync(new DateOnly(2026, 1, 1), default));

        Assert.Equal("INVALID_DASHBOARD_RANGE", exception.Code);

        _currentUser.DidNotReceive().GetUserId();

        await _queries.DidNotReceive().GetAsync(
            Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2026, 5, 1, 2026, 5, 1)]
    [InlineData(2026, 5, 2, 2026, 5, 1)]
    public async Task GetAsync_WhenEndIsNotAfterStart_ThrowsAndDoesNotCallQueries(int fromYear, int fromMonth, int fromDay, int toYear, int toMonth, int toDay)
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetAsync(
            new DateOnly(fromYear, fromMonth, fromDay),
            new DateOnly(toYear, toMonth, toDay)));

        Assert.Equal("INVALID_DASHBOARD_RANGE", exception.Code);

        _currentUser.DidNotReceive().GetUserId();

        await _queries.DidNotReceive().GetAsync(
            Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenRangeExceedsFiveYears_ThrowsAndDoesNotCallQueries()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetAsync(
            new DateOnly(2020, 1, 1), new DateOnly(2025, 1, 2)));

        Assert.Equal("INVALID_DASHBOARD_RANGE", exception.Code);

        _currentUser.DidNotReceive().GetUserId();

        await _queries.DidNotReceive().GetAsync(
            Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenQueriesFail_PropagatesTheException()
    {
        var from = new DateOnly(2026, 1, 1);
        var toExclusive = new DateOnly(2026, 2, 1);
        var expectedException = new InvalidOperationException("Dashboard query failed.");

        _queries.GetAsync(UserId, from, toExclusive, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DashboardResponseDTO>(expectedException));

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAsync(from, toExclusive));

        Assert.Same(expectedException, actualException);
    }

    private static DashboardResponseDTO CreateResponse() => new(
        Revenue: 1250.50m,
        Profit: 430.25m,
        SoldCount: 3,
        Invested: 820.25m,
        StockCount: 4,
        Daily: [new DashboardDailyDTO("2026-01-01", 1250.50m, 430.25m)],
        SourceProfits: [new DashboardChannelDTO("Loja", 430.25m)],
        Sales: [new DashboardSaleDTO(1, 2, "Produto", "2026-01-01", "Loja", 1250.50m, 430.25m)],
        Stock: [new DashboardStockDTO(3, "Em stock", 100m, 50m, 50m)]);
}
