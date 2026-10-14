#!/usr/bin/env python3
"""Cold backup and destructive-free restore rehearsal. Dumps contain sensitive data.
No raw Docker output/credentials/SQL/object bodies are logged. Restore creates only owned temporary resources.
"""
import argparse,hashlib,json,os,pathlib,secrets,subprocess,tarfile,time,uuid
PG_IMAGE='postgres:17.6-alpine'
SEAWEED_IMAGE='chrislusf/seaweedfs:4.48@sha256:4e61d15fd35994cb1e43e1e553dff106794841fd9a99ade2fc8c8bfce4d7872d'
def docker(*args,stdin=None,output=None,env=None):
 r=subprocess.run(['docker',*args],input=stdin,stdout=output or subprocess.PIPE,stderr=subprocess.PIPE,env=env)
 if r.returncode:raise RuntimeError('Docker command failed; check the named service privately')
 return r.stdout or b''
def inspect(name):return json.loads(docker('inspect',name))[0]
def wait_pg(name):
 for _ in range(60):
  # The image's init-time server listens only on the socket; TCP readiness means the final server.
  try:docker('exec',name,'pg_isready','-h','127.0.0.1','-U','restore');return
  except RuntimeError:time.sleep(.5)
 raise RuntimeError('Temporary PostgreSQL did not become ready')
def sql(container,user,db,query):return docker('exec','-i',container,'psql','-v','ON_ERROR_STOP=1','-U',user,'-d',db,'-At',stdin=query.encode()).decode()
def fingerprint(container,user,db):
 tables=[line.split('\t') for line in sql(container,user,db,"SELECT table_schema || E'\\t' || table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog','information_schema') ORDER BY 1;").splitlines()]
 def q(s):return '"'+s.replace('"','""')+'"'
 commands=[]
 for schema,table in tables:
  label=json.dumps([schema,table],ensure_ascii=True).replace("'","''")
  commands.append(f"SELECT '{label}' || E'\\t' || count(*) || E'\\t' || md5(COALESCE(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),'')) FROM {q(schema)}.{q(table)} t;")
 return sql(container,user,db,'\n'.join(commands))
def digest(path):
 h=hashlib.sha256()
 with open(path,'rb') as f:
  for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
 return h.hexdigest()
def archive_files(path):
 result={}
 with tarfile.open(path,'r:gz') as tar:
  for member in tar:
   p=pathlib.PurePosixPath(member.name)
   if p.is_absolute() or '..' in p.parts or not (member.isdir() or member.isfile()):raise RuntimeError('Unsafe archive member')
   if member.isfile():result[str(p)]=hashlib.sha256(tar.extractfile(member).read()).hexdigest()
 return result
def backup(output,databases,volume,writers):
 # Operator must also quiesce native apps/external writers; container guards are additional checks.
 for name in writers:
  if inspect(name)['State']['Running']:raise RuntimeError('Writers/SeaweedFS must be stopped before a consistent backup')
 p=pathlib.Path(output).resolve();p.mkdir(mode=0o700,parents=True,exist_ok=False);os.chmod(p,0o700)
 manifest={'format':2,'postgresImage':PG_IMAGE,'seaweedImage':SEAWEED_IMAGE,'databases':{},'sha256':{}}
 try:
  for label,(container,user,db) in databases.items():
   if label not in ('application','keycloak'):raise RuntimeError('Unknown database label')
   before=fingerprint(container,user,db)
   target=p/(label+'.dump')
   with target.open('xb') as f:docker('exec',container,'pg_dump','-U',user,'-d',db,'--format=custom','--no-owner','--no-acl',output=f)
   if fingerprint(container,user,db)!=before:raise RuntimeError('Database changed during backup; quiesce all writers')
   manifest['databases'][label]={'fingerprint':before};manifest['sha256'][target.name]=digest(target);os.chmod(target,0o600)
  storage=[inspect(name) for name in writers if inspect(name)['Config']['Image']==SEAWEED_IMAGE and any(v.startswith('AWS_SECRET_ACCESS_KEY=') for v in inspect(name)['Config']['Env'])]
  if len(storage)!=1:raise RuntimeError('Expected one SeaweedFS writer for authenticated restore')
  env=dict(v.split('=',1) for v in storage[0]['Config']['Env'] if '=' in v)
  auth={key:env.get(key,'') for key in ('AWS_ACCESS_KEY_ID','AWS_SECRET_ACCESS_KEY')}
  if not all(auth.values()):raise RuntimeError('Missing object storage credentials; never restore anonymous S3')
  target=p/'storage-auth.json';target.write_text(json.dumps(auth));os.chmod(target,0o600);manifest['sha256'][target.name]=digest(target)
  target=p/'seaweed.tar.gz'
  with target.open('xb') as f:docker('run','--rm','--network','none','--mount','type=volume,src='+volume+',dst=/data,readonly','--entrypoint','tar',PG_IMAGE,'-C','/data','-czf','-','.',output=f)
  manifest['sha256'][target.name]=digest(target);manifest['seaweedFiles']=archive_files(target);os.chmod(target,0o600)
  (p/'manifest.json').write_text(json.dumps(manifest,indent=2));os.chmod(p/'manifest.json',0o600)
  return manifest
 except Exception:
  # Partial backup remains private for diagnosis; a failed run must never be used as a completed backup.
  (p/'INCOMPLETE').write_text('Backup failed. Do not restore.');os.chmod(p/'INCOMPLETE',0o600);raise

