#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

patterns=(
  "EntertainmentDocs"
  "Entertainment Docs"
  "entertainment-api-docs"
)

for pattern in "${patterns[@]}"; do
  matches="$(
    grep -RIl \
      --exclude-dir=.git \
      --exclude-dir=bin \
      --exclude-dir=obj \
      --exclude='verify-repository.sh' \
      -- "$pattern" . || true
  )"

  if [[ -n "$matches" ]]; then
    echo "Forbidden legacy product trace '$pattern' found in:" >&2
    echo "$matches" >&2
    exit 1
  fi
done

unexpected_top_level="$(
  find . -mindepth 1 -maxdepth 1 \
    ! -name '.git' \
    ! -name '.github' \
    ! -name '.editorconfig' \
    ! -name '.gitignore' \
    ! -name 'CHANGELOG.md' \
    ! -name 'CONTRIBUTING.md' \
    ! -name 'Directory.Build.props' \
    ! -name 'Directory.Packages.props' \
    ! -name 'FoundationKit.sln' \
    ! -name 'LICENSE' \
    ! -name 'README.md' \
    ! -name 'SECURITY.md' \
    ! -name 'apps' \
    ! -name 'catalog' \
    ! -name 'deploy' \
    ! -name 'docs' \
    ! -name 'examples' \
    ! -name 'global.json' \
    ! -name 'postman' \
    ! -name 'samples' \
    ! -name 'scripts' \
    ! -name 'src' \
    ! -name 'tests' \
    ! -name 'tools' \
    -print
)"

if [[ -n "$unexpected_top_level" ]]; then
  echo "Unexpected top-level entries found:" >&2
  echo "$unexpected_top_level" >&2
  exit 1
fi

provider_leaks="$(grep -RIl -- 'Microsoft.EntityFrameworkCore.SqlServer' src || true)"
if [[ -n "$provider_leaks" ]]; then
  echo "SQL Server provider coupling leaked into reusable core packages:" >&2
  echo "$provider_leaks" >&2
  exit 1
fi

migration_leaks="$(find src -type d -iname migrations -print)"
if [[ -n "$migration_leaks" ]]; then
  echo "EF Core migrations must belong to consuming applications, not reusable packages:" >&2
  echo "$migration_leaks" >&2
  exit 1
fi

required_files=(
  "README.md"
  "catalog/foundationkit.catalog.json"
  "docs/FEATURES.md"
  "docs/WORKBENCH.md"
  "docs/DUAL-FULL-STACK.md"
  "docs/PRODUCTION-READINESS-AR.md"
  "docs/ADDING-A-PROJECT-AR.md"
  "samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
  "samples/FoundationKit.Workbench/Program.cs"
  "samples/FoundationKit.Workbench/Endpoints/SystemEndpoints.cs"
  "samples/FoundationKit.Workbench/Endpoints/UserPortalEndpoints.cs"
  "samples/FoundationKit.Workbench/Endpoints/AdminPortalEndpoints.cs"
  "samples/FoundationKit.Workbench/Application/User/CreateUserRequestUseCase.cs"
  "samples/FoundationKit.Workbench/Application/Admin/ReviewUserRequestUseCase.cs"
  "samples/FoundationKit.Workbench/Infrastructure/Migrations/20260806113000_InitialWorkbench.cs"
  "samples/FoundationKit.Workbench/Infrastructure/Migrations/20260806164000_DualPortalWorkflow.cs"
  "samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
  "samples/FoundationKit.Workbench.Client/Pages/Home.razor"
  "samples/FoundationKit.Workbench.Client/Pages/UserPortal.razor"
  "samples/FoundationKit.Workbench.Client/Pages/AdminPortal.razor"
  "samples/FoundationKit.Workbench.Contracts/User/UserContracts.cs"
  "samples/FoundationKit.Workbench.Contracts/Admin/AdminContracts.cs"
  "examples/Athar/README.md"
  "examples/Athar/Athar.Domain/Initiative.cs"
  "examples/Athar/Athar.Application/InitiativeManager.cs"
  "examples/Athar/Athar.Infrastructure/AtharDbContext.cs"
  "examples/Athar/Athar.Infrastructure/Migrations/20260806180000_InitialAthar.cs"
  "examples/Athar/Athar.Infrastructure/Migrations/AtharDbContextModelSnapshot.cs"
  "examples/Athar/Athar.Contracts/Contracts.cs"
  "examples/Athar/Athar.Api/Program.cs"
  "examples/Athar/Athar.Api/Endpoints.cs"
  "examples/Athar/Athar.Api/DatabaseExceptionMiddleware.cs"
  "examples/Athar/Athar.Client/Services/AtharApiClient.cs"
  "examples/Athar/Athar.Client/ViewModels/ViewModels.cs"
  "examples/Athar/Athar.Client/Pages/Home.razor"
  "examples/Athar/Athar.Client/Pages/Account.razor"
  "examples/Athar/Athar.Client/Pages/Initiatives.razor"
  "examples/Athar/Athar.Client/Pages/Admin.razor"
  "tests/Athar.Tests/InitiativeTests.cs"
  "postman/FoundationKit.Workbench.postman_collection.json"
  "postman/Athar.Api.postman_collection.json"
  "deploy/docker-compose.yml"
  "deploy/athar-compose.yml"
  "scripts/smoke-athar.sh"
  "scripts/run-athar.ps1"
  "scripts/run-athar.sh"
  "scripts/stop-athar.ps1"
  "scripts/stop-athar.sh"
  "src/FoundationKit.Application/Models/EntityDto.cs"
  "src/FoundationKit.Blazor/Mvvm/ViewModelBase.cs"
)

