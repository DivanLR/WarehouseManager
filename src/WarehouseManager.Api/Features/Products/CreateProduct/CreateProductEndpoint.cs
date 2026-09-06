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
            ICommandHandler<CreateProductCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(request.Code, request.Description);

            var result = await handler.Handle(command, cancellationToken);
            return result.Match(
                () => TypedResults.Created((string?)null, new SuccessResponse("Product created.")),
                failure => failure.ToProblem());
        })
        .WithTags("Products");
    }
}
