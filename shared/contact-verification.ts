// Strict, own-account DTO boundary. Unknown states must never look verified.
export const contactStates=["MISSING","UNVERIFIED","PENDING","VERIFIED","UNKNOWN"] as const;
export type ContactState=typeof contactStates[number];
export const outcomes=["ACCEPTED","UNKNOWN","FAILED","UNAVAILABLE","TOO_SOON","RATE_LIMITED","REJECTED","LOCKED","EXPIRED","CANCELLED","VERIFIED","ALREADY_VERIFIED"] as const;
export type Outcome=typeof outcomes[number];
export type Contact={version:number;email:{status:ContactState;value:string|null;available:boolean;checkedAt:string};phone:{status:ContactState;profileValue:string|null;verifiedValue:string|null;pendingValue:string|null;verifiedAt:string|null;canSendCode:boolean;deliveryStatus:string;challengeId:string|null;expiresAt:string|null;attemptsRemaining:number|null;retryAfterSeconds:number}};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const count=(v:unknown):v is number=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=0;
const text=(v:unknown,n:number):v is string|null=>v===null||typeof v==="string"&&v.length<=n;
const date=(v:unknown):v is string=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(v);
const fail=():never=>{throw new Error("Invalid contact response");};
export function parseContact(v:unknown):Contact{
 if(!obj(v)||!count(v.version)||!obj(v.email)||!obj(v.phone))return fail();const e=v.email,p=v.phone;
 if(!contactStates.includes(e.status as ContactState)||e.status==="PENDING"||!text(e.value,254)||typeof e.available!=="boolean"||!date(e.checkedAt)||!contactStates.includes(p.status as ContactState)||p.status==="UNKNOWN")return fail();
 if(![p.profileValue,p.verifiedValue,p.pendingValue].every(v=>text(v,32))||!(p.verifiedAt===null||date(p.verifiedAt))||typeof p.canSendCode!=="boolean"||!["NOT_REQUESTED","RESERVED","ACCEPTED","UNKNOWN","FAILED","VERIFIED","CANCELLED","EXPIRED","LOCKED"].includes(String(p.deliveryStatus))||!(p.challengeId===null||guid(p.challengeId))||!(p.expiresAt===null||date(p.expiresAt))||!(p.attemptsRemaining===null||count(p.attemptsRemaining)&&p.attemptsRemaining<=5)||!count(p.retryAfterSeconds))return fail();
 if(e.status==="VERIFIED"&&(!e.available||!e.value)||p.status==="VERIFIED"&&(!p.verifiedValue||!p.verifiedAt)||p.status==="PENDING"&&!p.pendingValue)return fail();
 return v as unknown as Contact;
}
export function parseOtpResult(v:unknown,operationId:string){
 if(!obj(v)||v.operationId!==operationId||typeof v.replayed!=="boolean"||!outcomes.includes(v.outcome as Outcome)||!count(v.retryAfterSeconds))return fail();
 return {outcome:v.outcome as Outcome,contact:parseContact(v.contact),retryAfterSeconds:v.retryAfterSeconds};
}
export function maskContact(v:string|null,channel:"email"|"phone"){
 if(!v)return "—";
 if(channel==="email"){const at=v.lastIndexOf("@");return at>0?v[0]+"•••"+v.slice(at):"•••";}
 const digits=v.replace(/\D/g,"");return digits.length>4?"+•• ••• ••• "+digits.slice(-4,-2)+" "+digits.slice(-2):"•••";
}
export function challengeOpen(p:Contact["phone"]){return !!p.challengeId&&["ACCEPTED","UNKNOWN"].includes(p.deliveryStatus)&&!!p.expiresAt&&(p.attemptsRemaining??0)>0;}
export const emailAction=(status:ContactState)=>status==="UNVERIFIED"?"VERIFY_EMAIL":"UPDATE_EMAIL";
// Fixed callback excludes query/hash credentials and arbitrary return targets.
export const contactReturnUri=(origin:string)=>new URL("/account",origin).href;

export function phoneCommand(contact:Contact,operationId:string,action:"SEND"|"VERIFY"|"CANCEL",code=""){
 return {operationId,expectedVersion:contact.version,action,...(action!=="SEND"?{challengeId:contact.phone.challengeId}:{}),...(action==="VERIFY"?{code}:{})};
}
export function challengeExpired(p:Contact["phone"],now=Date.now()){return p.deliveryStatus==="EXPIRED"||challengeOpen(p)&&!!p.expiresAt&&Date.parse(p.expiresAt)<=now;}
