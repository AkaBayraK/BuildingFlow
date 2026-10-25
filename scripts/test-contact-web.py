#!/usr/bin/env python3
"""Opt-in real browser API harness. Owned ephemeral PG only; close with fixture-stop."""
from pathlib import Path
import os,secrets,subprocess,tempfile,time
root=Path(__file__).resolve().parents[1]
name='buildingflow-test-web-contact-'+secrets.token_hex(4)
def docker(*args):
 return subprocess.run(['docker',*args],check=True,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout.strip()
try:
 with tempfile.TemporaryDirectory(prefix='buildingflow-test-web-contact-') as folder:
  password=secrets.token_hex(32);private=Path(folder)/'postgres.env'
  private.write_text('POSTGRES_USER=contact_test\nPOSTGRES_DB=buildingflow_test_contact_web\nPOSTGRES_PASSWORD='+password+'\n');private.chmod(0o600)
  docker('run','-d','--pull=never','--name',name,'--env-file',str(private),'-p','127.0.0.1::5432','--tmpfs','/var/lib/postgresql:rw,size=768m','--tmpfs','/docker-entrypoint-initdb.d:rw,size=1m','postgres:18.6')
  for _ in range(60):
   try:docker('exec',name,'pg_isready','-U','contact_test','-d','buildingflow_test_contact_web');break
   except subprocess.CalledProcessError:time.sleep(1)
  else:raise RuntimeError('PostgreSQL fixture unavailable')
  docker('exec',name,'psql','-U','contact_test','-d','buildingflow_test_contact_web','-c','CREATE EXTENSION IF NOT EXISTS citext WITH SCHEMA public;')
  port=docker('port',name,'5432/tcp').rsplit(':',1)[-1];env=os.environ.copy()
  env['BUILDINGFLOW_TEST_DATABASE']=f'Host=127.0.0.1;Port={port};Database=buildingflow_test_contact_web;Username=contact_test;Password={password}'
  env['BUILDINGFLOW_CONTACT_BROWSER']='1'
  for key in list(env):
   if key.startswith('BUILDINGFLOW_TEST_STORAGE_'):env.pop(key)
  code=subprocess.call(['dotnet','test','backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj','--no-restore','--disable-build-servers','-m:1','-p:UseSharedCompilation=false','--filter','FullyQualifiedName~PhoneContactBrowserHost','--logger','console;verbosity=normal'],cwd=root,env=env)
  raise SystemExit(code)
except Exception as error:
 print('[HATA] İzole web test API: '+type(error).__name__+' (hassas ayrıntı bastırıldı).');raise SystemExit(1)
finally:
 subprocess.run(['docker','rm','-f','-v',name],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
