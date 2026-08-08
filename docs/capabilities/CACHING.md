# FoundationKit.Caching

## Purpose

`FoundationKit.Caching` provides a small provider-neutral byte-cache boundary for reusable FoundationKit consumers. It defines bounded keys, explicit finite TTL, hit/miss semantics, remove behavior, and a BCL-only in-memory reference provider without selecting Redis or distributed cache policy.

Current maturity: **ReferenceOnly**.

## Public surface

- `CacheKey` — bounded normalized cache identifier.
- `CacheEntryOptions` — explicit positive finite time-to-live.
- `CacheReadResult` — explicit hit/miss result with defensive byte snapshots.
- `ICacheStore` — provider-neutral asynchronous get/set/remove contract.
- `InMemoryCacheOptions` — per-provider bounds for entries, value size, and maximum TTL.
- `InMemoryCacheStore` — BCL-only reference provider using `TimeProvider`.

## Data boundary

The reusable contract stores bytes rather than arbitrary .NET objects. Serialization format, schema/versioning, compression, encryption, and domain-object mapping remain consumer concerns.

`CacheReadResult` and the in-memory provider defensively copy payloads. Mutating a caller buffer after `SetAsync`, or mutating a byte array copied from a prior read, does not mutate the stored entry.

Diagnostics expose only `Hit` or `Miss`; cache payloads are not included in result diagnostics.

## Expiration and capacity

The reference provider:

1. rejects non-positive or unbounded TTL values;
2. enforces a configurable maximum TTL;
3. removes expired entries when they are read or before writes;
4. enforces a configurable maximum entry count and value size;
5. when capacity is full after expired-item cleanup, evicts the entry with the earliest expiry, with ordinal key ordering as the deterministic tie-break.

This eviction policy is reference-provider behavior, not a requirement that every future distributed provider implement the same internal algorithm.

## Cancellation

`ICacheStore` operations accept `CancellationToken`. The in-memory provider checks caller cancellation and preserves cancellation as cancellation instead of translating it to a cache miss or provider failure.

## Workbench consumer evidence

Workbench's existing `CatalogService` is the first runtime consumer. The service now:

1. asks `ICacheStore` for `workbench/catalog/embedded-v1`;
2. on a hit, parses the cached bytes and returns the same cloned JSON root contract;
3. on a miss, reads the existing embedded `foundationkit.catalog.json` resource;
4. parses the bytes, caches a defensive snapshot for 15 minutes, and returns the same contract.

The Workbench host registers `InMemoryCacheStore` with explicit entry/value/TTL limits. `CatalogCachingTests` proves that two consecutive service reads produce two cache gets but only one cache set, while the SQL integration smoke flow calls `/api/catalog` twice before exercising the existing user/admin workflow. The catalog remains an embedded repository artifact and cache is only an acceleration layer, never the source of truth.

No database migration, schema, authentication, authorization, or Athar runtime change is introduced by Caching v1.

## Explicit non-goals

Caching v1 does not implement:

- Redis or another distributed provider;
- cross-node coherence;
- distributed locks;
- cache-as-source-of-truth persistence;
- object serialization/deserialization conventions;
- tag/group invalidation;
- refresh-ahead;
- stale-while-revalidate;
- provider discovery or failover;
- a guarantee that secrets or regulated data are appropriate to cache;
- application-specific cache invalidation policy.

A future Redis/provider package must remain separate from `FoundationKit.Caching` and must define its operational consistency/security behavior from a real deployment requirement.

## Dependency direction

`FoundationKit.Caching` is BCL-only and has no direct FoundationKit package dependency. The Capability Model records `caching -> kernel` as composition metadata while lower and peer packages remain independent of Caching.
