// Web and mobile share the same bounded DTO parser; no extra package or storage.
export * from '../../../../shared/contact-verification.ts';
export type EmailAction='VERIFY_EMAIL'|'UPDATE_EMAIL';
export function isContactRequest(path:string,body:unknown){return body===undefined?path==='/api/v1/mobile/contact-verification':path==='/api/v1/mobile/contact-verification/phone'||path==='/api/v1/mobile/contact-verification/phone/commands';}
export function contactError(status:number){
 if(status===409)return 'Bilgiler başka bir ekranda değişti. Güncel durumu kontrol edip tekrar deneyin.';
 if(status===429)return 'İstek sınırına ulaşıldı. Biraz bekleyip tekrar deneyin.';
 if(status===401||status===403)return 'Doğrulama için hesabınıza yeniden giriş yapın.';
 if(status===400)return 'Numarayı veya 6 haneli kodu kontrol edin.';
 return 'Doğrulama servisine ulaşılamadı. Bağlantıyı kontrol edip tekrar deneyin.';
}
