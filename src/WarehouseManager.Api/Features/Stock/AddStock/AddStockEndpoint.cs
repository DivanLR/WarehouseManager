using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Stock.AddStock;

public sealed class AddStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("stock", async (
            AddStockRequest request,
            ICommandHandler<AddStockCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddStockCommand(request.WarehouseCode, request.ProductCode, request.Quantity);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Ok(new SuccessResponse("Stock added.")),
                failure => failure.ToProblem());
        })
        .WithTags("Stock");
    }
}
