---
name: add-migration
description: Add a DbUp SQL migration script to WarehouseManager from a table or schema change described in words or as a C# type. Use when asked to add a migration, create or alter a table, add a column, index, constraint or foreign key, or when a new feature needs a table that does not exist yet.
argument-hint: <table and columns, e.g. "products: code text PK, description text, created_at">
---

# Add a migration

Schema lives only in numbered SQL scripts under `src/WarehouseManager.Api/Database/Migrations/`. DbUp embeds them (csproj wildcard, no edit needed), runs them in filename order on `dotnet run` in Development, and journals each one so it runs once. Every script is immutable once written: a change to an existing table is a new script, never an edit.

## Steps

1. **Pin the spec.** From the user's description or C# type, settle for each column: name, Postgres type, nullability, default. Settle for the table: primary key, unique constraints, foreign keys, indexes. Ask only where the request leaves a real gap, typically "what is the key?" when no natural key is obvious. Types and conventions: [references/postgres-patterns.md](references/postgres-patterns.md).

2. **Next number.** List the folder, take the highest `NNNN` prefix and add one, zero padded to four digits. Name: `{NNNN}_{PascalCaseDescription}.sql`, verb first: `0003_CreateStockLevels.sql`, `0004_AddUnitCostToProducts.sql`.

3. **Write the script.** New table: `CREATE TABLE IF NOT EXISTS`. Change: `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, and so on. One statement per line, comma aligned, lowercase snake_case identifiers, `text` and `timestamptz` over `varchar` and `timestamp`. Shape:
   ```sql
   CREATE TABLE IF NOT EXISTS stock_levels
   (
       id           uuid PRIMARY KEY,
       product_code text NOT NULL REFERENCES products (code),
       quantity     integer NOT NULL DEFAULT 0,
       updated_at   timestamptz NOT NULL DEFAULT now(),
       CONSTRAINT uq_stock_levels_product_code UNIQUE (product_code)
   );
   ```

4. **Dapper mapping.** Any multi word column (`created_at`) needs a matching C# property (`CreatedAt`). Dapper matches names case insensitively but does not strip underscores by default, so either alias in every SELECT (`created_at AS CreatedAt`) or, once, set `DefaultTypeMap.MatchNamesWithUnderscores = true;` at the top of `HostDiExtensions.AddWebHostInfrastructure`. Prefer the one line setting the first time a snake_case column appears, and say so in the reply.

5. **Verify.** `dotnet build WarehouseManager.slnx` (confirms the file is picked up as an embedded resource). Then `dotnet run --project src/WarehouseManager.Api`: with Postgres reachable, DbUp logs `Executing Database Server script '...{NNNN}_....sql'` and the app stays up. Reachable and the script fails: DbUp prints the Postgres error and the app exits, fix the SQL in place (the script has not been journaled, so editing it is still allowed at this point). Postgres unreachable: report that the script is written and embedded but unverified against a live server.

Done when: the script exists with the next number, the build is clean, and either DbUp has applied it or the reply states plainly that it could not be run.

## Rules that hold every time

- A script already applied anywhere (journaled) is frozen. Follow up changes are new scripts, including fixes to mistakes.
- Every `CREATE` and `ADD` carries `IF NOT EXISTS`, so a script survives being run against a database that was hand patched.
- Foreign keys reference tables created in an earlier numbered script.
- Dropping or renaming a column that existing code reads is a two step change: add the new shape first, migrate code, drop later in a separate script.
