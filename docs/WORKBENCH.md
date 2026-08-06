# FoundationKit Workbench

## Purpose

The Workbench is a real local consumer of FoundationKit. It provides a creative discovery page, explains implemented capabilities, recommends starting ideas, asks what the visitor wants to build, saves completed briefs in SQL Server, and creates a public-safe contact link for ALHassan ALShami.

It is not part of the reusable package surface and must not be copied into `src/`.

## Fastest start with Docker

Requirements:

- Docker Desktop on Windows or macOS, or Docker Engine with Compose on Linux;
- free local ports `8080` for the Workbench and `14333` for optional host access to SQL Server.

PowerShell:

```powershell
.\scripts\run-workbench.ps1
```

Bash:

```bash
./scripts/run-workbench.sh
```

The helper generates a strong ephemeral development password, starts both containers, waits for `/api/health`, and opens:

```text
http://localhost:8080
```

The SQL Server database is persisted in the Docker volume `foundationkit-sql-data`. Stop services without deleting data:

```powershell
.\scripts\stop-workbench.ps1
```

```bash
./scripts/stop-workbench.sh
```

Delete services and local database data intentionally:

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

## Use an existing local SQL Server

The default application connection string uses Windows authentication:

```text
Server=localhost;
Database=FoundationKitWorkbench;
Trusted_Connection=True;
TrustServerCertificate=True;
MultipleActiveResultSets=True
```

Run:

```powershell
dotnet run --project samples/FoundationKit.Workbench
```

The `http` launch profile opens `http://localhost:5057` automatically.

For a named SQL Server Express instance:

```powershell
$env:ConnectionStrings__Workbench="Server=.\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project samples/FoundationKit.Workbench
```

For SQL authentication:

```powershell
$env:ConnectionStrings__Workbench="Server=localhost,1433;Database=FoundationKitWorkbench;User Id=foundationkit;Password=<local-password>;TrustServerCertificate=True;Encrypt=False"
dotnet run --project samples/FoundationKit.Workbench
```

Do not commit real credentials to `appsettings.json`, Docker files, scripts, or documentation.

## Migrations and schema

The Workbench owns its migrations under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

The application calls `Database.MigrateAsync` at startup with bounded retries. The migration creates the `BuildBriefs` table and index. EF Core migrations are the Workbench schema source of truth.

Create a new migration after changing Workbench persistence:

```bash
dotnet ef migrations add <MigrationName> \
  --project samples/FoundationKit.Workbench \
  --startup-project samples/FoundationKit.Workbench \
  --output-dir Infrastructure/Migrations
```

Review generated SQL and migration code before committing. Never move Workbench migrations into a reusable FoundationKit package.

## API routes

| Route | Behavior |
|---|---|
| `GET /api/runtime` | reports local runtime and SQL Server persistence mode |
| `GET /api/catalog` | returns the canonical implemented capability catalog |
| `GET /api/health` | verifies SQL Server connectivity |
| `POST /api/build-briefs` | validates and saves a BuildBrief aggregate |
| `GET /api/build-briefs/{id}` | reads a saved local brief |

A saved response includes a prefilled GitHub contact URL. The GitHub issue is public; confidential details must use a separately agreed private channel.

## Local execution flow

```text
Browser loads shared site assets
        ↓
GET /api/runtime identifies local mode
        ↓
GET /api/catalog renders packages, capabilities, ideas, and adoption steps
        ↓
Visitor completes the four-step builder
        ↓
POST /api/build-briefs
        ↓
FoundationKit Result validation + repository + unit of work
        ↓
SQL Server migration-backed table
        ↓
Contact summary and link
```

## GitHub Pages difference

GitHub Pages deploys the same `site/` assets and catalog but no ASP.NET Core host. The page detects the missing API and switches to demo mode. In demo mode answers remain in browser memory, are not persisted, and are not sent automatically.

## Troubleshooting

Check containers:

```bash
docker compose -f deploy/docker-compose.yml ps
```

Read logs:

```bash
docker compose -f deploy/docker-compose.yml logs --tail=300
```

Verify health:

```bash
curl http://localhost:8080/api/health
```

A local port conflict can be fixed by stopping the conflicting service or adjusting the host-side port in `deploy/docker-compose.yml`. The Workbench container must continue listening on internal port `8080`, and the SQL Server container must continue listening on internal port `1433` unless both the compose connection string and service configuration are changed together.

## Production warning

The Workbench is not a production starter application. It intentionally omits identity, authorization, rate limiting, production secret management, telemetry export, backups, high availability, external ingress, and a controlled deployment migration strategy.
