# EntertainmentDocs Reference Consumer

EntertainmentDocs is the first validated consumer of FoundationKit. It is a real working product used to prove the reusable packages, dependency rules, database integration, API behavior, frontend composition, testing, and deployment workflow.

## Source location

The current validated implementation remains under [`platform/`](../../platform/):

```text
platform/
├── src/                         # Product Domain, Application, Contracts, Infrastructure, API
├── apps/                        # Admin, Client, and shared UI
├── tests/                       # Product and architecture tests
├── postman/                     # HTTP contracts and local environment
├── deploy/                      # SQL Server, Docker, Nginx
├── scripts/                     # Setup, smoke tests, packaging
└── EntertainmentDocs.sln       # Complete reference-product solution
```

## Why it remains in this repository

The product currently serves as a compatibility and integration proving ground for FoundationKit. It is not part of the core package surface. A future extraction into a separate repository should happen only after:

1. versioned FoundationKit packages are published to a package source;
2. the consumer no longer needs project references;
3. CI proves package consumption rather than source coupling;
4. documentation and deployment paths are updated in the same change.

## Implemented behavior

- ASP.NET Core Identity and JWT authentication;
- Administrator, Editor, Reviewer, and Reader roles;
- document draft, review, publish, and archive domain behavior;
- SQL Server with EF Core migrations as schema source of truth;
- Blazor WebAssembly Admin and Client applications;
- Docker Compose full-stack test topology;
- end-to-end create → version → review → publish → public read workflow.

## Important boundary

EntertainmentDocs business rules, API routes, transport contracts, SQL Server configuration, migrations, roles, and UI remain product-owned. They must not be moved into FoundationKit merely for convenience.
