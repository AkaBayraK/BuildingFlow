#!/usr/bin/env python3
"""Regression fixtures for the QA gate and GitHub helper; no network, gh is replaced by a recorder."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import json
import tempfile

spec = spec_from_file_location('qa_github', Path(__file__).with_name('qa_github.py'))
qa = module_from_spec(spec)
spec.loader.exec_module(qa)

def item(module, status='PASS', platform='api', screen='-', actual='PASS', issue=''):
    return {'run_id': 'qa-x', 'module': module, 'screen': screen, 'scenario': f'BuildingFlow.IntegrationTests.{module}Tests.Case', 'platform': platform,
            'actor_role': '-', 'expected': 'test geçer', 'actual': actual, 'status': status, 'screenshot_uri': '', 'issue_url': issue}

full = [item('OnlinePaymentApi'), item('Authentication'), item('Performance'), item('People')]
assert qa.gate(full)[0] == 'PASS'
assert qa.gate(full[:2] + full[3:])[1]['performance'][0] == 'INCOMPLETE'
assert qa.gate(full[:2] + full[3:])[0] == 'INCOMPLETE'
assert qa.gate(full + [item('PaymentApi', 'FAIL', actual='FAIL')])[0] == 'RED'
assert qa.gate(full + [item('Website', 'SKIPPED')])[0] == 'INCOMPLETE'
assert qa.gate([])[0] == 'INCOMPLETE'
assert qa.gate(full + [item('UploadLoaded')])[1]['performance'] == ('PASS', '1 kalem PASS'), 'Load substring is not performance'
assert qa.gate(full + [item('ConsentApi', 'SKIPPED')])[1]['security'][0] == 'INCOMPLETE'

a11y = item('buildings', 'FAIL', 'web-mobile', '/properties/[id]/buildings', 'başlık yok')
assert (qa.severity(a11y), qa.severity(item('OnlinePaymentApi', 'FAIL', actual='FAIL')), qa.severity(item('Survey', 'FAIL', actual='FAIL'))) == ('low', 'high', 'medium')
title, body = qa.bug(a11y)
assert title.startswith(f'[QA-BUG][{qa.fingerprint(a11y)}] /properties/[id]/buildings (web-mobile): başlık yok') and 'Tekrar üretim' in body

calls = []
class Result:
    def __init__(self, out=''): self.stdout = out
def runner(args, **_):
    calls.append(args)
    return Result(json.dumps([{'title': f'[QA-BUG][{qa.fingerprint(a11y)}] old'}]) if args[1:3] == ['issue', 'list'] else '')
with tempfile.TemporaryDirectory() as folder:
    report = Path(folder) / 'report.json'
    report.write_text(json.dumps({'items': full + [a11y, item('Survey', 'FAIL', actual='FAIL', issue='https://x/1')]}))
    assert qa.main(['bugs', str(report)], runner) == 0 and calls == [], 'dry-run must not call gh'
    assert qa.main(['bugs', str(report), '--apply'], runner) == 0
    assert [c[1:3] for c in calls] == [['issue', 'list']], 'duplicate fingerprint and linked issue are skipped'
    calls.clear()
    assert qa.main(['close-phase', str(report), '104', '--apply'], runner) == 1
    assert [c[1:3] for c in calls] == [['issue', 'comment']], 'RED gate comments but never closes'
    calls.clear(); report.write_text(json.dumps({'items': full}))
    assert qa.main(['close-phase', str(report), '104', '--apply'], runner) == 0
    assert [c[1:3] for c in calls] == [['issue', 'comment'], ['issue', 'close']]
print('PASS: QA gate/GitHub helper 15/15 fixtures; gh replaced, no network.')
