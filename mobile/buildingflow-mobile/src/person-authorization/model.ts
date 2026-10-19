import type {Role} from '../auth/contracts.ts';
export type CanonicalContext={id:string;kind:'PLATFORM'|'MANAGEMENT'|'OWNER'|'TENANT'|'RESIDENT';tenantId:string;tenantName:string;propertyId:string;propertyName:string;unitId:string|null;unitNumber:string|null;role:Role;startDate:string|null;endDate:string|null;canManagePeople:boolean;canManageFinance:boolean;canManageAssignments:boolean};
export type PersonIdentity={id:string;tenantId:string;displayName:string};
export type Authorization={policyVersion:1;snapshotId:string;userId:string;issuedAt:string;expiresAt:string;offlineReadOnly:true;people:PersonIdentity[];contexts:CanonicalContext[]};
export const MAX_OFFLINE_MS=24*60*60*1000;
const bad=():never=>{throw Error('Yetki kopyası doğrulanamadı.');};
const obj=(v:unknown):Record<string,unknown>=>v&&typeof v==='object'&&!Array.isArray(v)?v as Record<string,unknown>:bad();
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v)&&v!=='00000000-0000-0000-0000-000000000000';
const text=(v:unknown):string=>typeof v==='string'&&!!v.trim()&&v.length<=500&&!/[\u0000-\u001f]/.test(v)?v:bad();
const date=(v:unknown):v is string=>typeof v==='string'&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&Number.isFinite(Date.parse(v+'T00:00:00Z'))&&new Date(v+'T00:00:00Z').toISOString().slice(0,10)===v;
export function parseAuthorization(value:unknown):Authorization{
 const r=obj(value);if(r.policyVersion!==1||r.offlineReadOnly!==true||!guid(r.snapshotId)||!Array.isArray(r.people)||!Array.isArray(r.contexts)||r.contexts.length>500||r.people.length>500)bad();
 const userId=text(r.userId),issuedAt=text(r.issuedAt),expiresAt=text(r.expiresAt);const duration=Date.parse(expiresAt)-Date.parse(issuedAt);if(!Number.isFinite(duration)||duration<=0||duration>MAX_OFFLINE_MS)bad();
 const contexts=(r.contexts as unknown[]).map(v=>{const c=obj(v);if(!guid(c.id)||!guid(c.tenantId)||!guid(c.propertyId)||!['PLATFORM','MANAGEMENT','OWNER','TENANT','RESIDENT'].includes(String(c.kind)))bad();
  const member=['OWNER','TENANT','RESIDENT'].includes(String(c.kind)),platform=c.kind==='PLATFORM';
  if(member?(!guid(c.unitId)||c.role!==c.kind):(c.unitId!==null||c.unitNumber!==null||!(platform?c.role==='SUPER_ADMIN':['SITE_ADMIN','MANAGER','ACCOUNTANT'].includes(String(c.role)))))bad();
  if(platform?(c.id!==c.propertyId||c.startDate!==null||c.endDate!==null):(!date(c.startDate)||c.endDate!==null&&(!date(c.endDate)||c.endDate<c.startDate)))bad();
  if(c.canManagePeople!==(!member&&c.role!=='ACCOUNTANT')||c.canManageFinance!==!member||c.canManageAssignments!==(!member&&['SUPER_ADMIN','SITE_ADMIN'].includes(String(c.role))))bad();
  return {id:c.id,kind:c.kind,tenantId:c.tenantId,tenantName:text(c.tenantName),propertyId:c.propertyId,propertyName:text(c.propertyName),unitId:c.unitId,unitNumber:c.unitNumber===null?null:text(c.unitNumber),role:c.role,startDate:c.startDate,endDate:c.endDate,canManagePeople:c.canManagePeople,canManageFinance:c.canManageFinance,canManageAssignments:c.canManageAssignments} as CanonicalContext;
 });
 if((contexts.length===0||contexts.some(c=>c.canManageFinance))&&duration>60*60*1000)bad();
 if(new Set(contexts.map(c=>c.kind+':'+c.id)).size!==contexts.length)bad();
 const people=(r.people as unknown[]).map(v=>{const p=obj(v);if(!guid(p.id)||!guid(p.tenantId)||!contexts.some(c=>c.tenantId===p.tenantId))bad();return{id:p.id,tenantId:p.tenantId,displayName:text(p.displayName)} as PersonIdentity;});
 if(new Set(people.map(p=>p.tenantId)).size!==people.length)bad();
 for(const c of contexts)if(c.kind!=='PLATFORM'&&!people.some(p=>p.tenantId===c.tenantId))bad();
 return{policyVersion:1,snapshotId:r.snapshotId as string,userId,issuedAt,expiresAt,offlineReadOnly:true,people,contexts};
}
export function readableOffline(value:Authorization,now:number,lastSeen:number){return Number.isSafeInteger(now)&&Number.isSafeInteger(lastSeen)&&now>=lastSeen&&now>=Date.parse(value.issuedAt)-60000&&now<Date.parse(value.expiresAt);}
