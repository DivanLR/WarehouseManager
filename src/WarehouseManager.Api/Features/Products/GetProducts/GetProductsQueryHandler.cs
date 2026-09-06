using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.GetProducts;

internal sealed class GetProductsQueryHandler(NpgsqlDataSource dataSource)
    : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductResponse>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var products = await connection.QueryAsync<ProductResponse>(new CommandDefinition(
            """
            SELECT id, code, description
            FROM products
            ORDER BY code
            """,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ProductResponse>>(products.AsList());
    }
}
