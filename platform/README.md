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

Configure these projects as multiple startup projects using their `http` profiles:

- `EntertainmentDocs.Api` — Start
- `EntertainmentDocs.Client` — Start
- `EntertainmentDocs.Admin` — Start

The launch profiles intentionally do not open debugger-managed browser windows. This prevents Visual Studio from terminating the complete multi-project debug session when a browser process or tab closes.

After the three services report that they are listening, open all local pages with:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\open-local-platform.ps1
```

Local URLs:

- API Swagger: `http://localhost:5080/swagger`
- Client: `http://localhost:5081`
- Admin: `http://localhost:5082/login`

See `docs/LOCAL-SQLSERVER.md` for the complete setup and SSMS verification steps.

## Integration stack

Docker Compose remains available for repeatable SQL Server integration testing and CI:

```bash
docker compose -f platform/deploy/docker-compose.test.yml up --build -d
```

All values in the test compose file are isolated test-only credentials. Never reuse them in a shared or production environment.
