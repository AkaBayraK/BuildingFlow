import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {DatabaseSync} from 'node:sqlite';
import {parseMarkup,safeHref,normalizeMarkup,hiddenCodePoint} from '../src/consents/markup.ts';
import {parseList,parseCommand,parseResult,wire,CONSENT_HOLD} from '../src/consents/contracts.ts';
import * as store from '../src/consents/store.ts';
import {flushConsents,refreshConsents,ConsentError} from '../src/consents/engine.ts';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {enqueue as enqueueSync,jobs as syncJobs,cancel as cancelSync} from '../src/sync/outbox.ts';
import {synchronize} from '../src/sync/engine.ts';
import * as profileQueue from '../src/profile/outbox.ts';
import {synchronizeProfile} from '../src/profile/engine.ts';

const vectors=JSON.parse(readFileSync(new URL('../../../docs/api/consent-markup-vectors.json',import.meta.url),'utf8'));
const user='consent-fixture';
const sha=text=>createHash('sha256').update(text,'utf8').digest('hex');
const ids={info:'6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4b01',profile:'6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4b02',request:'6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4b03',profile2:'77777777-7777-4777-8777-777777777777'};
const op=n=>`00000000-0000-4000-8000-${String(n).padStart(12,'0')}`;
function text(id,version,body){return {id,textVersion:version,title:'Metin '+version,body,bodySha256:sha(body),requiresReconsent:true,publishedAt:'2026-10-16T10:00:00Z'};}
function status(code,kind,feature,sortOrder,current,state='NONE',approved=null){return {typeCode:code,nameTr:code,nameEn:code,kind,feature,sortOrder,state,approvedVersion:approved,approvedAt:approved?'2026-10-16T11:00:00Z':null,currentTextId:current?.id??null,currentVersion:current?.textVersion??null};}
const infoText=text(ids.info,1,'# Aydınlatma\n\nVeri sorumlusu **yönetim**.'),profileText=text(ids.profile,1,'# Profil fotoğrafı\n\n- Amaç\n- Süre'),requestText=text(ids.request,1,'Talep fotoğrafı [ayrıntı](https://example.com/kvkk).');
function list(over={}){return {userId:user,items:[
 {status:status('KVKK_INFORMATION','INFORMATION',null,10,infoText,over.info??'NONE',over.info==='ACTIVE'?1:null),currentText:infoText},
 {status:status('PROFILE_PHOTO','EXPLICIT_CONSENT','PROFILE_PHOTO',20,over.profileText??profileText,over.profile??'NONE',over.profile==='ACTIVE'||over.profile==='OUTDATED'?1:null),currentText:over.profileText??profileText},
 {status:status('REQUEST_PHOTO','EXPLICIT_CONSENT','REQUEST_PHOTO',30,requestText,over.request??'NONE',over.request==='ACTIVE'?1:null),currentText:requestText}]};}
async function fixture(){
 const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');
 const db={execAsync:async s=>sql.exec(s),runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null,getAllAsync:async(s,...v)=>sql.prepare(s).all(...v)};
 await migrate(db);await writeSnapshot(db,{userId:user,username:'Fixture',contexts:[context],hasMore:false},1000,'session',null);
 const map=new Map();const files={write:async(k,v)=>{map.set(k,v);},read:async k=>{if(!map.has(k))throw Error('missing');return map.get(k);},drop:async k=>{map.delete(k);}};
 return {sql,db,files,map};
}
const reply=(status,value,retryAfter)=>({status,json:async()=>value,retryAfter});
const ports=(f,request,now=()=>1000,active=()=>true)=>({store:fn=>fn(f.db,f.files),request,now,active,digest:async t=>sha(t)});
const approve=(n,code,t)=>({operationId:op(n),action:'APPROVE',typeCode:code,textId:t.id,bodySha256:t.bodySha256,appVersion:'0.1.0'});
const withdraw=(n,code)=>({operationId:op(n),action:'WITHDRAW',typeCode:code,textId:null,bodySha256:null,appVersion:'0.1.0'});
const receipt=(c,state,current)=>({operationId:c.operationId,replayed:false,status:status(c.typeCode,c.typeCode==='KVKK_INFORMATION'?'INFORMATION':'EXPLICIT_CONSENT',c.typeCode==='KVKK_INFORMATION'?null:c.typeCode,10,current,state,state==='ACTIVE'?current.textVersion:null)});
const context={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:'22222222-2222-2222-2222-222222222222',unitNumber:'1',role:'OWNER'};
const key=context.propertyId+':'+context.unitId+':OWNER';

