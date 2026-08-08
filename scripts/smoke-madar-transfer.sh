#!/usr/bin/env bash
set -euo pipefail

base_url="${MADAR_BASE_URL:-http://localhost:8100}"
: "${MADAR_ADMIN_EMAIL:?MADAR_ADMIN_EMAIL is required}"
: "${MADAR_ADMIN_PASSWORD:?MADAR_ADMIN_PASSWORD is required}"
: "${MADAR_OPERATOR_EMAIL:?MADAR_OPERATOR_EMAIL is required}"
: "${MADAR_OPERATOR_PASSWORD:?MADAR_OPERATOR_PASSWORD is required}"
: "${MADAR_SQL_PASSWORD:?MADAR_SQL_PASSWORD is required}"

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

admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
admin_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Administrator" in item["roles"]; print(item["userId"])' <<< "$admin_login")"
admin_token="$(csrf_token "$admin_cookie")"

operator_login="$(login "$operator_cookie" "$MADAR_OPERATOR_EMAIL" "$MADAR_OPERATOR_PASSWORD")"
operator_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Operator" in item["roles"]; print(item["userId"])' <<< "$operator_login")"
operator_token="$(csrf_token "$operator_cookie")"

sqlcmd_path='/opt/mssql-tools18/bin/sqlcmd'
if ! docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver test -x "$sqlcmd_path"; then
  sqlcmd_path='/opt/mssql-tools/bin/sqlcmd'
fi

# The ephemeral integration fixture gives the bootstrap Administrator the Operator
# role as well, allowing one independently authenticated user to be the second
# eligible assignee without introducing test-only product APIs or bootstrap config.
docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver \
  "$sqlcmd_path" -S localhost -U sa -P "$MADAR_SQL_PASSWORD" -C -d MadarDb \
  -Q "SET NOCOUNT ON; DECLARE @RoleId uniqueidentifier = (SELECT TOP (1) [Id] FROM [identity].[Roles] WHERE [NormalizedName] = 'OPERATOR'); IF @RoleId IS NULL THROW 51000, 'Operator role not found', 1; IF NOT EXISTS (SELECT 1 FROM [identity].[UserRoles] WHERE [UserId] = '$admin_id' AND [RoleId] = @RoleId) INSERT INTO [identity].[UserRoles] ([UserId], [RoleId]) VALUES ('$admin_id', @RoleId);" >/dev/null

departments_json="$(curl --fail --silent --show-error -b "$admin_cookie" "$base_url/api/departments")"
source_department_id="$(python3 -c 'import json,sys; items=json.load(sys.stdin); matches=[item for item in items if item["code"] == "operations" and item["isActive"]]; assert len(matches) == 1, matches; print(matches[0]["id"])' <<< "$departments_json")"

target_code="transfer-target-$(date +%s)-$RANDOM"
target_payload="$(python3 -c 'import json,sys; print(json.dumps({"code":sys.argv[1],"name":"قسم النقل التشغيلي"}))' "$target_code")"
target_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$target_payload" "$base_url/api/admin/departments/")"
target_department_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isActive"] is True; print(item["id"])' <<< "$target_json")"

member_payload="$(python3 -c 'import json,sys; print(json.dumps({"userId":sys.argv[1]}))' "$admin_id")"
for department_id in "$source_department_id" "$target_department_id"; do
  curl --fail --silent --show-error \
    -c "$admin_cookie" -b "$admin_cookie" \
    -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
    -d "$member_payload" \
    "$base_url/api/admin/departments/$department_id/members" >/dev/null
done

create_payload='{"title":"Controlled transfer SQL case","description":"Case used to prove reassignment and cross-department transfer without losing SLA or audit evidence.","caseType":"internal-service-request","priority":"medium"}'
case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_payload" "$base_url/api/cases/")"
case_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "new"; assert item["slaTargetUtc"] is not None; print(item["id"])' <<< "$case_json")"
original_sla_target="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["slaTargetUtc"])' <<< "$case_json")"

route_payload="$(python3 -c 'import json,sys; print(json.dumps({"departmentId":sys.argv[1]}))' "$source_department_id")"
curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$route_payload" "$base_url/api/cases/$case_id/route" >/dev/null

