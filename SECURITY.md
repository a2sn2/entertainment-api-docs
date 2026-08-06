# Security Policy

## Supported scope

FoundationKit is pre-1.0. Security fixes target the current `main` branch and the current package version unless a separate maintenance commitment is published.

## Reporting a vulnerability

Do not open a public GitHub issue for a vulnerability, credential, customer record, internal document, or confidential architecture detail. Contact the repository owner through the GitHub profile and agree on a private channel before sharing sensitive material:

<https://github.com/a2sn2>

Include the affected package or Workbench component, impact, reproduction conditions, and a proposed mitigation when available.

## Public Workbench contact links

The Workbench and Pages demo generate GitHub issue links. GitHub issues are public. The UI warns users not to include passwords, personal data, customer information, financial secrets, or confidential business details.

The static Pages demo does not transmit or persist answers automatically. The local Workbench stores submitted briefs in the user's local SQL Server database.

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
