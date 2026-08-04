# Entertainment Documentation Platform

Production-oriented foundation for evolving the static API documentation into a managed platform.

## Applications

- `EntertainmentDocs.Api` — backend API and authorization boundary.
- `EntertainmentDocs.Client` — read-only client experience.
- `EntertainmentDocs.Admin` — administration experience.

## Core layers

- `Domain` — business invariants and aggregates.
- `Application` — use cases and ports.
- `Infrastructure` — PostgreSQL, Identity, JWT, repositories.
- `API` — HTTP contracts and policies.

## Local infrastructure

```bash
cd platform/deploy
cp .env.example .env
docker compose up --build
```

Do not use example secrets in any shared environment.
