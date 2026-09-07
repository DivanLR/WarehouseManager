using WarehouseManager.Api.Abstract;

namespace WarehouseManager.Api.Features.Auth.Register;

internal sealed record RegisterCommand(string Email, string Password) : ICommand;
