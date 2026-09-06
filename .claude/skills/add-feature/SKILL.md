---
name: add-feature
description: Add a vertical slice to WarehouseManager, one folder per use case under Features/{Entity}/{UseCase}/ with request, command or query, handler, validator, response and endpoint each in its own file, plus migration and tests. Use when asked to add a feature, use case, endpoint, command, query, or CRUD for an entity, whether the entity is new (needs a table) or already exists.
argument-hint: <entity and use cases, e.g. "Products with create and list, fields code and description">
---

# Add a feature

One use case = one folder = one endpoint. The folder holds every file for that use case, so `Features/Products/` ends up holding every Products endpoint. Nothing is registered by hand: Scrutor finds handlers, FluentValidation finds validators, `AddEndpoints` finds endpoints. `Program.cs` and `Extensions/HostDiExtensions.cs` stay untouched.

Structure follows the v1 layout of Anton Martyniuk's ShippingService sample (`C:\Users\Divan\Downloads\vertical-slice-architecture-the-best-ways-to-structure-your-project`, `Features/v1/`), with this project's own `ICommand`/`IQuery` handlers in place of MediatR and Dapper in place of EF Core.

## Steps

1. **Classify each use case.** State change is a command (POST, PUT, DELETE); read is a query (GET). Name it `{Verb}{Entity}`: `CreateProduct`, `GetProducts`, `GetProductByCode`, `UpdateProductDescription`. Confirm fields, key, and verbs with the user only where the request left them open.

2. **Table.** If the entity has no table yet, use the `add-migration` skill, which owns numbering, the `id uuid PRIMARY KEY` rule, type mapping and immutability. Every table has an `id`; natural codes are `UNIQUE` columns. Handlers generate the id in C# (`var id = Guid.NewGuid();`) and pass it in as `@Id`. Commands return plain `Result`; the endpoint answers with the shared `SuccessResponse` body (`{ "message": "Product created." }`), `201 Created` for a create and `200 OK` for update, upsert or delete. Only queries return rows.

3. **Slice files.** Create `src/WarehouseManager.Api/Features/{Entity}/{UseCase}/` and one file per concern, from [references/command-slice.md](references/command-slice.md) or [references/query-slice.md](references/query-slice.md). Both are the compiled Products code; rename types and adjust SQL and fields, keep everything else.

4. **Tests.** Commands get `tests/WarehouseManager.UnitTests/{Entity}/{UseCase}CommandValidatorTests.cs`; every endpoint gets coverage in `tests/WarehouseManager.IntegrationTests/{Entity}/{Entity}Tests.cs`. Templates in [references/tests.md](references/tests.md).

5. **Verify.**
   ```bash
   dotnet build WarehouseManager.slnx
   dotnet test tests/WarehouseManager.UnitTests/WarehouseManager.UnitTests.csproj
   dotnet test tests/WarehouseManager.ArchitectureTests/WarehouseManager.ArchitectureTests.csproj
   ```
   Then `dotnet run --project src/WarehouseManager.Api`. With Postgres reachable, watch DbUp apply the new script and exercise each new endpoint (happy path and each error case) with curl or Scalar's Try it. Without Postgres it stops at `Failed to connect to 127.0.0.1:5432`, which still proves DI resolved every new handler and endpoint (a wiring fault throws before that point). Run `WarehouseManager.IntegrationTests` too when Docker is up.

Done when: build has zero warnings, unit and architecture tests pass, every file in the naming table below exists for each use case, and `Program.cs` and `HostDiExtensions.cs` show no diff.

## Naming

| File in `Features/{Entity}/{UseCase}/` | Shape |
|---|---|
| `{UseCase}Request.cs` | `public sealed record`, HTTP body, commands only |
| `{UseCase}Command.cs` / `{UseCase}Query.cs` | `internal sealed record : ICommand` (or `ICommand<T>`) / `: IQuery<T>` |
| `{UseCase}CommandHandler.cs` / `{UseCase}QueryHandler.cs` | `internal sealed class`, primary constructor taking `NpgsqlDataSource`, returns `Result` / `Result<T>` |
| `{UseCase}CommandValidator.cs` | `internal sealed class : AbstractValidator<{UseCase}Command>`, commands only |
| `../{Entity}Response.cs` | `public sealed record` at `Features/{Entity}/`, returned by that entity's queries, always carries `Guid Id` first |
| `{UseCase}Endpoint.cs` | `public sealed class : IEndpoint`, inline lambda that resolves the handler and calls it |

Tests: `{UseCase}CommandValidatorTests` (methods `Validate_Should_{Outcome}_When{Condition}`), `{Entity}Tests` (methods `{UseCase}_Should_{Outcome}_When{Condition}`).

## Gotchas the compiler will hand you

- `internal` command means the validator is `internal` too (CS0060 otherwise). Tests see internals via `InternalsVisibleTo`.
- Response type is a generic interface (`IReadOnlyCollection<T>`): pass `TypedResults.Ok<IReadOnlyCollection<T>>` to `Match`, the bare `TypedResults.Ok` group is ambiguous (CS0121).
- List queries: `var rows = await connection.QueryAsync<T>(...)` then `return Result.Success<IReadOnlyCollection<T>>(rows.AsList());`. The explicit type argument is required, inference would produce `Result<List<T>>` and the implicit conversion fails on interfaces (CS0266).
- Prefer `var`. It is required where the type is apparent (`var id = Guid.NewGuid();`, IDE0007 is an error) and allowed everywhere else.
- Duplicate key on insert: catch `PostgresException` with `SqlState == PostgresErrorCodes.UniqueViolation` and return `Error.Conflict("{Entity}.Duplicate{Key}", ...)`. Missing row on a single item query: `Error.NotFound("{Entity}.NotFound", ...)`. Both map to the right status code through `ToProblem()`.
