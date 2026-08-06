# Database and Product Infrastructure

This chapter explains the concrete adapters selected by EntertainmentDocs: Microsoft SQL Server, EF Core, ASP.NET Core Identity persistence, JWT generation, repository implementation, seeding, and schema configuration.

---

# 1. Infrastructure project boundary

`EntertainmentDocs.Infrastructure` references:

- Product Domain;
- Product Application;
- FoundationKit.Infrastructure;
- ASP.NET Core shared framework;
- ASP.NET Core Identity EF stores;
- EF Core Design and Tools as private build/design dependencies;
- Microsoft SQL Server EF provider.

This is the first layer allowed to select SQL Server. FoundationKit.Infrastructure remains provider-neutral.

---

# 2. `DependencyInjection.cs`

`AddInfrastructure(IServiceCollection, IConfiguration)` is the product Infrastructure composition method.

## 2.1 Connection string validation

```csharp
var connectionString = configuration.GetConnectionString("SqlServer");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(...);
```

The application fails during startup instead of reaching a later database call with ambiguous configuration.

## 2.2 Foundation infrastructure registration

```csharp
services.AddFoundationInfrastructure();
```

Registers domain-event dispatcher and EF save interceptor before the DbContext requests the interceptor.

## 2.3 DbContext registration

```csharp
services.AddDbContext<AppDbContext>((serviceProvider, options) => { ... });
```

The overload exposes the DI provider so the scoped domain-event interceptor can be resolved.

### SQL Server provider

```csharp
options.UseSqlServer(connectionString, sqlServer => { ... });
```

This line is the provider choice owned by the product.

### Retry policy

```text
maxRetryCount = 5
maxRetryDelay = 10 seconds
```

EF retries transient database errors recognized by the SQL Server provider. This does not make an entire business workflow idempotent, and it does not justify retrying unknown purchase execution requests.

### Save interceptor

```csharp
options.AddInterceptors(
    serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
```

Connects FoundationKit domain events to successful DbContext saves.

## 2.4 Identity registration

`AddIdentityCore<ApplicationUser>` selects product user type and configures:

- minimum password length 12;
- at least one digit;
- at least one uppercase letter;
- unique email;
- maximum five failed attempts before lockout.

`AddRoles<IdentityRole<Guid>>()` enables GUID-based roles.

`AddEntityFrameworkStores<AppDbContext>()` stores Identity data in the product database.

Current configuration does not explicitly set every lockout duration, lowercase/symbol requirement, or token provider. Unspecified values follow ASP.NET Core Identity defaults.

## 2.5 JWT settings

```csharp
services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
```

Binds the `Jwt` configuration section to typed options.

## 2.6 Product port bindings

- `IDocumentRepository` -> `DocumentRepository`, scoped;
- product `IUnitOfWork` -> current `AppDbContext`, scoped;
- product `IClock` -> `SystemClock`, singleton;
- `ITokenService` -> `JwtTokenService`, scoped.

The DbContext already implements the product unit-of-work interface, so the same tracked context commits repository changes.

---

# 3. Identity implementation

## 3.1 `Identity/ApplicationUser.cs`

```csharp
public sealed class ApplicationUser : IdentityUser<Guid>
```

Inherits standard Identity fields such as user name, normalized names, email, password hash, security stamp, concurrency stamp, phone, lockout, and failed count.

Product additions:

- `DisplayName`: UI/token name;
- `IsActive`: application-level account enable flag, default true.

`IsActive` is checked by login. It is separate from Identity lockout: lockout is temporary security behavior; inactive is product lifecycle state.

## 3.2 `Identity/SystemRoles.cs`

Defines exact case-sensitive role names:

```text
Administrator
Editor
Reviewer
Reader
```

`All` is used by seeding and user endpoint validation. Role strings are centralized to avoid misspelling divergence.

## 3.3 `Identity/JwtOptions.cs`

Configuration model:

- `SectionName = "Jwt"`;
- issuer;
- audience;
- signing key;
- access-token lifetime, default 30 minutes.

Properties use `init`, making them configuration-initialized values.

## 3.4 `Identity/ITokenService.cs`

One method creates an access token from a product user and role collection. Endpoint code does not know signature or serialization details.

## 3.5 `Identity/JwtTokenService.cs`

The implementation builds a compact JWT manually.

### Time calculation

```csharp
var now = DateTimeOffset.UtcNow;
var expires = now.AddMinutes(settings.AccessTokenMinutes);
```

The service currently uses system time directly rather than product `IClock`. This is acceptable for infrastructure token timestamps but makes isolated token-time tests less flexible.

### Header

