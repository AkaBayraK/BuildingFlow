import type {SyncDb} from '../sync/outbox';
import type {PrivateFiles} from '../profile/files';
import {CONSENT_HOLD,parseCommand,parseList,type Command,type ConsentItem,type ConsentList,type ConsentState,type Feature,type ServerState} from './contracts.ts';
// Local consent data lives in the same encrypted per-account SQLite as the profile; logout destroys it with the database key.
export type JobStatus='pending'|'sending'|'retry'|'conflict'|'failed';
export type Job={operation_id:string;user_id:string;type_code:string;action:'APPROVE'|'WITHDRAW';payload:string;status:JobStatus;attempts:number;next_attempt_at:number;created_at:number;error:string|null};
export type Cached={list:ConsentList;checkedAt:number};
async function owner(db:SyncDb,user:string){const row=await db.getFirstAsync<{user_id:string}>('SELECT user_id FROM account LIMIT 1');if(row?.user_id!==user)throw Error('Rıza hesabı eşleşmiyor.');}
export async function cacheList(db:SyncDb,list:ConsentList,now:number){
 await owner(db,list.userId);
 await db.runAsync('INSERT INTO consent_cache(user_id,payload,checked_at) VALUES (?,?,?) ON CONFLICT(user_id) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',list.userId,JSON.stringify(list),now);
}
export async function cachedList(db:SyncDb,user:string):Promise<Cached|null>{
 const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM consent_cache WHERE user_id=?',user);
 return row?{list:parseList(JSON.parse(row.payload),user),checkedAt:row.checked_at}:null;
}
// A server receipt replaces one type's state when it describes the cached text; otherwise the caller reloads the list (returns false).
export async function applyState(db:SyncDb,user:string,state:ConsentState){
 const cached=await cachedList(db,user);const item=cached?.list.items.find(i=>i.status.typeCode===state.typeCode);
 if(!cached||!item||item.status.currentTextId!==state.currentTextId)return false;
 await cacheList(db,{userId:user,items:cached.list.items.map(i=>i===item?{...i,status:state}:i)},cached.checkedAt);
 return true;
}
export async function jobs(db:SyncDb,user:string){return db.getAllAsync<Job>('SELECT * FROM consent_jobs WHERE user_id=? ORDER BY created_at,operation_id',user);}
export async function enqueue(db:SyncDb,user:string,value:Command,now:number){
 const command=parseCommand(value);await owner(db,user);
 const prior=await db.getFirstAsync<Job>('SELECT * FROM consent_jobs WHERE operation_id=?',command.operationId);
 if(prior){if(prior.user_id!==user||prior.payload!==JSON.stringify(command))throw Error('Rıza işlem kimliği yeniden kullanılamaz.');return;}
 await db.execAsync('BEGIN IMMEDIATE');
 try{
  // An unsent decision for the same type is replaced by the newer one; anything already attempted may have reached the server and stays in order.
  await db.runAsync("DELETE FROM consent_jobs WHERE user_id=? AND type_code=? AND (status='pending' AND attempts=0 OR status IN ('conflict','failed'))",user,command.typeCode);
  await db.runAsync("INSERT INTO consent_jobs(operation_id,user_id,type_code,action,payload,status,attempts,next_attempt_at,created_at) VALUES (?,?,?,?,?,'pending',0,?,?)",command.operationId,user,command.typeCode,command.action,JSON.stringify(command),now,now);
  await db.execAsync('COMMIT');
 }catch(error){await db.execAsync('ROLLBACK');throw error;}
}
export function loadCommand(job:Job):Command{const c=parseCommand(JSON.parse(job.payload));if(c.operationId!==job.operation_id||c.typeCode!==job.type_code||c.action!==job.action)throw Error('Rıza işlemi eşleşmiyor.');return c;}
export async function recover(db:SyncDb,user:string,now:number){await db.runAsync("UPDATE consent_jobs SET status='retry',next_attempt_at=? WHERE user_id=? AND status='sending'",now,user);}
// Oldest due job first, so an approval is always sent before a later withdrawal of the same type.
export async function claim(db:SyncDb,user:string,now:number){
 return db.getFirstAsync<Job>("UPDATE consent_jobs SET status='sending',attempts=attempts+1 WHERE operation_id=(SELECT operation_id FROM consent_jobs WHERE user_id=? AND status IN ('pending','retry') AND next_attempt_at<=? AND NOT EXISTS (SELECT 1 FROM consent_jobs o WHERE o.user_id=consent_jobs.user_id AND o.status='sending') ORDER BY created_at,operation_id LIMIT 1) RETURNING *",user,now);
}
export async function finish(db:SyncDb,job:Job,status:JobStatus,error:string|null,now:number,delay=0){await db.runAsync("UPDATE consent_jobs SET status=?,error=?,next_attempt_at=? WHERE user_id=? AND operation_id=? AND status='sending'",status,error,now+delay,job.user_id,job.operation_id);}
export async function remove(db:SyncDb,job:Job){await db.runAsync('DELETE FROM consent_jobs WHERE user_id=? AND operation_id=?',job.user_id,job.operation_id);}
export function canRetry(job:Job){return job.status==='failed'&&(job.error==='NETWORK'||job.error==='HTTP_429'||/^HTTP_5/.test(job.error??''));}
export async function retry(db:SyncDb,user:string,operation:string,now:number){
 const job=await db.getFirstAsync<Job>('SELECT * FROM consent_jobs WHERE user_id=? AND operation_id=?',user,operation);
 if(!job||!canRetry(job))throw Error('Bu rıza işlemi yeniden gönderilemez.');
 await db.runAsync("UPDATE consent_jobs SET status='retry',error=NULL,next_attempt_at=? WHERE user_id=? AND operation_id=?",now,user,operation);
}
// Unsent, refused or outdated decisions can be dropped; one whose result is unknown must be confirmed by the server first.
export function canDiscard(job:Job){return job.status==='pending'&&job.attempts===0||job.status==='conflict'||job.status==='failed';}
export async function discard(db:SyncDb,user:string,operation:string){
 const job=await db.getFirstAsync<Job>('SELECT * FROM consent_jobs WHERE user_id=? AND operation_id=?',user,operation);
 if(!job||!canDiscard(job))throw Error('Gönderimi belirsiz rıza işlemi kaldırılamaz.');
 await remove(db,job);
}
// Photo jobs the server refused for missing consent go back to the queue once that consent is ACTIVE.
export async function releaseHeld(db:SyncDb,user:string,feature:Feature,now:number){
 if(feature==='PROFILE_PHOTO')await db.runAsync("UPDATE profile_jobs SET status='retry',error_code=NULL,next_attempt_at=? WHERE user_id=? AND status='failed' AND error_code=?",now,user,CONSENT_HOLD);
 else await db.runAsync("UPDATE sync_outbox SET status='retry',error_code=NULL,next_attempt_at=?,updated_at=? WHERE user_id=? AND status='blocked' AND error_code=?",now,now,user,CONSENT_HOLD);
}
export async function heldCount(db:SyncDb,user:string,feature:Feature){
 const row=feature==='PROFILE_PHOTO'?await db.getFirstAsync<{n:number}>("SELECT COUNT(*) AS n FROM profile_jobs WHERE user_id=? AND status='failed' AND error_code=?",user,CONSENT_HOLD)
  :await db.getFirstAsync<{n:number}>("SELECT COUNT(*) AS n FROM sync_outbox WHERE user_id=? AND status='blocked' AND error_code=?",user,CONSENT_HOLD);
 return row?.n??0;
}
// After a withdrawal the photos it covered leave this device at once, even before the server hears about it.
export async function dropLocalPhotos(db:SyncDb,files:PrivateFiles,user:string,feature:Feature){
 if(feature==='PROFILE_PHOTO'){
  const row=await db.getFirstAsync<{photo_file:string|null}>('SELECT photo_file FROM profile_cache WHERE user_id=?',user);
  await db.runAsync('UPDATE profile_cache SET photo_file=NULL WHERE user_id=?',user);if(row?.photo_file)await files.drop(row.photo_file);
  return;
 }
 const copies=await db.getAllAsync<{body_file:string}>('SELECT body_file FROM request_photo_cache WHERE user_id=?',user);
 await db.runAsync('DELETE FROM request_photo_cache WHERE user_id=?',user);for(const c of copies)await files.drop(c.body_file);
 // Uploads the server already accepted are copies of photos the person no longer sees; unsent ones wait for consent again.
 const sent=await db.getAllAsync<{operation_id:string;body_file:string}>("SELECT f.operation_id,f.body_file FROM request_files f JOIN sync_outbox o ON o.operation_id=f.operation_id WHERE f.user_id=? AND o.status='sent'",user);
 for(const f of sent){await db.runAsync('DELETE FROM request_files WHERE operation_id=? AND user_id=?',f.operation_id,user);await files.drop(f.body_file);}
}
export async function markSeen(db:SyncDb,user:string,textId:string,now:number){await owner(db,user);await db.runAsync('INSERT OR IGNORE INTO consent_seen(user_id,text_id,seen_at) VALUES (?,?,?)',user,textId,now);}
export async function seen(db:SyncDb,user:string){return new Set((await db.getAllAsync<{text_id:string}>('SELECT text_id FROM consent_seen WHERE user_id=?',user)).map(r=>r.text_id));}

