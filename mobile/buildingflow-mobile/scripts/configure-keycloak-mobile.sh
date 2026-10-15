#!/bin/bash
set -euo pipefail
script_dir="$(cd "$(dirname "$0")" && pwd)"
docker exec -i keycloak-26.7.5 sh -ec '
 payload=$(mktemp); cfg=$(mktemp)
 trap '\''rm -f "$payload" "$cfg"'\'' EXIT
 cat > "$payload"
 kc=/opt/keycloak/bin/kcadm.sh
 "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$SHARED_ADMIN_USERNAME" --password "$SHARED_ADMIN_PASSWORD" >/dev/null 2>&1
 id=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-mobile --fields id --format csv --noquotes)
 if [ -z "$id" ]; then
  "$kc" create clients --config "$cfg" -r buildingflow -f "$payload" >/dev/null 2>&1
 else
  "$kc" update "clients/$id" --config "$cfg" -r buildingflow -f "$payload" >/dev/null 2>&1
 fi
 id=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-mobile --fields id --format csv --noquotes)
 basic=$("$kc" get client-scopes --config "$cfg" -r buildingflow --fields id,name --format csv --noquotes | while IFS=, read -r sid sname; do if [ "$sname" = "basic" ]; then printf "%s" "$sid"; fi; done)
 [ -n "$basic" ] || { echo "basic scope bulunamadı" >&2; exit 1; }
 "$kc" update "clients/$id/default-client-scopes/$basic" --config "$cfg" -r buildingflow >/dev/null 2>&1
 echo "BuildingFlow mobil public client hazır; S256, password grant kapalı."
' < "$script_dir/keycloak-client.dev.json"
