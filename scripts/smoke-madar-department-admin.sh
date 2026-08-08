#!/usr/bin/env bash
set -euo pipefail

base_url="${MADAR_BASE_URL:-http://localhost:8100}"
: "${MADAR_ADMIN_EMAIL:?MADAR_ADMIN_EMAIL is required}"
: "${MADAR_ADMIN_PASSWORD:?MADAR_ADMIN_PASSWORD is required}"
: "${MADAR_OPERATOR_EMAIL:?MADAR_OPERATOR_EMAIL is required}"
: "${MADAR_SQL_PASSWORD:?MADAR_SQL_PASSWORD is required}"

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT
admin_cookie="${MADAR_ADMIN_COOKIE_FILE:-$workdir/admin.cookies}"

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

expect_status() {
  local expected="$1"
  shift
  local actual
  actual="$(curl --silent --show-error -o "$workdir/response.json" -w '%{http_code}' "$@")"
  if [ "$actual" != "$expected" ]; then
    echo "Expected HTTP $expected but received $actual" >&2
    cat "$workdir/response.json" >&2 || true
    exit 1
  fi
}

if [ -n "${MADAR_ADMIN_COOKIE_FILE:-}" ] \
  && [ -n "${MADAR_ADMIN_CSRF_TOKEN:-}" ] \
  && [ -n "${MADAR_OPERATOR_ID:-}" ]; then
  admin_token="$MADAR_ADMIN_CSRF_TOKEN"
  operator_id="$MADAR_OPERATOR_ID"
else
  admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
  python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Administrator" in item["roles"]' <<< "$admin_login"
  admin_token="$(csrf_token "$admin_cookie")"

  operators_json="$(curl --fail --silent --show-error \
    -b "$admin_cookie" \
    "$base_url/api/users/operators")"
  operator_id="$(python3 -c 'import json,sys; expected=sys.argv[1].lower(); items=json.load(sys.stdin); matches=[item for item in items if (item.get("email") or "").lower() == expected]; assert len(matches) == 1, matches; print(matches[0]["userId"])' "$MADAR_OPERATOR_EMAIL" <<< "$operators_json")"
fi

department_code="support-admin-$(date +%s)-$RANDOM"
create_department_payload="$(python3 -c 'import json,sys; print(json.dumps({"code":sys.argv[1],"name":"الدعم التشغيلي"}))' "$department_code")"
department_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_department_payload" \
  "$base_url/api/admin/departments/")"
department_id="$(python3 -c 'import json,sys; code=sys.argv[1]; item=json.load(sys.stdin); assert item["code"] == code; assert item["name"] == "الدعم التشغيلي"; assert item["isActive"] is True; assert item["createdUtc"] == item["updatedUtc"]; print(item["id"])' "$department_code" <<< "$department_json")"

admin_departments="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/admin/departments/")"
python3 -c 'import json,sys; department_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == department_id and item["isActive"] for item in items)' "$department_id" <<< "$admin_departments"

member_payload="$(python3 -c 'import json,sys; print(json.dumps({"userId":sys.argv[1]}))' "$operator_id")"
member_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$member_payload" \
  "$base_url/api/admin/departments/$department_id/members")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; item=json.load(sys.stdin); assert item["userId"] == operator_id; assert item["joinedUtc"] is not None' "$operator_id" <<< "$member_json"

expect_status 409 \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$member_payload" \
  "$base_url/api/admin/departments/$department_id/members"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item.get("code") == "Madar.DepartmentAdmin.MembershipAlreadyExists"' < "$workdir/response.json"

members_json="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/admin/departments/$department_id/members")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; items=json.load(sys.stdin); matches=[item for item in items if item["userId"] == operator_id]; assert len(matches) == 1' "$operator_id" <<< "$members_json"

create_case_payload='{"title":"Department administration guard case","description":"Case used to prove safe department deactivation and membership removal guards through real SQL Server.","caseType":"internal-service-request","priority":"medium"}'
case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_case_payload" \
  "$base_url/api/cases/")"
case_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "new"; print(item["id"])' <<< "$case_json")"

route_payload="$(python3 -c 'import json,sys; print(json.dumps({"departmentId":sys.argv[1]}))' "$department_id")"
curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$route_payload" \
  "$base_url/api/cases/$case_id/route" >/dev/null

