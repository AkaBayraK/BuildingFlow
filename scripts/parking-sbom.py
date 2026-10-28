#!/usr/bin/env python3
"""Flatten declared web/mobile lock and restored NuGet components; no install or scanner.
Container/native OS components and a resolved dependency graph are separate evidence.
"""
import argparse, base64, datetime, hashlib, json, subprocess, uuid
from pathlib import Path
from urllib.parse import quote
root = Path(__file__).resolve().parents[1]
def generate():
    inventory = json.loads((root / 'docs/licenses/package-inventory.json').read_text())
    reviewed = {(r['ecosystem'].lower(), r['name'].lower(), r['version']): r['license'] for r in inventory}
    components = {}
    inputs = {}
    def add(ecosystem, name, version, license, source, direct=False, integrity=None):
        if not license: raise ValueError('Missing declared license: ' + name)
        ref = 'pkg:' + ecosystem + '/' + quote(name.lower() if ecosystem == 'nuget' else name, safe='/') + '@' + quote(version, safe='.-+')
        component = components.setdefault(ref, {'type':'library', 'bom-ref':ref, 'name':name, 'version':version, 'purl':ref, 'licenses':[{'expression':license}], 'properties':[]})
        component['properties'].append({'name':'buildingflow:source', 'value':source})
        component['properties'].append({'name':'buildingflow:relationship', 'value':'direct' if direct else 'transitive-or-platform-lock'})
        if name == 'node-forge': component['properties'].append({'name':'buildingflow:selected-license', 'value':'BSD-3-Clause'})
        if integrity and integrity.startswith('sha512-'):
            digest = base64.b64decode(integrity[7:], validate=True).hex()
            if len(digest) != 128: raise ValueError('Invalid lock integrity')
            component['hashes'] = [{'alg':'SHA-512','content':digest}]
    for folder in ['frontend/buildingflow-web', 'mobile/buildingflow-mobile']:
        path = root / folder / 'package-lock.json'; lock = json.loads(path.read_text()); inputs[str(path.relative_to(root))] = hashlib.sha256(path.read_bytes()).hexdigest()
        direct = set(lock['packages'][''].get('dependencies', {})) | set(lock['packages'][''].get('devDependencies', {}))
        for location, package in lock['packages'].items():
            if not location: continue
            name = location.split('node_modules/')[-1]
            add('npm',name,package['version'],package.get('license'),folder+':'+location,location=='node_modules/'+name and name in direct,package.get('integrity'))
    for project in sorted((root / 'backend').glob('**/*.csproj')):
        path=project.parent/'obj/project.assets.json'
        if not path.exists(): raise ValueError('Restore required: '+str(project.relative_to(root)))
        data=json.loads(path.read_text()); direct={n.lower() for f in data['project']['frameworks'].values() for n in f.get('dependencies',{})}
        for key,package in data['libraries'].items():
            if package['type']!='package': continue
            name,version=key.rsplit('/',1)
            add('nuget',name,version,reviewed.get(('nuget',name.lower(),version)),str(project.relative_to(root)),name.lower() in direct)
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
    return {'bomFormat':'CycloneDX','specVersion':'1.6','serialNumber':'urn:uuid:'+str(uuid.uuid4()),'version':1,
        'metadata':{'timestamp':datetime.datetime.now(datetime.timezone.utc).isoformat().replace('+00:00','Z'),
        'component':{'type':'application','name':'BuildingFlow Parking release candidate','version':head[:12]+'.worktree'},
        'properties':[{'name':'buildingflow:scope','value':'Declared web/mobile lock components (including development and platform variants) and all restored solution NuGet packages, including tests; flattened, graph not asserted; excludes native SDK and container OS binaries.'},{'name':'buildingflow:git-head','value':head}]+[{'name':'buildingflow:input-sha256:'+p,'value':h} for p,h in inputs.items()]},
        'components':[components[k] for k in sorted(components)]}
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args()
    bom=generate();target=Path(args.output);target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(bom,ensure_ascii=False,indent=2)+'\n')
    print('[BUILDINGFLOW][TEST] Declared SBOM:',len(bom['components']),'unique components; container/native binaries are separate.')
