#!/usr/bin/env bash
set -euo pipefail

base_url="${MADAR_BASE_URL:-http://localhost:8100}"
: "${MADAR_ADMIN_EMAIL:?MADAR_ADMIN_EMAIL is required}"
: "${MADAR_ADMIN_PASSWORD:?MADAR_ADMIN_PASSWORD is required}"
: "${MADAR_OPERATOR_EMAIL:?MADAR_OPERATOR_EMAIL is required}"
: "${MADAR_OPERATOR_PASSWORD:?MADAR_OPERATOR_PASSWORD is required}"

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT
admin_cookie="$workdir/admin.cookies"
operator_cookie="$workdir/operator.cookies"

csrf_token() {
  local cookie_file="$1"
  curl --fail --silent --show-error \
    -c "$cookie_file" \
    -b "$cookie_file" \
    "$base_url/api/security/antiforgery" \
    | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])'
}

login() {
  local cookie_file="$1"
  local email="$2"
  local password="$3"
  local token payload
  token="$(csrf_token "$cookie_file")"
  payload="$(python3 -c 'import json,sys; print(json.dumps({"email":sys.argv[1],"password":sys.argv[2],"rememberMe":False}))' "$email" "$password")"
  curl --fail --silent --show-error \
    -c "$cookie_file" \
    -b "$cookie_file" \
    -H 'Content-Type: application/json' \
    -H "X-CSRF-TOKEN: $token" \
    -d "$payload" \
    "$base_url/api/auth/login"
}

live_json="$(curl --fail --silent --show-error "$base_url/health/live")"
grep -q '"status":"healthy"' <<< "$live_json"

ready_json="$(curl --fail --silent --show-error "$base_url/health/ready")"
grep -q '"status":"ready"' <<< "$ready_json"
grep -q '"service":"madar-api"' <<< "$ready_json"
if grep -Eqi 'connection|string|password|server=|database=|sqlserver|data source' <<< "$ready_json"; then
  echo "Madar readiness response exposed infrastructure-sensitive details." >&2
  exit 1
fi

anonymous_status="$(curl --silent --output /dev/null --write-out '%{http_code}' "$base_url/api/cases")"
test "$anonymous_status" = "401"

admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
grep -q '"isAuthenticated":true' <<< "$admin_login"
grep -q '"Administrator"' <<< "$admin_login"
admin_token="$(csrf_token "$admin_cookie")"

create_payload='{"title":"Transfer investigation","description":"Customer transfer requires an operational investigation and controlled follow-up.","caseType":"operational-incident","priority":"high"}'
case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_payload" \
  "$base_url/api/cases/")"
case_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$case_json")"
initial_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$case_json")"
test "$initial_status" = "new"

operators_json="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/users/operators")"
operator_id="$(python3 -c 'import json,sys; items=json.load(sys.stdin); assert items, "No Madar operators returned"; print(items[0]["userId"])' <<< "$operators_json")"

assign_payload="$(python3 -c 'import json,sys; print(json.dumps({"assigneeUserId":sys.argv[1]}))' "$operator_id")"
assigned_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assign_payload" \
  "$base_url/api/cases/$case_id/assignment")"
assigned_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$assigned_json")"
test "$assigned_status" = "assigned"

operator_login="$(login "$operator_cookie" "$MADAR_OPERATOR_EMAIL" "$MADAR_OPERATOR_PASSWORD")"
grep -q '"isAuthenticated":true' <<< "$operator_login"
grep -q '"Operator"' <<< "$operator_login"
operator_token="$(csrf_token "$operator_cookie")"

operator_cases="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases")"
python3 -c 'import json,sys; case_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == case_id for item in items), "Assigned case is not visible to operator"' "$case_id" <<< "$operator_cases"

in_progress_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"start-progress"}' \
  "$base_url/api/cases/$case_id/transition")"
in_progress_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$in_progress_json")"
test "$in_progress_status" = "in-progress"

resolved_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"resolve"}' \
  "$base_url/api/cases/$case_id/transition")"
resolved_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$resolved_json")"
test "$resolved_status" = "resolved"

closed_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"trigger":"close"}' \
  "$base_url/api/cases/$case_id/transition")"
closed_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$closed_json")"
test "$closed_status" = "closed"

timeline_json="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$case_id/timeline")"
python3 -c 'import json,sys; items=json.load(sys.stdin); actions=[item["action"] for item in items]; assert len(items) >= 5, f"Expected at least 5 audit events, got {len(items)}"; assert "madar.case.created" in actions; assert "madar.case.assigned" in actions; assert actions.count("madar.case.transitioned") >= 3' <<< "$timeline_json"

final_case="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$case_id")"
final_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$final_case")"
test "$final_status" = "closed"

echo "Madar readiness + SQL/auth/case/audit smoke workflow passed for case $case_id"
