---
name: add-feature
description: Add a vertical slice to WarehouseManager, one folder per use case under Features/{Entity}/{UseCase}/ with request, command or query, handler, validator, response and endpoint each in its own file, plus migration, tests and .http request. Use when asked to add a feature, use case, endpoint, command, query, or CRUD for an entity, whether the entity is new (needs a table) or already exists.
argument-hint: <entity and use cases, e.g. "Products with create and list, fields code and description">
---

# Add a feature

One use case = one folder = one endpoint. The folder holds every file for that use case, so `Features/Products/` ends up holding every Products endpoint. Nothing is registered by hand: Scrutor finds handlers, FluentValidation finds validators, `AddEndpoints` finds endpoints. `Program.cs` and `Extensions/HostDiExtensions.cs` stay untouched.

Structure follows the v1 layout of Anton Martyniuk's ShippingService sample (`C:\Users\Divan\Downloads\vertical-slice-architecture-the-best-ways-to-structure-your-project`, `Features/v1/`), with this project's own `ICommand`/`IQuery` handlers in place of MediatR and Dapper in place of EF Core.

## Steps

1. **Classify each use case.** State change is a command (POST, PUT, DELETE); read is a query (GET). Name it `{Verb}{Entity}`: `CreateProduct`, `GetProducts`, `GetProductByCode`, `UpdateProductDescription`. Confirm fields, key, and verbs with the user only where the request left them open.

2. **Table.** If the entity has no table yet, add `src/WarehouseManager.Api/Database/Migrations/{NNNN}_{Description}.sql` with the next unused number. Existing scripts are immutable, schema changes are always a new script. Lowercase snake_case identifiers, the natural business key as `PRIMARY KEY` where one exists, otherwise `uuid`:
   ```sql
   CREATE TABLE IF NOT EXISTS products
   (
       code        text PRIMARY KEY,
       description text NOT NULL
   );
   ```
   Scripts are embedded by the wildcard in the csproj and applied by DbUp on `dotnet run` in Development.

3. **Slice files.** Create `src/WarehouseManager.Api/Features/{Entity}/{UseCase}/` and one file per concern, from [references/command-slice.md](references/command-slice.md) or [references/query-slice.md](references/query-slice.md). Both are the compiled Products code; rename types and adjust SQL and fields, keep everything else.

4. **Tests.** Commands get `tests/WarehouseManager.UnitTests/{Entity}/{UseCase}CommandValidatorTests.cs`; every endpoint gets coverage in `tests/WarehouseManager.IntegrationTests/{Entity}/{Entity}Tests.cs`. Templates in [references/tests.md](references/tests.md).

5. **Requests.** Add a `###` block per endpoint to `src/WarehouseManager.Api/_requests/{Entity}.http` (create the file if new), so the user can send it from the IDE.

6. **Verify.**
   ```bash
   dotnet build WarehouseManager.slnx
   dotnet test tests/WarehouseManager.UnitTests/WarehouseManager.UnitTests.csproj
   dotnet test tests/WarehouseManager.ArchitectureTests/WarehouseManager.ArchitectureTests.csproj
   ```
   Then `dotnet run --project src/WarehouseManager.Api`. With Postgres reachable, watch DbUp apply the new script and send the `.http` requests. Without Postgres it stops at `Failed to connect to 127.0.0.1:5432`, which still proves DI resolved every new handler and endpoint (a wiring fault throws before that point). Run `WarehouseManager.IntegrationTests` too when Docker is up.

Done when: build has zero warnings, unit and architecture tests pass, every file in the naming table below exists for each use case, and `Program.cs` and `HostDiExtensions.cs` show no diff.

## Naming

| File in `Features/{Entity}/{UseCase}/` | Shape |
|---|---|
| `{UseCase}Request.cs` | `public sealed record`, HTTP body, commands only |
| `{UseCase}Command.cs` / `{UseCase}Query.cs` | `internal sealed record : ICommand` (or `ICommand<T>`) / `: IQuery<T>` |
| `{UseCase}CommandHandler.cs` / `{UseCase}QueryHandler.cs` | `internal sealed class`, primary constructor taking `NpgsqlDataSource`, returns `Result` / `Result<T>` |
| `{UseCase}CommandValidator.cs` | `internal sealed class : AbstractValidator<{UseCase}Command>`, commands only |
| `{Entity}Response.cs` | `public sealed record`, what the caller receives, queries and value-returning commands |
| `{UseCase}Endpoint.cs` | `public sealed class : IEndpoint`, inline lambda that resolves the handler and calls it |

Tests: `{UseCase}CommandValidatorTests` (methods `Validate_Should_{Outcome}_When{Condition}`), `{Entity}Tests` (methods `{UseCase}_Should_{Outcome}_When{Condition}`).

## Gotchas the compiler will hand you

- `internal` command means the validator is `internal` too (CS0060 otherwise). Tests see internals via `InternalsVisibleTo`.
- Response type is a generic interface (`IReadOnlyCollection<T>`): pass `TypedResults.Ok<IReadOnlyCollection<T>>` to `Match`, the bare `TypedResults.Ok` group is ambiguous (CS0121).
- Returning an interface typed value from a handler: `return Result.Success(products);`, the implicit conversion fails on interfaces (CS0266).
- `var` when the type is on the right hand side (`var command = new X(...)`), explicit type otherwise. IDE0007 is an error in this build.
- Duplicate key on insert: catch `PostgresException` with `SqlState == PostgresErrorCodes.UniqueViolation` and return `Error.Conflict("{Entity}.Duplicate{Key}", ...)`. Missing row on a single item query: `Error.NotFound("{Entity}.NotFound", ...)`. Both map to the right status code through `ToProblem()`.
