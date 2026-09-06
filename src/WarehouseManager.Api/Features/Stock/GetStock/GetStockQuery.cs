using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Stock.GetStock;

internal sealed record GetStockQuery(string? WarehouseCode, string? ProductCode) : IQuery<IReadOnlyCollection<StockResponse>>;
