using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

internal sealed class CreateWarehouseCommandHandler(NpgsqlDataSource dataSource) : ICommandHandler<CreateWarehouseCommand>
{
    public async Task<Result> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var id = Guid.NewGuid();

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO warehouses (id, code, name)
                VALUES (@Id, @Code, @Name)
                """,
                new { Id = id, command.Code, command.Name },
                cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure(Error.Conflict(
                "Warehouses.DuplicateCode",
                $"A warehouse with code '{command.Code}' already exists."));
        }

        return Result.Success();
    }
}
