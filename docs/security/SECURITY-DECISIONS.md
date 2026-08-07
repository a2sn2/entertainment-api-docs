# FoundationKit — Security Decisions Requiring Organizational Approval

The repository must remain configurable or fail closed where these values are unresolved. Contributors must not invent a policy value merely to make a test green.

| Decision ID | Decision | Why repository cannot decide it alone | Current safe engineering position | Required owner/evidence |
|---|---|---|---|---|
| D-001 | Required PR reviewer count | Segregation-of-duties authority is organizational | Repository provides CODEOWNERS/checklists; protected-branch setting remains external | Repository owner / security governance |
| D-002 | Final MFA scope and accepted factor types | Depends on identity assurance/risk/applicability | Provide MFA-ready design; require admin MFA before sensitive production approval | Security/Risk |
| D-003 | Password policy values and breached-password source | Must align with approved authentication standard | Keep Identity hashing/lockout and configurable values; do not hard-code new enterprise values | Security/Risk |
| D-004 | ASVS target level | Depends on product risk and business context | Map current controls/gaps; no false certification | AppSec/Risk |
| D-005 | RPO | Business loss tolerance | Restore capability must be tested regardless | Business owner/Risk |
| D-006 | RTO | Service recovery objective | Automate restore evidence, but do not claim target | Business owner/Operations |
| D-007 | Log retention | Legal/security/operations requirement | Produce structured events and configurable sinks | Security/Legal/Operations |
| D-008 | PII retention/deletion periods | Legal/business purpose | Inventory data and implement configurable lifecycle hooks | Privacy/Legal/Product |
| D-009 | Backup retention/off-site/immutability policy | Business continuity and data classification | Local backup remains development convenience only | Operations/Risk |
| D-010 | Vulnerability remediation SLA | Risk appetite/severity governance | Findings must be tracked immediately; no invented due dates | Security/Risk |
| D-011 | Exception validity/renewal | Governance authority | Exceptions must be explicit and expiring once policy is approved | Risk authority |
| D-012 | Release approval authority | Organization structure/SoD | CI cannot self-approve production | Change/Release authority |
| D-013 | Risk acceptance authority | Enterprise governance | No repository finding is implicitly accepted | Risk authority |
| D-014 | Secret manager/KMS/certificate provider | Production platform architecture | Fail closed when production secret/key prerequisites are absent | Platform/Security |
| D-015 | Production hosting/network architecture | External infrastructure | Repository publishes minimum security contract, not provider assumption | Platform/Architecture |
| D-016 | Production database account provisioning model | DBA/identity platform | Separate migration/runtime roles are required; exact principal creation is external | DBA/Security |

## Decision record format

When an item is approved, append:

```text
Decision ID:
Approved value/scope:
Approver:
Approval date:
Evidence reference:
Review/expiry date:
Affected policies:
Affected implementation/tests:
```

Do not replace this register with undocumented chat or verbal decisions.