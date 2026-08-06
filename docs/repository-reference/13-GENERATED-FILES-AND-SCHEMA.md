# Generated Files, Solution Metadata, and Database Schema

Generated files are part of the repository because they define reproducible build or schema state. They should be understood and reviewed, but not edited as if they were ordinary handwritten business code.

---

# 1. Visual Studio solution file

## `platform/EntertainmentDocs.sln`

The solution is a text manifest consumed by Visual Studio/MSBuild tooling.

Header fields:

- solution format version;
- Visual Studio major version;
- exact Visual Studio build used to write it;
- minimum supported Visual Studio version.

Each project entry contains:

```text
project type GUID
project display name
relative .csproj path
project instance GUID
```

The solution currently includes 15 projects:

```text
5 FoundationKit packages
5 product backend/contract projects
3 frontend projects
2 test projects
```

`GlobalSection(SolutionConfigurationPlatforms)` defines Debug and Release for Any CPU.

`ProjectConfigurationPlatforms` maps every project GUID to active/build configurations.

`SolutionProperties` keeps the solution node visible.

`ExtensibilityGlobals` stores the solution GUID.

## Maintenance rule

Add/remove projects through `dotnet sln` or Visual Studio where practical. Manual edits are possible but GUID/path mistakes can make projects disappear or fail configuration mapping.

The solution file does not define project dependency references; those live in `.csproj` files.

---

# 2. EF Core migration artifact roles

```text
Migration class
    executable schema transition

Migration Designer
    model metadata associated with that migration

Model Snapshot
    latest model baseline used to generate the next migration
```

All three are required for a healthy EF migration history.

---

# 3. Initial migration class

## `20260805113706_InitialSqlServerSchema.cs`

The class derives from `Migration` and defines:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
protected override void Down(MigrationBuilder migrationBuilder)
```

### `Up`

Transforms an empty database into the initial product schema.

### `Down`

Reverses the migration. It drops dependent child tables before parents to avoid foreign-key violations.

The exact generated order may group Identity and product tables according to dependency analysis rather than conceptual module order.

---

# 4. Schema objects created by the initial migration

## 4.1 Identity role table: `AspNetRoles`

Core columns include:

- `Id` GUID primary key;
- `Name`;
- `NormalizedName`;
- `ConcurrencyStamp`.

Indexes include the standard normalized-role unique index.

Purpose: named role definitions such as Administrator and Editor.

## 4.2 Identity user table: `AspNetUsers`

Standard Identity fields include:

- GUID ID;
- user name and normalized user name;
- email and normalized email;
- email-confirmed flag;
- password hash;
- security stamp;
- concurrency stamp;
- phone and confirmation;
- two-factor flag;
- lockout end/enabled;
- failed access count.

Product fields:

- `DisplayName`;
- `IsActive`.

Indexes include normalized email/user name and active state.

Password plaintext is never stored; Identity stores a hash.

## 4.3 Identity role claims: `AspNetRoleClaims`

Integer primary key, role foreign key, claim type/value. Cascade delete follows role deletion.

## 4.4 Identity user claims: `AspNetUserClaims`

Integer primary key, user foreign key, claim type/value.

## 4.5 Identity user logins: `AspNetUserLogins`

Composite key for external provider/login key, provider display name, user foreign key.

Current product does not expose an external-login flow, but Identity schema supports it.

## 4.6 Identity user roles: `AspNetUserRoles`

Composite key `(UserId, RoleId)` and foreign keys to users/roles. Represents many-to-many assignment.

## 4.7 Identity user tokens: `AspNetUserTokens`

Composite key `(UserId, LoginProvider, Name)` with token value. Current custom JWT access token is not persisted here by default.

---

# 5. Product tables

## 5.1 `documentation_documents`

Conceptual columns:

| Column | Meaning |
|---|---|
| `Id` | GUID primary key |
| `Reference` | unique business/document reference, max 64 |
| `Slug` | unique lowercase route key, max 120 |
| `Title` | display title, max 240 |
| `Status` | enum integral value |
| `OwnerId` | creator/owner user GUID |
| `CreatedAt` | creation timestamp with offset semantics |
| `UpdatedAt` | last aggregate update |
| `PublishedAt` | nullable publication timestamp |

Indexes:

- unique reference;
- unique slug.

The migration may use SQL Server types selected by EF conventions, such as `uniqueidentifier`, `nvarchar`, `int`, and `datetimeoffset`.

`OwnerId` is currently stored as a GUID property without an explicitly configured foreign-key relationship to Identity user. This avoids cross-model navigation coupling but means referential enforcement is not present unless migration/configuration states otherwise.

## 5.2 `documentation_versions`

Conceptual columns:

| Column | Meaning |
|---|---|
| `Id` | GUID primary key |
| `DocumentId` | parent document foreign key |
| `Version` | label, max 32 |
| `Content` | Unicode large text |
| `AuthorId` | author user GUID |
| `CreatedAt` | version creation time |

Constraints:

- foreign key to document;
- cascade delete;
- unique `(DocumentId, Version)`.

`AuthorId` is product metadata and is not currently configured as an Identity foreign key.

## 5.3 `audit_entries`

Conceptual columns:

| Column | Meaning |
|---|---|
| `Id` | GUID primary key |
| `UserId` | optional actor GUID |
| `Action` | operation name, max 120 |
| `EntityName` | target type, max 120 |
| `EntityId` | target identifier text, max 128 |
| `CreatedAt` | audit timestamp |
| `MetadataJson` | Unicode JSON text |

Indexes:

- CreatedAt;
- UserId.

The table is schema foundation only. No current interceptor automatically inserts all mutations.

---

# 6. EF migration history table

`__EFMigrationsHistory` is created/maintained by EF Core. It records migration ID and product version. `database update` compares committed migrations against this table.

Never manually mark a migration applied without ensuring schema matches it.

---

# 7. Migration Designer file

## `20260805113706_InitialSqlServerSchema.Designer.cs`

Generated elements commonly include:

- `[DbContext(typeof(AppDbContext))]`;
- `[Migration("20260805113706_InitialSqlServerSchema")]`;
- `BuildTargetModel(ModelBuilder)`;
- product-version annotation;
- SQL Server identity/column annotations;
- every entity, property, key, index, relationship, navigation, and table name at that migration.

Why it is verbose: EF stores a complete model representation so tooling can understand the migration's target.

Do not optimize or hand-format this file. Regeneration may replace formatting/order.

---

# 8. Model snapshot

## `AppDbContextModelSnapshot.cs`

Generated class derives from `ModelSnapshot` and implements `BuildModel`.

It represents the latest committed model, not a database query result.

When generating a new migration, EF:

1. builds model from current entity/configuration code;
2. compares it with snapshot;
3. emits Up/Down difference;
4. updates snapshot.

Deleting or manually altering the snapshot can make EF generate destructive or duplicate operations.

---

# 9. How to create a new migration

From `platform/` after changing Domain/Infrastructure mapping:

```powershell
dotnet tool restore

