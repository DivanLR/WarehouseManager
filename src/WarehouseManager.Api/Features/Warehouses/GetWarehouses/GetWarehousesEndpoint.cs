using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Warehouses.GetWarehouses;

public sealed class GetWarehousesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("warehouses", async (
            IQueryHandler<GetWarehousesQuery, IReadOnlyCollection<WarehouseResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<WarehouseResponse>> result = await handler.Handle(new GetWarehousesQuery(), cancellationToken);

            return result.Match(
                TypedResults.Ok<IReadOnlyCollection<WarehouseResponse>>,
                failure => failure.ToProblem());
        })
        .WithTags("Warehouses");
    }
}
