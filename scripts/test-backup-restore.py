#!/usr/bin/env python3
"""Backup/restore rehearsal with an owned fixture only.

Creates buildingflow-test-backup-* PostgreSQL, Keycloak and SeaweedFS resources, backs them up while quiesced,
restores into new isolated targets and compares schema/data, identity and object hashes. Live services,
volumes and the developer databases are never read or written. Secrets are generated per run and never printed.
"""
import http.client,urllib.error,hashlib,importlib.util,json,os,pathlib,secrets,shutil,subprocess,sys,tarfile,tempfile,time,uuid
root=pathlib.Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('backup_restore',root/'scripts/backup_restore.py');br=importlib.util.module_from_spec(spec);spec.loader.exec_module(br)
KEYCLOAK_IMAGE='quay.io/keycloak/keycloak:26.7.0'
REALM='buildingflow-restore-fixture'
prefix='buildingflow-test-backup-'+uuid.uuid4().hex[:12]
created=[]
def docker(*args,**kw):return br.docker(*args,**kw)
def own(kind,name):created.append((kind,name));return name
def wait(check,what,seconds=180):
 deadline=time.time()+seconds
 while time.time()<deadline:
  try:
   if check():return
  except (RuntimeError,OSError,http.client.HTTPException,urllib.error.URLError):pass
  time.sleep(2)
 raise RuntimeError(what+' did not become ready')
def postgres(name,network,user,db,password):
 docker('run','-d','--name',own('container',name),'--network',network,'-e','POSTGRES_USER='+user,'-e','POSTGRES_DB='+db,'-e','POSTGRES_PASSWORD',br.PG_IMAGE,env=dict(os.environ,POSTGRES_PASSWORD=password))
 wait(lambda:docker('exec',name,'pg_isready','-h','127.0.0.1','-U',user,'-d',db) is not None and br.sql(name,user,db,'SELECT 1;').strip()=='1','PostgreSQL fixture')
def keycloak(name,network,db_host,db_name,db_user,db_password,admin_password):
 env=dict(os.environ,KC_DB_PASSWORD=db_password,KC_BOOTSTRAP_ADMIN_PASSWORD=admin_password)
 docker('run','-d','--name',name,'--network',network,'--memory','1g','-e','JAVA_OPTS_KC_HEAP=-Xms128m -Xmx512m','-e','KC_DB=postgres','-e',f'KC_DB_URL=jdbc:postgresql://{db_host}/{db_name}','-e','KC_DB_USERNAME='+db_user,'-e','KC_DB_PASSWORD','-e','KC_BOOTSTRAP_ADMIN_USERNAME=fixture-admin','-e','KC_BOOTSTRAP_ADMIN_PASSWORD',KEYCLOAK_IMAGE,'start-dev',env=env)
 wait(lambda:kcadm(name,admin_password,'get','realms/master','--fields','realm') is not None,'Keycloak',240)
def kcadm(name,admin_password,*args):
 docker('exec',name,'/opt/keycloak/bin/kcadm.sh','config','credentials','--config','/tmp/kcadm.config','--server','http://localhost:8080','--realm','master','--user','fixture-admin','--password',admin_password)
 return docker('exec',name,'/opt/keycloak/bin/kcadm.sh',*args,'--config','/tmp/kcadm.config')
from s3_client import S3
def expect_failure(action,message):
 try:action()
 except (RuntimeError,FileExistsError,tarfile.TarError) as e:
  if message and message not in str(e):raise AssertionError(f'expected {message!r}, got {type(e).__name__}')
  return
 raise AssertionError('expected failure: '+(message or 'error'))
def compose_state():
 ids=subprocess.run(['docker','ps','-aq','--filter','label=com.docker.compose.project=buildingflow'],capture_output=True,text=True).stdout.split()
 return sorted((c['Name'],c['State']['Status'],c['State']['StartedAt']) for c in (br.inspect(i) for i in ids))
def leftovers():
 found=[]
 for kind,args in (('container',['ps','-a','--format','{{.Names}}']),('volume',['volume','ls','--format','{{.Name}}']),('network',['network','ls','--format','{{.Name}}'])):
  found+=[n for n in docker(*args).decode().split() if n.startswith((prefix,'buildingflow-test-restore-'))]
 return found
