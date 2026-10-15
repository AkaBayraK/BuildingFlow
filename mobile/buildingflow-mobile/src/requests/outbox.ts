import {contextKey,type Session} from '../auth/contracts.ts';
import {enqueue,type SyncDb} from '../sync/outbox.ts';
import {uuid,type Command} from '../sync/contracts.ts';
import type {PrivateFiles} from '../profile/files';
import {parseDraft,type Draft} from './contracts.ts';
export type PhotoStatus='pending'|'sending'|'retry'|'sent'|'conflict'|'blocked'|'failed'|'cancelled';
export type PhotoJob={operation_id:string;user_id:string;context_key:string;record_id:string;parent_operation_id:string;body_file:string;status:PhotoStatus;attempts:number;next_attempt_at:number;created_at:number;error_code:number|null};
async function tx<T>(db:SyncDb,task:()=>Promise<T>):Promise<T>{await db.execAsync('BEGIN IMMEDIATE');try{const value=await task();await db.execAsync('COMMIT');return value;}catch(error){await db.execAsync('ROLLBACK');throw error;}}
export async function readDraft(db:SyncDb,user:string,key:string):Promise<Draft|null>{const row=await db.getFirstAsync<{payload:string}>('SELECT payload FROM request_drafts WHERE user_id=? AND context_key=?',user,key);return row?parseDraft(JSON.parse(row.payload)):null;}
export async function saveDraft(db:SyncDb,user:string,key:string,draft:Draft,now:number){const d=parseDraft(draft);await db.runAsync('INSERT INTO request_drafts(user_id,context_key,payload,updated_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,updated_at=excluded.updated_at',user,key,JSON.stringify(d),now);}
// Photos referenced by queued jobs are never dropped with a draft.
async function owned(db:SyncDb,file:string){return !!await db.getFirstAsync("SELECT 1 AS n FROM request_photo_jobs WHERE body_file=? AND status NOT IN ('sent','cancelled')",file);}
export async function discardDraft(db:SyncDb,files:PrivateFiles,user:string,key:string){const d=await readDraft(db,user,key);await db.runAsync('DELETE FROM request_drafts WHERE user_id=? AND context_key=?',user,key);for(const f of d?.photos??[])if(!await owned(db,f))await files.drop(f);}
export async function dropDraftPhoto(db:SyncDb,files:PrivateFiles,file:string){if(!await owned(db,file))await files.drop(file);}
// The text command and its photo jobs are queued atomically; photos upload only after the server acknowledges the command.
export async function submit(db:SyncDb,session:Session,key:string,command:Command,draft:Draft,photoOperations:string[],now:number){
 const d=parseDraft(draft);
 if(command.action!=='CREATE' || photoOperations.length!==d.photos.length || photoOperations.some(id=>!uuid.test(id)) || new Set([...photoOperations,command.operationId]).size!==photoOperations.length+1)throw Error('Geçersiz talep gönderimi.');
 await tx(db,async()=>{
  await enqueue(db,session,key,command,now);
  for(let i=0;i<d.photos.length;i++)await db.runAsync("INSERT INTO request_photo_jobs(operation_id,user_id,context_key,record_id,parent_operation_id,body_file,status,attempts,next_attempt_at,created_at) VALUES (?,?,?,?,?,?,'pending',0,?,?)",photoOperations[i].toLowerCase(),session.userId,key,command.recordId.toLowerCase(),command.operationId.toLowerCase(),d.photos[i],now,now+i);
  await db.runAsync('DELETE FROM request_drafts WHERE user_id=? AND context_key=?',session.userId,key);
 });
}
export async function photoJobs(db:SyncDb,user:string,key:string){return db.getAllAsync<PhotoJob>('SELECT * FROM request_photo_jobs WHERE user_id=? AND context_key=? ORDER BY created_at,operation_id',user,key);}
export async function recover(db:SyncDb,user:string,key:string,now:number){await db.runAsync("UPDATE request_photo_jobs SET status='retry',next_attempt_at=? WHERE user_id=? AND context_key=? AND status='sending'",now,user,key);}
// A cancelled or revoked parent command can never be acknowledged, so its photos are released locally.
export async function reconcile(db:SyncDb,files:PrivateFiles,user:string,key:string){
 const orphans=await db.getAllAsync<PhotoJob&{parent:string}>("SELECT p.*,o.status AS parent FROM request_photo_jobs p JOIN sync_outbox o ON o.operation_id=p.parent_operation_id WHERE p.user_id=? AND p.context_key=? AND p.status IN ('pending','retry') AND (o.status='cancelled' OR (o.status='blocked' AND o.payload=''))",user,key);
 for(const j of orphans){await db.runAsync("UPDATE request_photo_jobs SET status=?,error_code=? WHERE operation_id=? AND user_id=?",j.parent==='cancelled'?'cancelled':'blocked',j.parent==='cancelled'?null:403,j.operation_id,user);await files.drop(j.body_file);}
}
export async function purgeUnauthorized(db:SyncDb,files:PrivateFiles,session:Session){
 const keys=new Set(session.contexts.map(contextKey));
 const rows=await db.getAllAsync<PhotoJob>("SELECT * FROM request_photo_jobs WHERE user_id=? AND status NOT IN ('sent','cancelled','blocked')",session.userId);
 for(const j of rows)if(!keys.has(j.context_key)){await db.runAsync("UPDATE request_photo_jobs SET status='blocked',error_code=403 WHERE operation_id=? AND user_id=?",j.operation_id,session.userId);await files.drop(j.body_file);}
 const drafts=await db.getAllAsync<{context_key:string}>('SELECT context_key FROM request_drafts WHERE user_id=?',session.userId);
 for(const d of drafts)if(!keys.has(d.context_key))await discardDraft(db,files,session.userId,d.context_key);
}
export async function claim(db:SyncDb,user:string,key:string,now:number):Promise<PhotoJob|null>{
 return db.getFirstAsync<PhotoJob>("UPDATE request_photo_jobs SET status='sending',attempts=attempts+1 WHERE operation_id=(SELECT p.operation_id FROM request_photo_jobs p JOIN sync_outbox o ON o.operation_id=p.parent_operation_id AND o.user_id=p.user_id WHERE p.user_id=? AND p.context_key=? AND p.status IN ('pending','retry') AND p.next_attempt_at<=? AND o.status='sent' ORDER BY p.created_at,p.operation_id LIMIT 1) RETURNING *",user,key,now);
}
export async function finish(db:SyncDb,job:PhotoJob,status:PhotoStatus,now:number,error:number|null=null,delay=0){await db.runAsync("UPDATE request_photo_jobs SET status=?,error_code=?,next_attempt_at=? WHERE operation_id=? AND user_id=? AND status='sending'",status,error,now+delay,job.operation_id,job.user_id);}
export async function complete(db:SyncDb,files:PrivateFiles,job:PhotoJob){await db.runAsync("UPDATE request_photo_jobs SET status='sent',error_code=NULL WHERE operation_id=? AND user_id=? AND status='sending'",job.operation_id,job.user_id);await files.drop(job.body_file);}
export function canRetry(j:PhotoJob){return j.status==='retry' || (j.status==='failed' && (j.error_code===0 || j.error_code===429 || j.error_code===-1 || (j.error_code??0)>=500));}
// After an attempt the server may hold the photo; discarding then only stops local retries.
export function canDiscard(j:PhotoJob){return ['pending','retry','conflict','blocked','failed'].includes(j.status) && j.status!=='sending';}
export async function retry(db:SyncDb,user:string,key:string,id:string,now:number){const j=await db.getFirstAsync<PhotoJob>('SELECT * FROM request_photo_jobs WHERE operation_id=? AND user_id=? AND context_key=?',id,user,key);if(!j || !canRetry(j))throw Error('Bu fotoğraf yeniden gönderilemez.');await db.runAsync("UPDATE request_photo_jobs SET status='retry',next_attempt_at=?,error_code=NULL WHERE operation_id=? AND user_id=?",now,id,user);}
export async function discard(db:SyncDb,files:PrivateFiles,user:string,key:string,id:string){const j=await db.getFirstAsync<PhotoJob>('SELECT * FROM request_photo_jobs WHERE operation_id=? AND user_id=? AND context_key=?',id,user,key);if(!j || !canDiscard(j))throw Error('Gönderilmekte olan fotoğraf silinemez.');await db.runAsync("UPDATE request_photo_jobs SET status='cancelled' WHERE operation_id=? AND user_id=?",id,user);await files.drop(j.body_file);}
