# Entertainment Services API Documentation

A multi-page, offline-capable developer portal for the Entertainment Services API.

- **Document reference:** API-ENT-DOC-001
- **Version:** 1.0
- **Environment:** Test
- **Classification:** Internal Integration Use

## Architecture

The repository follows a lightweight Clean Architecture / DDD-aligned structure without a build step:

```text
src/
├── domain/             # API contracts, purchase flow, identifiers, quality evidence
├── application/        # Search, request building, validation, decision and filtering use cases
├── infrastructure/     # Static repositories, browser preferences and caching adapters
└── presentation/       # Shared shell, components and page renderers
```

## Pages

- Overview dashboard
- Quick Start
- Interactive Purchase Flow
- API Reference
- Offline Playground
- Error Decision Assistant
- Test Coverage
- Governance
- Known Limitations
- Open Questions

## Run locally

Serve the repository over HTTP so native ES modules and the service worker work correctly:

```bash
python -m http.server 8080
```

Then open `http://localhost:8080/`.

## Security

This repository is public. Never add credentials, access tokens, production URLs, personal data, real player identifiers, or internal secrets.
