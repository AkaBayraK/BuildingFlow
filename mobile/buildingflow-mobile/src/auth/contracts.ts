export type Role = 'SUPER_ADMIN' | 'SITE_ADMIN' | 'MANAGER' | 'ACCOUNTANT' | 'OWNER' | 'TENANT' | 'RESIDENT';
export type Context = { propertyId: string; propertyName: string; unitId: string | null; unitNumber: string | null; role: Role };
export type Session = { userId: string; username: string | null; contexts: Context[]; hasMore: boolean };
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
  return v;
}
export function contextKey(c: Context): string { return `${c.propertyId}:${c.unitId ?? '-'}:${c.role}`; }
export function selectContext(session: Session, key: string): Context {
  const found = session.contexts.find(c => contextKey(c) === key);
  if (!found) throw new Error('Yetkili bağlam bulunamadı.');
  return found;
}
export const roleLabels: Record<Role, string> = { SUPER_ADMIN: 'Yönetici', SITE_ADMIN: 'Site yöneticisi', MANAGER: 'Yönetici', ACCOUNTANT: 'Muhasebe', OWNER: 'Ev sahibi', TENANT: 'Kiracı', RESIDENT: 'Sakin' };
