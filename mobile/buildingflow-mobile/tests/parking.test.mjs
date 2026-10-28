import test from 'node:test';import assert from 'node:assert/strict';import {DatabaseSync} from 'node:sqlite';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {path,authorizePath,parseSnapshot,permitted,canManage} from '../src/parking/contracts.ts';
import {readCopy,saveCopy,saveDraft,loadDraft,removeDraft,purge,parseDraft} from '../src/parking/cache.ts';
import {loadScoped} from '../src/data/scopedRead.ts';
import {types,statuses,tabs} from '../src/parking/type-style.ts';
const P='11111111-1111-4111-8111-111111111111',U='22222222-2222-4222-8222-222222222222',I='33333333-3333-4333-8333-333333333333',R='44444444-4444-4444-8444-444444444444',user='parking-test';
const c={propertyId:P,propertyName:'Fixture',unitId:U,unitNumber:'1',role:'TENANT'},manager={...c,unitId:null,unitNumber:null,role:'MANAGER'};
const vehicle={id:I,unitId:U,plate:'34ABC1',type:'CAR',brand:null,model:null,color:null,status:'PENDING',reviewReason:null,version:1};
const snapshot={userId:user,contextKey:`${P}:${U}:TENANT`,propertyId:P,unitId:U,role:'TENANT',asOf:'2026-10-27T10:00:00Z',page:1,vehicleTotal:1,allocationTotal:0,guestTotal:0,unitTotal:0,spaceTotal:0,vehicles:[vehicle],allocations:[],guests:[],units:[],spaces:[],policyConfigured:true,guestMaxHours:24};
const m={...snapshot,contextKey:`${P}:-:MANAGER`,unitId:null,role:'MANAGER',vehicleTotal:0,vehicles:[]};
const draft={kind:'vehicle',requestId:R,plate:'34 abc 1',type:'CAR',brand:' Test ',model:'',color:'',hostUnitId:'',spaceId:'',startsAt:'',endsAt:''};
async function fixture(){const sql=new DatabaseSync(':memory:');const db={execAsync:async s=>sql.exec(s),runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null};await migrate(db);await writeSnapshot(db,{userId:user,username:null,contexts:[c,manager],hasMore:false},1000,'fixture',snapshot.contextKey);return{db,sql};}
test('parking paths bind exact account context and separate manager operations from resident actions',()=>{
 const own=path(c,1);assert.doesNotThrow(()=>authorizePath(c,own,'GET'));assert.doesNotThrow(()=>authorizePath(c,path(c,1,'/vehicles'),'POST'));
 for(const url of [own+'&unitId='+I,own.replace(U,I),own.replace('role=TENANT','role=MANAGER'),own+'&plate=34ABC1','https://evil.test'+own])assert.throws(()=>authorizePath(c,url,'GET'));
 assert.throws(()=>authorizePath(c,path(c,1,'/guests'),'POST'));assert.throws(()=>authorizePath(manager,path(manager,1,'/vehicles'),'POST'));
 assert.doesNotThrow(()=>authorizePath(manager,path(manager,1,`/guests/${I}/arrive`),'POST'));
 assert.equal(permitted({...c,role:'ACCOUNTANT'}),false);assert.equal(permitted({...manager,role:'ACCOUNTANT'}),false);assert.equal(canManage(manager),true);
 const canonical={...c,canonical:{kind:'TENANT',id:R,tenantId:I}};assert.match(path(canonical,2),new RegExp(`contextKind=TENANT&contextId=${R}&page=2$`));
 assert.throws(()=>path(c,0));assert.throws(()=>path(c,10001));
});
test('parking snapshots reject foreign scope and strip unrelated personal fields',()=>{
 const v=parseSnapshot({...snapshot,token:'secret',vehicles:[{...vehicle,personName:'Other',token:'secret'}]},user,c,1);assert.equal('token' in v,false);assert.equal('personName' in v.vehicles[0],false);
 for(const patch of [{userId:'other'},{contextKey:'foreign'},{propertyId:I},{unitId:I},{role:'OWNER'},{page:2},{vehicles:[{...vehicle,unitId:I}]},{vehicles:[vehicle,vehicle]},{guests:[{}]}])assert.throws(()=>parseSnapshot({...snapshot,...patch},user,c,1));
 const guest={id:I,hostUnitId:U,unitNumber:'1',spaceId:null,spaceCode:null,plate:'34ABC2',guestName:'Sensitive',note:'Private',startsAt:'2026-10-27T10:00:00Z',endsAt:'2026-10-27T11:00:00Z',status:'PLANNED',arrivedAt:null,departedAt:null,version:1,contact:'secret'};
 const mg=parseSnapshot({...m,guestTotal:1,guests:[guest]},user,manager,1);assert.equal(mg.guests[0].guestName,null);assert.equal(mg.guests[0].note,null);assert.equal('contact' in mg.guests[0],false);
});
test('SQLite parking copies isolate contexts/pages and expire on TTL or clock rollback',async()=>{
 const {db,sql}=await fixture();try{await saveCopy(db,user,c,1,snapshot,1000);assert.equal((await readCopy(db,user,c,1,1001)).vehicles[0].id,I);assert.equal(await readCopy(db,user,c,2,1001),null);assert.equal(await readCopy(db,user,{...c,role:'OWNER'},1,1001),null);assert.equal(await readCopy(db,'other',c,1,1001),null);assert.equal(await readCopy(db,user,c,1,999),null);assert.equal(await readCopy(db,user,c,1,86401001),null);
 await saveCopy(db,user,manager,1,m,1000);assert.equal(await readCopy(db,user,manager,1,3601001),null);
 }finally{sql.close();}
});
test('offline vehicle draft keeps UUID across reopen, does not become an outbox command and purges only its scope',async()=>{
 const {db,sql}=await fixture();try{await saveDraft(db,user,c,draft,1000);const v=await loadDraft(db,user,c,1001);assert.equal(v.requestId,R);assert.equal(v.plate,'34ABC1');assert.equal(v.brand,'Test');assert.equal(await loadDraft(db,'other',c,1001),null);assert.equal(await loadDraft(db,user,manager,1001),null);
 assert.throws(()=>parseDraft({...draft,kind:'guest'},c));assert.throws(()=>parseDraft({...draft,token:'secret',plate:''},c));assert.equal(sql.prepare('SELECT COUNT(*) AS n FROM sync_outbox').get().n,0);
 await saveCopy(db,user,c,1,snapshot,1000);await saveCopy(db,user,manager,1,m,1000);await purge(db,user,c);assert.equal(await loadDraft(db,user,c,1001),null);assert.equal(await readCopy(db,user,c,1,1001),null);assert.ok(await readCopy(db,user,manager,1,1001));await removeDraft(db,user,c);
 }finally{sql.close();}
});
test('guest draft is role-bound and validates server-facing half-open time window',()=>{
 const d={...draft,kind:'guest',hostUnitId:U,startsAt:'2026-10-27T13:00',endsAt:'2026-10-27T14:00'};assert.equal(parseDraft(d,manager).requestId,R);assert.throws(()=>parseDraft(d,c));assert.throws(()=>parseDraft({...d,endsAt:d.startsAt},manager));assert.throws(()=>parseDraft({...d,spaceId:'bad'},manager));
});
test('offline read never sends; denied or corrupt cache clears presentation even when purge fails',async()=>{
 let sends=0,shown=[],saved=0;const base={active:()=>true,offline:true,cache:true,read:async()=>snapshot,request:async()=>{sends++;return{status:403,json:async()=>({})};},parse:v=>v,save:async()=>{saved++;},purge:async()=>{},value:v=>shown.push(v),error:()=>{}};
 await loadScoped(base);assert.equal(sends,0);assert.equal(saved,0);assert.equal(shown[0],snapshot);
 shown=[];await loadScoped({...base,offline:false,purge:async()=>{throw Error('disk');}});assert.equal(shown.at(-1),null);assert.equal(saved,0);
 shown=[];await loadScoped({...base,read:async()=>{throw Error('corrupt');},purge:async()=>{throw Error('disk');}});assert.equal(shown.at(-1),null);
});
test('a late parking response cannot save after context change',async()=>{
 let active=true,saved=false,shown=false;await loadScoped({active:()=>active,offline:false,cache:false,read:async()=>null,request:async()=>{active=false;return{status:200,json:async()=>snapshot};},parse:v=>v,save:async()=>{saved=true;},purge:async()=>{},value:()=>{shown=true;},error:()=>{}});assert.equal(saved,false);assert.equal(shown,false);
});
test('native parking labels carry icons for every type/status',()=>{for(const table of [types,statuses,tabs])for(const item of Object.values(table))assert.ok(item.icon&&item.label);});
