#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env
set +a
docker compose --env-file .env -f deploy/docker-compose.yml exec -T -e BF_WEB_PORT="$WEB_PORT" keycloak sh -ec '
  config=$(mktemp)
  trap '\''rm -f "$config"'\'' EXIT
  kc=/opt/keycloak/bin/kcadm.sh
  "$kc" config credentials --config "$config" --server http://localhost:8080 --realm master --user "$KC_BOOTSTRAP_ADMIN_USERNAME" --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null 2>&1
  client_id=$("$kc" get clients --config "$config" -r buildingflow -q clientId=buildingflow-web --fields id --format csv --noquotes)
  "$kc" update "clients/$client_id" --config "$config" -r buildingflow -s "redirectUris=[\"http://localhost:$BF_WEB_PORT/*\"]" -s "webOrigins=[\"http://localhost:$BF_WEB_PORT\"]"
'
echo 'Keycloak web client origin updated.'
