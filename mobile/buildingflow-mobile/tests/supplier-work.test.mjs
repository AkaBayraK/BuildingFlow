import test from 'node:test';
import assert from 'node:assert/strict';
import {DatabaseSync} from 'node:sqlite';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {contextKey} from '../src/auth/contracts.ts';
import {permitted,nextStatuses,listPath,authorizePath,parseList,validateCommand,commandPath,commandBody} from '../src/supplier-work/contracts.ts';
import {enqueue,visibleJobs,discard,retry,purgeUnauthorized,flush,saveCopy,readCopy,FlushStopped} from '../src/supplier-work/queue.ts';
const tenant='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',property='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',supplier='cccccccc-cccc-4ccc-8ccc-cccccccccccc',order='dddddddd-dddd-4ddd-8ddd-dddddddddddd',other='eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
const canonical=(role,id='11111111-1111-4111-8111-111111111111')=>({id,kind:'MANAGEMENT',tenantId:tenant,tenantName:'T',propertyId:property,propertyName:'Site',unitId:null,unitNumber:null,role,startDate:null,endDate:null,canManagePeople:true,canManageFinance:false,canManageAssignments:false});
const ctx=(role='MANAGER')=>({propertyId:property,propertyName:'Site',unitId:null,unitNumber:null,role,canonical:canonical(role)});
const c=ctx(),session={userId:'manager-a',username:null,hasMore:false,contexts:[c]};
const row=(patch={})=>({record:{id:order,tenantId:tenant,supplierId:supplier,propertyId:property,serviceName:'Elevator',title:'Repair',description:'Scope',status:'DRAFT',dueAt:null,isOverdue:false,photoCount:0,version:3,modifiedAt:'2026-10-30T08:00:00Z',...patch},supplierName:'Lift Co'});
const page=(items=[row()])=>({items,page:1,pageSize:20,totalCount:items.length});
const current=parseList(page(),session.userId,c,1,'2026-10-30T08:00:00Z').items[0];
const cmd=(patch={})=>({operationId:'99999999-9999-4999-8999-999999999999',kind:'NOTE',supplierId:supplier,workOrderId:order,title:'Repair',expectedVersion:3,status:null,text:' Checked on site ',photoFile:null,...patch});
async function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');const db={execAsync:async q=>{sql.exec(q);},runAsync:async(q,...v)=>sql.prepare(q).run(...v),getFirstAsync:async(q,...v)=>sql.prepare(q).get(...v)??null,getAllAsync:async(q,...v)=>sql.prepare(q).all(...v)};await migrate(db);await writeSnapshot(db,{userId:session.userId,username:null,hasMore:false,contexts:[]},1000,'snap',null);return {sql,db};}
const files=()=>{const store=new Map([['supplier-photo-'+other+'.enc','/9j/photo']]);return {store,read:async n=>{if(!store.has(n))throw Error('missing');return store.get(n);},drop:async n=>{store.delete(n);}};};
const ports=(db,f,send,now=()=>1000,active=()=>true)=>({db:fn=>fn(db),files:f,send,now,active});
test('only canonical management contexts are permitted and finalization stays with site admins',()=>{
 assert.ok(permitted(c));assert.ok(!permitted({...c,canonical:undefined}));assert.ok(!permitted({...ctx('ACCOUNTANT')}));assert.ok(!permitted({...c,unitId:order}));assert.ok(!permitted({...c,canonical:{...c.canonical,kind:'OWNER'}}));
 assert.deepEqual(nextStatuses(c,'COMPLETED'),['CANCELLED']);assert.deepEqual(nextStatuses(ctx('SITE_ADMIN'),'COMPLETED'),['VERIFIED','CANCELLED']);assert.deepEqual(nextStatuses(c,'CLOSED'),[]);
});
test('request paths are whitelisted to the selected tenant, property and allowed methods',()=>{
 assert.equal(listPath(c,1),`/api/v2/tenants/${tenant}/supplier-work-orders?propertyId=${property}&page=1&pageSize=20`);authorizePath(c,listPath(c,2),'GET');
 const root=`/api/v2/tenants/${tenant}/suppliers/${supplier}/work-orders/${order}`;authorizePath(c,root+'/transition','PUT');authorizePath(c,root+'/notes','POST');authorizePath(c,root+'/photos','POST');
 for(const [url,method] of [[root+'/transition','POST'],[root+'/notes','PUT'],[root.replace(tenant,other)+'/notes','POST'],[root+'/history','GET'],[listPath(c,1).replace(property,other),'GET'],[listPath(c,1)+'&status=DRAFT','GET'],[`/api/v2/tenants/${tenant}/suppliers/${supplier}/invoices`,'POST']])assert.throws(()=>authorizePath(c,url,method));
 assert.equal(commandPath(c,cmd()),root+'/notes');assert.deepEqual(commandBody(validateCommand(cmd(),c)),{operationId:'99999999-9999-4999-8999-999999999999',version:3,note:'Checked on site'});
});
test('list DTO rejects foreign tenant or property, overdue terminal rows and oversized pages',()=>{
 assert.equal(current.supplierName,'Lift Co');
 for(const r of [row({tenantId:other}),row({propertyId:other}),row({status:'PAID'}),row({status:'CLOSED',isOverdue:true}),row({photoCount:11}),row({version:0}),{...row(),supplierName:''}])assert.throws(()=>parseList(page([r]),session.userId,c,1,'2026-10-30T08:00:00Z'));
 assert.throws(()=>parseList({...page(),pageSize:50},session.userId,c,1,'2026-10-30T08:00:00Z'));assert.throws(()=>parseList(page([row(),row()]),session.userId,c,1,'2026-10-30T08:00:00Z'));
});
test('commands validate against the cached version, allowed transitions and evidence limits',()=>{
 assert.equal(validateCommand(cmd(),c,current).text,'Checked on site');
 for(const patch of [{expectedVersion:2},{kind:'TRANSITION',status:'VERIFIED',text:'Skip'},{kind:'TRANSITION',status:null},{kind:'PHOTO',photoFile:null},{kind:'PHOTO',photoFile:'../escape.enc'},{text:' '},{supplierId:other}])assert.throws(()=>validateCommand(cmd(patch),c,current));
 assert.equal(validateCommand(cmd({kind:'TRANSITION',status:'ASSIGNED',text:'Assign'}),c,current).status,'ASSIGNED');
 const closed=parseList(page([row({status:'CLOSED'})]),session.userId,c,1,'2026-10-30T08:00:00Z').items[0];assert.throws(()=>validateCommand(cmd(),c,closed));
});
test('offline queue keeps one active job per work order, then sends it once when reconnected',async()=>{
 const {sql,db}=await fixture();try{
  const f=files();await enqueue(db,session,c,cmd(),current,1000);await enqueue(db,session,c,cmd(),current,1000);
  await assert.rejects(enqueue(db,session,c,cmd({operationId:other}),current,1000),/bekleyen/);
  let online=false,calls=[];const send=async(path,method,body,photo)=>{calls.push({path,method,body,photo});if(!online)throw Error('offline');return {status:200};};
  assert.equal(await flush(ports(db,files(),send,()=>1000),session,c),0);let jobs=await visibleJobs(db,session.userId,c);assert.equal(jobs[0].status,'retry');assert.equal(jobs[0].attempts,1);
  assert.equal(await flush(ports(db,files(),send,()=>1500),session,c),0);assert.equal(calls.length,1);
  online=true;assert.equal(await flush(ports(db,files(),send,()=>5000),session,c),1);
  assert.equal(calls.length,2);assert.equal(calls[1].body.operationId,calls[0].body.operationId);assert.equal(calls[1].method,'POST');
  assert.equal((await visibleJobs(db,session.userId,c)).length,0);
  await enqueue(db,session,c,cmd({operationId:other,kind:'TRANSITION',status:'ASSIGNED',text:'Assign'}),current,6000);
 }finally{sql.close();}
});
test('an interrupted send is resent with the same operation id so the server replays instead of duplicating',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,c,cmd(),current,1000);sql.exec("UPDATE supplier_work_jobs SET status='sending',attempts=1");
  const ids=[];const sent=await flush(ports(db,files(),async(_p,_m,body)=>{ids.push(body.operationId);return {status:200};}),session,c);
  assert.equal(sent,1);assert.deepEqual(ids,['99999999-9999-4999-8999-999999999999']);
 }finally{sql.close();}
});
test('server conflict stays visible until the manager discards; no automatic overwrite',async()=>{
 const {sql,db}=await fixture();try{
  const f=files();const photo='supplier-photo-'+other+'.enc';await enqueue(db,session,c,cmd({kind:'PHOTO',photoFile:photo,text:'Before'}),current,1000);
  let calls=0;const send=async(_p,_m,_b,data)=>{calls++;assert.equal(data,'/9j/photo');return {status:409};};
  await flush(ports(db,f,send),session,c);await flush(ports(db,f,send,()=>99999),session,c);assert.equal(calls,1);
  const [job]=await visibleJobs(db,session.userId,c);assert.equal(job.status,'conflict');await assert.rejects(retry(db,session.userId,c,job.operation_id,2000));
  await discard(db,f,session.userId,c,job.operation_id,2000);assert.equal(f.store.has(photo),false);assert.equal((await visibleJobs(db,session.userId,c)).length,0);
  assert.equal(sql.prepare('SELECT payload FROM supplier_work_jobs').get().payload,'');
 }finally{sql.close();}
});
test('401 stops the flush and keeps the job; 403 blocks; 5xx backs off then fails and can be retried manually',async()=>{
 const {sql,db}=await fixture();try{
  await enqueue(db,session,c,cmd(),current,1000);
  await assert.rejects(flush(ports(db,files(),async()=>({status:401})),session,c),e=>e instanceof FlushStopped&&e.status===401);assert.equal((await visibleJobs(db,session.userId,c))[0].status,'retry');
  let t=1000;for(let i=0;i<8;i++){t+=200000;await flush(ports(db,files(),async()=>({status:503}),()=>t),session,c);}
  let [job]=await visibleJobs(db,session.userId,c);assert.equal(job.status,'failed');assert.equal(job.error_code,503);
  await retry(db,session.userId,c,job.operation_id,t);await flush(ports(db,files(),async()=>({status:403}),()=>t),session,c);
  [job]=await visibleJobs(db,session.userId,c);assert.equal(job.status,'blocked');
 }finally{sql.close();}
});
test('losing the context purges its jobs, photo files and copies; another account sees nothing',async()=>{
 const {sql,db}=await fixture();try{
  const f=files();const photo='supplier-photo-'+other+'.enc';await enqueue(db,session,c,cmd({kind:'PHOTO',photoFile:photo,text:'Evidence'}),current,1000);
  const copy=parseList(page(),session.userId,c,1,'2026-10-30T08:00:00Z');await saveCopy(db,session.userId,c,copy,1000);
  assert.equal((await readCopy(db,session.userId,c,1,1000+59*60000)).items.length,1);assert.equal(await readCopy(db,session.userId,c,1,1000+61*60000),null);assert.equal(await readCopy(db,'manager-b',c,1,1001),null);
  assert.equal((await visibleJobs(db,'manager-b',c)).length,0);
  await purgeUnauthorized(db,f,{...session,contexts:[]});
  assert.equal((await visibleJobs(db,session.userId,c)).length,0);assert.equal(f.store.has(photo),false);assert.equal(await readCopy(db,session.userId,c,1,1001),null);
  let calls=0;await assert.rejects(flush(ports(db,f,async()=>{calls++;return {status:200};}),{...session,contexts:[]},c),e=>e.status===403);assert.equal(calls,0);
 }finally{sql.close();}
});
test('a cancelled screen stops the flush before anything is sent',async()=>{
 const {sql,db}=await fixture();try{await enqueue(db,session,c,cmd(),current,1000);let calls=0;await assert.rejects(flush(ports(db,files(),async()=>{calls++;return {status:200};},()=>1000,()=>false),session,c));assert.equal(calls,0);assert.equal(contextKey(c),`${tenant}:MANAGEMENT:11111111-1111-4111-8111-111111111111`);}finally{sql.close();}
});
