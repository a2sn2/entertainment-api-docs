# Starting a New Product with FoundationKit

## 1. Create product projects

Use product-specific names and keep the same dependency direction:

```text
src/
├── Product.Domain
├── Product.Application
├── Product.Contracts
├── Product.Infrastructure
└── Product.Api

apps/
├── Product.Admin
├── Product.Client
└── Product.Ui

tests/
├── Product.Domain.Tests
├── Product.Application.Tests
├── Product.IntegrationTests
└── Product.ArchitectureTests
```

## 2. Reference only required FoundationKit packages

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application
Product.Infrastructure  → FoundationKit.Infrastructure
Product.Api             → FoundationKit.WebApi
Product.Admin/Client    → FoundationKit.Blazor
```

Do not reference Infrastructure from Domain or Application. Do not reference Domain entities from Blazor.

## 3. Add one business capability

For each capability:

1. model the aggregate, entity, or value object in Domain;
2. define one command or query for each use case;
3. implement a dedicated handler;
4. add a repository port only when persistence is required;
5. extend the generic repository only with business-language queries;
6. define transport contracts separately;
7. map a thin API endpoint;
8. add a typed Blazor client and feature state;
9. add Postman requests and automated tests.

## 4. Database provider belongs to the product

FoundationKit does not choose SQL Server or another provider. Configure the provider in Product.Infrastructure:

```csharp
services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(connectionString));
```

A different product may use PostgreSQL or SQLite without changing FoundationKit.

## 5. API behavior

Use FoundationKit result mapping so every endpoint returns consistent RFC 7807 errors:

```text
Validation    → 400
Unauthorized  → 401
Forbidden     → 403
Not Found     → 404
Conflict      → 409
Business Rule → 422
Failure       → 500
```

The API remains the security boundary. Frontend role visibility is only a user-experience concern.

## 6. Frontend behavior

Razor pages render and collect input. Typed API clients own HTTP details. Feature state owns loading, values, and failures. Shared visual controls belong in Product.Ui; transport behavior belongs in FoundationKit.Blazor.

## 7. Required quality gates

Before publishing:

- warnings as errors;
- Release build;
- domain and application tests;
- architecture dependency tests;
- database migration verification;
- Postman collection validation and execution;
- API authorization tests;
- Admin and Client builds;
- end-to-end business workflow;
- production secrets and allowed origins configured outside source control.

## 8. Promote FoundationKit versions deliberately

FoundationKit starts at `0.1.0`. Increase its version only when a reusable contract changes. Do not move product-specific behavior into FoundationKit merely because two files look similar; extract behavior only after it is proven reusable.
