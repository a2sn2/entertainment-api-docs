# Future Product Templates

Future products should normally live in separate repositories and consume versioned FoundationKit packages.

## Recommended product shape

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

## Bootstrap sequence

1. Define the business problem and vocabulary.
2. Model the first aggregate or capability in Product.Domain.
3. Add one command or query for each use case.
4. Define product-owned transport contracts.
5. Select the database provider in Product.Infrastructure.
6. Map thin API endpoints.
7. Add typed frontend clients and explicit UI state.
8. Add migrations, tests, Postman requests, Docker, and CI.
9. Reference only the FoundationKit packages actually needed.

## Required gates

- Release build with warnings as errors;
- domain and application tests;
- architecture dependency tests;
- migration verification;
- authorization tests;
- end-to-end business workflow;
- production secrets outside source control;
- documentation updated with every behavior change.

No generic generator can decide the product's domain, permissions, workflows, or operational risks. The template establishes boundaries; product analysis still determines behavior.
