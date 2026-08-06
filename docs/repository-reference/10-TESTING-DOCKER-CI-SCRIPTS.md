# Testing, Scripts, Docker, Nginx, and Continuous Integration

This chapter explains how the repository proves correctness at multiple levels and how local/CI execution is automated.

---

# 1. Quality layers

```text
Compiler and warnings-as-errors
        ↓
FoundationKit unit tests
        ↓
Product domain unit tests
        ↓
Architecture dependency tests
        ↓
Postman JSON syntax validation
        ↓
Docker image builds
        ↓
SQL Server migration and health
        ↓
API/Admin/Client/static docs availability
        ↓
End-to-end document workflow smoke test
```

No single test layer proves all concerns. Unit tests are fast but do not prove container routing; smoke tests prove integration but do not isolate every domain edge case.

---

# 2. Global build quality settings

## `platform/Directory.Build.props`

Applies:

- `net8.0`;
- nullable reference types;
- implicit usings;
- `TreatWarningsAsErrors=true`;
- invariant globalization disabled.

Warnings-as-errors prevents warning debt from silently growing. A package warning or nullable regression can fail CI and must be deliberately resolved or narrowly configured.

## `platform/Directory.Packages.props`

Centralizes versions. This makes dependency review and upgrades visible in one file and avoids version drift among projects.

---

# 3. FoundationKit unit tests

## `EntityTests.cs`

### `Entities_with_same_non_default_identifier_are_equal`

- creates a fixed GUID;
- constructs two separate test entities with same ID;
- asserts equality.

Protects identity-based entity semantics.

### `Different_transient_entities_are_not_equal`

- constructs two default-ID entities;
- asserts they are not equal;
- asserts an object equals itself.

Protects against the common bug where all unsaved entities compare equal because their IDs are default.

The nested test entity exposes base construction paths without adding product behavior.

## `ResultTests.cs`

### Success test

Asserts:

- `IsSuccess` true;
- `Error.None`;
- expected value accessible.

### Failure test

- creates typed NotFound error;
- creates failed generic result;
- asserts `IsFailure`;
- asserts exact error record;
- asserts accessing guarded `Value` throws.

Protects impossible-state and caller-discipline rules.

## `ArchitectureRulesTests.cs`

Uses assembly references to enforce boundaries.

Tests include:

- Foundation Domain does not reference Application/Infrastructure/WebApi/Blazor/EF/ASP.NET;
- Foundation Infrastructure does not reference SQL Server/PostgreSQL/SQLite providers or ASP.NET host;
- Product Domain does not reference outer product layers or frameworks;
- Product Application does not reference Infrastructure/API/SQL provider;
- Contracts do not reference Domain/Application/Infrastructure/EF;
- Product Infrastructure does not reference API/Admin/Client;
- Foundation Application does not reference outer adapters/EF.

The helper reads `Assembly.GetReferencedAssemblies()` and checks forbidden names case-insensitively.

Limit: reference-level tests do not detect every conceptual violation inside allowed dependencies. Code review remains necessary.

## `GlobalUsings.cs`

Provides shared test imports such as `Xunit` to all files in the test project.

## Test project file

References core/product assemblies needed by tests, Microsoft.NET.Test.Sdk, xUnit, and Visual Studio runner. Runner assets are private so test tooling does not flow to consumers.

---

# 4. Product domain tests

## `DocumentationDocumentTests.cs`

### `Publish_requires_review_state`

1. create document;
2. add version;
3. call publish without review;
4. expect `InvalidOperationException`.

### `Reviewed_document_can_be_published`

1. create;
2. add version;
3. submit review;
4. publish;
5. assert Published status.

These tests protect the central workflow gate.

Current missing domain test cases include archive/add-version rejection, review without version, duplicate state transitions, published-new-version reset, normalization, and timestamp changes.

---

# 5. Local SQL Server setup script

## `platform/scripts/setup-local-sqlserver.ps1`

### Parameters

- `Server`: defaults to Windows computer name;
- `Database`: defaults `EntertainmentDocs_Dev`;
- `StartApi`: optional switch.

`[CmdletBinding()]` enables advanced script behavior. `$ErrorActionPreference = "Stop"` prevents silent continuation.

### Paths

Uses `$PSScriptRoot` to derive platform root, solution, API project, and Infrastructure project independent of caller's current directory.

### Server validation

Rejects blank server and instructs named-instance syntax.

### Connection string

Uses:

```text
Trusted_Connection=True
TrustServerCertificate=True
MultipleActiveResultSets=True
```

This is local Windows-authentication configuration.

### Environment variables

Sets:

- `ASPNETCORE_ENVIRONMENT=Development`;
- `ConnectionStrings__SqlServer`;
- `ENTERTAINMENTDOCS_SQLSERVER`.

Both runtime and design-time factory can resolve the selected database.

### Four steps

1. restore local tools;
2. restore packages;
3. build Debug solution without second restore;
4. apply EF migrations with explicit project/startup/context.

After every native command, `$LASTEXITCODE` is checked and converted to a terminating error.

### Location safety

`Push-Location` enters platform root; `finally` always `Pop-Location` even on failure.

