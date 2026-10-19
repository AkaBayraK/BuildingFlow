import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {resolve,dirname} from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
export function releaseErrors(env){
 const errors=[];const origins={};
 for(const key of ['WEBSITE_PUBLIC_ORIGIN','WEBSITE_MANAGEMENT_ORIGIN','WEBSITE_API_ORIGIN']){
  try{const u=new URL(env[key]);if(u.username||u.password||u.pathname!=='/'||u.search||u.hash||u.protocol!=='https:'&&!(key==='WEBSITE_API_ORIGIN'&&u.protocol==='http:'&&['buildingflow-api','localhost','127.0.0.1'].includes(u.hostname)))throw Error();if(key!=='WEBSITE_API_ORIGIN'&&['localhost','127.0.0.1','[::1]'].includes(u.hostname))throw Error();origins[key]=u.origin;}catch{errors.push(key);}
 }
 if(origins.WEBSITE_PUBLIC_ORIGIN&&origins.WEBSITE_PUBLIC_ORIGIN===origins.WEBSITE_MANAGEMENT_ORIGIN)errors.push('SEPARATE_PUBLIC_AND_MANAGEMENT_ORIGINS');
 return errors;
}
export function makeBom(lock,inventory){
 const reviewed=new Map(inventory.filter(p=>p.ecosystem==='npm').map(p=>[p.name+'@'+p.version,p.license]));const components=[];
 for(const [path,p] of Object.entries(lock.packages)){
  if(!path)continue;const name=path.split('node_modules/').at(-1);if(!p.version||!p.license||reviewed.get(name+'@'+p.version)!==p.license)throw Error('Unreviewed dependency metadata');
  components.push({type:'library',name,version:p.version,'bom-ref':path,purl:'pkg:npm/'+name.replace('@','%40')+'@'+p.version,licenses:[{expression:p.license}],properties:[{name:'buildingflow:lock-path',value:path},{name:'buildingflow:development',value:String(!!p.dev)}]});
 }
 return {bomFormat:'CycloneDX',specVersion:'1.6',version:1,metadata:{component:{type:'application',name:'buildingflow-website',version:'0.1.0'},properties:[{name:'buildingflow:scope',value:'Shared frontend npm install; container OS and NuGet excluded'}]},components};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(resolve(process.argv[1])).href){
 if(process.argv[2]==='--check-env'){const errors=releaseErrors(process.env);if(errors.length){console.error('FAIL: '+errors.join(', '));process.exitCode=1;}else console.log('PASS: website production origins (values omitted)');}
 else if(process.argv[2]==='--sbom'&&process.argv[3]){const lock=JSON.parse(await readFile(resolve(root,'frontend/buildingflow-web/package-lock.json'),'utf8'));const inventory=JSON.parse(await readFile(resolve(root,'docs/licenses/package-inventory.json'),'utf8'));const output=resolve(process.argv[3]);await mkdir(dirname(output),{recursive:true});await writeFile(output,JSON.stringify(makeBom(lock,inventory),null,2)+'\n');console.log('PASS: CycloneDX npm inventory generated; OS/legal acceptance remains separate');}
 else{console.error('Usage: node website/scripts/release.mjs --check-env | --sbom <output.json>');process.exitCode=2;}
}
