#!/usr/bin/env python3
"""Fail if generated private runtime configuration is tracked or lacks an ignore rule.
This is a repository hygiene gate, not a secret/history scanner. Never read values.
"""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
PRIVATE_PATHS = (
    '.env',
    'frontend/buildingflow-web/.env.local',
    'mobile/buildingflow-mobile/.env',
    'deploy/seaweedfs/private-s3.json',
)

def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, text=True)

def main():
    tracked = git('ls-files', '-z')
    if tracked.returncode:
        print('FAIL: private-config check needs a Git checkout.', file=sys.stderr)
        return 1
    names = set(tracked.stdout.split('\0'))
    issues = []
    for path in PRIVATE_PATHS:
        if path in names:
            issues.append(f'TRACKED_PRIVATE_CONFIG: {path}')
        if git('check-ignore', '--no-index', '--quiet', '--', path).returncode:
            issues.append(f'MISSING_PRIVATE_IGNORE: {path}')
    if issues:
        print('\n'.join(issues), file=sys.stderr)
        return 1
    print(f'PASS: {len(PRIVATE_PATHS)} private runtime paths are ignored and untracked; values were not read.')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
