## Summary

Describe the reusable core change.

## Why it belongs in FoundationKit

Explain why the behavior is technical, product-independent, and stable enough for a shared package.

## Compatibility

Describe public API, behavior, package, or migration impact. Write `None` when there is no compatibility impact.

## Verification

- [ ] `bash scripts/verify-core-only.sh`
- [ ] `dotnet build FoundationKit.sln --configuration Release`
- [ ] `dotnet test FoundationKit.sln --configuration Release`
- [ ] `bash scripts/pack.sh`
- [ ] Documentation and changelog updated
