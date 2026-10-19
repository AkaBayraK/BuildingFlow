import {enqueue,type Job,type SyncDb} from '../sync/outbox.ts';
import type {PrivateFiles} from '../profile/files';
import {base64Bytes,MAX_PHOTO_BYTES} from '../profile/contracts.ts';
import {contextKey,sameContextModel,type Session,type Context} from '../auth/contracts.ts';
import {validateCommand,categories,priorities,uuid,type Command,type Category,type Priority} from '../sync/contracts.ts';
export type Draft={operationId:string;recordId:string;description:string;category:Category;priority:Priority;photoBase64:string|null};
export function parseDraft(v:unknown):Draft{
 if(!v||typeof v!=='object')throw Error('Talep taslağı geçersiz.');const d=v as Draft;
 if(!uuid.test(d.operationId)||!uuid.test(d.recordId)||d.operationId.startsWith('00000000-0000-0000-0000-000000000000')||d.recordId.startsWith('00000000-0000-0000-0000-000000000000')||typeof d.description!=='string'||d.description.length>2000||!Object.hasOwn(categories,d.category)||!Object.hasOwn(priorities,d.priority))throw Error('Talep taslağı geçersiz.');
 if(d.photoBase64!==null&&(typeof d.photoBase64!=='string'||d.photoBase64.length>350000||!d.photoBase64.startsWith('/9j/')||base64Bytes(d.photoBase64).length>MAX_PHOTO_BYTES))throw Error('Fotoğraf JPEG biçiminde ve 256 KB altında olmalı.');
 return {operationId:d.operationId.toLowerCase(),recordId:d.recordId.toLowerCase(),description:d.description,category:d.category,priority:d.priority,photoBase64:d.photoBase64};
}
export async function loadDraft(db:SyncDb,files:PrivateFiles,user:string,key:string){const row=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_drafts WHERE user_id=? AND context_key=?',user,key);return row?parseDraft(JSON.parse(await files.read(row.body_file))):null;}
export async function saveDraft(db:SyncDb,files:PrivateFiles,user:string,key:string,draft:Draft,fileId:string,now:number){
 const d=parseDraft(draft);if(!uuid.test(fileId))throw Error('Taslak dosyası geçersiz.');
 const owner=await db.getFirstAsync<{user_id:string}>('SELECT user_id FROM account LIMIT 1');if(owner?.user_id!==user)throw Error('Hesap eşleşmiyor.');
 const old=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_drafts WHERE user_id=? AND context_key=?',user,key);
 const name='request-draft-'+fileId+'.enc';await files.write(name,JSON.stringify(d));
 try{await db.runAsync('INSERT INTO request_drafts(user_id,context_key,body_file,updated_at) VALUES(?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET body_file=excluded.body_file,updated_at=excluded.updated_at',user,key,name,now);}catch(e){await files.drop(name);throw e;}
 if(old&&old.body_file!==name)await files.drop(old.body_file);return d;
}
export async function dropDraft(db:SyncDb,files:PrivateFiles,user:string,key:string){const row=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_drafts WHERE user_id=? AND context_key=?',user,key);await db.runAsync('DELETE FROM request_drafts WHERE user_id=? AND context_key=?',user,key);if(row)await files.drop(row.body_file);}
export async function queueDraft(db:SyncDb,files:PrivateFiles,session:Session,c:Context,draft:Draft,now:number){
 const d=parseDraft(draft),key=contextKey(c);
 const command=validateCommand({...(c.canonical?{contextKind:c.canonical.kind,contextId:c.canonical.id}:{}),operationId:d.operationId,recordId:d.recordId,propertyId:c.propertyId,unitId:c.unitId,role:c.role,action:'CREATE',expectedVersion:0,description:d.description,category:d.category,priority:d.priority,photoAction:d.photoBase64?'REPLACE':'KEEP'},session,key);
 const old=await db.getFirstAsync<Job>('SELECT * FROM sync_outbox WHERE operation_id=?',d.operationId);
 if(old){if(old.user_id!==session.userId||old.context_key!==key||old.payload!==JSON.stringify(command))throw Error('İşlem kimliği çakışıyor.');if(d.photoBase64){const photo=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_files WHERE operation_id=? AND user_id=?',d.operationId,session.userId);if(!photo||await files.read(photo.body_file)!==d.photoBase64)throw Error('Aynı işlem kimliği farklı fotoğrafla kullanılamaz.');}await dropDraft(db,files,session.userId,key);return;}
 const file='request-job-'+d.operationId+'.enc';
 const count=await db.getFirstAsync<{n:number}>('SELECT COUNT(*) AS n FROM request_files WHERE user_id=?',session.userId);if(d.photoBase64&&(count?.n??0)>=40)throw Error('Yerel fotoğraf sınırına ulaşıldı. Gönderilmiş taleplerin fotoğraflarını görüntüleyip eşitleyin.');
 if(d.photoBase64)await files.write(file,d.photoBase64);
 await db.execAsync('BEGIN IMMEDIATE');
 try{await enqueue(db,session,key,command,now);if(d.photoBase64)await db.runAsync('INSERT INTO request_files(operation_id,user_id,context_key,body_file) VALUES(?,?,?,?)',d.operationId,session.userId,key,file);await db.execAsync('COMMIT');}
 catch(e){await db.execAsync('ROLLBACK');if(d.photoBase64)await files.drop(file);throw e;}
 await dropDraft(db,files,session.userId,key);
}
export async function wireCommand(db:SyncDb,files:PrivateFiles,user:string,key:string,c:Command){
 if(c.photoAction!=='REPLACE')return c;
 const row=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_files WHERE operation_id=? AND user_id=? AND context_key=?',c.operationId,user,key);if(!row)throw Error('Fotoğraf dosyası bulunamadı.');
 const photoBase64=await files.read(row.body_file);parseDraft({operationId:c.operationId,recordId:c.recordId,description:c.description,category:c.category,priority:c.priority,photoBase64});return {...c,photoBase64};
}
export async function cleanRequestFiles(db:SyncDb,files:PrivateFiles,session:Session){
 const keys=new Set(session.contexts.map(contextKey));
 const copies=await db.getAllAsync<{context_key:string;record_id:string;photo_id:string;body_file:string}>('SELECT * FROM request_photo_cache WHERE user_id=?',session.userId);
 for(const copy of copies){
  if(!sameContextModel(session,copy.context_key))continue;
  const current=await db.getFirstAsync<{payload:string;deleted:number}>('SELECT payload,deleted FROM sync_requests WHERE user_id=? AND context_key=? AND record_id=?',session.userId,copy.context_key,copy.record_id);
  if(!keys.has(copy.context_key)||current&&(current.deleted===1||JSON.parse(current.payload).photoId!==copy.photo_id)){
  await db.runAsync('DELETE FROM request_photo_cache WHERE user_id=? AND context_key=? AND record_id=?',session.userId,copy.context_key,copy.record_id);await dropUnusedPhoto(db,files,session.userId,copy.body_file);
 }}

 const drafts=await db.getAllAsync<{context_key:string;body_file:string}>('SELECT context_key,body_file FROM request_drafts WHERE user_id=?',session.userId);
 for(const d of drafts)if(!session.authorization&&sameContextModel(session,d.context_key)&&!keys.has(d.context_key))await dropDraft(db,files,session.userId,d.context_key);
 const rows=await db.getAllAsync<{operation_id:string;context_key:string;body_file:string;status:string}>('SELECT f.*,o.status FROM request_files f JOIN sync_outbox o ON o.operation_id=f.operation_id WHERE f.user_id=?',session.userId);
 for(const f of rows)if(sameContextModel(session,f.context_key)&&(!session.authorization&&!keys.has(f.context_key)||f.status==='cancelled')){await files.drop(f.body_file);await db.runAsync('DELETE FROM request_files WHERE operation_id=? AND user_id=?',f.operation_id,session.userId);}
}

export async function readPhotoCopy(db:SyncDb,files:PrivateFiles,user:string,key:string,record:string,photo:string,sha:string,now:number){
 const row=await db.getFirstAsync<{body_file:string;checked_at:number}>('SELECT body_file,checked_at FROM request_photo_cache WHERE user_id=? AND context_key=? AND record_id=? AND photo_id=? AND sha256=?',user,key,record,photo,sha.toLowerCase());
 if(!row||row.checked_at>now||now-row.checked_at>86400000)return null;return files.read(row.body_file);
}
export async function savePhotoCopy(db:SyncDb,files:PrivateFiles,user:string,key:string,record:string,photo:string,sha:string,value:string,now:number){
 const old=await db.getFirstAsync<{body_file:string}>('SELECT body_file FROM request_photo_cache WHERE user_id=? AND context_key=? AND record_id=?',user,key,record);
 const file='request-photo-'+photo+'.enc';await files.write(file,value);
 await db.runAsync('INSERT INTO request_photo_cache(user_id,context_key,record_id,photo_id,sha256,body_file,checked_at) VALUES(?,?,?,?,?,?,?) ON CONFLICT(user_id,context_key,record_id) DO UPDATE SET photo_id=excluded.photo_id,sha256=excluded.sha256,body_file=excluded.body_file,checked_at=excluded.checked_at',user,key,record,photo,sha.toLowerCase(),file,now);
 if(old&&old.body_file!==file)await dropUnusedPhoto(db,files,user,old.body_file);
 const excess=await db.getAllAsync<{context_key:string;record_id:string;body_file:string}>('SELECT context_key,record_id,body_file FROM request_photo_cache WHERE user_id=? ORDER BY checked_at DESC,record_id LIMIT -1 OFFSET 40',user);
 for(const old of excess){await db.runAsync('DELETE FROM request_photo_cache WHERE user_id=? AND context_key=? AND record_id=?',user,old.context_key,old.record_id);await dropUnusedPhoto(db,files,user,old.body_file);}
}

export async function localSubmittedPhoto(db:SyncDb,files:PrivateFiles,user:string,key:string,record:string,photoId:string,sha:string){
 const cached=await db.getFirstAsync<{payload:string}>('SELECT payload FROM sync_requests WHERE user_id=? AND context_key=? AND record_id=? AND deleted=0',user,key,record);
 if(!cached)return null;const value=JSON.parse(cached.payload);if(value.photoId!==photoId||value.photoSha256?.toLowerCase()!==sha.toLowerCase())return null;
 const uploads=await db.getAllAsync<{payload:string;body_file:string}>("SELECT o.payload,f.body_file FROM request_files f JOIN sync_outbox o ON o.operation_id=f.operation_id WHERE f.user_id=? AND f.context_key=? AND o.status='sent'",user,key);
 for(const u of uploads){const command=JSON.parse(u.payload);if(command.recordId===record&&command.expectedVersion+1===value.version&&command.photoAction==='REPLACE')return files.read(u.body_file);}
 return null;
}

async function dropUnusedPhoto(db:SyncDb,files:PrivateFiles,user:string,file:string){const refs=await db.getFirstAsync<{n:number}>('SELECT COUNT(*) AS n FROM request_photo_cache WHERE user_id=? AND body_file=?',user,file);if(!refs?.n)await files.drop(file);}
