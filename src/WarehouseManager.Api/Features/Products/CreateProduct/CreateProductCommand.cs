using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed record CreateProductCommand(string Code, string Description) : ICommand;
