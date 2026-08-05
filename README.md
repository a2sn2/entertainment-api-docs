# Entertainment Services API Documentation

A multi-page API documentation portal plus a production-oriented platform foundation.

- **Document reference:** API-ENT-DOC-001
- **Version:** 1.0
- **Environment:** Test
- **Classification:** Internal Integration Use

## Live documentation

https://a2sn2.github.io/entertainment-api-docs/

## Current portal architecture

The static portal follows a lightweight Clean Architecture / DDD-aligned JavaScript structure:

```text
src/
├── domain/
├── application/
├── infrastructure/
└── presentation/
```

## Production platform foundation

A separate .NET platform exists under `platform/`:

```text
platform/
├── src/        # Domain, Application, Infrastructure, API
├── apps/       # Client and Admin applications
├── tests/
├── deploy/
└── docs/
```

It introduces:

- Microsoft SQL Server persistence through EF Core;
- versioned EF Core migrations;
- ASP.NET Core API;
- business logic isolated in Domain and Application layers;
- ASP.NET Core Identity and JWT authentication;
- Administrator, Editor, Reviewer, and Reader roles;
- separate Client and Admin applications;
- SQL Server integration testing through Docker Compose and CI;
- versioned documentation workflow.

## Local Visual Studio setup

1. Open `platform/EntertainmentDocs.sln`.
2. Ensure the local SQL Server service is running.
3. From the repository root run:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1
```

4. Start `EntertainmentDocs.Api`, `EntertainmentDocs.Client`, and `EntertainmentDocs.Admin` using their `http` launch profiles.

See:

- `platform/docs/ARCHITECTURE.md`
- `platform/docs/LOCAL-SQLSERVER.md`
- `platform/docs/PRODUCTION-READINESS.md`

## Security

The repository is public. Never add production credentials, tokens, production connection strings, personal data, real player identifiers, or internal secrets. Development-only credentials are isolated to the Development environment and must never be reused elsewhere.
