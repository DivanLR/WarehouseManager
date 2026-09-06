# WarehouseManager — Web API

## Project Context

WarehouseManager is a .NET 10 REST API for warehouse management (inventory, stock, orders, exact
domain features are still to be built). It is a single project Vertical Slice Architecture,
`src/WarehouseManager.Api`, with no Domain, Application or Infrastructure layers. Everything that
belongs to one use case lives together in one folder under `Features/{Entity}/{UseCase}/`, with each
concern in its own file (request, command or query, handler, validator, response, endpoint). This
follows the "v1" layout from Anton Martyniuk's ShippingService sample ("Vertical Slice Architecture:
the best ways to structure your project"), adopted on request. The `add-feature` skill
(`.claude/skills/add-feature/`) is the single source of truth for the shape of a slice, its
`references/` folder holds the compiled templates.

## Tech Stack

- **.NET 10** / C# 14
- **ASP.NET Core Minimal APIs** — one `IEndpoint` (`Abstract/IEndpoint.cs`) per use case, living inside
  that use case's feature folder, auto discovered and mapped via `app.MapEndpoints()`. Program.cs never
  changes when adding endpoints.
- **PostgreSQL with Dapper**, not EF Core. `NpgsqlDataSource` is injected directly into handlers
  (registered via the `Npgsql.DependencyInjection` package's `AddNpgsqlDataSource`). No repository
  or `IApplicationDbContext` abstraction.
- **Custom lightweight CQRS**, not MediatR, not Wolverine. `ICommand`/`IQuery` and their handler
  interfaces live in `Abstract/`, discovered by Scrutor assembly scanning.
- **Scrutor** for handler discovery and decoration. Logging and validation are cross cutting
  decorators (`Behaviors/`), not pipeline behaviours on a mediator.
- **FluentValidation** — request validation, one validator per command, auto discovered.
- **Central package management** — `Directory.Packages.props` at the solution root. Add packages
  with `dotnet add package <name>` from the project directory so the version lands centrally.
- **xUnit** + **Shouldly** + **NSubstitute** + **NetArchTest** + **Testcontainers.PostgreSql** — testing.

No authentication, no Serilog/Seq, no OpenTelemetry, and no Docker Compose yet. All were deliberately
deferred at setup time and should be added when a real requirement appears, not speculatively.

## Architecture

```
src/WarehouseManager.Api/
  Abstract/                 # IEndpoint, ICommand, ICommandHandler, IQuery, IQueryHandler
  Behaviors/                # LoggingDecorator, ValidationDecorator (Scrutor TryDecorate), GlobalExceptionHandler
  Database/Migrations/      # DbUp SQL scripts, see Commands section below
  Extensions/               # HostDiExtensions (all DI), EndpointExtensions (auto discovery),
                            # EndpointResultsExtensions (Result -> ProblemDetails), ResultExtensions (Match),
                            # MigrationExtensions (DbUp)
  Features/
    {Entity}/
      {UseCase}/            # one folder per use case, one file per concern:
        {UseCase}Request.cs           # public record, HTTP body shape (commands only)
        {UseCase}Command.cs           # internal record : ICommand / ICommand<T>   (or {UseCase}Query.cs : IQuery<T>)
        {UseCase}CommandHandler.cs    # internal sealed class                       (or {UseCase}QueryHandler.cs)
        {UseCase}CommandValidator.cs  # internal sealed, AbstractValidator<{UseCase}Command>, auto discovered
        {Entity}Response.cs           # public record returned to the caller
        {UseCase}Endpoint.cs          # public sealed class : IEndpoint, maps the route, calls the handler
  SharedModels/             # Result, Error, ErrorType, ValidationError
  _requests/                # .http files for hitting the endpoints from the IDE
  Program.cs                # composition root only

tests/
  WarehouseManager.UnitTests/          # xUnit, Shouldly, NSubstitute. Validators and SharedModels.
  WarehouseManager.ArchitectureTests/  # NetArchTest. Handlers and endpoints must be sealed.
  WarehouseManager.IntegrationTests/   # WebApplicationFactory<Program> + Testcontainers.PostgreSql.
                                        # Requires Docker running locally to execute.
```

When adding a feature, use the `add-feature` skill. It carries the steps, the naming table, the
file templates and the compiler gotchas; nothing about slice shape is duplicated here. The one rule
worth repeating because it has caused confusion: the endpoint file contains routing only, an inline
lambda that resolves the handler from DI and calls it. The handler is always its own class in its
own file, and no method named `Handle` belongs in an endpoint.

## Coding Standards

- **C# 14 features** — primary constructors, collection expressions, records, pattern matching
- **File scoped namespaces** — always
- **`var` for obvious types** — explicit types when the type is not clear from context
- **Naming** — PascalCase for public members, suffix async methods with `Async`
- **No regions** — ever
- **No comments for obvious code** — only comment "why", never "what". A `ponytail:` prefixed
  comment marks a deliberate shortcut with a named ceiling and upgrade path, do not remove these
  without addressing what they flag.
- **Sealed by default** — handlers and endpoints are checked by `ArchitectureTests` and must stay sealed.

## Skills

Load these dotnet-claude-kit skills for context:

- `modern-csharp` — C# 14 language features and idioms
- `vertical-slice` — feature folder structure and handler patterns
- `minimal-api` — endpoint routing, TypedResults, OpenAPI metadata
- `testing` — xUnit, WebApplicationFactory, Testcontainers
- `error-handling` — Result pattern, ProblemDetails
- `dependency-injection` — service registration, Scrutor scanning and decoration
- `authentication` — load only once a real login requirement appears
- `logging`, `opentelemetry` — load only once observability is actually being wired up
- `workflow-mastery` — parallel worktrees, verification loops, subagent patterns, context discipline
- `instinct-system` — capture corrections, instincts, and discoveries as persistent learning
- `wrap-up` — structured session handoff to `.claude/handoff.md`

## MCP Tools

`cwm-roslyn-navigator` is already registered as a dotnet-claude-kit plugin server (`claude mcp list`
shows it connected), no project level `.mcp.json` is required for it.

- **Before modifying a type** — `find_symbol` to locate it, `get_public_api` to understand its surface
- **Before adding a reference** — `find_references` to understand existing usage
- **To understand architecture** — `get_project_graph` to see how the test projects depend on the Api
- **To find implementations** — `find_implementations` instead of grep for `IEndpoint`/`ICommandHandler` implementers
- **To check for errors** — `get_diagnostics` after changes

## Commands

```bash
# Build the whole solution
dotnet build WarehouseManager.slnx

# Run the Api
dotnet run --project src/WarehouseManager.Api

# Run unit and architecture tests (no external dependencies required)
dotnet test tests/WarehouseManager.UnitTests/WarehouseManager.UnitTests.csproj
dotnet test tests/WarehouseManager.ArchitectureTests/WarehouseManager.ArchitectureTests.csproj

# Run integration tests (requires Docker running, spins up a throwaway Postgres container)
dotnet test tests/WarehouseManager.IntegrationTests/WarehouseManager.IntegrationTests.csproj

# Add a package with the version landing in Directory.Packages.props
dotnet add package <PackageName>

# Format check
dotnet format --verify-no-changes
```

There is no `dotnet ef` migration workflow, this project uses Dapper, not EF Core. Schema changes
are plain SQL scripts under `src/WarehouseManager.Api/Database/Migrations/`, applied by DbUp
(`WarehouseManager.Api/Extensions/MigrationExtensions.cs`). Scripts run in filename order,
each one only once, tracked in DbUp's own journal table. `app.ApplyMigrations()` runs them
automatically in Development on every `dotnet run`, including creating the target database itself
if it does not exist yet. Add a new script with the next number prefix (for example
`0002_...sql`), never edit a script that has already shipped.

## Workflow

- **Plan first** — enter plan mode for any non-trivial task (3+ steps or architecture decisions).
- **Verify before done** — run `dotnet build` and the unit/architecture tests after changes. For
  anything touching DI wiring, also run the Api and hit the affected endpoint: a broken Scrutor
  `Decorate` call throws at startup, not at compile time (see Anti-patterns below).
- **Fix bugs autonomously** — investigate logs, errors, and failing tests, then resolve them.
- **Stop and re-plan** — if implementation goes sideways, stop and re-plan.
- **Learn from corrections** — after any correction, capture the pattern in memory.

## Anti-patterns

Do NOT generate code that:

- Defines endpoints in Program.cs — use `IEndpoint` per feature with `app.MapEndpoints()` auto discovery
- Manually wires `MapGroup` calls in Program.cs — Program.cs should never change when adding endpoints
- Uses `services.Decorate(...)` for the CQRS pipeline decorators — use `services.TryDecorate(...)`.
  `Decorate` throws at startup if a generic handler type (for example `ICommandHandler<,>`) has zero
  registrations, which is the normal state whenever a slice only has queries or only has commands.
- Creates a repository or `IApplicationDbContext` abstraction over Dapper/`NpgsqlDataSource` — inject
  `NpgsqlDataSource` directly into handlers
- Adds EF Core, a DbContext, or an `dotnet ef migrations` step — this project uses Dapper, not EF Core
- Edits an already shipped migration script under `Database/Migrations/` — add a new numbered script instead
- Uses `DateTime.Now` or `DateTime.UtcNow` directly in a handler — inject the built-in `TimeProvider`
  instead (register `TimeProvider.System` when the first consumer actually needs it, there is none yet)
- Creates `new HttpClient()` — use `IHttpClientFactory`
- Uses `async void` — always return `Task`
- Blocks with `.Result` or `.Wait()` — await instead
- Returns domain entities from endpoints — always map to response DTOs
- Uses an in-memory fake for integration tests — use Testcontainers.PostgreSql
- Catches bare `Exception` — catch specific types, let `GlobalExceptionHandler` catch the rest
- Uses string interpolation in log messages — use structured logging templates
- Adds JWT auth, Serilog/Seq, OpenTelemetry, or Docker Compose without being asked — these were
  deliberately deferred, add them when a real requirement appears
