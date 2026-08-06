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
    ! -name 'samples' \
    ! -name 'scripts' \
    ! -name 'site' \
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
  "catalog/foundationkit.catalog.json"
  "docs/FEATURES.md"
  "docs/WORKBENCH.md"
  "samples/FoundationKit.Workbench/Program.cs"
  "samples/FoundationKit.Workbench/Infrastructure/Migrations/20260806113000_InitialWorkbench.cs"
  "site/index.html"
  "site/app.js"
  "deploy/docker-compose.yml"
)

for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Required repository file is missing: $required_file" >&2
    exit 1
  fi
done

if ! grep -q 'GitHub Pages Demo' site/index.html; then
  echo "Static site must state that GitHub Pages is a demo without backend persistence." >&2
  exit 1
fi

if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' samples/FoundationKit.Workbench/FoundationKit.Workbench.csproj; then
  echo "Workbench must explicitly own the SQL Server provider dependency." >&2
  exit 1
fi

echo "Repository boundary verification passed."
