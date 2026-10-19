import { familyName, givenName } from '../profile/names.ts';
export type RegistrationInput = { username: string; password: string; passwordConfirmation: string; firstName: string; lastName: string; phoneNumber: string; email: string; personCode: string };
export type PersonLinkInput = { personCode: string; firstName: string; lastName: string; phoneNumber: string; email: string };
export type LinkStatus = 'LINKED' | 'REJECTED' | 'LOCKED' | 'NONE';
export type RegistrationResult = { userId: string; username: string; personLink: { status: LinkStatus; propertyName: string | null } };
export type FieldErrors = Partial<Record<keyof RegistrationInput, string>>;
const username = /^[a-z0-9][a-z0-9._-]{2,49}$/;
const email = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const statuses: LinkStatus[] = ['LINKED', 'REJECTED', 'LOCKED', 'NONE'];

// Mirrors the server: E.164, accepting 0 5xx…, 5xx…, 0090… and +90 forms.
export function normalizePhone(value: string): string | null {
  let text = '';
  for (const c of value.trim()) { if (/[0-9]/.test(c) || c === '+' && text === '') text += c; else if (!' ()-.'.includes(c)) return null; }
  if (text.startsWith('00')) text = '+' + text.slice(2);
  else if (text.startsWith('0') && text.length === 11) text = '+90' + text.slice(1);
  else if (text.startsWith('5') && text.length === 10) text = '+90' + text;
  return /^\+[1-9][0-9]{7,14}$/.test(text) ? text : null;
}
export const strongPassword = (v: string) => v.length >= 12 && v.length <= 128 && /[A-Z]/.test(v) && /[a-z]/.test(v) && /[0-9]/.test(v);
export const validCode = (v: string) => /^[1-9][0-9]{5}$/.test(v.trim());
const name = (v: string) => v.trim().length > 0 && v.trim().length <= 80;

export function validateRegistration(input: RegistrationInput): FieldErrors {
  const errors: FieldErrors = {};
  if (!username.test(input.username.trim().toLowerCase())) errors.username = 'Kullanıcı adı 3–50 karakter olmalı; küçük harf, rakam, nokta, alt çizgi veya tire.';
  if (!strongPassword(input.password)) errors.password = 'Parola en az 12 karakter olmalı; büyük harf, küçük harf ve rakam içermeli.';
  if (input.password !== input.passwordConfirmation) errors.passwordConfirmation = 'Parolalar aynı değil.';
  if (!name(input.firstName)) errors.firstName = 'Adını gir.';
  if (!name(input.lastName)) errors.lastName = 'Soyadını gir.';
  if (!normalizePhone(input.phoneNumber)) errors.phoneNumber = 'Geçerli bir cep telefonu gir.';
  if (!email.test(input.email.trim()) || input.email.trim().length > 254) errors.email = 'Geçerli bir e-posta gir.';
  if (input.personCode.trim() && !validCode(input.personCode)) errors.personCode = 'Kişi kodu 6 haneli olmalı.';
  return errors;
}
export function validateLink(input: PersonLinkInput): string | null {
  if (!validCode(input.personCode)) return 'Kişi kodu 6 haneli olmalı.';
  if (!name(input.firstName) || !name(input.lastName)) return 'Adını ve soyadını kayıttaki gibi gir.';
  if (!normalizePhone(input.phoneNumber)) return 'Geçerli bir cep telefonu gir.';
  if (input.email.trim() && !email.test(input.email.trim())) return 'Geçerli bir e-posta gir.';
  return null;
}
export function registrationBody(requestId: string, input: RegistrationInput) {
  return { requestId, username: input.username.trim().toLowerCase(), password: input.password, passwordConfirmation: input.passwordConfirmation,
    firstName: givenName(input.firstName), lastName: familyName(input.lastName), phoneNumber: normalizePhone(input.phoneNumber), email: input.email.trim().toLowerCase(), personCode: input.personCode.trim() || null };
}
export function linkBody(input: PersonLinkInput) {
  return { personCode: input.personCode.trim(), firstName: input.firstName.trim(), lastName: input.lastName.trim(), phoneNumber: normalizePhone(input.phoneNumber), email: input.email.trim().toLowerCase() || null };
}
export function parseRegistration(value: unknown): RegistrationResult {
  const v = value as { userId?: unknown; username?: unknown; personLink?: { status?: unknown; propertyName?: unknown } };
  if (!v || typeof v.userId !== 'string' || !v.userId || typeof v.username !== 'string' || !v.personLink || !statuses.includes(v.personLink.status as LinkStatus) || !(v.personLink.propertyName == null || typeof v.personLink.propertyName === 'string')) throw new Error('Geçersiz kayıt yanıtı.');
  return { userId: v.userId, username: v.username, personLink: { status: v.personLink.status as LinkStatus, propertyName: (v.personLink.propertyName as string | null | undefined) ?? null } };
}
// Server error codes are mapped to user text without echoing server-provided content.
export function errorText(code: string | null): string {
  switch (code) {
    case 'BF_MOBILE_LINK_REJECTED': return 'Kişi kodu veya girdiğin bilgiler kayıtla uyuşmuyor. Ad, soyad, telefon ve e-postanı yönetimdeki kaydınla aynı yaz.';
    case 'BF_MOBILE_LINK_LOCKED': return 'Çok fazla hatalı deneme yapıldı. 15 dakika sonra tekrar dene.';
    case 'BF_MOBILE_REGISTRATION_CONFLICT': return 'Bu kullanıcı adı veya e-postayla kayıt oluşturulamadı. Farklı bir kullanıcı adı dene ya da giriş yap.';
    case 'BF_MOBILE_RATE_LIMITED': return 'Çok fazla kayıt denemesi yapıldı. Bir süre sonra tekrar dene.';
    case 'BF_USER_PASSWORD_WEAK': return 'Parola en az 12 karakter olmalı; büyük harf, küçük harf ve rakam içermeli.';
    case 'BF_IDENTITY_UNAVAILABLE': return 'Kayıt sunucusuna şu anda ulaşılamıyor. Biraz sonra tekrar dene.';
    default: return 'İşlem tamamlanamadı. Bilgilerini kontrol edip tekrar dene.';
  }
}
export async function responseCode(response: Response): Promise<string | null> {
  try { const body = await response.json() as { code?: unknown }; return typeof body.code === 'string' ? body.code : null; } catch { return null; }
}
