# Contributing

## Before changing code

1. Confirm that the behavior is reusable and product-independent.
2. Identify the owning package and preserve dependency direction.
3. Avoid adding a database provider, product contract, hosted application, or deployment-specific policy.
4. Update tests and documentation with the same change.

## Local verification

```bash
./scripts/verify-core-only.sh
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
./scripts/pack.sh
```

All warnings are treated as errors.

## Public API changes

For every public contract change:

- add or update tests;
- update the relevant package documentation;
- update `CHANGELOG.md`;
- explain compatibility impact in the pull request.

## Pull requests

Keep pull requests focused. The description should explain:

- what changed;
- why it belongs in the reusable core;
- compatibility impact;
- checks executed.

Do not commit generated `bin`, `obj`, package, test-result, or coverage artifacts.
