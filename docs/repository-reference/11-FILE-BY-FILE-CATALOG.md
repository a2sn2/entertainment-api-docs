# Exhaustive File-by-File Catalog

This catalog lists every tracked file at the reference baseline and identifies its role and detailed chapter. Generated build output (`bin`, `obj`, package artifacts, local databases, user secrets, IDE caches) is intentionally excluded because it is not tracked source.

Legend:

- **Runtime**: directly loaded or executed.
- **Build**: influences compilation/package generation.
- **Test**: validates behavior.
- **Docs**: documentation only.
- **Generated**: created by tooling and tracked because it defines schema/build metadata.

---

# 1. Repository support and static root

| File | Kind | Purpose | Detail |
|---|---|---|---|
| `.devcontainer/devcontainer.json` | Build/DevEx | .NET 8 Codespaces/devcontainer with Docker and tools | Chapter 10 |
| `.github/workflows/foundationkit-ci.yml` | CI | Build/test/pack FoundationKit | Chapter 10 |
| `.github/workflows/platform-ci.yml` | CI | Build/test/full-stack SQL Server workflow | Chapter 10 |
| `.gitignore` | Build | Excludes outputs, local secrets, IDE and package artifacts | This catalog / root README |
| `.nojekyll` | Runtime hosting | Disables GitHub Pages Jekyll processing | Chapter 3 |
| `.vscode/tasks.json` | DevEx | VS Code task wrappers for repository commands | Chapter 10 |
| `404.html` | Runtime | GitHub Pages fallback shell | Chapter 3 |
| `README.md` | Docs | Primary repository overview | Root reference |
| `index.html` | Runtime | Static portal entry shell | Chapter 3 |
| `manifest.webmanifest` | Runtime | PWA metadata | Chapter 3 |
| `sw.js` | Runtime | Static portal service worker/cache | Chapter 3 |

## Static CSS

| File | Kind | Purpose | Detail |
|---|---|---|---|
| `assets/css/tokens.css` | Runtime | Theme/design custom properties | Chapter 3 |
| `assets/css/base.css` | Runtime | Global element/reset/typography rules | Chapter 3 |
| `assets/css/layout.css` | Runtime | Shell/navigation/responsive layout | Chapter 3 |
| `assets/css/components.css` | Runtime | Reusable static component styling | Chapter 3 |
| `assets/css/pages.css` | Runtime | Page-specific styling | Chapter 3 |

## Static HTML page shells

| File | Logical page |
|---|---|
| `pages/api-reference.html` | API Reference |
| `pages/error-assistant.html` | Error Assistant |
| `pages/governance.html` | Governance |
| `pages/known-limitations.html` | Known Limitations |
| `pages/open-questions.html` | Open Questions |
| `pages/platform-architecture.html` | Platform Architecture |
| `pages/playground.html` | Offline Playground |
| `pages/purchase-flow.html` | Purchase Flow |
| `pages/quick-start.html` | Quick Start |
| `pages/test-coverage.html` | Test Coverage |

All shells are Runtime files explained in Chapter 3.

---

# 2. Static portal JavaScript

## Application use cases

| File | Purpose |
|---|---|
| `src/application/use-cases/build-purchase-requests.js` | Normalize inputs, build initial/execute/check JSON, validate required fields |
| `src/application/use-cases/filter-test-scenarios.js` | Filter quality scenarios by status |
| `src/application/use-cases/resolve-error-action.js` | Select recovery guidance for an error category |
| `src/application/use-cases/search-documentation.js` | Build/search deterministic browser index |

## Domain documentation data

| File | Purpose |
|---|---|
| `src/domain/api-contracts.js` | External endpoint catalog, fields, examples, rules, errors |
| `src/domain/documentation-model.js` | Document control, response envelope, naming notice, success matrix |
| `src/domain/purchase-flow.js` | Ordered transaction stages and identifier meanings |
| `src/domain/quality-data.js` | Tests, error scenarios, limitations, open questions |

## Infrastructure adapters

| File | Purpose |
|---|---|
| `src/infrastructure/repositories/static-documentation-repository.js` | Query facade over domain documentation data |
| `src/infrastructure/storage/browser-preferences.js` | Theme/navigation preference storage adapter |

## Presentation components

