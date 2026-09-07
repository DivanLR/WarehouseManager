using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Orders.CreateOrder;

internal sealed record CreateOrderCommand(
    string ProductCode,
    string SourceWarehouseCode,
    string DestinationWarehouseCode,
    int Quantity) : ICommand;
