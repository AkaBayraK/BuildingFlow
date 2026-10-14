#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
sql="${1:?Usage: scripts/check-idempotent-sql.sh <migrations.sql>}"
set -a; source .env; set +a
db="buildingflow_ci_sql_$$"
compose=(docker compose --env-file .env -f deploy/docker-compose.yml)
psql_admin() { docker exec -i postgres-18.6 psql -U postgres -d postgres -v ON_ERROR_STOP=1 -qAt "$@"; }
# Only this throwaway database is created and dropped; application databases are never touched.
trap 'psql_admin -c "DROP DATABASE IF EXISTS \"$db\" WITH (FORCE)" >/dev/null' EXIT
psql_admin -c "CREATE DATABASE \"$db\"" >/dev/null
for pass in 1 2; do
  docker exec -i postgres-18.6 psql -U postgres -d "$db" -v ON_ERROR_STOP=1 -q < "$sql" >/dev/null
done
expected="$(find backend/src/BuildingFlow.Infrastructure/Migrations -name '2*.cs' ! -name '*.Designer.cs' | wc -l | tr -d ' ')"
applied="$(docker exec -i postgres-18.6 psql -U postgres -d "$db" -qAt -c 'SELECT count(*) FROM "__EFMigrationsHistory"')"
[ "$applied" = "$expected" ] || { echo "FAIL: idempotent SQL applied $applied of $expected migrations" >&2; exit 1; }
echo "[TEST] Idempotent migration SQL applied twice to a throwaway database ($applied migrations) — PASS"
