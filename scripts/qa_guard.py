#!/usr/bin/env python3
"""QA test environment guard: refuses any run that could reach production data or real outside services.

Only setting names and rule codes are printed; values (connection strings, secrets) never are.
"""
from datetime import datetime, timezone
from urllib.parse import urlsplit
import os
import re
import secrets
import sys

LOOPBACK = {'localhost', '127.0.0.1', '::1'}
TEST_DATABASE = re.compile(r'buildingflow_(test(_[a-z0-9]+)?|ci_test)')
LOOPBACK_URLS = ('BUILDINGFLOW_TEST_STORAGE_ENDPOINT', 'BUILDINGFLOW_TEST_MAILPIT_URL',
                 'BUILDINGFLOW_TEST_KEYCLOAK_ADMIN_URL', 'Payments__Sandbox__PublicBaseUrl')
LOOPBACK_HOSTS = ('BUILDINGFLOW_TEST_RABBITMQ_HOST', 'Smtp__Host')
TRUE = {'1', 'true', 'yes'}


def connection_settings(value):
    pairs = (part.split('=', 1) for part in value.split(';') if '=' in part)
    return {key.strip().lower(): item.strip() for key, item in pairs}


def violations(env):
    found = []
    for name in ('ASPNETCORE_ENVIRONMENT', 'DOTNET_ENVIRONMENT'):
        if env.get(name, '').strip().lower() == 'production':
            found.append(('PROD_ENVIRONMENT', name))
    database = env.get('BUILDINGFLOW_TEST_DATABASE', '')
    if not database:
        found.append(('TEST_DATABASE_MISSING', 'BUILDINGFLOW_TEST_DATABASE'))
    else:
        settings = connection_settings(database)
        if settings.get('host', '').lower() not in LOOPBACK:
            found.append(('DATABASE_NOT_LOOPBACK', 'BUILDINGFLOW_TEST_DATABASE'))
        name = settings.get('database', '')
        if not TEST_DATABASE.fullmatch(name):
            found.append(('DATABASE_NOT_TEST', 'BUILDINGFLOW_TEST_DATABASE'))
        if env.get('POSTGRES_DB') and name == env['POSTGRES_DB']:
            found.append(('DATABASE_IS_MAIN', 'BUILDINGFLOW_TEST_DATABASE'))
    for name in LOOPBACK_URLS:
        if env.get(name) and (urlsplit(env[name]).hostname or '').lower() not in LOOPBACK:
            found.append(('SERVICE_NOT_LOOPBACK', name))
    for name in LOOPBACK_HOSTS:
        if env.get(name) and env[name].strip().lower() not in LOOPBACK:
            found.append(('SERVICE_NOT_LOOPBACK', name))
    if env.get('ContactVerification__PhoneEnabled', '').strip().lower() in TRUE:
        found.append(('REAL_SMS_PROVIDER', 'ContactVerification__PhoneEnabled'))
    for name in sorted(key for key in env if key.startswith('PersonRefactor__')):
        found.append(('FEATURE_FLAG_LEAK', name))
    return found


def new_run_id(now=None):
    stamp = (now or datetime.now(timezone.utc)).strftime('%Y%m%dT%H%M%SZ')
    return f'qa-{stamp}-{secrets.token_hex(3)}'


def main():
    found = violations(os.environ)
    for code, name in found:
        print(f'[HATA] QA guard {code}: {name}', file=sys.stderr)
    if found:
        print(f'[QA GUARD] RED: {len(found)} kural ihlali; test çalıştırılmadı.', file=sys.stderr)
        return 1
    print('[QA GUARD] GEÇTİ: izole test veritabanı ve loopback servisler doğrulandı.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
