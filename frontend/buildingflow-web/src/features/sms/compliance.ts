export type LegalSender={id:string;propertyId:string|null;title:string;mersisNumber:string|null;contactInformation:string;optOutKeyword:string;optOutShortCode:string;optOutEnabled:boolean;iysIntegrationEnabled:boolean;iysBrandCode:string|null;isEnabled:boolean;version:number};
export type SmsPreview={typeCode:string;category:string;body:string;footer:string;encoding:"GSM7"|"UCS2";characters:number;segments:number;allowed:boolean;blockedCode:string|null;legalSenderId:string|null};
export type ConsentDecision="APPROVED"|"REJECTED";
export type ConsentRecord={recordId:string;senderId:string;recipientMasked:string;status:ConsentDecision;source:string;occurredAt:string;textVersion:number|null;iysStatus:string;replayed:boolean};
export type ConsentText={version:number;title:string;body:string;bodySha256:string};
export type PreferenceItem={senderId:string;scope:"PLATFORM"|"PROPERTY";propertyId:string|null;senderTitle:string;status:ConsentDecision|"NONE";occurredAt:string|null;textVersion:number|null};
export type Preferences={verifiedPhoneMasked:string|null;text:ConsentText;items:PreferenceItem[]};
export const consentSources=["WEB","MOBILE","ADMIN","SMS_REPLY","PROVIDER_CALLBACK","IYS"] as const;
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const str=(v:unknown,nullable=false)=>typeof v==="string"||nullable&&v===null;
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const date=(v:unknown)=>typeof v==="string"&&!Number.isNaN(Date.parse(v));
const masked=(v:unknown)=>typeof v==="string"&&!/[0-9]{5}/.test(v);
function fail(kind:string):never{throw new Error(`Invalid SMS ${kind}`);}
export function readSender(v:unknown,propertyId:string|null):LegalSender{
 if(!obj(v)||!guid(v.id)||v.propertyId!==propertyId||!str(v.title)||!(v.mersisNumber===null||typeof v.mersisNumber==="string"&&/^[0-9]{16}$/.test(v.mersisNumber))||!str(v.contactInformation)||!str(v.optOutKeyword)||!str(v.optOutShortCode)||typeof v.optOutEnabled!=="boolean"||typeof v.iysIntegrationEnabled!=="boolean"||!str(v.iysBrandCode,true)||typeof v.isEnabled!=="boolean"||!int(v.version,1))fail("sender");
 return v as LegalSender;
}
export function readSenderView(v:unknown,propertyId:string|null):LegalSender|null{
 if(!obj(v)||!("sender" in v))fail("sender view");
 return v.sender===null?null:readSender(v.sender,propertyId);
}
export function readPreview(v:unknown):SmsPreview{
 if(!obj(v)||!str(v.typeCode)||!str(v.category)||!str(v.body)||!str(v.footer)||!["GSM7","UCS2"].includes(String(v.encoding))||!int(v.characters)||!int(v.segments)||typeof v.allowed!=="boolean"||!str(v.blockedCode,true)||!(v.legalSenderId===null||guid(v.legalSenderId))||v.allowed===(v.blockedCode!==null))fail("preview");
 return v as SmsPreview;
}
export function readRecord(v:unknown):ConsentRecord{
 if(!obj(v)||!guid(v.recordId)||!guid(v.senderId)||!masked(v.recipientMasked)||!["APPROVED","REJECTED"].includes(String(v.status))||!consentSources.includes(v.source as typeof consentSources[number])||!date(v.occurredAt)||!(v.textVersion===null||int(v.textVersion,1))||!str(v.iysStatus)||typeof v.replayed!=="boolean")fail("consent record");
 return v as ConsentRecord;
}
export function readPreferenceItem(v:unknown):PreferenceItem{
 if(!obj(v)||!guid(v.senderId)||!["PLATFORM","PROPERTY"].includes(String(v.scope))||!(v.propertyId===null||guid(v.propertyId))||(v.scope==="PLATFORM")!==(v.propertyId===null)||!str(v.senderTitle)||!["APPROVED","REJECTED","NONE"].includes(String(v.status))||!(v.occurredAt===null||date(v.occurredAt))||!(v.textVersion===null||int(v.textVersion,1)))fail("preference");
 return v as PreferenceItem;
}
export function readPreferences(v:unknown):Preferences{
 if(!obj(v)||!(v.verifiedPhoneMasked===null||masked(v.verifiedPhoneMasked))||!obj(v.text)||!int(v.text.version,1)||!str(v.text.title)||!str(v.text.body)||typeof v.text.bodySha256!=="string"||!/^[0-9a-f]{64}$/.test(v.text.bodySha256)||!Array.isArray(v.items))fail("preferences");
 return {verifiedPhoneMasked:v.verifiedPhoneMasked as string|null,text:v.text as ConsentText,items:v.items.map(readPreferenceItem)};
}
export const senderKeyword=(value:string)=>/^RET[A-Z0-9 ]{0,17}$/.test(value)&&value.length>=3;
export const senderShortCode=(value:string)=>/^[0-9]{3,15}$/.test(value);
export const senderMersis=(value:string)=>value===""||/^[0-9]{16}$/.test(value);
// The text shown is the text approved: the server rejects an approval whose hash differs from its current text.
export async function textMatches(text:ConsentText,hash:(value:string)=>Promise<string>){return await hash(text.body)===text.bodySha256;}
export const approveCommand=(text:ConsentText,operationId:string)=>({operationId,approved:true,textVersion:text.version,textSha256:text.bodySha256,channel:"WEB"});
export const withdrawCommand=(operationId:string)=>({operationId,approved:false,channel:"WEB"});
