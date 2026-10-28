#!/usr/bin/env bash
# QA FAZ 05: throwaway Keycloak realm (bf-qa-*) cloned from the realm definition without users or secrets,
# one user per role plus a multi-role user, protocol/isolation checks, then realm removal. The buildingflow realm is untouched.
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env
set +a
export BF_QA_REALM="bf-qa-$(openssl rand -hex 4)"
export BF_QA_RUN_ID="${BF_QA_RUN_ID:-$(python3 -c 'import importlib.util as u;s=u.spec_from_file_location("g","scripts/qa_guard.py");m=u.module_from_spec(s);s.loader.exec_module(m);print(m.new_run_id())')}"
realm_json=$(python3 - <<'PY'
import json, os
realm = json.load(open('deploy/keycloak/realm-buildingflow.json'))
realm.update(realm=os.environ['BF_QA_REALM'], enabled=True)
for key in ('users', 'smtpServer'): realm.pop(key, None)
realm['clients'] = [client for client in realm['clients'] if client['clientId'] in ('buildingflow-web', 'buildingflow-api')]
print(json.dumps(realm))
PY
)
users_json=$(python3 -c 'import json,secrets;roles=[["SUPER_ADMIN"],["SITE_ADMIN"],["MANAGER"],["ACCOUNTANT"],["RESIDENT"],["SECURITY"],["MANAGER","RESIDENT"]];print(json.dumps([{"username":"qa-"+"-".join(r).lower().replace("_","")+"-"+secrets.token_hex(3),"password":"A1"+secrets.token_hex(16),"roles":r} for r in roles]))')
export BF_QA_USERS="$users_json"
kc() {
  docker exec -i -e BF_QA_REALM -e BF_QA_USERS "${KEYCLOAK_INTERNAL_HOST:-keycloak-26.7.5}" sh -ec '
    cfg=$(mktemp)
    trap '\''rm -f "$cfg"'\'' EXIT
    kc=/opt/keycloak/bin/kcadm.sh
    "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$SHARED_ADMIN_USERNAME" --password "$SHARED_ADMIN_PASSWORD" >/dev/null 2>&1
    case "$1" in
      create) "$kc" create realms --config "$cfg" -f - >/dev/null;;
      user)
        id=$("$kc" create users --config "$cfg" -r "$BF_QA_REALM" -i -s "username=$2" -s "email=$2@example.test" -s enabled=true -s emailVerified=true -s firstName=QA -s lastName=Robot)
        "$kc" set-password --config "$cfg" -r "$BF_QA_REALM" --userid "$id" --new-password "$3" >/dev/null 2>&1
        shift 3; for role in "$@"; do "$kc" add-roles --config "$cfg" -r "$BF_QA_REALM" --uid "$id" --rolename "$role" >/dev/null 2>&1; done;;
      delete) "$kc" delete "realms/$BF_QA_REALM" --config "$cfg" >/dev/null;;
    esac
  ' sh "$@"
}
created=""
cleanup() {
  if [ -n "$created" ]; then
    kc delete
    code=$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:${KEYCLOAK_PORT}/realms/$BF_QA_REALM/.well-known/openid-configuration")
    [ "$code" = 404 ] || { echo "[HATA] QA realm silinemedi (HTTP $code)." >&2; exit 1; }
    echo "[QA KİMLİK] Geçici realm silindi (discovery HTTP 404)."
  fi
}
trap cleanup EXIT
printf '%s' "$realm_json" | kc create
created=1
python3 -c 'import json,os;[print(u["username"],u["password"]," ".join(u["roles"])) for u in json.loads(os.environ["BF_QA_USERS"])]' | while read -r name secret roles; do
  # shellcheck disable=SC2086
  kc user "$name" "$secret" $roles </dev/null
done
node scripts/qa-identity.mjs
