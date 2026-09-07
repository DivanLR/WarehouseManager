using WarehouseManager.Api.Features.Auth;

namespace WarehouseManager.Api.Authentication;

internal interface ITokenProvider
{
    string Create(User user);
}
