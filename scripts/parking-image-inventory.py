#!/usr/bin/env python3
"""Read only the three local parking release images; retain declared OS license evidence.
This is an OS package/notice inventory, not an exhaustive binary scanner or legal approval.
"""
import argparse,hashlib,json,subprocess
from pathlib import Path
IMAGES=['buildingflow-parking08-api:test','buildingflow-parking08-worker:test','buildingflow-parking08-web:test']
READ=r'''if [ -f /var/lib/dpkg/status ]; then cat /var/lib/dpkg/status; elif [ -f /lib/apk/db/installed ]; then cat /lib/apk/db/installed; fi'''
def command(*args):return subprocess.check_output(['docker',*args],text=True,stderr=subprocess.PIPE)
def main(output):
    records=[]
    for image in IMAGES:
        info=json.loads(command('image','inspect',image))[0]
        text=command('run','--rm','--entrypoint','sh',image,'-c',READ)
        rows=[]
        for paragraph in text.strip().split('\n\n'):
            fields={}
            for line in paragraph.splitlines():
                if line and not line[0].isspace() and ':' in line:
                    key,value=line.split(':',1);fields[key]=value.strip()
            name=fields.get('Package') or fields.get('P');version=fields.get('Version') or fields.get('V')
            if name and version:rows.append({'name':name,'version':version,'architecture':fields.get('Architecture') or fields.get('A'),'declaredLicense':fields.get('L'),'licenseReview':'container copyright/notice obligations require review'})
        # No process configuration/env or service credentials are included.
        records.append({'image':image,'imageId':info['Id'],'architecture':info['Architecture'],'user':info['Config'].get('User'),'osPackages':rows,'packageDatabaseSha256':hashlib.sha256(text.encode()).hexdigest(),'scope':'OS package database only; native bundled libraries and exhaustive binary/license scan not asserted'})
    path=Path(output);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(records,indent=2)+'\n')
    for row in records:print('[BUILDINGFLOW][TEST]',row['image'],len(row['osPackages']),'OS packages; runtime user:',row['user'])
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args();main(args.output)
