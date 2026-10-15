import test from 'node:test';
import assert from 'node:assert/strict';
import { DatabaseSync } from 'node:sqlite';
import { migrate, writeSnapshot } from '../src/offline/database.ts';
import { enqueue, jobs, cancel, retry, putRecord } from '../src/sync/outbox.ts';
import { synchronize, SyncError } from '../src/sync/engine.ts';
const context={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:null,unitNumber:null,role:'MANAGER'};
const key=context.propertyId+':-:MANAGER';const session={userId:'fixture-a',username:'Fixture',contexts:[context],hasMore:false};
const command={operationId:'22222222-2222-2222-2222-222222222222',recordId:'33333333-3333-3333-3333-333333333333',propertyId:context.propertyId,unitId:null,role:'MANAGER',action:'CREATE',expectedVersion:0,description:'Fixture'};
const record={id:command.recordId,propertyId:command.propertyId,unitId:null,role:'MANAGER',description:'Fixture',version:1,deleted:false,modifiedAt:'2026-10-14T00:00:00Z'};
async function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');const db={execAsync:async q=>{sql.exec(q);},runAsync:async(q,...v)=>sql.prepare(q).run(...v),getFirstAsync:async(q,...v)=>sql.prepare(q).get(...v)??null,getAllAsync:async(q,...v)=>sql.prepare(q).all(...v)};await migrate(db);await writeSnapshot(db,session,1000,'fixture-session',key);return {sql,db};}
const response=(status,value,retryAfter)=>({status,json:async()=>value,retryAfter});
function ports(db,request,now=()=>1000){return{db:fn=>fn(db),request,now,active:()=>true};}
const pull=records=>({userId:session.userId,contextKey:key,records,hasMore:false});
test('lost response after server commit retries same operation without duplicate creation',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);let committed=false;let creates=0;let pushes=0;let time=1000;
  const request=async(path,c)=>{if(path==='/api/v1/mobile/session')return response(200,session);if(path.includes('/requests?'))return response(200,pull(committed?[record]:[]));pushes++;if(!committed){committed=true;creates++;throw Error('network lost after commit');}return response(200,{operationId:c.operationId,record,replayed:true});};
  let result=await synchronize(ports(db,request,()=>time),session,context);assert.equal(result.summary.pending,1);assert.equal((await jobs(db,session.userId,key))[0].status,'retry');
  time=4000;result=await synchronize(ports(db,request,()=>time),session,context);assert.equal(creates,1);assert.equal(pushes,2);assert.equal(result.summary.pending,0);assert.equal((await jobs(db,session.userId,key))[0].status,'sent');assert.equal(sql.prepare('SELECT count(*) AS n FROM sync_requests').get().n,1);
 }finally{sql.close();}
});
test('server409 remains conflicted and is not automatically retried; newer snapshot wins',async()=>{
 const {sql,db}=await fixture();try{
  const edit={...command,action:'UPDATE',expectedVersion:1};await enqueue(db,session,key,edit,1000);let pushes=0;const newer={...record,version:2,description:'Server edit'};
  const request=async(path)=>path==='/api/v1/mobile/session'?response(200,session):path.includes('/requests?')?response(200,pull([newer])):(pushes++,response(409,{}));
  await synchronize(ports(db,request),session,context);await synchronize(ports(db,request),session,context);assert.equal(pushes,1);assert.equal((await jobs(db,session.userId,key))[0].status,'conflict');await assert.rejects(retry(db,session,key,command.operationId,2000));await putRecord(db,session.userId,key,record);assert.equal(sql.prepare('SELECT version FROM sync_requests').get().version,2);
 }finally{sql.close();}
});
test('unsent cancel is local, foreign cancel blocked, attempted job cannot be cancelled',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);await assert.rejects(cancel(db,{...session,userId:'foreign'},key,command.operationId,1001));await cancel(db,session,key,command.operationId,1002);let pushes=0;
  await synchronize(ports(db,async path=>path==='/api/v1/mobile/session'?response(200,session):path.includes('/requests?')?response(200,pull([])):(pushes++,response(500,{}))),session,context);assert.equal(pushes,0);
  sql.prepare("UPDATE sync_outbox SET status='retry',attempts=1").run();await assert.rejects(cancel(db,session,key,command.operationId,1003));
 }finally{sql.close();}
});
test('429 uses bounded retry delay; retry budget exhausts without changing identity',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);
  const request=async path=>path==='/api/v1/mobile/session'?response(200,session):path.includes('/requests?')?response(200,pull([])):response(429,{},900000);
  await synchronize(ports(db,request),session,context);let job=(await jobs(db,session.userId,key))[0];assert.equal(job.next_attempt_at,121000);assert.equal(job.operation_id,command.operationId);
  sql.prepare("UPDATE sync_outbox SET attempts=5,next_attempt_at=0").run();await synchronize(ports(db,request),session,context);job=(await jobs(db,session.userId,key))[0];assert.equal(job.status,'failed');assert.equal(job.attempts,6);await retry(db,session,key,command.operationId,1000);assert.equal((await jobs(db,session.userId,key))[0].operation_id,command.operationId);
 }finally{sql.close();}
});
test('expired credentials do not push; revoked context is purged and blocked',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);await putRecord(db,session.userId,key,record);
  await assert.rejects(synchronize(ports(db,async()=>response(401,{})),session,context),e=>e instanceof SyncError && e.status===401);assert.equal((await jobs(db,session.userId,key))[0].attempts,0);
  await assert.rejects(synchronize(ports(db,async()=>response(200,{...session,contexts:[]})),session,context),e=>e instanceof SyncError && e.status===403);assert.equal(sql.prepare('SELECT count(*) AS n FROM sync_requests').get().n,0);const job=(await jobs(db,session.userId,key))[0];assert.equal(job.status,'blocked');assert.equal(job.payload,'');
 }finally{sql.close();}
});
test('foreign or truncated snapshot does not replace authorized cached records',async()=>{
 const {sql,db}=await fixture();try{
  await putRecord(db,session.userId,key,record);
  for(const value of [{...pull([]),userId:'foreign'},{...pull([]),hasMore:true}])await assert.rejects(synchronize(ports(db,async path=>response(200,path==='/api/v1/mobile/session'?session:value)),session,context));
  assert.equal(sql.prepare('SELECT count(*) AS n FROM sync_requests').get().n,1);
 }finally{sql.close();}
});
test('duplicate IDs cannot change payload and token fields are excluded',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,{...command,accessToken:'never-store'},1000);await enqueue(db,session,key,command,1001);assert.ok(!(await jobs(db,session.userId,key))[0].payload.includes('never-store'));await assert.rejects(enqueue(db,session,key,{...command,description:'Different'},1001));
 }finally{sql.close();}
});
test('restart recovers sending operation and server tombstone replaces local content',async()=>{
 const {sql,db}=await fixture();try{
  const del={...command,action:'DELETE',expectedVersion:1,description:null};await enqueue(db,session,key,del,1000);sql.prepare("UPDATE sync_outbox SET status='sending',attempts=1").run();await putRecord(db,session.userId,key,record);
  const tombstone={...record,version:2,deleted:true,description:null};
  await synchronize(ports(db,async(path,c)=>path==='/api/v1/mobile/session'?response(200,session):path.includes('/requests?')?response(200,pull([tombstone])):response(200,{operationId:c.operationId,record:tombstone,replayed:true})),session,context);
  assert.equal((await jobs(db,session.userId,key))[0].attempts,2);const row=sql.prepare('SELECT payload,deleted FROM sync_requests').get();assert.equal(row.deleted,1);assert.equal(JSON.parse(row.payload).description,null);
 }finally{sql.close();}
});
test('invalid success receipt is never marked sent or retried automatically',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);
  const request=async path=>path==='/api/v1/mobile/session'?response(200,session):response(200,{operationId:'foreign',record,replayed:false});
  await assert.rejects(synchronize(ports(db,request),session,context));
  assert.equal((await jobs(db,session.userId,key))[0].status,'failed');assert.equal(sql.prepare('SELECT count(*) AS n FROM sync_requests').get().n,0);
 }finally{sql.close();}
});
test('logout during response prevents old result from being cached or reported sent',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,key,command,1000);let active=true;
  const p=ports(db,async(path,c)=>{if(path==='/api/v1/mobile/session')return response(200,session);active=false;return response(200,{operationId:c.operationId,record,replayed:false});});p.active=()=>active;
  await assert.rejects(synchronize(p,session,context));
  assert.equal((await jobs(db,session.userId,key))[0].status,'sending');assert.equal(sql.prepare('SELECT count(*) AS n FROM sync_requests').get().n,0);
 }finally{sql.close();}
});
