#!/usr/bin/env python3
"""Generate the private S3 gateway secret from local server settings; never print values."""
import json,os,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
env={}
for line in (root/'.env').read_text().splitlines():
 if '=' in line and not line.lstrip().startswith('#'):
  k,v=line.split('=',1);env[k.strip()]=v.strip().strip('"').strip("'")
key=env.get('OBJECT_STORAGE_ACCESS_KEY');secret=env.get('OBJECT_STORAGE_SECRET_KEY')
if not key or not secret:raise SystemExit('Object storage settings missing; run scripts/init-local.sh')
target=root/'deploy/seaweedfs/private-s3.json';target.parent.mkdir(parents=True,exist_ok=True)
content=json.dumps({'identities':[{'name':'buildingflow-api','credentials':[{'accessKey':key,'secretKey':secret}],'actions':['Admin','Read','Write','List','Tagging']}]})
fd=os.open(target,os.O_WRONLY|os.O_CREAT|os.O_TRUNC,0o600)
with os.fdopen(fd,'w') as f:f.write(content)
os.chmod(target,0o600)
print('Private S3 gateway configuration ready.')
