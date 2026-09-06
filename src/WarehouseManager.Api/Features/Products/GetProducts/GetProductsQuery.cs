using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.GetProducts;

internal sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductResponse>>;
