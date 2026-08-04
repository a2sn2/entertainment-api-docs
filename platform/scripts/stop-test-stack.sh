#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
docker compose -f "$ROOT_DIR/platform/deploy/docker-compose.test.yml" down --remove-orphans
