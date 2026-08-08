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

sla_payload='{"title":"Critical SLA smoke case","description":"Critical case used only to prove configured SLA breach and idempotent escalation behavior in CI.","caseType":"operational-incident","priority":"critical"}'
sla_case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$sla_payload" \
  "$base_url/api/cases/")"
sla_case_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$sla_case_json")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["slaTargetUtc"] is not None, "Critical CI case did not snapshot an SLA target"; assert item["slaState"] == "active", item["slaState"]' <<< "$sla_case_json"

sleep 3

sla_evaluation="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"limit":50}' \
  "$base_url/api/cases/sla/evaluate")"
python3 -c 'import json,sys; result=json.load(sys.stdin); assert result["evaluatedCount"] >= 1; assert result["breachedCount"] >= 1' <<< "$sla_evaluation"

sla_case_after="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$sla_case_id")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["slaState"] == "breached", item["slaState"]; assert item["slaBreachedUtc"] is not None; assert item["escalatedUtc"] is not None' <<< "$sla_case_after"

sla_timeline="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$sla_case_id/timeline")"
python3 -c 'import json,sys; items=json.load(sys.stdin); actions=[item["action"] for item in items]; assert actions.count("madar.case.sla-breached") == 1, actions' <<< "$sla_timeline"

second_sla_evaluation="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"limit":50}' \
  "$base_url/api/cases/sla/evaluate")"
python3 -c 'import json,sys; result=json.load(sys.stdin); assert result["breachedCount"] == 0, result' <<< "$second_sla_evaluation"

sla_timeline_after="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$sla_case_id/timeline")"
python3 -c 'import json,sys; items=json.load(sys.stdin); actions=[item["action"] for item in items]; assert actions.count("madar.case.sla-breached") == 1, actions' <<< "$sla_timeline_after"

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
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["slaTargetUtc"] is not None; assert item["slaState"] == "active"' <<< "$case_json"

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

comment_body='Operator follow-up comment: internal smoke marker 8f6f0b28.'
comment_payload="$(python3 -c 'import json,sys; print(json.dumps({"body":sys.argv[1]}))' "$comment_body")"
comment_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d "$comment_payload" \
  "$base_url/api/cases/$case_id/comments")"
comment_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); print(item["id"])' <<< "$comment_json")"
python3 -c 'import json,sys; expected=sys.argv[1]; case_id=sys.argv[2]; item=json.load(sys.stdin); assert item["body"] == expected; assert item["caseId"] == case_id; assert item["authorDisplayName"]' "$comment_body" "$case_id" <<< "$comment_json"

comments_json="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id/comments")"
python3 -c 'import json,sys; comment_id=sys.argv[1]; expected=sys.argv[2]; items=json.load(sys.stdin); matches=[item for item in items if item["id"] == comment_id]; assert len(matches) == 1; assert matches[0]["body"] == expected; assert matches[0]["authorDisplayName"]' "$comment_id" "$comment_body" <<< "$comments_json"

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
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["slaState"] == "met", item["slaState"]' <<< "$resolved_json"

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
python3 -c 'import json,sys; marker=sys.argv[1]; items=json.load(sys.stdin); actions=[item["action"] for item in items]; assert len(items) >= 6, f"Expected at least 6 audit events, got {len(items)}"; assert "madar.case.created" in actions; assert "madar.case.assigned" in actions; assert actions.count("madar.case.transitioned") >= 3; assert actions.count("madar.case.comment-added") == 1; assert "madar.case.sla-breached" not in actions; serialized=json.dumps(items); assert marker not in serialized, "Comment body leaked into audit timeline"' "$comment_body" <<< "$timeline_json"

final_case="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$case_id")"
final_status="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["status"])' <<< "$final_case")"
test "$final_status" = "closed"

comments_after_close="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id/comments")"
python3 -c 'import json,sys; comment_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == comment_id for item in items), "Comment history disappeared after case close"' "$comment_id" <<< "$comments_after_close"

