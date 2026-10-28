import {contextKey,type Context} from '../auth/contracts.ts';
import type {SyncDb} from '../sync/outbox';
import {parseSnapshot,permitted,canManage,guid,type Snapshot} from './contracts.ts';
import {normalizePlate,vehicleTypes,istanbulInstant} from '../../../../shared/parking.ts';
const key=(c:Context,slot:string)=>contextKey(c)+':parking:'+slot;
const lease=(c:Context)=>canManage(c)?3600000:86400000;
export async function purge(db:SyncDb,user:string,c:Context){for(const slot of ['copy','draft'])await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,slot));}
export async function readCopy(db:SyncDb,user:string,c:Context,page:number,now:number){
 const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,'copy'));
 if(!row||!permitted(c)||row.checked_at>now||now-row.checked_at>lease(c))return null;
 const raw=JSON.parse(row.payload);if(raw?.page!==page)return null;return parseSnapshot(raw,user,c,page);
}
export async function saveCopy(db:SyncDb,user:string,c:Context,page:number,v:Snapshot,now:number){const safe=parseSnapshot(v,user,c,page);await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,key(c,'copy'),JSON.stringify(safe),now);}
export type Draft={kind:'vehicle'|'guest';requestId:string;plate:string;type:typeof vehicleTypes[number];brand:string;model:string;color:string;hostUnitId:string;spaceId:string;startsAt:string;endsAt:string};
export function parseDraft(v:unknown,c:Context):Draft{
 const d=v as Draft;if(!d||!permitted(c)||d.kind!==(canManage(c)?'guest':'vehicle')||!guid(d.requestId)||!normalizePlate(d.plate)||!vehicleTypes.includes(d.type)||[d.brand,d.model,d.color].some(s=>typeof s!=='string'||s.length>50))throw Error('Araç taslağı geçersiz.');
 if(d.kind==='guest'&&(!guid(d.hostUnitId)||d.spaceId!==''&&!guid(d.spaceId)||!Number.isFinite(istanbulInstant(d.startsAt))||!Number.isFinite(istanbulInstant(d.endsAt))||istanbulInstant(d.endsAt)<=istanbulInstant(d.startsAt)))throw Error('Misafir taslağı geçersiz.');
 return {kind:d.kind,requestId:d.requestId,plate:normalizePlate(d.plate)!,type:d.type,brand:d.brand.trim(),model:d.model.trim(),color:d.color.trim(),hostUnitId:d.kind==='guest'?d.hostUnitId:'',spaceId:d.kind==='guest'?d.spaceId:'',startsAt:d.kind==='guest'?d.startsAt:'',endsAt:d.kind==='guest'?d.endsAt:''};
}
export async function saveDraft(db:SyncDb,user:string,c:Context,v:Draft,now:number){const d=parseDraft(v,c);await db.runAsync('INSERT INTO owner_snapshots(user_id,context_key,payload,checked_at) VALUES (?,?,?,?) ON CONFLICT(user_id,context_key) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at',user,key(c,'draft'),JSON.stringify(d),now);}
export async function loadDraft(db:SyncDb,user:string,c:Context,now:number){const row=await db.getFirstAsync<{payload:string;checked_at:number}>('SELECT payload,checked_at FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,'draft'));return row&&row.checked_at<=now&&now-row.checked_at<=lease(c)?parseDraft(JSON.parse(row.payload),c):null;}
export async function removeDraft(db:SyncDb,user:string,c:Context){await db.runAsync('DELETE FROM owner_snapshots WHERE user_id=? AND context_key=?',user,key(c,'draft'));}
