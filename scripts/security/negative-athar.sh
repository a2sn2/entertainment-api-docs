#!/usr/bin/env bash
set -euo pipefail

base_url="${ATHAR_URL:-http://localhost:8090}"
admin_email="${ATHAR_ADMIN_EMAIL:?ATHAR_ADMIN_EMAIL is required}"
admin_password="${ATHAR_ADMIN_PASSWORD:?ATHAR_ADMIN_PASSWORD is required}"
stamp="$(date +%s)-$RANDOM"
user1_email="negative.user1.$stamp@example.test"
user2_email="negative.user2.$stamp@example.test"
mfa_email="negative.mfa.$stamp@example.test"
user_password="NegativeUser!${stamp}Aa1"

cookies1="$(mktemp)"
cookies2="$(mktemp)"
admin_cookies="$(mktemp)"
mfa_cookies="$(mktemp)"
trap 'rm -f "$cookies1" "$cookies2" "$admin_cookies" "$mfa_cookies"' EXIT

csrf() {
  local jar="$1"
  curl --fail --silent -c "$jar" -b "$jar" \
    "$base_url/api/v1/security/antiforgery" \
    | python3 -c 'import json,sys; print(json.load(sys.stdin)["requestToken"])'
}

http_code() {
  curl --silent --output /dev/null --write-out '%{http_code}' "$@"
}

totp() {
  python3 - "$1" <<'PY'
import base64
import hashlib
import hmac
import struct
import sys
import time

key = ''.join(sys.argv[1].split()).upper()
padding = '=' * ((8 - len(key) % 8) % 8)
secret = base64.b32decode(key + padding)
counter = int(time.time()) // 30
message = struct.pack('>Q', counter)
digest = hmac.new(secret, message, hashlib.sha1).digest()
offset = digest[-1] & 0x0F
value = (struct.unpack('>I', digest[offset:offset + 4])[0] & 0x7FFFFFFF) % 1000000
print(f'{value:06d}')
PY
}

# Anonymous users must not reach administrator data.
code="$(http_code "$base_url/api/v1/admin/dashboard")"
test "$code" = "401"

# A state-changing request without antiforgery evidence must be rejected.
code="$(http_code \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$user1_email\",\"displayName\":\"اختبار سلبي\",\"password\":\"$user_password\"}" \
  "$base_url/api/v1/auth/register")"
test "$code" = "400"

# Create first user and one owned initiative.
token="$(csrf "$cookies1")"
curl --fail --silent -c "$cookies1" -b "$cookies1" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"email\":\"$user1_email\",\"displayName\":\"مالك المبادرة\",\"password\":\"$user_password\"}" \
  "$base_url/api/v1/auth/register" >/dev/null

token="$(csrf "$cookies1")"
created="$(curl --fail --silent -c "$cookies1" -b "$cookies1" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"clientRequestId\":\"$(python3 -c 'import uuid; print(uuid.uuid4())')\",\"title\":\"مبادرة اختبار ملكية\",\"summary\":\"بيانات اختبار صناعية للتحقق من منع الوصول إلى مبادرة مستخدم آخر داخل اختبارات الأمن السلبية.\",\"category\":\"اختبار\",\"city\":\"صنعاء\",\"requestedBudget\":100,\"targetBeneficiaries\":10}" \
  "$base_url/api/v1/initiatives")"
initiative_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$created")"

token="$(csrf "$cookies1")"
curl --fail --silent -c "$cookies1" -b "$cookies1" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" -d '{}' \
  "$base_url/api/v1/auth/logout" >/dev/null

# A second ordinary user must not learn whether the first user's object exists.
token="$(csrf "$cookies2")"
curl --fail --silent -c "$cookies2" -b "$cookies2" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"email\":\"$user2_email\",\"displayName\":\"مستخدم ثان\",\"password\":\"$user_password\"}" \
  "$base_url/api/v1/auth/register" >/dev/null
code="$(http_code -c "$cookies2" -b "$cookies2" "$base_url/api/v1/initiatives/$initiative_id")"
test "$code" = "404"

token="$(csrf "$cookies2")"
curl --fail --silent -c "$cookies2" -b "$cookies2" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" -d '{}' \
  "$base_url/api/v1/auth/logout" >/dev/null

# Account recovery responses must not disclose account existence or return a token.
forgot_existing="$(curl --fail --silent -c "$cookies2" -b "$cookies2" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $(csrf "$cookies2")" \
  -d "{\"email\":\"$user1_email\"}" "$base_url/api/v1/auth/password/forgot")"
forgot_missing="$(curl --fail --silent -c "$cookies2" -b "$cookies2" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $(csrf "$cookies2")" \
  -d '{"email":"missing.account@example.test"}' "$base_url/api/v1/auth/password/forgot")"
test "$forgot_existing" = "$forgot_missing"
! grep -qi 'token\|passwordResetToken\|resetToken' <<< "$forgot_existing"

