# Postgres patterns for this project

## Type mapping

| Described as / C# type | Postgres | Note |
|---|---|---|
| `string`, text, name, code, description | `text` | No length cap in the column, cap in the FluentValidation rule instead |
| `Guid` | `uuid` | Generate in C# (`Guid.NewGuid()`) or `DEFAULT gen_random_uuid()` |
| `int` | `integer` | |
| `long` | `bigint` | |
| `decimal`, money, price, cost | `numeric(18,2)` | Never `real`/`double precision` for money |
| `double` | `double precision` | |
| `bool` | `boolean` | Give it a `DEFAULT` |
| `DateTime`, `DateTimeOffset`, any timestamp | `timestamptz` | Always the `tz` variant, Npgsql maps it to `DateTime` in UTC |
| `DateOnly`, date | `date` | |
| `TimeOnly`, time | `time` | |
| enum | `text` | Store the name, keeps the data readable; `integer` only if asked |
| `byte[]` | `bytea` | |
| JSON blob | `jsonb` | |

## Keys

Every table has `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` as its first column, and every
foreign key references an `id`. This is a standing decision from the user (2026-09-06), so it
applies without asking.

- Natural business key present (code, sku, reference number): keep it as a `text NOT NULL` column
  with `CONSTRAINT uq_{table}_{column} UNIQUE ({column})`. It is what humans and the API use to look
  rows up; the `id` is what other tables link to.
- Handlers generate the id in C# (`var id = Guid.NewGuid();`) and pass it in as `@Id`, a standing
  preference of the user's. The `DEFAULT gen_random_uuid()` stays on the column purely so hand
  written inserts in psql also work.
- Never `serial`/`identity` unless the user asks for integer ids; they leak insert order and complicate merges.

## Constraints and indexes

```sql
-- unique, named so the violation is recognisable in logs
CONSTRAINT uq_{table}_{column} UNIQUE ({column})

-- foreign key, named, referencing an earlier script's table
{column} text NOT NULL REFERENCES {other_table} ({other_column})

-- index on a column queried by but not unique
CREATE INDEX IF NOT EXISTS ix_{table}_{column} ON {table} ({column});

-- check
CONSTRAINT ck_{table}_{column}_non_negative CHECK ({column} >= 0)
```

A unique violation surfaces in a handler as `PostgresException` with `SqlState == PostgresErrorCodes.UniqueViolation`; a foreign key violation as `PostgresErrorCodes.ForeignKeyViolation`. Map both to `Error.Conflict(...)`.

## Audit columns

When asked for "audit", "timestamps" or "created/updated":

```sql
created_at timestamptz NOT NULL DEFAULT now(),
updated_at timestamptz
```

`updated_at` is nullable and set by the update handler, no trigger.

## Altering an existing table

Each as its own numbered script:

```sql
-- add a column
ALTER TABLE products ADD COLUMN IF NOT EXISTS unit_cost numeric(18,2);

-- add a column that must be NOT NULL to a table with rows: default first, then tighten
ALTER TABLE products ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;

-- rename (no IF EXISTS form; guard with a DO block only if the script may hit a hand patched db)
ALTER TABLE products RENAME COLUMN description TO name;

-- drop, only after all code reading it is gone
ALTER TABLE products DROP COLUMN IF EXISTS legacy_flag;
```

## Naming

Tables plural snake_case (`stock_levels`), columns singular snake_case (`product_code`), constraints prefixed `pk_`, `uq_`, `fk_`, `ck_`, indexes `ix_`. Script file PascalCase after the number: `0003_CreateStockLevels.sql`.
