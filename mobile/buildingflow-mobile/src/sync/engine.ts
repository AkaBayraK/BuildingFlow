import { contextKey, parseSession, selectContext, type Context, type Session } from '../auth/contracts.ts';
import { parsePull, parsePush, validateCommand, type Command } from './contracts.ts';
import { applyPull, claim, finish, jobs, putRecord, purgeUnauthorized, recover, type SyncDb } from './outbox.ts';
export class SyncError extends Error { status:number; constructor(status:number){super('Eşitleme tamamlanamadı.');this.status=status;} }
export type Ports = {db: <T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; request:(path:string,command?:Command)=>Promise<{status:number;json:()=>Promise<unknown>;retryAfter?:number}>; now:()=>number; active:()=>boolean};
export type Summary={pending:number;conflicts:number;blocked:number;failed:number;lastPulledAt:number|null};
export async function summary(db:SyncDb,userId:string,key:string):Promise<Summary> {
  const rows=await jobs(db,userId,key);
  const state=await db.getFirstAsync<{last_pulled_at:number}>('SELECT last_pulled_at FROM sync_state WHERE user_id=? AND context_key=?',userId,key);
  return {pending:rows.filter(r=>['pending','retry','sending'].includes(r.status)).length,conflicts:rows.filter(r=>r.status==='conflict').length,blocked:rows.filter(r=>r.status==='blocked').length,failed:rows.filter(r=>r.status==='failed').length,lastPulledAt:state?.last_pulled_at??null};
}
export async function synchronize(ports:Ports,known:Session,selected:Context):Promise<{session:Session;summary:Summary}> {
  const active=()=>{if(!ports.active())throw new SyncError(0);}; active();
  const auth=await ports.request('/api/v1/mobile/session');active();
  if(auth.status!==200)throw new SyncError(auth.status);
  const fresh=parseSession(await auth.json());
  if(fresh.userId!==known.userId)throw new SyncError(403);
  const key=contextKey(selected);
  await ports.db(db=>purgeUnauthorized(db,fresh));active();
  let context:Context;
  try {context=selectContext(fresh,key);}catch{throw new SyncError(403);}
  if(context.role==='ACCOUNTANT')throw new SyncError(403);
  await ports.db(db=>recover(db,fresh.userId,key,ports.now()));
  for(let n=0;n<5;n++){
    active();const job=await ports.db(db=>claim(db,fresh.userId,key,ports.now()));if(!job)break;
    let command:Command;
    try {command=validateCommand(JSON.parse(job.payload),fresh,key);}catch {await ports.db(db=>finish(db,job,'blocked',ports.now(),403));continue;}
    let response;
    try {response=await ports.request('/api/v1/mobile/sync/commands',command);}
    catch {
      active();await ports.db(db=>finish(db,job,job.attempts>=6?'failed':'retry',ports.now(),0,Math.min(60000,1000*2**Math.min(job.attempts,6))));break;
    }
    active();
    if(response.status===200){
      let record;
      try {record=parsePush(await response.json(),command,context);}catch {active();await ports.db(db=>finish(db,job,'failed',ports.now(),-1));throw new SyncError(502);}active();
      await ports.db(async db=>{await db.execAsync('BEGIN IMMEDIATE');try{await putRecord(db,fresh.userId,key,record);await finish(db,job,'sent',ports.now());await db.execAsync('COMMIT');}catch(error){await db.execAsync('ROLLBACK');throw error;}});
    } else if(response.status===401)throw new SyncError(401);
    else if(response.status===409)await ports.db(db=>finish(db,job,'conflict',ports.now(),409));
    else if(response.status===403 || response.status===404)await ports.db(db=>finish(db,job,'blocked',ports.now(),response.status));
    else if(response.status===429 || response.status>=500){await ports.db(db=>finish(db,job,job.attempts>=6?'failed':'retry',ports.now(),response.status,Math.min(120000,Math.max(1000,response.retryAfter??1000*2**Math.min(job.attempts,6)))));break;}
    else await ports.db(db=>finish(db,job,'failed',ports.now(),response.status));
  }
  active();
  const query=new URLSearchParams({propertyId:context.propertyId,role:context.role});if(context.unitId)query.set('unitId',context.unitId);
  const response=await ports.request('/api/v1/mobile/sync/requests?'+query);active();
  if(response.status!==200)throw new SyncError(response.status);
  const records=parsePull(await response.json(),fresh,context);active();
  await ports.db(db=>applyPull(db,fresh.userId,key,records,ports.now()));active();
  return {session:fresh,summary:await ports.db(db=>summary(db,fresh.userId,key))};
}
