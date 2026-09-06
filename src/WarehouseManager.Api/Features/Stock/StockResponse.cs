namespace WarehouseManager.Api.Features.Stock;

public sealed record StockResponse(
    Guid Id,
    Guid WarehouseId,
    string WarehouseCode,
    Guid ProductId,
    string ProductCode,
    int Quantity);
