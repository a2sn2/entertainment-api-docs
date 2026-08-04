# Production Readiness

The repository now contains a production-oriented foundation, not a claim of production certification.

Before a real production release, complete:

- create and review EF Core migrations;
- use managed secrets, never repository settings;
- choose a managed PostgreSQL service and backup policy;
- deploy API, Client, and Admin separately;
- configure TLS, CORS, domain names, and WAF;
- add structured logs, tracing, metrics, and alerting;
- add refresh-token rotation or an external identity provider;
- add e-mail verification, password reset, MFA, and account lifecycle;
- add audit interception and immutable retention policy;
- add integration, authorization, security, load, and recovery tests;
- run threat modeling, SAST, dependency scanning, and penetration testing;
- define RPO/RTO, incident response, rollback, and disaster recovery.

GitHub Pages cannot run the API or PostgreSQL. It remains a static preview channel only.
