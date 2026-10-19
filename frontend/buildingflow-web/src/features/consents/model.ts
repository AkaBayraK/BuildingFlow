export type ConsentKind="INFORMATION"|"EXPLICIT_CONSENT";
export type ConsentRowState="ACTIVE"|"OUTDATED"|"WITHDRAWN"|"SUPERSEDED";
export type ConsentSummaryState=ConsentRowState|"NONE";
export type ConsentTextSummary={id:string;textVersion:number;title:string;requiresReconsent:boolean;publishedAt:string;publishedBy:string};
export type ConsentType={id:string;code:string;nameTr:string;nameEn:string;kind:ConsentKind;isSystem:boolean;feature:string|null;sortOrder:number;isEnabled:boolean;version:number;textCount:number;currentText:ConsentTextSummary|null};
export type ConsentText={id:string;consentTypeId:string;typeCode:string;textVersion:number;title:string;body:string;bodySha256:string;requiresReconsent:boolean;publishedAt:string;publishedBy:string};
export type ConsentRecord={id:string;ownerUserId:string;approverName:string;typeId:string;typeCode:string;typeNameTr:string;typeNameEn:string;kind:ConsentKind;textId:string;textVersion:number;approvedAt:string;channel:"MOBILE"|"WEB";appVersion:string|null;revokedAt:string|null;revokeReason:"WITHDRAWN"|"SUPERSEDED"|null;state:ConsentRowState};
export type ConsentDocument={record:ConsentRecord;title:string;body:string;bodySha256:string;evidenceSha256:string;bodyIntact:boolean;evidenceIntact:boolean};
export type ConsentTypeState={typeId:string;code:string;nameTr:string;nameEn:string;kind:ConsentKind;state:ConsentSummaryState;approvedVersion:number|null;approvedAt:string|null;currentVersion:number|null};
export type ConsentHistory={userId:string;summary:ConsentTypeState[];records:ConsentRecord[]};
export type Page<T>={items:T[];page:number;pageSize:number;totalCount:number};

export const consentKinds:ConsentKind[]=["INFORMATION","EXPLICIT_CONSENT"];
export const consentRowStates:ConsentRowState[]=["ACTIVE","OUTDATED","WITHDRAWN","SUPERSEDED"];
export const consentCodePattern=/^[A-Z][A-Z0-9_]{1,39}$/;

const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const text=(v:unknown,nullable=false)=>typeof v==="string"||nullable&&v===null;
const date=(v:unknown)=>typeof v==="string"&&!Number.isNaN(Date.parse(v));
const sha=(v:unknown)=>typeof v==="string"&&/^[0-9a-f]{64}$/.test(v);
const kind=(v:unknown)=>consentKinds.includes(v as ConsentKind);
function fail(name:string):never{throw new Error(`Invalid consent ${name}`);}

function readSummary(v:unknown):ConsentTextSummary{
 if(!obj(v)||!guid(v.id)||!int(v.textVersion,1)||!text(v.title)||typeof v.requiresReconsent!=="boolean"||!date(v.publishedAt)||!text(v.publishedBy))fail("text summary");
 return v as ConsentTextSummary;
}
export function readType(v:unknown):ConsentType{
 if(!obj(v)||!guid(v.id)||typeof v.code!=="string"||!consentCodePattern.test(v.code)||!text(v.nameTr)||!text(v.nameEn)||!kind(v.kind)||typeof v.isSystem!=="boolean"||!text(v.feature,true)||(v.feature!==null&&!v.isSystem)||!int(v.sortOrder)||typeof v.isEnabled!=="boolean"||!int(v.version,1)||!int(v.textCount)||(v.textCount===0)!==(v.currentText===null))fail("type");
 return {...(v as ConsentType),currentText:v.currentText===null?null:readSummary(v.currentText)};
}
export function readTypes(v:unknown):ConsentType[]{if(!Array.isArray(v))fail("types");return v.map(readType);}
export function readText(v:unknown,typeId?:string):ConsentText{
 if(!obj(v)||!guid(v.id)||!guid(v.consentTypeId)||(typeId!==undefined&&v.consentTypeId!==typeId)||!text(v.typeCode)||!int(v.textVersion,1)||!text(v.title)||!text(v.body)||!sha(v.bodySha256)||typeof v.requiresReconsent!=="boolean"||!date(v.publishedAt)||!text(v.publishedBy))fail("text");
 return v as ConsentText;
}
export function readRecord(v:unknown):ConsentRecord{
 if(!obj(v)||!guid(v.id)||!text(v.ownerUserId)||!text(v.approverName)||!guid(v.typeId)||!text(v.typeCode)||!text(v.typeNameTr)||!text(v.typeNameEn)||!kind(v.kind)||!guid(v.textId)||!int(v.textVersion,1)||!date(v.approvedAt)||!["MOBILE","WEB"].includes(String(v.channel))||!text(v.appVersion,true)
  ||!(v.revokedAt===null||date(v.revokedAt))||!(v.revokeReason===null||v.revokeReason==="WITHDRAWN"||v.revokeReason==="SUPERSEDED")||(v.revokedAt===null)!==(v.revokeReason===null)||!consentRowStates.includes(v.state as ConsentRowState)
  ||(v.revokeReason!==null&&v.state!==v.revokeReason)||(v.revokeReason===null&&(v.state==="WITHDRAWN"||v.state==="SUPERSEDED")))fail("record");
 return v as ConsentRecord;
}
export function readDocument(v:unknown,recordId:string):ConsentDocument{
 if(!obj(v)||!text(v.title)||!text(v.body)||!sha(v.bodySha256)||!sha(v.evidenceSha256)||typeof v.bodyIntact!=="boolean"||typeof v.evidenceIntact!=="boolean")fail("document");
 const record=readRecord(v.record);if(record.id!==recordId)fail("document");
 return {...(v as ConsentDocument),record};
}
function readTypeState(v:unknown):ConsentTypeState{
 if(!obj(v)||!guid(v.typeId)||!text(v.code)||!text(v.nameTr)||!text(v.nameEn)||!kind(v.kind)||!(v.state==="NONE"||consentRowStates.includes(v.state as ConsentRowState))||!(v.approvedVersion===null||int(v.approvedVersion,1))||!(v.approvedAt===null||date(v.approvedAt))||!(v.currentVersion===null||int(v.currentVersion,1))||(v.approvedAt===null)!==(v.state==="NONE"||v.state==="WITHDRAWN"))fail("summary");
 return v as ConsentTypeState;
}
export function readHistory(v:unknown,userId:string):ConsentHistory{
 if(!obj(v)||v.userId!==userId||!Array.isArray(v.summary)||!Array.isArray(v.records))fail("history");
 const records=v.records.map(readRecord);if(records.some(r=>r.ownerUserId!==userId))fail("history");
 return {userId,summary:v.summary.map(readTypeState),records};
}
export function readPage<T>(v:unknown,read:(row:unknown)=>T):Page<T>{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize))fail("page");
 return {items:v.items.map(read),page:Number(v.page),pageSize:Number(v.pageSize),totalCount:Number(v.totalCount)};
}
export const typeName=(value:{nameTr:string;nameEn:string},locale:string)=>locale==="en"?value.nameEn:value.nameTr;
export const recordTypeName=(value:ConsentRecord,locale:string)=>locale==="en"?value.typeNameEn:value.typeNameTr;