def validate_backup(folder):
 p=pathlib.Path(folder)
 if (p/'INCOMPLETE').exists():raise RuntimeError('Incomplete backup')
 m=json.loads((p/'manifest.json').read_text())
 if m.get('format')!=2 or set(m.get('sha256',{}))!={'application.dump','keycloak.dump','seaweed.tar.gz','storage-auth.json'} or set(m.get('databases',{}))!={'application','keycloak'}:raise RuntimeError('Invalid/legacy backup manifest; migrate legacy objects through S3 before creating a SeaweedFS backup')
 if m.get('postgresImage')!=PG_IMAGE or m.get('seaweedImage')!=SEAWEED_IMAGE:raise RuntimeError('Unsupported backup image version')
 for name,expected in m['sha256'].items():
  f=p/name
  if f.is_symlink() or not f.is_file() or digest(f)!=expected:raise RuntimeError('Backup checksum mismatch')
 auth=json.loads((p/'storage-auth.json').read_text())
 if set(auth)!={'AWS_ACCESS_KEY_ID','AWS_SECRET_ACCESS_KEY'} or not all(isinstance(v,str) and v for v in auth.values()):raise RuntimeError('Invalid object storage auth snapshot')
 if archive_files(p/'seaweed.tar.gz')!=m.get('seaweedFiles'):raise RuntimeError('Object archive fingerprint mismatch')
 return m

