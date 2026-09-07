using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Extensions;

namespace WarehouseManager.Api.Features.Auth.Login;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/login", async (
            LoginRequest request,
            ICommandHandler<LoginCommand, AuthResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                TypedResults.Ok,
                failure => failure.ToProblem());
        })
        .AllowAnonymous()
        .WithTags("Auth");
    }
}
