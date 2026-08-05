# FoundationKit Core

## Purpose

FoundationKit is the reusable engineering core for future products. It is deliberately independent from Entertainment Docs: product-specific business rules remain in their bounded contexts, while repeatable technical behavior lives in the foundation projects.

The current repository is the first production-style consumer and validation environment for the core.

## Projects

```text
core/
├── FoundationKit.Domain
├── FoundationKit.Application
├── FoundationKit.Infrastructure
├── FoundationKit.WebApi
└── FoundationKit.Blazor
```

### FoundationKit.Domain

Framework-independent primitives:

- `Entity<TId>` with stable equality;
- `AggregateRoot<TId>` with domain events;
- `ValueObject` equality;
- `IDomainEvent` and `IHasDomainEvents`;
- typed `DomainException`.

It must never reference EF Core, ASP.NET Core, Blazor, SQL Server, or a product assembly.

### FoundationKit.Application

Use-case and boundary abstractions:

- typed `Result`, `Result<T>`, `Error`, and `ErrorType`;
- `ICommand`, `IQuery`, and handler contracts;
- `IUnitOfWork`, `ICurrentUser`, and `IClock`;
- hybrid repository contracts;
- specification pattern without exposing `IQueryable`;
- pagination primitives;
- domain-event handler and dispatcher contracts;
- lightweight validation contracts.

### FoundationKit.Infrastructure

Adapters for technical persistence behavior:

- `EfRepository<TEntity,TId,TDbContext>`;
- `SpecificationEvaluator`;
- `EfUnitOfWork<TDbContext>`;
- scoped domain-event dispatching after a successful database commit.

A bounded context may extend the generic repository with business-language queries. Generic repositories do not replace aggregate-specific repositories.

### FoundationKit.WebApi

ASP.NET Core conventions:

- one application-result-to-HTTP mapping;
- RFC 7807 Problem Details with typed error codes;
- correlation IDs;
- baseline security headers;
- reusable API service and middleware registration.

### FoundationKit.Blazor

Frontend behavior shared by Admin and Client applications:

- `ApiClientBase`;
- `ApiResult`, `ApiResult<T>`, and detailed `ApiError`;
- RFC 7807 response parsing;
- network and timeout normalization;
- reusable `AsyncState<T>`.

MudBlazor visual components stay in `EntertainmentDocs.Ui`. FoundationKit.Blazor contains transport and state behavior only.

## Extension rule

A new capability follows this sequence:

```text
Domain aggregate / value object
    ↓
Command or Query + dedicated Handler
    ↓
Repository port only when persistence is needed
    ↓
Infrastructure adapter
    ↓
HTTP Contract
    ↓
Thin Endpoint
    ↓
Typed Blazor API Client
    ↓
Feature state / components / page
    ↓
Postman and automated tests
```

## Generic versus product-specific

Generic foundation:

- entity identity and equality;
- results and error classification;
- unit of work;
- repository mechanics and specifications;
- domain-event dispatching;
- HTTP problem mapping;
- correlation and security middleware;
- HTTP client execution and asynchronous UI state.

Product-specific code:

- document lifecycle rules;
- user and role policies;
- document repository queries;
- endpoint routes;
- request and response contracts;
- feature pages and workflows.

## Explicit non-goals

FoundationKit does not provide:

- generic controllers or generic endpoints;
- generic business managers;
- reflection-heavy request buses;
- direct `IQueryable` exposure outside Infrastructure;
- automatic CRUD for aggregates;
- database access from Blazor;
- business rules inside Razor pages.

## Quality gates

Every change must pass:

- nullable analysis and warnings-as-errors;
- Release build of the full solution;
- unit tests;
- architecture dependency tests;
- SQL Server migration test;
- API/Admin/Client/Docs end-to-end workflow;
- Postman JSON validation.

The core can later be extracted into versioned internal NuGet packages without changing the dependency direction used by product projects.
