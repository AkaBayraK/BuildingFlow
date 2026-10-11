#!/usr/bin/env python3
"""Negative checks for the CI result gate: missing, failed, skipped and empty runs must fail."""
import pathlib,subprocess,sys,tempfile
gate=pathlib.Path(__file__).with_name('check-test-results.py')
def trx(total,passed,not_executed=0):
    return f'<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><ResultSummary><Counters total="{total}" passed="{passed}" failed="{total-passed}" notExecuted="{not_executed}"/></ResultSummary></TestRun>'
def run(files):
    with tempfile.TemporaryDirectory() as d:
        for i,body in enumerate(files):pathlib.Path(d,f'r{i}.trx').write_text(body)
        return subprocess.run([sys.executable,str(gate),d],capture_output=True,text=True).returncode
cases={'missing':([],1),'failed':([trx(3,2)],1),'skipped':([trx(3,3,1)],1),'empty':([trx(0,0)],1),'one bad of two':([trx(2,2),trx(2,1)],1),'all passed':([trx(2,2),trx(5,5)],0)}
bad=[name for name,(files,expected) in cases.items() if (run(files)!=0)!=(expected!=0)]
if bad:raise SystemExit(f'FAIL: CI result gate accepted/rejected wrongly: {bad}')
print(f'[TEST] CI result gate {len(cases)} cases — PASS')