| File | Purpose |
|---|---|
| `src/presentation/components/app-shell.js` | Shared header/navigation/shell renderer |
| `src/presentation/components/code-block.js` | Escaped code block/copy presentation |
| `src/presentation/components/command-palette.js` | Search palette behavior |
| `src/presentation/components/data-table.js` | Shared field/test table renderer |
| `src/presentation/components/dom-utils.js` | DOM and escaping helpers |
| `src/presentation/components/interactions.js` | Global events, theme, navigation, copy/disclosures |

## Presentation entry and pages

| File | Purpose |
|---|---|
| `src/presentation/main.js` | Static portal composition and renderer selection |
| `src/presentation/pages/api-reference-page.js` | API catalog renderer |
| `src/presentation/pages/error-assistant-page.js` | Error guidance renderer/initializer |
| `src/presentation/pages/governance-page.js` | Governance renderer |
| `src/presentation/pages/home-page.js` | Overview renderer |
| `src/presentation/pages/known-limitations-page.js` | Limitations renderer |
| `src/presentation/pages/open-questions-page.js` | Pending questions renderer |
| `src/presentation/pages/platform-architecture-page.js` | Dynamic platform architecture renderer |
| `src/presentation/pages/playground-page.js` | Offline request builder renderer/initializer |
| `src/presentation/pages/purchase-flow-page.js` | Transaction flow renderer/initializer |
| `src/presentation/pages/quick-start-page.js` | Quick start renderer/initializer |
| `src/presentation/pages/test-coverage-page.js` | Test matrix renderer/initializer |

All static JavaScript files are Runtime and explained in Chapter 3.

---

# 3. Platform root

| File | Kind | Purpose | Detail |
|---|---|---|---|
| `platform/.config/dotnet-tools.json` | Build | Pins local .NET tools, notably `dotnet-ef` | Chapters 6/10 |
| `platform/Directory.Build.props` | Build | Common target/nullability/warnings settings | Chapters 1/10 |
| `platform/Directory.Packages.props` | Build | Central NuGet versions | Chapters 1/10 |
| `platform/EntertainmentDocs.sln` | Build/Generated | 15-project Visual Studio solution/config map | Chapter 13 |
| `platform/README.md` | Docs | Platform quick start | Root docs |

---

# 4. FoundationKit.Domain

| File | Kind | Purpose |
|---|---|---|
| `platform/core/Directory.Build.props` | Build | Imports platform settings and adds NuGet package metadata |
| `platform/core/FoundationKit.Domain/FoundationKit.Domain.csproj` | Build | Framework-independent domain package definition |
| `platform/core/FoundationKit.Domain/Events/IDomainEvent.cs` | Runtime library | Domain event timestamp contract |
| `platform/core/FoundationKit.Domain/Events/IHasDomainEvents.cs` | Runtime library | Pending event owner contract |
| `platform/core/FoundationKit.Domain/Exceptions/DomainException.cs` | Runtime library | Coded domain exception |
| `platform/core/FoundationKit.Domain/Primitives/AggregateRoot.cs` | Runtime library | Entity plus pending events |
| `platform/core/FoundationKit.Domain/Primitives/Entity.cs` | Runtime library | Generic identity/equality primitive |
| `platform/core/FoundationKit.Domain/Primitives/ValueObject.cs` | Runtime library | Equality-component value object base |

Detailed line walkthrough: Chapter 4.

---

# 5. FoundationKit.Application

| File | Purpose |
|---|---|
| `platform/core/FoundationKit.Application/FoundationKit.Application.csproj` | Application package/project definition |
| `.../Abstractions/IClock.cs` | Time port |
| `.../Abstractions/ICurrentUser.cs` | Current actor port |
| `.../Abstractions/IUnitOfWork.cs` | Persistence commit port |
| `.../Events/IDomainEventHandler.cs` | Event handler and dispatcher contracts |
| `.../Messaging/ICommand.cs` | Command markers |
| `.../Messaging/ICommandHandler.cs` | Command execution contracts |
| `.../Messaging/IQuery.cs` | Query marker |
| `.../Messaging/IQueryHandler.cs` | Query execution contract |
| `.../Pagination/PageRequest.cs` | Validated page/size/skip model |
| `.../Pagination/PagedResult.cs` | Paged data and navigation metadata |
| `.../Persistence/IReadRepository.cs` | Generic read port |
| `.../Persistence/IRepository.cs` | Generic write port |
| `.../Persistence/ISpecification.cs` | Query description contract |
| `.../Persistence/Specification.cs` | Protected specification builder base |
| `.../Results/Error.cs` | Typed coded error record/factories |
| `.../Results/ErrorType.cs` | Error category enum |
| `.../Results/Result.cs` | Success/failure union types |
| `.../Validation/IValidator.cs` | Validation port and failure record |

