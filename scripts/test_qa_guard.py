#!/usr/bin/env python3
"""Regression fixtures for the QA environment guard; only synthetic settings are used."""
from datetime import datetime, timezone
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import re

spec = spec_from_file_location('qa_guard', Path(__file__).with_name('qa_guard.py'))
guard = module_from_spec(spec)
spec.loader.exec_module(guard)

SAFE = {'BUILDINGFLOW_TEST_DATABASE': 'Host=127.0.0.1;Port=5436;Database=buildingflow_test_0123456789abcdef;Username=u;Password=p',
        'POSTGRES_DB': 'buildingflow', 'BUILDINGFLOW_TEST_STORAGE_ENDPOINT': 'http://127.0.0.1:9040',
        'BUILDINGFLOW_TEST_RABBITMQ_HOST': '127.0.0.1', 'BUILDINGFLOW_TEST_MAILPIT_URL': 'http://127.0.0.1:18025'}


def codes(**changes):
    env = {**SAFE, **changes}
    return {code for code, _ in guard.violations({key: value for key, value in env.items() if value is not None})}


cases = [
    ({}, set()),
    ({'BUILDINGFLOW_TEST_DATABASE': 'Host=localhost;Database=buildingflow_ci_test'}, set()),
    ({'BUILDINGFLOW_TEST_DATABASE': 'Host=localhost;Database=buildingflow_test_pay'}, set()),
    ({'ASPNETCORE_ENVIRONMENT': 'Production'}, {'PROD_ENVIRONMENT'}),
    ({'DOTNET_ENVIRONMENT': ' production '}, {'PROD_ENVIRONMENT'}),
    ({'BUILDINGFLOW_TEST_DATABASE': None}, {'TEST_DATABASE_MISSING'}),
    ({'BUILDINGFLOW_TEST_DATABASE': 'Host=db.example.com;Database=buildingflow_test_x'}, {'DATABASE_NOT_LOOPBACK'}),
    ({'BUILDINGFLOW_TEST_DATABASE': 'Host=127.0.0.1;Database=buildingflow'}, {'DATABASE_NOT_TEST', 'DATABASE_IS_MAIN'}),
    ({'BUILDINGFLOW_TEST_DATABASE': 'Host=127.0.0.1;Database=customer_prod'}, {'DATABASE_NOT_TEST'}),
    ({'POSTGRES_DB': 'buildingflow_ci_test', 'BUILDINGFLOW_TEST_DATABASE': 'Host=::1;Database=buildingflow_ci_test'}, {'DATABASE_IS_MAIN'}),
    ({'BUILDINGFLOW_TEST_STORAGE_ENDPOINT': 'https://s3.amazonaws.com'}, {'SERVICE_NOT_LOOPBACK'}),
    ({'BUILDINGFLOW_TEST_RABBITMQ_HOST': 'mq.internal'}, {'SERVICE_NOT_LOOPBACK'}),
    ({'Smtp__Host': 'smtp.gmail.com'}, {'SERVICE_NOT_LOOPBACK'}),
    ({'Payments__Sandbox__PublicBaseUrl': 'https://pay.example.com'}, {'SERVICE_NOT_LOOPBACK'}),
    ({'ContactVerification__PhoneEnabled': 'true'}, {'REAL_SMS_PROVIDER'}),
]
for index, (changes, expected) in enumerate(cases, 1):
    actual = codes(**changes)
    assert actual == expected, f'case {index}: expected {sorted(expected)}, got {sorted(actual)}'

run_id = guard.new_run_id(datetime(2026, 10, 28, 17, 30, tzinfo=timezone.utc))
assert re.fullmatch(r'qa-20261028T173000Z-[0-9a-f]{6}', run_id), 'run id format'
print(f'PASS: QA guard {len(cases)}/{len(cases)} fixtures + run_id format; no real settings read.')
