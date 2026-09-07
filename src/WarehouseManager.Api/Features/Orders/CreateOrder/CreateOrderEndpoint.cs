using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Orders.CreateOrder;

public sealed class CreateOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("orders", async (
            CreateOrderRequest request,
            ICommandHandler<CreateOrderCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrderCommand(
                request.ProductCode,
                request.SourceWarehouseCode,
                request.DestinationWarehouseCode,
                request.Quantity);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Created((string?)null, new SuccessResponse("Order created.")),
                failure => failure.ToProblem());
        })
        .WithTags("Orders");
    }
}