Detailed line walkthrough: Chapter 4.

---

# 6. FoundationKit.Infrastructure

| File | Purpose |
|---|---|
| `platform/core/FoundationKit.Infrastructure/FoundationKit.Infrastructure.csproj` | Provider-neutral EF package definition |
| `.../DependencyInjection.cs` | Dispatcher/interceptor registration |
| `.../Events/DomainEventDispatcher.cs` | Resolve/invoke event handlers |
| `.../Events/DomainEventsSaveChangesInterceptor.cs` | Capture/dispatch events around successful save |
| `.../Persistence/EfRepository.cs` | Generic EF repository adapter |
| `.../Persistence/EfUnitOfWork.cs` | Generic DbContext save adapter |
| `.../Persistence/SpecificationEvaluator.cs` | Convert specification to IQueryable operations |

Detailed line walkthrough: Chapter 4.

---

# 7. FoundationKit.WebApi

| File | Purpose |
|---|---|
| `platform/core/FoundationKit.WebApi/FoundationKit.WebApi.csproj` | ASP.NET Core package definition |
| `.../DependencyInjection.cs` | ProblemDetails and middleware extension registration |
| `.../Middleware/CorrelationIdMiddleware.cs` | Request/response/log correlation |
| `.../Middleware/SecurityHeadersMiddleware.cs` | Baseline response security headers |
| `.../Results/ResultHttpExtensions.cs` | Result-to-HTTP/ProblemDetails mapping |

Detailed line walkthrough: Chapters 4 and 7.

---

# 8. FoundationKit.Blazor

| File | Purpose |
|---|---|
| `platform/core/FoundationKit.Blazor/FoundationKit.Blazor.csproj` | Browser transport/state package definition |
| `.../Api/ApiClientBase.cs` | Common send/deserialization/error/timeout behavior |
| `.../Api/ApiError.cs` | Structured browser error |
| `.../Api/ApiResponseReader.cs` | RFC7807/simple/raw HTTP error parser |
| `.../Api/ApiResult.cs` | Browser HTTP outcome types |
| `.../State/AsyncState.cs` | Loading/value/error state wrapper |

Detailed line walkthrough: Chapters 4 and 8.

## Core overview

| File | Purpose |
|---|---|
| `platform/core/README.md` | FoundationKit package and consumption guide |

---

# 9. EntertainmentDocs.Domain

| File | Purpose |
|---|---|
| `platform/src/EntertainmentDocs.Domain/EntertainmentDocs.Domain.csproj` | Product Domain project and FoundationKit reference |
| `.../Common/Entity.cs` | GUID-specialized product entity base |
| `.../Common/AggregateRoot.cs` | GUID-specialized product aggregate base |
| `.../Documents/DocumentStatus.cs` | Draft/InReview/Published/Archived enum |
| `.../Documents/DocumentVersion.cs` | Owned version entity |
| `.../Documents/DocumentationDocument.cs` | Aggregate, invariants, workflow, version ownership |

Detailed line walkthrough: Chapter 5.

---

# 10. EntertainmentDocs.Application

| File | Purpose |
|---|---|
| `platform/src/EntertainmentDocs.Application/EntertainmentDocs.Application.csproj` | Product Application project |
| `.../Abstractions/IClock.cs` | Product alias of core clock |
| `.../Abstractions/ICurrentUser.cs` | Product alias of actor port |
| `.../Abstractions/IDocumentRepository.cs` | Hybrid document repository port |
| `.../Abstractions/IUnitOfWork.cs` | Product save boundary alias |
| `.../DependencyInjection.cs` | Explicit handler registration |
| `.../Documents/AddDocumentVersion.cs` | Add-version command and handler |
| `.../Documents/CreateDocument.cs` | Create command and handler |
| `.../Documents/DocumentDtos.cs` | Application output DTOs |
| `.../Documents/DocumentErrors.cs` | Stable typed document errors |
| `.../Documents/GetPublishedDocument.cs` | Public details query and handler |
| `.../Documents/ListPublishedDocuments.cs` | Public list query and handler |
| `.../Documents/PublishDocument.cs` | Publish command and handler |
| `.../Documents/SubmitDocumentForReview.cs` | Review submission command and handler |

