#!/bin/bash
set -euo pipefail
script_dir="$(cd "$(dirname "$0")" && pwd)"
cd "${BUILDINGFLOW_BACKEND_ROOT:-$script_dir/../../..}"
: "${BUILDINGFLOW_MOBILE_HOST:?Set BUILDINGFLOW_MOBILE_HOST to your computer LAN address}"
set -a
source .env
set +a
export ConnectionStrings__Database="Host=${POSTGRES_LOCAL_HOST:-localhost};Port=${POSTGRES_PORT};Search Path=${POSTGRES_SCHEMA:-buildingflow},extensions;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export Authentication__Authority="http://localhost:${KEYCLOAK_PORT}/realms/buildingflow"
export Authentication__Audience=buildingflow-api
export Authentication__DevelopmentIssuers__0="http://${BUILDINGFLOW_MOBILE_HOST}:${KEYCLOAK_PORT}/realms/buildingflow"
export Cors__Origins__0=http://localhost:8081
export Cors__Origins__1=http://127.0.0.1:8081
export Cors__Origins__2=http://localhost:8763
export Cors__Origins__3=http://127.0.0.1:8763
export ObjectStorage__Endpoint="http://127.0.0.1:${OBJECT_STORAGE_PORT:-9040}"
export ObjectStorage__AccessKey="$OBJECT_STORAGE_ACCESS_KEY"
export ObjectStorage__SecretKey="$OBJECT_STORAGE_SECRET_KEY"
export ObjectStorage__Bucket="buildingflow-documents"
# Same server secret and identity client as the central API: person codes hash identically and mobile sign-up can create accounts.
export PersonCode__HmacKey="${PERSON_CODE_HMAC_KEY:-}"
export KeycloakAdmin__BaseUrl="http://localhost:${KEYCLOAK_PORT}"
export KeycloakAdmin__ClientSecret="${BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET:-}"
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://0.0.0.0:5006
export AllowedHosts="localhost;127.0.0.1;${BUILDINGFLOW_MOBILE_HOST}"
export Database__AutoMigrate=false
export Telemetry__Enabled=false
unset Authentication__MetadataAddress
export Logging__LogLevel__Default=Warning
dotnet run --no-build --no-launch-profile --project backend/src/BuildingFlow.Api
