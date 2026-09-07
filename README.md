# WarehouseManager

A .NET 10 minimal API and an Angular 20 client in one repository.

```
src/, tests/   the API (vertical slice architecture, Dapper, PostgreSQL, JWT auth)
web/           the Angular client (Angular Material, list and create products)
```

The two are independent builds that share only the HTTP contract.

> **Reviewing this rather than running it?** Read [Developer notes](#developer-notes) at the bottom
> first. It covers what is AI generated, how I work, what I ran out of time for, and two design
> decisions I would like to discuss.

## What you need

| | Version | Notes |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Node.js | 22.12+ or 24+ | Angular CLI requires `^20.19.0 \|\| ^22.12.0 \|\| >=24.0.0` |
| Docker | any recent | PostgreSQL, and the integration tests |

## 1. Start PostgreSQL

First time, this creates the container and the database:

```bash
docker run --name scad-test-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=warehouse_manager -p 5432:5432 -d postgres:17-alpine
```

After a reboot, the container already exists, so just start it:

```bash
docker start scad-test-db
```

## 2. Set the JWT signing key

The API refuses to start without one, on purpose, so it can never fall back to a key committed to
the repository. Run this once, from `src/WarehouseManager.Api`:

```bash
dotnet user-secrets set "Jwt:Secret" "replace-this-with-at-least-32-random-characters"
```

The connection string in `appsettings.json` already matches the container above. Override it the
same way if your PostgreSQL differs:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=warehouse_manager;Username=postgres;Password=postgres"
```

In a deployed environment, supply both as environment variables instead: `Jwt__Secret` and
`ConnectionStrings__Database`.

## 3. Run the API

```bash
dotnet run --project src/WarehouseManager.Api
```

- Listens on <http://localhost:5026>
- Applies any pending database migrations on startup, in Development only
- Opens the Scalar API reference at <http://localhost:5026/scalar>
- Health check at <http://localhost:5026/health>

## 4. Create an account

The client has a sign in screen but no sign up screen, so create the first account through the API,
either from Scalar or with curl:

```bash
curl -X POST http://localhost:5026/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"a-valid-password"}'
```

The password must be at least 8 characters.

## 5. Run the client

```bash
cd web
npm ci
npx ng serve
```

Browse to <http://localhost:4200> and sign in with the account from step 4. The dev server proxies
`/api` to `http://localhost:5026`, so both halves appear on one origin and no CORS setup is needed.

The client can list products and create one. Warehouses, stock and orders are API only for now.

## API surface

Everything sits under `/api` and needs a bearer token, except where noted. A new endpoint is
protected automatically by an authorization fallback policy.

| Method | Route | |
|---|---|---|
| POST | `/api/auth/register` | anonymous |
| POST | `/api/auth/login` | anonymous, returns `{ "accessToken": "..." }` |
| GET | `/api/products` | |
| POST | `/api/products` | 409 if the code exists |
| GET | `/api/warehouses` | |
| POST | `/api/warehouses` | 409 if the code exists |
| GET | `/api/stock?warehouseCode=&productCode=` | both filters optional |
| POST | `/api/stock` | adds to the existing quantity for that warehouse and product |
| POST | `/api/orders` | transfers stock between warehouses, 400 if there is not enough |
| GET | `/health` | anonymous |
| GET | `/scalar`, `/openapi/v1.json` | anonymous, Development only |

Failures come back as RFC 9457 problem details, with `title` carrying the error code and `status` the
HTTP status. A validation failure adds an `errors` array naming each offending field, which is what
the client binds to its form controls. Every response also carries an `X-Correlation-Id` header,
echoed back if you send one.

## Tests

```bash
dotnet test tests/WarehouseManager.UnitTests/WarehouseManager.UnitTests.csproj
dotnet test tests/WarehouseManager.ArchitectureTests/WarehouseManager.ArchitectureTests.csproj
dotnet test tests/WarehouseManager.IntegrationTests/WarehouseManager.IntegrationTests.csproj
```

The first two need nothing running. The integration tests need Docker, because they start a
throwaway PostgreSQL container per run, so they never touch your development database.

## Database changes

Schema lives in numbered SQL scripts under `src/WarehouseManager.Api/Database/Migrations/`, applied
by DbUp on startup in Development. Each script runs once and is then frozen: to change something,
add the next numbered script rather than editing an existing one.

## If something will not start

| Symptom | Cause |
|---|---|
| `Jwt:Secret is missing` | Step 2 was skipped |
| `Failed to connect to 127.0.0.1:5432` | PostgreSQL is not running, see step 1 |
| Every request returns 401 | Not signed in, or the token expired after 60 minutes. Note that an anonymous request to a URL that does not exist also returns 401 rather than 404, because the fallback policy covers unmatched routes |
| `Port 4200 is already in use` | An earlier `ng serve` is still running |
| Integration tests all fail at startup | Docker is not running |

---

# Developer notes

I ran out of time during my creation of auth with the frontend, so this is not everything I set out
to ship. The majority of the code that is here is AI generated, using best practices and design
principles. The notes below are the honest version of what that means.

## AI generation

I would say the majority of the code is AI generated, especially the boilerplate. You will see that
I added Claude skills, which I use to repeat repetitive code rather than hand writing the same slice
every time. They are in `.claude/skills/` and are committed deliberately.

Because we are living in a world where 99% of code is AI generated, I try to use it as much as
possible, while using every second of free time to learn and understand it.

When code is generated I usually go over it and change what I do not agree with, or get it to a
structure I am comfortable with.

## How I work

I generate the boilerplate, then do my due diligence and research to make sure I am up to date with
the latest and newest technologies, and that every piece I add is up to standard. As an example,
this is my first project using Minimal APIs.

Whenever I need to implement a feature I always do my research first. These are the four go to blogs
I used for this project, and I also used their templates as references for what I built:

- <https://milanjovanovic.tech/blog>
- <https://antondevtips.com/blog>
- <https://www.nikolatech.net/>
- <https://codewithmukesh.com/>

## Two things I would like to discuss

**Why is Dapper preferred over EF Core?** I have not used Dapper much, but from a visible and
structural perspective EF Core reads easier and better, in my opinion. At the end of the day Dapper
is just a wrapper around SQL, so you should be able to do everything you can do in Dapper and raw
SQL in EF Core as well. I would like to understand the reasoning.

**Why use BadRequest for business validation?** I am not sure why this is preferred and I would love
to get your understanding of it. I recently had to take ownership of an API that does the exact same
thing, and it has been a headache working with clients and other developers when having to explain
it.

## Testing

The majority of my tests are also AI generated. This is one place where I will be honest and say
that I normally do not look at tests too much. It is the one thing I often do not look at, but for
the most part it allows me to keep my endpoints on a strict path.

Architecture tests are something I only recently started adding to projects. I have simply touched
on them here, but it is something I see often: project architecture rules get violated purely
because developers do not know they exist. Architecture tests enforce them.

## Frontend

For the frontend I focused on using best practices. Because I ran out of time I did not fully get to
vet everything. You will see that I am using signals and computed signals.

## What I would have implemented with more time

- **Role based access control.** I did not actually get to implement what I wanted with
  authentication. I would have loved to give RBAC a go, mainly because I am nearing Senior in my
  current company.
- **Role based product management.**
- **More screens in Angular**, just to show how I use components and how I set up an Angular
  project. It has been a minute since I worked in Angular and I enjoy it.

## Transparency

I kept all my Claude skills and Markdown files in the repository on purpose, so how I worked is
visible rather than hidden:

| Path | What it is |
|---|---|
| `CLAUDE.md` | API conventions, tech stack decisions and anti-patterns |
| `web/CLAUDE.md` | The Angular instructions the frontend was written against |
| `.claude/skills/add-feature/` | The shape of one vertical slice, with templates |
| `.claude/skills/add-migration/` | Schema change rules, numbering and type mapping |
