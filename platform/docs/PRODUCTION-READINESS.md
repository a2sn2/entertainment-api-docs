# Production Readiness

The repository contains a production-oriented foundation, not a claim of production certification.

## Database baseline

- Microsoft SQL Server is the supported relational database provider.
- EF Core migrations are the source of truth for schema evolution.
- Local Development uses Windows Authentication and `EntertainmentDocs_Dev`.
- Automated integration tests use an isolated `EntertainmentDocs_Test` database.
- Production must use a dedicated `EntertainmentDocs_Prod` database, managed credentials or workload identity, encrypted connections, backups, and monitored recovery procedures.

## Required before production release

- review generated EF Core migrations and produce an idempotent deployment script;
- use managed secrets, never repository settings;
- choose a managed SQL Server or Azure SQL service and define backup/retention policy;
- disable automatic application-start migrations unless the deployment policy explicitly approves them;
- deploy API, Client, and Admin separately;
- configure TLS, CORS, domain names, reverse proxy, and WAF;
- add structured logs, tracing, metrics, and alerting;
- add refresh-token rotation or an external identity provider;
- add e-mail verification, password reset, MFA, and account lifecycle;
- add audit interception and immutable retention policy;
- add integration, authorization, security, load, and recovery tests;
- run threat modeling, SAST, dependency scanning, and penetration testing;
- define RPO/RTO, incident response, rollback, and disaster recovery.

GitHub Pages cannot run the API or SQL Server. It remains a static preview channel only.
