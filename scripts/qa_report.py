#!/usr/bin/env python3
"""QA FAZ 11 evidence report: merges real run artefacts (web CDP, identity, .NET TRX) into one JSON/MD/CSV/HTML/PDF set.

Only recorded results are reported; nothing is inferred as PASS. A later TRX for the same test (a rerun) supersedes the
earlier one and both are kept in the item history. PDF is printed by the system Chrome; without Chrome it is skipped.
"""
from collections import Counter
from datetime import datetime, timezone
from html import escape
from pathlib import Path
import argparse
import csv
import json
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
TRX = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
OUTCOME = {'Passed': 'PASS', 'Failed': 'FAIL', 'NotExecuted': 'SKIPPED', 'Inconclusive': 'BLOCKED'}
FIELDS = ['run_id', 'module', 'screen', 'scenario', 'step', 'actor_role', 'platform', 'started_at_utc', 'duration_ms',
          'expected', 'actual', 'status', 'screenshot_uri', 'issue_url']


def module_of_route(route):
    parts = [p for p in route.split('/') if p and not p.startswith('[')]
    if parts[:1] == ['properties'] and len(parts) > 1:
        return parts[1]
    return parts[0] if parts else 'root'


def module_of_test(name):
    cls = name.split('.')[-2] if name.count('.') >= 1 else name
    return re.sub(r'(Api|Migration|Persistence|Store|Domain)?Tests$', '', cls) or cls


def trx_items(path, run_id):
    tree = ET.parse(path)
    for result in tree.iter(TRX + 'UnitTestResult'):
        name = result.get('testName')
        hours, minutes, seconds = (result.get('duration') or '0:0:0').split(':')
        yield {'run_id': run_id, 'module': module_of_test(name), 'screen': '-', 'scenario': name, 'step': '-', 'actor_role': '-',
               'platform': 'api', 'started_at_utc': result.get('startTime', ''), 'duration_ms': int((int(hours) * 3600 + int(minutes) * 60 + float(seconds)) * 1000),
               'expected': 'test geçer', 'actual': OUTCOME.get(result.get('outcome'), result.get('outcome')), 'status': OUTCOME.get(result.get('outcome'), 'BLOCKED'),
               'screenshot_uri': '', 'issue_url': ''}


def web_items(path, run_id):
    for item in json.loads(path.read_text())['items']:
        yield {'run_id': run_id, 'module': module_of_route(item['route']), 'screen': item['route'], 'scenario': item['action'], 'step': '-',
               'actor_role': 'SUPER_ADMIN', 'platform': 'web-' + item['viewport'] if item['viewport'] != '-' else 'web', 'started_at_utc': '', 'duration_ms': '',
               'expected': item['expected'], 'actual': item['actual'], 'status': item['status'], 'screenshot_uri': item.get('evidence', ''), 'issue_url': ''}


def identity_items(path, run_id):
    for item in json.loads(path.read_text())['results']:
        role = item['action'].split(' ')[0] if 'kullanıcısı' in item['action'] else '-'
        yield {'run_id': run_id, 'module': 'identity', 'screen': 'Keycloak', 'scenario': item['action'], 'step': '-', 'actor_role': role, 'platform': 'keycloak',
               'started_at_utc': '', 'duration_ms': '', 'expected': item['expected'], 'actual': item['actual'], 'status': item['status'], 'screenshot_uri': '', 'issue_url': ''}


def collect(run_dirs):
    items = {}
    for run in map(Path, run_dirs):
        run_id = run.name
        sources = []
        if (run / 'web/report.json').exists(): sources += web_items(run / 'web/report.json', run_id)
        if (run / 'identity/report.json').exists(): sources += identity_items(run / 'identity/report.json', run_id)
        for trx in sorted(run.rglob('*.trx'), key=lambda p: p.stat().st_mtime):
            sources += trx_items(trx, run_id)
        # A later web run re-judges a screen as a whole (e.g. a redirect now classified as SKIPPED), so it replaces older web rows.
        screens = {item['screen'] for item in sources if item['platform'].startswith('web')}
        for old in [k for k in items if k[0].startswith('web') and k[1] in screens]:
            items.pop(old)
        for item in sources:
            key = (item['platform'], item['screen'], item['scenario'])
            if key in items:
                item['history'] = items[key].get('history', []) + [items[key]['status']]
            items[key] = item
    return list(items.values())


def link_issues(items, known):
    for item in items:
        if item['status'] != 'PASS':
            item['issue_url'] = known.get(item['screen']) or known.get(item['module']) or item['issue_url']
    return items


def compare(items, previous):
    key = lambda item: (item['platform'], item['screen'], item['scenario'])
    before = {key(item): item['status'] for item in previous}
    now = {key(item): item['status'] for item in items}
    return {'newFailures': sorted(' | '.join(k) for k, s in now.items() if s == 'FAIL' and before.get(k) not in (None, 'FAIL')),
            'fixed': sorted(' | '.join(k) for k, s in now.items() if s == 'PASS' and before.get(k) == 'FAIL'),
            'stillFailing': sorted(' | '.join(k) for k, s in now.items() if s == 'FAIL' and before.get(k) == 'FAIL')}


