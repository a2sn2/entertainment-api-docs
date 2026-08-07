# STEP-02 — Secure SDLC and software supply-chain controls

## Policies

- Secure Software Development Life Cycle Policy.
- Malware Protection Policy.
- Cryptography and Key Management Policy.
- Change Management Policy.
- Risk Management Policy.
- Application Security Policy.

## Implemented repository controls

1. Enabled .NET analyzers at the .NET 8 recommended analysis level with warnings treated as errors.
2. Enabled NuGet vulnerability auditing for moderate-or-higher advisories through repository build properties.
3. Added a dependency-free tracked-source secret scanner that reports location/type without printing candidate secret values.
4. Added deterministic CycloneDX 1.5 NuGet dependency SBOM generation from the resolved .NET graph.
5. Added SHA-256 integrity manifests for package and published application artifacts.
6. Added CodeQL SAST for C# and JavaScript/TypeScript using an immutable CodeQL Action commit.
7. Added pinned Trivy filesystem/repository scanning for vulnerabilities, secrets, and misconfiguration.
8. Added pinned Trivy Athar container vulnerability scanning with HIGH/CRITICAL gate and SARIF upload.
9. Added container hardening static policy checks.
10. Pinned current checkout/setup-dotnet/upload-artifact, CodeQL, Trivy, and Pages actions to immutable commit SHAs in the workflows modified by this program.
11. Reduced the experimental product workflow so `packages: write` exists only on the container-publishing job.
12. Added artifact SHA-256 evidence to the experimental Windows package.

## Findings affected

- `FK-SDLC-001`: materially implemented (SAST, SCA/NuGet audit, secret scan, SBOM, container scan). IaC/DAST and final coverage gate remain to be expanded.
- `FK-SUP-001`: NuGet audit implemented; dependency lock files/source mapping/update automation remain open.
- `FK-SUP-002`: immutable action references implemented for repository workflows touched by this program; integrated verification pending.
- `FK-REL-001`: SBOM and artifact digests implemented. Cryptographic signing/attestation authority remains an organizational/release-platform decision.
- `FK-TEST-001`: negative tests are expanding; coverage threshold is still open.
- `FK-DOCK-001`: static/runtime non-root controls implemented; integrated container smoke pending.
- `FK-DOCK-002`: base-image digest pinning remains pending because an approved immutable-image update process/digests are not yet established.

## Verification planned in PR CI

- repository secret scanner;
- NuGet restore/audit;
- CodeQL C# and JavaScript analysis;
- Trivy filesystem and image scans;
- CycloneDX JSON validation;
- container static hardening checker;
- Docker build and non-root runtime assertion;
- full existing build/test/publish/pack/integration suite.

## Residual/external gaps

- Package lock files and NuGet package-source mapping are not yet implemented.
- DAST is not yet present.
- Test coverage threshold is not yet present.
- Artifact signing/attestation is not claimed without an approved signing/release authority.
- Base/container image digests remain mutable until an approved update process pins verified digests.