test('mobile consent parser matches every shared API vector and the web link rules',()=>{
 assert.ok(vectors.length>=16);
 for(const v of vectors){const r=parseMarkup(v.input);assert.equal(r.error,v.error,v.name);assert.deepEqual(r.blocks,v.blocks,v.name);}
 assert.equal(parseMarkup(null).error,'EMPTY');assert.equal(parseMarkup('a'.repeat(50_001)).error,'TOO_LONG');assert.equal(parseMarkup('a\n'.repeat(2_000)+'a').error,'TOO_MANY_LINES');
 for(const hidden of ['a\u0000b','a‮b','﻿a','a\ud800b','a\udc00b','a\u0007b','a\u{e0041}b'])assert.equal(parseMarkup(hidden).error,'CONTROL_CHAR',JSON.stringify(hidden));
 assert.equal(parseMarkup('emoji 😀 ve ZWJ a‍b').error,null);
 for(const href of ['javascript:alert(1)','http://example.com','https://user:pw@example.com','https://','data:text/html,x','mailto:not-an-email','HTTPS://example.com','https://'+'a'.repeat(2000),'https://a..b','https://host:99999','https://ex ample.com'])assert.equal(safeHref(href),false,href);
 for(const href of ['https://example.com/kvkk?x=1#a','mailto:kvkk@example.com','https://example.com:8443/a','https://kvkk.örnek.com.tr'])assert.equal(safeHref(href),true,href);
 assert.equal(normalizeMarkup('a\r\nb\rc\td'),'a\nb\nc d');
});
test('hidden character table equals Unicode Cc/Cf for every code point (Hermes has no \\p{} dependency)',()=>{
 for(let c=0;c<=0x10ffff;c++){if(c>=0xd800&&c<=0xdfff)continue;const s=String.fromCodePoint(c);const expected=c!==10&&(/\p{Cc}/u.test(s)||c!==0x200c&&c!==0x200d&&/\p{Cf}/u.test(s));if(expected!==hiddenCodePoint(c))assert.fail('U+'+c.toString(16));}
});
test('consent contracts refuse foreign, inconsistent or unsafe server data',()=>{
 assert.equal(parseList(list(),user).items.length,3);
 assert.throws(()=>parseList(list(),'other'));
 const bad=list();bad.items[1].currentText={...profileText,body:'a‮b'};assert.throws(()=>parseList(bad,user));
 const mismatch=list();mismatch.items[1].status.currentVersion=2;assert.throws(()=>parseList(mismatch,user));
 const duplicate=list();duplicate.items.push(duplicate.items[1]);assert.throws(()=>parseList(duplicate,user));
 const withdrawnInfo=list();withdrawnInfo.items[0].status.state='WITHDRAWN';assert.throws(()=>parseList(withdrawnInfo,user));
 const activeWithout=list();activeWithout.items[1].status.state='ACTIVE';assert.throws(()=>parseList(activeWithout,user));
 assert.equal(parseList({...list(),token:'secret'},user).token,undefined);
 const c=approve(1,'PROFILE_PHOTO',profileText);
 assert.deepEqual(wire(parseCommand(withdraw(2,'PROFILE_PHOTO'))),{operationId:op(2),action:'WITHDRAW',typeCode:'PROFILE_PHOTO',appVersion:'0.1.0'});
 assert.throws(()=>parseCommand({...c,bodySha256:'A'.repeat(64)}));assert.throws(()=>parseCommand({...withdraw(2,'PROFILE_PHOTO'),textId:ids.profile}));assert.throws(()=>parseCommand({...c,operationId:'00000000-0000-0000-0000-000000000000'}));
 assert.equal(parseResult(receipt(c,'ACTIVE',profileText),c).status.state,'ACTIVE');
 assert.throws(()=>parseResult({...receipt(c,'ACTIVE',profileText),operationId:op(9)},c));
 assert.throws(()=>parseResult(receipt(c,'NONE',profileText),c));
 assert.throws(()=>parseResult(receipt(withdraw(3,'PROFILE_PHOTO'),'ACTIVE',profileText),withdraw(3,'PROFILE_PHOTO')));
});
test('schema 8 keeps consent data per account; foreign account and reused operation ids are refused',async()=>{
 const f=await fixture();try{
  await store.cacheList(f.db,parseList(list(),user),1000);assert.equal((await store.cachedList(f.db,user)).list.items.length,3);
  await assert.rejects(store.cacheList(f.db,{...parseList(list(),user),userId:'other'},1000));
  await store.enqueue(f.db,user,approve(1,'PROFILE_PHOTO',profileText),1000);await store.enqueue(f.db,user,approve(1,'PROFILE_PHOTO',profileText),1000);
  await assert.rejects(store.enqueue(f.db,user,approve(1,'REQUEST_PHOTO',requestText),1000));await assert.rejects(store.enqueue(f.db,'other',approve(5,'PROFILE_PHOTO',profileText),1000));
  assert.equal((await store.jobs(f.db,user)).length,1);
  // An unsent decision for the same type is replaced, so approve-then-withdraw offline leaves only the withdrawal.
  await store.enqueue(f.db,user,withdraw(2,'PROFILE_PHOTO'),1001);assert.deepEqual((await store.jobs(f.db,user)).map(j=>j.action),['WITHDRAW']);
 }finally{f.sql.close();}
});
test('effective state, photo permission and launch gate follow queued decisions and seen texts',async()=>{
 const l=parseList(list({profile:'OUTDATED',profileText:{...profileText,textVersion:2,id:ids.profile2}}),user);
 let items=store.effective(l,[]);
 assert.equal(store.allows(store.forFeature(items,'PROFILE_PHOTO')),false);assert.equal(store.allows(store.forFeature(items,'REQUEST_PHOTO')),false);
 assert.deepEqual(store.attention(items,new Set()).map(e=>e.item.status.typeCode),['KVKK_INFORMATION','PROFILE_PHOTO']);
 assert.deepEqual(store.attention(items,new Set([ids.info,ids.profile2])),[]);
 const job={operation_id:op(1),user_id:user,type_code:'REQUEST_PHOTO',action:'APPROVE',payload:JSON.stringify(parseCommand(approve(1,'REQUEST_PHOTO',requestText))),status:'pending',attempts:0,next_attempt_at:0,created_at:0,error:null};
 items=store.effective(l,[job]);const request=store.forFeature(items,'REQUEST_PHOTO');
 assert.equal(request.state,'ACTIVE');assert.equal(request.pending,'APPROVE');assert.equal(store.allows(request),true);
 const stale={...job,payload:JSON.stringify(parseCommand({...approve(1,'REQUEST_PHOTO',requestText),textId:ids.profile2}))};
 assert.equal(store.allows(store.forFeature(store.effective(l,[stale]),'REQUEST_PHOTO')),false);
 const conflict={...job,status:'conflict',error:'BF_CONSENT_TEXT_OUTDATED'};
 assert.ok(store.attention(store.effective(l,[conflict]),new Set([ids.info,ids.profile2,ids.request])).some(e=>e.item.status.typeCode==='REQUEST_PHOTO'));
});
test('refresh verifies body SHA-256 before caching; a tampered text is never stored',async()=>{
 const f=await fixture();try{
  const tampered=list();tampered.items[1].currentText={...profileText,body:profileText.body+' değişti'};
  await assert.rejects(refreshConsents(ports(f,async()=>reply(200,tampered)),user),e=>e instanceof ConsentError&&e.status===-1);
  assert.equal(await store.cachedList(f.db,user),null);
  await refreshConsents(ports(f,async()=>reply(200,list())),user);assert.equal((await store.cachedList(f.db,user)).list.items.length,3);
  await assert.rejects(refreshConsents(ports(f,async()=>reply(200,{...list(),userId:'other'})),user));
 }finally{f.sql.close();}
});
test('offline decisions go out oldest first, survive response loss with the same UUID and update the cache',async()=>{
 const f=await fixture();try{
  await store.cacheList(f.db,parseList(list(),user),1000);
  await store.enqueue(f.db,user,approve(1,'KVKK_INFORMATION',infoText),1000);await store.enqueue(f.db,user,approve(2,'PROFILE_PHOTO',profileText),1001);
  const sent=[];let lose=true,time=2000;
  const request=async(path,body)=>{if(!body)return reply(200,list());sent.push(body.operationId);if(lose&&body.typeCode==='PROFILE_PHOTO'){lose=false;throw Error('lost after commit');}
   return reply(200,receipt(body,'ACTIVE',body.typeCode==='PROFILE_PHOTO'?profileText:infoText));};
  let result=await flushConsents(ports(f,request,()=>time),user);
  assert.deepEqual(sent,[op(1),op(2)]);assert.equal(result.sent,1);assert.equal((await store.jobs(f.db,user))[0].status,'retry');
  time=2500;result=await flushConsents(ports(f,request,()=>time),user);assert.equal(result.sent,0,'backoff holds the retry');
  time=100000;result=await flushConsents(ports(f,request,()=>time),user);
  assert.deepEqual(sent,[op(1),op(2),op(2)]);assert.deepEqual(result.released,['PROFILE_PHOTO']);assert.equal((await store.jobs(f.db,user)).length,0);
  const cached=store.effective((await store.cachedList(f.db,user)).list,[]);assert.equal(store.forFeature(cached,'PROFILE_PHOTO').state,'ACTIVE');
  assert.equal(cached.find(e=>e.item.status.typeCode==='KVKK_INFORMATION').state,'ACTIVE');
 }finally{f.sql.close();}
});
test('only one decision is in flight; a crashed send is recovered and resent',async()=>{
 const f=await fixture();try{
  await store.enqueue(f.db,user,approve(1,'PROFILE_PHOTO',profileText),1000);await store.enqueue(f.db,user,approve(2,'REQUEST_PHOTO',requestText),1001);
  assert.equal((await store.claim(f.db,user,1000)).operation_id,op(1));assert.equal(await store.claim(f.db,user,1000),null);
  await store.recover(f.db,user,2000);assert.equal((await store.claim(f.db,user,2000)).operation_id,op(1));
 }finally{f.sql.close();}
});
test('outdated text becomes a visible conflict, reloads texts and is asked again; other refusals stop without retry',async()=>{
 const f=await fixture();try{
  await store.cacheList(f.db,parseList(list(),user),1000);
  await store.enqueue(f.db,user,approve(1,'PROFILE_PHOTO',profileText),1000);await store.enqueue(f.db,user,withdraw(2,'KVKK_INFORMATION'),1001);
  const newer={...profileText,id:ids.profile2,textVersion:2,body:profileText.body+'\n\nYeni madde.'};newer.bodySha256=sha(newer.body);
  let pushes=0;const request=async(path,body)=>{if(!body)return reply(200,list({profileText:newer}));pushes++;return reply(409,{code:body.action==='APPROVE'?'BF_CONSENT_TEXT_OUTDATED':'BF_CONSENT_NOT_WITHDRAWABLE'});};
  const result=await flushConsents(ports(f,request,()=>2000),user);
  assert.equal(result.outdated,true);assert.equal(result.refreshed,true);assert.equal(pushes,2);
  const queue=await store.jobs(f.db,user);assert.deepEqual(queue.map(j=>[j.status,j.error]),[['conflict','BF_CONSENT_TEXT_OUTDATED'],['failed','BF_CONSENT_NOT_WITHDRAWABLE']]);
  assert.equal(store.canRetry(queue[1]),false);assert.equal(store.canDiscard(queue[0]),true);
  await flushConsents(ports(f,request,()=>3000),user);assert.equal(pushes,2,'refused decisions are not resent');
  const items=store.effective((await store.cachedList(f.db,user)).list,queue);
  const profile=store.forFeature(items,'PROFILE_PHOTO');assert.equal(profile.item.currentText.textVersion,2);assert.equal(store.allows(profile),false);
  assert.ok(store.attention(items,new Set([ids.profile2,ids.info])).includes(profile));
  // Approving the new text replaces the conflict.
  await store.enqueue(f.db,user,approve(3,'PROFILE_PHOTO',newer),2000);assert.equal((await store.jobs(f.db,user)).filter(j=>j.type_code==='PROFILE_PHOTO').length,1);
 }finally{f.sql.close();}
});
test('401 keeps the decision queued; server errors back off and stop after the retry budget',async()=>{
 const f=await fixture();try{
  await store.enqueue(f.db,user,approve(1,'PROFILE_PHOTO',profileText),1000);
  await assert.rejects(flushConsents(ports(f,async()=>reply(401,{})),user),e=>e.status===401);assert.equal((await store.jobs(f.db,user))[0].status,'retry');
  await flushConsents(ports(f,async()=>reply(503,{},900000)),user);let job=(await store.jobs(f.db,user))[0];assert.equal(job.status,'retry');assert.equal(job.next_attempt_at,121000);
  f.sql.exec('UPDATE consent_jobs SET attempts=6,next_attempt_at=0');await flushConsents(ports(f,async()=>reply(500,{})),user);job=(await store.jobs(f.db,user))[0];
  assert.equal(job.status,'failed');assert.equal(store.canRetry(job),true);await store.retry(f.db,user,job.operation_id,5000);assert.equal((await store.jobs(f.db,user))[0].status,'retry');
  let active=true;await assert.rejects(flushConsents(ports(f,async()=>{active=false;return reply(200,{});},()=>6000,()=>active),user));assert.equal((await store.jobs(f.db,user))[0].status,'retry');
 }finally{f.sql.close();}
});
test('request sync holds a photo refused for missing consent; approving releases it, plain 403 stays blocked',async()=>{
 const f=await fixture();try{
  const session={userId:user,username:'Fixture',contexts:[context],hasMore:false};
  const photo={operationId:op(11),recordId:'33333333-3333-4333-8333-333333333333',propertyId:context.propertyId,unitId:context.unitId,role:'OWNER',action:'CREATE',expectedVersion:0,description:'Musluk',photoAction:'REPLACE'};
  const plain={...photo,operationId:op(12),recordId:'44444444-4444-4444-8444-444444444444',photoAction:'KEEP'};
  await enqueueSync(f.db,session,key,photo,1000);await enqueueSync(f.db,session,key,plain,1001);
  const request=async(path,c)=>path==='/api/v1/mobile/session'?reply(200,session):path.includes('/requests?')?reply(200,{userId:user,contextKey:key,records:[],hasMore:false}):reply(403,{code:c.photoAction==='REPLACE'?'BF_CONSENT_REQUIRED':'BF_FORBIDDEN'});
  await synchronize({db:fn=>fn(f.db),request,now:()=>2000,active:()=>true},session,context);
  let rows=await syncJobs(f.db,user,key);assert.deepEqual(rows.map(r=>[r.status,r.error_code]),[['blocked',CONSENT_HOLD],['blocked',403]]);
  assert.equal(await store.heldCount(f.db,user,'REQUEST_PHOTO'),1);
  await store.cacheList(f.db,parseList(list(),user),1000);await store.enqueue(f.db,user,approve(1,'REQUEST_PHOTO',requestText),1000);
  const result=await flushConsents(ports(f,async(path,body)=>reply(200,receipt(body,'ACTIVE',requestText)),()=>2500),user);
  assert.deepEqual(result.released,['REQUEST_PHOTO']);rows=await syncJobs(f.db,user,key);
  assert.deepEqual(rows.map(r=>r.status),['retry','blocked']);assert.equal(rows[0].attempts,1,'same operation, same UUID');
  f.sql.prepare("UPDATE sync_outbox SET status='blocked',error_code=? WHERE operation_id=?").run(CONSENT_HOLD,op(11));
  await cancelSync(f.db,session,key,op(11),3000);await assert.rejects(cancelSync(f.db,session,key,op(12),3000));
 }finally{f.sql.close();}
});
test('profile command refused for missing consent keeps the encrypted draft and is released by approval',async()=>{
 const f=await fixture();try{
  const command={operationId:op(21),expectedVersion:0,firstName:'Ayşe',lastName:'Yılmaz',phone:null,photoAction:'REPLACE',photoBase64:'/9j/2Q=='};
  await profileQueue.enqueue(f.db,f.files,user,command,1000);
  const response=(status,value)=>new Response(JSON.stringify(value),{status,headers:{'Content-Type':'application/json'}});
  await assert.rejects(synchronizeProfile({store:fn=>fn(f.db,f.files),request:async()=>response(403,{code:'BF_CONSENT_REQUIRED'}),active:()=>true,now:()=>1000,digest:async()=>''},user),e=>e.status===CONSENT_HOLD);
  let job=await profileQueue.currentJob(f.db,user);assert.equal(job.status,'failed');assert.equal(profileQueue.isConsentHeld(job),true);
  assert.equal(profileQueue.canRetry(job),false);assert.equal(profileQueue.canDiscard(job),true);assert.ok(f.map.has(job.body_file));
  await store.releaseHeld(f.db,user,'PROFILE_PHOTO',2000);job=await profileQueue.currentJob(f.db,user);assert.equal(job.status,'retry');assert.equal(job.operation_id,op(21));
 }finally{f.sql.close();}
});
test('withdrawal removes photos from this device at once and keeps unsent uploads waiting',async()=>{
 const f=await fixture();try{
  const session={userId:user,username:'Fixture',contexts:[context],hasMore:false};
  await f.files.write('avatar-x.enc','photo');f.sql.prepare("INSERT INTO profile_cache(user_id,payload,checked_at,photo_file) VALUES (?,?,?,?)").run(user,'{}',1000,'avatar-x.enc');
  await store.dropLocalPhotos(f.db,f.files,user,'PROFILE_PHOTO');
  assert.equal(f.sql.prepare('SELECT photo_file FROM profile_cache').get().photo_file,null);assert.equal(f.map.has('avatar-x.enc'),false);
  const sent={operationId:op(31),recordId:'33333333-3333-4333-8333-333333333333',propertyId:context.propertyId,unitId:context.unitId,role:'OWNER',action:'CREATE',expectedVersion:0,description:'Musluk',photoAction:'REPLACE'};
  const unsent={...sent,operationId:op(32),recordId:'44444444-4444-4444-8444-444444444444'};
  await enqueueSync(f.db,session,key,sent,1000);await enqueueSync(f.db,session,key,unsent,1001);f.sql.prepare("UPDATE sync_outbox SET status='sent' WHERE operation_id=?").run(op(31));
  for(const [o,file] of [[op(31),'request-job-a.enc'],[op(32),'request-job-b.enc']]){await f.files.write(file,'x');f.sql.prepare('INSERT INTO request_files(operation_id,user_id,context_key,body_file) VALUES (?,?,?,?)').run(o,user,key,file);}
  await f.files.write('request-photo-c.enc','x');f.sql.prepare('INSERT INTO request_photo_cache(user_id,context_key,record_id,photo_id,sha256,body_file,checked_at) VALUES (?,?,?,?,?,?,?)').run(user,key,sent.recordId,op(33),'a'.repeat(64),'request-photo-c.enc',1000);
  await store.dropLocalPhotos(f.db,f.files,user,'REQUEST_PHOTO');
  assert.deepEqual([...f.map.keys()],['request-job-b.enc']);assert.equal(f.sql.prepare('SELECT count(*) AS n FROM request_photo_cache').get().n,0);
  // A successful withdrawal receipt applies the same cleanup again without failing.
  await store.cacheList(f.db,parseList(list({profile:'ACTIVE'}),user),1000);await store.enqueue(f.db,user,withdraw(1,'PROFILE_PHOTO'),1000);
  const result=await flushConsents(ports(f,async(path,body)=>reply(200,receipt(body,'WITHDRAWN',profileText))),user);assert.equal(result.sent,1);
  assert.equal(store.forFeature(store.effective((await store.cachedList(f.db,user)).list,[]),'PROFILE_PHOTO').state,'WITHDRAWN');
 }finally{f.sql.close();}
});
