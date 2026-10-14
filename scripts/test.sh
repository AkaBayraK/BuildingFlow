#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ ! -f .env ]; then
  echo 'Run scripts/init-local.sh first.' >&2
  exit 1
fi
set -a
source .env
set +a
# Tests use their own temporary database and role on the shared server.
shared_root="$(cd ../postgres-18.6 && pwd)"
bash "$shared_root/scripts/up.sh"
python3 scripts/configure-object-storage.py
docker compose --env-file .env -f deploy/docker-compose.yml up -d seaweedfs-s3 rabbitmq
test_settings="$(mktemp /tmp/buildingflow-test-settings.XXXXXX)"
python3 "$shared_root/scripts/test-db.py" create "$test_settings"
source "$test_settings"
export BUILDINGFLOW_TEST_DATABASE
cleanup_test_database() { python3 "$shared_root/scripts/test-db.py" cleanup "$test_settings"; }
trap cleanup_test_database EXIT
export BUILDINGFLOW_TEST_STORAGE_ENDPOINT="http://127.0.0.1:${OBJECT_STORAGE_PORT:-9040}"
export BUILDINGFLOW_TEST_STORAGE_ACCESS_KEY="${OBJECT_STORAGE_ACCESS_KEY}"
export BUILDINGFLOW_TEST_STORAGE_SECRET_KEY="${OBJECT_STORAGE_SECRET_KEY}"
export BUILDINGFLOW_TEST_RABBITMQ_HOST="127.0.0.1"
export BUILDINGFLOW_TEST_RABBITMQ_PORT="${RABBITMQ_PORT:-5672}"
export BUILDINGFLOW_TEST_RABBITMQ_USERNAME="${RABBITMQ_USER}"
export BUILDINGFLOW_TEST_RABBITMQ_PASSWORD="${RABBITMQ_PASSWORD}"
# Throwaway Mailpit on random loopback ports; the developer Mailpit and its messages are left untouched.
mailpit="buildingflow-test-mailpit-$$"
docker run -d --rm --name "$mailpit" -p 127.0.0.1::1025 -p 127.0.0.1::8025 axllent/mailpit:v1.27.10 \
  --smtp-allowed-recipients '@example\.test$' >/dev/null
trap 'docker stop "$mailpit" >/dev/null 2>&1 || true; cleanup_test_database' EXIT
sleep 2
export BUILDINGFLOW_TEST_SMTP_PORT="$(docker port "$mailpit" 1025 | head -1 | cut -d: -f2)"
export BUILDINGFLOW_TEST_MAILPIT_URL="http://$(docker port "$mailpit" 8025 | head -1)"
cd backend
dotnet test BuildingFlow.sln -m:1 --disable-build-servers "$@"
