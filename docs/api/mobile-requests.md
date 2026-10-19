# Mobil talep ayrıntıları ve fotoğraf — Faz 12

15.10.2026. Üretici: MobileSync DTO/store/configuration/endpoints ve MobileRequest/Photo. Tüketici: `mobile/buildingflow-mobile/src/requests` ve talep bileşenleri.

POST `/api/v1/mobile/sync/commands` mevcut sekiz alana optional Category/Priority/PhotoAction/PhotoBase64 ekler. Category: FAULT, CLEANING, SECURITY, COMMON_AREA, SUGGESTION, OTHER. Priority: LOW, NORMAL, HIGH. Legacy create OTHER/NORMAL kullanır; update eksik ayrıntıyı korur. KEEP varsayılandır; REPLACE JPEG fotoğraf alır, REMOVE referansı kaldırır. DELETE yeni ayrıntı veya fotoğraf içermez; tombstone korunur. Legacy fingerprint birebir, v2 fingerprint normalize fotoğraf SHA256 ile hesaplanır. Version/409, UUID replay ve aktif bağlam yetkileri korunur. JPEG en fazla 256 KB, JSON en fazla 400.000 bayt. İŞ17: REPLACE için ACTIVE `REQUEST_PHOTO` açık rızası gerekir (403 `BF_CONSENT_REQUIRED`); talep fotoğrafsız kaydedilebilir. Rıza geri çekilince fotoğraflar silinmez, sahibinin sorgularından gizlenir; ayrıntı [mobile-consents.md](mobile-consents.md).

Fotoğraf baytları özel SeaweedFS’te, metadata MobileRequestPhoto tablosundadır. Owner/property/photo bileşik FK ve owner/operation tekilliği vardır. STAGED rezervasyonu yüklemeden önce kaydedilir; upload sonrasında CURRENT olur. RETIRED ve eski STAGED nesneler bakımda temizlenir. İndirmede boyut ve SHA256 doğrulanır.

GET `/api/v1/mobile/sync/requests/{id}/photo`: kendi yetkili bağlamı veya gerçek yönetici rolü + aktif Property kapsamı; mevcut global SUPER_ADMIN istisnası korunur. ACCOUNTANT veya foreign/revoked erişim 403, anonim 401, silinmiş/olmayan fotoğraf 404. Nesne anahtarı ve credential dönmez. Yönetici başkasının fotoğrafını yalnız yetkili Property içinde okuyabilir; başkasının kaydını silme veya iş atama eklenmedi.

Eklemeli migration: `20261015133914_MobileRequestDetailsAndPhotos`. Mobil SQLCipher6 metadata ve şifreli dosyalar kullanır; web kalıcı kuyruk oluşturmaz. Ayrıntılar ve açık cihaz kabulü: [PHASE12](../../mobile/docs/mobile/PHASE12.md).

Bu tur: 60 mobil, 83 PG/SeaweedFS entegrasyon ve 3 persistence testi; sıfır atlama. TypeScript, üç platform export ve native iPhone build/install başarılı. Gerçek telefon kamera/izin/offline kullanıcı testi yapılmadı.


## Merkez yönetimi — 15.10.2026 tamamlaması

Merkez Talepler ve Arızalar, aynı MobileRequest üzerinde status/response ve değişmez geçmiş ekler. Eski fingerprint/makbuzlar değişmez. Yönetim işlemi version artırır; herhangi bir yönetim geçmişinden sonra mobil UPDATE/DELETE409 ile reddedilir, tekrar açılmış RECORDED dahil içerik/fotoğraf korunur. Yeni DTO alanları varsayılanlıdır; mobil sync/owner/tenant/manager aynı merkez durumunu okur. Ayrıntılar [request-management](request-management.md); mobil Faz16 oluşturulmadı.
