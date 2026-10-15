import test from 'node:test';
import assert from 'node:assert/strict';
import {DatabaseSync} from 'node:sqlite';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {validateCommand,parseRecord} from '../src/sync/contracts.ts';
import {enqueue,jobs,cancel} from '../src/sync/outbox.ts';
import {synchronize} from '../src/sync/engine.ts';
import {unitRequest} from '../src/owner/request.ts';
import {parseSnapshot} from '../src/owner/contracts.ts';
import {parseDraft,parsePhotoReceipt,photoFileName,emptyDraft} from '../src/requests/contracts.ts';
import * as q from '../src/requests/outbox.ts';
import {synchronizePhotos,PhotoSyncError} from '../src/requests/engine.ts';
const user='request-fixture';const c={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:'22222222-2222-2222-2222-222222222222',unitNumber:'1',role:'TENANT'};const key=c.propertyId+':'+c.unitId+':TENANT';const session={userId:user,username:'Fixture',contexts:[c],hasMore:false};
const op='33333333-3333-3333-3333-333333333333',rec='44444444-4444-4444-4444-444444444444',p1='55555555-5555-5555-5555-555555555555',p2='66666666-6666-6666-6666-666666666666';
const f1=photoFileName('77777777-7777-7777-7777-777777777777'),f2=photoFileName('88888888-8888-8888-8888-888888888888');
const sha='a'.repeat(64);
async function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');const db={execAsync:async s=>sql.exec(s),runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null,getAllAsync:async(s,...v)=>sql.prepare(s).all(...v)};await migrate(db);await writeSnapshot(db,session,1000,'session',key);const map=new Map([[f1,'/9j/AAAA'],[f2,'/9j/BBBB']]);const files={write:async(k,v)=>{map.set(k,v);},read:async k=>{if(!map.has(k))throw Error('missing');return map.get(k);},drop:async k=>{map.delete(k);}};return {sql,db,files,map};}
const draft={description:' Mutfakta sızıntı ',category:'PLUMBING',priority:'HIGH',photos:[f1,f2]};
const command=()=>unitRequest(session,c,draft,op,rec);
const record={id:rec,propertyId:c.propertyId,unitId:c.unitId,role:'TENANT',description:'Mutfakta sızıntı',version:1,deleted:false,modifiedAt:'2026-10-15T00:00:00Z',category:'PLUMBING',priority:'HIGH',photos:[]};
const res=(status,value,retryAfter)=>({status,json:async()=>value,retryAfter});
function server(state){return async(path,body)=>{
 if(path==='/api/v1/mobile/session')return res(200,state.session??session);
 if(path.includes('/requests?'))return res(200,{userId:user,contextKey:key,records:state.created?[record]:[],hasMore:false});
 if(path==='/api/v1/mobile/sync/commands'){if(state.offline)throw Error('offline');state.created=true;return res(200,{operationId:body.operationId,record,replayed:false});}
 const m=/^\/api\/v1\/mobile\/sync\/requests\/([0-9a-f-]{36})\/photos$/.exec(path);assert.ok(m);assert.equal(m[1],rec);assert.ok(body.photoBase64.startsWith('/9j/'));state.uploads.push(body.operationId);
 if(state.photoStatus)return res(state.photoStatus,{});
 if(state.loseFirst&&!state.lost){state.lost=true;throw Error('response lost');}
 return res(200,{operationId:body.operationId,requestId:rec,photo:{id:'99999999-9999-9999-9999-99999999999'+state.uploads.length,sha256:sha,size:10},replayed:false});
};}
function ports(f,state,time=()=>1000){return {sync:{db:fn=>fn(f.db),request:server(state),now:time,active:()=>true},photo:{store:fn=>fn(f.db,f.files),request:server(state),now:time,active:()=>true}};}
test('category/priority validate, DELETE rejects metadata and legacy queued commands stay valid',()=>{
 const k=key,base={operationId:op,recordId:rec,propertyId:c.propertyId,unitId:c.unitId,role:'TENANT',action:'CREATE',expectedVersion:0,description:'x'};
 assert.equal(validateCommand(base,session,k).category,null);
 assert.equal(validateCommand({...base,category:'ELEVATOR',priority:'URGENT'},session,k).priority,'URGENT');
 assert.throws(()=>validateCommand({...base,category:'FIRE'},session,k));assert.throws(()=>validateCommand({...base,priority:'low'},session,k));
 assert.throws(()=>validateCommand({...base,action:'DELETE',expectedVersion:1,description:null,category:'OTHER'},session,k));
 assert.equal(command().description,'Mutfakta sızıntı');const owner={...c,role:'OWNER'};assert.equal(unitRequest({...session,contexts:[c,owner]},owner,draft,op,rec).role,'OWNER');
 assert.throws(()=>unitRequest(session,{...c,role:'MANAGER',unitId:null},draft,op,rec));assert.throws(()=>unitRequest(session,c,{...draft,description:' '},op,rec));
});
test('server records require valid metadata and photo refs; legacy records default',()=>{
 const legacy={...record};delete legacy.category;delete legacy.priority;delete legacy.photos;
 assert.deepEqual([parseRecord(legacy,c).category,parseRecord(legacy,c).priority,parseRecord(legacy,c).photos],['OTHER','NORMAL',[]]);
 assert.throws(()=>parseRecord({...record,category:null},c));assert.throws(()=>parseRecord({...record,priority:'SOON'},c));
 const photo={id:p1,sha256:sha,size:10};assert.equal(parseRecord({...record,photos:[photo]},c).photos.length,1);
 assert.throws(()=>parseRecord({...record,photos:[photo,photo,photo,photo]},c));assert.throws(()=>parseRecord({...record,photos:[{...photo,size:300000}]},c));
 assert.throws(()=>parseRecord({...record,deleted:true,description:null,category:'OTHER',priority:null,photos:[]},c));
 assert.equal(parsePhotoReceipt({operationId:p1,requestId:rec,photo,replayed:true},p1,rec).id,p1);
 assert.throws(()=>parsePhotoReceipt({operationId:p2,requestId:rec,photo,replayed:false},p1,rec));assert.throws(()=>parsePhotoReceipt({operationId:p1,requestId:p2,photo,replayed:false},p1,rec));
});
test('owner snapshot shows request metadata and rejects invalid values',()=>{
 const data={userId:user,contextKey:key,asOf:'2026-10-15T00:00:00Z',unit:{propertyName:'F',address:'A',city:'C',district:'D',buildingName:null,number:'1',floor:1,area:'90.00'},tenants:[],moreTenants:false,balance:'0.00',overdue:'0.00',charges:[],moreCharges:false,announcements:[],moreAnnouncements:false,requests:[{id:rec,description:'x',modifiedAt:'2026-10-15T00:00:00Z',category:'HEATING',priority:'LOW',photos:2}],moreRequests:false,payments:[],morePayments:false};
 assert.equal(parseSnapshot(data,user,c).requests[0].photos,2);
 assert.equal(parseSnapshot({...data,requests:[{id:rec,description:'x',modifiedAt:'2026-10-15T00:00:00Z'}]},user,c).requests[0].category,'OTHER');
 assert.throws(()=>parseSnapshot({...data,requests:[{...data.requests[0],photos:4}]},user,c));assert.throws(()=>parseSnapshot({...data,requests:[{...data.requests[0],category:'X'}]},user,c));
});
test('draft survives reopening the store and rejects foreign file names',async()=>{
 const f=await fixture();try{
  await q.saveDraft(f.db,user,key,{...draft,description:'yarım'},1000);
  const again={...f.db};assert.deepEqual(await q.readDraft(again,user,key),{...draft,description:'yarım'});
  assert.equal(await q.readDraft(f.db,user,'other'),null);
  assert.throws(()=>parseDraft({...draft,photos:['../avatar.enc']}));assert.throws(()=>parseDraft({...draft,photos:[f1,f1]}));assert.throws(()=>parseDraft({...draft,photos:[f1,f2,photoFileName(p1),photoFileName(p2)]}));
  assert.deepEqual(parseDraft(emptyDraft()).photos,[]);
  await q.discardDraft(f.db,f.files,user,key);assert.equal(await q.readDraft(f.db,user,key),null);assert.equal(f.map.size,0);
 }finally{f.sql.close();}
});
test('submit is atomic: duplicate operation keeps draft and creates no photo job',async()=>{
 const f=await fixture();try{
  await q.saveDraft(f.db,user,key,draft,1000);await enqueue(f.db,session,key,{...command(),description:'different'},900);
  await assert.rejects(q.submit(f.db,session,key,command(),draft,[p1,p2],1000));
  assert.equal((await q.photoJobs(f.db,user,key)).length,0);assert.ok(await q.readDraft(f.db,user,key));
  await assert.rejects(q.submit(f.db,session,key,{...command(),operationId:p2},draft,[p1,p2],1000));
 }finally{f.sql.close();}
});
test('offline: photos wait for the request; online request is sent first then photos upload once despite lost response',async()=>{
 const f=await fixture();try{
  await q.saveDraft(f.db,user,key,draft,1000);await q.submit(f.db,session,key,command(),draft,[p1,p2],1000);
  assert.equal(await q.readDraft(f.db,user,key),null);assert.equal(f.map.size,2);
  let time=1000;const state={offline:true,uploads:[],loseFirst:true};const p=ports(f,state,()=>time);
  await synchronize(p.sync,session,c);await synchronizePhotos(p.photo,session,c);
  assert.equal((await jobs(f.db,user,key))[0].status,'retry');assert.equal(state.uploads.length,0);assert.ok((await q.photoJobs(f.db,user,key)).every(j=>j.status==='pending'));
  state.offline=false;time=5000;await synchronize(p.sync,session,c);assert.equal((await jobs(f.db,user,key))[0].status,'sent');
  await synchronizePhotos(p.photo,session,c);
  let photos=await q.photoJobs(f.db,user,key);assert.equal(photos.find(j=>j.operation_id===p1).status,'retry');assert.equal(photos.find(j=>j.operation_id===p2).status,'pending');
  time=10000;await synchronizePhotos(p.photo,session,c);photos=await q.photoJobs(f.db,user,key);
  assert.ok(photos.every(j=>j.status==='sent'));assert.deepEqual(state.uploads,[p1,p1,p2]);assert.equal(f.map.size,0);
 }finally{f.sql.close();}
});
test('cancelled request releases its photos; revoked context blocks and wipes them',async()=>{
 const f=await fixture();try{
  await q.submit(f.db,session,key,command(),draft,[p1,p2],1000);await cancel(f.db,session,key,op,1001);
  const state={uploads:[]};await synchronizePhotos(ports(f,state).photo,session,c);
  assert.ok((await q.photoJobs(f.db,user,key)).every(j=>j.status==='cancelled'));assert.equal(f.map.size,0);assert.equal(state.uploads.length,0);
 }finally{f.sql.close();}
 const g=await fixture();try{
  await q.submit(g.db,session,key,command(),draft,[p1,p2],1000);await q.saveDraft(g.db,user,key,{...draft,photos:[]},1000);
  await synchronizePhotos(ports(g,{uploads:[]}).photo,{...session,contexts:[]},c);
  assert.ok((await q.photoJobs(g.db,user,key)).every(j=>j.status==='blocked'));assert.equal(g.map.size,0);assert.equal(await q.readDraft(g.db,user,key),null);
 }finally{g.sql.close();}
});
test('400/409 stop retries, 429 backs off, 401 surfaces; discard drops the local copy',async()=>{
 const f=await fixture();try{
  await q.submit(f.db,session,key,command(),draft,[p1,p2],1000);f.sql.prepare("UPDATE sync_outbox SET status='sent'").run();
  const state={uploads:[],photoStatus:400};await synchronizePhotos(ports(f,state).photo,session,c);
  let photos=await q.photoJobs(f.db,user,key);assert.ok(photos.every(j=>j.status==='failed'&&j.error_code===400));assert.ok(photos.every(j=>!q.canRetry(j)));
  await assert.rejects(q.retry(f.db,user,key,p1,2000));await q.discard(f.db,f.files,user,key,p1);assert.equal(f.map.has(f1),false);
  f.sql.prepare("UPDATE request_photo_jobs SET status='pending',attempts=0,error_code=NULL WHERE operation_id=?").run(p2);state.photoStatus=429;
  await synchronizePhotos(ports(f,state).photo,session,c);let job=(await q.photoJobs(f.db,user,key)).find(j=>j.operation_id===p2);assert.equal(job.status,'retry');assert.ok(job.next_attempt_at>=2000);
  f.sql.prepare("UPDATE request_photo_jobs SET next_attempt_at=0 WHERE operation_id=?").run(p2);state.photoStatus=409;await synchronizePhotos(ports(f,state).photo,session,c);
  job=(await q.photoJobs(f.db,user,key)).find(j=>j.operation_id===p2);assert.equal(job.status,'conflict');assert.ok(q.canDiscard(job));
  f.sql.prepare("UPDATE request_photo_jobs SET status='retry',next_attempt_at=0 WHERE operation_id=?").run(p2);state.photoStatus=401;
  await assert.rejects(synchronizePhotos(ports(f,state).photo,session,c),e=>e instanceof PhotoSyncError&&e.status===401);
  f.map.delete(f2);f.sql.prepare("UPDATE request_photo_jobs SET status='retry',next_attempt_at=0 WHERE operation_id=?").run(p2);state.photoStatus=undefined;
  await synchronizePhotos(ports(f,state).photo,session,c);job=(await q.photoJobs(f.db,user,key)).find(j=>j.operation_id===p2);assert.equal(job.status,'failed');assert.equal(job.error_code,-2);
 }finally{f.sql.close();}
});
