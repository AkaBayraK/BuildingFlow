// Preserve installed package notices beside the lock-based SBOM; never invent license text.
import {readFile,readdir,mkdir,cp,writeFile} from 'node:fs/promises';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const frontend=resolve(root,'frontend/buildingflow-web'),output=resolve(process.argv[2]??'artifacts/website/notices');
const lock=JSON.parse(await readFile(resolve(frontend,'package-lock.json'),'utf8'));
const report={copied:[],withoutRootNotice:[],notInstalled:[]};
for(const [path,pkg] of Object.entries(lock.packages)){
 if(!path)continue;
 const source=resolve(frontend,path);if(!source.startsWith(frontend+'/node_modules/'))throw Error('Invalid lock path');
 let entries;try{entries=await readdir(source,{withFileTypes:true});}catch(e){if(e.code!=='ENOENT')throw e;report.notInstalled.push(path);continue;}
 const notices=entries.filter(e=>/^(?:licen[cs]es?|notice|copying|copyright)(?:[._-]|$)/i.test(e.name));
 if(!notices.length)report.withoutRootNotice.push({path,license:pkg.license});
 for(const entry of notices){const target=resolve(output,path,entry.name);await mkdir(dirname(target),{recursive:true});await cp(resolve(source,entry.name),target,{recursive:true});report.copied.push(path+'/'+entry.name);}
}
await mkdir(output,{recursive:true});await writeFile(resolve(output,'notice-manifest.json'),JSON.stringify(report,null,2)+'\n');
console.log(`PASS: ${report.copied.length} installed notice entries preserved; ${report.withoutRootNotice.length} packages require root-notice review (metadata evidence remains separate)`);
