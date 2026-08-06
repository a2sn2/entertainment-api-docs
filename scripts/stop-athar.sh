#!/usr/bin/env bash
set -euo pipefail
docker compose -f deploy/athar-compose.yml down --remove-orphans
