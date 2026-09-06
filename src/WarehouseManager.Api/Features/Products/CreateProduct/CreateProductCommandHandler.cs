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
