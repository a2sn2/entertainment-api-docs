# Architecture

## Purpose

FoundationKit supplies technical building blocks. It does not define a product domain, select a database provider, own migrations, issue identity tokens, or host an application.

## Dependency rules

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor is independent from server-side packages.
```

### Domain

May depend only on the .NET base class library.

### Application

May depend on Domain. It owns use-case contracts and ports but does not depend on EF Core, ASP.NET Core, or a UI framework.

### Infrastructure

May depend on Application, Domain, and provider-neutral EF Core abstractions. It must not reference a relational provider package or an ASP.NET Core host.

### WebApi

May depend on Application and the ASP.NET Core shared framework. It adapts classified results to HTTP and supplies reusable middleware.

### Blazor

Owns browser-side transport and state helpers. It must not reference Domain, Application, Infrastructure, EF Core, or server hosting.

Architecture tests enforce assembly-level references. Code review is still required for conceptual coupling that assembly tests cannot detect.

## Persistence

`EfRepository<TEntity, TId, TContext>` is a technical adapter. A consuming product owns:

- its DbContext;
- entity configurations;
- provider selection;
- migrations;
- transaction policy;
- concurrency strategy;
- specialized repositories and read models.

## Domain events

`DomainEventsSaveChangesInterceptor` supports synchronous and asynchronous EF Core saves.

Sequence:

```text
Capture pending events
        ↓
Database save succeeds
        ↓
Clear aggregate event queues
        ↓
Dispatch handlers in process
```

A database failure dispatches nothing and leaves aggregate event queues unchanged. A handler failure occurs after the database commit and is surfaced to the caller, but cleared events are not dispatched again automatically.

This contract deliberately avoids pretending to provide durable messaging. Use an outbox for delivery guarantees.

## HTTP

`FoundationKit.WebApi` supplies:

- RFC 7807 Problem Details mapping for classified results;
- a bounded correlation-ID middleware;
- baseline response headers;
- registration and pipeline extensions.

Authentication, authorization policies, CORS, rate limiting, forwarded headers, TLS, OpenAPI, and product endpoints belong to the consuming host.

## Versioning

The repository is pre-1.0. Package versions are coordinated in `src/Directory.Build.props`. Public API changes must update tests, documentation, and `CHANGELOG.md`.
