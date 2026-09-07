using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Authentication;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Auth.Login;

internal sealed class LoginCommandHandler(
    NpgsqlDataSource dataSource,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var user = await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            "SELECT id, email, password_hash FROM users WHERE lower(email) = lower(@Email)",
            new { Email = command.Email.Trim() },
            cancellationToken: cancellationToken));

        var invalidCredentials = Error.Unauthorized("Users.InvalidCredentials", "Email or password is incorrect.");

        if (user is null)
            return Result.Failure<AuthResponse>(invalidCredentials);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(invalidCredentials);

        return Result.Success(new AuthResponse(tokenProvider.Create(user)));
    }
}
