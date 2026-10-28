#!/usr/bin/env python3
"""CycloneDX component inventory from reviewed installed/locked dependencies; no network or new package.
Dependency graph, container OS packages and legal deployment obligations are deliberately outside this inventory.
"""
import argparse,datetime,hashlib,json,pathlib,subprocess,urllib.parse,uuid
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args()
central=root/'docs/licenses/package-inventory.json';mobile=root/'mobile/buildingflow-mobile/package-lock.json'
rows=json.loads(central.read_text());lock=json.loads(mobile.read_text());components={}
def add(ecosystem,name,version,license,source):
    if not name or not version or not license:raise SystemExit('Missing component identity/license; run license gates first.')
    package='npm' if ecosystem=='npm' else 'nuget';name=name if package=='npm' else name.lower()
    purl='pkg:'+package+'/'+urllib.parse.quote(name,safe='/')+'@'+urllib.parse.quote(version,safe='')
    if purl not in components:components[purl]={'type':'library','bom-ref':purl,'name':name,'version':version,'purl':purl,'licenses':[{'expression':license}],'properties':[]}
    c=components[purl]
    if c['licenses']!=[{'expression':license}]:raise SystemExit('Conflicting license metadata; review required: '+name)
    prop={'name':'buildingflow:inventory-source','value':source}
    if prop not in c['properties']:c['properties'].append(prop)
for r in rows:add(r['ecosystem'],r['name'],r['version'],r['license'],'central-reviewed-inventory')
for path,p in lock['packages'].items():
    if path:add('npm',path.split('node_modules/')[-1],p['version'],p.get('license'),'mobile-package-lock')
base=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
bom={'bomFormat':'CycloneDX','specVersion':'1.6','serialNumber':'urn:uuid:'+str(uuid.uuid4()),'version':1,'metadata':{'timestamp':datetime.datetime.now(datetime.timezone.utc).isoformat().replace('+00:00','Z'),'component':{'type':'application','bom-ref':'buildingflow-working-tree','name':'BuildingFlow','version':'working-tree'},'properties':[{'name':'buildingflow:git-base','value':base},{'name':'buildingflow:scope','value':'web/backend/mobile component inventory; dependency relationships and container OS excluded; not a binary attestation'},{'name':'buildingflow:central-inventory-sha256','value':hashlib.sha256(central.read_bytes()).hexdigest()},{'name':'buildingflow:mobile-lock-sha256','value':hashlib.sha256(mobile.read_bytes()).hexdigest()}]},'components':sorted(components.values(),key=lambda c:c['purl'])}
output=pathlib.Path(args.output);output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(bom,ensure_ascii=False,indent=2)+'\n')
print('[BUILDINGFLOW][SBOM] CycloneDX1.6 component inventory: '+str(len(components))+' unique components; dependency graph/container OS not included.')
