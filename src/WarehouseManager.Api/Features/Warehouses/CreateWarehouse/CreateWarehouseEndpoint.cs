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
            ICommandHandler<CreateWarehouseCommand, WarehouseResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateWarehouseCommand(request.Code, request.Name);

            Result<WarehouseResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                warehouse => TypedResults.Created((string?)null, warehouse),
                failure => failure.ToProblem());
        })
        .WithTags("Warehouses");
    }
}
