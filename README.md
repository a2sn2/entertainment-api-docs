# Entertainment Services API Documentation

A multi-page API documentation portal plus a production-oriented platform foundation.

- **Document reference:** API-ENT-DOC-001
- **Version:** 1.0
- **Environment:** Test
- **Classification:** Internal Integration Use

## Live documentation

https://a2sn2.github.io/entertainment-api-docs/

## Current portal architecture

The static portal follows a lightweight Clean Architecture / DDD-aligned JavaScript structure:

```text
src/
├── domain/
├── application/
├── infrastructure/
└── presentation/
```

## Production platform foundation

A separate .NET platform scaffold now exists under `platform/`:

```text
platform/
├── src/        # Domain, Application, Infrastructure, API
├── apps/       # Client and Admin applications
├── tests/
├── deploy/
└── docs/
```

It introduces:

- PostgreSQL persistence;
- ASP.NET Core API;
- business logic isolated in Domain and Application layers;
- ASP.NET Core Identity and JWT authentication;
- Administrator, Editor, Reviewer, and Reader roles;
- separate Client and Admin applications;
- Docker Compose and CI;
- versioned documentation workflow.

See `platform/docs/ARCHITECTURE.md` and `platform/docs/PRODUCTION-READINESS.md`.

## Security

The repository is public. Never add credentials, tokens, production URLs, personal data, real player identifiers, or internal secrets.
