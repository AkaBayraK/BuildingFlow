import {uuid,dateOnly,portfolio,capacities,portfolioStatuses,type Capacity,type PortfolioProperty,type PortfolioStatus} from "../person-center/model.ts";
import {context,type WorkspaceContext} from "../session-workspaces/model.ts";
export const capabilities=["VIEW_DUES","VIEW_PAYMENTS","VIEW_ANNOUNCEMENTS","VIEW_DOCUMENTS","CREATE_REQUEST","ANSWER_SURVEY","VIEW_LEASE","VIEW_OWN_RELATIONS"] as const;
export type Capability=typeof capabilities[number];
export type ContextRef={capacity:Capacity;relationId:string;contextKey:string};
export type UnitAccess={tenantId:string;propertyId:string;unitId:string;status:PortfolioStatus;capabilities:Capability[];contexts:ContextRef[]};
export type PortfolioPerson={tenantId:string;tenantName:string;personId:string;displayName:string;properties:PortfolioProperty[]};
export type MyPortfolio={asOf:string;fingerprint:string;people:PortfolioPerson[];units:UnitAccess[];workspaces:WorkspaceContext[]};
export type Selection={tenantId:string;propertyId:string;unitId:string;capacity:Capacity;relationId:string;contextKey:string};
export type ActiveContext={context:WorkspaceContext;contextKey:string;capabilities:Capability[]};
export type Charge={id:string;period:string;amount:string;paid:string;balance:string;dueDate:string;status:string};
export type Summary={amount:string;paid:string;balance:string;overdue:string};
export type Rows<T>={items:T[];page:number;pageSize:number;totalCount:number};
export type Dues={contextKey:string;summary:Summary;rows:Rows<Charge>};
export type Payment={id:string;chargeId:string;period:string;amount:string;paidDate:string;cancelled:boolean};
export type Announcement={id:string;title:string;content:string;category:string;publishedAt:string};
export type Document={id:string;announcementId:string;fileName:string;contentType:string;size:number;createdAt:string};
export type Lease={id:string;startDate:string;endDate:string|null;contractReference:string|null;status:string;partyCount:number;isParty:boolean};
const fail=():never=>{throw new Error("Invalid portfolio response");};
const obj=(v:unknown):Record<string,unknown>=>v&&typeof v==="object"&&!Array.isArray(v)?v as Record<string,unknown>:fail();
const list=(v:unknown):unknown[]=>Array.isArray(v)?v:fail();
const text=(v:unknown):string=>typeof v==="string"&&v.trim()?v:fail();
const money=(v:unknown):string=>typeof v==="string"&&/^-?\d{1,12}\.\d{2}$/.test(v)?v:fail();
const stamp=(v:unknown):string=>typeof v==="string"&&Number.isFinite(Date.parse(v))?v:fail();
const int=(v:unknown,min=0):number=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min?v:fail();
const pick=<T extends string>(v:unknown,values:readonly T[]):T=>values.includes(v as T)?v as T:fail();
export const contextKey=(tenant:string,kind:string,id:string)=>`${tenant}:${kind}:${id}`;
export function myPortfolio(v:unknown):MyPortfolio{
 const r=obj(v);if(!dateOnly(r.asOf)||typeof r.fingerprint!=="string"||!/^[0-9a-f]{64}$/.test(r.fingerprint))fail();
 const people=list(r.people).map(pv=>{const p=obj(pv);if(!uuid(p.tenantId)||!uuid(p.personId))fail();return{tenantId:p.tenantId as string,tenantName:text(p.tenantName),personId:p.personId as string,displayName:text(p.displayName),properties:portfolio({tenantId:p.tenantId,personId:p.personId,asOf:r.asOf,properties:p.properties},p.tenantId as string,p.personId as string).properties};});
 const units=list(r.units).map(uv=>{const u=obj(uv);if(!uuid(u.tenantId)||!uuid(u.propertyId)||!uuid(u.unitId))fail();const contexts=list(u.contexts).map(cv=>{const c=obj(cv),capacity=pick(c.capacity,capacities);if(!uuid(c.relationId)||c.contextKey!==contextKey(u.tenantId as string,capacity,c.relationId as string))fail();return{capacity,relationId:c.relationId as string,contextKey:c.contextKey as string};});
  if(!contexts.length)fail();const caps=list(u.capabilities).map(c=>pick(c,capabilities));if(new Set(caps).size!==caps.length)fail();return{tenantId:u.tenantId as string,propertyId:u.propertyId as string,unitId:u.unitId as string,status:pick(u.status,portfolioStatuses),capabilities:caps,contexts};});
 if(new Set(units.map(u=>u.unitId)).size!==units.length)fail();
 const known=new Set(people.flatMap(p=>p.properties.flatMap(x=>x.buildings.flatMap(b=>b.units.map(u=>u.unitId)))));if(units.some(u=>!known.has(u.unitId))||known.size!==units.length)fail();
 const workspaces=list(r.workspaces).map(context);if(workspaces.some(w=>w.kind!=="MANAGEMENT"&&w.kind!=="PLATFORM"))fail();
 return{asOf:r.asOf as string,fingerprint:r.fingerprint as string,people,units,workspaces};
}
export function activeContext(v:unknown,s:Selection):ActiveContext{
 const r=obj(v),c=context(r.context);if(c.kind!==s.capacity||c.id!==s.relationId||c.unitId!==s.unitId||c.propertyId!==s.propertyId||c.tenantId!==s.tenantId||r.contextKey!==s.contextKey)fail();
 const caps=list(r.capabilities).map(x=>pick(x,capabilities));return{context:c,contextKey:s.contextKey,capabilities:caps};
}
function rows<T>(v:unknown,parse:(v:unknown)=>T&{id:string}):Rows<T>{const r=obj(v);const items=list(r.items).map(parse);const page=int(r.page,1),pageSize=int(r.pageSize,1),totalCount=int(r.totalCount);if(pageSize>50||items.length>pageSize||items.length>totalCount||new Set(items.map(i=>i.id)).size!==items.length)fail();return{items,page,pageSize,totalCount};}
// A response for a previous selection is rejected, so a fast unit switch can never show the other unit's data.
function scoped(v:unknown,key:string){const r=obj(v);if(r.contextKey!==key)fail();return r;}
const period=(v:unknown)=>typeof v==="string"&&/^\d{4}-\d{2}$/.test(v)?v:fail();
export function dues(v:unknown,key:string):Dues{const r=scoped(v,key),s=obj(r.summary);return{contextKey:key,summary:{amount:money(s.amount),paid:money(s.paid),balance:money(s.balance),overdue:money(s.overdue)},rows:rows(r.rows,cv=>{const c=obj(cv);if(!uuid(c.id)||!dateOnly(c.dueDate))fail();return{id:c.id as string,period:period(c.period),amount:money(c.amount),paid:money(c.paid),balance:money(c.balance),dueDate:c.dueDate as string,status:pick(c.status,["OPEN","PARTIALLY_PAID","PAID","OVERDUE","CANCELLED"])};})};}
export function payments(v:unknown,key:string):Rows<Payment>{return rows(scoped(v,key).rows,pv=>{const p=obj(pv);if(!uuid(p.id)||!uuid(p.chargeId)||!dateOnly(p.paidDate)||typeof p.cancelled!=="boolean")fail();return{id:p.id as string,chargeId:p.chargeId as string,period:period(p.period),amount:money(p.amount),paidDate:p.paidDate as string,cancelled:p.cancelled as boolean};});}
export function announcements(v:unknown,key:string):Rows<Announcement>{return rows(scoped(v,key).rows,av=>{const a=obj(av);if(!uuid(a.id))fail();return{id:a.id as string,title:text(a.title),content:typeof a.content==="string"?a.content:fail(),category:text(a.category),publishedAt:stamp(a.publishedAt)};});}
export function documents(v:unknown,key:string):Rows<Document>{return rows(scoped(v,key).rows,dv=>{const d=obj(dv);if(!uuid(d.id)||!uuid(d.announcementId))fail();return{id:d.id as string,announcementId:d.announcementId as string,fileName:text(d.fileName),contentType:pick(d.contentType,["application/pdf","image/jpeg","image/png"]),size:int(d.size,1),createdAt:stamp(d.createdAt)};});}
export function leases(v:unknown,key:string):Rows<Lease>{return rows(scoped(v,key).rows,lv=>{const l=obj(lv);if(!uuid(l.id)||!dateOnly(l.startDate)||l.endDate!==null&&(!dateOnly(l.endDate)||(l.endDate as string)<(l.startDate as string))||l.contractReference!==null&&typeof l.contractReference!=="string"||typeof l.isParty!=="boolean")fail();for(const k of ["displayName","personId","parties"])if(k in l)fail();return{id:l.id as string,startDate:l.startDate as string,endDate:l.endDate as string|null,contractReference:l.contractReference as string|null,status:pick(l.status,["ACTIVE","UPCOMING","ENDED","DISABLED"]),partyCount:int(l.partyCount),isParty:l.isParty as boolean};});}
export const unitPath=(s:Selection,section:string,page=1)=>`/api/v2/me/units/${encodeURIComponent(s.unitId)}/${section}?${new URLSearchParams({propertyId:s.propertyId,capacity:s.capacity,relationId:s.relationId,page:String(page),pageSize:"20"})}`;
export const activePath=(s:Selection)=>`/api/v2/me/active-context?${new URLSearchParams({propertyId:s.propertyId,unitId:s.unitId,capacity:s.capacity,relationId:s.relationId})}`;
export const sectionsFor=(caps:readonly Capability[])=>(["dues","payments","announcements","documents","lease","requests","relations"] as const).filter(s=>caps.includes(({dues:"VIEW_DUES",payments:"VIEW_PAYMENTS",announcements:"VIEW_ANNOUNCEMENTS",documents:"VIEW_DOCUMENTS",lease:"VIEW_LEASE",requests:"CREATE_REQUEST",relations:"VIEW_OWN_RELATIONS"} as const)[s]));
export type RequestRow={id:string;description:string;category:string;priority:string;status:string;modifiedAt:string;version:number};
export type FinanceSummary={asOf:string;scope:{tenantId:string;propertyId:string;propertyName:string;unitId:string;unitNumber:string;capacity:Capacity}[];chargeCount:number;sharedCharges:number;summary:Summary};
const requestCategories=["ELEVATOR","WATER","ELECTRICITY","HEATING","RENOVATION","FAULT","CLEANING","SECURITY","COMMON_AREA","SUGGESTION","OTHER"] as const;
// Requests reuse the canonical mobile sync contract; the snapshot key must equal the selected context.
export function requestRows(v:unknown,s:Selection):RequestRow[]{const r=obj(v);if(r.contextKey!==s.contextKey||typeof r.hasMore!=="boolean")fail();return list(r.records).map(xv=>{const x=obj(xv);if(!uuid(x.id)||x.propertyId!==s.propertyId||x.unitId!==s.unitId||typeof x.deleted!=="boolean")fail();return{x,deleted:x.deleted as boolean};}).filter(e=>!e.deleted).map(({x})=>({id:x.id as string,description:typeof x.description==="string"?x.description:fail(),category:pick(x.category,requestCategories),priority:pick(x.priority,["LOW","NORMAL","HIGH"]),status:text(x.managementStatus),modifiedAt:stamp(x.modifiedAt),version:int(x.version,1)}));}
export function requestCreated(v:unknown,s:Selection,recordId:string){const r=obj(v),x=obj(r.record);if(x.id!==recordId||x.propertyId!==s.propertyId||x.unitId!==s.unitId)fail();return{version:int(x.version,1)};}
export const requestsPath=(s:Selection)=>`/api/v1/mobile/sync/requests?${new URLSearchParams({propertyId:s.propertyId,unitId:s.unitId,role:s.capacity,contextKind:s.capacity,contextId:s.relationId})}`;
export const downloadPath=(s:Selection,id:string)=>`/api/v2/me/units/${encodeURIComponent(s.unitId)}/documents/${encodeURIComponent(id)}/download?${new URLSearchParams({propertyId:s.propertyId,capacity:s.capacity,relationId:s.relationId})}`;
export function financeSummary(v:unknown):FinanceSummary{const r=obj(v),s=obj(r.summary);if(!dateOnly(r.asOf))fail();const scope=list(r.scope).map(xv=>{const x=obj(xv);if(!uuid(x.tenantId)||!uuid(x.propertyId)||!uuid(x.unitId))fail();return{tenantId:x.tenantId as string,propertyId:x.propertyId as string,propertyName:text(x.propertyName),unitId:x.unitId as string,unitNumber:text(x.unitNumber),capacity:pick(x.capacity,["OWNER","TENANT"] as const)};});return{asOf:r.asOf as string,scope,chargeCount:int(r.chargeCount),sharedCharges:int(r.sharedCharges),summary:{amount:money(s.amount),paid:money(s.paid),balance:money(s.balance),overdue:money(s.overdue)}};}
