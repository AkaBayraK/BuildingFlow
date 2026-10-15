import {CATEGORIES,PRIORITIES,MAX_REQUEST_PHOTOS,uuid,type Category,type Priority,type PhotoRef} from '../sync/contracts.ts';
export const categoryLabels:Record<Category,string>={PLUMBING:'Su / tesisat',ELECTRICAL:'Elektrik',HEATING:'Isıtma / kombi',ELEVATOR:'Asansör',CLEANING:'Temizlik',SECURITY:'Güvenlik',OTHER:'Diğer'};
export const priorityLabels:Record<Priority,string>={LOW:'Düşük',NORMAL:'Normal',HIGH:'Yüksek',URGENT:'Acil'};
// photos are names of encrypted private files; the draft row itself never holds image bytes.
export type Draft={description:string;category:Category;priority:Priority;photos:string[]};
export const emptyDraft=():Draft=>({description:'',category:'OTHER',priority:'NORMAL',photos:[]});
const photoFile=/^photo-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.enc$/;
export function photoFileName(id:string){const name='photo-'+id.toLowerCase()+'.enc';if(!photoFile.test(name))throw Error('Geçersiz fotoğraf kimliği.');return name;}
export function parseDraft(value:unknown):Draft {
 if(!value || typeof value!=='object' || Array.isArray(value))throw Error('Geçersiz talep taslağı.');
 const d=value as Record<string,unknown>;
 if(typeof d.description!=='string' || d.description.length>2000 || !CATEGORIES.includes(d.category as Category) || !PRIORITIES.includes(d.priority as Priority) || !Array.isArray(d.photos) || d.photos.length>MAX_REQUEST_PHOTOS || d.photos.some(p=>typeof p!=='string' || !photoFile.test(p)) || new Set(d.photos).size!==d.photos.length)throw Error('Geçersiz talep taslağı.');
 return {description:d.description,category:d.category as Category,priority:d.priority as Priority,photos:[...d.photos as string[]]};
}
export type PhotoCommand={operationId:string;propertyId:string;unitId:string|null;role:string;photoBase64:string};
export function parsePhotoReceipt(value:unknown,operationId:string,recordId:string):PhotoRef {
 if(!value || typeof value!=='object')throw Error('Geçersiz fotoğraf makbuzu.');
 const r=value as {operationId:unknown;requestId:unknown;photo:{id:unknown;sha256:unknown;size:unknown}|null;replayed:unknown};
 const p=r.photo;
 if(r.operationId!==operationId || typeof r.requestId!=='string' || r.requestId.toLowerCase()!==recordId || typeof r.replayed!=='boolean' || !p || typeof p.id!=='string' || !uuid.test(p.id) || typeof p.sha256!=='string' || !/^[0-9a-f]{64}$/i.test(p.sha256) || typeof p.size!=='number' || !Number.isSafeInteger(p.size) || p.size<1 || p.size>262144)throw Error('Fotoğraf makbuzu işlemle eşleşmiyor.');
 return {id:p.id.toLowerCase(),sha256:p.sha256.toLowerCase(),size:p.size};
}
