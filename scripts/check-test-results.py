#!/usr/bin/env python3
"""Fail a quality gate when tests fail, are skipped or no result exists."""
import pathlib,sys,xml.etree.ElementTree as ET
files=list(pathlib.Path(sys.argv[1]).glob('*.trx'))
if not files:raise SystemExit('FAIL: test result files are missing')
total=0
for p in files:
 root=ET.parse(p).getroot();c=root.find('.//{*}Counters')
 if c is None or int(c.get('total','0'))==0 or c.get('total')!=c.get('passed') or int(c.get('notExecuted','0'))!=0:raise SystemExit('FAIL: failed/skipped/incomplete test result')
 total+=int(c.get('passed'))
print(f'[TEST] {total} .NET tests; failure/skip0 — PASS')
