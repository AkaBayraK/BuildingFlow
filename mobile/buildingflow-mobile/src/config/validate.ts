export function validateApiUrl(value: string | undefined, development: boolean): string | null {
  if (!value?.trim()) return null;
  const url = new URL(value);
  if (url.username || url.password || url.search || url.hash) throw new Error('API adresi kimlik bilgisi veya sorgu içeremez.');
  if (url.protocol !== 'https:' && !(development && url.protocol === 'http:')) throw new Error('API adresi HTTPS olmalı.');
  return url.toString().replace(/\/$/, '');
}
