#!/usr/bin/env bash
set -euo pipefail

export ATHAR_SQL_PASSWORD="${ATHAR_SQL_PASSWORD:-AtharSql!$(openssl rand -hex 16)Aa1}"
export ATHAR_ADMIN_EMAIL="${ATHAR_ADMIN_EMAIL:-admin@athar.local}"
export ATHAR_ADMIN_PASSWORD="${ATHAR_ADMIN_PASSWORD:-AtharAdmin!$(openssl rand -hex 16)Aa1}"

mkdir -p .local
credential_file=".local/athar-bootstrap-admin.env"
umask 077
cat > "$credential_file" <<EOF
ATHAR_ADMIN_EMAIL=$ATHAR_ADMIN_EMAIL
ATHAR_ADMIN_PASSWORD=$ATHAR_ADMIN_PASSWORD
EOF
chmod 600 "$credential_file"

docker compose -f deploy/athar-compose.yml up --build -d

echo
echo "Athar is starting at http://localhost:8090"
echo "Admin email: $ATHAR_ADMIN_EMAIL"
echo "Bootstrap credentials are stored locally in $credential_file with owner-only permissions."
echo "Do not share, commit, or use these development credentials in production."
