using Dapper;
using Npgsql;
using WarehouseManager.Api.Abstract;
using WarehouseManager.Api.Authentication;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Features.Auth.Register;

internal sealed class RegisterCommandHandler(
    NpgsqlDataSource dataSource,
    IPasswordHasher passwordHasher) : ICommandHandler<RegisterCommand>
{
    public async Task<Result> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var id = Guid.NewGuid();
        var email = command.Email.Trim();
        var passwordHash = passwordHasher.Hash(command.Password);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO users (id, email, password_hash)
                VALUES (@Id, @Email, @PasswordHash)
                """,
                new { Id = id, Email = email, PasswordHash = passwordHash },
                cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure(Error.Conflict(
                "Users.DuplicateEmail",
                $"An account with email '{email}' already exists."));
        }

        return Result.Success();
    }
}
