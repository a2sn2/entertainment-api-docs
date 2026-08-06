# FoundationKit Core

This directory is the navigation boundary for the reusable core. The validated source currently remains under [`platform/core/`](../platform/core/) so the existing solution, package scripts, Docker build context, and CI stay stable during the repository identity change.

## Canonical packages

- [`FoundationKit.Domain`](../platform/core/FoundationKit.Domain/)
- [`FoundationKit.Application`](../platform/core/FoundationKit.Application/)
- [`FoundationKit.Infrastructure`](../platform/core/FoundationKit.Infrastructure/)
- [`FoundationKit.WebApi`](../platform/core/FoundationKit.WebApi/)
- [`FoundationKit.Blazor`](../platform/core/FoundationKit.Blazor/)

Use the root [`FoundationKit.sln`](../FoundationKit.sln) to build the five package projects without loading the EntertainmentDocs product projects. The existing `FoundationKit.Tests` project also performs consumer architecture checks and therefore remains outside the core-only solution.

## Ownership rule

FoundationKit may contain reusable technical behavior only. It must not own:

- product entities, workflows, terminology, or policies;
- product request/response contracts or HTTP routes;
- a database provider or product migration;
- product pages, branding, or navigation;
- product-specific user roles;
- behavior extracted only because two files look similar.

Extraction requires proven reuse and a stable contract.

## Consumption direction

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application + Product.Domain
Product.Infrastructure  → FoundationKit.Infrastructure + Product.Application
Product.Api             → FoundationKit.WebApi + Product.Application/Infrastructure
Product.Blazor          → FoundationKit.Blazor + Product.Contracts
```

FoundationKit never references a consuming product.

## Versioning

Current package version: `0.1.0`.

Increase a package version only when a reusable public contract changes. Product-only changes do not require a FoundationKit version change.
