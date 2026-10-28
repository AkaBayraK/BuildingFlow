#!/usr/bin/env python3
"""Regression fixtures for the QA evidence report; synthetic run folders only."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import json
import tempfile

spec = spec_from_file_location('qa_report', Path(__file__).with_name('qa_report.py'))
report = module_from_spec(spec)
spec.loader.exec_module(report)

TRX = '''<?xml version="1.0"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>
<UnitTestResult testName="BuildingFlow.IntegrationTests.ParkingMigrationTests.Retention" outcome="{0}" duration="00:00:01.5000000" startTime="2026-10-28T18:00:00Z"/>
</Results></TestRun>'''
with tempfile.TemporaryDirectory() as folder:
    run = Path(folder) / 'qa-run'
    (run / 'backend').mkdir(parents=True); (run / 'rerun').mkdir(); (run / 'web').mkdir()
    (run / 'backend/a.trx').write_text(TRX.format('Failed'))
    (run / 'rerun/b.trx').write_text(TRX.format('Passed'))
    (run / 'web/report.json').write_text(json.dumps({'items': [
        {'route': '/properties/[id]/buildings', 'viewport': 'mobile', 'action': 'Ekran açıldı', 'expected': 'başlık', 'actual': 'başlık yok', 'status': 'FAIL', 'evidence': 'x.png'},
        {'route': '/workspaces', 'viewport': '-', 'action': 'Ekran atlandı', 'expected': 'kayıt', 'actual': 'bayrak kapalı', 'status': 'SKIPPED', 'evidence': ''}]}))
    items = report.link_issues(report.collect([run]), {'/properties/[id]/buildings': 'issue-87'})
    api = next(i for i in items if i['platform'] == 'api')
    assert (api['status'], api['history'], api['module'], api['duration_ms']) == ('PASS', ['FAIL'], 'Parking', 1500), api
    web = next(i for i in items if i['screen'] == '/properties/[id]/buildings')
    assert (web['platform'], web['module'], web['issue_url']) == ('web-mobile', 'buildings', 'issue-87'), web
    assert report.summary(items)['byStatus'] == {'PASS': 1, 'FAIL': 1, 'SKIPPED': 1}
    previous = [dict(api, status='FAIL'), dict(web, status='PASS')]
    assert report.compare(items, previous) == {'newFailures': ['web-mobile | /properties/[id]/buildings | Ekran açıldı'], 'fixed': ['api | - | BuildingFlow.IntegrationTests.ParkingMigrationTests.Retention'], 'stillFailing': []}
    line = report.sentence(1, web)
    assert line.startswith('1. [FAIL] /properties/[id]/buildings (web-mobile): Bu ekranda Ekran açıldı işlemi yapıldı; beklenen başlık, gerçekleşen başlık yok, kanıt x.png, issue issue-87'), line
    later = Path(folder) / 'qa-later'; (later / 'web').mkdir(parents=True)
    (later / 'web/report.json').write_text(json.dumps({'items': [{'route': '/properties/[id]/buildings', 'viewport': '-', 'action': 'Ekran atlandı', 'expected': 'kayıt', 'actual': 'yok', 'status': 'SKIPPED', 'evidence': ''}]}))
    merged = [i for i in report.collect([run, later]) if i['screen'] == '/properties/[id]/buildings']
    assert [i['status'] for i in merged] == ['SKIPPED'], merged
print('PASS: QA evidence report 7/7 fixtures; synthetic runs only.')
