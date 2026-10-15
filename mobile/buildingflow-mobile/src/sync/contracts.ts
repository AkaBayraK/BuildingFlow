import { contextKey, selectContext, type Session, type Context } from '../auth/contracts.ts';
export type Action = 'CREATE' | 'UPDATE' | 'DELETE';
export const CATEGORIES = ['PLUMBING','ELECTRICAL','HEATING','ELEVATOR','CLEANING','SECURITY','OTHER'] as const;
export const PRIORITIES = ['LOW','NORMAL','HIGH','URGENT'] as const;
export type Category = typeof CATEGORIES[number];
export type Priority = typeof PRIORITIES[number];
export const MAX_REQUEST_PHOTOS = 3;
// category/priority are null only for DELETE or legacy queued commands; the server then keeps defaults.
export type Command = { operationId: string; recordId: string; propertyId: string; unitId: string | null; role: string; action: Action; expectedVersion: number; description: string | null; category?: Category | null; priority?: Priority | null };
export type PhotoRef = { id: string; sha256: string; size: number };
export type RecordValue = { id: string; propertyId: string; unitId: string | null; role: string; description: string | null; version: number; deleted: boolean; modifiedAt: string; category: Category | null; priority: Priority | null; photos: PhotoRef[] };
export const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
export function validateCommand(command: Command, session: Session, key: string): Command {
  const c=selectContext(session,key);
  const category=command.category ?? null, priority=command.priority ?? null;
  if((category!==null && !CATEGORIES.includes(category)) || (priority!==null && !PRIORITIES.includes(priority)) || (command.action==='DELETE' && (category!==null || priority!==null))) throw Error('Geçersiz eşitleme işlemi.');
  if(c.role==='ACCOUNTANT' || command.propertyId!==c.propertyId || command.unitId!==c.unitId || command.role!==c.role || !uuid.test(command.operationId) || command.operationId==='00000000-0000-0000-0000-000000000000' || !uuid.test(command.recordId) || command.recordId==='00000000-0000-0000-0000-000000000000' || !['CREATE','UPDATE','DELETE'].includes(command.action) || !Number.isSafeInteger(command.expectedVersion) || command.expectedVersion<0 || (command.action==='CREATE' && command.expectedVersion!==0) || (command.action!=='CREATE' && command.expectedVersion<1) || (command.action==='DELETE' ? command.description!==null : typeof command.description!=='string' || !command.description.trim() || command.description.trim().length>2000)) throw Error('Geçersiz eşitleme işlemi.');
  return {operationId:command.operationId.toLowerCase(),recordId:command.recordId.toLowerCase(),propertyId:command.propertyId,unitId:command.unitId,role:command.role,action:command.action,expectedVersion:command.expectedVersion,description:command.description?.trim() ?? null,category,priority};
}
export function parseRecord(value: unknown, context: Context): RecordValue {
  if(!value || typeof value!=='object') throw Error('Geçersiz sunucu kaydı.');
  const r=value as RecordValue;
  if(!uuid.test(r.id) || r.propertyId!==context.propertyId || r.unitId!==context.unitId || r.role!==context.role || !Number.isSafeInteger(r.version) || r.version<1 || typeof r.deleted!=='boolean' || typeof r.modifiedAt!=='string' || !Number.isFinite(Date.parse(r.modifiedAt)) || (r.deleted ? r.description!==null : typeof r.description!=='string' || !r.description.trim() || r.description.length>2000)) throw Error('Sunucu kaydı bağlamla eşleşmiyor.');
  // Pre-phase-12 servers omit these fields; absent (not null) values map to the server defaults.
  const category=r.category===undefined?(r.deleted?null:'OTHER'):r.category, priority=r.priority===undefined?(r.deleted?null:'NORMAL'):r.priority, photos=r.photos===undefined?[]:r.photos;
  if(r.deleted ? (category!==null || priority!==null || photos.length>0) : (!CATEGORIES.includes(category as Category) || !PRIORITIES.includes(priority as Priority)) || !Array.isArray(photos) || photos.length>MAX_REQUEST_PHOTOS || photos.some(p=>!p || !uuid.test(p.id) || typeof p.sha256!=='string' || !/^[0-9a-f]{64}$/i.test(p.sha256) || !Number.isSafeInteger(p.size) || p.size<1 || p.size>262144)) throw Error('Sunucu kaydı bağlamla eşleşmiyor.');
  return {id:r.id,propertyId:r.propertyId,unitId:r.unitId,role:r.role,description:r.description,version:r.version,deleted:r.deleted,modifiedAt:r.modifiedAt,category,priority,photos:photos.map(p=>({id:p.id.toLowerCase(),sha256:p.sha256.toLowerCase(),size:p.size}))};
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
  return record;
}
