# Query slice

The `GetProducts` use case as it compiled and passed its tests in this project. Three files in `src/WarehouseManager.Api/Features/Products/GetProducts/`, reusing the entity level `Features/Products/ProductResponse.cs` shown in [command-slice.md](command-slice.md). Queries have no request record (parameters arrive on the route or query string) and no validator (`ValidationDecorator` wraps commands only).

## GetProductsQuery.cs

```csharp
using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.GetProducts;

internal sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductResponse>>;
```

## GetProductsQueryHandler.cs

```csharp
using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.GetProducts;

internal sealed class GetProductsQueryHandler(NpgsqlDataSource dataSource)
    : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductResponse>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var products = await connection.QueryAsync<ProductResponse>(new CommandDefinition(
            """
            SELECT id, code, description
            FROM products
            ORDER BY code
            """,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ProductResponse>>(products.AsList());
    }
}
```

`QueryAsync<T>` returns `IEnumerable<T>`; Dapper's `AsList()` returns the `List<T>` it already materialised (no copy), and the explicit type argument on `Result.Success<...>` is what converts it to the handler's `IReadOnlyCollection<T>` contract. The user prefers `var` on the awaited line over a collection expression with an explicit type.

Dapper fills the record's constructor by column name, case insensitively and with underscores ignored (`DefaultTypeMap.MatchNamesWithUnderscores` is on), so `warehouse_id` maps to `WarehouseId` with no alias.

## GetProductsEndpoint.cs

```csharp
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
```

## Single item variant: GetProductById

Same three files in `Features/Products/GetProductById/`. The differences:

```csharp
internal sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
```

Handler body:

```csharp
ProductResponse? product = await connection.QuerySingleOrDefaultAsync<ProductResponse>(new CommandDefinition(
    """
    SELECT id, code, description
    FROM products
    WHERE id = @Id
    """,
    query,
    cancellationToken: cancellationToken));

return product is null
    ? Result.Failure<ProductResponse>(Error.NotFound("Products.NotFound", $"No product with id '{query.Id}'."))
    : Result.Success(product);
```

Endpoint: `app.MapGet("products/{id:guid}", async (Guid id, IQueryHandler<GetProductByIdQuery, ProductResponse> handler, CancellationToken cancellationToken) => ...)` with `new GetProductByIdQuery(id)` and `result.Match(TypedResults.Ok, failure => failure.ToProblem())`. `ProductResponse` is a concrete record, so the bare `TypedResults.Ok` group is unambiguous here. Lookup by natural code is the same shape with `string code`, `WHERE code = @Code` and route `products/by-code/{code}`.
