#!/bin/sh
# Downloads the whisper.cpp model once into the /models volume and refuses to use it unless its SHA256 matches.
set -eu
case "${WHISPER_MODEL:-}" in ggml-*.bin) ;; *) echo "WHISPER_MODEL must look like ggml-<name>.bin" >&2; exit 1;; esac
echo "${WHISPER_MODEL_SHA256:-}" | grep -Eq '^[0-9a-f]{64}$' || { echo "WHISPER_MODEL_SHA256 must be a SHA-256 hex digest" >&2; exit 1; }
file="/models/$WHISPER_MODEL"
check() { echo "$WHISPER_MODEL_SHA256  $1" | sha256sum -c - >/dev/null 2>&1; }
if [ -f "$file" ] && check "$file"; then echo "whisper model present and verified: $WHISPER_MODEL"; exit 0; fi
tmp="$file.part"; rm -f "$tmp"
curl -fL --retry 3 --proto '=https' -o "$tmp" "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/$WHISPER_MODEL"
if ! check "$tmp"; then rm -f "$tmp"; echo "SHA256 mismatch for $WHISPER_MODEL; model rejected" >&2; exit 1; fi
mv "$tmp" "$file"
echo "whisper model downloaded and verified: $WHISPER_MODEL"
