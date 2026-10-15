import test from 'node:test';
import assert from 'node:assert/strict';
import {DatabaseSync} from 'node:sqlite';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {parseManager} from '../src/manager/contracts.ts';
import {readManager,writeManager,purgeManager} from '../src/manager/cache.ts';
const c={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:null,unitNumber:null,role:'MANAGER'},user='manager-fixture';
const summary={buildings:1,units:51,residents:0,announcements:0,requests:0,balance:'100.00',overdue:'20.00'};
const row={id:'22222222-2222-2222-2222-222222222222',title:'1',detail:'A · Kat 1',amount:null,state:null};
const data={userId:user,contextKey:c.propertyId+':-:MANAGER',asOf:'2026-10-15T00:00:00Z',propertyName:'Fixture',address:'Address',section:'units',page:2,total:51,summary,rows:[row]};
async function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');const db={execAsync:async s=>sql.exec(s),runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null};await migrate(db);await writeSnapshot(db,{userId:user,username:null,contexts:[c],hasMore:false},1000,'fixture',data.contextKey);return {sql,db};}
test('manager rejects cross-user/property/role/section/page responses and accountant access',()=>{
 assert.equal(parseManager(data,user,c,'units',2).rows.length,1);
 for(const bad of [{...c,role:'ACCOUNTANT'},{...c,role:'OWNER'},{...c,propertyId:row.id},{...c,unitId:row.id}])assert.throws(()=>parseManager(data,user,bad,'units',2));
 assert.throws(()=>parseManager(data,'other',c,'units',2));assert.throws(()=>parseManager(data,user,c,'residents',2));assert.throws(()=>parseManager(data,user,c,'units',1));
});
test('page count, duplicate rows, unknown states and invalid money fail closed',()=>{
 for(const value of [{...data,total:52},{...data,rows:[row,row]},{...data,rows:[{...row,state:'PUBLISHED'}]},{...data,summary:{...summary,overdue:'101.00'}},{...data,summary:{...summary,balance:'1e20'}},{...data,summary:{...summary,units:-1}}])assert.throws(()=>parseManager(value,user,c,'units',2));
});
test('encrypted database cache preserves only whitelist, last page and scoped 24h copy',async()=>{
 const {sql,db}=await fixture();try{
 await writeManager(db,user,c,'units',2,{...data,accessToken:'private',summary:{...summary,identityNumber:'private'},rows:[{...row,email:'private'}]},1000);
 assert.ok(await readManager(db,user,c,'units',2,1001));assert.equal(await readManager(db,user,c,'units',1,1001),null);assert.equal(await readManager(db,user,c,'units',2,999),null);assert.equal(await readManager(db,user,c,'units',2,86401001),null);
 assert.equal(await readManager(db,'foreign',c,'units',2,1001),null);assert.equal(await readManager(db,user,{...c,role:'SITE_ADMIN'},'units',2,1001),null);
 assert.ok(!sql.prepare('SELECT payload FROM owner_snapshots').get().payload.includes('private'));
 await assert.rejects(writeManager(db,user,c,'units',2,{...data,asOf:'2026-10-14T00:00:00Z'},1002));
 await writeManager(db,user,c,'units',3,{...data,page:3,total:51,rows:[]},1003);assert.equal(await readManager(db,user,c,'units',2,1004),null);assert.equal(sql.prepare('SELECT COUNT(*) AS n FROM owner_snapshots').get().n,1);
 await purgeManager(db,user,c);assert.equal(await readManager(db,user,c,'units',3,1004),null);
 }finally{sql.close();}
});
