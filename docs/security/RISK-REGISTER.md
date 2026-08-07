# FoundationKit — Repository Security Risk Register

This is a repository-level engineering risk register. It does **not** replace an organizational enterprise risk register or legal/compliance assessment.

## Scoring rule

No numeric likelihood/impact thresholds are invented here. Severity follows the confirmed repository audit/review until organizational risk appetite is approved.

## Active risks

| Risk ID | Scenario | Assets | Existing controls | Current rating | Treatment / current state | Evidence target |
|---|---|---|---|---|---|---|
| R-FK-001 | A change reaches `main` without independent approval | Source, CI, releases | PRs, CI, CODEOWNERS/evidence template | High | **External treatment remains:** independent PR approval + protected `main` required; author/self-review cannot close SoD | GitHub protected-branch/ruleset evidence + independent PR approval |
| R-FK-002 | Malicious/vulnerable dependency or secret enters supply chain | Source, packages, images | Central versions, secret scan, NuGet audit, Trivy, baseline CycloneDX inventory, Dependabot | High | Block fixable vulnerabilities/secrets; monitor updates; do not claim dependency inventory as complete provenance | CI security job + Dependabot + artifact evidence |
| R-FK-003 | Administrator approves own business object | Initiative decisions/audit | Role authorization + domain maker-checker invariant | High | Repository treatment implemented/previously verified | Unit + E2E negative test |
| R-FK-004 | Public production surface exposes Swagger or implementation details | API metadata | HTTPS/HSTS baseline, Development-only Swagger, minimal readiness | High | Repository treatment implemented/previously verified | Production-mode/config tests |
| R-FK-005 | Authentication abuse succeeds or harms availability; proxy topology collapses client identities into one limiter bucket | Accounts/sessions/availability | Identity lockout, cookies, partitioned rate limits | High | Explicit trusted proxy IPs; forwarded headers processed before HTTPS/rate limiting; untrusted forwarded headers ignored; runtime 429 assertion added | Trusted/untrusted proxy tests + black-box 429 security run |
| R-FK-006 | Runtime account can alter schema or uses excessive DB privilege | SQL schema/data | EF Core, migrations | High | Runtime rejects `sa`; startup migration/role seed disabled in Production; exact runtime/migration principals remain external DBA provisioning | Production template + startup tests + DBA evidence |
| R-FK-007 | Database/PII/authentication token travels without validated transport encryption | Credentials/data/tokens | TLS at web edge, SQL production validation | High | SQL encryption/cert validation gate + SMTP TLS fail-closed validation; ingress/certificate/provider evidence remains external | Configuration tests + production transport evidence |
| R-FK-008 | Backup exists but cannot be restored during incident | SQL data, audit, identity | SQL backup with CHECKSUM | Critical for production | Isolated restore drill implemented and previously verified; Production backup service/RPO/RTO remain external decisions | Restore workflow + production recovery evidence |
| R-FK-009 | Secrets leak via local files/terminal/logs | Admin/SQL credentials | `.gitignore`, generated values | High | Stop routine printing, restrictive local storage, secret scanning, masked ephemeral CI credentials | Script tests + scan + workflow logs |
| R-FK-010 | Audit evidence is changed or lacks enough context | Decisions/investigation evidence | `AuditEntries`, correlation IDs, target event catalog | High | Catalog is explicitly a target contract, not runtime-proof; structured audit enrichment + external append-only sink remain | DB migration/tests + central sink evidence |
| R-FK-011 | Public Quick Tunnel exposes development data/config | Local Athar/PII | Warning text, temporary URL | High if public | Demo-only boundary; refuse Production; synthetic data only | Launcher tests/docs |
| R-FK-012 | Container compromise has unnecessary privilege | Runtime/container host | Multi-stage image | High | Non-root runtime, no-new-privileges, cap drop where compatible; previously verified | Container smoke test |
| R-FK-013 | PII retained/transferred without approved lifecycle | User/account/audit/backup data | Limited DTO exposure, PII inventory | High | Inventory/minimization implemented; retention/deletion/legal decisions remain organizational | Privacy evidence record |
| R-FK-014 | Security event occurs without detection or response evidence | Accounts/API/DB | App/CI logs, target event catalog, runbook | High | Central sink/alerts/retention/on-call remain external; do not infer runtime coverage from catalog | Observability/SIEM evidence |
| R-FK-015 | Automatic startup migration causes destructive/unreviewed production change | SQL schema/data | EF migrations | High | Config-gated migration; Production fail-closed; controlled migration runbook | Production-mode startup test + deployment evidence |
| R-FK-016 | Upstream runtime/base-image vulnerability has no vendor-fixed package yet | Containers/runtime | Trivy image scan, rebuilds, dependency monitoring | High | Block fixable HIGH/CRITICAL findings, retain complete SARIF for unfixed findings, monitor vendor updates; no implicit residual-risk acceptance | Trivy gate + SARIF + update PRs |
| R-FK-017 | Attacker with a stolen session/password removes MFA or rotates recovery codes without possessing the second factor | Accounts/authenticators | Password re-check, CSRF, Identity security stamp | High | Full re-authentication now requires current password + fresh TOTP or recovery factor; sensitive MFA changes invalidate/update session state; black-box negative/positive cases added | Security workflow MFA step-up evidence |
| R-FK-018 | User is not independently alerted when password/MFA factors change | Accounts/incident detection | In-session success messages | High | Independent email security notifications added for password reset/change, MFA enable/disable, recovery-code regeneration; operational delivery requires Production SMTP/provider | Source/build + approved SMTP delivery evidence |
| R-FK-019 | Missing Production configuration silently disables confirmed email/MFA/password controls | Accounts/configuration | appsettings Development defaults | Medium–High | Production startup now requires explicit true/false/value decisions without inventing their approved values | Production configuration tests |

## Review linkage

The independent read-only PR #34 review is mapped as follows:

- `PR34-APP-01` → `R-FK-005`.
- `PR34-AUTH-01` → `R-FK-017`.
- `PR34-AUTH-02` → `R-FK-018`.
- `PR34-CRY-01` → `R-FK-007`.
- `PR34-AUTH-03` → `R-FK-019`.
- `PR34-EVID-01` and `PR34-EVID-02` are evidence-integrity findings tracked in the canonical policy register.
- `PR34-REV-01` → `R-FK-001`.

## Acceptance

No risk in this file is considered accepted merely because the repository builds or a maintainer merges a PR. Residual-risk acceptance requires a named authority, date, scope, expiration/review date, rationale, and evidence reference.