### Completion

Prints local credentials and either starts API or prints an exact command.

The script does not start Client/Admin unless Visual Studio is configured separately.

---

# 6. Local page launcher

## `open-local-platform.ps1`

Parameter `TimeoutSeconds` is validated from 1 to 300, default 60.

Targets define separate probe/open URLs:

```text
API: probe /health, open /swagger
Client: probe/open 5081
Admin: probe 5082, open /login
```

### `Test-LocalUrl`

Uses `Invoke-WebRequest` with two-second timeout. Treats status 200–499 as process reachable; 401/404 can still indicate a listening service. Exceptions return false.

### Wait loop

- computes deadline;
- probes not-yet-ready targets once per second;
- records readiness in hashtable;
- stops when all ready or timeout.

### Opening

Opens only ready targets through system browser, warns for missing targets, and throws when none became reachable.

Running the script repeatedly opens duplicate tabs; one run per active session is sufficient.

---

# 7. FoundationKit package scripts

## `pack-foundation.ps1`

Parameters:

- configuration default Release;
- output directory default `artifacts/foundation`.

Flow:

1. resolve platform root;
2. create output directory;
3. recursively find `FoundationKit.*.csproj` under core;
4. sort paths for deterministic order;
5. fail if none;
6. run `dotnet pack` for each;
7. check exit code;
8. print output package names and sizes.

## `pack-foundation.sh`

Unix equivalent.

`set -euo pipefail` ensures strict failure. It derives paths from `BASH_SOURCE`, uses `find | sort`, loads projects with `mapfile`, packs each, and prints package filenames.

Both scripts package all current core projects automatically, so adding a matching project includes it without editing a hard-coded list.

---

# 8. Test-stack scripts

## `start-test-stack.sh`

1. derive repository root;
2. select test Compose file;
3. run `docker compose ... up --build -d`;
4. poll gateway `/healthz` up to 60 attempts, two seconds apart;
5. print routes on success;
6. on timeout, print Compose status and logs, exit 1.

It waits for the externally useful gateway, while Compose health dependencies wait for internal services.

## `stop-test-stack.sh`

Runs Compose down with orphan removal. It does not remove volumes by default, preserving test database unless caller uses `--volumes` explicitly.

## `smoke-test.sh`

Strict shell mode and required environment variables:

```text
TEST_ADMIN_EMAIL
TEST_ADMIN_PASSWORD
```

Optional `BASE_URL`, default `http://localhost:8080`.

### Unique test data

Current Unix timestamp produces unique slug/reference, reducing conflict when rerun against preserved DB.

### Availability checks

`check_url` uses curl and prints PASS for:

- gateway;
- API health;
- Client;
- Admin;
- static docs.

### Login

Python safely JSON-encodes credentials from environment. Curl submits login. Python parses access token. Script asserts token non-empty.

### Create

Posts unique reference/slug/title with Bearer token, parses document ID, asserts non-empty.

### Version/review/publish

- POST version body;
- bodyless review POST;
- bodyless publish POST.

`curl --fail` makes non-2xx terminate the strict script.

### Public verification

GET by slug and `grep` expected title. Then prints complete success.

### Security

Token is held in shell variable and not echoed. CI secrets are test-only environment values. Shell tracing must not be enabled around credentials.

---

# 9. Docker build files

## `Dockerfile.api`

Typical multi-stage structure:

1. .NET SDK build stage;
2. copy solution/project files and restore;
3. copy sources;
4. publish API Release to output;
5. ASP.NET runtime stage;
6. copy published files;
7. expose container port;
8. run `EntertainmentDocs.Api.dll`.

Multi-stage build keeps SDK out of runtime image.

## `Dockerfile.blazor`

Reusable build for Admin or Client. Build arguments select:

- project path;
- base path such as `admin` or `client`.

It publishes Blazor static files, then copies them into Nginx. Base-path configuration ensures generated `<base href>` and route assets work behind gateway subpaths.

## `Dockerfile.docs`

Copies root static portal into Nginx image. It must exclude irrelevant repository artifacts through build context/.dockerignore policy where available.

---

# 10. Nginx configuration

## `nginx-spa.conf`

Serves Blazor static files and uses `try_files` fallback to `index.html` so direct routes such as `/documents/x` load the SPA instead of returning Nginx 404.

Static cache headers and compressed file behavior must be compatible with Blazor versioned assets.

## `nginx-gateway.conf`

Single external port 8080 routes:

```text
/healthz -> gateway health
/api/    -> API service
/admin/  -> Admin Nginx
/client/ -> Client Nginx
/docs/   -> static docs Nginx
```

Proxy configuration forwards relevant host/client/scheme headers and maintains route prefixes as expected by each service.

One gateway avoids browser CORS complexity in the test topology because all surfaces share one origin.

---

# 11. Docker Compose files

## `docker-compose.test.yml`

Project name: `entertainment-docs-test`.

### SQL Server service

- image `mcr.microsoft.com/mssql/server:2022-latest`;
- accepts EULA;
- Developer edition;
- isolated test-only SA password;
- host port 14333 -> container 1433;
- persistent named volume;
- health check using available sqlcmd path;
- retries/start period for SQL boot.

