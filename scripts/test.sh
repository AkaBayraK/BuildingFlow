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
# Uses a separate database, never deletes application data.
docker compose --env-file .env -f deploy/docker-compose.yml up -d postgres minio rabbitmq
docker compose --env-file .env -f deploy/docker-compose.yml exec -T postgres sh -ec '
  if ! psql -U "$POSTGRES_USER" -d postgres -At -c "SELECT 1 FROM pg_database WHERE datname = '\''buildingflow_tests'\''" | grep -q 1; then
    createdb -U "$POSTGRES_USER" buildingflow_tests
  fi'
export BUILDINGFLOW_TEST_DATABASE="Host=localhost;Port=${POSTGRES_PORT};Database=buildingflow_tests;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export BUILDINGFLOW_TEST_MINIO_ENDPOINT="127.0.0.1:${MINIO_PORT}"
export BUILDINGFLOW_TEST_MINIO_ACCESS_KEY="${MINIO_ROOT_USER}"
export BUILDINGFLOW_TEST_MINIO_SECRET_KEY="${MINIO_ROOT_PASSWORD}"
export BUILDINGFLOW_TEST_RABBITMQ_HOST="127.0.0.1"
export BUILDINGFLOW_TEST_RABBITMQ_PORT="${RABBITMQ_PORT:-5672}"
export BUILDINGFLOW_TEST_RABBITMQ_USERNAME="${RABBITMQ_USER}"
export BUILDINGFLOW_TEST_RABBITMQ_PASSWORD="${RABBITMQ_PASSWORD}"
cd backend
dotnet test BuildingFlow.sln -m:1
