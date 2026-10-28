#!/usr/bin/env python3
"""Regression fixtures for the KVKK log scan; every value below is synthetic."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

spec = spec_from_file_location('qa_log_scan', Path(__file__).with_name('qa_log_scan.py'))
scan = module_from_spec(spec)
spec.loader.exec_module(scan)

cases = [
    ('Kişi 10000000146 kaydedildi', ['TCKN']),
    ('sipariş 12345678901 işlendi', []),
    ('IBAN TR33 0006 1005 1978 6457 8413 26', ['IBAN']),
    ('kart 4111 1111 1111 1111 reddedildi', ['CARD']),
    ('trace 1234 5678 9012 3456', []),
    ('Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTYifQ.c2lnbmF0dXJlLXZhbHVl', ['JWT']),
    ('password=Gizli-Parola1', ['PASSWORD']),
    ('password=*** masked', []),
    ('kullanıcı ali.veli@gmail.com', ['EMAIL']),
    ('kullanıcı qa-robot@example.test', []),
    ('telefon +90 532 123 45 67', ['PHONE']),
    ('telefon 0532 123 45 67', ['PHONE']),
    ('BuildingFlow.QaCenter run processing failed (TimeoutException)', []),
]
for index, (text, expected) in enumerate(cases, 1):
    actual = [category for category, _ in scan.findings(text)]
    assert actual == expected, f'case {index}: expected {expected}, got {actual}'
print(f'PASS: KVKK log scan {len(cases)}/{len(cases)} fixtures; values are synthetic.')
