import type {SyncDb} from '../sync/outbox';
import {parseSnapshot,type Snapshot} from './contracts.ts';
import {contextKey,type Context} from '../auth/contracts.ts';
export async function readOwner(db:SyncDb,user:string,c:Context,now:number){const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,contextKey(c));if(!row||row.checked_at>now||now-row.checked_at>24*60*60*1000)return null;return parseSnapshot(JSON.parse(row.payload),user,c);}
export async function writeOwner(db:SyncDb,user:string,c:Context,value:unknown,now:number){const snapshot=parseSnapshot(value,user,c);const old=await readOwner(db,user,c,now);if(old&&Date.parse(old.asOf)>Date.parse(snapshot.asOf))throw Error('Eski mülk yanıtı reddedildi.');await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,contextKey(c),JSON.stringify(snapshot),now);return snapshot;}
export async function purgeOwner(db:SyncDb,user:string,c:Context){await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,contextKey(c));}
export type {Snapshot};