// What the person sees: the server state with queued decisions applied in order.
export type Effective={item:ConsentItem;state:ServerState;pending:'APPROVE'|'WITHDRAW'|null;problem:Job|null;approvedVersion:number|null};
export function effective(list:ConsentList|null,queue:Job[]):Effective[]{
 if(!list)return [];
 return list.items.map(item=>{
  let state=item.status.state,pending:Effective['pending']=null,problem:Job|null=null,approvedVersion=item.status.approvedVersion;
  for(const job of queue.filter(j=>j.type_code===item.status.typeCode)){
   if(job.status==='conflict'||job.status==='failed'){problem=job;continue;}
   let c:Command;try{c=loadCommand(job);}catch{continue;}
   if(c.action==='APPROVE'){if(c.textId!==item.currentText?.id)continue;state='ACTIVE';approvedVersion=item.currentText.textVersion;pending='APPROVE';}
   else if(state==='ACTIVE'||state==='OUTDATED'){state='WITHDRAWN';approvedVersion=null;pending='WITHDRAW';}
   else pending='WITHDRAW';
  }
  return {item,state,pending,problem,approvedVersion};
 }).sort((a,b)=>a.item.status.sortOrder-b.item.status.sortOrder||a.item.status.typeCode.localeCompare(b.item.status.typeCode));
}
export const forFeature=(items:Effective[],feature:Feature)=>items.find(e=>e.item.status.feature===feature&&e.item.status.kind==='EXPLICIT_CONSENT')??null;
// Photo pickers open only when the effective consent covers the current text (a queued approval counts).
export const allows=(e:Effective|null)=>!!e&&e.state==='ACTIVE'&&!!e.item.currentText;
// Launch gate: unread or revised notices and revised explicit consents, once per text on this device; an outdated-text refusal always asks again.
export function attention(items:Effective[],seenTexts:Set<string>){
 return items.filter(e=>{
  const text=e.item.currentText;if(!text||e.pending)return false;
  if(e.problem?.error==='BF_CONSENT_TEXT_OUTDATED')return true;
  if(seenTexts.has(text.id))return false;
  return e.item.status.kind==='INFORMATION'?e.state==='NONE'||e.state==='OUTDATED':e.state==='OUTDATED';
 });
}
