#!/usr/bin/env python3
"""Owned native acceptance services; real isolated IdP/SMTP/PG and API, fake SMS only."""
import importlib.util,json,os,secrets,subprocess,tempfile,time,urllib.parse,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('contact_identity',root/'scripts/test-contact-identity.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
f=module.Fixture();f.realm='buildingflow';name='buildingflow-test-native-contact-'+secrets.token_hex(4)
try:
 f.start();f.password='Fixture.Native1!2026'
 # Existing callback scheme and S256; passwords/keys never enter the app's public configuration.
 clients=f.admin('/clients');mobile=next(c for c in clients if c['clientId']=='mobile');mobile['redirectUris']=['buildingflow-dev://auth','http://127.0.0.1:13057/auth'];mobile['webOrigins']=['http://127.0.0.1:13057'];mobile['protocolMappers']=[{'name':'api-audience','protocol':'openid-connect','protocolMapper':'oidc-audience-mapper','config':{'included.custom.audience':'buildingflow-api','access.token.claim':'true','id.token.claim':'false'}}];f.admin('/clients/'+mobile['id'],mobile,'PUT')
 f.admin('/roles',{'name':'RESIDENT'})
 role=f.admin('/roles/RESIDENT')
 for username in ['native.owner','native.other']:
  uid=f.user(username,verified=False);f.admin('/users/'+uid+'/role-mappings/realm',[role])
 f.admin('/clients',{'clientId':'native-test-admin','enabled':True,'publicClient':False,'secret':f.secret,'serviceAccountsEnabled':True,'standardFlowEnabled':False,'directAccessGrantsEnabled':False})
 cid=next(c['id'] for c in f.admin('/clients') if c['clientId']=='native-test-admin');service=f.admin('/clients/'+cid+'/service-account-user');management=next(c['id'] for c in f.admin('/clients') if c['clientId']=='realm-management')
 roles=[f.admin('/clients/'+management+'/roles/'+n) for n in ['view-users','query-users']];f.admin('/users/'+service['id']+'/role-mappings/clients/'+management,roles)
 with tempfile.TemporaryDirectory(prefix='buildingflow-test-native-') as folder:
  password=secrets.token_hex(32);private=Path(folder)/'postgres.env';private.write_text('POSTGRES_USER=native_test\nPOSTGRES_DB=buildingflow_test_native_contact\nPOSTGRES_PASSWORD='+password+'\n');private.chmod(0o600)
  f.docker('run','-d','--pull=never','--name',name,'--env-file',str(private),'-p','127.0.0.1::5432','--tmpfs','/var/lib/postgresql:rw,size=768m','--tmpfs','/docker-entrypoint-initdb.d:rw,size=1m','postgres:18.6')
  for _ in range(60):
   try:f.docker('exec',name,'pg_isready','-U','native_test','-d','buildingflow_test_native_contact');break
   except subprocess.CalledProcessError:time.sleep(1)
  f.docker('exec',name,'psql','-U','native_test','-d','buildingflow_test_native_contact','-c','CREATE EXTENSION IF NOT EXISTS citext WITH SCHEMA public;')
  port=f.docker('port',name,'5432/tcp').rsplit(':',1)[-1];env=os.environ.copy();env['BUILDINGFLOW_TEST_DATABASE']=f'Host=127.0.0.1;Port={port};Database=buildingflow_test_native_contact;Username=native_test;Password={password}'
  env.update(BUILDINGFLOW_CONTACT_NATIVE='1',BF_NATIVE_ISSUER=f.base+'/realms/buildingflow',BF_NATIVE_ADMIN_SECRET=f.secret)
  for key in list(env):
   if key.startswith('BUILDINGFLOW_TEST_STORAGE_'):env.pop(key)
  # Only public loopback endpoints are written for the dev process; no token/password/link.
  Path('/tmp/bf-verify05-public.json').write_text(json.dumps({'issuer':f.base+'/realms/buildingflow','api':'http://127.0.0.1:13048','mail':f.mailbase,'client':'mobile'}))
  print('[BUILDINGFLOW] Native fixture public endpoints ready /tmp/bf-verify05-public.json',flush=True)
  child=subprocess.Popen(['dotnet','test','backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj','--no-build','--no-restore','--disable-build-servers','-m:1','-p:UseSharedCompilation=false','--filter','FullyQualifiedName~NativeContactApiWithRealOidc','--logger','console;verbosity=normal'],cwd=root,env=env)
  try:
   for _ in range(90):
    if child.poll() is not None:raise RuntimeError('Test host stopped')
    try:
     if f.call('http://127.0.0.1:13048/api/v1/mobile/contact-verification')[0]==401:break
    except Exception:pass
    time.sleep(1)
   else:raise RuntimeError('Test host unavailable')
   if '--probe' in sys.argv:
    probe_spec=importlib.util.spec_from_file_location('native_probe',root/'scripts/contact-native-probe.py');probe=importlib.util.module_from_spec(probe_spec);probe_spec.loader.exec_module(probe);probe.probe(f)
   if '--probe-only' in sys.argv:f.call('http://127.0.0.1:13048/fixture-stop',bearer='fixture-only')
   raise SystemExit(child.wait())
  finally:
   if child.poll() is None:
    try:f.call('http://127.0.0.1:13048/fixture-stop',bearer='fixture-only');child.wait(timeout=20)
    except Exception:child.terminate();child.wait(timeout=20)
except Exception as error:
 print('[HATA] Native kabul ortamı: '+type(error).__name__+' (hassas ayrıntı bastırıldı).');raise SystemExit(1)
finally:
 subprocess.run(['docker','rm','-f','-v',name],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);f.close()
