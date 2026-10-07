#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -f .env ]; then
  echo '.env already exists; keeping existing settings.'
  exit 0
fi
umask 077
cp .env.example .env
for key in POSTGRES_PASSWORD KEYCLOAK_ADMIN_PASSWORD KEYCLOAK_DB_PASSWORD RABBITMQ_PASSWORD MINIO_ROOT_PASSWORD GRAFANA_ADMIN_PASSWORD; do
  secret="$(openssl rand -hex 24)"
  # Hex secrets contain no shell or sed metacharacters.
  sed -i.bak "s/^${key}=.*/${key}=${secret}/" .env
  rm -f .env.bak
done
echo 'Created .env with unique local secrets. Do not commit it.'
