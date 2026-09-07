namespace WarehouseManager.Api.Features.Orders.CreateOrder;

public sealed record CreateOrderRequest(
    string ProductCode,
    string SourceWarehouseCode,
    string DestinationWarehouseCode,
    int Quantity);
