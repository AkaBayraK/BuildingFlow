import type {SyncDb} from '../sync/outbox';
import {contextKey,type Context} from '../auth/contracts.ts';
import {parseCopy,type Query,type Copy} from './contracts.ts';
import {guid} from '../../../../shared/common-areas.ts';
import {canManage,validDay} from './contracts.ts';
const key=(c:Context,q:Query)=>contextKey(c)+':common-areas:'+(q.areaId?'schedule':'list');
export async function readCopy(db:SyncDb,user:string,c:Context,q:Query,now:number){
 const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,q));
 if(!row||row.checked_at>now||now-row.checked_at>86400000)return null;
 const raw=JSON.parse(row.payload);if(!raw?.query||typeof raw.query!=='object')throw Error('Önbellek bozuk.');if(JSON.stringify(raw.query)!==JSON.stringify(q))return null;return parseCopy(raw,user,c,q);
}
export async function saveCopy(db:SyncDb,user:string,c:Context,q:Query,v:Copy,now:number){
 const copy=parseCopy(v,user,c,q);
 await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,key(c,q),JSON.stringify(copy),now);
}
export async function purgeCopies(db:SyncDb,user:string,c:Context){for(const slot of ['list','schedule','draft'])await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,contextKey(c)+':common-areas:'+slot);}
// A reservation draft is never an outbox command and carries no person/unit/note.
export type Draft={areaId:string;requestId:string;date:string;startsAt:string;endsAt:string;partySize:string};
export function parseDraft(v:unknown):Draft{const d=v as Draft;if(!d||!guid(d.areaId)||!guid(d.requestId)||!validDay(d.date)||!/^([01]\d|2[0-3]):(00|15|30|45)$/.test(d.startsAt)||!/^([01]\d|2[0-3]):(00|15|30|45)$/.test(d.endsAt)||d.endsAt<=d.startsAt||!/^\d{1,5}$/.test(d.partySize)||Number(d.partySize)<1||Number(d.partySize)>10000)throw Error('Taslak geçersiz.');return{areaId:d.areaId,requestId:d.requestId,date:d.date,startsAt:d.startsAt,endsAt:d.endsAt,partySize:d.partySize};}
const draftKey=(c:Context)=>contextKey(c)+':common-areas:draft';
export async function storeDraft(db:SyncDb,user:string,c:Context,input:Draft,now:number){if(!canManage(c))throw Error('Yönetici gerekli.');const d=parseDraft(input);await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,draftKey(c),JSON.stringify(d),now);}
export async function loadDraft(db:SyncDb,user:string,c:Context,now:number){if(!canManage(c))return null;const r=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,draftKey(c));return r&&r.checked_at<=now&&now-r.checked_at<=86400000?parseDraft(JSON.parse(r.payload)):null;}
export async function removeDraft(db:SyncDb,user:string,c:Context){await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,draftKey(c));}
