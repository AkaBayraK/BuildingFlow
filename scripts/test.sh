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
# Local runtime feature flags must not leak into test hosts, whose fixtures set their own defaults.
for flag in $(compgen -v | grep '^PersonRefactor__' || true); do unset "$flag"; done
if [ -d ../postgres-18.6 ]; then
  # Tests use their own temporary database and role on the shared server.
  shared_root="$(cd ../postgres-18.6 && pwd)"
  bash "$shared_root/scripts/up.sh"
  test_settings="$(mktemp /tmp/buildingflow-test-settings.XXXXXX)"
  python3 "$shared_root/scripts/test-db.py" create "$test_settings"
  source "$test_settings"
  cleanup_test_database() { python3 "$shared_root/scripts/test-db.py" cleanup "$test_settings"; }
elif [ "${CI:-}" = true ]; then
  # CI runners have no shared server: a throwaway container with the same name stands in for it,
  # so check-idempotent-sql.sh can reach it after the tests. The runner is discarded afterwards.
  docker network inspect "${SHARED_POSTGRES_NETWORK:-shared-postgres-net}" >/dev/null 2>&1 ||
    docker network create "${SHARED_POSTGRES_NETWORK:-shared-postgres-net}" >/dev/null
  if ! docker inspect postgres-18.6 >/dev/null 2>&1; then
    docker run -d --name postgres-18.6 --network "${SHARED_POSTGRES_NETWORK:-shared-postgres-net}" \
      -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" -p "127.0.0.1:${POSTGRES_PORT:-5436}:5432" postgres:18.6-alpine >/dev/null
  fi
  for _ in $(seq 60); do
    docker exec postgres-18.6 pg_isready -U postgres -h 127.0.0.1 >/dev/null 2>&1 && break
    sleep 1
  done
  # The shared server provides citext up front; installing it in template1's public schema gives every
  # database the tests create the same type, whatever schema search path a test uses.
  docker exec postgres-18.6 psql -U postgres -d template1 -v ON_ERROR_STOP=1 -qc 'CREATE EXTENSION IF NOT EXISTS citext SCHEMA public'
  docker exec postgres-18.6 psql -U postgres -v ON_ERROR_STOP=1 -qc 'DROP DATABASE IF EXISTS buildingflow_ci_test' -c 'CREATE DATABASE buildingflow_ci_test'
  BUILDINGFLOW_TEST_DATABASE="Host=127.0.0.1;Port=${POSTGRES_PORT:-5436};Database=buildingflow_ci_test;Username=postgres;Password=$POSTGRES_PASSWORD"
  cleanup_test_database() { :; }
  ci_runner=1
else
  echo 'Shared PostgreSQL project ../postgres-18.6 not found.' >&2
  exit 1
fi
# Every test class migrates its own schema and therefore gets its own Npgsql pool; idle connections must be released
# quickly or a full run exhausts the shared server's max_connections (PostgreSQL 53300).
BUILDINGFLOW_TEST_DATABASE="$BUILDINGFLOW_TEST_DATABASE;Maximum Pool Size=10;Connection Idle Lifetime=2;Connection Pruning Interval=1"
export BUILDINGFLOW_TEST_DATABASE
trap cleanup_test_database EXIT
python3 scripts/configure-object-storage.py
if [ -n "${ci_runner:-}" ]; then
  # Linux bind mounts keep host ownership; the gateway runs as the image's seaweed user (uid 1000).
  sudo chown 1000:1000 deploy/seaweedfs/private-s3.json
fi
docker compose --env-file .env -f deploy/docker-compose.yml up -d seaweedfs-s3 rabbitmq
# The S3 gateway accepts connections only after its filer is up; wait so storage tests do not race it.
for _ in $(seq 60); do
  curl -s -o /dev/null "http://127.0.0.1:${OBJECT_STORAGE_PORT:-9040}/" && break
  docker compose --env-file .env -f deploy/docker-compose.yml up -d seaweedfs-s3 >/dev/null 2>&1
  sleep 2
done
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
# Every run must prove it targets only throwaway, loopback test resources.
python3 scripts/qa_guard.py
cd backend
dotnet test BuildingFlow.sln -m:1 --disable-build-servers "$@"
