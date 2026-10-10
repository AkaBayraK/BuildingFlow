export type Audience="ALL_MEMBERS"|"MANAGEMENT";
export type Announcement={id:string;propertyId:string;title:string;content:string;audience:Audience;status:"DRAFT"|"PUBLISHED"|"ARCHIVED";version:number;createdAt:string;publishedAt:string|null};
export type DocumentRecord={id:string;propertyId:string;requestId:string;announcementId:string|null;audience:Audience;fileName:string;contentType:string;size:number;sha256:string;version:number;createdAt:string;replayed:boolean};
export type ContentPage<T>={items:T[];page:number;pageSize:number;totalCount:number};
export const maxFileBytes=10*1024*1024;
const object=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown):v is string=>typeof v==="string";
const guid=(v:unknown):v is string=>text(v)&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const positive=(v:unknown)=>typeof v==="number"&&Number.isSafeInteger(v)&&v>0;
const timestamp=(v:unknown)=>text(v)&&Number.isFinite(Date.parse(v));
const audience=(v:unknown)=>v==="ALL_MEMBERS"||v==="MANAGEMENT";
export function readAnnouncement(v:unknown,property:string):Announcement{
 if(!object(v)||!guid(v.id)||v.propertyId!==property||!text(v.title)||!v.title.trim()||v.title.length>200||!text(v.content)||!v.content.trim()||v.content.length>10000||!audience(v.audience)||!positive(v.version)||!timestamp(v.createdAt)||!["DRAFT","PUBLISHED","ARCHIVED"].includes(String(v.status))||(v.status==="DRAFT"?v.publishedAt!==null:!timestamp(v.publishedAt)))throw new Error("Invalid announcement");
 return v as Announcement;
}
export function readDocument(v:unknown,property:string,announcement?:string):DocumentRecord{
 if(!object(v)||!guid(v.id)||!guid(v.requestId)||v.propertyId!==property||!(v.announcementId===null||guid(v.announcementId))||(announcement!==undefined&&v.announcementId!==announcement)||!audience(v.audience)||!text(v.fileName)||!v.fileName.trim()||v.fileName.length>200||/[\\/\x00-\x1f\x7f]/.test(v.fileName)||!positive(v.size)||Number(v.size)>maxFileBytes||!text(v.contentType)||!["application/pdf","image/jpeg","image/png"].includes(v.contentType)||!text(v.sha256)||! /^[0-9A-F]{64}$/.test(v.sha256)||!positive(v.version)||!timestamp(v.createdAt)||typeof v.replayed!=="boolean")throw new Error("Invalid document");
 return v as DocumentRecord;
}
export function readContentPage<T extends {id:string}>(v:unknown,read:(row:unknown)=>T):ContentPage<T>{
 if(!object(v)||!Array.isArray(v.items)||!positive(v.page)||!positive(v.pageSize)||Number(v.pageSize)>100||typeof v.totalCount!=="number"||!Number.isSafeInteger(v.totalCount)||v.totalCount<0||v.items.length>Number(v.pageSize)||v.items.length>v.totalCount)throw new Error("Invalid page");
 const items=v.items.map(read);if(new Set(items.map(i=>i.id)).size!==items.length)throw new Error("Duplicate rows");return {items,page:v.page as number,pageSize:v.pageSize as number,totalCount:v.totalCount};
}
export function validFile(file:{name:string;size:number;type:string}):boolean{
 const extension=file.name.split('.').pop()?.toLowerCase();return !!file.name.trim()&&file.name.length<=200&&!/[\\/\x00-\x1f\x7f]/.test(file.name)&&Number.isSafeInteger(file.size)&&file.size>0&&file.size<=maxFileBytes&&(file.type==="application/pdf"?extension==="pdf":file.type==="image/png"?extension==="png":file.type==="image/jpeg"&&(extension==="jpg"||extension==="jpeg"));
}
export async function verifyDownload(blob:Blob,file:DocumentRecord):Promise<boolean>{
 if(blob.size!==file.size||blob.size>maxFileBytes||blob.type!==file.contentType)return false;
 const digest=await crypto.subtle.digest("SHA-256",await blob.arrayBuffer());return Array.from(new Uint8Array(digest),b=>b.toString(16).padStart(2,"0")).join("").toUpperCase()===file.sha256;
}
