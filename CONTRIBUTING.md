# Contributing

## Start from repository truth

Before changing code, read `README.md`, `docs/ARCHITECTURE.md`, `docs/PACKAGES.md`, `docs/WORKBENCH.md`, the canonical catalogs, and the relevant tests. Distinguish implemented behavior from design intent and future recommendations.

For Windows local execution, use `docs/LOCAL-RUN-WINDOWS-AR.md` as the canonical first-run sequence.

## Preserve boundaries

Reusable code belongs under `src/`. Product rules, hosted applications, database providers, connection strings, and migrations do not.

The Workbench under `samples/` and Athar under `examples/Athar/` may reference SQL Server because they are explicit consumers. `scripts/verify-repository.sh` rejects provider references or migration directories inside reusable packages.

## Keep the tracked repository clean

Do not commit local/generated/sensitive artifacts such as:

- `bin/`, `obj/`, `artifacts/`, `TestResults/`, coverage output, logs, or NuGet packages;
- `.local/`, `.env*`, User Secrets, IDE state, or local databases;
- `.bak`, `.pfx`, `.p12`, `.key`, or other local backup/private-key material.

`.gitignore` is the first line of defense. `scripts/repository-hygiene.py` independently checks Git's tracked file set so an accidentally force-added artifact still fails CI.

## Capability information

When a public implemented capability changes:

1. update code and tests;
2. update `catalog/foundationkit.catalog.json` when the human implemented surface changes;
3. update `src/FoundationKit.Application/Capabilities/CapabilityModel.cs` when composition metadata changes;
4. regenerate the generated catalogs/docs;
5. update `CHANGELOG.md`.

```bash
dotnet run --project tools/FoundationKit.CatalogGenerator
```

Do not edit `docs/FEATURES.md` or `catalog/foundationkit.capabilities.json` manually. Do not list planned behavior as implemented.

## Persistence changes

EF Core migrations are the schema source of truth for each consuming application.

- Workbench migrations stay under `samples/FoundationKit.Workbench/Infrastructure/Migrations/`.
- Athar migrations stay under `examples/Athar/Athar.Infrastructure/Migrations/`.

Add and review a migration whenever the mapped schema changes. Do not move product migrations into reusable packages.

Persistence changes must pass the Dockerized SQL Server integration/smoke flow in CI.

## Required verification

On Linux/CI-compatible environments:

```bash
python3 scripts/repository-hygiene.py
bash scripts/verify-repository.sh
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet run --project tools/FoundationKit.CatalogGenerator --configuration Release --no-build -- --check
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/pack.sh
```

On Windows, start with:

```powershell
.\foundationkit.ps1 doctor
.\foundationkit.ps1 verify
```

The root manager is the preferred Windows entry point. Lower-level scripts remain available for focused troubleshooting and CI parity.

## Pull requests

Explain:

- what changed;
- why it belongs in the reusable core, Workbench, Athar, catalog, tooling, or documentation;
- compatibility and migration impact;
- tests and runtime verification used;
- documentation and catalog updates;
- any deployment/organizational decision deliberately left outside the repository.

Keep product-specific code outside core packages and avoid unrelated changes.