assign_payload="$(python3 -c 'import json,sys; print(json.dumps({"assigneeUserId":sys.argv[1]}))' "$operator_id")"
assigned_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assign_payload" "$base_url/api/cases/$case_id/assignment")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; item=json.load(sys.stdin); assert item["status"] == "assigned"; assert item["assignedToUserId"] == operator_id' "$operator_id" <<< "$assigned_json"

in_progress_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" -b "$operator_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"start-progress"}' "$base_url/api/cases/$case_id/transition")"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "in-progress"' <<< "$in_progress_json"

reassign_payload="$(python3 -c 'import json,sys; print(json.dumps({"assigneeUserId":sys.argv[1]}))' "$admin_id")"
reassigned_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$reassign_payload" "$base_url/api/cases/$case_id/reassignment")"
python3 -c 'import json,sys; admin_id,source_id,sla=sys.argv[1:4]; item=json.load(sys.stdin); assert item["status"] == "in-progress"; assert item["assignedToUserId"] == admin_id; assert item["departmentId"] == source_id; assert item["slaTargetUtc"] == sla' "$admin_id" "$source_department_id" "$original_sla_target" <<< "$reassigned_json"

transfer_payload="$(python3 -c 'import json,sys; print(json.dumps({"departmentId":sys.argv[1]}))' "$target_department_id")"
transferred_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$transfer_payload" "$base_url/api/cases/$case_id/transfer")"
python3 -c 'import json,sys; target_id,sla=sys.argv[1:3]; item=json.load(sys.stdin); assert item["status"] == "new"; assert item["departmentId"] == target_id; assert item["assignedToUserId"] is None; assert item["slaTargetUtc"] == sla' "$target_department_id" "$original_sla_target" <<< "$transferred_json"

target_queue="$(curl --fail --silent --show-error -b "$admin_cookie" "$base_url/api/departments/$target_department_id/queue")"
python3 -c 'import json,sys; case_id=sys.argv[1]; item=json.load(sys.stdin); assert any(case["id"] == case_id and case["status"] == "new" for case in item["cases"])' "$case_id" <<< "$target_queue"

claimed_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d '{}' "$base_url/api/cases/$case_id/claim")"
python3 -c 'import json,sys; admin_id,target_id,sla=sys.argv[1:4]; item=json.load(sys.stdin); assert item["status"] == "assigned"; assert item["assignedToUserId"] == admin_id; assert item["departmentId"] == target_id; assert item["slaTargetUtc"] == sla' "$admin_id" "$target_department_id" "$original_sla_target" <<< "$claimed_json"

timeline_json="$(curl --fail --silent --show-error -b "$admin_cookie" "$base_url/api/cases/$case_id/timeline")"
python3 -c 'import json,sys; source,target,operator_id,admin_id=sys.argv[1:5]; items=json.load(sys.stdin); reassign=[x for x in items if x["action"]=="madar.case.reassigned"]; transfer=[x for x in items if x["action"]=="madar.case.transferred"]; assert len(reassign)==1, reassign; assert len(transfer)==1, transfer; assert reassign[0]["attributes"]=={"previousAssigneeUserId":operator_id,"assigneeUserId":admin_id,"status":"in-progress","departmentId":source}, reassign[0]["attributes"]; assert transfer[0]["attributes"]=={"fromDepartmentId":source,"toDepartmentId":target,"previousStatus":"in-progress","previousAssigneeUserId":admin_id}, transfer[0]["attributes"]' "$source_department_id" "$target_department_id" "$operator_id" "$admin_id" <<< "$timeline_json"

persisted="$(docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver \
  "$sqlcmd_path" -S localhost -U sa -P "$MADAR_SQL_PASSWORD" -C -d MadarDb \
  -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT [Status], CONVERT(varchar(36), [DepartmentId]), CONVERT(varchar(36), [AssignedToUserId]) FROM [madar].[Cases] WHERE [Id] = '$case_id';")"
PERSISTED="$persisted" TARGET="$target_department_id" ADMIN="$admin_id" python3 - <<'PY'
import os
from uuid import UUID

parts = [part.strip() for part in os.environ['PERSISTED'].strip().split('|')]
assert len(parts) == 3, parts
assert parts[0] == 'assigned', parts
assert UUID(parts[1]) == UUID(os.environ['TARGET']), parts
assert UUID(parts[2]) == UUID(os.environ['ADMIN']), parts
PY

echo "Madar reassignment + transfer SQL workflow passed for case $case_id from $source_department_id to $target_department_id"
