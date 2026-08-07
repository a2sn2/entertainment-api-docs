# FoundationKit.Security

`FoundationKit.Security` is the opt-in HTTP security capability that sits above `FoundationKit.WebApi`.

It exists to centralize security conventions that are reusable across products without pulling authentication, user storage, or provider-specific infrastructure into the kernel.

## Current v1 surface

### Trusted reverse proxy forwarding

`TrustedProxyOptions` and `TrustedProxySecurity` provide a fail-closed boundary for `X-Forwarded-For` and `X-Forwarded-Proto`.

When forwarding is enabled:

- `KnownProxies` must contain at least one explicit IP address;
- invalid or duplicate proxy addresses are rejected;
- `ForwardLimit` is bounded to `1..10`;
- ASP.NET Core trust-all defaults are cleared;
- only the configured proxy IPs are trusted;
- only `X-Forwarded-For` and `X-Forwarded-Proto` are enabled by this helper.

Typical registration:

```csharp
var reverseProxy = configuration
    .GetSection(TrustedProxyOptions.SectionName)
    .Get<TrustedProxyOptions>()
    ?? new TrustedProxyOptions();

services.AddFoundationTrustedProxyForwarding(reverseProxy);
```

Pipeline placement:

```csharp
app.UseFoundationTrustedProxyForwarding(reverseProxy);

// Security decisions that depend on RemoteIpAddress / Request.Scheme come after this point.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
```

Do not enable forwarded headers with an empty trust list and do not replace the explicit proxy list with trust-all network settings.

## Rate-limit partition conventions

`FoundationRateLimitPartitions` provides deterministic partition keys only:

- authentication traffic: remote IP;
- authenticated write traffic: user identifier when available, otherwise remote IP;
- explicit remote-address helper.

The package deliberately does **not** choose permit counts, window duration, queueing, persistence, Redis, or a distributed rate-limit provider. Those are deployment/product decisions.

Reverse-proxy forwarding must run before rate-limit partitioning when the application is deployed behind a trusted proxy.

## Authentication assurance

`FoundationAuthenticationAssurance` defines the reusable MFA assurance convention:

```text
amr = mfa
```

`RequireFoundationMultiFactor()` adds the shared claim requirement to an ASP.NET Core authorization policy.

The Security package only defines and evaluates the assurance signal. It does not:

- authenticate users;
- enroll authenticators;
- issue recovery codes;
- decide how step-up is performed;
- persist sessions;
- send account notifications.

Those responsibilities belong to the Identity capability and its adapters.

## Dependency direction

```text
FoundationKit.Domain
        ↑
FoundationKit.Application
        ↑
FoundationKit.WebApi
        ↑
FoundationKit.Security
```

The kernel never depends on Security.

Athar is the first repository consumer of this package. Its existing trusted-proxy, rate-limit partition, and administrator MFA policy behavior is routed through the reusable Security primitives rather than duplicated in the product.

## Maturity

Catalog maturity remains `Preview` in v1. The package has a real consumer and automated tests, but it should not be promoted to `Stable` until the API shape has survived additional reusable consumers and the later Identity integration.
