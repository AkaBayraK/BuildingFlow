import type {SyncDb} from '../sync/outbox';
import {contextKey,type Context} from '../auth/contracts.ts';
import {parseManager,type Section} from './contracts.ts';
// Reuse the encrypted, account-owned snapshot table. One last page per section bounds disk use.
const key=(c:Context,s:Section)=>contextKey(c)+':manager:'+s;
export async function readManager(db:SyncDb,user:string,c:Context,s:Section,page:number,now:number){
 const r=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,s));
 if(!r||r.checked_at>now||now-r.checked_at>86400000)return null;
 const v=JSON.parse(r.payload);if(v.page!==page)return null;return parseManager(v,user,c,s,page);
}
export async function writeManager(db:SyncDb,user:string,c:Context,s:Section,page:number,value:unknown,now:number){
 const v=parseManager(value,user,c,s,page);
 const old=await db.getFirstAsync<{payload:string}>('SELECT payload FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,s));
 if(old&&Date.parse(JSON.parse(old.payload).asOf)>Date.parse(v.asOf))throw Error('Eski yönetim yanıtı reddedildi.');
 await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,key(c,s),JSON.stringify(v),now);return v;
}
export async function purgeManager(db:SyncDb,user:string,c:Context){for(const s of ['overview','buildings','units','residents','finance','announcements','requests'] as Section[])await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,s));}
