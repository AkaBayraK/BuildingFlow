#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Upgrade existing local .env files without replacing any service secrets.
umask 077
if ! grep -Eq '^BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET=.+$' .env; then
  secret="$(openssl rand -hex 32)"
  grep -v '^BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET=' .env > .env.tmp || true
  printf 'BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET=%s\n' "$secret" >> .env.tmp
  mv .env.tmp .env
fi
set -a
source .env
set +a
# Keycloak can take a few seconds to become ready after a recreate.
for attempt in {1..60}; do
  if curl -fsS "http://localhost:${KEYCLOAK_PORT}/realms/buildingflow/.well-known/openid-configuration" >/dev/null 2>&1; then break; fi
  sleep 1
done
docker exec -i -e BF_WEB_PORT="$WEB_PORT" -e BF_ADMIN_CLIENT_SECRET="$BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET" "${KEYCLOAK_INTERNAL_HOST:-keycloak-26.7.5}" sh -ec '
  cfg=$(mktemp)
  trap '\''rm -f "$cfg"'\'' EXIT
  kc=/opt/keycloak/bin/kcadm.sh
  "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$SHARED_ADMIN_USERNAME" --password "$SHARED_ADMIN_PASSWORD" >/dev/null 2>&1
  "$kc" update realms/buildingflow --config "$cfg" -s loginTheme=buildingflow -s resetPasswordAllowed=true -s internationalizationEnabled=true -s '\''supportedLocales=["tr","en"]'\'' -s defaultLocale=tr -s actionTokenGeneratedByUserLifespan=600 -s '\''passwordPolicy="length(12) and digits(1) and upperCase(1) and lowerCase(1)"'\'' -s '\''smtpServer={"host":"mailpit","port":"1025","from":"no-reply@buildingflow.local","fromDisplayName":"BuildingFlow","auth":"false","ssl":"false","starttls":"false"}'\''
  id=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-web --fields id --format csv --noquotes)
  "$kc" update "clients/$id" --config "$cfg" -r buildingflow -s "redirectUris=[\"http://localhost:$BF_WEB_PORT/\",\"http://localhost:$BF_WEB_PORT/login\"]" -s "webOrigins=[\"http://localhost:$BF_WEB_PORT\"]" -s "attributes.\"post.logout.redirect.uris\"=http://localhost:$BF_WEB_PORT/login"
  # Confidential service account for the API user-management screen: only view/query/manage-users, never realm-admin.
  admin=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-admin-api --fields id --format csv --noquotes)
  if [ -z "$admin" ]; then
    "$kc" create clients --config "$cfg" -r buildingflow -s clientId=buildingflow-admin-api -s "name=BuildingFlow user administration" -s enabled=true -s publicClient=false -s standardFlowEnabled=false -s implicitFlowEnabled=false -s directAccessGrantsEnabled=false -s serviceAccountsEnabled=true -s clientAuthenticatorType=client-secret >/dev/null 2>&1
    admin=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-admin-api --fields id --format csv --noquotes)
  fi
  "$kc" update "clients/$admin" --config "$cfg" -r buildingflow -s "secret=$BF_ADMIN_CLIENT_SECRET" -s standardFlowEnabled=false -s directAccessGrantsEnabled=false -s serviceAccountsEnabled=true >/dev/null 2>&1
  "$kc" add-roles --config "$cfg" -r buildingflow --uusername service-account-buildingflow-admin-api --cclientid realm-management --rolename view-users --rolename query-users --rolename manage-users >/dev/null 2>&1
'
echo 'Configured login theme, local SMTP, reset policy, web redirect URIs and the user-administration service account.'