def summary(items):
    return {'total': len(items), 'byStatus': dict(Counter(item['status'] for item in items)),
            'byPlatform': {p: dict(Counter(i['status'] for i in items if i['platform'] == p)) for p in sorted({i['platform'] for i in items})},
            'byModule': {m: dict(Counter(i['status'] for i in items if i['module'] == m)) for m in sorted({i['module'] for i in items})}}


def sentence(index, item):
    where = item['screen'] if item['screen'] != '-' else item['module']
    text = f"{index}. [{item['status']}] {where} ({item['platform']}): Bu ekranda {item['scenario']} işlemi yapıldı; beklenen {item['expected']}, gerçekleşen {item['actual']}"
    return text + (f", kanıt {item['screenshot_uri']}" if item['screenshot_uri'] else '') + (f", issue {item['issue_url']}" if item['issue_url'] else '') + '.'


def markdown(report):
    s = report['summary']
    lines = [f"# QA kanıt raporu {report['generatedAt']}", '', f"Kaynak koşumlar: {', '.join(report['runs'])}", '',
             '| Durum | Sayı |', '|---|---|'] + [f'| {k} | {v} |' for k, v in sorted(s['byStatus'].items())] + ['', '| Platform | Dağılım |', '|---|---|']
    lines += [f"| {p} | {', '.join(f'{k} {v}' for k, v in sorted(c.items()))} |" for p, c in s['byPlatform'].items()]
    if report.get('comparison'):
        c = report['comparison']
        lines += ['', f"Önceki raporla: yeni FAIL {len(c['newFailures'])}, düzelen {len(c['fixed'])}, hâlâ FAIL {len(c['stillFailing'])}"]
    ordered = sorted(report['items'], key=lambda i: ({'FAIL': 0, 'BLOCKED': 1, 'SKIPPED': 2, 'PASS': 3}.get(i['status'], 4), i['platform'], i['module'], i['scenario']))
    return '\n'.join(lines + ['', '## Ayrıntı', ''] + [sentence(n, item) for n, item in enumerate(ordered, 1)]) + '\n'


def html(report):
    rows = ''.join(f"<tr class='{escape(i['status'])}'><td>{escape(i['status'])}</td><td>{escape(i['platform'])}</td><td>{escape(i['module'])}</td><td>{escape(i['screen'])}</td>"
                   f"<td>{escape(i['scenario'])}</td><td>{escape(str(i['expected']))}</td><td>{escape(str(i['actual']))}</td><td>{escape(i['issue_url'])}</td></tr>" for i in report['items'])
    counts = ' · '.join(f'{k}: {v}' for k, v in sorted(report['summary']['byStatus'].items()))
    return ("<!doctype html><meta charset='utf-8'><title>QA kanıt raporu</title><style>body{font:12px system-ui,sans-serif;margin:24px;color:#0b2540}"
            "table{border-collapse:collapse;width:100%}td,th{border:1px solid #cbd5e1;padding:4px;vertical-align:top}th{background:#0b2540;color:#fff}"
            ".FAIL td:first-child{background:#fee2e2}.PASS td:first-child{background:#dcfce7}.SKIPPED td:first-child,.BLOCKED td:first-child{background:#fef3c7}</style>"
            f"<h1>BuildingFlow QA kanıt raporu</h1><p>{escape(report['generatedAt'])} · {escape(', '.join(report['runs']))}</p><p>{escape(counts)}</p>"
            "<table><tr><th>Durum</th><th>Platform</th><th>Modül</th><th>Ekran</th><th>Senaryo</th><th>Beklenen</th><th>Gerçekleşen</th><th>Issue</th></tr>" + rows + "</table>")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('runs', nargs='+')
    parser.add_argument('--out', required=True)
    parser.add_argument('--previous')
    args = parser.parse_args()
    known = json.loads((ROOT / 'qa-center/known-issues.json').read_text())
    items = link_issues(collect(args.runs), known)
    report = {'generatedAt': datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'), 'runs': [Path(r).name for r in args.runs], 'summary': summary(items), 'items': items}
    if args.previous:
        report['comparison'] = compare(items, json.loads(Path(args.previous).read_text())['items'])
    out = Path(args.out); out.mkdir(parents=True, exist_ok=True)
    (out / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=1))
    (out / 'report.md').write_text(markdown(report))
    with open(out / 'report.csv', 'w', newline='', encoding='utf-8-sig') as handle:
        writer = csv.DictWriter(handle, FIELDS, delimiter=';', extrasaction='ignore'); writer.writeheader(); writer.writerows(items)
    (out / 'report.html').write_text(html(report))
    pdf = 'yok (Chrome bulunamadı)'
    if Path(CHROME).exists():
        subprocess.run([CHROME, '--headless=new', '--disable-gpu', '--no-pdf-header-footer', f"--print-to-pdf={out / 'report.pdf'}", (out / 'report.html').resolve().as_uri()],
                       check=True, capture_output=True, timeout=120)
        pdf = str(out / 'report.pdf')
    print(f"[QA RAPOR] {len(items)} kalem {report['summary']['byStatus']}; JSON/MD/CSV/HTML {out}; PDF {pdf}")


if __name__ == '__main__':
    main()
