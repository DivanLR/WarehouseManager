namespace WarehouseManager.Api.Features.Stock.AddStock;

public sealed record AddStockRequest(string WarehouseCode, string ProductCode, int Quantity);
