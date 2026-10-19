import {readFileSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import config from './config.cjs';
import {mobileLicenses} from './licenses.mjs';
import {readinessIssues,auditIssues} from './gates.mjs';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const hook=process.argv.includes('--build-hook');
if(hook&&process.env.BUILDINGFLOW_APP_VARIANT==='development'&&process.env.EAS_BUILD_PROFILE!=='production'){console.log('[BuildingFlow] Geliştirme profili; üretim kabulü çalıştırılmadı.');process.exit(0);}
const issues=config.configurationIssues(process.env);
try{issues.push(...mobileLicenses().issues);}catch{issues.push('LICENSE:unavailable');}
if(process.env.BUILDINGFLOW_APP_VARIANT!=='production')issues.push('RELEASE_VARIANT:production-required');
try{issues.push(...readinessIssues(JSON.parse(readFileSync(resolve(root,'release/readiness.json'),'utf8'))));}catch{issues.push('READINESS:unavailable');}
try{const run=spawnSync('npm',['audit','--json'],{cwd:root,encoding:'utf8',timeout:60000,maxBuffer:4*1024*1024});if(run.error||run.signal||![0,1].includes(run.status))issues.push('AUDIT:unavailable');else issues.push(...auditIssues(JSON.parse(run.stdout)));}catch{issues.push('AUDIT:unavailable');}
if(issues.length){for(const code of [...new Set(issues)])console.error('[BuildingFlow release] '+code);console.error('[BuildingFlow] Üretim ön kontrolü geçmedi. Değerler, anahtarlar veya ham audit çıktısı loglanmadı.');process.exitCode=1;}else console.log('[BuildingFlow] Yerel yayın ön kontrolü geçti. Bu sonuç mağazaya gönderim veya yayın onayı değildir.');
