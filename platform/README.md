# Entertainment Documentation Platform

Production-oriented foundation for evolving the static API documentation into a managed platform.

## Applications

- `EntertainmentDocs.Api` — backend API and authorization boundary.
- `EntertainmentDocs.Client` — read-only client experience.
- `EntertainmentDocs.Admin` — administration experience.

## Core layers

- `Domain` — business invariants and aggregates.
- `Application` — use cases and ports.
- `Infrastructure` — Microsoft SQL Server, EF Core, Identity, JWT, and repositories.
- `API` — HTTP contracts and policies.

## Local Visual Studio development

Use the local SQL Server instance with Windows Authentication:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1
```

Then run the API, Client, and Admin projects using their `http` launch profiles.

See `docs/LOCAL-SQLSERVER.md` for the complete setup and SSMS verification steps.

## Integration stack

Docker Compose remains available for repeatable SQL Server integration testing and CI:

```bash
docker compose -f platform/deploy/docker-compose.test.yml up --build -d
```

All values in the test compose file are isolated test-only credentials. Never reuse them in a shared or production environment.
