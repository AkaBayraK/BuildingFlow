import { contextKey, selectContext, type Session, type Context } from '../auth/contracts.ts';
export type Action = 'CREATE' | 'UPDATE' | 'DELETE';
export const categories={ELEVATOR:'Asansör arızası',WATER:'Su / tesisat',ELECTRICITY:'Elektrik',HEATING:'Isıtma / doğalgaz',RENOVATION:'Boya / tadilat',FAULT:'Arıza',CLEANING:'Temizlik',SECURITY:'Güvenlik',COMMON_AREA:'Ortak alan',SUGGESTION:'Öneri',OTHER:'Diğer'};
export const priorities={LOW:'Düşük',NORMAL:'Normal',HIGH:'Yüksek'};
export const requestStatuses={RECORDED:'Kaydedildi',IN_PROGRESS:'İşlemde',RESOLVED:'Çözüldü',REJECTED:'Reddedildi'};
export type Category=keyof typeof categories;
// Request type icons (Ionicons names) and colours; the web centre uses the same mapping so both sides read alike.
const tone={red:{color:'#dc2626',background:'#fef2f2'},blue:{color:'#0866e8',background:'#edf4ff'},amber:{color:'#d97706',background:'#fffbeb'},green:{color:'#059669',background:'#ecfdf5'},violet:{color:'#6d28d9',background:'#f5f3ff'},teal:{color:'#0f766e',background:'#f0fdfa'},neutral:{color:'#385374',background:'#f4f6f9'}};
export const categoryIcons={ELEVATOR:{icon:'build-outline',...tone.red},WATER:{icon:'water-outline',...tone.blue},ELECTRICITY:{icon:'flash-outline',...tone.amber},HEATING:{icon:'flame-outline',...tone.red},RENOVATION:{icon:'brush-outline',...tone.blue},
 FAULT:{icon:'hammer-outline',...tone.amber},CLEANING:{icon:'sparkles-outline',...tone.green},SECURITY:{icon:'shield-checkmark-outline',...tone.violet},COMMON_AREA:{icon:'business-outline',...tone.teal},SUGGESTION:{icon:'bulb-outline',...tone.amber},OTHER:{icon:'ellipsis-horizontal-circle-outline',...tone.neutral}} as const satisfies Record<Category,{icon:string;color:string;background:string}>;
