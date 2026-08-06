#!/usr/bin/env bash
set -euo pipefail

export ATHAR_SQL_PASSWORD="${ATHAR_SQL_PASSWORD:-AtharSql!$(openssl rand -hex 16)Aa1}"
export ATHAR_ADMIN_EMAIL="${ATHAR_ADMIN_EMAIL:-admin@athar.local}"
export ATHAR_ADMIN_PASSWORD="${ATHAR_ADMIN_PASSWORD:-AtharAdmin!$(openssl rand -hex 16)Aa1}"

docker compose -f deploy/athar-compose.yml up --build -d

echo
echo "Athar is starting at http://localhost:8090"
echo "Admin email: $ATHAR_ADMIN_EMAIL"
echo "Admin password: $ATHAR_ADMIN_PASSWORD"
echo "These credentials are temporary for the current local environment."
