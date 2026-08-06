$ErrorActionPreference = "Stop"
docker compose -f deploy/athar-compose.yml down --remove-orphans
