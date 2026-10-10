#!/usr/bin/env bash
set -euo pipefail
# Kept for compatibility; also updates logout URLs and local identity settings.
exec bash "$(dirname "$0")/configure-identity.sh"
