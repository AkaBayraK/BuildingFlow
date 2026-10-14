#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env
set +a
check() {
  local name="$1" url="$2" expected="$3" code
  code="$(curl -sS --max-time 10 -o /dev/null -w '%{http_code}' "$url")"
  if [ "$code" != "$expected" ]; then
    echo "FAIL $name: HTTP $code (expected $expected)" >&2
    exit 1
  fi
  echo "PASS $name: HTTP $code"
}
check 'Web' "http://localhost:${WEB_PORT}/" 200
check 'Web readiness' "http://localhost:${WEB_PORT}/health" 200
check 'API/PostgreSQL readiness' "http://localhost:${API_PORT}/health" 200
check 'API liveness' "http://localhost:${API_PORT}/health/live" 200
check 'OpenAPI' "http://localhost:${API_PORT}/openapi/v1.json" 200
check 'Protected endpoint without JWT' "http://localhost:${API_PORT}/api/v1/status" 401
code="$(curl -sS --max-time 10 -o /dev/null -w '%{http_code}' -H 'Authorization: Bearer malformed-token' "http://localhost:${API_PORT}/api/v1/status")"
[ "$code" = 401 ] || { echo "FAIL invalid JWT: HTTP $code" >&2; exit 1; }
echo 'PASS invalid JWT rejected: HTTP 401'
check 'Keycloak realm discovery' "http://localhost:${KEYCLOAK_PORT}/realms/buildingflow/.well-known/openid-configuration" 200
check 'Local reset-email inbox' "http://localhost:${MAILPIT_PORT:-8025}/api/v1/info" 200
check 'RabbitMQ management' "http://localhost:${RABBITMQ_UI_PORT}/" 200
# The S3 gateway rejects anonymous requests; files are served only through the API.
check 'SeaweedFS S3 authentication' "http://127.0.0.1:${OBJECT_STORAGE_PORT:-9040}/" 403
check 'Grafana health' "http://localhost:${GRAFANA_PORT}/api/health" 200
docker compose --env-file .env -f deploy/docker-compose.yml exec -T redis redis-cli ping | grep -qx PONG
echo 'PASS Redis: PONG'
docker compose --env-file .env -f deploy/docker-compose.yml exec -T rabbitmq rabbitmq-diagnostics -q ping
docker compose --env-file .env -f deploy/docker-compose.yml ps --status running --services | grep -qx buildingflow-worker
echo 'PASS Worker running'