deactivate_payload='{"name":"الدعم التشغيلي","isActive":false}'
expect_status 409 \
  -X PUT \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$deactivate_payload" \
  "$base_url/api/admin/departments/$department_id"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item.get("code") == "Madar.DepartmentAdmin.HasOpenCases"' < "$workdir/response.json"

assignment_payload="$(python3 -c 'import json,sys; print(json.dumps({"assigneeUserId":sys.argv[1]}))' "$operator_id")"
assigned_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assignment_payload" \
  "$base_url/api/cases/$case_id/assignment")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; item=json.load(sys.stdin); assert item["status"] == "assigned"; assert item["assignedToUserId"] == operator_id' "$operator_id" <<< "$assigned_json"

expect_status 409 \
  -X DELETE \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H "X-CSRF-TOKEN: $admin_token" \
  "$base_url/api/admin/departments/$department_id/members/$operator_id"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item.get("code") == "Madar.DepartmentAdmin.MemberHasOpenAssignments"' < "$workdir/response.json"

transition() {
  local trigger="$1"
  local payload
  payload="$(python3 -c 'import json,sys; print(json.dumps({"trigger":sys.argv[1]}))' "$trigger")"
  curl --fail --silent --show-error \
    -c "$admin_cookie" \
    -b "$admin_cookie" \
    -H 'Content-Type: application/json' \
    -H "X-CSRF-TOKEN: $admin_token" \
    -d "$payload" \
    "$base_url/api/cases/$case_id/transition"
}

started_json="$(transition start-progress)"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "in-progress"' <<< "$started_json"
resolved_json="$(transition resolve)"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "resolved"' <<< "$resolved_json"
closed_json="$(transition close)"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "closed"' <<< "$closed_json"

expect_status 204 \
  -X DELETE \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H "X-CSRF-TOKEN: $admin_token" \
  "$base_url/api/admin/departments/$department_id/members/$operator_id"

members_after_remove="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/admin/departments/$department_id/members")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; items=json.load(sys.stdin); assert all(item["userId"] != operator_id for item in items)' "$operator_id" <<< "$members_after_remove"

updated_department="$(curl --fail --silent --show-error \
  -X PUT \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"name":"الدعم المركزي","isActive":false}' \
  "$base_url/api/admin/departments/$department_id")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["name"] == "الدعم المركزي"; assert item["isActive"] is False; assert item["updatedUtc"] != item["createdUtc"]' <<< "$updated_department"

sqlcmd_path='/opt/mssql-tools18/bin/sqlcmd'
if ! docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver test -x "$sqlcmd_path"; then
  sqlcmd_path='/opt/mssql-tools/bin/sqlcmd'
fi

audit_rows="$(docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver \
  "$sqlcmd_path" -S localhost -U sa -P "$MADAR_SQL_PASSWORD" -C -d MadarDb \
  -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT [Action], JSON_VALUE([AttributesJson], '$.departmentId'), JSON_VALUE([AttributesJson], '$.userId') FROM [audit].[AuditEvents] WHERE [SubjectType] = 'Department' AND [SubjectId] = '$department_id' AND [Action] IN ('madar.department.created','madar.department.updated','madar.department.member-added','madar.department.member-removed') ORDER BY [OccurredAtUtc], [Id];")"

AUDIT_ROWS="$audit_rows" DEPARTMENT_ID="$department_id" OPERATOR_ID="$operator_id" python3 - <<'PY'
import os
rows = [line.strip().split('|') for line in os.environ['AUDIT_ROWS'].splitlines() if line.strip()]
actions = [row[0].strip() for row in rows]
required = {
    'madar.department.created',
    'madar.department.updated',
    'madar.department.member-added',
    'madar.department.member-removed',
}
assert required.issubset(set(actions)), rows
for action, department_id, user_id in rows:
    action = action.strip()
    department_id = department_id.strip()
    user_id = user_id.strip()
    if action in {'madar.department.member-added', 'madar.department.member-removed'}:
        assert department_id == os.environ['DEPARTMENT_ID'], rows
        assert user_id == os.environ['OPERATOR_ID'], rows
PY

echo "Madar department administration SQL workflow passed for department $department_id and case $case_id"
