# Security Policy

## Supported scope

FoundationKit is pre-1.0. Security fixes target the current `main` branch and current package version unless a separate maintenance commitment is published.

## Reporting a vulnerability

Do not open a public GitHub issue for a vulnerability, credential, customer record, internal document, or confidential architecture detail. Contact the repository owner through the GitHub profile and agree on a private channel before sharing sensitive material:

<https://github.com/a2sn2>

Include the affected package or Workbench component, impact, reproduction conditions, and a proposed mitigation when available.

## Workbench contact links

The Workbench can generate GitHub issue links. GitHub issues are public. Do not include passwords, personal data, customer information, financial secrets, or confidential business details.

The GitHub Pages deployment runs the Blazor WebAssembly client in demo mode. It cannot execute the ASP.NET Core API or connect to SQL Server, and database submission is disabled.

The local API-hosted Workbench stores submitted briefs in the developer's configured SQL Server database.

## API testing

Swagger and the Postman collection are development tools. Do not expose an unauthenticated Workbench API to untrusted networks. The current sample does not implement identity, authorization, or rate limiting.

Do not place production tokens, secrets, or confidential payloads in committed Postman collections or public screenshots.

## Local development credentials

The Workbench helper scripts generate an ephemeral SQL Server password for the current shell and Docker Compose invocation. Do not reuse that topology or credential strategy in production.

Never commit:

- real connection strings or passwords;
- `.env` files;
- access tokens;
- production certificates;
- database backups;
- customer or employee data.

## Production boundary

FoundationKit does not provide a complete security architecture. Consuming products own identity, authorization, rate limiting, TLS, secret management, network policy, audit, data protection, backup, incident response, dependency review, and secure deployment.
