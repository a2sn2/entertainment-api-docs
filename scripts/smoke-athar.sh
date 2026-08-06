#!/usr/bin/env bash
set -euo pipefail

base_url="${ATHAR_URL:-http://localhost:8090}"
admin_email="${ATHAR_ADMIN_EMAIL:?ATHAR_ADMIN_EMAIL is required}"
admin_password="${ATHAR_ADMIN_PASSWORD:?ATHAR_ADMIN_PASSWORD is required}"
user_email="athar.user.$(date +%s)@example.test"
user_password="AtharUser!$(date +%s)Aa1"
cookies="$(mktemp)"
trap 'rm -f "$cookies"' EXIT

csrf() {
  curl --fail --silent \
    -c "$cookies" \
    -b "$cookies" \
    "$base_url/api/v1/security/antiforgery" \
    | python3 -c 'import json,sys; print(json.load(sys.stdin)["requestToken"])'
}

token="$(csrf)"

register="$(curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d "{
    \"email\":\"$user_email\",
    \"displayName\":\"مستخدم اختبار أثر\",
    \"password\":\"$user_password\"
  }" \
  "$base_url/api/v1/auth/register")"

echo "$register" | grep -q '"isAuthenticated":true'

token="$(csrf)"
created="$(curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d '{
    "clientRequestId":"11111111-2222-3333-4444-555555555555",
    "title":"مختبر تعلم متنقل",
    "summary":"مبادرة اختبارية تقدم جلسات تعليم رقمية عملية للمدارس الواقعة خارج مراكز المدن.",
    "category":"تعليم",
    "city":"صنعاء",
    "requestedBudget":25000,
    "targetBeneficiaries":320
  }' \
  "$base_url/api/v1/initiatives")"

initiative_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$created")"
echo "$created" | grep -q '"status":"submitted"'

duplicate="$(curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d '{
    "clientRequestId":"11111111-2222-3333-4444-555555555555",
    "title":"مختبر تعلم متنقل",
    "summary":"مبادرة اختبارية تقدم جلسات تعليم رقمية عملية للمدارس الواقعة خارج مراكز المدن.",
    "category":"تعليم",
    "city":"صنعاء",
    "requestedBudget":25000,
    "targetBeneficiaries":320
  }' \
  "$base_url/api/v1/initiatives")"

duplicate_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$duplicate")"
test "$initiative_id" = "$duplicate_id"

token="$(csrf)"
curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d '{}' \
  "$base_url/api/v1/auth/logout" >/dev/null

token="$(csrf)"
admin_login="$(curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d "{
    \"email\":\"$admin_email\",
    \"password\":\"$admin_password\",
    \"rememberMe\":false
  }" \
  "$base_url/api/v1/auth/login")"

echo "$admin_login" | grep -q 'Administrator'
curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  "$base_url/api/v1/admin/initiatives?page=1&pageSize=20&status=submitted" \
  | grep -q "$initiative_id"

token="$(csrf)"
reviewed="$(curl --fail --silent \
  -c "$cookies" -b "$cookies" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $token" \
  -d '{
    "decision":"approve",
    "notes":"تم اجتياز معايير الاختبار الآلي."
  }' \
  "$base_url/api/v1/admin/initiatives/$initiative_id/review")"

echo "$reviewed" | grep -q '"status":"approved"'

curl --fail --silent "$base_url/health/live" | grep -q 'healthy'
curl --fail --silent "$base_url/health/ready" | grep -q 'ready'
curl --fail --silent "$base_url/" | grep -q 'blazor.webassembly.js'
curl --fail --silent "$base_url/swagger/v1/swagger.json" | grep -q 'CreateAtharInitiative'

echo "Athar end-to-end smoke test passed for initiative $initiative_id."
