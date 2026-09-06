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
