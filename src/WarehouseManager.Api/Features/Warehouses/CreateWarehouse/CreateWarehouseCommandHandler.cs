using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

internal sealed class CreateWarehouseCommandHandler(NpgsqlDataSource dataSource)
    : ICommandHandler<CreateWarehouseCommand, WarehouseResponse>
{
    public async Task<Result<WarehouseResponse>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            Guid id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                """
                INSERT INTO warehouses (code, name)
                VALUES (@Code, @Name)
                RETURNING id
                """,
                command,
                cancellationToken: cancellationToken));

            return Result.Success(new WarehouseResponse(id, command.Code, command.Name));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure<WarehouseResponse>(Error.Conflict(
                "Warehouses.DuplicateCode",
                $"A warehouse with code '{command.Code}' already exists."));
        }
    }
}
