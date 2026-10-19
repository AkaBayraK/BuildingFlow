import {symlink,access} from 'node:fs/promises';
const target=new URL('../../frontend/buildingflow-web/node_modules/',import.meta.url);
await access(target);
try{await access(new URL('../node_modules',import.meta.url))}catch{await symlink(target,new URL('../node_modules',import.meta.url),'dir')}
console.log('Mevcut yönetim frontend bağımlılıkları kullanılıyor; yeni paket kurulmadı.');
