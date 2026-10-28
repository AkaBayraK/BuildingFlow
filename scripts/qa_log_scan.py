#!/usr/bin/env python3
"""QA FAZ 05 KVKK log scan: finds unmasked TCKN, IBAN, card number, JWT, password, e-mail and phone values in log files.

Only the category and file:line are printed, never the matched value. Synthetic test domains are allowed.
"""
from pathlib import Path
import re
import sys

ALLOWED_EMAIL = re.compile(r'@(example\.test|buildingflow\.local)$', re.I)
PATTERNS = {
    'TCKN': re.compile(r'(?<!\d)[1-9]\d{10}(?!\d)'),
    'IBAN': re.compile(r'\bTR\d{2}(?: ?\d{4}){5} ?\d{2}\b'),
    'CARD': re.compile(r'(?<!\d)(?:\d[ -]?){13,19}(?!\d)'),
    'JWT': re.compile(r'\beyJ[\w-]{8,}\.eyJ[\w-]{8,}\.[\w-]{8,}'),
    'PASSWORD': re.compile(r'(?i)\b(password|passwd|pwd|secret)\b\s*[=:]\s*(?!\*+|REDACTED|\[)[^\s,;"\']{4,}'),
    'EMAIL': re.compile(r'[\w.+-]+@[\w-]+(?:\.[\w-]+)+'),
    'PHONE': re.compile(r'(?<![\d+])(?:\+90|0)\s?5\d{2}\s?\d{3}\s?\d{2}\s?\d{2}(?!\d)'),
}


def tckn(value):
    d = [int(c) for c in value]
    return ((sum(d[0:9:2]) * 7 - sum(d[1:8:2])) % 10 == d[9]) and sum(d[:10]) % 10 == d[10]


def luhn(value):
    digits = [int(c) for c in re.sub(r'\D', '', value)][::-1]
    return 13 <= len(digits) <= 19 and sum(d if i % 2 == 0 else (d * 2 - 9 if d > 4 else d * 2) for i, d in enumerate(digits)) % 10 == 0


def findings(text):
    found = []
    for number, line in enumerate(text.splitlines(), 1):
        for category, pattern in PATTERNS.items():
            for match in pattern.finditer(line):
                value = match.group(0)
                if category == 'TCKN' and not tckn(value): continue
                if category == 'CARD' and not luhn(value): continue
                if category == 'EMAIL' and ALLOWED_EMAIL.search(value): continue
                found.append((category, number))
    return found


def main(paths):
    total = 0
    for path in map(Path, paths):
        for category, line in findings(path.read_text(errors='ignore')):
            total += 1
            print(f'[HATA] KVKK {category}: {path}:{line}', file=sys.stderr)
    print(f'[QA KVKK] {len(paths)} dosya tarandı, {total} maskesiz değer bulundu.')
    return 1 if total else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
