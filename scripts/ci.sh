#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
printf '[BUILDINGFLOW]\n[FAZ İŞ11 | KAYIT14/14]\n[ADIM02/04] CI\n[DURUM] BAŞLADI\n'
[ "${1:-}" = "" ] || [ "$1" = --install ] || { echo 'Usage: bash scripts/ci.sh [--install]' >&2; exit 2; }
# Results from earlier runs must never satisfy the gates.
rm -rf artifacts/ci
mkdir -p artifacts/ci
if [ "${1:-}" = --install ]; then
  (cd frontend/buildingflow-web && npm ci)
  dotnet restore backend/BuildingFlow.sln --verbosity minimal
  (cd backend && dotnet tool restore)
fi
python3 scripts/check-licenses.py
python3 scripts/test_ci_gates.py
(cd frontend/buildingflow-web && npm test && npm run typecheck && npm run build && npm audit --audit-level=high)
# This runner prepares separate PG schemas and fixture-only MinIO/RabbitMQ/Mailpit.
bash scripts/test.sh --no-restore --logger trx --results-directory "$(pwd)/artifacts/ci/test-results"
python3 scripts/check-test-results.py artifacts/ci/test-results
(cd backend && dotnet ef migrations has-pending-model-changes --no-build --project src/BuildingFlow.Infrastructure && dotnet ef migrations script --idempotent --no-build --project src/BuildingFlow.Infrastructure --output ../artifacts/ci/migrations.sql)
bash scripts/check-idempotent-sql.sh artifacts/ci/migrations.sql
dotnet list backend/BuildingFlow.sln package --vulnerable --include-transitive --format json > artifacts/ci/nuget-audit.json
python3 - <<'PY'
import json
from pathlib import Path
x=json.loads(Path('artifacts/ci/nuget-audit.json').read_text())
if x.get('problems'):raise SystemExit('FAIL: NuGet vulnerability sources were unavailable')
for p in x.get('projects',[]):
 for f in p.get('frameworks',[]):
  for k in ('topLevelPackages','transitivePackages'):
   for dependency in f.get(k,[]):
    if dependency.get('vulnerabilities'):raise SystemExit('FAIL: vulnerable NuGet package requires review')
print('[TEST] NuGet dependency audit — PASS')
PY
git diff --check
printf '[TAMAMLANDI] CI quality gates passed. Production was not deployed.\n'
