using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("products", async (
            CreateProductRequest request,
            ICommandHandler<CreateProductCommand, ProductResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(request.Code, request.Description);

            Result<ProductResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                product => TypedResults.Created((string?)null, product),
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
