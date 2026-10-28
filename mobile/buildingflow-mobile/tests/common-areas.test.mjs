import test from 'node:test';
import assert from 'node:assert/strict';
import {DatabaseSync} from 'node:sqlite';
import {path,authorizePath,canManage,parseCopy,validDay} from '../src/common-areas/contracts.ts';
import {readCopy,saveCopy,purgeCopies,storeDraft,loadDraft,parseDraft} from '../src/common-areas/cache.ts';
import {segments,readSchedule} from '../../../shared/common-areas.ts';
import {contextKey} from '../src/auth/contracts.ts';
const p='11111111-1111-4111-8111-111111111111',id='22222222-2222-4222-8222-222222222222',r='33333333-3333-4333-8333-333333333333';
const c={propertyId:p,propertyName:'Fixture',unitId:null,unitNumber:null,role:'MANAGER'},q={page:1,areaId:id,date:'2026-10-26'};
const own={...c,role:'RESIDENT',unitId:id,canonical:{kind:'RESIDENT',id:r,tenantId:p}};
const schedule={commonAreaId:id,date:q.date,opensAt:'08:00',closesAt:'22:00',capacity:14,reservable:true,busy:[{startsAt:'10:00',endsAt:'12:00',state:'CLOSED',reserved:0},{startsAt:'12:00',endsAt:'13:00',state:'FULL',reserved:14},{startsAt:'13:00',endsAt:'14:00',state:'PARTIAL',reserved:2}]};
const copy={userId:'fixture',scope:contextKey(c),query:q,asOf:'2026-10-26T08:00:00Z',page:null,schedule};
function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('CREATE TABLE owner_snapshots(user_id TEXT,context_key TEXT,payload TEXT,checked_at INTEGER,PRIMARY KEY(user_id,context_key))');return{sql,db:{runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null}};}
test('manager writes and readonly paths are bound to the selected property/unit/relation',()=>{
 assert.equal(canManage(c),true);assert.equal(canManage({...c,role:'ACCOUNTANT'}),false);assert.throws(()=>path({...c,role:'ACCOUNTANT'},q));
 const url=path(own,q,'/schedule');assert.match(url,/relationId=333/);assert.match(url,/capacity=RESIDENT/);assert.doesNotThrow(()=>authorizePath(own,url,'GET'));
 for(const role of ['RESIDENT','OWNER','TENANT','ACCOUNTANT'])assert.throws(()=>authorizePath({...own,role},`/api/v1/properties/${p}/common-areas/${id}/reservations`,'POST'));
 assert.throws(()=>authorizePath(own,path(own,q,'/reservations'),'GET'));
 for(const bad of [url.replace(r,p),url.replace('capacity=RESIDENT','capacity=OWNER'),url+'&secret=1',url.replace(p,id)])assert.throws(()=>authorizePath(own,bad,'GET'));
 assert.doesNotThrow(()=>authorizePath(c,`/api/v1/properties/${p}/common-areas/${id}/reservations/${r}/cancel`,'POST'));
 assert.throws(()=>authorizePath(c,`/api/v1/properties/${id}/common-areas/${id}/reservations`,'POST'));
});
test('readonly schedule validates privacy, closure state, counts and calendar context',()=>{
 assert.deepEqual(segments(readSchedule(schedule,id,q.date)).map(s=>s.state),['FREE','CLOSED','FULL','PARTIAL','FREE']);
 for(const bad of [{...schedule,personName:'secret'},{...schedule,date:'2026-10-27'},{...schedule,commonAreaId:r},{...schedule,busy:[{...schedule.busy[0],note:'secret'}]},{...schedule,busy:[{...schedule.busy[0],reserved:1}]},{...schedule,busy:[schedule.busy[1],schedule.busy[0]]}])assert.throws(()=>readSchedule(bad,id,q.date));
 assert.equal(validDay('2026-02-30'),false);assert.equal(validDay('2028-02-29'),true);
});
test('cache refuses foreign accounts, contexts, dates, expired copies and clock rollback',async()=>{
 const {sql,db}=fixture();try{await saveCopy(db,'fixture',c,q,copy,1000);assert.deepEqual(await readCopy(db,'fixture',c,q,2000),copy);
 assert.equal(await readCopy(db,'other',c,q,2000),null);assert.equal(await readCopy(db,'fixture',{...c,role:'SITE_ADMIN'},q,2000),null);assert.equal(await readCopy(db,'fixture',c,{...q,date:'2026-10-27'},2000),null);assert.equal(await readCopy(db,'fixture',c,q,999),null);assert.equal(await readCopy(db,'fixture',c,q,86401001),null);
 assert.throws(()=>parseCopy({...copy,scope:'foreign'},'fixture',c,q));assert.throws(()=>parseCopy({...copy,userId:'foreign'},'fixture',c,q));
 await purgeCopies(db,'fixture',c);assert.equal(await readCopy(db,'fixture',c,q,2000),null);
 }finally{sql.close();}
});
test('one bounded schedule slot replaces old date; corrupted payload cannot become trusted data',async()=>{
 const {sql,db}=fixture();try{await saveCopy(db,'fixture',c,q,copy,1000);const next={...q,date:'2026-10-27'};await saveCopy(db,'fixture',c,next,{...copy,query:next,schedule:{...schedule,date:next.date}},2000);assert.equal(sql.prepare('SELECT count(*) n FROM owner_snapshots').get().n,1);assert.equal(await readCopy(db,'fixture',c,q,3000),null);sql.exec("UPDATE owner_snapshots SET payload='{}'");await assert.rejects(()=>readCopy(db,'fixture',c,next,3000));}finally{sql.close();}
});
test('drafts preserve retry UUID, strip private notes, expire and never become outbox commands',async()=>{
 const {sql,db}=fixture(),d={areaId:id,requestId:r,date:q.date,startsAt:'10:00',endsAt:'11:00',partySize:'2'};try{
 assert.deepEqual(parseDraft({...d,note:'secret',unitId:p}),d);await assert.rejects(()=>storeDraft(db,'fixture',own,d,1000));await storeDraft(db,'fixture',c,d,1000);assert.deepEqual(await loadDraft(db,'fixture',c,2000),d);assert.equal(await loadDraft(db,'fixture',c,86401001),null);assert.equal(await loadDraft(db,'other',c,2000),null);await purgeCopies(db,'fixture',c);assert.equal(await loadDraft(db,'fixture',c,2000),null);
 }finally{sql.close();}
});