for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Required repository file is missing: $required_file" >&2
    exit 1
  fi
done

workbench_api="samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
workbench_client="samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
athar_api="examples/Athar/Athar.Api/Athar.Api.csproj"
athar_client="examples/Athar/Athar.Client/Athar.Client.csproj"

if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' "$workbench_api"; then
  echo "Workbench API must explicitly own SQL Server." >&2
  exit 1
fi

if ! grep -q 'MudBlazor' "$workbench_client"; then
  echo "Workbench client must use MudBlazor." >&2
  exit 1
fi

if ! grep -q 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' "$athar_api"; then
  echo "Athar API must explicitly own ASP.NET Core Identity persistence." >&2
  exit 1
fi

if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' "$athar_api"; then
  echo "Athar API must explicitly own SQL Server." >&2
  exit 1
fi

if ! grep -q 'MudBlazor' "$athar_client"; then
  echo "Athar client must use MudBlazor." >&2
  exit 1
fi

if ! grep -q 'FoundationKit.Blazor' "$athar_client"; then
  echo "Athar client must consume FoundationKit.Blazor." >&2
  exit 1
fi

client_persistence_leaks="$(
  grep -RIl \
    --include='*.cs' \
    --include='*.csproj' \
    -- 'Microsoft.EntityFrameworkCore\|Microsoft.EntityFrameworkCore.SqlServer' \
    samples/FoundationKit.Workbench.Client \
    samples/FoundationKit.Workbench.Contracts \
    examples/Athar/Athar.Client \
    examples/Athar/Athar.Contracts || true
)"

if [[ -n "$client_persistence_leaks" ]]; then
  echo "Client and transport contracts must not reference EF Core or SQL Server:" >&2
  echo "$client_persistence_leaks" >&2
  exit 1
fi

if ! grep -q 'CreateUserRequest' postman/FoundationKit.Workbench.postman_collection.json; then
  echo "Workbench Postman collection must document the user request contract." >&2
  exit 1
fi

if ! grep -q 'clientRequestId' postman/Athar.Api.postman_collection.json; then
  echo "Athar Postman collection must verify idempotent creation." >&2
  exit 1
fi

if ! grep -q 'X-CSRF-TOKEN' postman/Athar.Api.postman_collection.json; then
  echo "Athar Postman collection must demonstrate anti-CSRF protection." >&2
  exit 1
fi

if ! grep -q 'AddIdentity' examples/Athar/Athar.Api/Program.cs; then
  echo "Athar must configure ASP.NET Core Identity." >&2
  exit 1
fi

if ! grep -q 'AddRateLimiter' examples/Athar/Athar.Api/Program.cs; then
  echo "Athar must configure rate limiting." >&2
  exit 1
fi

if ! grep -q 'AddAntiforgery' examples/Athar/Athar.Api/Program.cs; then
  echo "Athar must configure anti-CSRF protection." >&2
  exit 1
fi

if ! grep -q 'RowVersion' examples/Athar/Athar.Domain/Initiative.cs; then
  echo "Athar aggregate must expose optimistic concurrency." >&2
  exit 1
fi

if ! grep -q 'AuditEntries' examples/Athar/Athar.Infrastructure/AtharDbContext.cs; then
  echo "Athar must persist audit entries." >&2
  exit 1
fi

if ! grep -q 'ViewModelBase' examples/Athar/Athar.Client/ViewModels/ViewModels.cs; then
  echo "Athar must demonstrate Blazor-oriented MVVM." >&2
  exit 1
fi

if ! grep -q 'EntityDto' examples/Athar/Athar.Contracts/Contracts.cs; then
  echo "Athar must demonstrate generic DTO bases." >&2
  exit 1
fi

echo "FoundationKit, Workbench, and Athar repository verification passed."
