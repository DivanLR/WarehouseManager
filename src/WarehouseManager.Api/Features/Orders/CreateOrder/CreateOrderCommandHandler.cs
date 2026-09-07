using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Orders.CreateOrder;

internal sealed class CreateOrderCommandHandler(NpgsqlDataSource dataSource) : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var ids = await connection.QuerySingleAsync<ResolvedIds>(new CommandDefinition(
            """
            SELECT
                (SELECT id FROM products   WHERE lower(code) = lower(@ProductCode))              AS product_id,
                (SELECT id FROM warehouses WHERE lower(code) = lower(@SourceWarehouseCode))      AS source_warehouse_id,
                (SELECT id FROM warehouses WHERE lower(code) = lower(@DestinationWarehouseCode)) AS destination_warehouse_id
            """,
            command,
            cancellationToken: cancellationToken));

        if (ids.ProductId is null)
            return Result.Failure(Error.NotFound("Orders.ProductNotFound", $"No product with code '{command.ProductCode}'."));

        if (ids.SourceWarehouseId is null)
            return Result.Failure(Error.NotFound("Orders.SourceWarehouseNotFound", $"No warehouse with code '{command.SourceWarehouseCode}'."));

        if (ids.DestinationWarehouseId is null)
            return Result.Failure(Error.NotFound("Orders.DestinationWarehouseNotFound", $"No warehouse with code '{command.DestinationWarehouseCode}'."));

        var parameters = new
        {
            OrderId = Guid.NewGuid(),
            StockId = Guid.NewGuid(),
            ProductId = ids.ProductId.Value,
            SourceWarehouseId = ids.SourceWarehouseId.Value,
            DestinationWarehouseId = ids.DestinationWarehouseId.Value,
            command.Quantity
        };

        //I am using a transaction here to ensure everything goes in or nothing
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var available = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                """
                SELECT quantity
                FROM stock
                WHERE warehouse_id = @SourceWarehouseId AND product_id = @ProductId
                FOR UPDATE NOWAIT
                """,
                parameters,
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (available is null || available < command.Quantity)
                return Result.Failure(Error.Problem(
                    "Orders.InsufficientStock",
                    $"Warehouse '{command.SourceWarehouseCode}' has {available ?? 0} of '{command.ProductCode}', cannot transfer {command.Quantity}."));

           
            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE stock
                SET quantity = quantity - @Quantity
                WHERE warehouse_id = @SourceWarehouseId AND product_id = @ProductId;

                SELECT quantity
                FROM stock
                WHERE warehouse_id = @DestinationWarehouseId AND product_id = @ProductId
                FOR UPDATE NOWAIT;

                INSERT INTO stock (id, warehouse_id, product_id, quantity)
                VALUES (@StockId, @DestinationWarehouseId, @ProductId, @Quantity)
                ON CONFLICT (warehouse_id, product_id)
                DO UPDATE SET quantity = stock.quantity + EXCLUDED.quantity;

                INSERT INTO orders (id, product_id, source_warehouse_id, destination_warehouse_id, quantity)
                VALUES (@OrderId, @ProductId, @SourceWarehouseId, @DestinationWarehouseId, @Quantity);
                """,
                parameters,
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            return Result.Failure(Error.Conflict(
                "Orders.StockLocked",
                $"Stock of '{command.ProductCode}' is being changed by another operation, try again."));
        }

        return Result.Success();
    }

    private sealed record ResolvedIds(Guid? ProductId, Guid? SourceWarehouseId, Guid? DestinationWarehouseId);
}
