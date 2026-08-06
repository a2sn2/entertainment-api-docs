# Contributing

## Start from repository truth

Before changing code, read `README.md`, `docs/ARCHITECTURE.md`, `docs/PACKAGES.md`, `docs/WORKBENCH.md`, the canonical catalog, and the relevant tests. Distinguish implemented behavior from design intent and future recommendations.

## Preserve boundaries

Reusable code belongs under `src/`. Product rules, hosted applications, database providers, connection strings, and migrations do not.

The local Workbench under `samples/` may reference SQL Server because it is an explicit consumer. `scripts/verify-repository.sh` rejects provider references or migration directories inside reusable packages.

## Capability information

When a public implemented capability changes:

1. update code and tests;
2. update `catalog/foundationkit.catalog.json`;
3. regenerate `docs/FEATURES.md`;
4. update `CHANGELOG.md`.

```bash
dotnet run --project tools/FoundationKit.CatalogGenerator
```

Do not edit `docs/FEATURES.md` manually. Do not list planned behavior as implemented.

## Workbench persistence changes

Treat Workbench EF Core migrations as its schema source of truth. Add and review a migration whenever the mapped schema changes. Keep migration files under the Workbench project.

Changes to local persistence must also pass the Dockerized Workbench + SQL Server smoke test in CI.

## Required verification

```bash
bash scripts/verify-repository.sh
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet run --project tools/FoundationKit.CatalogGenerator --configuration Release --no-build -- --check
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/pack.sh
```

Use `./scripts/run-workbench.sh` or `.\scripts\run-workbench.ps1` for a complete local UI and SQL Server check.

## Pull requests

Explain:

- what changed;
- why it belongs in the reusable core, Workbench, catalog, or static demo;
- compatibility and migration impact;
- tests and runtime verification used;
- documentation and catalog updates.

Keep product-specific code outside core packages and avoid unrelated changes.