export type Priority=keyof typeof priorities;
export type Command = { operationId: string; recordId: string; propertyId: string; unitId: string | null; role: string; action: Action; expectedVersion: number; description: string | null; category?:Category; priority?:Priority; photoAction?:'KEEP'|'REPLACE'|'REMOVE';contextKind?:string;contextId?:string };
export type RecordValue = { id: string; propertyId: string; unitId: string | null; role: string; description: string | null; version: number; deleted: boolean; modifiedAt: string; category?:Category; priority?:Priority; photoId?:string|null; photoSha256?:string|null; managementStatus?:keyof typeof requestStatuses; managementResponse?:string|null; hasManagementHistory?:boolean };
export const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
export function validateCommand(command: Command, session: Session, key: string): Command {
  const c=selectContext(session,key);
  if(c.canonical?(command.contextId!==c.canonical.id||command.contextKind!==c.canonical.kind):(command.contextId!==undefined||command.contextKind!==undefined))throw Error('İşlem çalışma alanıyla eşleşmiyor.');
  if(c.role==='ACCOUNTANT' || command.propertyId!==c.propertyId || command.unitId!==c.unitId || command.role!==c.role || !uuid.test(command.operationId) || command.operationId==='00000000-0000-0000-0000-000000000000' || !uuid.test(command.recordId) || command.recordId==='00000000-0000-0000-0000-000000000000' || !['CREATE','UPDATE','DELETE'].includes(command.action) || !Number.isSafeInteger(command.expectedVersion) || command.expectedVersion<0 || (command.action==='CREATE' && command.expectedVersion!==0) || (command.action!=='CREATE' && command.expectedVersion<1) || (command.action==='DELETE' ? command.description!==null : typeof command.description!=='string' || !command.description.trim() || command.description.trim().length>2000)) throw Error('Geçersiz eşitleme işlemi.');
  if(command.category!==undefined&&!Object.hasOwn(categories,command.category)||command.priority!==undefined&&!Object.hasOwn(priorities,command.priority)||command.photoAction!==undefined&&!['KEEP','REPLACE','REMOVE'].includes(command.photoAction)||(command.action==='DELETE'&&(command.category!==undefined||command.priority!==undefined||command.photoAction!==undefined&&command.photoAction!=='KEEP')))throw Error('Talep kategori/öncelik bilgisi geçersiz.');
  return {...(c.canonical?{contextKind:c.canonical.kind,contextId:c.canonical.id}:{}),operationId:command.operationId.toLowerCase(),recordId:command.recordId.toLowerCase(),propertyId:command.propertyId,unitId:command.unitId,role:command.role,action:command.action,expectedVersion:command.expectedVersion,description:command.description?.trim() ?? null,...(command.category===undefined?{}:{category:command.category}),...(command.priority===undefined?{}:{priority:command.priority}),...(command.photoAction===undefined?{}:{photoAction:command.photoAction})};
}
export function parseRecord(value: unknown, context: Context): RecordValue {
  if(!value || typeof value!=='object') throw Error('Geçersiz sunucu kaydı.');
  const r=value as RecordValue;
  if(!uuid.test(r.id) || r.propertyId!==context.propertyId || r.unitId!==context.unitId || r.role!==context.role || !Number.isSafeInteger(r.version) || r.version<1 || typeof r.deleted!=='boolean' || typeof r.modifiedAt!=='string' || !Number.isFinite(Date.parse(r.modifiedAt)) || (r.deleted ? r.description!==null : typeof r.description!=='string' || !r.description.trim() || r.description.length>2000)) throw Error('Sunucu kaydı bağlamla eşleşmiyor.');
  const category=r.category??'OTHER',priority=r.priority??'NORMAL',photoId=r.photoId??null,photoSha256=r.photoSha256??null;
  if(!Object.hasOwn(categories,category)||!Object.hasOwn(priorities,priority)||(photoId===null?photoSha256!==null:!uuid.test(photoId)||typeof photoSha256!=='string'||!/^[a-f0-9]{64}$/i.test(photoSha256))||(r.deleted&&photoId!==null))throw Error('Talep eki geçersiz.');
  const managementStatus=r.managementStatus??'RECORDED',managementResponse=r.managementResponse??null,hasManagementHistory=r.hasManagementHistory??false;
  if(!Object.hasOwn(requestStatuses,managementStatus)||typeof hasManagementHistory!=='boolean'||(managementResponse!==null&&(typeof managementResponse!=='string'||!managementResponse.trim()||managementResponse.length>2000))||(!hasManagementHistory&&(managementStatus!=='RECORDED'||managementResponse!==null))||(hasManagementHistory&&managementResponse===null))throw Error('Talep yönetim bilgisi geçersiz.');
  return {managementStatus,managementResponse,hasManagementHistory,category,priority,photoId,photoSha256:photoSha256?.toLowerCase()??null,id:r.id,propertyId:r.propertyId,unitId:r.unitId,role:r.role,description:r.description,version:r.version,deleted:r.deleted,modifiedAt:r.modifiedAt};
}
export function parsePull(value: unknown, session: Session, context: Context): RecordValue[] {
  if(!value || typeof value!=='object') throw Error('Geçersiz eşitleme yanıtı.');
  const v=value as {userId:string;contextKey:string;records:unknown[];hasMore:boolean};
  if(v.userId!==session.userId || v.contextKey!==contextKey(context) || v.hasMore!==false || !Array.isArray(v.records) || v.records.length>1000) throw Error('Eşitleme kapsamı eksik veya yabancı.');
  const records=v.records.map(r=>parseRecord(r,context));
  if(new Set(records.map(r=>r.id)).size!==records.length) throw Error('Yinelenen sunucu kaydı.');
  return records;
}
export function parsePush(value: unknown, command: Command, context: Context): RecordValue {
  if(!value || typeof value!=='object') throw Error('Geçersiz işlem makbuzu.');
  const v=value as {operationId:string;record:unknown;replayed:boolean};
  if(v.operationId!==command.operationId || typeof v.replayed!=='boolean') throw Error('Makbuz işlemle eşleşmiyor.');
  const record=parseRecord(v.record,context);
  if(record.id!==command.recordId || record.version!==command.expectedVersion+1 || record.deleted!==(command.action==='DELETE')) throw Error('Makbuz kaydı eşleşmiyor.');
  if(command.category!==undefined&&record.category!==command.category||command.priority!==undefined&&record.priority!==command.priority||command.photoAction==='REPLACE'&&!record.photoId||command.photoAction==='REMOVE'&&record.photoId!==null)throw Error('Talep ayrıntı makbuzu eşleşmiyor.');
  return record;
}
