#!/usr/bin/env python3
"""QA FAZ 02 screen/API/DB inventory: joins the runtime endpoint dump with static web, mobile, DB and test sources.

Static signals (CRUD, search, filter, paging, export) are text heuristics, not test evidence.
Output is sorted and timestamp-free, so two runs on the same tree are byte-identical.
"""
from collections import defaultdict
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / 'frontend/buildingflow-web/src'
MOBILE = ROOT / 'mobile/buildingflow-mobile/src'
OUT = ROOT / 'qa-center/inventory'
API_CALL = re.compile(r"""['"`](/api/(?:v\d|public)/[^'"`\s?]*)""")
FEATURE_IMPORT = re.compile(r'from\s+"@/features/([\w-]+)/')
NAV_LINK = re.compile(r'link\("([\w-]+)",\s*"(/[^"]*)"')
NAV_PLANNED = re.compile(r'planned\("([\w-]+)"')
TAB = re.compile(r'<Tabs\.Screen name="([\w-]+)" options=\{\{\s*(href:\s*([^,]+),)?')
ROLE = re.compile(r"['\"](SUPER_ADMIN|SITE_ADMIN|MANAGER|ACCOUNTANT|RESIDENT|OWNER|TENANT|SECURITY)['\"]")
TABLE = re.compile(r'ToTable\("([a-z0-9_]+)"')
SIGNALS = {
    'create': re.compile(r'method:\s*"POST"|\bPOST\b'), 'update': re.compile(r'method:\s*"PUT"|\bPUT\b'),
    'delete': re.compile(r'method:\s*"DELETE"|\bDELETE\b'), 'search': re.compile(r'search', re.I),
    'filter': re.compile(r'FilterChips|filter', re.I), 'paging': re.compile(r'pageSize|pagination', re.I),
    'export': re.compile(r'export(Csv|Pdf|Excel)|\.csv|text/csv|application/pdf', re.I),
    'table': re.compile(r'useReactTable|DataTable'), 'form': re.compile(r'<form|onSubmit'),
}


def normalize(path):
    path = re.sub(r'\$\{[^}]*\}|\{[^}]*\}', '{}', path.split('?')[0]).split('$')[0]
    return '/' + '/'.join(part for part in path.split('/') if part)


def matches(call, route):
    left, right = call.split('/'), route.split('/')
    return len(left) == len(right) and all(a == b or '{}' in (a, b) or (a.endswith('{}') and b.startswith(a[:-2]))
                                           for a, b in zip(left, right))


def prefix_of(call, route):
    left, right = call.split('/'), route.split('/')
    return 4 <= len(left) < len(right) and matches(call, '/'.join(right[:len(left)]))


def sources(folder, pattern='*.ts*'):
    return sorted(path for path in folder.rglob(pattern) if 'node_modules' not in path.parts)


def calls_by_group(files, group):
    found = defaultdict(set)
    for path in files:
        for call in API_CALL.findall(path.read_text(errors='ignore')):
            found[group(path)].add(normalize(call))
    return found


def signals(files):
    text = '\n'.join(path.read_text(errors='ignore') for path in files)
    return sorted(name for name, pattern in SIGNALS.items() if pattern.search(text))


def tab_visibility(layout):
    rules = {'null': 'HIDDEN', 'linked ? undefined : null': 'LINKED_CONTEXT'}
    return [{'tab': name, 'visibility': rules.get(href.strip(), 'CONDITIONAL') if href else 'ALWAYS'}
            for name, _, href in TAB.findall(layout)]


def role_gates(folder):
    gates = defaultdict(set)
    for path in sources(folder):
        group = path.relative_to(folder).parts[0] if len(path.relative_to(folder).parts) > 1 else path.stem
        if group == 'app':
            group = 'screen:' + path.stem
        gates[group].update(ROLE.findall(path.read_text(errors='ignore')))
    return {group: sorted(roles) for group, roles in sorted(gates.items()) if roles}


def web_pages():
    pages = []
    for page in sources(WEB / 'app', 'page.tsx'):
        route = '/' + '/'.join(part for part in page.parent.relative_to(WEB / 'app').parts if not part.startswith('('))
        pages.append({'route': route.rstrip('/') or '/', 'features': sorted(set(FEATURE_IMPORT.findall(page.read_text())))})
    return sorted(pages, key=lambda page: page['route'])


