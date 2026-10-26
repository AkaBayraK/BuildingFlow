export type Decision='APPROVED'|'REJECTED'|'NONE';
export type Item={senderId:string;scope:'PLATFORM'|'PROPERTY';propertyId:string|null;senderTitle:string;status:Decision;occurredAt:string|null;textVersion:number|null};
export type Text={version:number;title:string;body:string;bodySha256:string};
export type Preferences={verifiedPhoneMasked:string|null;text:Text;items:Item[]};
const root='/api/v1/mobile/communication-preferences';
const guid=/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const masked=(v:unknown)=>v===null||typeof v==='string'&&!/[0-9]{5}/.test(v);
export const preferencesPath=root;
export const decisionPath=(sender:string)=>{if(!guid.test(sender))throw Error('Invalid sender');return `${root}/${sender.toLowerCase()}`;};
export function isCommunicationRequest(path:string,body:unknown){return body===undefined?path===root:new RegExp(`^${root}/[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$`).test(path);}
export function parseItem(v:unknown):Item{
 if(!obj(v)||typeof v.senderId!=='string'||!guid.test(v.senderId)||!['PLATFORM','PROPERTY'].includes(String(v.scope))||(v.scope==='PLATFORM')!==(v.propertyId===null)||!(v.propertyId===null||typeof v.propertyId==='string'&&guid.test(v.propertyId))||typeof v.senderTitle!=='string'||!['APPROVED','REJECTED','NONE'].includes(String(v.status))||!(v.occurredAt===null||typeof v.occurredAt==='string'&&!Number.isNaN(Date.parse(v.occurredAt)))||!(v.textVersion===null||Number.isSafeInteger(v.textVersion)))throw Error('Invalid preference');
 return v as Item;
}
export function parsePreferences(v:unknown):Preferences{
 if(!obj(v)||!masked(v.verifiedPhoneMasked)||!obj(v.text)||!Number.isSafeInteger(v.text.version)||typeof v.text.title!=='string'||typeof v.text.body!=='string'||typeof v.text.bodySha256!=='string'||!/^[0-9a-f]{64}$/.test(v.text.bodySha256)||!Array.isArray(v.items))throw Error('Invalid preferences');
 return {verifiedPhoneMasked:v.verifiedPhoneMasked as string|null,text:v.text as Text,items:v.items.map(parseItem)};
}
// Approval carries the exact text version and hash shown on screen; the server rejects anything else.
export const approve=(text:Text,operationId:string)=>({operationId,approved:true,textVersion:text.version,textSha256:text.bodySha256,channel:'MOBILE'});
export const withdraw=(operationId:string)=>({operationId,approved:false,channel:'MOBILE'});
export function preferenceError(status:number){
 if(status===409)return 'Metin güncellendi veya telefonunuz doğrulanmadı. Durumu yenileyip tekrar deneyin.';
 if(status===404)return 'Bu gönderici artık size açık değil.';
 if(status===401||status===403)return 'Tercih için hesabınıza yeniden giriş yapın.';
 return 'Tercih kaydedilemedi. Bağlantınızı kontrol edip tekrar deneyin.';
}
export const statusLabels:Record<Decision,string>={APPROVED:'İzin verildi',REJECTED:'İzin yok (ret)',NONE:'İzin verilmedi'};
