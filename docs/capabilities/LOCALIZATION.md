# FoundationKit.Localization

## Purpose

`FoundationKit.Localization` provides a small provider-neutral culture/time-zone foundation for reusable FoundationKit consumers. It owns culture identity, RTL/LTR metadata, supported-culture fallback, and a bounded opaque time-zone identifier without selecting translation resources, persistence, or a time-zone provider.

Current maturity: **ReferenceOnly**.

## Public surface

- `CultureDefinition` — canonical .NET culture name, parent name, and derived text direction.
- `TextDirection` — `LeftToRight` or `RightToLeft`.
- `SupportedCultureSet` — bounded supported cultures plus an explicit default.
- `CultureResolution` / `CultureResolutionSource` — resolved culture and provenance (`Exact`, `Parent`, `Default`, or `InvalidRequested`).
- `TimeZoneId` — bounded opaque provider-neutral time-zone identifier.
- `LocalizationContext` — resolved culture plus time-zone identity.

## Resolution semantics

`SupportedCultureSet.Resolve` is deterministic:

1. exact supported culture wins;
2. otherwise a supported parent culture is selected while walking the BCL culture-parent chain;
3. otherwise the explicit default culture is selected;
4. null/blank requests use the default;
5. invalid or overlong requests use the default with `InvalidRequested` provenance.

The default culture must itself be present in the supported set. Duplicate canonical cultures are rejected.

## Directionality

Direction is derived from `CultureInfo.TextInfo.IsRightToLeft` instead of being reimplemented by FoundationKit. For example, Workbench proves `ar-YE` as `RightToLeft` while the unit suite covers `en-US` as `LeftToRight`.

## Time-zone boundary

`TimeZoneId` intentionally validates only the identifier boundary:

- non-empty after trimming;
- maximum 128 characters;
- no control characters.

It does **not** call `TimeZoneInfo.FindSystemTimeZoneById`. This keeps the reusable contract neutral between IANA, Windows, cloud-provider, and application-owned mappings. A deployment/provider adapter that performs date-time conversion must validate/resolve the identifier in its own supported environment.

## Explicit non-goals

v1 does not implement:

- translation/resource storage;
- machine translation;
- localization administration UI;
- user/tenant culture persistence;
- browser/HTTP language negotiation;
- date/time conversion engine;
- Windows ↔ IANA mapping;
- currency/numbering business rules;
- product-specific fallback policy beyond the explicit supported set/default supplied by the consumer.

## Workbench consumer evidence

Workbench uses Settings to provide:

- `workbench.experience.default-culture = ar-YE`;
- `workbench.experience.default-time-zone = UTC`.

`GET /api/platform-reference` resolves the configured culture through `SupportedCultureSet`, reports `RightToLeft`, reports exact-resolution provenance, exposes the opaque `UTC` time-zone ID, and retains the existing Settings/Feature Management evidence. The SQL Server integration smoke flow asserts all of those values before exercising the existing user/admin workflow.

No database migration, schema, authentication, or authorization change is introduced by Localization v1.

## Dependency direction

`FoundationKit.Localization` has no direct dependency on another FoundationKit package. The Capability Model records `localization -> kernel` as composition metadata while the package remains BCL-only and lower/peer packages do not depend back on it.