def build(endpoints):
    for endpoint in endpoints:
        endpoint['key'] = normalize(endpoint['Route'])
    web_calls = calls_by_group(sources(WEB / 'features'), lambda path: path.relative_to(WEB / 'features').parts[0])
    mobile_calls = calls_by_group(sources(MOBILE), lambda path: path.relative_to(MOBILE).parts[0])
    test_calls = calls_by_group(sources(ROOT / 'backend/tests', '*.cs'), lambda path: 'backend')
    web_calls_flat = {call for calls in web_calls.values() for call in calls}
    mobile_calls_flat = {call for calls in mobile_calls.values() for call in calls}

    def consumers(calls, key):
        if any(matches(call, key) for call in calls):
            return 'EXACT'
        return 'BASE_PREFIX' if any(prefix_of(call, key) for call in calls) else None

    api = [{'method': e['Method'], 'route': e['Route'], 'access': e['Access'], 'policies': e['Policies'],
            'web': consumers(web_calls_flat, e['key']), 'mobile': consumers(mobile_calls_flat, e['key']),
            'backendTestReference': consumers(test_calls['backend'], e['key']) == 'EXACT'} for e in endpoints]
    keys = sorted({e['key'] for e in endpoints})
    unmatched = lambda calls: sorted(call for call in calls if not any(matches(call, key) or prefix_of(call, key) for key in keys))
    pages = web_pages()
    for page in pages:
        files = [path for feature in page['features'] for path in sources(WEB / 'features' / feature)]
        page['apiCalls'] = sorted({call for feature in page['features'] for call in web_calls.get(feature, ())})
        page['signals'] = signals(files)
    navigation = (WEB / 'features/navigation/navigation.ts').read_text()
    routes = {page['route'] for page in pages}
    nav = [{'key': key, 'route': route, 'pageExists': route in routes} for key, route in sorted(set(NAV_LINK.findall(navigation)))]
    mobile_screens = sorted('/' + str(path.relative_to(MOBILE / 'app').with_suffix('')) for path in sources(MOBILE / 'app', '*.tsx')
                            if not path.name.startswith(('_', '+')))
    snapshot = next((ROOT / 'backend/src/BuildingFlow.Infrastructure/Migrations').glob('*ModelSnapshot.cs'))
    tables = sorted(set(TABLE.findall(snapshot.read_text())))
    return {
        'api': api, 'webPages': pages, 'navigation': nav, 'plannedNavigation': sorted(set(NAV_PLANNED.findall(navigation))),
        'mobileScreens': mobile_screens, 'mobileRoleGates': role_gates(MOBILE), 'mobileTabs': tab_visibility((MOBILE / 'app/(tabs)/_layout.tsx').read_text()),
        'mobileModules': {module: sorted(calls) for module, calls in sorted(mobile_calls.items())},
        'tables': tables, 'unmatchedWebCalls': unmatched(web_calls_flat), 'unmatchedMobileCalls': unmatched(mobile_calls_flat),
    }