### API service

- built from API Dockerfile;
- Testing environment;
- listens on 8080;
- SQL connection points to SQL service;
- test JWT issuer/audience/key;
- test bootstrap admin;
- migrations on startup;
- waits for healthy SQL;
- internal health check.

### Admin and Client

- use reusable Blazor Dockerfile;
- select project and base path;
- wait for healthy API;
- expose Nginx port 80;
- health checks verify index file and Nginx config.

### Docs

Builds static portal Nginx image and verifies index/config.

### Gateway

- Nginx Alpine;
- mounted gateway config;
- host port 8080;
- waits for healthy API/Admin/Client/Docs;
- config health check.

### Volume

Named SQL data volume. CI cleanup removes it; local stop script preserves it unless explicitly removed.

## `docker-compose.yml`

General/local deployment composition separate from test-only topology. Environment values are supplied through `.env`/host configuration. It should not embed real production secrets.

## `.env.example`

Documents required variable names and safe placeholders. Copy to a local ignored `.env`; do not commit populated secret file.

---

# 12. Development container

## `.devcontainer/devcontainer.json`

- .NET 8 Debian-based devcontainer;
- Docker-in-Docker feature;
- GitHub CLI feature;
- forwards port 8080 privately;
- post-create restores solution and marks scripts executable;
- post-attach prints command, does not auto-start blocking stack;
- installs C#, C# Dev Kit, Docker, GitLens;
- runs as `vscode` user.

This supports Codespaces/VS Code. It does not replace local Visual Studio/SQL Server setup.

---

# 13. VS Code tasks

## `.vscode/tasks.json`

Defines named editor tasks for common scripts/builds. Tasks should call repository scripts rather than duplicate long commands, keeping one operational source of truth.

Visual Studio multi-start configuration is user/solution setting and is not represented by these tasks.

---

# 14. GitHub Actions: platform workflow

## Triggers

- push to main when platform/static/development-container/workflow paths change;
- pull requests affecting those paths;
- manual dispatch.

Path filters avoid expensive full-stack runs for unrelated docs-only changes outside listed paths.

## Permissions

Read-only contents.

## Concurrency

One run per ref group; newer run cancels in-progress older run.

## Job `build-test`

Runs Ubuntu with working directory `platform`.

Steps:

1. checkout;
2. setup .NET 8 with dependency cache;
3. validate both Postman JSON files using Python JSON parser;
4. restore solution;
5. Release build without restore;
6. test solution without build and output TRX;
7. always upload available TRX results.

## Job `full-stack`

Depends on build-test.

Environment supplies test admin credentials.

Steps:

1. checkout;
2. mark scripts executable;
3. validate Compose config;
4. build/start detached stack;
5. poll gateway up to 120 attempts;
6. run smoke test;
7. print last 300 log lines on failure;
8. always stop stack, remove volumes/orphans.

The cleanup step prevents hosted-runner state leakage.

---

# 15. GitHub Actions: FoundationKit workflow

Triggers when core, core tests, pack scripts, central packages, or workflow change.

Steps:

1. checkout;
2. setup .NET 8/cache;
3. restore entire solution;
4. Release build;
5. run FoundationKit test project;
6. pack all core projects through Bash script;
7. upload `.nupkg` and `.snupkg`, failing when none exist.

Building entire solution before core tests verifies current product compatibility with the core change.

The artifact is internal workflow output; packages are not automatically published to NuGet.org.

---

# 16. Test data and credentials

Development:

```text
admin@local.test / LocalAdmin!2026
```

Testing:

```text
admin@test.local / TestAdmin!2026
```

SQL test password and JWT key in Compose are isolated values.

Rules:

- never reuse them in staging/production;
- never interpret public test values as secure secrets;
- never include real customer/player data in smoke tests;
- ensure logs do not print tokens/passwords;
- use unique test identifiers;
- destroy CI volumes.

---

# 17. What each quality gate proves

| Gate | Proves | Does not prove |
|---|---|---|
| Compile | type correctness/build graph | runtime workflow |
| Warnings-as-errors | no accepted compiler warnings | business correctness |
| Unit tests | selected isolated rules | database/container behavior |
| Architecture tests | assembly dependency restrictions | all conceptual coupling |
| Postman JSON parse | valid JSON structure | request correctness |
| Docker build | build files and publish path | service interaction |
| Health checks | process/config/database reachability | full business behavior |
| Smoke test | main happy-path integration | all errors, load, security |
| Package creation | NuGet artifacts can be produced | compatibility with every future product |

---

# 18. Recommended future test expansion

- Application handler tests with fake repository/user/clock;
- API integration tests for every 401/403 role combination;
- validation and ProblemDetails contract assertions;
- SQL unique race/concurrency tests;
- role-replacement atomicity test;
- migration rollback/idempotent script validation;
- Newman execution of Postman collection;
- Blazor component tests for state/authorization rendering;
- accessibility tests;
- static portal unit/DOM tests;
- load tests;
- security scans and dependency review;
- backup/restore and disaster-recovery drills.

These are recommended gaps, not current passing claims.
