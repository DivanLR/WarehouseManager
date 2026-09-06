using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Stock.GetStock;

internal sealed class GetStockQueryHandler(NpgsqlDataSource dataSource)
    : IQueryHandler<GetStockQuery, IReadOnlyCollection<StockResponse>>
{
    public async Task<Result<IReadOnlyCollection<StockResponse>>> Handle(GetStockQuery query, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        if (query.WarehouseCode is { Length: > 0 })
        {
            var warehouseExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS (SELECT 1 FROM warehouses WHERE code = @WarehouseCode)",
                query,
                cancellationToken: cancellationToken));

            if (!warehouseExists)
                return Result.Failure<IReadOnlyCollection<StockResponse>>(
                    Error.NotFound("Warehouses.NotFound", $"No warehouse with code '{query.WarehouseCode}'."));
        }

        if (query.ProductCode is { Length: > 0 })
        {
            var productExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS (SELECT 1 FROM products WHERE code = @ProductCode)",
                query,
                cancellationToken: cancellationToken));

            if (!productExists)
                return Result.Failure<IReadOnlyCollection<StockResponse>>(
                    Error.NotFound("Products.NotFound", $"No product with code '{query.ProductCode}'."));
        }

        var stock = await connection.QueryAsync<StockResponse>(new CommandDefinition(
            """
            SELECT s.id, s.warehouse_id, w.code AS warehouse_code, s.product_id, p.code AS product_code, s.quantity
            FROM stock s
            JOIN warehouses w ON w.id = s.warehouse_id
            JOIN products p ON p.id = s.product_id
            WHERE (@WarehouseCode IS NULL OR w.code = @WarehouseCode)
              AND (@ProductCode IS NULL OR p.code = @ProductCode)
            ORDER BY w.code, p.code
            """,
            new
            {
                WarehouseCode = string.IsNullOrEmpty(query.WarehouseCode) ? null : query.WarehouseCode,
                ProductCode = string.IsNullOrEmpty(query.ProductCode) ? null : query.ProductCode
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<StockResponse>>(stock.AsList());
    }
}
