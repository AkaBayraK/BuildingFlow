export type SmsType={code:string;purpose:"TRANSACTIONAL"|"OPERATIONAL";parameters:string[];secretParameters:string[];defaultContent:string};
export type SmsAccount={id:string;propertyId:string;name:string;provider:"MOCK";senderHeader:string;username:string|null;hasSecret:boolean;apiUrl:string|null;creditBalance:number;lowCreditThreshold:number;isLowCredit:boolean;isEnabled:boolean;version:number};
export type SmsOverview={propertyId:string;account:SmsAccount|null;types:SmsType[]};
export type SmsCredit={id:string;type:"LOAD"|"CONSUME"|"REFUND"|"ADJUST";quantity:number;balanceAfter:number;unitPrice:number|null;currency:string|null;referenceNo:string|null;description:string|null;messageId:string|null;createdAt:string;createdBy:string};
export type SmsTemplate={id:string|null;typeCode:string;name:string;content:string;purpose:string;isEnabled:boolean;isSystemDefault:boolean;estimatedSegments:number;version:number};
export type SmsMessage={id:string;typeCode:string;purpose:string;personId:string|null;phoneMasked:string;message:string;segments:number;creditCost:number;status:"QUEUED"|"SENT"|"FAILED";provider:string|null;providerMessageId:string|null;errorCode:string|null;attempts:number;createdAt:string;sentAt:string|null;createdBy:string};
export type Page<T>={items:T[];page:number;pageSize:number;totalCount:number};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const text=(v:unknown,nullable=false)=>typeof v==="string"||nullable&&v===null;
const date=(v:unknown)=>typeof v==="string"&&!Number.isNaN(Date.parse(v));
function fail(kind:string):never{throw new Error(`Invalid SMS ${kind}`);}
export function readType(v:unknown):SmsType{
 if(!obj(v)||typeof v.code!=="string"||!["TRANSACTIONAL","OPERATIONAL"].includes(String(v.purpose))||!Array.isArray(v.parameters)||!v.parameters.every(p=>typeof p==="string")||!Array.isArray(v.secretParameters)||typeof v.defaultContent!=="string")fail("type");
 return v as SmsType;
}
export function readAccount(v:unknown,propertyId:string):SmsAccount{
 if(!obj(v)||!guid(v.id)||v.propertyId!==propertyId||!text(v.name)||v.provider!=="MOCK"||!text(v.senderHeader)||!text(v.username,true)||typeof v.hasSecret!=="boolean"||!text(v.apiUrl,true)||!int(v.creditBalance)||!int(v.lowCreditThreshold)||typeof v.isLowCredit!=="boolean"||typeof v.isEnabled!=="boolean"||!int(v.version,1))fail("account");
 return v as SmsAccount;
}
export function readOverview(v:unknown,propertyId:string):SmsOverview{
 if(!obj(v)||v.propertyId!==propertyId||!Array.isArray(v.types))fail("overview");
 return {propertyId,account:v.account===null?null:readAccount(v.account,propertyId),types:v.types.map(readType)};
}
export function readCredit(v:unknown):SmsCredit{
 if(!obj(v)||!guid(v.id)||!["LOAD","CONSUME","REFUND","ADJUST"].includes(String(v.type))||!Number.isSafeInteger(v.quantity)||v.quantity===0||!int(v.balanceAfter)||!(v.unitPrice===null||typeof v.unitPrice==="number")||!text(v.currency,true)||!text(v.referenceNo,true)||!text(v.description,true)||!(v.messageId===null||guid(v.messageId))||!date(v.createdAt)||!text(v.createdBy))fail("credit");
 return v as SmsCredit;
}
export function readTemplate(v:unknown):SmsTemplate{
 if(!obj(v)||!(v.id===null||guid(v.id))||!text(v.typeCode)||!text(v.name)||!text(v.content)||!text(v.purpose)||typeof v.isEnabled!=="boolean"||typeof v.isSystemDefault!=="boolean"||!int(v.estimatedSegments)||!int(v.version)||(v.id===null)!==v.isSystemDefault)fail("template");
 return v as SmsTemplate;
}
export function readMessage(v:unknown):SmsMessage{
 if(!obj(v)||!guid(v.id)||!text(v.typeCode)||!text(v.purpose)||!(v.personId===null||guid(v.personId))||!text(v.phoneMasked)||/[0-9]{5}/.test(String(v.phoneMasked))||!text(v.message)||!int(v.segments,1)||!int(v.creditCost,1)||!["QUEUED","SENT","FAILED"].includes(String(v.status))||!text(v.provider,true)||!text(v.providerMessageId,true)||!text(v.errorCode,true)||!int(v.attempts)||!date(v.createdAt)||!(v.sentAt===null||date(v.sentAt))||!text(v.createdBy))fail("message");
 return v as SmsMessage;
}
export function readPage<T>(v:unknown,read:(row:unknown)=>T):Page<T>{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize))fail("page");
 return {items:v.items.map(read),page:Number(v.page),pageSize:Number(v.pageSize),totalCount:Number(v.totalCount)};
}
const gsmBasic="@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
const gsmExtended="^{}\\[~]|€\f";
export function segments(value:string):{encoding:"GSM-7"|"UCS-2";length:number;segments:number}{
 if(value==="")return {encoding:"GSM-7",length:0,segments:0};
 let units=0;
 for(const c of value){if(gsmBasic.includes(c))units++;else if(gsmExtended.includes(c))units+=2;else return {encoding:"UCS-2",length:value.length,segments:value.length<=70?1:Math.ceil(value.length/67)};}
 return {encoding:"GSM-7",length:units,segments:units<=160?1:Math.ceil(units/153)};
}
export const placeholders=(value:string)=>[...new Set([...value.matchAll(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g)].map(m=>m[1]))];
export const preview=(value:string,values:Record<string,string>)=>value.replace(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g,(match,name:string)=>values[name]??match);
