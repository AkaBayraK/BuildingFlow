#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Existing Keycloak jars only; no Maven, dependency download or container rebuild.
target=${1:?Pass an output jar path outside the source tree}
image=${BUILDINGFLOW_TEST_KEYCLOAK_IMAGE:-quay.io/keycloak/keycloak:26.7.5}
work=$(mktemp -d "${TMPDIR:-/tmp}/buildingflow-contact-build.XXXXXX")
container=
cleanup() { if [ -n "$container" ]; then docker rm "$container" >/dev/null; fi; rm -rf "$work"; }
trap cleanup EXIT
container=$(docker create --pull=never --entrypoint true "$image")
docker cp "$container:/opt/keycloak/lib/lib/main" "$work/lib" >/dev/null
mkdir "$work/classes"
javac --release 21 -cp "$work/lib/*" -d "$work/classes" deploy/keycloak/contact-notice/src/com/buildingflow/identity/ContactNoticeFactory.java
jar --create --file "$target" -C "$work/classes" . -C deploy/keycloak/contact-notice/resources .
echo '[BUILDINGFLOW] [TEST] Projeye ait Keycloak bildirim eklentisi mevcut 26.7.5 API ile derlendi.'
