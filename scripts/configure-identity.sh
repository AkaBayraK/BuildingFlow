#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Upgrade existing local .env files without replacing any service secrets.
umask 077
if ! grep -q '^BUILDINGFLOW_DEV_USERNAME=' .env; then
  printf '\nBUILDINGFLOW_DEV_USERNAME=buildingflow-dev\n' >> .env
fi
if ! grep -q '^BUILDINGFLOW_DEV_EMAIL=' .env; then
  printf 'BUILDINGFLOW_DEV_EMAIL=dev@buildingflow.local\n' >> .env
fi
if ! grep -Eq '^BUILDINGFLOW_DEV_PASSWORD=.+$' .env; then
  secret="A1$(openssl rand -hex 16)"
  if grep -q '^BUILDINGFLOW_DEV_PASSWORD=' .env; then
    sed -i.bak "s/^BUILDINGFLOW_DEV_PASSWORD=.*/BUILDINGFLOW_DEV_PASSWORD=$secret/" .env
    rm -f .env.bak
  else
    printf 'BUILDINGFLOW_DEV_PASSWORD=%s\n' "$secret" >> .env
  fi
fi
set -a
source .env
set +a
# Keycloak can take a few seconds to become ready after a recreate.
for attempt in {1..60}; do
  if curl -fsS "http://localhost:${KEYCLOAK_PORT}/realms/buildingflow/.well-known/openid-configuration" >/dev/null 2>&1; then break; fi
  sleep 1
done
docker compose --env-file .env -f deploy/docker-compose.yml exec -T -e BF_WEB_PORT="$WEB_PORT" -e BF_DEV_USERNAME="$BUILDINGFLOW_DEV_USERNAME" -e BF_DEV_EMAIL="$BUILDINGFLOW_DEV_EMAIL" -e BF_DEV_PASSWORD="$BUILDINGFLOW_DEV_PASSWORD" keycloak sh -ec '
  cfg=$(mktemp)
  trap '\''rm -f "$cfg"'\'' EXIT
  kc=/opt/keycloak/bin/kcadm.sh
  "$kc" config credentials --config "$cfg" --server http://localhost:8080 --realm master --user "$KC_BOOTSTRAP_ADMIN_USERNAME" --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null 2>&1
  "$kc" update realms/buildingflow --config "$cfg" -s loginTheme=buildingflow -s resetPasswordAllowed=true -s internationalizationEnabled=true -s '\''supportedLocales=["tr","en"]'\'' -s defaultLocale=tr -s actionTokenGeneratedByUserLifespan=600 -s '\''passwordPolicy="length(12) and digits(1) and upperCase(1) and lowerCase(1)"'\'' -s '\''smtpServer={"host":"mailpit","port":"1025","from":"no-reply@buildingflow.local","fromDisplayName":"BuildingFlow","auth":"false","ssl":"false","starttls":"false"}'\''
  id=$("$kc" get clients --config "$cfg" -r buildingflow -q clientId=buildingflow-web --fields id --format csv --noquotes)
  "$kc" update "clients/$id" --config "$cfg" -r buildingflow -s "redirectUris=[\"http://localhost:$BF_WEB_PORT/\",\"http://localhost:$BF_WEB_PORT/login\"]" -s "webOrigins=[\"http://localhost:$BF_WEB_PORT\"]" -s "attributes.\"post.logout.redirect.uris\"=http://localhost:$BF_WEB_PORT/login"
  user=$("$kc" get users --config "$cfg" -r buildingflow -q "username=$BF_DEV_USERNAME" --fields id --format csv --noquotes)
  if [ -z "$user" ]; then
    "$kc" create users --config "$cfg" -r buildingflow -s "username=$BF_DEV_USERNAME" -s "email=$BF_DEV_EMAIL" -s enabled=true -s emailVerified=true -s firstName=BuildingFlow -s lastName=Development >/dev/null 2>&1
    "$kc" set-password --config "$cfg" -r buildingflow --username "$BF_DEV_USERNAME" --new-password "$BF_DEV_PASSWORD" >/dev/null 2>&1
    "$kc" add-roles --config "$cfg" -r buildingflow --uusername "$BF_DEV_USERNAME" --rolename SUPER_ADMIN >/dev/null 2>&1
  fi
'
echo 'Configured login theme, local SMTP, reset policy and web redirect URIs.'
