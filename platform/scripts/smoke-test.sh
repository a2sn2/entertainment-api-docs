#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
ADMIN_EMAIL="${TEST_ADMIN_EMAIL:?TEST_ADMIN_EMAIL is required}"
ADMIN_PASSWORD="${TEST_ADMIN_PASSWORD:?TEST_ADMIN_PASSWORD is required}"
STAMP="$(date +%s)"
SLUG="smoke-$STAMP"
REFERENCE="API-SMOKE-$STAMP"

check_url() {
  curl --fail --silent --show-error "$1" >/dev/null
  echo "PASS: $2"
}

check_url "$BASE_URL/healthz" "gateway"
check_url "$BASE_URL/api/health" "api health"
check_url "$BASE_URL/client/" "client application"
check_url "$BASE_URL/admin/" "admin application"
check_url "$BASE_URL/docs/" "documentation portal"

LOGIN_PAYLOAD="$(python3 -c 'import json,os; print(json.dumps({"email":os.environ["TEST_ADMIN_EMAIL"],"password":os.environ["TEST_ADMIN_PASSWORD"]}))')"
LOGIN_RESPONSE="$(curl --fail --silent --show-error -H 'Content-Type: application/json' -d "$LOGIN_PAYLOAD" "$BASE_URL/api/v1/auth/login")"
TOKEN="$(printf '%s' "$LOGIN_RESPONSE" | python3 -c 'import json,sys; print(json.load(sys.stdin)["accessToken"])')"
[ -n "$TOKEN" ]
echo "PASS: identity login"

CREATE_RESPONSE="$(curl --fail --silent --show-error -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -d "{\"reference\":\"$REFERENCE\",\"slug\":\"$SLUG\",\"title\":\"Repository Smoke Test\"}" "$BASE_URL/api/v1/admin/documents")"
DOCUMENT_ID="$(printf '%s' "$CREATE_RESPONSE" | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')"
[ -n "$DOCUMENT_ID" ]
echo "PASS: create document"

curl --fail --silent --show-error -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -d '{"version":"1.0","content":"Repository integration smoke-test content."}' "$BASE_URL/api/v1/admin/documents/$DOCUMENT_ID/versions" >/dev/null
echo "PASS: add document version"

curl --fail --silent --show-error -X POST -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/v1/admin/documents/$DOCUMENT_ID/submit-review" >/dev/null
echo "PASS: submit for review"

curl --fail --silent --show-error -X POST -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/v1/admin/documents/$DOCUMENT_ID/publish" >/dev/null
echo "PASS: publish document"

curl --fail --silent --show-error "$BASE_URL/api/v1/documents/$SLUG" | grep -q 'Repository Smoke Test'
echo "PASS: public document query"
echo "All repository full-stack smoke tests passed."
