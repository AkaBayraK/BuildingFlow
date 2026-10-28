#!/usr/bin/env python3
"""QA FAZ 08 coverage ratchet: a new web page or API endpoint without any test reference fails CI.

Existing gaps live in qa-center/inventory/coverage-baseline.json and may only shrink. Run with --update after
closing gaps to record the smaller baseline. References are text matches (inventory heuristics), not proof of behaviour.
"""
from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / 'qa-center/inventory/coverage-baseline.json'
WEB_TESTS = ROOT / 'frontend/buildingflow-web/tests'


def gaps(inventory, web_tests_text):
    endpoints = sorted(f"{item['method']} {item['route']}" for item in inventory['api']
                       if not item['backendTestReference'] and item['access'] != 'PUBLIC_IMPLICIT')
    pages = sorted(page['route'] for page in inventory['webPages']
                   if page['features'] and not any(f'features/{feature}/' in web_tests_text for feature in page['features']))
    return {'untestedEndpoints': endpoints, 'pagesWithoutWebTest': pages}


def compare(current, baseline):
    added = {key: sorted(set(current[key]) - set(baseline.get(key, []))) for key in current}
    closed = {key: sorted(set(baseline.get(key, [])) - set(current[key])) for key in current}
    return added, closed


def main(args):
    inventory_dir = Path(args[0]) if args and not args[0].startswith('--') else ROOT / 'qa-center/inventory'
    inventory = json.loads((inventory_dir / 'inventory.json').read_text())
    text = '\n'.join(path.read_text(errors='ignore') for path in sorted(WEB_TESTS.glob('*.test.mjs')))
    current = gaps(inventory, text)
    if '--update' in args:
        BASELINE.write_text(json.dumps(current, ensure_ascii=False, indent=1) + '\n')
        print(f"[QA KAPSAM] Taban güncellendi: {len(current['untestedEndpoints'])} endpoint, {len(current['pagesWithoutWebTest'])} sayfa.")
        return 0
    added, closed = compare(current, json.loads(BASELINE.read_text()))
    for key, items in added.items():
        for item in items:
            print(f'[HATA] Testsiz yeni kalem ({key}): {item}', file=sys.stderr)
    shrink = sum(map(len, closed.values()))
    if shrink:
        print(f'[QA KAPSAM] {shrink} kalem artık testli; tabanı küçültmek için --update çalıştırın.')
    failed = sum(map(len, added.values()))
    print(f"[QA KAPSAM] {'RED' if failed else 'GEÇTİ'}: yeni testsiz kalem {failed}; taban {len(current['untestedEndpoints'])} endpoint, {len(current['pagesWithoutWebTest'])} sayfa.")
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
