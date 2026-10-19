import {parseMarkup} from './markup.ts';
// İŞ18: shapes of GET /api/v1/mobile/consents and POST /commands (docs/api/mobile-consents.md). Unknown or foreign data is refused.
export type Kind='INFORMATION'|'EXPLICIT_CONSENT';
export type Feature='PROFILE_PHOTO'|'REQUEST_PHOTO';
export type ServerState='NONE'|'ACTIVE'|'OUTDATED'|'WITHDRAWN';
export type Action='APPROVE'|'WITHDRAW';
export type ConsentText={id:string;textVersion:number;title:string;body:string;bodySha256:string;requiresReconsent:boolean;publishedAt:string};
export type ConsentState={typeCode:string;nameTr:string;nameEn:string;kind:Kind;feature:Feature|null;sortOrder:number;state:ServerState;approvedVersion:number|null;approvedAt:string|null;currentTextId:string|null;currentVersion:number|null};
export type ConsentItem={status:ConsentState;currentText:ConsentText|null};
export type ConsentList={userId:string;items:ConsentItem[]};
export type Command={operationId:string;action:Action;typeCode:string;textId:string|null;bodySha256:string|null;appVersion:string|null};
export type Result={operationId:string;status:ConsentState;replayed:boolean};
// error_code kept on a photo job that the server refused with BF_CONSENT_REQUIRED; approving the consent releases it.
export const CONSENT_HOLD=4031;
export const consentRequiredCode='BF_CONSENT_REQUIRED';
export const textOutdatedCode='BF_CONSENT_TEXT_OUTDATED';
export const stateLabels:Record<ServerState,string>={NONE:'Onay verilmedi',ACTIVE:'Onaylı',OUTDATED:'Yeni sürüm onay bekliyor',WITHDRAWN:'Geri çekildi'};
// An information notice is read, not consented to (KVKK art. 10); its labels say so.
export const noticeLabels:Record<ServerState,string>={NONE:'Okundu kaydı yok',ACTIVE:'Okundu',OUTDATED:'Yeni sürüm okunmadı',WITHDRAWN:'Okundu kaydı yok'};
export const stateLabel=(kind:Kind,state:ServerState)=>(kind==='INFORMATION'?noticeLabels:stateLabels)[state];
const uuid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,empty='00000000-0000-0000-0000-000000000000';
const code=/^[A-Z][A-Z0-9_]{1,63}$/,sha=/^[a-f0-9]{64}$/;
function object(value:unknown,what:string):Record<string,unknown>{if(!value||typeof value!=='object'||Array.isArray(value))throw Error(what+' geçersiz.');return value as Record<string,unknown>;}
const id=(v:unknown)=>typeof v==='string'&&uuid.test(v)&&v!==empty;
const date=(v:unknown)=>typeof v==='string'&&Number.isFinite(Date.parse(v));
const version=(v:unknown)=>typeof v==='number'&&Number.isSafeInteger(v)&&v>0;
function label(v:unknown,max:number){if(typeof v!=='string'||!v.trim()||v.length>max||/[\x00-\x1f\x7f]/.test(v))throw Error('Rıza adı geçersiz.');return v;}
export function parseText(value:unknown):ConsentText{
 const t=object(value,'Rıza metni');
 if(!id(t.id)||!version(t.textVersion)||typeof t.requiresReconsent!=='boolean'||!date(t.publishedAt)||typeof t.bodySha256!=='string'||!sha.test(t.bodySha256))throw Error('Rıza metni geçersiz.');
 const title=label(t.title,200);
 // The client shows only what it can draw safely; a body the shared parser rejects is never offered for approval.
 if(typeof t.body!=='string'||parseMarkup(t.body).error)throw Error('Rıza metni biçimi geçersiz.');
 return {id:String(t.id).toLowerCase(),textVersion:t.textVersion as number,title,body:t.body,bodySha256:t.bodySha256,requiresReconsent:t.requiresReconsent,publishedAt:String(t.publishedAt)};
}
export function parseState(value:unknown):ConsentState{
 const s=object(value,'Rıza durumu');
 if(typeof s.typeCode!=='string'||!code.test(s.typeCode)||!['INFORMATION','EXPLICIT_CONSENT'].includes(String(s.kind))||!(s.feature===null||s.feature==='PROFILE_PHOTO'||s.feature==='REQUEST_PHOTO')||typeof s.sortOrder!=='number'||!Number.isSafeInteger(s.sortOrder)||!['NONE','ACTIVE','OUTDATED','WITHDRAWN'].includes(String(s.state)))throw Error('Rıza durumu geçersiz.');
 const approved=s.approvedVersion===null&&s.approvedAt===null||version(s.approvedVersion)&&date(s.approvedAt);
 const current=s.currentTextId===null&&s.currentVersion===null||id(s.currentTextId)&&version(s.currentVersion);
 const state=s.state as ServerState;
 // ACTIVE/OUTDATED always carry an approval; NONE/WITHDRAWN never do. Information notices cannot be withdrawn.
 if(!approved||!current||(state==='ACTIVE'||state==='OUTDATED')!==(s.approvedVersion!==null)||s.kind==='INFORMATION'&&(s.state==='WITHDRAWN'||s.feature!==null)||s.approvedVersion!==null&&s.currentVersion!==null&&(s.approvedVersion as number)>(s.currentVersion as number))throw Error('Rıza durumu tutarsız.');
 return {typeCode:s.typeCode,nameTr:label(s.nameTr,120),nameEn:label(s.nameEn,120),kind:s.kind as Kind,feature:s.feature as Feature|null,sortOrder:s.sortOrder,state,approvedVersion:s.approvedVersion as number|null,approvedAt:s.approvedAt as string|null,currentTextId:s.currentTextId===null?null:String(s.currentTextId).toLowerCase(),currentVersion:s.currentVersion as number|null};
}
export function parseList(value:unknown,userId:string):ConsentList{
 const l=object(value,'Rıza listesi');
 if(l.userId!==userId||!Array.isArray(l.items)||l.items.length>50)throw Error('Rıza listesi hesapla eşleşmiyor.');
 const items=l.items.map(raw=>{const i=object(raw,'Rıza kaydı');const status=parseState(i.status);const currentText=i.currentText===null?null:parseText(i.currentText);
  if((currentText?.id??null)!==status.currentTextId||(currentText?.textVersion??null)!==status.currentVersion)throw Error('Rıza metni durumla eşleşmiyor.');
  return {status,currentText};});
 if(new Set(items.map(i=>i.status.typeCode)).size!==items.length)throw Error('Yinelenen rıza türü.');
 return {userId,items};
}
export function parseCommand(value:unknown):Command{
 const c=object(value,'Rıza işlemi');
 if(!id(c.operationId)||(c.action!=='APPROVE'&&c.action!=='WITHDRAW')||typeof c.typeCode!=='string'||!code.test(c.typeCode))throw Error('Rıza işlemi geçersiz.');
 const approve=c.action==='APPROVE';
 if(approve?!id(c.textId)||typeof c.bodySha256!=='string'||!sha.test(c.bodySha256):c.textId!==null||c.bodySha256!==null)throw Error('Rıza işlemi metni geçersiz.');
 if(!(c.appVersion===null||typeof c.appVersion==='string'&&c.appVersion.length<=40&&!/[\x00-\x1f\x7f]/.test(c.appVersion)))throw Error('Uygulama sürümü geçersiz.');
 return {operationId:String(c.operationId).toLowerCase(),action:c.action,typeCode:c.typeCode,textId:approve?String(c.textId).toLowerCase():null,bodySha256:approve?String(c.bodySha256):null,appVersion:c.appVersion as string|null};
}
// The API omits null fields it does not need; the wire body carries only what the server reads.
export function wire(c:Command){return {operationId:c.operationId,action:c.action,typeCode:c.typeCode,...(c.textId?{textId:c.textId,bodySha256:c.bodySha256}:{}),...(c.appVersion?{appVersion:c.appVersion}:{})};}
export function parseResult(value:unknown,command:Command):Result{
 const r=object(value,'Rıza makbuzu');
 if(r.operationId!==command.operationId||typeof r.replayed!=='boolean')throw Error('Rıza makbuzu işlemle eşleşmiyor.');
 const status=parseState(r.status);
 if(status.typeCode!==command.typeCode)throw Error('Rıza makbuzu türü eşleşmiyor.');
 // A withdrawal never leaves an approval behind; an approval of the current text is ACTIVE for exactly that text.
 if(command.action==='WITHDRAW'&&(status.state==='ACTIVE'||status.state==='OUTDATED')||command.action==='APPROVE'&&(status.state!=='ACTIVE'||status.currentTextId!==command.textId||status.approvedVersion!==status.currentVersion))throw Error('Rıza makbuzu durumu eşleşmiyor.');
 return {operationId:command.operationId,status,replayed:r.replayed};
}
