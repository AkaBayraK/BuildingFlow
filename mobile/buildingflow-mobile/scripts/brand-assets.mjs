import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const output=resolve(root,'assets/brand');
const geometry=JSON.parse(readFileSync(resolve(output,'mark.json'),'utf8'));
function paths(colors=geometry.colors) {return geometry.paths.map(p=>'<path d="'+p.d+'" fill="none" stroke="'+colors[p.tone]+'" stroke-width="'+geometry.strokeWidth+'" stroke-linecap="round" stroke-linejoin="round"/>').join('');}
function svg(body,box='0 0 128 128'){return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="'+box+'">'+body+'</svg>';}
const light={navy:'#F5FAFF',blue:'#69BFE4',teal:'#8BD0D6'};
writeFileSync(resolve(output,'buildingflow-mark.svg'),svg(paths()));
writeFileSync(resolve(output,'buildingflow-mark-light.svg'),svg(paths(light)));
writeFileSync(resolve(output,'buildingflow-mark-mono.svg'),svg(paths({navy:'#082B49',blue:'#082B49',teal:'#082B49'})));
writeFileSync(resolve(output,'buildingflow-logo.svg'),svg('<g transform="translate(128 24) scale(2)">'+paths()+'</g><text x="256" y="380" text-anchor="middle" fill="#082B49" font-family="Arial,Helvetica,sans-serif" font-size="54" font-weight="700" letter-spacing="-2">BuildingFlow</text>','0 0 512 512'));
writeFileSync(resolve(output,'app-icon.svg'),svg('<rect width="1024" height="1024" fill="#F6F9FC"/><g transform="translate(80 80) scale(6.75)">'+paths()+'</g>','0 0 1024 1024'));
writeFileSync(resolve(output,'adaptive-foreground.svg'),svg('<g transform="translate(224 224) scale(4.5)">'+paths()+'</g>','0 0 1024 1024'));
const preview='<rect width="1200" height="440" fill="#EDF2F7"/><rect x="20" y="20" width="370" height="400" rx="24" fill="white"/><rect x="415" y="20" width="370" height="400" rx="24" fill="#062641"/><rect x="810" y="20" width="370" height="400" rx="24" fill="white"/><g transform="translate(105 50) scale(1.6)">'+paths()+'</g><g transform="translate(500 50) scale(1.6)">'+paths({navy:'#F5FAFF',blue:'#F5FAFF',teal:'#F5FAFF'})+'</g><rect x="895" y="54" width="200" height="200" rx="44" fill="#F6F9FC"/><g transform="translate(924 83) scale(1.1)">'+paths()+'</g><g font-family="Arial,Helvetica,sans-serif" font-size="23" font-weight="700" text-anchor="middle"><text x="205" y="310" fill="#082B49">1 · Referans / Ana marka</text><text x="600" y="310" fill="#F5FAFF">2 · Tek renk / Koyu zemin</text><text x="995" y="310" fill="#082B49">3 · Uygulama ikonu</text></g><g font-family="Arial,Helvetica,sans-serif" font-size="17" text-anchor="middle"><text x="205" y="352" fill="#617187">Mobil ve merkez için önerim</text><text x="600" y="352" fill="#C0D4E2">Aynı işaret, daha yalın kullanım</text><text x="995" y="352" fill="#617187">iOS / Android ana ekranı</text></g>';
writeFileSync(resolve(output,'brand-preview.svg'),svg(preview,'0 0 1200 440'));
console.log('BuildingFlow SVG marka varlıkları üretildi.');
