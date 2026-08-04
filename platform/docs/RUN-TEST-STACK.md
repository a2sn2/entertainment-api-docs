# Run the Complete Test Platform

The repository includes a repeatable full-stack test environment. It runs PostgreSQL, the ASP.NET Core API, Identity/RBAC, the Admin WebAssembly app, the Client WebAssembly app, the static documentation portal, and a single-origin Nginx gateway.

## One-click GitHub Codespaces

1. Open the repository on GitHub.
2. Select **Code → Codespaces → Create codespace on main**.
3. Wait for the container setup and Docker builds to complete.
4. Open the forwarded port named **Entertainment Docs Platform**.

The Codespace runs `platform/scripts/start-test-stack.sh` automatically.

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
- PostgreSQL: `localhost:5432`

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

Remove the test database volume as well:

```bash
docker compose -f platform/deploy/docker-compose.test.yml down --volumes --remove-orphans
```

## What the smoke test verifies

1. Gateway health.
2. PostgreSQL-backed API health.
3. Client, Admin, and static documentation availability.
4. Bootstrap administrator login.
5. JWT issuance.
6. Role-protected document creation.
7. Version creation.
8. Review submission.
9. Publishing authorization.
10. Public retrieval of the published document.

## Security boundary

All credentials in `docker-compose.test.yml` are explicitly test-only values for an isolated local or CI environment. Never reuse them in a shared, staging, or production environment.

GitHub Pages continues to host only the static documentation portal. The complete dynamic platform runs in Codespaces, local Docker, or GitHub Actions because GitHub Pages cannot host an API or PostgreSQL database.
