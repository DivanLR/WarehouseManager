# Command slice

The `CreateProduct` use case as it compiled in this project. Five files in `src/WarehouseManager.Api/Features/Products/CreateProduct/`. For a new use case, rename `Product`/`Products`/`CreateProduct` throughout, change the fields, the SQL, the validation rules and the conflict error, keep the shape.

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

For a command that returns a value, implement `ICommand<T>` instead and the handler `ICommandHandler<TCommand, T>` returning `Result<T>`.

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
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO products (code, description)
                VALUES (@Code, @Description)
                """,
                command,
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

Dapper binds `@Code` and `@Description` from the command record's properties by name. For an update or delete, `ExecuteAsync` returns the affected row count; zero rows means `Result.Failure(Error.NotFound("Products.NotFound", ...))`.

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

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Created($"/products/{command.Code}"),
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
```

Route parameters go on the lambda as ordinary parameters: `app.MapPut("products/{code}", async (string code, UpdateProductRequest request, ...)`. Success results by verb: POST `TypedResults.Created(location)`, PUT and DELETE `TypedResults.NoContent()`.
