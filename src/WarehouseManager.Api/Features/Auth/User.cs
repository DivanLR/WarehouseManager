namespace WarehouseManager.Api.Features.Auth;

internal sealed record User(Guid Id, string Email, string PasswordHash);
