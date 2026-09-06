using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.GetProducts;

public sealed class GetProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products", async (
            IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<ProductResponse>> result = await handler.Handle(new GetProductsQuery(), cancellationToken);

            return result.Match(
                TypedResults.Ok<IReadOnlyCollection<ProductResponse>>,
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
