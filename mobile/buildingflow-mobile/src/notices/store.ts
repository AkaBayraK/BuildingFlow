import type {SyncDb} from '../sync/outbox';
import {contextKey,type Context} from '../auth/contracts.ts';
import {parseNotice,validateQuery,readResult,type Kind,type Query} from './contracts.ts';
const slot=(c:Context,kind:Kind)=>contextKey(c)+':notice:'+kind;
export async function readNotices(db:SyncDb,user:string,c:Context,q:Query,now:number){const r=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,slot(c,q.kind));if(!r||r.checked_at>now||now-r.checked_at>86400000)return null;const v=JSON.parse(r.payload);if(v.page!==q.page||v.itemId!==q.itemId)return null;return parseNotice(v,user,c,q);}
export async function saveNotices(db:SyncDb,user:string,c:Context,q:Query,value:unknown,now:number){const v=parseNotice(value,user,c,q);const old=await db.getFirstAsync<{payload:string}>('SELECT payload FROM owner_snapshots WHERE user_id=? AND context_key=?',user,slot(c,q.kind));if(old&&Date.parse(JSON.parse(old.payload).asOf)>Date.parse(v.asOf))throw Error('Eski bildirim yanıtı reddedildi.');await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES(?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,slot(c,q.kind),JSON.stringify(v),now);}
export async function purgeNotices(db:SyncDb,user:string,c:Context){for(const kind of ['ANNOUNCEMENT','MESSAGE'] as Kind[])await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,slot(c,kind));await db.runAsync('DELETE FROM notice_jobs WHERE user_id=? AND context_key=?',user,contextKey(c));}
export type ReadJob={item_id:string;kind:Kind;status:'pending'|'retry'|'failed';attempts:number;next_at:number;error:number|null};
export async function noticeJobs(db:SyncDb,user:string,c:Context){return db.getAllAsync<ReadJob>('SELECT item_id,kind,status,attempts,next_at,error FROM notice_jobs WHERE user_id=? AND context_key=? ORDER BY item_id',user,contextKey(c));}
export async function enqueueRead(db:SyncDb,user:string,c:Context,kind:Kind,item:string,now:number){validateQuery({kind,page:1,itemId:item});const count=await db.getFirstAsync<{n:number}>('SELECT COUNT(*) AS n FROM notice_jobs WHERE user_id=? AND context_key=?',user,contextKey(c));if((count?.n??0)>=1000)throw Error('Bekleyen okundu kayıtlarını önce eşitleyin.');await db.runAsync("INSERT INTO notice_jobs(user_id,context_key,kind,item_id,status,attempts,next_at) VALUES(?,?,?,?,'pending',0,?) ON CONFLICT(user_id,context_key,kind,item_id) DO NOTHING",user,contextKey(c),kind,item,now);}
export async function retryReads(db:SyncDb,user:string,c:Context,now:number){await db.runAsync("UPDATE notice_jobs SET status='retry',attempts=0,next_at=? WHERE user_id=? AND context_key=? AND (error=0 OR error=429 OR error>=500)",now,user,contextKey(c));}
export type ReadPorts={db:<T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>;request:(kind:Kind,id:string)=>Promise<{status:number;json:()=>Promise<unknown>}>;active:()=>boolean;now:()=>number};
export async function flushReads(p:ReadPorts,user:string,c:Context){
 const check=()=>{if(!p.active())throw Error('Bildirim bağlamı değişti.');};check();const queue=await p.db(db=>noticeJobs(db,user,c));
 for(const job of queue.filter(j=>['pending','retry'].includes(j.status)&&j.next_at<=p.now()).slice(0,10)){
  check();const attempts=job.attempts+1;await p.db(db=>db.runAsync('UPDATE notice_jobs SET attempts=? WHERE user_id=? AND context_key=? AND kind=? AND item_id=?',attempts,user,contextKey(c),job.kind,job.item_id));
  let code=0;
  try{const response=await p.request(job.kind,job.item_id);check();code=response.status;if(code===200){readResult(await response.json(),job.kind,job.item_id);check();await p.db(db=>db.runAsync('DELETE FROM notice_jobs WHERE user_id=? AND context_key=? AND kind=? AND item_id=?',user,contextKey(c),job.kind,job.item_id));continue;}}
  catch{check();if(code===200)code=-1;}
  const retry=(code===0||code===429||code>=500)&&attempts<6;
  await p.db(db=>db.runAsync('UPDATE notice_jobs SET status=?,error=?,next_at=? WHERE user_id=? AND context_key=? AND kind=? AND item_id=?',retry?'retry':'failed',code,p.now()+Math.min(60000,1000*2**Math.min(attempts,6)),user,contextKey(c),job.kind,job.item_id));
  if(code===401||code===403)break;
 }
}

export async function purgeUnauthorizedNotices(db:SyncDb,session:import('../auth/contracts.ts').Session){
 const allowed=new Set(session.contexts.map(contextKey));
 const jobs=await db.getAllAsync<{context_key:string}>('SELECT DISTINCT context_key FROM notice_jobs WHERE user_id=?',session.userId);
 for(const row of jobs)if(!allowed.has(row.context_key))await db.runAsync('DELETE FROM notice_jobs WHERE user_id=? AND context_key=?',session.userId,row.context_key);
 const copies=await db.getAllAsync<{context_key:string}>('SELECT context_key FROM owner_snapshots WHERE user_id=?',session.userId);
 for(const row of copies)if(/:notice:(ANNOUNCEMENT|MESSAGE)$/.test(row.context_key)&&!allowed.has(row.context_key.replace(/:notice:(ANNOUNCEMENT|MESSAGE)$/,'')))await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',session.userId,row.context_key);
}
