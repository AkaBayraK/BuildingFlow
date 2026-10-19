#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
project_root="$PWD"
mode="${1:-}"
require() {
  command -v "$1" >/dev/null 2>&1 || { echo "Required command missing: $1. Install it and restart VS Code." >&2; exit 1; }
}
load_env() {
  [ -f .env ] || { echo 'Run the BuildingFlow: Prepare local development task first.' >&2; exit 1; }
  set -a
  source .env
  set +a
}
backend_env() {
  export ConnectionStrings__Database="Host=${POSTGRES_LOCAL_HOST:-localhost};Port=${POSTGRES_PORT};Search Path=${POSTGRES_SCHEMA:-buildingflow},extensions;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
  export Authentication__Authority="http://localhost:${KEYCLOAK_PORT}/realms/buildingflow"
  export Authentication__Audience=buildingflow-api
  export Cors__Origins__0="http://localhost:${WEB_PORT}"
  export ObjectStorage__Endpoint="http://127.0.0.1:${OBJECT_STORAGE_PORT:-9040}"
  export ObjectStorage__AccessKey="${OBJECT_STORAGE_ACCESS_KEY}"
  export ObjectStorage__SecretKey="${OBJECT_STORAGE_SECRET_KEY}"
  export ObjectStorage__Bucket="buildingflow-documents"
  export PersonCode__HmacKey="${PERSON_CODE_HMAC_KEY:-}"
  export Website__PreviewHmacKey="${WEBSITE_PREVIEW_HMAC_KEY:-}"
  export Website__InquiryHmacKey="${WEBSITE_INQUIRY_HMAC_KEY:-}"
  export Website__PlatformDomain="${WEBSITE_PLATFORM_DOMAIN:-}"
  export KeycloakAdmin__BaseUrl="http://localhost:${KEYCLOAK_PORT}"
  export KeycloakAdmin__ClientSecret="${BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET:-}"
  export ASPNETCORE_ENVIRONMENT=Development
  export ASPNETCORE_URLS="http://localhost:${API_PORT}"
  export Telemetry__Enabled="${BUILDINGFLOW_TELEMETRY_ENABLED:-true}"
  export Telemetry__Endpoint="http://localhost:${OTLP_PORT:-14318}"
  unset Authentication__MetadataAddress
}
api_listeners() {
  lsof -nP -t -iTCP:"$API_PORT" -sTCP:LISTEN 2>/dev/null | sort -u || true
}
check_api_port() {
  command -v lsof >/dev/null 2>&1 || return 0
  if [ -n "$(api_listeners)" ]; then
    echo "API başlatılamadı: $API_PORT portu zaten kullanımda." >&2
    echo 'Açık API terminalinde Ctrl+C kullanın. Bu projeye ait API için:' >&2
    echo '  bash scripts/dev.sh api-stop' >&2
    echo 'Ardından: bash scripts/dev.sh api' >&2
    return 1
  fi
}
stop_api() {
  require lsof
  require ps
  local listeners pid executable attempt
  listeners="$(api_listeners)"
  if [ -z "$listeners" ]; then
    echo "$API_PORT portunda çalışan API yok; port hazır."
    return 0
  fi
  # Validate every listener before stopping any process. Never kill by port alone.
  for pid in $listeners; do
    [[ "$pid" =~ ^[0-9]+$ ]] || { echo 'Geçersiz süreç kimliği; işlem durduruldu.' >&2; return 1; }
    executable="$(ps -p "$pid" -o comm= 2>/dev/null)" || {
      echo 'Süreç bilgisi okunamadı; hiçbir süreç durdurulmadı.' >&2; return 1;
    }
    case "$executable" in
      "$project_root"/backend/src/BuildingFlow.Api/bin/*/BuildingFlow.Api) ;;
      *)
        echo "$API_PORT portu bu projeye ait olmayan bir süreç tarafından kullanılıyor; süreç durdurulmadı." >&2
        echo 'İlgili uygulamayı kendi terminalinden durdurun.' >&2
        return 1
        ;;
    esac
  done
  for pid in $listeners; do kill -TERM "$pid"; done
  for attempt in {1..20}; do
    if [ -z "$(api_listeners)" ]; then
      echo "BuildingFlow API durduruldu; $API_PORT portu hazır."
      return 0
    fi
    sleep 0.25
  done
  echo 'API henüz kapanmadı. Terminalini kontrol edip tekrar deneyin.' >&2
  return 1
}
case "$mode" in
  infrastructure)
    require docker
    bash scripts/init-local.sh
    docker compose --env-file .env -f deploy/docker-compose.yml stop buildingflow-web buildingflow-api
    bash ../keycloak-26.7.5/scripts/up.sh
    python3 scripts/configure-object-storage.py
    docker compose --env-file .env -f deploy/docker-compose.yml up -d --wait redis rabbitmq seaweedfs-s3 grafana mailpit otel-collector prometheus loki blackbox
    bash scripts/configure-identity.sh
    ;;
  prepare)
    require dotnet
    require npm
    bash scripts/dev.sh infrastructure
    load_env
    backend_env
    (
      cd backend
      dotnet tool restore
      dotnet build BuildingFlow.sln
      dotnet ef database update --project src/BuildingFlow.Infrastructure
    )
    (cd frontend/buildingflow-web && npm ci)
    echo 'Ready. Run the FE + BE task.'
    ;;
  api)
    require dotnet
    load_env
    check_api_port
    backend_env
    # Entity changes become a migration first; the API applies pending migrations on startup (Database__AutoMigrate).
    bash scripts/db.sh sync
    export Database__AutoMigrate="${BUILDINGFLOW_AUTO_MIGRATE:-true}"
    cd backend
    exec dotnet run --project src/BuildingFlow.Api --no-launch-profile
    ;;
  api-stop)
    load_env
    stop_api
    ;;
  web)
    require npm
    load_env
    export API_INTERNAL_URL="http://localhost:${API_PORT}"
    export NEXT_PUBLIC_API_URL="http://localhost:${API_PORT}"
    export NEXT_PUBLIC_OIDC_AUTHORITY="http://localhost:${KEYCLOAK_PORT}/realms/buildingflow"
    export NEXT_PUBLIC_OIDC_CLIENT_ID=buildingflow-web
    cd frontend/buildingflow-web
    exec npm run dev -- --hostname localhost --port "$WEB_PORT"
    ;;
  website)
    require npm
    load_env
    export WEBSITE_API_ORIGIN="http://localhost:${API_PORT:-5005}"
    export WEBSITE_MANAGEMENT_ORIGIN="http://localhost:${WEB_PORT:-3003}"
    export WEBSITE_PUBLIC_ORIGIN="http://localhost:${WEBSITE_PORT:-3004}"
    npm run setup --prefix website
    cd website
    exec npm run dev -- --hostname localhost --port "${WEBSITE_PORT:-3004}"
    ;;
  worker)
    require dotnet
    load_env
    backend_env
    export RabbitMQ__Host=localhost
    export RabbitMQ__Port="${RABBITMQ_PORT:-5672}"
    export RabbitMQ__Username="$RABBITMQ_USER"
    export RabbitMQ__Password="$RABBITMQ_PASSWORD"
    cd backend
    exec dotnet run --project src/BuildingFlow.Worker
    ;;
  *)
    echo 'Usage: bash scripts/dev.sh {infrastructure|prepare|api|api-stop|web|website|worker}' >&2
    exit 2
    ;;
esac
