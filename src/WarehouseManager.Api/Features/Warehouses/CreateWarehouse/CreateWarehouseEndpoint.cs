using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

public sealed class CreateWarehouseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("warehouses", async (
            CreateWarehouseRequest request,
            ICommandHandler<CreateWarehouseCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateWarehouseCommand(request.Code, request.Name);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Created((string?)null, new SuccessResponse("Warehouse created.")),
                failure => failure.ToProblem());
        })
        .WithTags("Warehouses");
    }
}
