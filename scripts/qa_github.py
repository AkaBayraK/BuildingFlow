#!/usr/bin/env python3
"""QA FAZ 12: quality gate over a qa_report.py report.json, evidence-backed bug issues and evidence-gated phase closing.

GitHub writes happen only with --apply; the default prints what would be sent. A category without evidence is
INCOMPLETE, never PASS, and only an overall PASS may close a phase issue.
"""
from hashlib import sha256
from pathlib import Path
import argparse
import json
import re
import subprocess
import sys

REPO = 'AkaBayraK/BuildingFlow'
CATEGORIES = {
    'security': re.compile(r'Auth|Security|Authorization|Consent|Identity|ReleaseSecurity|QaAiAssistant', re.I),
    'finance': re.compile(r'OnlinePayment|Payment|Dues|LatePenalty|Ledger|Reconciliation|Expense', re.I),
    # Only explicit performance/load tests count: a name that merely contains 'Load' (Upload) is not evidence.
    'performance': re.compile(r'Performance|LoadTest|P95'),
}
HIGH = re.compile(r'Security|Auth|Payment|Dues|Ledger|Consent|identity', re.I)
LOW = re.compile(r'başlık yok|adsız kontrol|etiketsiz alan|alt metinsiz|aria-selected|taşma', re.I)


def text_of(item):
    return ' '.join(str(item.get(key, '')) for key in ('module', 'screen', 'scenario', 'platform'))


def gate(items):
    result = {}
    failures = [i for i in items if i['status'] == 'FAIL']
    open_items = [i for i in items if i['status'] in ('BLOCKED', 'SKIPPED')]
    result['regression'] = ('RED', f'{len(failures)} FAIL') if failures else ('PASS', f'{len(items)} kalem FAIL yok') if items else ('INCOMPLETE', 'kanıt yok')
    for name, pattern in CATEGORIES.items():
        scoped = [i for i in items if pattern.search(text_of(i))]
        failed = [i for i in scoped if i['status'] == 'FAIL']
        pending = [i for i in scoped if i['status'] != 'PASS' and i['status'] != 'FAIL']
        result[name] = (('INCOMPLETE', 'kanıt yok') if not scoped else ('RED', f'{len(failed)}/{len(scoped)} FAIL') if failed
                        else ('INCOMPLETE', f'{len(pending)}/{len(scoped)} SKIPPED/BLOCKED') if pending else ('PASS', f'{len(scoped)} kalem PASS'))
    result['skipped_blocked'] = ('INCOMPLETE', f'{len(open_items)} SKIPPED/BLOCKED') if open_items else ('PASS', 'yok')
    states = {state for state, _ in result.values()}
    overall = 'RED' if 'RED' in states else 'INCOMPLETE' if 'INCOMPLETE' in states else 'PASS'
    return overall, result


def fingerprint(item):
    return sha256(f"{item['platform']}|{item['screen']}|{item['scenario']}".encode()).hexdigest()[:10]


def severity(item):
    if HIGH.search(text_of(item)): return 'high'
    if LOW.search(str(item.get('actual', ''))): return 'low'
    return 'medium'


def bug(item):
    where = item['screen'] if item['screen'] != '-' else item['module']
    title = f"[QA-BUG][{fingerprint(item)}] {where} ({item['platform']}): {str(item['actual'])[:80]}"
    body = '\n'.join([
        f"QA kanıt raporundan otomatik hazırlandı. Severity: **{severity(item)}**.", '',
        '**Tekrar üretim**', f"1. Koşum `{item['run_id']}`, platform `{item['platform']}`, rol `{item['actor_role']}`.",
        f"2. Ekran/test: `{where}` — {item['scenario']}.", '',
        f"**Beklenen:** {item['expected']}", f"**Gerçekleşen:** {item['actual']}",
        f"**Kanıt:** `{item['screenshot_uri'] or 'TRX/konsol logu ' + item['run_id']}` (yerel, git dışı)", '',
        'Kapanış: düzeltme sonrası aynı senaryo yeniden koşulup PASS kanıtı eklenmeden kapatılmaz.'])
    return title, body


def gh(args, apply, runner=subprocess.run):
    if not apply:
        print('[DRY-RUN] gh ' + ' '.join(args[:4]) + ' …')
        return ''
    return runner(['gh', *args], check=True, capture_output=True, text=True).stdout


def existing_fingerprints(runner=subprocess.run):
    out = runner(['gh', 'issue', 'list', '--repo', REPO, '--state', 'all', '--limit', '500', '--search', 'QA-BUG in:title', '--json', 'title'],
                 check=True, capture_output=True, text=True).stdout
    return {match.group(1) for issue in json.loads(out or '[]') for match in [re.search(r'\[QA-BUG\]\[([0-9a-f]{10})\]', issue['title'])] if match}


def main(argv=None, runner=subprocess.run):
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest='command', required=True)
    for name in ('gate', 'bugs', 'close-phase'):
        command = sub.add_parser(name); command.add_argument('report')
        if name != 'gate': command.add_argument('--apply', action='store_true')
        if name == 'close-phase': command.add_argument('issue', type=int)
    args = parser.parse_args(argv)
    items = json.loads(Path(args.report).read_text())['items']
    overall, categories = gate(items)
    lines = [f'[QA KAPI] {overall}'] + [f'  {name}: {state} ({detail})' for name, (state, detail) in categories.items()]
    print('\n'.join(lines))
    if args.command == 'gate':
        return {'PASS': 0, 'INCOMPLETE': 2}.get(overall, 1)
    if args.command == 'bugs':
        known = existing_fingerprints(runner) if args.apply else set()
        candidates = [i for i in items if i['status'] == 'FAIL' and not i.get('issue_url') and fingerprint(i) not in known]
        for item in candidates:
            title, body = bug(item)
            print(f'[QA BUG] {title}')
            gh(['issue', 'create', '--repo', REPO, '--label', 'bug', '--title', title, '--body', body], args.apply, runner)
        print(f'[QA BUG] {len(candidates)} yeni bug; mevcut issue bağlantılı veya mükerrer olanlar atlandı.')
        return 0
    comment = '\n'.join(['QA kalite kapısı sonucu:', '```text', *lines, '```',
                         'Tüm zorunlu kategoriler PASS: faz kapatılıyor.' if overall == 'PASS' else 'Zorunlu kategoriler PASS değil: issue açık kalıyor.'])
    gh(['issue', 'comment', str(args.issue), '--repo', REPO, '--body', comment], args.apply, runner)
    if overall == 'PASS':
        gh(['issue', 'close', str(args.issue), '--repo', REPO], args.apply, runner)
    return 0 if overall == 'PASS' else 1


if __name__ == '__main__':
    sys.exit(main())
