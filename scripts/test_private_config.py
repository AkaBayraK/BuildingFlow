#!/usr/bin/env python3
"""Regression fixtures for generated-secret tracking; no real settings are read."""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

source = Path(__file__).with_name('check-private-config.py')
with tempfile.TemporaryDirectory(prefix='bf-private-config-') as directory:
    root = Path(directory)
    (root / 'scripts').mkdir()
    shutil.copyfile(source, root / 'scripts/check-private-config.py')
    ignore = root / '.gitignore'
    ignore.write_text('.env\n.env.*\n**/.env\ndeploy/seaweedfs/private-s3.json\n')
    subprocess.run(['git', 'init', '-q', str(root)], check=True)
    def check():
        return subprocess.run([sys.executable, str(root / 'scripts/check-private-config.py')], capture_output=True, text=True)
    assert check().returncode == 0, 'ignored/untracked fixture must pass'
    private = root / 'deploy/seaweedfs/private-s3.json'
    private.parent.mkdir(parents=True)
    private.write_text('{}')
    subprocess.run(['git', '-C', str(root), 'add', '-f', 'deploy/seaweedfs/private-s3.json'], check=True)
    result = check()
    assert result.returncode == 1 and 'TRACKED_PRIVATE_CONFIG' in result.stderr, 'tracked ignored file must fail'
    subprocess.run(['git', '-C', str(root), 'rm', '--cached', '--quiet', 'deploy/seaweedfs/private-s3.json'], check=True)
    ignore.write_text('')
    result = check()
    assert result.returncode == 1 and 'MISSING_PRIVATE_IGNORE' in result.stderr, 'missing ignore must fail'
print('PASS: private-config gate 3/3 black-box fixtures; no real credentials used.')
