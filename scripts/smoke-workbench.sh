#!/usr/bin/env bash
set -euo pipefail

base_url="${WORKBENCH_URL:-http://localhost:8080}"

curl --fail --silent "$base_url/api/health" | grep -q 'healthy'
curl --fail --silent "$base_url/api/catalog" | grep -q 'FoundationKit.Domain'

response="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{
    "projectName":"CI Workbench",
    "projectType":"Internal platform",
    "audience":"Engineering team",
    "goal":"Verify that the real Workbench API persists a valid project brief in SQL Server.",
    "selectedCapabilityIds":["commands-queries","ef-repository"],
    "priorities":"Correctness and maintainability",
    "notes":"Automated public-safe smoke test"
  }' \
  "$base_url/api/build-briefs")"

echo "$response" | grep -q 'contactUrl'
id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$response")"
curl --fail --silent "$base_url/api/build-briefs/$id" | grep -q 'CI Workbench'

echo "Workbench SQL Server smoke test passed for brief $id."