Detailed line walkthrough: Chapter 5.

---

# 11. EntertainmentDocs.Contracts

| File | Purpose |
|---|---|
| `platform/src/EntertainmentDocs.Contracts/EntertainmentDocs.Contracts.csproj` | Transport-only project |
| `.../Authentication/AuthenticationContracts.cs` | Login request/user/token response records |
| `.../Common/ApiErrorResponse.cs` | Simple legacy Identity error body |
| `.../Documents/DocumentContracts.cs` | Document request/response records |
| `.../Users/UserContracts.cs` | User create/list/role records |

Detailed line walkthrough: Chapters 5 and 9.

---

# 12. EntertainmentDocs.Infrastructure

| File | Purpose |
|---|---|
| `platform/src/EntertainmentDocs.Infrastructure/EntertainmentDocs.Infrastructure.csproj` | SQL Server/Identity/EF project definition |
| `.../DependencyInjection.cs` | Concrete provider and port registrations |
| `.../Identity/ApplicationUser.cs` | Product Identity user fields |
| `.../Identity/ITokenService.cs` | Access-token creation port |
| `.../Identity/IdentitySeeder.cs` | Role/bootstrap-admin idempotent seed |
| `.../Identity/JwtOptions.cs` | Typed JWT settings |
| `.../Identity/JwtTokenService.cs` | HS256 token construction |
| `.../Identity/SystemRoles.cs` | Role constants/list |
| `.../Persistence/AppDbContext.cs` | Identity/product context and audit model |
| `.../Persistence/AppDbContextFactory.cs` | Design-time context for migrations |
| `.../Persistence/Configurations/ApplicationUserConfiguration.cs` | User columns/default/index |
| `.../Persistence/Configurations/AuditEntryConfiguration.cs` | Audit table mapping |
| `.../Persistence/Configurations/DocumentationDocumentConfiguration.cs` | Document/version tables, indexes, relationship |
| `.../Persistence/Repositories/DocumentRepository.cs` | Product EF repository adapter |
| `.../Services/SystemClock.cs` | UTC clock implementation |

Detailed line walkthrough: Chapter 6.

## EF Core generated migration files

| File | Kind | Purpose |
|---|---|---|
| `.../Migrations/20260805113706_InitialSqlServerSchema.cs` | Generated/reviewed schema code | Initial Up/Down SQL model operations |
| `.../Migrations/20260805113706_InitialSqlServerSchema.Designer.cs` | Generated | Migration-specific model metadata |
| `.../Migrations/AppDbContextModelSnapshot.cs` | Generated | Latest model used for future migration diff |

Detailed schema treatment: Chapter 13.

---

# 13. EntertainmentDocs.Api

| File | Purpose |
|---|---|
| `platform/src/EntertainmentDocs.Api/EntertainmentDocs.Api.csproj` | ASP.NET Core host definition |
| `.../Program.cs` | Composition root, middleware, security, mappings, startup migration/seed |
| `.../Authorization/Policies.cs` | Stable policy names |
| `.../Endpoints/AdminDocumentEndpoints.cs` | Protected document workflow routes |
| `.../Endpoints/AdminUserEndpoints.cs` | Administrator Identity routes |
| `.../Endpoints/AuthEndpoints.cs` | Login route/token issuance |
| `.../Endpoints/DocumentEndpoints.cs` | Public published-document routes |
| `.../Services/HttpCurrentUser.cs` | ClaimsPrincipal-to-Application adapter |
| `.../appsettings.json` | Safe base placeholders/defaults |
| `.../appsettings.Development.json` | Local-only SQL/JWT/admin/logging values |
| `.../Properties/launchSettings.json` | Local ports/profiles/no auto-browser |

Detailed line walkthrough: Chapter 7.

---

# 14. EntertainmentDocs.Ui