def summary(data):
    api = data['api']
    count = lambda predicate: sum(1 for item in api if predicate(item))
    unused = [item for item in api if not item['web'] and not item['mobile'] and item['access'] != 'PUBLIC_IMPLICIT']
    lines = ['# QA FAZ 02 — Ekran/API/DB kapsama raporu', '',
             'Üretici: `python3 scripts/qa_inventory.py`. Sinyaller metin sezgisidir; test kanıtı değildir.', '',
             '| Kalem | Sayı |', '|---|---|',
             f"| API endpoint (çalışma zamanı) | {len(api)} |",
             f"| Yetkili / açık anonim / public prefix | {count(lambda i: i['access'] == 'AUTHORIZED')} / {count(lambda i: i['access'] == 'ANONYMOUS')} / {count(lambda i: i['access'] == 'PUBLIC_IMPLICIT')} |",
             f"| Web çağrısı: tam eşleşme / yalnız taban yol | {count(lambda i: i['web'] == 'EXACT')} / {count(lambda i: i['web'] == 'BASE_PREFIX')} |",
             f"| Mobil çağrısı: tam eşleşme / yalnız taban yol | {count(lambda i: i['mobile'] == 'EXACT')} / {count(lambda i: i['mobile'] == 'BASE_PREFIX')} |",
             f"| Backend testlerinde route metni geçen endpoint | {count(lambda i: i['backendTestReference'])} |",
             f"| Web/mobilde tam eşleşmesi olmayan endpoint | {count(lambda i: 'EXACT' not in (i['web'], i['mobile']) and i['access'] != 'PUBLIC_IMPLICIT')} |",
             f"| Hiç tüketicisi bulunmayan endpoint | {len(unused)} |",
             f"| Web sayfası | {len(data['webPages'])} |", f"| Menü bağlantısı / planlı menü | {len(data['navigation'])} / {len(data['plannedNavigation'])} |",
             f"| Mobil ekran | {len(data['mobileScreens'])} |",
             f"| Mobil sekme: her zaman / bağlı kişi gerekir / gizli | {sum(t['visibility'] == 'ALWAYS' for t in data['mobileTabs'])} / {sum(t['visibility'] == 'LINKED_CONTEXT' for t in data['mobileTabs'])} / {sum(t['visibility'] == 'HIDDEN' for t in data['mobileTabs'])} |", f"| DB tablosu (EF snapshot) | {len(data['tables'])} |", '',
             '## Web sayfaları ve statik sinyaller', '', '| Route | Feature | API çağrısı | Sinyal |', '|---|---|---|---|']
    lines += [f"| `{p['route']}` | {', '.join(p['features']) or '-'} | {len(p['apiCalls'])} | {', '.join(p['signals']) or '-'} |" for p in data['webPages']]
    lines += ['', '## Mobil rol kapıları (statik: kaynakta geçen rol/ilişki adları)', '', '| Modül/ekran | Roller |', '|---|---|']
    lines += [f"| `{group}` | {', '.join(roles)} |" for group, roles in data['mobileRoleGates'].items()]
    lines += ['', '## Bulgular', '']
    lines += [f"- Sayfası olmayan menü bağlantısı: `{n['key']}` → `{n['route']}`" for n in data['navigation'] if not n['pageExists']]
    lines += [f"- Planlı (henüz ekranı olmayan) menü: `{key}`" for key in data['plannedNavigation']]
    lines += [f"- Mobil sekme sekme çubuğunda gizli: `{t['tab']}`" for t in data['mobileTabs'] if t['visibility'] == 'HIDDEN']
    lines += [f"- Public prefix ile anonim, AllowAnonymous işareti yok: `{i['method']} {i['route']}`" for i in api if i['access'] == 'PUBLIC_IMPLICIT']
    lines += [f"- Eşleşmeyen web çağrısı: `{call}`" for call in data['unmatchedWebCalls']]
    lines += [f"- Eşleşmeyen mobil çağrısı: `{call}`" for call in data['unmatchedMobileCalls']]
    lines += ['', f'## Web/mobil tüketicisi bulunmayan endpointler ({len(unused)})', '',
              'Worker, admin, entegrasyon veya dinamik URL ile çağrılıyor olabilir; FAZ 04’te tek tek doğrulanır.', '']
    lines += [f"- `{i['method']} {i['route']}`" for i in unused]
    return '\n'.join(lines) + '\n'


def main():
    dump = OUT / 'api-endpoints.json'
    if not dump.exists():
        print('[HATA] api-endpoints.json yok: önce QaEndpointInventoryTests çalıştırılmalı.', file=sys.stderr)
        return 1
    data = build(json.loads(dump.read_text()))
    (OUT / 'inventory.json').write_text(json.dumps(data, ensure_ascii=False, indent=1, sort_keys=True) + '\n')
    (OUT / 'coverage.md').write_text(summary(data))
    print(f"[QA ENVANTER] API {len(data['api'])}, web sayfası {len(data['webPages'])}, mobil ekran {len(data['mobileScreens'])}, tablo {len(data['tables'])}.")
    return 0


if __name__ == '__main__':
    sys.exit(main())
