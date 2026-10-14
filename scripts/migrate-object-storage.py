#!/usr/bin/env python3
"""Copy legacy S3 objects to SeaweedFS without changing/deleting the source. Print counts only."""
import argparse,hashlib,json,os,pathlib,sys,time
from s3_client import S3,S3Error
def settings(path):
 result={}
 for line in pathlib.Path(path).read_text().splitlines():
  if line.strip() and not line.lstrip().startswith('#') and '=' in line:
   k,v=line.split('=',1);result[k.strip()]=v.strip().strip('"').strip("'")
 return result
def run(args):
 env=settings(args.env_file)
 legacy=settings(args.source_env_file) if args.source_env_file else env
 old=S3('http://127.0.0.1:'+legacy.get('MINIO_PORT','9030'),legacy['MINIO_ROOT_USER'],legacy['MINIO_ROOT_PASSWORD'])
 new=S3('http://127.0.0.1:'+env.get('OBJECT_STORAGE_PORT','9040'),env['OBJECT_STORAGE_ACCESS_KEY'],env['OBJECT_STORAGE_SECRET_KEY'])
 inventory={b:list(old.objects(b)) for b in old.buckets()};count=sum(len(v) for v in inventory.values());copied=0
 if not args.copy:print(json.dumps({'buckets':len(inventory),'objects':count,'copy':False}));return
 if not args.confirm_writers_stopped:raise RuntimeError('Quiesce API/native writers before copying')
 for b,items in inventory.items():
  if b not in new.buckets():new.create(b)
  for key,size in items:
   data,headers=old.get(b,key)
   if len(data)!=size:raise RuntimeError('Source object changed during migration')
   digest=hashlib.sha256(data).hexdigest()
   try:existing,_=new.get(b,key)
   except S3Error as e:
    if e.status!=404:raise
    existing=None
   if existing is not None and hashlib.sha256(existing).hexdigest()!=digest:raise RuntimeError('Target object collision; nothing overwritten')
   if existing is None:new.put(b,key,data,headers.get('Content-Type','application/octet-stream'))
   actual,_=new.get(b,key)
   if hashlib.sha256(actual).hexdigest()!=digest:raise RuntimeError('Target object verification failed')
   copied+=1
 # A changed source list/body aborts cutover; credentials/body/key names are never emitted.
 if {b:list(old.objects(b)) for b in old.buckets()}!=inventory:raise RuntimeError('Source listing changed during migration')
 for b,items in inventory.items():
  for key,size in items:
   if hashlib.sha256(old.get(b,key)[0]).digest()!=hashlib.sha256(new.get(b,key)[0]).digest():raise RuntimeError('Source changed during migration')
 report={'provider':'SeaweedFS4.48','buckets':len(inventory),'objects':count,'verified':copied,'sourcePreserved':True}
 out=pathlib.Path(args.report);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n');os.chmod(out,0o600)
 print(json.dumps(report))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--env-file',default='.env');p.add_argument('--source-env-file');p.add_argument('--copy',action='store_true');p.add_argument('--confirm-writers-stopped',action='store_true');p.add_argument('--report',default='artifacts/storage/migration-report.json')
 try:run(p.parse_args())
 except (RuntimeError,OSError,ValueError,KeyError) as e:raise SystemExit('Storage migration failed: '+(str(e) if isinstance(e,RuntimeError) else type(e).__name__))
