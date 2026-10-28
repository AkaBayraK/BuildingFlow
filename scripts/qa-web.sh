#!/usr/bin/env bash
# QA FAZ 04 browser run: a disposable SUPER_ADMIN account in the local realm, the CDP crawler, then account removal.
# Read-only against the local stack: pages are only opened; the one write is a QA centre run (test evidence table).
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env
set +a
export BF_QA_WEB_USERNAME="bf-qa-web-$(openssl rand -hex 6)"
export BF_QA_WEB_PASSWORD="A1$(openssl rand -hex 16)"
export BF_QA_RUN_ID="${BF_QA_RUN_ID:-$(python3 -c 'import importlib.util as u;s=u.spec_from_file_location("g","scripts/qa_guard.py");m=u.module_from_spec(s);s.loader.exec_module(m);print(m.new_run_id())')}"
user_id=""
admin() {
  docker exec -i -e BF_QA_WEB_USERNAME -e BF_QA_WEB_PASSWORD "${KEYCLOAK_INTERNAL_HOST:-keycloak-26.7.5}" sh -ec '
    cfg=$(mktemp)
    trap '\''rm -f "$cfg"'\'' EXIT
    kc=/opt/keycloak/bin/kcadm.sh
    "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$SHARED_ADMIN_USERNAME" --password "$SHARED_ADMIN_PASSWORD" >/dev/null 2>&1
    case "$1" in
      create)
        id=$("$kc" create users --config "$cfg" -r buildingflow -i -s "username=$BF_QA_WEB_USERNAME" -s "email=$BF_QA_WEB_USERNAME@example.test" -s enabled=true -s emailVerified=true -s firstName=QA -s lastName=Robot)
        "$kc" set-password --config "$cfg" -r buildingflow --userid "$id" --new-password "$BF_QA_WEB_PASSWORD" >/dev/null 2>&1
        "$kc" add-roles --config "$cfg" -r buildingflow --uid "$id" --rolename SUPER_ADMIN >/dev/null 2>&1
        printf "%s" "$id";;
      delete) "$kc" delete "users/$2" --config "$cfg" -r buildingflow >/dev/null;;
      exists) "$kc" get users --config "$cfg" -r buildingflow -q "username=$BF_QA_WEB_USERNAME" -q exact=true --fields id;;
    esac
  ' sh "$@"
}
cleanup() {
  if [ -n "$user_id" ]; then
    admin delete "$user_id"
    if admin exists | grep -q '"id"'; then echo '[HATA] Geçici QA kullanıcısı silinemedi.' >&2; exit 1; fi
    echo '[QA WEB] Geçici Keycloak test kullanıcısı silindi.'
  fi
}
trap cleanup EXIT
user_id=$(admin create)
node scripts/qa-web-e2e.mjs
