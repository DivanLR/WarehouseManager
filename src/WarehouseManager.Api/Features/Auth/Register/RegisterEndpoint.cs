using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Auth.Register;

public sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/register", async (
            RegisterRequest request,
            ICommandHandler<RegisterCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(request.Email, request.Password);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => TypedResults.Created((string?)null, new SuccessResponse("Account created.")),
                failure => failure.ToProblem());
        })
        .AllowAnonymous()
        .WithTags("Auth");
    }
}
