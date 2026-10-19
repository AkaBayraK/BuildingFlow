#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -f .env ]; then
  echo '.env already exists; keeping existing settings.'
  python3 scripts/configure-object-storage.py
  exit 0
fi
umask 077
cp .env.example .env
for key in POSTGRES_PASSWORD KEYCLOAK_ADMIN_PASSWORD KEYCLOAK_DB_PASSWORD RABBITMQ_PASSWORD OBJECT_STORAGE_SECRET_KEY GRAFANA_ADMIN_PASSWORD PERSON_CODE_HMAC_KEY; do
  secret="$(openssl rand -hex 24)"
  # Hex secrets contain no shell or sed metacharacters.
  sed -i.bak "s/^${key}=.*/${key}=${secret}/" .env
  rm -f .env.bak
done
echo 'Created .env with unique local secrets. Do not commit it.'

python3 scripts/configure-object-storage.py