```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

HS256 is HMAC SHA-256 using one symmetric signing secret.

### Payload claims

- `iss`: configured issuer;
- `aud`: configured audience;
- `sub`: user ID;
- `jti`: unique token identifier;
- `iat`: issued-at Unix seconds;
- `nbf`: not-before Unix seconds;
- `exp`: expiry Unix seconds;
- .NET name identifier claim;
- display-name claim;
- email claim;
- role claim array.

### Encoding/signature

1. serialize header and payload to UTF-8 JSON bytes;
2. Base64URL encode each;
3. join with period;
4. compute HMACSHA256 over the unsigned token using signing key bytes;
5. Base64URL encode signature;
6. return `header.payload.signature`.

Base64URL conversion removes `=` padding and replaces characters unsafe in URLs.

### Security implications

- the signing key must remain secret and be at least the API-enforced minimum length;
- payload is encoded, not encrypted;
- clients can read claims but cannot create a valid signature without the key;
- production should consider established token libraries/external identity for rotation, key IDs, standards maintenance, and lifecycle features;
- no refresh token is currently issued.

## 3.6 `Identity/IdentitySeeder.cs`

`SeedAsync` creates a scope from the root service provider, then resolves role and user managers.

### Role seed

For each known role:

1. check existence;
2. create if missing.

The operation is idempotent across startups.

### Bootstrap admin configuration

Reads:

```text
BootstrapAdmin:Email
BootstrapAdmin:Password
```

When either is blank, administrator creation is skipped. This lets production omit unsafe source-controlled bootstrap values.

### User seed

- look up by email;
- create GUID-based user when absent;
- use fixed display name `Bootstrap Administrator`;
- mark email confirmed;
- run Identity password validation/hashing;
- throw with joined Identity descriptions when creation fails;
- ensure Administrator role assignment.

The seeder currently guarantees Administrator role, not all four roles. A bootstrap administrator can still pass all current policy requirements because Administrator is included in each administrative policy.

Production bootstrap policy should avoid permanent static passwords and should define first-login rotation or external provisioning.

---

# 4. Time adapter

## `Services/SystemClock.cs`

Implements product `IClock`:

```csharp
public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
```

It is stateless and registered singleton. Tests can substitute a fake clock in a separate service collection or handler unit test.

---

# 5. `AppDbContext.cs`

```csharp
public sealed class AppDbContext(...)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
```

Responsibilities:

- Identity persistence;
- product document/version persistence;
- audit-entry persistence model;
- change tracking;
- unit-of-work save method inherited from DbContext;
- configuration discovery.

## DbSets

- `Documents` -> `DocumentationDocument`;
- `DocumentVersions` -> `DocumentVersion`;
- `AuditEntries` -> `AuditEntry`.

Expression-bodied properties call `Set<TEntity>()`; they do not allocate a new set each time.

## `OnModelCreating`

1. call base so Identity configures its schema;
2. scan Infrastructure assembly for `IEntityTypeConfiguration<T>` implementations.

Omitting `base.OnModelCreating` would break Identity mapping.

## `AuditEntry`

Current persistence model fields:

- GUID ID;
- optional user ID;
- action;
- entity name;
- entity ID text;
- creation time;
- metadata JSON.

The model/table exists, but current code does not automatically create audit rows for every mutation. It is a foundation for future audit implementation, not proof of complete auditing.

---

# 6. Design-time DbContext factory

## `AppDbContextFactory.cs`

Implements `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` can create the context without running the full API host.

Connection priority:

1. `ConnectionStrings__SqlServer` environment variable;
2. `ENTERTAINMENTDOCS_SQLSERVER` environment variable;
3. default localhost Windows-authentication connection.

It builds DbContext options with SQL Server and retry-on-failure, then returns a context.

Why this exists:

- migrations can run in developer tools and scripts;
- startup-only services do not need to be constructed;
- CI/local scripts can override connection through environment.

The default connection is development only.

---

# 7. EF Core configurations

## 7.1 `ApplicationUserConfiguration.cs`

- `DisplayName`: required, max 200;
- `IsActive`: SQL default true;
- index on `IsActive`.

The active index can support administrative filters/login lifecycle queries.

## 7.2 `AuditEntryConfiguration.cs`

- table `audit_entries`;
- GUID primary key;
- action max 120, required;
- entity name max 120, required;
- entity ID max 128, required;
- metadata `nvarchar(max)`, required;
- index on creation time;
- index on optional user ID.

`nvarchar` preserves Unicode metadata.

## 7.3 `DocumentationDocumentConfiguration.cs`

### Document mapping

- table `documentation_documents`;
- primary key ID;
- reference max 64, required;
- slug max 120, required;
- title max 240, required;
- unique reference index;
- unique slug index.

### Relationship

```text
DocumentationDocument 1 -> many DocumentVersion
foreign key: DocumentVersion.DocumentId
on delete: cascade
```

`UsePropertyAccessMode(Field)` tells EF to use the private `_versions` field, preserving aggregate encapsulation.

### Version mapping

- table `documentation_versions`;
- primary key ID;
- version max 32, required;
- content `nvarchar(max)`, required;
- unique composite index `(DocumentId, Version)`.

The composite index permits different documents to use `1.0.0` but prevents duplicate label on one document.

---

# 8. `DocumentRepository.cs`

The repository derives from:

```csharp
EfRepository<DocumentationDocument, Guid, AppDbContext>
```

and implements product `IDocumentRepository`.

It stores `_dbContext` locally for product queries. The base already exposes a protected context, but the explicit field keeps current methods concise; it is not a second context.

## `ReferenceExistsAsync`

Uses SQL `EXISTS` behavior through `AnyAsync` and exact equality. SQL Server collation determines case sensitivity unless explicitly configured.

## `SlugExistsAsync`

Lowercases input before comparison, matching aggregate slug normalization.

## `GetWithVersionsAsync`

- queries document set;
- includes versions;
- expects zero or one ID match through `SingleOrDefaultAsync`;
- keeps tracking enabled for mutation.

## `GetPublishedBySlugAsync`

- read-only `AsNoTracking`;
- includes versions;
- lowercases input;
- filters slug and `Published` state;
- expects unique result.

## `ListPublishedAsync`

- no tracking;
- published filter;
- alphabetical title order;
- materialize list;
- expose as read-only list interface.

## `AddVersionAsync`

Adds child directly to `DocumentVersions` set and converts EF `ValueTask` to `Task`.

### Repository responsibilities

The repository does not decide whether a document may publish. Domain does. It only provides persistence/query shapes.

---

# 9. Database environments

## Local Development

Typical connection:

```text
Server=<local machine or instance>
Database=EntertainmentDocs_Dev
Trusted_Connection=True
TrustServerCertificate=True
MultipleActiveResultSets=True
```

Windows Authentication avoids a committed local SQL password.

## Automated Testing

Docker SQL Server 2022 container:

```text
Database=EntertainmentDocs_Test
Host port=14333
```

Credentials in Compose are isolated test-only values.

## Production

No production connection string is committed. Production must provide:

- managed secret or workload identity;
- encrypted connection;
- dedicated database;
- backup/restore policy;
- monitored availability;
- reviewed migration deployment.

---

# 10. Initial schema migration

Files:

```text
20260805113706_InitialSqlServerSchema.cs
20260805113706_InitialSqlServerSchema.Designer.cs
AppDbContextModelSnapshot.cs
```

## Migration class

`Up` creates:

- Identity users;
- Identity roles;
- role/user claims;
- user logins;
- user roles;
- user tokens;
- documentation documents;
- documentation versions;
- audit entries;
- foreign keys and indexes.

`Down` drops objects in dependency-safe reverse order.

## Designer

Generated model metadata representing the model at this migration. EF uses it for migration tooling and history.

## Snapshot

Generated representation of the latest model. EF compares current model with this snapshot when generating the next migration.

Do not hand-edit generated annotations casually. Change entity/configuration code, generate migration, inspect generated operations, and test update/rollback path.

A detailed schema-oriented treatment appears in `13-GENERATED-FILES-AND-SCHEMA.md`.

---

# 11. Local setup script interaction

`setup-local-sqlserver.ps1` supplies connection through environment variables before running:

1. `dotnet tool restore`;
2. solution restore;
3. Debug build;
4. `dotnet ef database update` with Infrastructure project and API startup project.

The design factory and API configuration both understand the environment override, making migrations and runtime point at the same selected local database.

---

# 12. Persistence correctness rules

1. Domain rules are not database constraints alone.
2. Database unique indexes remain necessary even after application pre-checks.
3. Read-only queries should use no-tracking.
4. Mutating use cases must load tracked aggregates.
5. One request scope should use one DbContext/unit-of-work boundary.
6. Every schema change requires a migration and snapshot update.
7. Migration code must be reviewed before production.
8. Do not reuse test credentials in shared environments.
9. Do not expose cost/internal metadata in public contracts.
10. Do not mistake retry-on-transient-SQL-failure for business-operation idempotency.
11. Audit table presence is not a complete audit implementation.
12. Cascade-delete behavior must be reviewed before adding delete endpoints.

---

# 13. Known infrastructure improvement points

Implemented foundation but not complete production controls:

- translate SQL unique-constraint races to typed Conflict results;
- add optimistic concurrency (`rowversion`) where simultaneous editing matters;
- implement automatic audit capture and immutable retention;
- move production migration execution to deployment control;
- add structured observability;
- add secret manager integration;
- define backup, restore, recovery, RPO, and RTO;
- add refresh token/external identity lifecycle;
- add explicit connection resiliency and transaction policy per use case.
