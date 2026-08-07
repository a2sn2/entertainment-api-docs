# FoundationKit / Athar — PII and Data Lifecycle Inventory

This inventory documents repository-visible personal data and expected controls. Retention periods, legal bases, deletion deadlines, and data-subject obligations remain organizational/legal decisions and are intentionally not invented here.

## Data categories

| Data | Location / model | Purpose in current product | Transfers | Logs/audit rule | Retention/deletion decision |
|---|---|---|---|---|---|
| Email address | `AtharUser.Email`, auth requests, `CurrentUserResponse` | Account identity, login, account notifications | Browser ↔ API ↔ SQL; SMTP provider for notifications | Do not log full value at Information+ except explicitly approved security event with masking | **Pending organizational/privacy decision** |
| Display name | `AtharUser.DisplayName`, initiative DTOs | UI attribution and review display | Browser ↔ API ↔ SQL | Treat as PII; avoid free-text duplication | **Pending** |
| User identifier (`Guid`) | Identity, initiative owner/reviewer, audit | Authorization, ownership, audit correlation | API ↔ SQL; may appear in structured security logs | Allowed where required for audit; access-controlled | **Pending** |
| Role membership | ASP.NET Identity role tables / current-user response | Authorization | API ↔ SQL ↔ browser for navigation | Security-relevant; log role changes as structured events | **Pending** |
| Password hash/security stamp | ASP.NET Identity tables | Authentication | SQL only; framework-managed | Never log/export in normal diagnostics | Identity lifecycle + account deletion decision pending |
| MFA authenticator key | ASP.NET Identity token store fields | TOTP MFA | API ↔ SQL; displayed once during setup to authenticated/re-authenticated user | Never log | Revoked/reset on disable or compromise |
| MFA recovery codes | ASP.NET Identity token store | Recovery from second-factor loss | API ↔ SQL; returned only immediately after generation/regeneration | Never log; previous set invalidated on regeneration | User-controlled regeneration; organizational support process pending |
| Email confirmation/password reset token | In-memory/request, Identity token provider | Account confirmation/recovery | API → SMTP → user → API | Never log or persist in repository | Short-lived per Identity token configuration; final policy decision pending |
| Initiative title/summary/category/city | `Initiative` | Product business function | Browser ↔ API ↔ SQL | Summary/title are user-generated and may accidentally contain PII; do not copy to security logs | Product/privacy decision pending |
| Requested budget/beneficiary count | `Initiative` | Product business function | Browser ↔ API ↔ SQL | Business data; may become sensitive depending on deployment | Product decision pending |
| Review notes | `InitiativeReview.Notes` | Administrative decision explanation | Browser ↔ API ↔ SQL | Free text may contain PII; do not duplicate into logs | Governance/privacy decision pending |
| Audit actor/action/entity/details | `AuditEntry` | Accountability/investigation | API ↔ SQL; future central log sink | Must be structured/minimized; free-text details need hardening | Security/legal retention pending |
| Remote IP (rate limiter / possible web logs) | `HttpContext.Connection.RemoteIpAddress` | Abuse prevention | Edge/API | Treat as personal/security telemetry where applicable; do not retain without policy | Logging/privacy decision pending |
| Correlation ID | FoundationKit middleware | Request correlation | Browser/API/logs | Not PII by design; reject control characters and cap length | Logging retention decision pending |
| SQL backups | `.local/backups` in dev; future production backup store | Recovery | DB → backup media | Contains all Identity/product/audit data | RPO/RTO/retention/encryption/off-site/immutability decisions pending |
| CI test data | GitHub Actions containers | Automated validation | CI runner only | Synthetic test identities only; no real PII | Ephemeral cleanup expected |

## Data-minimization rules

- APIs return dedicated DTOs, never the Identity entity or password/security fields.
- Authentication/recovery responses must not disclose whether an account exists.
- Security scanners and error handlers must not echo secret/token values.
- Browser-only demo content must remain synthetic and must not be presented as real server data.
- Quick Tunnel and LAN testing use synthetic data only.
- Logs/audit should prefer IDs and classified event fields over copied free text.

## Required organizational decisions

- lawful/approved processing purpose and privacy notice;
- retention/deletion schedule by data category;
- account deletion/export workflow and exceptions for required audit evidence;
- log and backup retention;
- SMTP provider/data-transfer assessment;
- cross-border/third-party transfer requirements where applicable;
- incident notification process;
- test-data policy and masking requirements.

## Verification targets

- Negative tests prove password/tokens never appear in normal API responses.
- Repository secret scan runs in CI.
- Logging review confirms no account/recovery token is emitted.
- Backup/restore test uses synthetic data only.
- PII inventory is reviewed whenever contracts, Identity fields, audit schema, telemetry, or integrations change.
