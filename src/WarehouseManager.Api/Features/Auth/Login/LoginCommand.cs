using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Auth.Login;

internal sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;
