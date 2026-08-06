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
      --exclude='verify-core-only.sh' \
      -- "$pattern" . || true
  )"

  if [[ -n "$matches" ]]; then
    echo "Forbidden product-specific trace '$pattern' found in:" >&2
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
    ! -name 'docs' \
    ! -name 'global.json' \
    ! -name 'scripts' \
    ! -name 'src' \
    ! -name 'tests' \
    -print
)"

if [[ -n "$unexpected_top_level" ]]; then
  echo "Unexpected top-level entries found:" >&2
  echo "$unexpected_top_level" >&2
  exit 1
fi

echo "Core-only repository verification passed."
