# Local SQL Server Development

## Standard local topology

- SQL Server: local default instance or a named instance
- Database: `EntertainmentDocs_Dev`
- Authentication: Windows Authentication
- ORM: Entity Framework Core 8
- Schema management: EF Core migrations
- API: `http://localhost:5080`
- Client: `http://localhost:5081`
- Admin: `http://localhost:5082`

## One-command setup

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1
```

The script uses the Windows computer name as the default SQL Server name. For a named instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1 -Server "MACHINE\INSTANCE"
```

It restores tools and packages, builds the solution, and applies all EF Core migrations to `EntertainmentDocs_Dev`.

## Visual Studio startup

1. Open `platform/EntertainmentDocs.sln`.
2. Set `EntertainmentDocs.Api` as the startup project and use the `http` launch profile.
3. Start the API and verify `http://localhost:5080/health`.
4. Start `EntertainmentDocs.Client` and `EntertainmentDocs.Admin` using their `http` profiles.

Development-only credentials:

- E-mail: `admin@local.test`
- Password: `LocalAdmin!2026`

These values are loaded only from `appsettings.Development.json` and must never be used outside local development.

## Useful EF Core commands

Run from `platform/`:

```powershell
dotnet tool restore

dotnet ef migrations list `
  --project .\src\EntertainmentDocs.Infrastructure\EntertainmentDocs.Infrastructure.csproj `
  --startup-project .\src\EntertainmentDocs.Api\EntertainmentDocs.Api.csproj

dotnet ef database update `
  --project .\src\EntertainmentDocs.Infrastructure\EntertainmentDocs.Infrastructure.csproj `
  --startup-project .\src\EntertainmentDocs.Api\EntertainmentDocs.Api.csproj
```

## SSMS verification

Connect with Windows Authentication, refresh **Databases**, and open `EntertainmentDocs_Dev`. The database should contain Identity tables, documentation tables, audit entries, and `__EFMigrationsHistory`.

## Safety rules

- Do not commit production connection strings or passwords.
- Do not point local development at production databases.
- Review every migration before production deployment.
- Back up production before applying schema changes.
- Use environment variables or a managed secret store outside Development.
