#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/platform/deploy/docker-compose.test.yml"

docker compose -f "$COMPOSE_FILE" up --build -d

echo "Waiting for the repository test gateway..."
for attempt in {1..60}; do
  if curl --fail --silent http://localhost:8080/healthz >/dev/null; then
    echo "Test stack is ready."
    echo "Client: http://localhost:8080/client/"
    echo "Admin:  http://localhost:8080/admin/"
    echo "Docs:   http://localhost:8080/docs/"
    echo "API:    http://localhost:8080/api/health"
    exit 0
  fi
  sleep 2
done

docker compose -f "$COMPOSE_FILE" ps
docker compose -f "$COMPOSE_FILE" logs --no-color --tail=200
exit 1
