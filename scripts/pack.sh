#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
output="${2:-artifacts/packages}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$root"
rm -rf "$output"
mkdir -p "$output"

dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration "$configuration" --no-restore

mapfile -t projects < <(find src -mindepth 2 -maxdepth 2 -name 'FoundationKit.*.csproj' -type f | sort)
if [[ ${#projects[@]} -ne 10 ]]; then
  echo "Expected exactly ten FoundationKit package projects, found ${#projects[@]}." >&2
  exit 1
fi

for project in "${projects[@]}"; do
  dotnet pack "$project" \
    --configuration "$configuration" \
    --no-build \
    --output "$output"
done

package_count="$(find "$output" -maxdepth 1 -name '*.nupkg' ! -name '*.symbols.nupkg' | wc -l | tr -d ' ')"
symbol_count="$(find "$output" -maxdepth 1 -name '*.snupkg' | wc -l | tr -d ' ')"

if [[ "$package_count" -ne 10 || "$symbol_count" -ne 10 ]]; then
  echo "Expected ten packages and ten symbol packages; got $package_count and $symbol_count." >&2
  exit 1
fi

printf 'Created %s packages and %s symbol packages in %s\n' \
  "$package_count" "$symbol_count" "$output"