| File | Purpose |
|---|---|
| `platform/apps/EntertainmentDocs.Ui/EntertainmentDocs.Ui.csproj` | Razor Class Library definition |
| `.../_Imports.razor` | Shared Razor namespace imports |
| `.../Theme/AppTheme.cs` | Light/dark MudBlazor theme |
| `.../Components/EmptyState.razor` | Reusable empty UI |
| `.../Components/ErrorState.razor` | Error and optional retry UI |
| `.../Components/LoadingState.razor` | Loading UI |
| `.../Components/PageHeader.razor` | Shared title/subtitle/icon/action layout |

Detailed line walkthrough: Chapter 8.

---

# 15. EntertainmentDocs.Admin

## Core and startup

| File | Purpose |
|---|---|
| `platform/apps/EntertainmentDocs.Admin/EntertainmentDocs.Admin.csproj` | Admin WebAssembly project/dependencies |
| `.../Program.cs` | Admin composition/API base/DI/run |
| `.../App.razor` | Providers, auth router, not-found state |
| `.../_Imports.razor` | Common Razor imports |
| `.../Properties/launchSettings.json` | Admin local ports/profile |

## Authentication

| File | Purpose |
|---|---|
| `.../Features/Authentication/AuthenticationApiClient.cs` | Login/logout typed client |
| `.../Features/Authentication/Pages/Login.razor` | Login form and safe return navigation |
| `.../Infrastructure/Authentication/AccessTokenStore.cs` | sessionStorage token abstraction/adapter |
| `.../Infrastructure/Authentication/JwtAuthenticationStateProvider.cs` | Restore/set/clear principal state |
| `.../Infrastructure/Authentication/JwtClaimsParser.cs` | UI-only JWT payload parser/expiry check |
| `.../Infrastructure/Api/AuthenticatedRequestFactory.cs` | Bearer request creation |

## Documents

| File | Purpose |
|---|---|
| `.../Features/Documents/DocumentsApiClient.cs` | Protected document typed client |
| `.../Features/Documents/Pages/Documents.razor` | Create/version/review/publish UI |

## Users

| File | Purpose |
|---|---|
| `.../Features/Users/UsersApiClient.cs` | User list/create/replace-role typed client |
| `.../Features/Users/Pages/Users.razor` | User table/create UI |

## Pages and layouts

| File | Purpose |
|---|---|
| `.../Pages/Index.razor` | Protected dashboard |
| `.../Pages/ApiReference.razor` | In-app Postman body reference |
| `.../Shared/EmptyLayout.razor` | Centered login layout |
| `.../Shared/MainLayout.razor` | App bar/drawer/role-aware navigation |
| `.../Shared/RedirectToLogin.razor` | Return-URL login redirect |

## Web root

| File | Purpose |
|---|---|
| `.../wwwroot/index.html` | WebAssembly host document |
| `.../wwwroot/css/app.css` | Admin host/app styles and Blazor error UI |
| `.../wwwroot/favicon.svg` | Admin favicon |
| `.../wwwroot/appsettings.json` | Deployable API base setting |
| `.../wwwroot/appsettings.Development.json` | Local API base |

Detailed line walkthrough: Chapter 8.

---

# 16. EntertainmentDocs.Client

## Core/startup

| File | Purpose |
|---|---|
| `platform/apps/EntertainmentDocs.Client/EntertainmentDocs.Client.csproj` | Public WebAssembly project/dependencies |
| `.../Program.cs` | Client composition/API base/DI/run |
| `.../App.razor` | Providers/router/not-found |
| `.../_Imports.razor` | Common imports |
| `.../Properties/launchSettings.json` | Client ports/profile |

## Runtime behavior

| File | Purpose |
|---|---|
| `.../Services/DocumentationApiClient.cs` | Public list/detail typed client |
| `.../Pages/Index.razor` | Catalog, local search, state display |
| `.../Pages/DocumentDetails.razor` | Latest published content page |
| `.../Shared/MainLayout.razor` | Public app bar/theme/content layout |

## Web root

| File | Purpose |
|---|---|
| `.../wwwroot/index.html` | WebAssembly host |
| `.../wwwroot/css/app.css` | Client styles/error UI |
| `.../wwwroot/favicon.svg` | Client favicon |
| `.../wwwroot/appsettings.json` | Deployable API base |
| `.../wwwroot/appsettings.Development.json` | Local API base |

Detailed line walkthrough: Chapter 8.

