#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
printf '[BUILDINGFLOW][WEBSITE] Test/typecheck/build/public HTTP isolation gates\n'
npm run setup --prefix website
npm test --prefix website
npm run typecheck --prefix website
npm run build --prefix website
npm run test:smoke --prefix website
node website/scripts/release.mjs --sbom artifacts/website/npm.cdx.json
