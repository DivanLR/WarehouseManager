using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Stock.GetStock;

public sealed class GetStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stock", async (
            string? warehouseCode,
            string? productCode,
            IQueryHandler<GetStockQuery, IReadOnlyCollection<StockResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<StockResponse>> result = await handler.Handle(new GetStockQuery(warehouseCode, productCode), cancellationToken);

            return result.Match(
                TypedResults.Ok<IReadOnlyCollection<StockResponse>>,
                failure => failure.ToProblem());
        })
        .WithTags("Stock");
    }
}
