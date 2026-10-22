import {parseAuthorization,type Authorization,type CanonicalContext} from '../person-authorization/model.ts';
export type Role = 'SUPER_ADMIN' | 'SITE_ADMIN' | 'MANAGER' | 'ACCOUNTANT' | 'OWNER' | 'TENANT' | 'RESIDENT';
export type Context = { propertyId: string; propertyName: string; unitId: string | null; unitNumber: string | null; role: Role; canonical?:CanonicalContext };
export type Session = { userId: string; username: string | null; contexts: Context[]; hasMore: boolean; authorization?:Authorization };
const roles: string[] = ['SUPER_ADMIN', 'SITE_ADMIN', 'MANAGER', 'ACCOUNTANT', 'OWNER', 'TENANT', 'RESIDENT'];
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
export function parseSession(value: unknown): Session {
  if (!value || typeof value !== 'object') throw new Error('Geçersiz oturum yanıtı.');
  const v = value as Session;
  if (typeof v.userId !== 'string' || !v.userId || !(v.username === null || typeof v.username === 'string') || !Array.isArray(v.contexts) || v.hasMore !== false) throw new Error('Oturum bağlamı yüklenemedi.');
  if (v.contexts.length > 500) throw new Error('Çok fazla bağlam.');
  for (const c of v.contexts) {
    if (!c || !uuid.test(c.propertyId) || typeof c.propertyName !== 'string' || !roles.includes(c.role) || !(c.unitId === null || uuid.test(c.unitId)) || !(c.unitNumber === null || typeof c.unitNumber === 'string')) throw new Error('Geçersiz mülk bağlamı.');
    if (['OWNER', 'TENANT', 'RESIDENT'].includes(c.role) && c.unitId === null) throw new Error('Daire bağlamı eksik.');
  }
  if(v.authorization!==undefined){
    const authorization=parseAuthorization(v.authorization);if(authorization.userId!==v.userId)throw Error('Yetki hesabı eşleşmiyor.');
    const projected=sessionFromAuthorization(authorization);if(v.contexts.length!==projected.contexts.length || v.contexts.some((c,i)=>contextKey(c)!==contextKey(projected.contexts[i])))throw Error('Yetki bağlamları eşleşmiyor.');
    return projected;
  }
  return v;
}
export function contextKey(c: Context): string { return c.canonical?`${c.canonical.tenantId}:${c.canonical.kind}:${c.canonical.id}`:`${c.propertyId}:${c.unitId ?? '-'}:${c.role}`; }
export function selectContext(session: Session, key: string): Context {
  const found = session.contexts.find(c => contextKey(c) === key);
  if (!found) throw new Error('Yetkili bağlam bulunamadı.');
  return found;
}
export const roleLabels: Record<Role, string> = { SUPER_ADMIN: 'Yönetici', SITE_ADMIN: 'Site yöneticisi', MANAGER: 'Yönetici', ACCOUNTANT: 'Muhasebe', OWNER: 'Ev sahibi', TENANT: 'Kiracı', RESIDENT: 'Sakin' };

export function sessionFromAuthorization(value:unknown):Session {const authorization=parseAuthorization(value);return{userId:authorization.userId,username:null,hasMore:false,authorization,contexts:authorization.contexts.map(c=>({propertyId:c.propertyId,propertyName:c.propertyName,unitId:c.unitId,unitNumber:c.unitNumber,role:c.role,canonical:c}))};}

export function sameContextModel(session:Session,key:string){const canonical=['PLATFORM','MANAGEMENT','OWNER','TENANT','RESIDENT'].includes(key.split(':')[1]);return !!session.authorization===canonical;}

// Canonical mode never guesses among several units: only a remembered or the single available context opens by itself.
export function initialContext(contexts:Context[],remembered:string|null,canonical:boolean):Context|null{return contexts.find(c=>contextKey(c)===remembered)??(canonical?(contexts.length===1?contexts[0]:null):contexts[0]??null);}
