# FoundationKit / Athar — Cryptography, Keys, Certificates, and Secrets Inventory

This document inventories cryptographic/security material visible from repository design. It does not select a production KMS/Vault/CA provider.

## Inventory

| Item | Purpose | Current repository control | Production requirement / status |
|---|---|---|---|
| ASP.NET Identity password hashes | Password verification | Framework-managed Identity hashers; raw passwords never stored by application code | Keep framework-supported algorithm/config; final password/authenticator policy requires approval |
| Authentication cookie | Authenticated browser session | `HttpOnly`, `SameSite=Strict`, `Secure=Always` outside Development | HTTPS-only, controlled lifetime, session revocation/monitoring; final timeout decision pending |
| Antiforgery cookie/token | CSRF protection | HttpOnly/SameSite cookie + `X-CSRF-TOKEN` on writes | Keep HTTPS-only outside Development; negative tests required |
| Data Protection master keys | Protect auth/antiforgery/token-provider material | Default ASP.NET Core behavior; no production persistence/key-encryption provider committed | **External production gate:** durable, access-controlled, encrypted key store; backup/rotation/recovery design required |
| Identity email/reset tokens | Confirmation/recovery | Framework token providers; sent through SMTP and never logged/returned by recovery request endpoints | Configure provider lifetime according to approved policy; secure SMTP/TLS; incident revocation process |
| TOTP authenticator key | MFA seed | Identity user token store; exposed only in authenticated setup after password re-authentication | Treat as authentication secret; never log; user reset/compromise workflow |
| TOTP recovery codes | MFA recovery | Identity recovery-code store; returned only on generation/regeneration | One-time storage by user; never log; support workflow decision pending |
| SQL application credential | API database authentication | Dev Native may use Windows auth; dev Compose uses generated `sa` password | Production validator rejects `sa`; provision least-privilege runtime principal in external secret manager |
| SQL migration credential | Schema change | Development app may migrate automatically | Production app refuses startup migrations; separate migration principal/process required |
| SQL TLS trust | DB transport | Development permits `Encrypt=False`/trusted local certificate | Production validator requires encryption and rejects `TrustServerCertificate=True` |
| SMTP credential | Account notifications | Configuration fields only; no committed secret | Inject from production secret manager; SMTP TLS/certificate validation; rotation/compromise procedure |
| GitHub `GITHUB_TOKEN` | Actions API/package access | Workflow-scoped permissions; experimental `packages:write` limited to publishing job | Maintain least privilege; protected environments/release approvals when configured |
| Cloudflare Quick Tunnel | Temporary public HTTPS edge | Account-less temporary URL, IPv4/HTTP2, no durable key committed | Development/demo only; not a production trust anchor or permanent ingress |
| Package/image artifact digests | Integrity evidence | SHA-256 manifests generated in CI | Cryptographic signing/attestation requires approved identity/key provider |

## Secret-storage rules

- No production secret is committed to Git, appsettings, Postman, Docker Compose, documentation, or test code.
- Local development credentials live under `.local/` (Git-ignored). Windows launchers now restrict credential-file ACLs to the current SID and no longer print the administrator password during normal startup.
- Shell launcher creates an owner-only (`0600`) bootstrap credential file and does not echo the password.
- CI credentials are generated at runtime or provided through GitHub secrets.
- Secret scanners report only location/type, not candidate secret values.
- Account confirmation/reset/MFA tokens must never appear in logs.

## Production key-management gates

The following remain external organizational/platform decisions:

- KMS/Vault/HSM/secret-manager product;
- Data Protection key persistence and at-rest protection provider;
- certificate authority and issuance process;
- key/secret/certificate rotation frequencies;
- revocation/compromise authority and emergency procedure;
- artifact signing identity and trust policy;
- database/SMTP certificate lifecycle monitoring.

## Production fail-closed controls already in repository

- explicit host allow-list required;
- automatic DB migration/role seeding/admin seed forbidden;
- operational SMTP account-notification path required;
- SQL encryption required;
- SQL certificate trust bypass rejected;
- runtime `sa` rejected.

These startup checks demonstrate repository intent but do not replace secret-manager/KMS/CA evidence.