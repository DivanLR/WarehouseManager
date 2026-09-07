namespace WarehouseManager.Api.Authentication;

public sealed class UserContextUnavailableException : Exception
{
    public UserContextUnavailableException()
        : base("User context is unavailable")
    {
    }

    public UserContextUnavailableException(string message)
        : base(message)
    {
    }

    public UserContextUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
