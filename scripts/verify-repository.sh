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
    ! -name 'catalog' \
    ! -name 'deploy' \
    ! -name 'docs' \
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
  "samples/FoundationKit.Workbench.Client/Services/WorkbenchApiClient.cs"
  "samples/FoundationKit.Workbench.Contracts/FoundationKit.Workbench.Contracts.csproj"
  "samples/FoundationKit.Workbench.Contracts/User/UserContracts.cs"
  "samples/FoundationKit.Workbench.Contracts/Admin/AdminContracts.cs"
  "samples/FoundationKit.Workbench.Contracts/Workflow/WorkflowContracts.cs"
  "postman/FoundationKit.Workbench.postman_collection.json"
  "deploy/docker-compose.yml"
)

for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Required repository file is missing: $required_file" >&2
    exit 1
  fi
done

api_project="samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
client_project="samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
postman_collection="postman/FoundationKit.Workbench.postman_collection.json"

if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' "$api_project"; then
  echo "Workbench API must explicitly own the SQL Server provider dependency." >&2
  exit 1
fi

if ! grep -q 'FoundationKit.Workbench.Contracts' "$api_project"; then
  echo "Workbench API must reference the shared Contracts project." >&2
  exit 1
fi

if ! grep -q 'FoundationKit.Workbench.Client' "$api_project"; then
  echo "Workbench API must host the Blazor WebAssembly client." >&2
  exit 1
fi

if ! grep -q 'MudBlazor' "$client_project"; then
  echo "Workbench client must use MudBlazor." >&2
  exit 1
fi

if ! grep -q 'FoundationKit.Blazor' "$client_project"; then
  echo "Workbench client must consume the reusable FoundationKit.Blazor package." >&2
  exit 1
fi

if grep -RIl -- 'Microsoft.EntityFrameworkCore\|Microsoft.EntityFrameworkCore.SqlServer' \
    samples/FoundationKit.Workbench.Client \
    samples/FoundationKit.Workbench.Contracts \
    --include='*.cs' --include='*.csproj' | grep -q .; then
  echo "Client and transport contracts must not reference EF Core or SQL Server." >&2
  exit 1
fi

if ! grep -q 'CreateUserRequest' "$postman_collection"; then
  echo "Postman collection must document the user request contract." >&2
  exit 1
fi

if ! grep -q 'AdminReviewRequest' "$postman_collection"; then
  echo "Postman collection must document the admin review contract." >&2
  exit 1
fi

if ! grep -q '/api/user/requests' samples/FoundationKit.Workbench/Endpoints/UserPortalEndpoints.cs; then
  echo "User vertical slice must expose a dedicated user route group." >&2
  exit 1
fi

if ! grep -q '/api/admin' samples/FoundationKit.Workbench/Endpoints/AdminPortalEndpoints.cs; then
  echo "Admin vertical slice must expose a dedicated admin route group." >&2
  exit 1
fi

if ! grep -q 'AdminReviews' samples/FoundationKit.Workbench/Infrastructure/Migrations/20260806164000_DualPortalWorkflow.cs; then
  echo "Dual-stack migration must persist the admin review side of the workflow." >&2
  exit 1
fi

if ! grep -q 'USER FULL STACK' samples/FoundationKit.Workbench.Client/Pages/UserPortal.razor; then
  echo "User portal must identify its full-stack responsibility clearly." >&2
  exit 1
fi

if ! grep -q 'ADMIN FULL STACK' samples/FoundationKit.Workbench.Client/Pages/AdminPortal.razor; then
  echo "Admin portal must identify its full-stack responsibility clearly." >&2
  exit 1
fi

echo "Dual full-stack repository boundary verification passed."
