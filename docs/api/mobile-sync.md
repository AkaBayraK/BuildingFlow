# Mobil senkronizasyon — Faz06

14.10.2026. Kullanıcı eksik backend API'lerini bu mobil faz kapsamında onayladı. Mevcut iş/UI roadmap sayacı değişmedi; ayrı mobil15faz planı izlenir.

POST /api/v1/mobile/sync/commands
- operationId/recordId/propertyId UUID; unitId UUID|null; role mevcut MobileSession bağlamından.
- action CREATE|UPDATE|DELETE. CREATE expectedVersion0, diğerleri>=1. CREATE/UPDATE description1–2000trim karakter; DELETE descriptionNULL.
-200 operationId, record{id,propertyId,unitId,role,description,version,deleted,modifiedAt}, replayed.
- Aynı actor+operationId+canonical fingerprint aynı transaction'ın özgün sonucu; farklı içerik veya eski sürüm409 BF_MOBILE_SYNC_CONFLICT. İdempotence kimliği yeni denemede değiştirilmez.
- Body32KiB. Açıklama/istek/token loglanmaz.
-401 anonim;403 aktif yetkili bağlam/record ownership yok;400invalid input;409conflict. Gövde JSON değilse415, limit aşımı413.

GET /api/v1/mobile/sync/requests?propertyId=...&unitId=...&role=...
- userId/contextKey/records/hasMore. Güncel tam bağlam listesi, max1000; hasMore=true istemciye tam snapshot olarak uygulanmaz.
- Actor yalnız kendi taleplerini okur/yazar. Yöneticinin diğer kullanıcı taleplerini yönetmesi bu kapsamda yok.
- Exact MobileSession Property/Unit/role aktif üyelik/occupancy kapsamı her push/replay/pull'da yeniden doğrulanır. ACCOUNTANT-only bağlam kapalı.
- Soft-delete kayıt descriptionNULL/deletedtrue/version ile tombstone olarak gelir. Eski replay yeni cached version'ı geriye çevirmemelidir.
- Yeni MobileRequests/receipt modeli mevcut Person/UnitOccupancy ilişkilerini okur; ayrı talep kişisi veya kullanıcı profili uydurulmaz.

Migration20261014062447_MobileRequestSync: mobile_requests + mobile_operation_receipts; Property/Unit compositeFK, actor/operation uniqueindex, audit/concurrency. Dolu Down silmeyi reddeder. Migration+Designer+snapshot kaynakta; model check geçti ve yerel dev DB'ye uygulandı. Mevcut kayıtlar silinmedi. Üretim yayın/migration işi bu tur yapılmadı.

Tüketici ayrı buildingflow-mobile: SQLCipher schema3 outbox/request cache/sync state; stable UUID, bounded backoff,401/403/409, partial snapshot reddi, onaysız otomatik overwrite yok. Çıkışta Faz05 anahtar/kopya temizliği kuyruğu da siler. Henüz talep formu/fotoğraf/kategori UI yok (Faz12).

Doğrulama:52/52 seçili MobileSync/MobileSession/Authentication/ReleaseSecurity integration test (atlama0), ayrı geçici PostgreSQL DB/role/schema temizlendi. Backend build0hata/0uyarı. Mobil26/26 gerçek SQLite+kontrollü HTTP test, TypeScript/lint, üç platform export geçti. iPhone SQLCipher fixture nativeQueueReplaytrue; kontrollü HTTP simülasyonu ve ayrı API entegrasyon testlerinin kanıtları ayrıdır. Gerçek kullanıcı uçak modu/soğuk açılış ve Android native kabulü açık; sahte canlı talep/başarı kaydı üretilmedi.

## Faz12 — kategori, öncelik ve fotoğraf ekleri (15.10.2026)

POST /api/v1/mobile/sync/commands
- Opsiyonel `category` PLUMBING|ELECTRICAL|HEATING|ELEVATOR|CLEANING|SECURITY|OTHER ve `priority` LOW|NORMAL|HIGH|URGENT. CREATE'te boşsa OTHER/NORMAL; UPDATE'te boş alan mevcut değeri korur; DELETE'te ikisi de null olmalı (400).
- İkisi de null olan komutlar Faz06 fingerprint'ini kullanır; kuyruktaki eski işlemler replay olarak kalır.
- `record` yanıtına `category`, `priority`, `photos[{id,sha256,size}]` eklendi; tombstone'da null/boş.

POST /api/v1/mobile/sync/requests/{requestId}/photos — `{operationId,propertyId,unitId,role,photoBase64}`
- Yalnız talebin sahibi, talebin Property/Unit/role bağlamında ve aktif yetkiyle. ACCOUNTANT 403; yabancı/olmayan talep 403; silinmiş talep 409.
- JPEG yapısal doğrulama ve EXIF/APP/yorum temizliği (profil fotoğrafıyla ortak), en çok 256 KiB, gövde 384 KiB (413), JSON değilse 415. Talep başına en çok 3 güncel fotoğraf (400).
- Yüklemeden önce STAGED kaydı; aynı actor+operationId+fingerprint replay olarak döner, farklı içerik/talep 409. Talep version'ı değişmez.
- Nesne anahtarı `requests/{requestId}/{photoId}.jpg` sunucu UUID'sidir.

GET /api/v1/mobile/sync/requests/{requestId}/photos/{photoId}?propertyId&unitId&role
- Sahibi ve aynı bağlam; SHA256/boyut doğrulanır, `no-store`. Başka kullanıcı/silinmiş talep 404.

Talep DELETE edilince fotoğraflar RETIRED olur; bakım işi (2 dk) RETIRED ve 30 dk'dan eski STAGED nesneleri siler ve PURGED yapar. Yönetici ekranında fotoğraf görüntüleme bu fazda yoktur.

Migration 20261015133409_MobileRequestCategoriesAndPhotos: mobile_requests.category/priority (varsayılan backfill + check), mobile_request_photos (Property+Request composite FK, actor/operation unique, state/size check). Down, fotoğraf veya varsayılan dışı kategori/öncelik varsa reddeder.
