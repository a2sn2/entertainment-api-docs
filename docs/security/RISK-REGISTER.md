# FoundationKit — Repository Security Risk Register

This is a repository-level engineering risk register. It does **not** replace an organizational enterprise risk register or legal/compliance assessment.

## Scoring rule

No numeric likelihood/impact thresholds are invented here. Severity follows the confirmed repository audit until organizational risk appetite is approved.

## Active risks

| Risk ID | Scenario | Assets | Existing controls | Current rating | Treatment | Evidence target |
|---|---|---|---|---|---|---|
| R-FK-001 | A change reaches `main` without independent approval | Source, CI, releases | PRs, CI | High | Add CODEOWNERS/evidence template; enable protected-branch review externally | GitHub branch-rule export + reviewed PR |
| R-FK-002 | Malicious/vulnerable dependency or secret enters supply chain | Source, packages, images | Central versions, `.gitignore`, build/test | High | NuGet audit, secret scan, SBOM/inventory, action pinning, automated dependency update monitoring | CI security job + Dependabot PR evidence |
| R-FK-003 | Administrator approves own business object | Initiative decisions/audit | Role authorization | High | Domain maker-checker invariant | Unit + E2E negative test |
| R-FK-004 | Public production surface exposes Swagger or implementation details | API metadata | HTTPS/HSTS baseline | High | Environment-restricted Swagger, minimal readiness | Production-mode test |
| R-FK-005 | Authentication abuse succeeds or harms availability | Accounts/sessions | Identity lockout, cookies | High | Partitioned limits, complete identity lifecycle, MFA capability | Auth security tests |
| R-FK-006 | Runtime account can alter schema or uses excessive DB privilege | SQL schema/data | EF Core, migrations | High | Separate migration/runtime identity in production; startup migration disabled by default | Production template + startup tests |
| R-FK-007 | Database/PII travels without validated transport encryption | Credentials/data | TLS at web edge | High | Production SQL encryption/certificate validation gate | Configuration validation |
| R-FK-008 | Backup exists but cannot be restored during incident | SQL data, audit, identity | SQL backup with CHECKSUM | Critical for production | Isolated restore drill and documented recovery | Restore workflow evidence |
| R-FK-009 | Secrets leak via local files/terminal/logs | Admin/SQL credentials | `.gitignore`, generated values | High | Stop routine printing, restrictive local storage guidance, secret scanning, mask ephemeral CI credentials | Script tests + scan + workflow logs |
| R-FK-010 | Audit evidence is changed or lacks enough context | Decisions/investigation evidence | `AuditEntries`, correlation IDs | High | Structured audit enrichment + external append-only sink requirement | DB migration/tests + environment evidence |
| R-FK-011 | Public Quick Tunnel exposes development data/config | Local Athar/PII | Warning text, temporary URL | High if public | Refuse production env, explicit demo-only checks, no-real-data rule | Launcher tests/docs |
| R-FK-012 | Container compromise has unnecessary privilege | Runtime/container host | Multi-stage image | High | Non-root runtime, no-new-privileges, cap drop where compatible | Container smoke test |
| R-FK-013 | PII retained/transferred without approved lifecycle | User/account/audit/backup data | Limited DTO exposure | High | PII inventory, decision register, deletion/retention design | Privacy evidence record |
| R-FK-014 | Security event occurs without detection or response evidence | Accounts/API/DB | App/CI logs | High | Security event catalog, central sink/alerts external gate, incident runbook | Observability evidence |
| R-FK-015 | Automatic startup migration causes destructive/unreviewed production change | SQL schema/data | EF migrations | High | Config-gated migration; production default off; controlled migration runbook | Production-mode startup test |
| R-FK-016 | Upstream runtime/base-image vulnerability has no vendor-fixed package yet | Containers/runtime | Trivy image scan, image rebuilds, dependency monitoring | High | Block fixable HIGH/CRITICAL findings, retain complete SARIF for unfixed findings, monitor vendor updates, reassess base image when a fix or safer supported alternative exists | Trivy blocking gate + complete SARIF + dependency update PRs |

## Acceptance

No risk in this file is considered accepted merely because the repository builds or a maintainer merges a PR. Residual-risk acceptance requires a named authority, date, scope, expiration/review date, and evidence reference.
