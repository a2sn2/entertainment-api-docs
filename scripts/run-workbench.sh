#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker Desktop or Docker Engine is required." >&2
  exit 1
fi

export FOUNDATIONKIT_SQL_PASSWORD="${FOUNDATIONKIT_SQL_PASSWORD:-Fkit!$(date +%s)${RANDOM}Aa1}"
docker compose -f deploy/docker-compose.yml up --build -d

url="http://localhost:8080"
for attempt in $(seq 1 120); do
  if curl --fail --silent "$url/api/health" >/dev/null 2>&1; then
    echo "FoundationKit Workbench is ready: $url"
    if command -v xdg-open >/dev/null 2>&1; then
      xdg-open "$url" >/dev/null 2>&1 || true
    elif command -v open >/dev/null 2>&1; then
      open "$url" >/dev/null 2>&1 || true
    fi
    exit 0
  fi
  sleep 2
done

echo "Workbench did not become healthy. Recent logs:" >&2
docker compose -f deploy/docker-compose.yml logs --tail=200 >&2
exit 1
