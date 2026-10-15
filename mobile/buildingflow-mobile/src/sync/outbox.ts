import { contextKey, type Session } from '../auth/contracts.ts';
import { validateCommand, type Command, type RecordValue } from './contracts.ts';
import type { Database } from '../offline/database.ts';
export type JobStatus = 'pending'|'sending'|'retry'|'sent'|'conflict'|'blocked'|'failed'|'cancelled';
export type Job = {operation_id:string; user_id:string; context_key:string; payload:string; status:JobStatus; attempts:number; next_attempt_at:number; created_at:number; error_code:number|null};
export interface SyncDb extends Database { getAllAsync<T>(sql:string,...values:(string|number|null)[]):Promise<T[]>; }
export async function enqueue(db:SyncDb, session:Session, key:string, command:Command, now:number) {
  command=validateCommand(command,session,key);
  const prior=await db.getFirstAsync<Job>('SELECT * FROM sync_outbox WHERE operation_id=?',command.operationId);
  const payload=JSON.stringify(command);
  if(prior) {if(prior.user_id!==session.userId || prior.context_key!==key || prior.payload!==payload) throw Error('İşlem kimliği yeniden kullanılamaz.');return;}
  await db.runAsync("INSERT INTO sync_outbox(operation_id,user_id,context_key,payload,status,attempts,next_attempt_at,created_at,updated_at) VALUES (?,?,?,?,'pending',0,?,?,?)",command.operationId,session.userId,key,payload,now,now,now);
}
export async function jobs(db:SyncDb,userId:string,key:string) { return db.getAllAsync<Job>('SELECT * FROM sync_outbox WHERE user_id=? AND context_key=? ORDER BY created_at,operation_id',userId,key); }
export async function recover(db:SyncDb,userId:string,key:string,now:number) {await db.runAsync("UPDATE sync_outbox SET status='retry',next_attempt_at=?,updated_at=? WHERE user_id=? AND context_key=? AND status='sending'",now,now,userId,key);}
export async function claim(db:SyncDb,userId:string,key:string,now:number):Promise<Job|null> {
  return db.getFirstAsync<Job>("UPDATE sync_outbox SET status='sending',attempts=attempts+1,updated_at=? WHERE operation_id=(SELECT operation_id FROM sync_outbox WHERE user_id=? AND context_key=? AND status IN ('pending','retry') AND next_attempt_at<=? ORDER BY created_at,operation_id LIMIT 1) RETURNING *",now,userId,key,now);
}
export async function finish(db:SyncDb, job:Job,status:JobStatus, now:number, error:number|null=null, delay=0) {
  await db.runAsync("UPDATE sync_outbox SET status=?,updated_at=?,error_code=?,next_attempt_at=? WHERE operation_id=? AND user_id=? AND context_key=? AND status='sending'",status,now,error,now+delay,job.operation_id,job.user_id,job.context_key);
}
export async function cancel(db:SyncDb,session:Session,key:string,id:string,now:number) {
  const row=await db.getFirstAsync<Job>('SELECT * FROM sync_outbox WHERE operation_id=? AND user_id=? AND context_key=?',id,session.userId,key);
  if(!row || row.status!=='pending' || row.attempts!==0) throw Error('Gönderimi başlamış işlemin sonucu önce doğrulanmalı.');
  await db.runAsync("UPDATE sync_outbox SET status='cancelled',updated_at=? WHERE operation_id=? AND user_id=? AND context_key=? AND status='pending' AND attempts=0",now,id,session.userId,key);
}
export function canRetryJob(row:Job) { return row.status==='retry' || (row.status==='failed' && (row.error_code===0 || row.error_code===429 || (row.error_code!==null && row.error_code>=500))); }
export async function retry(db:SyncDb,session:Session,key:string,id:string,now:number) {
  const row=await db.getFirstAsync<Job>('SELECT * FROM sync_outbox WHERE operation_id=? AND user_id=? AND context_key=?',id,session.userId,key);
  if(!row || !canRetryJob(row)) throw Error('Bu işlem otomatik olarak yeniden gönderilemez.');
  validateCommand(JSON.parse(row.payload),session,key);
  await db.runAsync("UPDATE sync_outbox SET status='retry',next_attempt_at=?,updated_at=?,error_code=NULL WHERE operation_id=? AND user_id=? AND context_key=?",now,now,id,session.userId,key);
}
export async function putRecord(db:SyncDb, userId:string,key:string,record:RecordValue) {
  await db.runAsync("INSERT INTO sync_requests(user_id,context_key,record_id,payload,version,deleted,modified_at) VALUES (?,?,?,?,?,?,?) ON CONFLICT(user_id,context_key,record_id) DO UPDATE SET payload=excluded.payload,version=excluded.version,deleted=excluded.deleted,modified_at=excluded.modified_at WHERE excluded.version>=sync_requests.version",userId,key,record.id,JSON.stringify(record),record.version,record.deleted?1:0,record.modifiedAt);
}
export async function applyPull(db:SyncDb,userId:string,key:string,records:RecordValue[],now:number) {
  await db.execAsync('BEGIN IMMEDIATE');
  try {
    const existing=await db.getAllAsync<{record_id:string}>('SELECT record_id FROM sync_requests WHERE user_id=? AND context_key=?',userId,key);
    const ids=new Set(records.map(r=>r.id));
    for(const old of existing) if(!ids.has(old.record_id)) await db.runAsync('DELETE FROM sync_requests WHERE user_id=? AND context_key=? AND record_id=?',userId,key,old.record_id);
    for(const r of records) await putRecord(db,userId,key,r);
    await db.runAsync('INSERT INTO sync_state(user_id,context_key,last_pulled_at) VALUES (?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET last_pulled_at=excluded.last_pulled_at',userId,key,now);
    await db.execAsync('COMMIT');
  } catch(error){await db.execAsync('ROLLBACK');throw error;}
}
export async function purgeUnauthorized(db:SyncDb,session:Session) {
  const keys=new Set(session.contexts.map(contextKey));
  const scopes=await db.getAllAsync<{context_key:string}>('SELECT context_key FROM sync_requests WHERE user_id=? UNION SELECT context_key FROM sync_outbox WHERE user_id=?',session.userId,session.userId);
  for(const scope of scopes) if(!keys.has(scope.context_key)) {
    await db.runAsync('DELETE FROM sync_requests WHERE user_id=? AND context_key=?',session.userId,scope.context_key);
    await db.runAsync("UPDATE sync_outbox SET status='blocked',payload='',error_code=403 WHERE user_id=? AND context_key=? ",session.userId,scope.context_key);
  }
}