def restore_check(folder,verify=None):
 """Restore into brand-new owned resources only, compare fingerprints, then remove them."""
 p=pathlib.Path(folder).resolve();m=validate_backup(p);prefix='buildingflow-test-restore-'+uuid.uuid4().hex
 pg=prefix+'-pg';volume=prefix+'-objects';seaweed=prefix+'-seaweed';network=prefix+'-net';created=[]
 password=secrets.token_hex(24);env=dict(os.environ,POSTGRES_PASSWORD=password)
 try:
  docker('network','create','--internal',network);created.append(('network',network))
  docker('volume','create',volume);created.append(('volume',volume))
  docker('run','-d','--name',pg,'--network',network,'-e','POSTGRES_USER=restore','-e','POSTGRES_PASSWORD',PG_IMAGE,env=env);created.append(('container',pg));wait_pg(pg)
  for label,data in m['databases'].items():
   docker('exec',pg,'createdb','-U','restore',label)
   # Stream a logical dump only to a fresh, owned test DB; no existing target is accepted.
   with (p/(label+'.dump')).open('rb') as f:
    r=subprocess.run(['docker','exec','-i',pg,'pg_restore','-U','restore','-d',label,'--no-owner','--no-acl','--exit-on-error'],stdin=f,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if r.returncode:raise RuntimeError('Backup restore failed')
   if fingerprint(pg,'restore',label)!=data['fingerprint']:raise RuntimeError('Restored database fingerprint mismatch')
  with (p/'seaweed.tar.gz').open('rb') as f:
   r=subprocess.run(['docker','run','--rm','-i','--network','none','--mount','type=volume,src='+volume+',dst=/data','--entrypoint','tar',PG_IMAGE,'-C','/data','-xzf','-'],stdin=f,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   if r.returncode:raise RuntimeError('Object restore failed')
  import tempfile
  with tempfile.TemporaryDirectory(prefix='buildingflow-test-restore-check-') as temp:
   a=pathlib.Path(temp)/'objects.tar.gz'
   with a.open('wb') as f:docker('run','--rm','--network','none','--mount','type=volume,src='+volume+',dst=/data,readonly','--entrypoint','tar',PG_IMAGE,'-C','/data','-czf','-','.',output=f)
   if archive_files(a)!=m['seaweedFiles']:raise RuntimeError('Restored object fingerprint mismatch')
  if verify:
   storage_network=prefix+'-s3-net'
   docker('network','create',storage_network);created.append(('network',storage_network))
   # The private auth snapshot restores the same credentials; never start anonymous S3.
   auth=json.loads((p/'storage-auth.json').read_text());storage_env=dict(os.environ,**auth)
   docker('run','-d','--name',seaweed,'--network',storage_network,'-p','127.0.0.1::8333','-e','AWS_ACCESS_KEY_ID','-e','AWS_SECRET_ACCESS_KEY','--mount','type=volume,src='+volume+',dst=/data',SEAWEED_IMAGE,'mini','-dir=/data','-admin.ui=false','-webdav=false','-s3.iam.readOnly=true','-s3.allowDeleteBucketNotEmpty=false',env=storage_env);created.append(('container',seaweed))
   storage_url='http://'+docker('port',seaweed,'8333').decode().strip()
   verify({'network':network,'postgres':pg,'postgresUser':'restore','seaweed':seaweed,'storageUrl':storage_url,'password':password,'register':created.append})
  return len(m['databases']),len(m['seaweedFiles'])
 finally:
  for kind,name in reversed(created):
   try:docker('rm','-f',name) if kind=='container' else docker(kind,'rm',name)
   except RuntimeError:pass

def compose_backup(args):
 env_path=pathlib.Path(args.env_file)
 if env_path.exists() and any(line.strip()=='POSTGRES_DB=projects' for line in env_path.read_text().splitlines()):
  raise RuntimeError('Shared local PostgreSQL: use ../postgres-18.6/scripts/backup.sh and the shared README; keep the SeaweedFS backup separate')
 base=['compose','--env-file',args.env_file,'-f',args.compose]
 def container(service):
  value=docker(*base,'ps','-a','-q',service).decode().strip()
  if not value or '\n' in value:raise RuntimeError('Expected one container per backup service')
  return value
 app=container('postgres');keycloak=container('keycloak-db');seaweed=container('seaweedfs')
 def settings(name):return dict(v.split('=',1) for v in inspect(name)['Config']['Env'] if '=' in v)
 a=settings(app);k=settings(keycloak)
 mount=next((m for m in inspect(seaweed)['Mounts'] if m['Destination']=='/data' and m['Type']=='volume'),None)
 if not mount:raise RuntimeError('Expected dedicated SeaweedFS named volume')
 writers=[]
 for service in ('buildingflow-web','buildingflow-api','buildingflow-worker','keycloak','seaweedfs','seaweedfs-s3'):
  ids=docker(*base,'ps','-a','-q',service).decode().splitlines();writers.extend(ids)
 return backup(args.output,{'application':(app,a['POSTGRES_USER'],a['POSTGRES_DB']),'keycloak':(keycloak,k['POSTGRES_USER'],k['POSTGRES_DB'])},mount['Name'],writers)
if __name__=='__main__':
 os.umask(0o077)
 parser=argparse.ArgumentParser(description=__doc__);sub=parser.add_subparsers(dest='mode',required=True)
 b=sub.add_parser('backup');b.add_argument('--output',required=True);b.add_argument('--confirm-maintenance',required=True,action='store_true');b.add_argument('--compose',default='deploy/docker-compose.yml');b.add_argument('--env-file',default='.env')
 v=sub.add_parser('restore-check');v.add_argument('folder')
 args=parser.parse_args()
 try:
  if args.mode=='backup':compose_backup(args);print('[TEST] Private consistent backup created — PASS')
  else:restore_check(args.folder);print('[TEST] Isolated PostgreSQL/Keycloak/object restore fingerprints — PASS')
 except (RuntimeError,OSError,ValueError,KeyError,StopIteration,tarfile.TarError) as e:raise SystemExit('[HATA] Backup/restore failed: '+(str(e) if isinstance(e,RuntimeError) else type(e).__name__))
