#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mode="${1:-help}"
usage() {
  cat <<'HELP'
Usage (project root): bash scripts/db.sh COMMAND
  list          List migration source history without connecting to a database
  status        Show applied/pending migrations in the LOCAL database in .env
  check         Fail if model changes have no migration
  add NAME      Generate a new migration; inspect it before applying
  sync          If entities changed, generate an additive AutoSchema_* migration (refuses drops/renames/type changes)
  sql           Export idempotent SQL to artifacts/database/buildingflow.sql
  update        Apply pending migrations to the LOCAL database in .env
HELP
}
case "$mode" in
  help|--help|-h) usage; exit 0 ;;
  list|status|check|sql|update|sync)
    [ "$#" -eq 1 ] || { usage >&2; exit 2; }
    ;;
  add)
    [ "$#" -eq 2 ] && [[ "$2" =~ ^[A-Za-z][A-Za-z0-9_]{0,79}$ ]] || {
      echo 'Provide a migration name, e.g. AddSiteDescription (letters, numbers, underscore).' >&2
      exit 2
    }
    ;;
  *) usage >&2; exit 2 ;;
esac
command -v dotnet >/dev/null 2>&1 || { echo '.NET 10 SDK is required.' >&2; exit 1; }
if [ "$mode" = update ] || [ "$mode" = status ]; then
  [ -f .env ] || { echo 'Run scripts/init-local.sh first.' >&2; exit 1; }
  set -a
  source .env
  set +a
  # This helper always targets local development; production uses deployment artifacts.
  export ConnectionStrings__Database="Host=${POSTGRES_LOCAL_HOST:-localhost};Port=${POSTGRES_PORT};Search Path=${POSTGRES_SCHEMA:-buildingflow},extensions;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
else
  # Generating/checking source and SQL requires no production credentials.
  unset ConnectionStrings__Database
fi
project="src/BuildingFlow.Infrastructure"
(
  cd backend
  dotnet tool restore
  case "$mode" in
    list) dotnet ef migrations list --no-connect --project "$project" ;;
    status) dotnet ef migrations list --project "$project" ;;
    check) dotnet ef migrations has-pending-model-changes --project "$project" ;;
    add) dotnet ef migrations add "$2" --project "$project" --output-dir Migrations ;;
    sql)
      mkdir -p ../artifacts/database
      dotnet ef migrations script --idempotent --project "$project" --output ../artifacts/database/buildingflow.sql
      ;;
    update) dotnet ef database update --project "$project" ;;
    sync)
      dotnet restore "$project" --verbosity quiet
      if dotnet ef migrations has-pending-model-changes --project "$project" >/dev/null 2>&1; then
        echo 'Entity model and migrations are in sync.'
        exit 0
      fi
      dir="$project/Migrations"
      snapshot="$dir/BuildingFlowDbContextModelSnapshot.cs"
      saved="$(mktemp)"
      cp "$snapshot" "$saved"
      name="AutoSchema_$(date +%Y%m%d%H%M%S)"
      dotnet ef migrations add "$name" --project "$project" --output-dir Migrations
      created="$(ls "$dir"/*_"$name".cs | grep -v Designer)"
      # Only additive changes are generated automatically; data-losing or ambiguous ones need a reviewed migration.
      if sed -n '/void Up(/,/void Down(/p' "$created" | grep -qE 'migrationBuilder\.(Drop|Rename|AlterColumn|AlterTable|Sql)'; then
        rm -f "$created" "${created%.cs}.Designer.cs"
        cp "$saved" "$snapshot"
        rm -f "$saved"
        echo "Refused: the change drops, renames or alters existing schema. Create a reviewed migration: bash scripts/db.sh add Name" >&2
        exit 1
      fi
      rm -f "$saved"
      echo "Generated $(basename "$created"). Review it and commit it with the entity change; it is applied when the API starts."
      ;;
  esac
)
