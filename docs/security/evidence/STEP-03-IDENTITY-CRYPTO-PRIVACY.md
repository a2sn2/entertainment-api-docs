# STEP-03 — Identity lifecycle, cryptography, secrets, and privacy

## Policies

- Password Management Policy.
- Personally Identifiable Information Protection Policy.
- Data Transfer Policy.
- Cryptography and Key Management Policy.
- Application Security Policy.
- Logging and Monitoring Policy.
- Risk Management Policy.

## Implemented

- Email-confirmation token generation/delivery endpoint flow through SMTP without returning or logging tokens.
- Password-forgot/reset lifecycle with account-enumeration-resistant request response.
- Authenticated password change with sign-in refresh.
- TOTP authenticator setup after password re-authentication.
- MFA enable/disable, recovery-code generation/regeneration, authenticator-code and recovery-code login.
- Optional administrator policy requiring an `amr=mfa` authenticated session when organizational configuration enables it.
- Account UI for confirmation, recovery, password change, MFA setup, MFA login, and recovery-code handoff.
- Production requires operational SMTP recovery delivery.
- Local bootstrap administrator password is no longer printed by normal launchers; credential files receive owner-only ACL/permissions.
- Production Data Protection keys can be persisted to a durable path and encrypted with a configured X.509 certificate; production configuration requires both.
- Added `PII-DATA-INVENTORY.md` and `CRYPTO-AND-SECRETS-INVENTORY.md`.
- Added production deployment contract requiring secret injection, encrypted SQL transport, non-`sa` runtime identity, persistent protected Data Protection keys, and no startup privilege.

## Findings affected

- `FK-AUTH-001`: identity lifecycle capabilities implemented; final MFA scope/factor policy and production SMTP provider remain organizational/external decisions.
- `FK-AUTH-002`: existing password values remain configurable baseline values; final policy/breached-password source remains pending organizational decision.
- `FK-AUTH-003`: development admin seed no longer silently promotes existing accounts; production validator forbids admin seed.
- `FK-SEC-001`: normal launchers no longer print the bootstrap password and restrict local credential files.
- `FK-PII-001`: repository data inventory/control rules implemented; retention/deletion/legal decisions remain pending.
- `FK-CRY-001`: crypto/secret inventory and fail-closed production transport/secret contract implemented; provider/rotation decisions remain external.
- `FK-CRY-002`: durable encrypted Data Protection key capability implemented; production certificate/key lifecycle remains external.
- `FK-DATA-001`: production validator rejects unencrypted/unvalidated SQL transport; development Compose remains intentionally insecure-for-production.
- `FK-DATA-002`: production validator rejects `sa`; provisioning separate runtime/migration principals remains platform/DBA evidence.

## Verification still required

Integrated CI must prove compilation, API/client publishing, account-contract compatibility, security configuration tests, secret scanning, and the existing Athar E2E workflow. SMTP/TOTP end-to-end tests that require external providers/devices remain controlled-environment evidence rather than fabricated CI success.