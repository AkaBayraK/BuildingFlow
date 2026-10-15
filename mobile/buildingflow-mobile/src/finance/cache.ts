import type {SyncDb} from '../sync/outbox';
import {contextKey,type Context} from '../auth/contracts.ts';
import {parseFinance,type Query} from './contracts.ts';
// Two bounded slots per authorized context: most recent charges/payments page, including detail filter.
const key=(c:Context,q:Query)=>contextKey(c)+':finance:'+q.view;
export async function readFinance(db:SyncDb,user:string,c:Context,q:Query,now:number){
 const r=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,q));
 if(!r||r.checked_at>now||now-r.checked_at>86400000)return null;
 const v=JSON.parse(r.payload);if(v.page!==q.page||v.chargeId!==q.chargeId)return null;return parseFinance(v,user,c,q);
}
export async function writeFinance(db:SyncDb,user:string,c:Context,q:Query,value:unknown,now:number){
 const v=parseFinance(value,user,c,q),old=await db.getFirstAsync<{payload:string}>('SELECT payload FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,q));
 if(old&&Date.parse(JSON.parse(old.payload).asOf)>Date.parse(v.asOf))throw Error('Eski finans yanıtı reddedildi.');
 await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,key(c,q),JSON.stringify(v),now);return v;
}
export async function purgeFinance(db:SyncDb,user:string,c:Context){for(const view of ['charges','payments'] as const)await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,{view,page:1,chargeId:null}));}
