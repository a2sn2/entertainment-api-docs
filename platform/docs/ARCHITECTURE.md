# Platform Architecture

## Decision

Use a **modular monolith** with Clean Architecture and DDD boundaries. This gives strong separation today without the operational cost of microservices. Bounded contexts can later be extracted behind the same contracts.

## Bounded contexts

1. **Identity & Access** — users, roles, authentication, authorization.
2. **Documentation Catalog** — documents, versions, references, slugs.
3. **Publishing Workflow** — draft, review, publish, archive.
4. **Audit & Operations** — audit trail, health, observability, support.
5. **Integration Registry** — future API environments, credentials metadata, schemas, and provider links.

## Dependency rule

`Domain <- Application <- Infrastructure <- API / Admin / Client`

The Domain project has no dependency on databases, HTTP, Identity, or UI frameworks.

## Runtime topology

```text
Client Web ─────┐
Admin Web ──────┼── HTTPS ──> ASP.NET Core API ──> Microsoft SQL Server
External Tools ─┘                    │
                                    ├── Identity/RBAC
                                    ├── Audit
                                    └── Provider adapters (future)
```

Microsoft SQL Server is the platform database provider. EF Core migrations own schema evolution. Local Development uses Windows Authentication; Testing uses an isolated SQL Server container; Production must use managed credentials or workload identity through a secret manager.

GitHub Pages continues to host the public static documentation preview. Dynamic admin, identity, and database features require deployment of the API and web applications to an application host.

## Extension pattern

A new feature should be added as:
1. Domain model or policy.
2. Application use case and port.
3. Infrastructure adapter.
4. API contract.
5. Admin/client presentation.
6. Tests and migration.

No layer may bypass the Application boundary to talk directly to the database.