checks=[]
def ok(name):checks.append(name);print('[TEST]',name,'— PASS',flush=True)
def main(sql_path):
 live_before=compose_state()
 network=own('network',prefix+'-net');docker('network','create','--internal',network)
 app_pw,kcdb_pw,admin_pw,seaweed_pw=(secrets.token_hex(24) for _ in range(4))
 app=prefix+'-app';kcdb=prefix+'-kcdb';kc=own('container',prefix+'-kc');seaweed=prefix+'-seaweed';volume=own('volume',prefix+'-objects')
 postgres(app,network,'fixture','buildingflow',app_pw)
 with open(sql_path,'rb') as f:
  r=subprocess.run(['docker','exec','-i',app,'psql','-U','fixture','-d','buildingflow','-v','ON_ERROR_STOP=1','-q'],stdin=f,capture_output=True)
  if r.returncode:raise RuntimeError('Fixture schema migration failed')
 br.sql(app,'fixture','buildingflow',"""INSERT INTO properties (id,name,city,district,address,type,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES
 ('00000000-0000-4000-8000-000000000001','Restore Fixture A','City','District','Address','APARTMENT',3,'2026-01-01Z','fixture','2026-01-02Z','fixture',false,true),
 ('00000000-0000-4000-8000-000000000002','Restore Fixture B','City','District','Address','SITE',1,'2026-01-01Z','fixture','2026-01-01Z','fixture',true,false);""")
 migrations=int(br.sql(app,'fixture','buildingflow','SELECT count(*) FROM "__EFMigrationsHistory";'))
 postgres(kcdb,network,'keycloak','keycloak',kcdb_pw)
 keycloak(kc,network,kcdb,'keycloak','keycloak',kcdb_pw,admin_pw)
 kcadm(kc,admin_pw,'create','realms','-s','realm='+REALM,'-s','enabled=true')
 kcadm(kc,admin_pw,'create','users','-r',REALM,'-s','username=fixture-user','-s','email=fixture-user@example.test','-s','enabled=true')
 storage_network=own('network',prefix+'-s3-net');docker('network','create',storage_network)
 docker('run','-d','--name',own('container',seaweed),'--network',storage_network,'-p','127.0.0.1::8333','-e','AWS_ACCESS_KEY_ID=fixture','-e','AWS_SECRET_ACCESS_KEY','-e','S3_BUCKET=buildingflow-test-fixture','--mount','type=volume,src='+volume+',dst=/data',br.SEAWEED_IMAGE,'mini','-dir=/data','-admin.ui=false','-webdav=false','-s3.iam.readOnly=true',env=dict(os.environ,AWS_SECRET_ACCESS_KEY=seaweed_pw))
 storage_url='http://'+docker('port',seaweed,'8333').decode().strip();storage=S3(storage_url,'fixture',seaweed_pw)
 wait(lambda:storage.buckets() is not None,'SeaweedFS fixture')
 objects={}
 for key,size in (('documents/a.pdf',4096),('documents/b.png',70000),('c.bin',1)):
  storage.put('buildingflow-test-fixture',key,os.urandom(size),'application/octet-stream')
  objects[key]=hashlib.sha256(storage.get('buildingflow-test-fixture',key)[0]).hexdigest()
 ok(f'Fixture: {migrations} migrations + 2 rows, Keycloak realm/user, {len(objects)} objects')
 databases={'application':(app,'fixture','buildingflow'),'keycloak':(kcdb,'keycloak','keycloak')}
 work=pathlib.Path(tempfile.mkdtemp(prefix=prefix+'-files-'))
 try:
  expect_failure(lambda:br.backup(work/'running',databases,volume,[kc,seaweed]),'must be stopped');assert not (work/'running').exists()
  ok('Backup refused while Keycloak/SeaweedFS writers run')
  docker('stop',kc);docker('stop',seaweed)
  backup=work/'backup';manifest=br.backup(backup,databases,volume,[kc,seaweed])
  assert oct(backup.stat().st_mode&0o777)=='0o700' and all(oct(f.stat().st_mode&0o777)=='0o600' for f in backup.iterdir())
  assert set(manifest['sha256'])=={'application.dump','keycloak.dump','seaweed.tar.gz','storage-auth.json'} and manifest['seaweedFiles']
  ok('Quiesced backup: SHA256 manifest, 0700 folder / 0600 files')
  expect_failure(lambda:br.backup(backup,databases,volume,[kc,seaweed]),None);ok('Existing backup folder is never overwritten')
  def variant(name,mutate,message):
   copy=work/name;shutil.copytree(backup,copy);mutate(copy);expect_failure(lambda:br.validate_backup(copy),message);expect_failure(lambda:br.restore_check(copy),message)
  def flip(copy):
   f=copy/'application.dump';data=bytearray(f.read_bytes());data[len(data)//2]^=1;f.write_bytes(bytes(data))
  variant('tampered',flip,'checksum mismatch')
  variant('incomplete',lambda c:(c/'INCOMPLETE').write_text('x'),'Incomplete backup')
  variant('image',lambda c:(c/'manifest.json').write_text(json.dumps(dict(json.loads((c/'manifest.json').read_text()),postgresImage='postgres:latest'))),'Unsupported backup image')
  variant('missing',lambda c:(c/'keycloak.dump').unlink(),'checksum mismatch')
  evil=work/'evil.tar.gz'
  with tarfile.open(evil,'w:gz') as t:
   info=tarfile.TarInfo('../outside');info.size=1;import io;t.addfile(info,io.BytesIO(b'x'))
  expect_failure(lambda:br.archive_files(evil),'Unsafe archive member')
  ok('Tampered/incomplete/unsupported/missing backups and path-traversal archives rejected before restore')
  seen={}
  def verify(ctx):
   rows=br.sql(ctx['postgres'],'restore','application',"SELECT name||'|'||version||'|'||is_deleted FROM properties ORDER BY name;").splitlines()
   assert rows==['Restore Fixture A|3|false','Restore Fixture B|1|true'],'restored rows differ'
   assert int(br.sql(ctx['postgres'],'restore','application','SELECT count(*) FROM "__EFMigrationsHistory";'))==migrations
   restored=S3(ctx['storageUrl'],'fixture',seaweed_pw)
   wait(lambda:restored.buckets() is not None,'Restored SeaweedFS')
   seen['objects']={k:hashlib.sha256(restored.get('buildingflow-test-fixture',k)[0]).hexdigest() for k in objects}
   import urllib.request,urllib.error
   try:urllib.request.urlopen(ctx['storageUrl'],timeout=5);raise AssertionError('Restored S3 allowed anonymous access')
   except urllib.error.HTTPError as e:assert e.code==403
   restored_kc=ctx['postgres'].replace('-pg','-kc');ctx['register'](('container',restored_kc))
   keycloak(restored_kc,ctx['network'],ctx['postgres'],'keycloak',ctx['postgresUser'],ctx['password'],admin_pw)
   users=json.loads(kcadm(restored_kc,admin_pw,'get','users','-r',REALM,'-q','username=fixture-user'))
   seen['user']=[(u['username'],u.get('email')) for u in users]
  counts=br.restore_check(backup,verify)
  assert seen['objects']==objects,'restored object hashes differ'
  assert seen['user']==[('fixture-user','fixture-user@example.test')],'restored identity differs'
  ok(f'Restore to new isolated targets: {counts[0]} DB fingerprints, schema/rows, Keycloak login + realm user, {len(objects)} object SHA256')
 finally:
  shutil.rmtree(work,ignore_errors=True)
 return live_before
if __name__=='__main__':
 os.umask(0o077)
 sql=pathlib.Path(sys.argv[1] if len(sys.argv)>1 else root/'artifacts/ci/migrations.sql')
 if not sql.is_file():raise SystemExit('Run scripts/ci.sh first or pass an idempotent migrations.sql path')
 status=1;live_before=None
 try:
  live_before=main(sql);status=0
 except (AssertionError,RuntimeError,OSError,ValueError,KeyError,tarfile.TarError) as e:
  print('[HATA] Backup/restore rehearsal failed:',type(e).__name__,str(e) if isinstance(e,(AssertionError,RuntimeError)) else '',file=sys.stderr)
 finally:
  for kind,name in reversed(created):
   subprocess.run(['docker','rm','-f',name] if kind=='container' else ['docker',kind,'rm',name],capture_output=True)
 left=leftovers()
 if left:print('[HATA] Owned test resources remain:',len(left),file=sys.stderr);status=1
 elif status==0:ok('Owned fixture/restore containers, volumes and networks removed')
 if status==0 and live_before is not None:
  if compose_state()!=live_before:print('[HATA] Live compose container state changed',file=sys.stderr);status=1
  else:ok('Live compose containers untouched (name/state/start time identical)')
 if status==0:print(f'[TAMAMLANDI] Backup/restore rehearsal {len(checks)} checks passed. Live data was not backed up or restored.')
 raise SystemExit(status)