dotnet ef migrations add MeaningfulMigrationName `
  --project .\src\EntertainmentDocs.Infrastructure\EntertainmentDocs.Infrastructure.csproj `
  --startup-project .\src\EntertainmentDocs.Api\EntertainmentDocs.Api.csproj `
  --context AppDbContext
```

Then inspect:

- generated Up operations;
- generated Down operations;
- column nullability/defaults;
- indexes and uniqueness;
- data loss warnings;
- foreign-key delete behavior;
- provider-specific types;
- snapshot diff.

Apply locally:

```powershell
dotnet ef database update `
  --project .\src\EntertainmentDocs.Infrastructure\EntertainmentDocs.Infrastructure.csproj `
  --startup-project .\src\EntertainmentDocs.Api\EntertainmentDocs.Api.csproj
```

---

# 10. Migration review examples

## Adding a required column

A required column on an existing table needs a safe default, backfill, or phased nullable migration. Merely generating `nullable: false` can fail against existing rows or assign incorrect data.

## Renaming a column

EF may generate drop/add if it cannot infer rename. Review and use `RenameColumn` to preserve data where appropriate.

## Changing enum values

Changing enum numeric order does not automatically transform stored integers. Add explicit data migration or keep stable values.

## Adding unique index

Existing duplicates must be resolved before index creation. Application pre-checks do not repair historical data.

## Changing cascade behavior

Review delete implications and test child retention/removal.

---

# 11. SQL Server-specific generated annotations

Generated model files may include annotations for:

- provider product version;
- maximum identifier length;
- identity columns for integer Identity claim IDs;
- Unicode and max length;
- SQL column types;
- index filters for nullable normalized names;
- value generation.

These are provider/tool metadata, not handwritten business rules.

---

# 12. Other generated/runtime assets

## Blazor framework files

`_framework` WebAssembly files under build/publish output are generated and ignored. They are not tracked source and should not be documented individually.

## `bin/` and `obj/`

Generated assemblies, intermediate MSBuild assets, static web assets manifests, NuGet restore files. Ignored.

## `platform/artifacts/foundation/`

Generated `.nupkg` and `.snupkg` output. Ignored/local or workflow artifact, not source.

## Docker layers/volumes

Generated outside Git. SQL test volume is removed by CI cleanup.

---

# 13. Tool version notes

The local tool manifest pins EF CLI 8.0.0 because the solution targets .NET/EF Core 8. A globally newer tool does not need to replace the repository tool merely because it exists.

Package versions are centralized and should be upgraded deliberately with restore/build/test/migration review.

---

# 14. Generated-file modification policy

| File type | Direct manual edit policy |
|---|---|
| `.sln` | Prefer Visual Studio/`dotnet sln`; review diff |
| Migration `.cs` | Review and deliberate adjustments allowed when understood |
| Migration `.Designer.cs` | Do not hand-edit normally |
| Model snapshot | Do not hand-edit normally |
| `bin/obj` | Never commit/edit |
| NuGet package output | Rebuild, do not edit archive |
| Blazor publish assets | Rebuild, do not edit generated output |

---

# 15. Schema verification in SSMS

After migration:

1. connect to selected local instance with Windows Authentication;
2. refresh Databases;
3. open `EntertainmentDocs_Dev`;
4. verify Identity, document, version, audit, and migration-history tables;
5. inspect indexes/keys when diagnosing uniqueness or relationship behavior;
6. do not make untracked schema edits as a normal development path.

SSMS is an inspection/query tool; EF configuration/migrations remain source of truth.
