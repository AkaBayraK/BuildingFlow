import test from 'node:test';
import assert from 'node:assert/strict';
import { DatabaseSync } from 'node:sqlite';
import { migrate, readSnapshot, writeSnapshot, writeSelection, MAX_CACHE_AGE_MS } from '../src/offline/database.ts';
function fixture() {
 const sql = new DatabaseSync(':memory:'); sql.exec('PRAGMA foreign_keys=ON');
 const db = { execAsync: async s=>{sql.exec(s);}, runAsync:async(s,...v)=>sql.prepare(s).run(...v), getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null };
 return {sql,db};
}
const context={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Test mülkü',unitId:'22222222-2222-2222-2222-222222222222',unitNumber:'1',role:'OWNER'};
const key=`${context.propertyId}:${context.unitId}:OWNER`;
const session={userId:'user-a',username:'Test',contexts:[context],hasMore:false};
test('migration creates version5, preserves rows when repeated, indexes account context',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,session,1000,'id-1',key);await migrate(db);assert.equal(sql.prepare('PRAGMA user_version').get().user_version,6);assert.equal((await readSnapshot(db,'user-a',1001)).session.contexts[0].propertyName,'Test mülkü');assert.ok(sql.prepare("SELECT name FROM sqlite_master WHERE name='cache_scope'").get());}finally{sql.close();}
});
test('version1 upgrades without erasing snapshot; future schema is refused',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,session,1000,'id-1',key);sql.exec('DROP TABLE request_photo_jobs; DROP TABLE request_drafts; DROP TABLE owner_snapshots; DROP TABLE profile_cache; DROP TABLE profile_jobs; DROP TABLE profile_settings; DROP INDEX cache_scope; DROP TABLE sync_outbox; DROP TABLE sync_requests; DROP TABLE sync_state; PRAGMA user_version=1');await migrate(db);assert.ok(await readSnapshot(db,'user-a',1001));sql.exec('PRAGMA user_version=7');await assert.rejects(migrate(db));assert.equal(sql.prepare('SELECT count(*) AS n FROM cache_records').get().n,1);}finally{sql.close();}
});
test('another account cannot read or overwrite cache; malformed context is refused',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,session,1000,'id-1',key);assert.equal(await readSnapshot(db,'user-b',1001),null);await assert.rejects(writeSnapshot(db,{...session,userId:'user-b'},1001,'id-2',null));await assert.rejects(writeSnapshot(db,session,1001,'id-2','foreign'));assert.equal((await readSnapshot(db,'user-a',1001)).session.userId,'user-a');}finally{sql.close();}
});
test('24h cache limit, backwards clock, deleted rows and invalid payload fail closed',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,session,1000,'id-1',key);assert.ok(await readSnapshot(db,'user-a',1000+MAX_CACHE_AGE_MS));assert.equal(await readSnapshot(db,'user-a',1001+MAX_CACHE_AGE_MS),null);assert.equal(await readSnapshot(db,'user-a',999),null);sql.exec('UPDATE cache_records SET deleted_at=1001');assert.equal(await readSnapshot(db,'user-a',1001),null);sql.exec("UPDATE cache_records SET deleted_at=NULL,payload='{}'");await assert.rejects(readSnapshot(db,'user-a',1001));}finally{sql.close();}
});
test('snapshot whitelists fields and never writes extra tokens; SQL text is safely bound',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,{...session,accessToken:'must-not-store',refreshToken:'must-not-store',username:"Test'); DROP TABLE account; --"},1000,'id-1',key);const row=sql.prepare('SELECT payload FROM cache_records').get();assert.ok(!row.payload.includes('must-not-store'));assert.equal((await readSnapshot(db,'user-a',1001)).session.username,"Test'); DROP TABLE account; --");}finally{sql.close();}
});
test('snapshot replacement is atomic on failure and selection stays inside authorized contexts',async()=>{
 const {sql,db}=fixture();try {await migrate(db);await writeSnapshot(db,session,1000,'id-1',key);sql.exec("CREATE TRIGGER stop_update BEFORE UPDATE ON preferences BEGIN SELECT RAISE(ABORT,'fixture'); END");await assert.rejects(writeSnapshot(db,{...session,username:'Changed'},2000,'id-2',key));assert.equal((await readSnapshot(db,'user-a',2001)).session.username,'Test');sql.exec('DROP TRIGGER stop_update');const cached=await readSnapshot(db,'user-a',2001);await assert.rejects(writeSelection(db,cached,'foreign'));await writeSelection(db,cached,key);assert.equal((await readSnapshot(db,'user-a',2001)).selectedKey,key);}finally{sql.close();}
});

const { CacheQueue } = await import('../src/offline/queue.ts');
test('logout invalidates queued cache writes; next account waits for deletion',async()=>{
 const q=new CacheQueue();const events=[];let release;const gate=new Promise(r=>release=r);
 const busy=q.run(()=>gate);const stale=q.write(async()=>{events.push('old-write');return true;},false);
 const clear=q.clear(async()=>{events.push('key-delete');events.push('file-delete');});
 const fresh=q.write(async()=>{events.push('new-account');return true;},false);
 release();await busy;assert.equal(await stale,false);await clear;assert.equal(await fresh,true);assert.deepEqual(events,['key-delete','file-delete','new-account']);
});
test('failed cache operation does not block subsequent cleanup',async()=>{
 const q=new CacheQueue();await assert.rejects(q.run(async()=>{throw Error('fixture');}));let cleaned=false;await q.clear(async()=>{cleaned=true;});assert.equal(cleaned,true);
});
