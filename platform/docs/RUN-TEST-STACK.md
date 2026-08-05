# Run the Complete Test Platform

The repository includes a repeatable full-stack test environment. It runs Microsoft SQL Server, the ASP.NET Core API, Identity/RBAC, the Admin WebAssembly app, the Client WebAssembly app, the static documentation portal, and a single-origin Nginx gateway.

## Local Docker

Requirements:

- Docker Desktop or Docker Engine with Compose v2.
- Git.
- `curl` and Python 3 for the smoke-test script.

Start everything:

```bash
chmod +x platform/scripts/*.sh
platform/scripts/start-test-stack.sh
```

Open:

- Client: `http://localhost:8080/client/`
- Admin: `http://localhost:8080/admin/`
- Documentation: `http://localhost:8080/docs/`
- API health: `http://localhost:8080/api/health`
- SQL Server test instance: `localhost,14333`
- Test database: `EntertainmentDocs_Test`

Run the end-to-end test:

```bash
export TEST_ADMIN_EMAIL=admin@test.local
export TEST_ADMIN_PASSWORD='TestAdmin!2026'
platform/scripts/smoke-test.sh
```

Stop the services:

```bash
platform/scripts/stop-test-stack.sh
```

Remove the isolated test database volume as well:

```bash
docker compose -f platform/deploy/docker-compose.test.yml down --volumes --remove-orphans
```

## What the smoke test verifies

1. Gateway health.
2. SQL Server-backed API health.
3. EF Core migration application.
4. Client, Admin, and static documentation availability.
5. Bootstrap administrator login.
6. JWT issuance.
7. Role-protected document creation.
8. Version creation.
9. Review submission.
10. Publishing authorization.
11. Public retrieval of the published document.

## GitHub Actions

The same Compose stack is built and executed by `Platform CI and Full-Stack Test`. CI creates an isolated SQL Server database, applies the committed migrations, executes the complete workflow, and removes the database volume afterward.

## Security boundary

All credentials in `docker-compose.test.yml` are explicitly test-only values for an isolated local or CI environment. Never reuse them in a shared, staging, or production environment.

GitHub Pages continues to host only the static documentation portal. The complete dynamic platform runs locally, in Docker, or in GitHub Actions because GitHub Pages cannot host an API or SQL Server database.
