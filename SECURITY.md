# Security Policy

## Supported version

FoundationKit is currently pre-1.0. Security fixes are applied to the latest code on `main`.

## Reporting a vulnerability

Do not publish sensitive vulnerability details in a public issue. Use GitHub's private vulnerability reporting feature for this repository when available, or contact the repository owner privately through the GitHub profile.

Include:

- affected package and version;
- impact;
- reproduction steps;
- suggested mitigation when known.

## Scope

FoundationKit provides reusable building blocks. A consuming application remains responsible for:

- authentication and authorization;
- secret storage and key rotation;
- TLS and reverse-proxy configuration;
- CORS and rate limiting;
- database security, migrations, and backups;
- logging, monitoring, and incident response;
- dependency and container scanning;
- product-specific validation and threat modeling.

The in-process domain-event dispatcher is not a durable security or audit mechanism.