# Administrator login remains valid in Development, but maker-checker prevents self-review.
token="$(csrf "$admin_cookies")"
curl --fail --silent -c "$admin_cookies" -b "$admin_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"email\":\"$admin_email\",\"password\":\"$admin_password\",\"rememberMe\":false}" \
  "$base_url/api/v1/auth/login" >/dev/null

token="$(csrf "$admin_cookies")"
admin_created="$(curl --fail --silent -c "$admin_cookies" -b "$admin_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"clientRequestId\":\"$(python3 -c 'import uuid; print(uuid.uuid4())')\",\"title\":\"مبادرة مسؤول للاختبار\",\"summary\":\"بيانات اختبار صناعية للتحقق من أن المسؤول لا يستطيع اتخاذ قرار مراجعة على مبادرة يملكها بنفسه.\",\"category\":\"اختبار\",\"city\":\"صنعاء\",\"requestedBudget\":100,\"targetBeneficiaries\":10}" \
  "$base_url/api/v1/initiatives")"
admin_initiative_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$admin_created")"

token="$(csrf "$admin_cookies")"
code="$(http_code -c "$admin_cookies" -b "$admin_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d '{"decision":"approve","notes":"يجب رفض هذه المراجعة الذاتية."}' \
  "$base_url/api/v1/admin/initiatives/$admin_initiative_id/review")"
test "$code" = "403"

# Invalid antiforgery material must fail without processing the write.
code="$(http_code -c "$admin_cookies" -b "$admin_cookies" \
  -H 'Content-Type: application/json' -H 'X-CSRF-TOKEN: invalid-token' \
  -d '{"decision":"reject","notes":"رمز حماية غير صالح."}' \
  "$base_url/api/v1/admin/initiatives/$initiative_id/review")"
test "$code" = "400"

# MFA-sensitive changes require full reauthentication: password + fresh MFA proof.
token="$(csrf "$mfa_cookies")"
curl --fail --silent -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"email\":\"$mfa_email\",\"displayName\":\"اختبار MFA\",\"password\":\"$user_password\"}" \
  "$base_url/api/v1/auth/register" >/dev/null

token="$(csrf "$mfa_cookies")"
setup="$(curl --fail --silent -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"currentPassword\":\"$user_password\"}" \
  "$base_url/api/v1/auth/mfa/setup")"
shared_key="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["sharedKey"])' <<< "$setup")"
code_now="$(totp "$shared_key")"

token="$(csrf "$mfa_cookies")"
curl --fail --silent -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"code\":\"$code_now\"}" \
  "$base_url/api/v1/auth/mfa/enable" >/dev/null

# Password login must now demand a second factor and preserve the 2FA login session.
token="$(csrf "$mfa_cookies")"
code="$(http_code -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"email\":\"$mfa_email\",\"password\":\"$user_password\",\"rememberMe\":false}" \
  "$base_url/api/v1/auth/login")"
test "$code" = "401"

token="$(csrf "$mfa_cookies")"
code_now="$(totp "$shared_key")"
curl --fail --silent -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"code\":\"$code_now\",\"rememberMe\":false,\"rememberMachine\":false}" \
  "$base_url/api/v1/auth/login/2fa" >/dev/null

# Password alone is insufficient to remove MFA or rotate recovery codes.
token="$(csrf "$mfa_cookies")"
code="$(http_code -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"currentPassword\":\"$user_password\",\"code\":\"invalid-recovery-code\"}" \
  "$base_url/api/v1/auth/mfa/disable")"
test "$code" = "403"

token="$(csrf "$mfa_cookies")"
code="$(http_code -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"currentPassword\":\"$user_password\",\"code\":\"invalid-recovery-code\"}" \
  "$base_url/api/v1/auth/mfa/recovery-codes")"
test "$code" = "403"

# A fresh valid MFA proof permits the sensitive operation and invalidates the active session.
token="$(csrf "$mfa_cookies")"
code_now="$(totp "$shared_key")"
code="$(http_code -c "$mfa_cookies" -b "$mfa_cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $token" \
  -d "{\"currentPassword\":\"$user_password\",\"code\":\"$code_now\"}" \
  "$base_url/api/v1/auth/mfa/disable")"
test "$code" = "200"
code="$(http_code -c "$mfa_cookies" -b "$mfa_cookies" "$base_url/api/v1/initiatives/mine?page=1&pageSize=1")"
test "$code" = "401"

# Prove runtime fixed-window enforcement. Earlier auth requests consume the same IP bucket;
# continue until the middleware itself rejects with 429. This test deliberately runs last.
rate_limited=false
for _ in $(seq 1 20); do
  code="$(http_code \
    -H 'Content-Type: application/json' \
    -d '{"email":"rate.limit.invalid@example.test"}' \
    "$base_url/api/v1/auth/password/forgot")"
  if [ "$code" = "429" ]; then
    rate_limited=true
    break
  fi
  test "$code" = "400"
done
test "$rate_limited" = "true"

echo "Athar negative security integration tests passed (authz, CSRF, BOLA, account enumeration, maker-checker, MFA step-up, runtime 429 rate limiting)."
