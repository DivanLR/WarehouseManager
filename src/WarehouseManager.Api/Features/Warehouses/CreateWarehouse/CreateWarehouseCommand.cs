using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

internal sealed record CreateWarehouseCommand(string Code, string Name) : ICommand<WarehouseResponse>;
