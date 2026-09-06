using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Warehouses.GetWarehouses;

internal sealed record GetWarehousesQuery : IQuery<IReadOnlyCollection<WarehouseResponse>>;
