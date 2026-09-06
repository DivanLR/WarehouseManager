# Query slice

The `GetProducts` use case as it compiled in this project. Four files in `src/WarehouseManager.Api/Features/Products/GetProducts/`. Queries have no request record (parameters arrive on the route or query string) and no validator (`ValidationDecorator` wraps commands only).

## GetProductsQuery.cs

```csharp
using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.GetProducts;

internal sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductResponse>>;
```

## ProductResponse.cs

```csharp
namespace WarehouseManager.Api.Features.Products.GetProducts;

public sealed record ProductResponse(string Code, string Description);
```

Dapper materialises this by matching column names to constructor parameters case insensitively, so `SELECT code, description` fills `(Code, Description)` with no aliases.

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

        IReadOnlyCollection<ProductResponse> products = [.. await connection.QueryAsync<ProductResponse>(new CommandDefinition(
            """
            SELECT code, description
            FROM products
            ORDER BY code
            """,
            cancellationToken: cancellationToken))];

        return Result.Success(products);
    }
}
```

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

## Single item variant: GetProductByCode

Same four files in `Features/Products/GetProductByCode/`. The differences:

```csharp
internal sealed record GetProductByCodeQuery(string Code) : IQuery<ProductResponse>;
```

Handler body:

```csharp
ProductResponse? product = await connection.QuerySingleOrDefaultAsync<ProductResponse>(new CommandDefinition(
    """
    SELECT code, description
    FROM products
    WHERE code = @Code
    """,
    query,
    cancellationToken: cancellationToken));

return product is null
    ? Result.Failure<ProductResponse>(Error.NotFound("Products.NotFound", $"No product with code '{query.Code}'."))
    : Result.Success(product);
```

Endpoint: `app.MapGet("products/{code}", async (string code, IQueryHandler<GetProductByCodeQuery, ProductResponse> handler, CancellationToken cancellationToken) => ...)` with `new GetProductByCodeQuery(code)` and `result.Match(TypedResults.Ok, failure => failure.ToProblem())`. `ProductResponse` is a concrete record, so the bare `TypedResults.Ok` group is unambiguous here. Reuse `ProductResponse` from `GetProducts` rather than declaring a second one, unless the shapes genuinely differ.
