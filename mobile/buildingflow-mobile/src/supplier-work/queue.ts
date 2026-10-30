import {contextKey,type Context,type Session} from '../auth/contracts.ts';
import type {SyncDb} from '../sync/outbox.ts';
import {validateCommand,commandPath,commandBody,parseSnapshot,permitted,type Command,type Snapshot,type WorkOrder} from './contracts.ts';
export type JobStatus='pending'|'sending'|'retry'|'sent'|'conflict'|'blocked'|'failed'|'cancelled';
export type Job={operation_id:string;user_id:string;context_key:string;work_order_id:string;payload:string;photo_file:string|null;status:JobStatus;attempts:number;next_attempt_at:number;created_at:number;updated_at:number;error_code:number|null};
export type Files={read(name:string):Promise<string>;drop(name:string):Promise<void>};
const LEASE_MS=60*60*1000;
const copyKey=(c:Context)=>contextKey(c)+':supplier-work:copy';
export async function saveCopy(db:SyncDb,user:string,c:Context,v:Snapshot,now:number){const safe=parseSnapshot(v,user,c,v.page);await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,copyKey(c),JSON.stringify(safe),now);}
// Manager copies expire after an hour, like other manager caches; an old or foreign copy is never shown.
export async function readCopy(db:SyncDb,user:string,c:Context,page:number,now:number){
 const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,copyKey(c));
 if(!row||!permitted(c)||row.checked_at>now||now-row.checked_at>LEASE_MS)return null;
 const raw=JSON.parse(row.payload) as Snapshot;return raw?.page===page?parseSnapshot(raw,user,c,page):null;
}
export async function enqueue(db:SyncDb,session:Session,c:Context,command:Command,current:WorkOrder,now:number){
 if(!session.contexts.some(x=>contextKey(x)===contextKey(c)))throw Error('Yabancı çalışma alanı.');
 const safe=validateCommand(command,c,current),key=contextKey(c),payload=JSON.stringify(safe);
 const prior=await db.getFirstAsync<Job>('SELECT * FROM supplier_work_jobs WHERE operation_id=?',safe.operationId);
 if(prior){if(prior.user_id!==session.userId||prior.context_key!==key||prior.payload!==payload)throw Error('İşlem kimliği yeniden kullanılamaz.');return;}
 const active=await db.getFirstAsync<{n:number}>("SELECT count(*) AS n FROM supplier_work_jobs WHERE user_id=? AND context_key=? AND work_order_id=? AND status IN ('pending','sending','retry','conflict','blocked','failed')",session.userId,key,safe.workOrderId);
 if(active&&active.n>0)throw Error('Bu iş emri için bekleyen bir işlem var; önce onun sonucunu bekleyin veya vazgeçin.');
 await db.runAsync("INSERT INTO supplier_work_jobs(operation_id,user_id,context_key,work_order_id,payload,photo_file,status,attempts,next_attempt_at,created_at,updated_at) VALUES (?,?,?,?,?,?,'pending',0,?,?,?)",safe.operationId,session.userId,key,safe.workOrderId,payload,safe.photoFile,now,now,now);
}
export function visibleJobs(db:SyncDb,user:string,c:Context){return db.getAllAsync<Job>("SELECT * FROM supplier_work_jobs WHERE user_id=? AND context_key=? AND status NOT IN ('sent','cancelled') ORDER BY created_at,operation_id LIMIT 20",user,contextKey(c));}
export const jobCommand=(job:Job)=>JSON.parse(job.payload) as Command;
export function canDiscard(job:Job){return job.status==='conflict'||job.status==='blocked'||job.status==='failed'||job.status==='pending'&&job.attempts===0;}
export function canRetry(job:Job){return job.status==='failed'&&(job.error_code===0||job.error_code===429||(job.error_code??0)>=500);}
// A conflict is never resolved automatically: the server copy wins until the manager discards or re-queues with the current version.
export async function discard(db:SyncDb,files:Files,user:string,c:Context,id:string,now:number){
 const job=await db.getFirstAsync<Job>('SELECT * FROM supplier_work_jobs WHERE operation_id=? AND user_id=? AND context_key=?',id,user,contextKey(c));
 if(!job||!canDiscard(job))throw Error('Gönderimi süren işlemden vazgeçilemez; sonucu bekleyin.');
 await db.runAsync("UPDATE supplier_work_jobs SET status='cancelled',payload='',updated_at=? WHERE operation_id=?",now,id);
 if(job.photo_file)await files.drop(job.photo_file);
}
export async function retry(db:SyncDb,user:string,c:Context,id:string,now:number){
 const job=await db.getFirstAsync<Job>('SELECT * FROM supplier_work_jobs WHERE operation_id=? AND user_id=? AND context_key=?',id,user,contextKey(c));
 if(!job||!canRetry(job))throw Error('Bu işlem otomatik olarak yeniden gönderilemez.');
 await db.runAsync("UPDATE supplier_work_jobs SET status='retry',attempts=0,next_attempt_at=?,updated_at=?,error_code=NULL WHERE operation_id=?",now,now,id);
}
// Contexts removed from the authorization lose their queued actions, copies and photo files; nothing is sent for them.
export async function purgeUnauthorized(db:SyncDb,files:Files,session:Session){
 const allowed=new Set(session.contexts.filter(permitted).map(contextKey));
 const stale=await db.getAllAsync<Job>("SELECT * FROM supplier_work_jobs WHERE user_id=? AND status NOT IN ('sent','cancelled')",session.userId);
 for(const job of stale)if(!allowed.has(job.context_key)){await db.runAsync("UPDATE supplier_work_jobs SET status='cancelled',payload='',error_code=403 WHERE operation_id=?",job.operation_id);if(job.photo_file)await files.drop(job.photo_file);}
 const copies=await db.getAllAsync<{context_key:string}>("SELECT context_key FROM owner_snapshots WHERE user_id=? AND context_key LIKE '%:supplier-work:copy'",session.userId);
 for(const row of copies)if(!allowed.has(row.context_key.slice(0,-':supplier-work:copy'.length)))await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',session.userId,row.context_key);
}
export type Response={status:number;retryAfter?:number};
export type Ports={db:<T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>;files:Files;send:(path:string,method:'PUT'|'POST',body:unknown,photo:string|null)=>Promise<Response>;now:()=>number;active:()=>boolean};
export class FlushStopped extends Error{status:number;constructor(status:number){super('Tedarikçi işlemleri gönderilemedi.');this.status=status;}}
export async function flush(ports:Ports,session:Session,c:Context){
 const user=session.userId,key=contextKey(c),check=()=>{if(!ports.active())throw new FlushStopped(0);};check();
 if(!permitted(c)||!session.contexts.some(x=>contextKey(x)===key))throw new FlushStopped(403);
 await ports.db(db=>db.runAsync("UPDATE supplier_work_jobs SET status='retry',updated_at=? WHERE user_id=? AND context_key=? AND status='sending'",ports.now(),user,key));
 let sent=0;
 for(let n=0;n<10;n++){
  check();const now=ports.now();
  const job=await ports.db(db=>db.getFirstAsync<Job>("UPDATE supplier_work_jobs SET status='sending',attempts=attempts+1,updated_at=? WHERE operation_id=(SELECT operation_id FROM supplier_work_jobs WHERE user_id=? AND context_key=? AND status IN ('pending','retry') AND next_attempt_at<=? ORDER BY created_at,operation_id LIMIT 1) RETURNING *",now,user,key,now));
  if(!job)break;
  const finish=(status:JobStatus,error:number|null,delay=0)=>ports.db(db=>db.runAsync("UPDATE supplier_work_jobs SET status=?,error_code=?,next_attempt_at=?,updated_at=? WHERE operation_id=? AND status='sending'",status,error,ports.now()+delay,ports.now(),job.operation_id));
  let command:Command,photo:string|null=null;
  try{command=validateCommand(jobCommand(job),c);if(command.photoFile)photo=await ports.files.read(command.photoFile);}catch{await finish('blocked',403);continue;}
  let response:Response;
  try{response=await ports.send(commandPath(c,command),command.kind==='TRANSITION'?'PUT':'POST',commandBody(command),photo);}
  catch{check();await finish(job.attempts>=6?'failed':'retry',0,Math.min(60000,1000*2**Math.min(job.attempts,6)));break;}
  check();
  if(response.status===200){await finish('sent',null);if(command.photoFile)await ports.files.drop(command.photoFile);sent++;}
  else if(response.status===401){await finish('retry',401);throw new FlushStopped(401);}
  else if(response.status===409)await finish('conflict',409);
  else if(response.status===403||response.status===404)await finish('blocked',response.status);
  else if(response.status===429||response.status>=500){await finish(job.attempts>=6?'failed':'retry',response.status,Math.min(120000,Math.max(1000,response.retryAfter??1000*2**Math.min(job.attempts,6))));break;}
  else await finish('failed',response.status);
 }
 return sent;
}
