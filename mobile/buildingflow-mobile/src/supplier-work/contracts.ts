import {contextKey,type Context} from '../auth/contracts.ts';
export const workStatuses=['DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED'] as const;
export type WorkStatus=typeof workStatuses[number];
export const statusLabels:Record<WorkStatus,string>={DRAFT:'Taslak',ASSIGNED:'Atandı',ACCEPTED:'Kabul edildi',IN_PROGRESS:'Devam ediyor',COMPLETED:'Tamamlandı',VERIFIED:'Doğrulandı',CLOSED:'Kapatıldı',REJECTED:'Reddedildi',CANCELLED:'İptal edildi'};
// Ionicons outline names; one icon per status, the label carries the meaning.
export const statusIcons:Record<WorkStatus,string>={DRAFT:'document-outline',ASSIGNED:'person-add-outline',ACCEPTED:'checkmark-circle-outline',IN_PROGRESS:'play-circle-outline',COMPLETED:'checkmark-done-outline',VERIFIED:'shield-checkmark-outline',CLOSED:'lock-closed-outline',REJECTED:'close-circle-outline',CANCELLED:'ban-outline'};
const transitions:Record<WorkStatus,WorkStatus[]>={DRAFT:['ASSIGNED','CANCELLED'],ASSIGNED:['ACCEPTED','REJECTED','CANCELLED'],ACCEPTED:['IN_PROGRESS','CANCELLED'],IN_PROGRESS:['COMPLETED','CANCELLED'],COMPLETED:['VERIFIED','CANCELLED'],VERIFIED:['CLOSED'],CLOSED:[],REJECTED:[],CANCELLED:[]};
export const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v)&&v!=='00000000-0000-0000-0000-000000000000';
const text=(v:unknown,max:number):v is string=>typeof v==='string'&&!!v.trim()&&v.length<=max&&!/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(v);
const fail=():never=>{throw Error('Tedarikçi iş emri yanıtı doğrulanamadı.');};
export type WorkOrder={id:string;supplierId:string;supplierName:string;propertyId:string;serviceName:string;title:string;description:string;status:WorkStatus;dueAt:string|null;isOverdue:boolean;photoCount:number;version:number;modifiedAt:string};
export type Snapshot={userId:string;contextKey:string;page:number;total:number;asOf:string;items:WorkOrder[]};
// Only canonical management contexts carry the tenant the supplier API is scoped to.
export function permitted(c:Context|null|undefined):c is Context&{canonical:NonNullable<Context['canonical']>}{return !!c&&!!c.canonical&&c.canonical.kind==='MANAGEMENT'&&['SUPER_ADMIN','SITE_ADMIN','MANAGER'].includes(c.role)&&c.unitId===null&&guid(c.canonical.tenantId)&&guid(c.propertyId);}
export function canFinalize(c:Context){return c.role==='SUPER_ADMIN'||c.role==='SITE_ADMIN';}
export function nextStatuses(c:Context,status:WorkStatus):WorkStatus[]{return transitions[status].filter(s=>canFinalize(c)||!['VERIFIED','CLOSED'].includes(s));}
export function listPath(c:Context,page:number){if(!permitted(c)||!Number.isInteger(page)||page<1||page>10000)throw Error('Yetkili yönetim bağlamı gerekli.');return `/api/v2/tenants/${c.canonical.tenantId}/supplier-work-orders?propertyId=${c.propertyId}&page=${page}&pageSize=20`;}
const orderRoot=(c:Context&{canonical:NonNullable<Context['canonical']>},supplier:string,order:string)=>`/api/v2/tenants/${c.canonical.tenantId}/suppliers/${supplier}/work-orders/${order}`;
export function commandPath(c:Context,command:Command){if(!permitted(c))throw Error('Yetkili yönetim bağlamı gerekli.');return orderRoot(c,command.supplierId,command.workOrderId)+(command.kind==='TRANSITION'?'/transition':command.kind==='NOTE'?'/notes':'/photos');}
export function authorizePath(c:Context,url:string,method:string){
 if(!permitted(c))throw Error('Tedarikçi işlemine izin verilmedi.');
 const page=Number(new URL(url,'https://scope.invalid').searchParams.get('page'));
 if(method==='GET'&&Number.isInteger(page)&&url===listPath(c,page))return;
 const m=/^\/api\/v2\/tenants\/([0-9a-f-]{36})\/suppliers\/([0-9a-f-]{36})\/work-orders\/([0-9a-f-]{36})\/(transition|notes|photos)$/i.exec(url);
 if(!m||m[1]!==c.canonical.tenantId||!guid(m[2])||!guid(m[3])||(m[4]==='transition'?method!=='PUT':method!=='POST'))throw Error('Tedarikçi işlemine izin verilmedi.');
}
function parseOrder(v:unknown,c:Context):WorkOrder{
 if(!v||typeof v!=='object')fail();const row=v as {record:Record<string,unknown>;supplierName:unknown};const r=row.record;
 if(!r||typeof r!=='object'||!guid(r.id)||!guid(r.supplierId)||r.propertyId!==c.propertyId||r.tenantId!==c.canonical?.tenantId||!text(row.supplierName,200)||!text(r.serviceName,200)||!text(r.title,200)||!text(r.description,4000))fail();
 if(!workStatuses.includes(r.status as WorkStatus)||!(r.dueAt===null||typeof r.dueAt==='string'&&Number.isFinite(Date.parse(r.dueAt)))||typeof r.isOverdue!=='boolean'||!Number.isSafeInteger(r.photoCount)||Number(r.photoCount)<0||Number(r.photoCount)>10||!Number.isSafeInteger(r.version)||Number(r.version)<1||typeof r.modifiedAt!=='string'||!Number.isFinite(Date.parse(r.modifiedAt)))fail();
 if(r.isOverdue&&!['ASSIGNED','ACCEPTED','IN_PROGRESS'].includes(String(r.status)))fail();
 return {id:String(r.id),supplierId:String(r.supplierId),supplierName:String(row.supplierName),propertyId:String(r.propertyId),serviceName:String(r.serviceName),title:String(r.title),description:String(r.description),status:r.status as WorkStatus,dueAt:r.dueAt as string|null,isOverdue:r.isOverdue as boolean,photoCount:Number(r.photoCount),version:Number(r.version),modifiedAt:String(r.modifiedAt)};
}
export function parseList(v:unknown,user:string,c:Context,page:number,asOf:string):Snapshot{
 if(!permitted(c)||!v||typeof v!=='object')fail();const p=v as {items:unknown;page:unknown;pageSize:unknown;totalCount:unknown};
 if(p.page!==page||p.pageSize!==20||!Number.isSafeInteger(p.totalCount)||Number(p.totalCount)<0||!Array.isArray(p.items)||p.items.length>20||p.items.length>Number(p.totalCount))fail();
 const items=(p.items as unknown[]).map(x=>parseOrder(x,c));if(new Set(items.map(x=>x.id)).size!==items.length)fail();
 return {userId:user,contextKey:contextKey(c),page,total:Number(p.totalCount),asOf,items};
}
export function parseSnapshot(v:unknown,user:string,c:Context,page:number):Snapshot{
 const s=v as Snapshot;if(!s||s.userId!==user||s.contextKey!==contextKey(c)||s.page!==page||!Number.isFinite(Date.parse(s.asOf)))fail();
 return parseList({items:s.items.map(i=>({record:{...i,tenantId:c.canonical?.tenantId}, supplierName:i.supplierName})),page:s.page,pageSize:20,totalCount:s.total},user,c,page,s.asOf);
}
export type CommandKind='TRANSITION'|'NOTE'|'PHOTO';
export type Command={operationId:string;kind:CommandKind;supplierId:string;workOrderId:string;title:string;expectedVersion:number;status:WorkStatus|null;text:string;photoFile:string|null};
export function validateCommand(v:Command,c:Context,current?:WorkOrder):Command{
 if(!permitted(c)||!guid(v.operationId)||!guid(v.supplierId)||!guid(v.workOrderId)||!text(v.title,200)||!Number.isSafeInteger(v.expectedVersion)||v.expectedVersion<1||!['TRANSITION','NOTE','PHOTO'].includes(v.kind))throw Error('Geçersiz iş emri işlemi.');
 const max=v.kind==='PHOTO'?1000:v.kind==='NOTE'?2000:v.status==='COMPLETED'?4000:1000;
 if(!text(v.text,max)||(v.kind==='TRANSITION')!==(v.status!==null)||(v.kind==='PHOTO')!==(v.photoFile!==null)||v.photoFile!==null&&!/^supplier-photo-[0-9a-f-]{36}\.enc$/.test(v.photoFile))throw Error('İşlem notu, durumu veya fotoğrafı geçersiz.');
 if(v.status!==null&&!workStatuses.includes(v.status))throw Error('Geçersiz durum.');
 if(current){
  if(current.id!==v.workOrderId||current.supplierId!==v.supplierId||current.version!==v.expectedVersion)throw Error('İş emri güncel değil; listeyi yenileyin.');
  if(v.status!==null&&!nextStatuses(c,current.status).includes(v.status))throw Error('Bu durum geçişine izin verilmiyor.');
  if(v.kind==='NOTE'&&['CLOSED','REJECTED','CANCELLED'].includes(current.status)||v.kind==='PHOTO'&&!['DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS'].includes(current.status)||v.kind==='PHOTO'&&current.photoCount>=10)throw Error('Bu iş emrine artık not veya fotoğraf eklenemez.');
 }
 return {operationId:v.operationId.toLowerCase(),kind:v.kind,supplierId:v.supplierId.toLowerCase(),workOrderId:v.workOrderId.toLowerCase(),title:v.title.trim(),expectedVersion:v.expectedVersion,status:v.status,text:v.text.trim(),photoFile:v.photoFile};
}
export function commandBody(command:Command){return command.kind==='TRANSITION'?{operationId:command.operationId,version:command.expectedVersion,status:command.status,reason:command.text}:command.kind==='PHOTO'?{operationId:command.operationId,version:command.expectedVersion,caption:command.text}:{operationId:command.operationId,version:command.expectedVersion,note:command.text};}
