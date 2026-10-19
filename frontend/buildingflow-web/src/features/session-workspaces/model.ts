import {uuid,dateOnly} from "../person-center/model.ts";
export const kinds=["PLATFORM","MANAGEMENT","OWNER","TENANT","RESIDENT"] as const;
export type ContextKind=typeof kinds[number];
export type WorkspaceContext={id:string;kind:ContextKind;tenantId:string;tenantName:string;propertyId:string;propertyName:string;unitId:string|null;unitNumber:string|null;role:string;startDate:string|null;endDate:string|null;canManagePeople:boolean;canManageFinance:boolean;canManageAssignments:boolean};
export type ContextPage={items:WorkspaceContext[];page:number;pageSize:number;totalCount:number};
export type WorkspaceDetail={context:WorkspaceContext;self:{id:string;tenantId:string;displayName:string;kind:"NATURAL";maskedIdentity:string;canEdit:false}|null};
const fail=():never=>{throw new Error("Invalid workspace response");};
const object=(v:unknown):Record<string,unknown>=>v&&typeof v==="object"&&!Array.isArray(v)?v as Record<string,unknown>:fail();
const text=(v:unknown):v is string=>typeof v==="string"&&!!v.trim();
export const validKind=(v:unknown):v is ContextKind=>typeof v==="string"&&kinds.includes(v as ContextKind);
export function context(value:unknown):WorkspaceContext{
 const r=object(value);if(!uuid(r.id)||!uuid(r.tenantId)||!uuid(r.propertyId)||!validKind(r.kind)||!text(r.tenantName)||!text(r.propertyName))fail();
 const member=["OWNER","TENANT","RESIDENT"].includes(r.kind as string),platform=r.kind==="PLATFORM";
 if(member?(!uuid(r.unitId)||!text(r.unitNumber)||r.role!==r.kind):(r.unitId!==null||r.unitNumber!==null||!(platform?r.role==="SUPER_ADMIN":["SITE_ADMIN","MANAGER","ACCOUNTANT"].includes(String(r.role)))))fail();
 if(platform?(r.id!==r.propertyId||r.startDate!==null||r.endDate!==null):(!dateOnly(r.startDate)||r.endDate!==null&&(!dateOnly(r.endDate)||r.endDate<r.startDate)))fail();
 for(const key of ["canManagePeople","canManageFinance","canManageAssignments"])if(typeof r[key]!=="boolean")fail();
 if(r.canManagePeople!==(!member&&r.role!=="ACCOUNTANT")||r.canManageFinance!==!member||r.canManageAssignments!==(!member&&["SUPER_ADMIN","SITE_ADMIN"].includes(String(r.role))))fail();
 return r as WorkspaceContext;
}
export function contextPage(value:unknown):ContextPage{
 const r=object(value);for(const key of ["page","pageSize","totalCount"])if(typeof r[key]!=="number"||!Number.isSafeInteger(r[key])||(r[key] as number)<(key==="totalCount"?0:1))fail();
 if(!Array.isArray(r.items)||(r.pageSize as number)>100)fail();const items=(r.items as unknown[]).map(context);if(items.length>(r.pageSize as number)||items.length>(r.totalCount as number)||new Set(items.map(c=>c.kind+":"+c.id)).size!==items.length)fail();return{items,page:r.page as number,pageSize:r.pageSize as number,totalCount:r.totalCount as number};
}
export function workspacePath(c:Pick<WorkspaceContext,"kind"|"id">){if(!validKind(c.kind)||!uuid(c.id))fail();return `/workspaces/${c.kind}/${c.id}`;}
export function contextDetail(value:unknown,kind:string,id:string):WorkspaceDetail{
 const r=object(value),c=context(r.context);if(c.kind!==kind||c.id!==id)fail();if(r.self===null){if(c.kind!=="PLATFORM")fail();return{context:c,self:null};}const s=object(r.self);
 if(!uuid(s.id)||s.tenantId!==c.tenantId||s.kind!=="NATURAL"||!text(s.displayName)||typeof s.maskedIdentity!=="string"||!/^\*+.{0,4}$/.test(s.maskedIdentity)||s.canEdit!==false)fail();
 return{context:c,self:{id:s.id as string,tenantId:s.tenantId as string,kind:"NATURAL",displayName:s.displayName as string,maskedIdentity:s.maskedIdentity as string,canEdit:false}};
}
export function soleWorkspace(page:ContextPage){return page.page===1&&page.totalCount===1&&page.items.length===1?page.items[0]:null;}

export function websiteContexts(value:unknown,audience:string):ContextPage{
 if(!['all','management','resident'].includes(audience))fail();
 const page=contextPage(value),allowed=audience==='all'?kinds:audience==='management'?['PLATFORM','MANAGEMENT']:['OWNER','TENANT','RESIDENT'];
 if(page.items.some(c=>!allowed.includes(c.kind))||new Set(page.items.map(c=>c.propertyId)).size>1)fail();
 return page;
}
