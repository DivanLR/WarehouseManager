using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Warehouses.GetWarehouses;

internal sealed class GetWarehousesQueryHandler(NpgsqlDataSource dataSource)
    : IQueryHandler<GetWarehousesQuery, IReadOnlyCollection<WarehouseResponse>>
{
    public async Task<Result<IReadOnlyCollection<WarehouseResponse>>> Handle(GetWarehousesQuery query, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var warehouses = await connection.QueryAsync<WarehouseResponse>(new CommandDefinition(
            """
            SELECT id, code, name
            FROM warehouses
            ORDER BY code
            """,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<WarehouseResponse>>(warehouses.AsList());
    }
}
