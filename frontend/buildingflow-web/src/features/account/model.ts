// İŞ19 "Hesap bilgilerim": shapes of GET /api/v1/account, the profile command and the account consent list.
// Unknown or foreign data is refused, as in the mobile client.
import {parseMarkup} from "../consents/markup.ts";
export type AccountContext={propertyId:string;propertyName:string;unitId:string|null;unitNumber:string|null;role:string};
export type AccountProfile={userId:string;firstName:string;lastName:string;phone:string|null;photoId:string|null;photoSha256:string|null;version:number;modifiedAt:string|null};
export type Account={userId:string;username:string|null;email:string|null;roles:string[];contexts:AccountContext[];hasMore:boolean;profile:AccountProfile};
export type ConsentState="NONE"|"ACTIVE"|"OUTDATED"|"WITHDRAWN";
export type AccountConsentText={id:string;textVersion:number;title:string;body:string;bodySha256:string;publishedAt:string};
export type AccountConsent={typeCode:string;nameTr:string;nameEn:string;kind:"INFORMATION"|"EXPLICIT_CONSENT";feature:string|null;state:ConsentState;approvedVersion:number|null;approvedAt:string|null;text:AccountConsentText|null};
export const appRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT","RESIDENT"];
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const text=(v:unknown,max=300)=>typeof v==="string"&&v.length<=max&&!/[\u0000-\u001f\u007f]/.test(v);
const optional=(v:unknown,max=300)=>v===null||text(v,max);
const date=(v:unknown)=>typeof v==="string"&&!Number.isNaN(Date.parse(v));
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
function fail(what:string):never{throw new Error("Invalid "+what);}
export function parseProfile(v:unknown,userId:string):AccountProfile{
 if(!obj(v)||v.userId!==userId||!text(v.firstName,80)||!text(v.lastName,80)||!optional(v.phone,32)||!(v.photoId===null||guid(v.photoId))||!(v.photoSha256===null||typeof v.photoSha256==="string"&&/^[a-f0-9]{64}$/i.test(v.photoSha256))||(v.photoId===null)!==(v.photoSha256===null)||!int(v.version)||!(v.modifiedAt===null||date(v.modifiedAt)))fail("profile");
 return {userId,firstName:String(v.firstName),lastName:String(v.lastName),phone:v.phone as string|null,photoId:v.photoId as string|null,photoSha256:v.photoSha256 as string|null,version:Number(v.version),modifiedAt:v.modifiedAt as string|null};
}
export function parseAccount(v:unknown,userId:string):Account{
 if(!obj(v)||v.userId!==userId||!optional(v.username,200)||!optional(v.email,254)||!Array.isArray(v.roles)||!v.roles.every(r=>appRoles.includes(String(r)))||!Array.isArray(v.contexts)||v.contexts.length>500||typeof v.hasMore!=="boolean")fail("account");
 const contexts=v.contexts.map(c=>{if(!obj(c)||!guid(c.propertyId)||!text(c.propertyName,200)||!(c.unitId===null||guid(c.unitId))||!optional(c.unitNumber,50)||!text(c.role,40))fail("account context");
  return {propertyId:c.propertyId,propertyName:String(c.propertyName),unitId:c.unitId as string|null,unitNumber:c.unitNumber as string|null,role:String(c.role)};});
 return {userId,username:v.username as string|null,email:v.email as string|null,roles:v.roles.map(String),contexts,hasMore:v.hasMore,profile:parseProfile(v.profile,userId)};
}
export function parseConsents(v:unknown,userId:string):AccountConsent[]{
 if(!obj(v)||v.userId!==userId||!Array.isArray(v.items)||v.items.length>50)fail("consent list");
 return v.items.map(raw=>{
  if(!obj(raw)||!obj(raw.status))fail("consent");const s=raw.status,t=raw.currentText;
  if(typeof s.typeCode!=="string"||!/^[A-Z][A-Z0-9_]{1,63}$/.test(s.typeCode)||!text(s.nameTr,120)||!text(s.nameEn,120)||!["INFORMATION","EXPLICIT_CONSENT"].includes(String(s.kind))||!["NONE","ACTIVE","OUTDATED","WITHDRAWN"].includes(String(s.state))||!(s.approvedVersion===null||int(s.approvedVersion,1))||!(s.approvedAt===null||date(s.approvedAt)))fail("consent status");
  let current:AccountConsentText|null=null;
  if(t!==null){if(!obj(t)||!guid(t.id)||!int(t.textVersion,1)||!text(t.title,200)||typeof t.body!=="string"||parseMarkup(t.body).error||typeof t.bodySha256!=="string"||!/^[a-f0-9]{64}$/.test(t.bodySha256)||!date(t.publishedAt)||t.id!==s.currentTextId)fail("consent text");
   current={id:t.id,textVersion:Number(t.textVersion),title:String(t.title),body:t.body,bodySha256:t.bodySha256,publishedAt:String(t.publishedAt)};}
  return {typeCode:s.typeCode,nameTr:String(s.nameTr),nameEn:String(s.nameEn),kind:s.kind as AccountConsent["kind"],feature:typeof s.feature==="string"?s.feature:null,state:s.state as ConsentState,approvedVersion:s.approvedVersion as number|null,approvedAt:s.approvedAt as string|null,text:current};
 });
}
// An approval is accepted only for the exact text the person read: the client checks the body hash before offering it.
export async function bodyMatches(text:AccountConsentText,digest:(value:string)=>Promise<string>){return await digest(text.body)===text.bodySha256;}
export const allows=(c:AccountConsent|undefined)=>c?.state==="ACTIVE";
export function approveCommand(c:AccountConsent,operationId:string){if(!c.text)fail("consent text");return {operationId,action:"APPROVE",typeCode:c.typeCode,textId:c.text.id,bodySha256:c.text.bodySha256,appVersion:"web"};}
export const withdrawCommand=(c:AccountConsent,operationId:string)=>({operationId,action:"WITHDRAW",typeCode:c.typeCode});
export type PhotoAction="KEEP"|"REPLACE"|"REMOVE";
export const profileCommand=(p:AccountProfile,operationId:string,values:{firstName:string;lastName:string;phone:string},photoAction:PhotoAction,photoBase64:string|null=null)=>({operationId,expectedVersion:p.version,firstName:values.firstName,lastName:values.lastName,phone:values.phone.trim()||null,photoAction,photoBase64:photoAction==="REPLACE"?photoBase64:null});
export const accountName=(p:AccountProfile|null|undefined,fallback:string)=>p&&(p.firstName||p.lastName)?[p.firstName,p.lastName].filter(Boolean).join(" "):fallback;
export const initials=(name:string)=>name.split(/\s+/).filter(Boolean).slice(0,2).map(v=>v[0]).join("").toLocaleUpperCase("tr-TR")||"?";

// A stale hook result must never carry the previous account's profile into the shared shell.
export function accountProfileForSubject(account:Account|null,subject:string|undefined):AccountProfile|null{
 return subject&&account?.userId===subject&&account.profile?.userId===subject?account.profile:null;
}
