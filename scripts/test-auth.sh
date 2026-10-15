#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env
set +a
export BF_AUTH_TEST_USERNAME="bf-auth-test-$(openssl rand -hex 6)"
export BF_AUTH_TEST_EMAIL="$BF_AUTH_TEST_USERNAME@buildingflow.local"
export BF_AUTH_TEST_PASSWORD="A1$(openssl rand -hex 16)"
user_id=""
admin() {
  docker exec -i -e BF_AUTH_TEST_USERNAME -e BF_AUTH_TEST_EMAIL -e BF_AUTH_TEST_PASSWORD "${KEYCLOAK_INTERNAL_HOST:-keycloak-26.7.5}" sh -ec '
    cfg=$(mktemp)
    trap '\''rm -f "$cfg"'\'' EXIT
    kc=/opt/keycloak/bin/kcadm.sh
    "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$SHARED_ADMIN_USERNAME" --password "$SHARED_ADMIN_PASSWORD" >/dev/null 2>&1
    if [ "$1" = create ]; then
      id=$("$kc" create users --config "$cfg" -r buildingflow -i -s "username=$BF_AUTH_TEST_USERNAME" -s "email=$BF_AUTH_TEST_EMAIL" -s enabled=true -s emailVerified=true -s firstName=Authentication -s lastName=Test)
      "$kc" set-password --config "$cfg" -r buildingflow --userid "$id" --new-password "$BF_AUTH_TEST_PASSWORD" >/dev/null 2>&1
      printf "%s" "$id"
    else
      "$kc" delete "users/$2" --config "$cfg" -r buildingflow >/dev/null
    fi
  ' sh "$@"
}
cleanup() { if [ -n "$user_id" ]; then admin delete "$user_id"; fi; }
trap cleanup EXIT
user_id=$(admin create)
node scripts/auth-smoke.mjs
