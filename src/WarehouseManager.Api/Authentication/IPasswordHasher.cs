namespace WarehouseManager.Api.Authentication;

internal interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
