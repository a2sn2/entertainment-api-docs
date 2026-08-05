#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
output_directory="${2:-artifacts/foundation}"
script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
platform_root="$(cd "$script_directory/.." && pwd)"
output_path="$platform_root/$output_directory"

mkdir -p "$output_path"
mapfile -t projects < <(find "$platform_root/core" -name 'FoundationKit.*.csproj' -type f | sort)

if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No FoundationKit projects were found." >&2
  exit 1
fi

for project in "${projects[@]}"; do
  echo "Packing $(basename "$project")..."
  dotnet pack "$project" --configuration "$configuration" --output "$output_path"
done

echo "FoundationKit packages created in: $output_path"
find "$output_path" -maxdepth 1 -name '*.nupkg' -printf '%f\n' | sort
