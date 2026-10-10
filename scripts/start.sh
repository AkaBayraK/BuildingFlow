#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
bash scripts/init-local.sh
docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
bash scripts/configure-identity.sh
echo 'BuildingFlow: http://localhost:3003 (or WEB_PORT in .env)'
echo 'Check status: docker compose --env-file .env -f deploy/docker-compose.yml ps'
