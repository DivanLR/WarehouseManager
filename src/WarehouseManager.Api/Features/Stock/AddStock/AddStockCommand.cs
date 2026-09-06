using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Stock.AddStock;

internal sealed record AddStockCommand(string WarehouseCode, string ProductCode, int Quantity) : ICommand;
