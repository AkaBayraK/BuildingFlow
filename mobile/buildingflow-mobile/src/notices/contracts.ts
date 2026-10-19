import {contextKey,selectContext,type Context,type Session} from '../auth/contracts.ts';
import {time,id} from '../owner/contracts.ts';
export type Kind='ANNOUNCEMENT'|'MESSAGE';
export type Query={kind:Kind;page:number;itemId:string|null};
export type Item={id:string;title:string;body:string;createdAt:string;readAt:string|null};
export type Snapshot={userId:string;contextKey:string;asOf:string;propertyName:string;kind:Kind;page:number;itemId:string|null;total:number;unread:number;preference:{highlightsEnabled:boolean;version:number};pushAvailable:false;items:Item[]};
export function validateQuery(q:Query){if(!['ANNOUNCEMENT','MESSAGE'].includes(q.kind)||!Number.isSafeInteger(q.page)||q.page<1||q.page>10000)throw Error('Bildirim sorgusu geçersiz.');if(q.itemId!==null)id(q.itemId);}
function text(v:unknown,max:number){if(typeof v!=='string'||!v.trim()||v.length>max)throw Error('Eksik bildirim bilgisi.');return v;}
export function parsePreference(v:unknown){if(!v||typeof v!=='object')throw Error('Bildirim tercihi eksik.');const p=v as Snapshot['preference'];if(typeof p.highlightsEnabled!=='boolean'||!Number.isSafeInteger(p.version)||p.version<0)throw Error('Bildirim tercihi geçersiz.');return {highlightsEnabled:p.highlightsEnabled,version:p.version};}
export function parseNotice(value:unknown,user:string,c:Context,q:Query):Snapshot{
 validateQuery(q);if(!value||typeof value!=='object')throw Error('Bildirim yanıtı geçersiz.');const v=value as Snapshot;
 if(v.userId!==user||v.contextKey!==contextKey(c)||v.kind!==q.kind||v.page!==q.page||v.itemId!==q.itemId||v.pushAvailable!==false||!Number.isSafeInteger(v.total)||v.total<0||(q.itemId!==null&&v.total>1)||(q.itemId===null&&v.unread>v.total)||!Number.isSafeInteger(v.unread)||v.unread<0||!Array.isArray(v.items)||v.items.length!==Math.min(50,Math.max(0,v.total-(q.page-1)*50)))throw Error('Bildirim kapsamı eşleşmiyor.');
 const ids=new Set<string>();const items=v.items.map(i=>{const itemId=id(i.id);if(ids.has(itemId)||q.itemId!==null&&itemId!==q.itemId)throw Error('Bildirim kimliği eşleşmiyor.');ids.add(itemId);const createdAt=time(i.createdAt),readAt=i.readAt===null?null:time(i.readAt);return {id:itemId,title:text(i.title,200),body:text(i.body,q.kind==='MESSAGE'?2000:10000),createdAt,readAt};});
 return {userId:user,contextKey:contextKey(c),asOf:time(v.asOf),propertyName:text(v.propertyName,200),...q,total:v.total,unread:v.unread,preference:parsePreference(v.preference),pushAvailable:false,items};
}
export function parseNoticeLink(params:Record<string,unknown>,session:Session,c:Context):Query|null{
 if(params.id===undefined&&params.kind===undefined&&params.propertyId===undefined)return null;
 if(typeof params.id!=='string'||typeof params.kind!=='string'||typeof params.propertyId!=='string'||params.propertyId!==c.propertyId)throw Error('Bağlantı seçili mülkle eşleşmiyor. Hesabım’dan yetkili mülkü seçip yeniden açın.');
 selectContext(session,contextKey(c));id(params.propertyId);const q={kind:params.kind as Kind,page:1,itemId:id(params.id)};validateQuery(q);return q;
}
export function readResult(value:unknown,kind:Kind,item:string){if(!value||typeof value!=='object')throw Error('Okundu makbuzu eksik.');const v=value as {id:string;kind:string;readAt:string};if(v.id!==item||v.kind!==kind)throw Error('Okundu makbuzu eşleşmiyor.');return time(v.readAt);}
