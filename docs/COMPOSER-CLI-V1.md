# FoundationKit Composer CLI v1

The Composer is the first executable developer-facing layer over the FoundationKit Capability Model.

Its v1 responsibility is deliberately narrow: **list, validate, and explain compositions before project generation exists**.

This prevents the future `foundationkit new` command from becoming a collection of hidden hard-coded templates.

## Commands

From the repository root:

```bash
dotnet run --project tools/FoundationKit.Composer -- capabilities
```

Lists every capability with kind, maturity, category, and direct dependencies.

```bash
dotnet run --project tools/FoundationKit.Composer -- profiles
```

Lists the current composition profiles.

```bash
dotnet run --project tools/FoundationKit.Composer -- validate docs/examples/foundationkit.project.minimal.json
```

Parses the manifest strictly, validates profile/capability/provider choices, resolves transitive dependencies, and reports maturity warnings.

```bash
dotnet run --project tools/FoundationKit.Composer -- validate docs/examples/foundationkit.project.minimal.json --require-stable
```

Uses the same validation but returns a non-zero exit code if any resolved capability is not `Stable`. This is intended for future generation/release automation that must fail closed.

```bash
dotnet run --project tools/FoundationKit.Composer -- explain docs/examples/foundationkit.project.example.json
```

Prints the dependency-first resolved composition and why each item is present, for example:

```text
authorization [Optional/ReferenceOnly] <- required-by:approvals
kernel [Kernel/Stable] <- profile:enterprise, required-by:web-api
```

Exact output may evolve; capability IDs and dependency semantics are the contract.

## Manifest v1

The JSON shape is documented by:

`catalog/foundationkit.project.schema.json`

Example:

```json
{
  "schemaVersion": 1,
  "name": "MySystem",
  "profile": "enterprise",
  "includeCapabilities": ["documents", "search"],
  "excludeCapabilities": ["localization"],
  "providers": ["provider-sqlserver"]
}
```

### Strictness

The parser rejects:

- unsupported schema versions;
- unknown JSON properties;
- missing name/profile;
- unsafe project names;
- duplicate capability IDs within a list;
- the same capability in include and exclude lists;
- unknown capabilities/providers;
- provider IDs placed in capability include/exclude lists;
- non-provider IDs placed in `providers`;
- tooling IDs selected as runtime capabilities;
- exclusions that break required dependency closure;
- dependency cycles.

## Maturity behavior

A valid manifest is not automatically generatable.

`validate` distinguishes **structural validity** from **capability maturity**. Planned, reference-only, and preview capabilities are reported as warnings. `--require-stable` turns those warnings into a failing readiness gate.

This is intentional: FoundationKit must never generate a project and imply a capability exists merely because its name appears in the roadmap/catalog.

## Security considerations

The Composer:

- never executes code from the manifest;
- does not accept script hooks in v1;
- does not print the raw manifest contents during `validate` or `explain`;
- treats providers as catalog identities, not arbitrary package names or shell commands;
- has no network/package-install behavior in v1.

Future generation/provider installation must preserve these boundaries and add explicit supply-chain controls.

## Next step

After v1 is verified, the Composer can grow toward:

```text
foundationkit new
  -> choose profile
  -> choose capabilities
  -> resolve dependencies
  -> choose providers
  -> show maturity/security warnings
  -> produce deterministic project plan
  -> generate only supported templates
  -> build/test generated result
```

Generation should consume the same compiled capability graph and manifest parser rather than introducing a parallel model.
