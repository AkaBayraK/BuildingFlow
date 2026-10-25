#!/usr/bin/env python3
"""Run OTP acceptance using only an owned ephemeral PostgreSQL and local HTTP fixtures."""
from pathlib import Path
import os
import secrets
import subprocess
import sys
import tempfile
import time

root=Path(__file__).resolve().parents[1]
name='buildingflow-test-otp-'+secrets.token_hex(4)
def docker(*args):
    return subprocess.run(['docker',*args],check=True,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout.strip()

try:
    with tempfile.TemporaryDirectory(prefix='buildingflow-test-otp-') as folder:
        password=secrets.token_hex(32)
        private=Path(folder)/'postgres.env';private.write_text('POSTGRES_USER=otp_test\nPOSTGRES_DB=buildingflow_test_otp\nPOSTGRES_PASSWORD='+password+'\n');private.chmod(0o600)
        docker('run','-d','--pull=never','--name',name,'--env-file',str(private),'-p','127.0.0.1::5432','--tmpfs','/var/lib/postgresql:rw,size=768m','--tmpfs','/docker-entrypoint-initdb.d:rw,size=1m','postgres:18.6','-c','max_connections=80')
        for _ in range(60):
            try:
                docker('exec',name,'pg_isready','-U','otp_test','-d','buildingflow_test_otp');break
            except subprocess.CalledProcessError:time.sleep(1)
        else:raise RuntimeError('PostgreSQL fixture unavailable')
        docker('exec',name,'psql','-U','otp_test','-d','buildingflow_test_otp','-c','CREATE EXTENSION IF NOT EXISTS citext WITH SCHEMA public;')
        port=docker('port',name,'5432/tcp').rsplit(':',1)[-1]
        env=os.environ.copy();env['BUILDINGFLOW_TEST_DATABASE']=f'Host=127.0.0.1;Port={port};Database=buildingflow_test_otp;Username=otp_test;Password={password}'
        for key in list(env):
            if key.startswith('BUILDINGFLOW_TEST_STORAGE_'):env.pop(key)
        selected='FullyQualifiedName~PhoneOtp|FullyQualifiedName~AccountPhoneVerificationTests|FullyQualifiedName~ContactVerificationTests|FullyQualifiedName~ContactStateIsShared|FullyQualifiedName~LegacyProfileWrites|FullyQualifiedName~ContactEndpointsReject|FullyQualifiedName~BuildingFlow.IntegrationTests.PersistenceTests|FullyQualifiedName~BuildingFlow.IntegrationTests.MigrationTests|FullyQualifiedName~ContactVerificationMigrationTests|FullyQualifiedName~AuthenticationTests|FullyQualifiedName~RegistrationSmsIsMaskedEncryptedAndDeliveredByMock'
        result=subprocess.run(['dotnet','test','backend/BuildingFlow.sln','--no-restore','--disable-build-servers','-m:1','-p:UseSharedCompilation=false','--filter',selected,'--logger','console;verbosity=minimal'],cwd=root,env=env)
        sys.exit(result.returncode)
except Exception as error:
    print('[HATA] İzole OTP test ortamı tamamlanamadı: '+type(error).__name__+' (hassas içerik bastırıldı).',file=sys.stderr)
    sys.exit(1)
finally:
    subprocess.run(['docker','rm','-f','-v',name],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
