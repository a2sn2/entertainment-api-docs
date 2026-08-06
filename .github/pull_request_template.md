## Summary

Describe the reusable core, Workbench, catalog, or static-demo change.

## Boundary impact

Explain whether the change affects reusable package APIs, the local SQL Server Workbench, the static Pages demo, or generated documentation.

## Compatibility

Describe public API, behavior, package, migration, or repository-layout impact. Write `None` when there is no compatibility impact.

## Verification

- [ ] `bash scripts/verify-repository.sh`
- [ ] `dotnet run --project tools/FoundationKit.CatalogGenerator -- --check`
- [ ] `dotnet build FoundationKit.sln --configuration Release`
- [ ] `dotnet test FoundationKit.sln --configuration Release`
- [ ] `bash scripts/pack.sh`
- [ ] Workbench + SQL Server smoke test passed when persistence changed
- [ ] Catalog, generated documentation, and changelog updated when behavior changed
