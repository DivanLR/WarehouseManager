# Command slice

The `CreateProduct` use case as it compiled and passed its tests in this project. Four files in `src/WarehouseManager.Api/Features/Products/CreateProduct/`. For a new use case, rename `Product`/`Products`/`CreateProduct` throughout, change the fields, the SQL, the validation rules and the conflict error, keep the shape.

Commands return plain `Result`; the endpoint turns success into the shared `SharedModels/SuccessResponse` (`{ "message": "..." }`). A create answers `201 Created` with that body, an update or upsert answers `200 OK` with it (a 204 cannot carry a body). The caller reads state back through the entity's GET. Only reach for `ICommand<T>` when the caller genuinely cannot proceed without a value the database produced.

## CreateProductRequest.cs

```csharp
namespace WarehouseManager.Api.Features.Products.CreateProduct;

public sealed record CreateProductRequest(string Code, string Description);
```

## CreateProductCommand.cs

```csharp
using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed record CreateProductCommand(string Code, string Description) : ICommand;
```

## CreateProductCommandHandler.cs

```csharp
using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed class CreateProductCommandHandler(NpgsqlDataSource dataSource) : ICommandHandler<CreateProductCommand>
{
    public async Task<Result> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var id = Guid.NewGuid();

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO products (id, code, description)
                VALUES (@Id, @Code, @Description)
                """,
                new { Id = id, command.Code, command.Description },
                cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure(Error.Conflict(
                "Products.DuplicateCode",
                $"A product with code '{command.Code}' already exists."));
        }

        return Result.Success();
    }
}
```

The id is generated in C# with `Guid.NewGuid()` and passed in as `@Id`, a standing preference of the user's, so no `RETURNING` is needed. The anonymous parameter object spreads the command's properties (`command.Code` binds as `@Code`) alongside the extra `Id`. Use `var` freely; the analyzer requires it where the type is apparent and allows it everywhere else.

Resolving a code to an id first (for a table that references another) is a separate `ExecuteScalarAsync<Guid?>("SELECT id FROM ... WHERE code = @Code")`, with `null` mapped to `Error.NotFound`. Several obvious statements beat one clever one; only the write itself has to be atomic. For an update or delete, `ExecuteAsync` returns the affected row count; zero rows means `Result.Failure(Error.NotFound("Products.NotFound", ...))`. For "insert or add to existing", use `INSERT ... ON CONFLICT (...) DO UPDATE SET quantity = table.quantity + EXCLUDED.quantity`, see `Features/Stock/AddStock`.

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
            ICommandHandler<CreateProductCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(request.Code, request.Description);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Created((string?)null, new SuccessResponse("Product created.")),
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
```

Success result by verb, always with a `SuccessResponse` body and a short past tense message: POST that creates `TypedResults.Created((string?)null, new SuccessResponse("... created."))`; POST that applies an operation, PUT and DELETE `TypedResults.Ok(new SuccessResponse("... added." / "... updated." / "... deleted."))`. Route parameters go on the lambda as ordinary parameters: `app.MapPut("products/{code}", async (string code, UpdateProductRequest request, ...)`.
