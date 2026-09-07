using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Stock.AddStock;

internal sealed class AddStockCommandHandler(NpgsqlDataSource dataSource) : ICommandHandler<AddStockCommand>
{
    public async Task<Result> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var warehouseId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM warehouses WHERE lower(code) = lower(@WarehouseCode)",
            command,
            cancellationToken: cancellationToken));

        if (warehouseId is null)
            return Result.Failure(Error.NotFound("Stock.WarehouseNotFound", $"No warehouse with code '{command.WarehouseCode}'."));

        var productId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM products WHERE lower(code) = lower(@ProductCode)",
            command,
            cancellationToken: cancellationToken));

        if (productId is null)
            return Result.Failure(Error.NotFound("Stock.ProductNotFound", $"No product with code '{command.ProductCode}'."));

        var id = Guid.NewGuid();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO stock (id, warehouse_id, product_id, quantity)
            VALUES (@Id, @WarehouseId, @ProductId, @Quantity)
            ON CONFLICT (warehouse_id, product_id)
            DO UPDATE SET quantity = stock.quantity + EXCLUDED.quantity
            """,
            new { Id = id, WarehouseId = warehouseId.Value, ProductId = productId.Value, command.Quantity },
            cancellationToken: cancellationToken));

        return Result.Success();
    }
}