---

# 17. Tests

| File | Kind | Purpose |
|---|---|---|
| `platform/tests/FoundationKit.Tests/FoundationKit.Tests.csproj` | Test build | Core test project |
| `.../ArchitectureRulesTests.cs` | Test | Assembly dependency/provider rules |
| `.../EntityTests.cs` | Test | Persistent/transient equality |
| `.../ResultTests.cs` | Test | Result success/failure invariants |
| `.../GlobalUsings.cs` | Test build | Shared xUnit imports |
| `platform/tests/EntertainmentDocs.Domain.Tests/EntertainmentDocs.Domain.Tests.csproj` | Test build | Product domain test project |
| `.../DocumentationDocumentTests.cs` | Test | Publish workflow rules |
| `.../Usings.cs` | Test build | Shared xUnit import |

Detailed walkthrough: Chapter 10.

---

# 18. Scripts

| File | Platform | Purpose |
|---|---|---|
| `platform/scripts/setup-local-sqlserver.ps1` | Windows | Restore/build/migrate local SQL Server, optional API start |
| `platform/scripts/open-local-platform.ps1` | Windows | Wait for and open API/Client/Admin pages |
| `platform/scripts/pack-foundation.ps1` | Windows | Pack all FoundationKit projects |
| `platform/scripts/pack-foundation.sh` | Unix | Pack all FoundationKit projects |
| `platform/scripts/start-test-stack.sh` | Unix | Build/start/wait for Compose test stack |
| `platform/scripts/stop-test-stack.sh` | Unix | Stop test stack/remove orphans |
| `platform/scripts/smoke-test.sh` | Unix | End-to-end gateway/login/publish/public-read test |

Detailed line walkthrough: Chapter 10.

---

# 19. Deployment

| File | Kind | Purpose |
|---|---|---|
| `platform/deploy/.env.example` | Config docs | Required deployment variable template |
| `platform/deploy/Dockerfile.api` | Build/runtime | Multi-stage API image |
| `platform/deploy/Dockerfile.blazor` | Build/runtime | Parameterized Admin/Client publish + Nginx image |
| `platform/deploy/Dockerfile.docs` | Runtime | Static portal Nginx image |
| `platform/deploy/docker-compose.test.yml` | Test runtime | Isolated SQL/API/apps/docs/gateway topology |
| `platform/deploy/docker-compose.yml` | Runtime template | General stack composition |
| `platform/deploy/nginx-gateway.conf` | Runtime | Single-origin route gateway |
| `platform/deploy/nginx-spa.conf` | Runtime | Blazor static hosting and SPA fallback |

Detailed walkthrough: Chapter 10.

---

# 20. Postman

| File | Kind | Purpose |
|---|---|---|
| `platform/postman/EntertainmentDocs.postman_collection.json` | Executable docs/test | Ordered API request suite and scripts |
| `platform/postman/EntertainmentDocs.Local.postman_environment.json` | Test config | Local URL/token/ID variables |

Detailed walkthrough: Chapter 9.

---

# 21. Focused platform documentation

| File | Purpose |
|---|---|
| `platform/docs/ARCHITECTURE.md` | Modular monolith, bounded contexts, dependency rule |
| `platform/docs/AUTHORIZATION.md` | Role/policy rules |
| `platform/docs/CORE-FOUNDATION-PLAN.md` | Core hardening plan/history |
| `platform/docs/FRONTEND-ARCHITECTURE.md` | Blazor/MudBlazor boundaries |
| `platform/docs/LOCAL-SQLSERVER.md` | Local SQL and SSMS guide |
| `platform/docs/NEW-PROJECT-BOOTSTRAP.md` | Future product structure using FoundationKit |
| `platform/docs/POSTMAN-REQUESTS.md` | Endpoint body/status guide |
| `platform/docs/PRODUCTION-READINESS.md` | Explicit non-certified production gaps |
| `platform/docs/RUN-TEST-STACK.md` | Docker stack and smoke test guide |

These are Docs files and complement this deeper reference.

---

# 22. Coverage statement

Every tracked file in the baseline repository is represented in this catalog. Files containing significant executable behavior are explained semantically in Chapters 3–10. Generated migration/solution metadata is explained in Chapter 13. Operational and architectural rationale is consolidated in Chapters 12 and 14.
