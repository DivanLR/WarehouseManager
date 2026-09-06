# Command slice

The `CreateProduct` use case as it compiled and passed its tests in this project. Four files in `src/WarehouseManager.Api/Features/Products/CreateProduct/`, plus the entity level response record. For a new use case, rename `Product`/`Products`/`CreateProduct` throughout, change the fields, the SQL, the validation rules and the conflict error, keep the shape.

## ../ProductResponse.cs (entity level, shared)

`src/WarehouseManager.Api/Features/Products/ProductResponse.cs`, one per entity, used by every use case that returns the entity.

```csharp
namespace WarehouseManager.Api.Features.Products;

public sealed record ProductResponse(Guid Id, string Code, string Description);
```

## CreateProductRequest.cs

```csharp
namespace WarehouseManager.Api.Features.Products.CreateProduct;

public sealed record CreateProductRequest(string Code, string Description);
```

## CreateProductCommand.cs

```csharp
using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed record CreateProductCommand(string Code, string Description) : ICommand<ProductResponse>;
```

A command with nothing to return implements plain `ICommand`, its handler `ICommandHandler<TCommand>` returning `Result`, and the endpoint answers `TypedResults.NoContent()`.

## CreateProductCommandHandler.cs

```csharp
using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed class CreateProductCommandHandler(NpgsqlDataSource dataSource)
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            Guid id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                """
                INSERT INTO products (code, description)
                VALUES (@Code, @Description)
                RETURNING id
                """,
                command,
                cancellationToken: cancellationToken));

            return Result.Success(new ProductResponse(id, command.Code, command.Description));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure<ProductResponse>(Error.Conflict(
                "Products.DuplicateCode",
                $"A product with code '{command.Code}' already exists."));
        }
    }
}
```

Dapper binds `@Code` and `@Description` from the command record's properties by name. The `id` column has a database default, so the insert omits it and reads it back with `RETURNING id`. For an update or delete, `ExecuteAsync` returns the affected row count; zero rows means `Result.Failure(Error.NotFound("Products.NotFound", ...))`.

## CreateProductCommandValidator.cs

```csharp
using FluentValidation;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(255);
    }
}
```

Runs automatically through `ValidationDecorator` before the handler. A failing rule becomes a 400 with an `errors` array, the handler is never called.

## CreateProductEndpoint.cs

```csharp
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("products", async (
            CreateProductRequest request,
            ICommandHandler<CreateProductCommand, ProductResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(request.Code, request.Description);

            Result<ProductResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                product => TypedResults.Created((string?)null, product),
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
```

`Created` carries the body so the caller receives the generated `Id`. The location is `null` until a `GET /products/{id}` exists; once it does, pass `$"/products/{product.Id}"`. Route parameters go on the lambda as ordinary parameters: `app.MapPut("products/{id:guid}", async (Guid id, UpdateProductRequest request, ...)`. PUT and DELETE answer `TypedResults.NoContent()`.
