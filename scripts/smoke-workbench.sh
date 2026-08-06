#!/usr/bin/env bash
set -euo pipefail

base_url="${WORKBENCH_URL:-http://localhost:8080}"

curl --fail --silent "$base_url/api/health" | grep -q 'healthy'
curl --fail --silent "$base_url/api/catalog" | grep -q 'FoundationKit.Domain'

user_response="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{
    "projectName":"CI Dual Portal",
    "projectType":"Internal platform",
    "audience":"Engineering team",
    "goal":"Verify the complete user and admin full-stack workflow against SQL Server.",
    "selectedCapabilityIds":["commands-queries","ef-repository"],
    "priorities":"Correctness and maintainability",
    "notes":"Automated public-safe smoke test"
  }' \
  "$base_url/api/user/requests")"

request_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$user_response")"
echo "$user_response" | grep -q '"status":"submitted"'

curl --fail --silent "$base_url/api/admin/requests?status=submitted" | grep -q 'CI Dual Portal'

review_response="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{
    "decision":"approve",
    "reviewedBy":"CI Admin",
    "notes":"Validated by the integration smoke test"
  }' \
  "$base_url/api/admin/requests/$request_id/review")"

echo "$review_response" | grep -q '"status":"approved"'
curl --fail --silent "$base_url/api/user/requests/$request_id" | grep -q '"status":"approved"'
curl --fail --silent "$base_url/api/admin/requests?status=approved" | grep -q 'CI Dual Portal'

echo "Dual full-stack SQL Server workflow passed for request $request_id."