sensitive_payload='{"title":"Privileged access review","description":"Access request used to prove maker-checker approval before sensitive case resolution.","caseType":"access-request","priority":"high"}'
sensitive_case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$sensitive_payload" \
  "$base_url/api/cases/")"
sensitive_case_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$sensitive_case_json")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["caseType"] == "access-request"; assert item["status"] == "new"' <<< "$sensitive_case_json"

sensitive_assigned="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assign_payload" \
  "$base_url/api/cases/$sensitive_case_id/assignment")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "assigned"' <<< "$sensitive_assigned"

sensitive_in_progress="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"start-progress"}' \
  "$base_url/api/cases/$sensitive_case_id/transition")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "in-progress"' <<< "$sensitive_in_progress"

blocked_resolve_body="$workdir/blocked-resolve.json"
blocked_resolve_status="$(curl --silent --show-error \
  -o "$blocked_resolve_body" \
  --write-out '%{http_code}' \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"resolve"}' \
  "$base_url/api/cases/$sensitive_case_id/transition")"
test "$blocked_resolve_status" = "409"
grep -q 'Madar.Approval.Required' "$blocked_resolve_body"

approval_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H "X-CSRF-TOKEN: $operator_token" \
  -X POST \
  "$base_url/api/cases/$sensitive_case_id/approvals")"
approval_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); print(item["id"])' <<< "$approval_json")"
python3 -c 'import json,sys; case_id=sys.argv[1]; operator_id=sys.argv[2]; item=json.load(sys.stdin); assert item["caseId"] == case_id; assert item["requestedByUserId"] == operator_id; assert item["status"] == "pending"' "$sensitive_case_id" "$operator_id" <<< "$approval_json"

approval_notes='Approval decision notes marker 63fb410e: authorized product data only.'
approval_decision_payload="$(python3 -c 'import json,sys; print(json.dumps({"decision":"approve","notes":sys.argv[1]}))' "$approval_notes")"
approved_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$approval_decision_payload" \
  "$base_url/api/cases/$sensitive_case_id/approvals/$approval_id/decision")"
python3 -c 'import json,sys; expected=sys.argv[1]; item=json.load(sys.stdin); assert item["status"] == "approved"; assert item["decisionNotes"] == expected; assert item["reviewedByUserId"] is not None; assert item["decidedUtc"] is not None' "$approval_notes" <<< "$approved_json"

approval_history="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$sensitive_case_id/approvals")"
python3 -c 'import json,sys; approval_id=sys.argv[1]; expected=sys.argv[2]; items=json.load(sys.stdin); matches=[item for item in items if item["id"] == approval_id]; assert len(matches) == 1; assert matches[0]["status"] == "approved"; assert matches[0]["decisionNotes"] == expected' "$approval_id" "$approval_notes" <<< "$approval_history"

sensitive_resolved="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"resolve"}' \
  "$base_url/api/cases/$sensitive_case_id/transition")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "resolved"' <<< "$sensitive_resolved"

sensitive_closed="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"trigger":"close"}' \
  "$base_url/api/cases/$sensitive_case_id/transition")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "closed"' <<< "$sensitive_closed"

sensitive_timeline="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$sensitive_case_id/timeline")"
python3 -c 'import json,sys; marker=sys.argv[1]; items=json.load(sys.stdin); actions=[item["action"] for item in items]; assert actions.count("madar.case.approval-requested") == 1, actions; assert actions.count("madar.case.approval-decided") == 1, actions; serialized=json.dumps(items); assert marker not in serialized, "Approval decision notes leaked into audit timeline"' "$approval_notes" <<< "$sensitive_timeline"

approval_history_after_close="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$sensitive_case_id/approvals")"
python3 -c 'import json,sys; approval_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == approval_id and item["status"] == "approved" for item in items), "Approval history disappeared after case close"' "$approval_id" <<< "$approval_history_after_close"

echo "Madar readiness + SLA + comments + approvals + SQL/auth/case/audit smoke workflows passed for cases $sla_case_id, $case_id, and $sensitive_case_id"
