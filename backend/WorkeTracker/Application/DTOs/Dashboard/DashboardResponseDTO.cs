namespace Application.DTOs.Dashboard;

public sealed record DashboardDailyDTO(
    string Date, 
    decimal Revenue, 
    decimal Profit);

public sealed record DashboardChannelDTO(
    string Name, 
    decimal Profit);

public sealed record DashboardSaleDTO(
    int Id, 
    int? ProductId, 
    string Name, 
    string SaleDate,
    string SourceName,
    decimal SalePrice,
    decimal Profit);

public sealed record DashboardStockDTO(
    int Id, 
    string Name, 
    decimal ListingPrice,
    decimal AcquisitionCost, 
    decimal PotentialProfit);

public sealed record DashboardResponseDTO(
    decimal Revenue,
    decimal Profit,
    int SoldCount,
    decimal Invested,
    int StockCount,
    IReadOnlyList<DashboardDailyDTO> Daily,
    IReadOnlyList<DashboardChannelDTO> SourceProfits,
    IReadOnlyList<DashboardSaleDTO> Sales,
    IReadOnlyList<DashboardStockDTO> Stock);
