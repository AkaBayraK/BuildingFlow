# Merkez Talepler ve Arızalar

15.10.2026 — kullanıcı onaylı merkez tamamlaması, GitHub [#5](https://github.com/AkaBayraK/BuildingFlow/issues/5), [Project 3](https://github.com/users/AkaBayraK/projects/3). Mobil planın 15 fazına yeni faz eklenmez.

## Yetki ve liste

`/requests` yönetim alanı seçer; `/properties/{id}/requests` canonical merkez ekranıdır. Menü yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER rollerine görünür. Backend tüm GET/POST işlemlerinde gerçek JWT yönetici rolü + etkin, silinmemiş Property ve aktif üyeliği denetler; mevcut global SUPER_ADMIN istisnası korunur. ACCOUNTANT/RESIDENT/anonim/foreign/revoked erişim açılmaz. Kendi talebini oluşturan sakin merkez yöneticisi olmaz.

- GET `/api/v1/properties/{propertyId}/requests`: page>=1, pageSize 1–100; merkez 10. search açıklamada literal alt metin (max200), status/category/priority izinli enum; sort newest/oldest/updated, UUID ile kararlı tie-break. Filtre değişince merkez sayfa1. Total filtre kapsamındadır; sayfa üzerinde yerel filtreleme yapılmaz.
- GET `/{id}`: Record + History + Replayed=false. ID aynı Property içinde aranır. Yetkili alanda olmayan kayıt404, erişilemeyen alan403. Soft-delete/pasif talepler gösterilmez. OwnerUserId, ActorUserId, CreatedBy veya kişi PII tabloya ad koymak için dönmez. Daire numarası aynı Property'den okunur; tarihsel bulunamayan daire alanı nullable'dır.
- Fotoğraf mevcut JWT kontrollü `/api/v1/mobile/sync/requests/{id}/photo` kullanır. Private SeaweedFS anahtarı/credential dönmez; hem API hem FE MIME/boyut/SHA256 doğrular, FE Blob URL cleanup/abort uygular. Snapshot alınan fotoğraf değişmişse hash uyuşmaz; güncel detay yeniden yüklenir.

## Dar yönetim akışı

POST `/{id}/management`: `{ operationId: UUID, version: long, status, response: 1..2000 }`. Yanıt sakine görünürdür; iç not/kişi bilgisi veya atama sistemi değildir.

| Mevcut durum | İzinli hedef |
|---|---|
| RECORDED | IN_PROGRESS, REJECTED |
| IN_PROGRESS | RESOLVED, REJECTED |
| RESOLVED veya REJECTED | RECORDED (gerekçeli yeniden açma) |

Her geçiş açıklama ister, same-status/no-op kabul edilmez. Invalid payload400; stale version/izin verilmeyen geçiş/aynı UUID farklı içerik409. MobileRequest status/response/hasManagementHistory aynı transaction'da güncellenir, version/audit artar. Her işlem BaseEntity RequestManagementEvent'e immutable from/to/response + server actor/operation/fingerprint/expectedVersion ekler. SQL tetikleyicisi UPDATE/DELETE'yi engeller; scope composite FK Property+Request'tir. Geçmiş expectedVersion sırasındadır. Domain EF/HTTP bağımlılığı almaz.

Actor+operation tekilliği, normalize fingerprint, operation advisory lock ve mobildeki `mobile-record:{id}` advisory lock yinelenen veya yarışan işlemi serileştirir. Property row lock ve kilit sonrası aktif üyelik kontrolü parent yarışı için kullanılır. Aynı UUID/aynı payload replay yeni olay eklemez; **güncel** kayıt ve geçmiş + Replayed=true döner, eski duruma geri dönmez. Başka alana/kayda aynı UUID ile replay içerik çatışmasıdır. Revoked üyelik replay'de de reddedilir. API doğrudan yönetici iş kaydının açıklama/kategori/fotoğrafını değiştirmez.

## Mobil/offline uyumu

MobileRequest tek merkezi kayıt deposudur; ikinci merkez talep tablosu yoktur. Eski command JSON, legacy ve request-v2 fingerprint, makbuz JSON, version+1 kabulü ve tombstone davranışı korunur. CREATE replay eski sürümlü makbuzu döndürmeye devam eder; istemci sonrasında pull ile güncel yönetim durumunu alır. Yeni DTO alanları optional eklenir: ManagementStatus=RECORDED, ManagementResponse=null, HasManagementHistory=false. Eski kayıt/makbuzlar bu varsayılanlarla okunur.

Merkez yönetim geçişi ortak request version'ı artırır: eski UPDATE/DELETE409'dur. **Herhangi bir yönetim geçmişi oluşunca**, yeniden açılmış RECORDED dahil, mobil içerik/fotoğraf değiştirme ve silme de409; kanıt/açıklama/fotoğraf korunur. Mobil kaldırma düğmesi yerine bu sınır gösterilir. Başka bir sorun için yeni talep açılır. Bekleyen eski komut otomatik yeniden yazılmaz; mevcut conflict kuyruğu + güncel pull davranışı korunur. Mobil sync cache JSON alanlarını mevcut şifreli veri akışında taşır; yeni SQLite migration veya ayrı dosya deposu gerekmez. Benim taleplerim ve owner/tenant özetleri merkez durum/yanıtını gösterir; manager listesi ortak status'ü gösterir. Mobil yönetim mutation eklenmez.

Property silme, silinmiş dahil talep geçmişi varsa reddedilir. Photo maintenance, STAGED/CURRENT/RETIRED/PURGED sözleşmesi değişmez.

## Migration ve kabul

`20261015203750_RequestManagementWorkflow` eklemelidir: mevcut talep satırlarında yalnız yeni sütunların defaultları, önceki içerik/audit/version korunur. Yeni tablo sekiz zorunlu NOT NULL BaseEntity alanını taşır. Source+Designer+snapshot birlikte tutulur; uygulanmış migration'lar değiştirilmez. Down geçmiş veya yönetim durumu/yanıtı varsa ilk drop öncesi reddeder. Yeni data olmadan Down/Up mümkündür. Tetikleyici tarihçeyi değişmez kılar; geçmişi export/geri getirme mekanizması değildir.

Otomatik kanıt: RequestManagementTests, RequestManagementMigrationTests, MobileOwnerTests + eski MobileRequestDetailsTests; frontend requests.test.mjs ve mobil requests.test.mjs. Gerçek izole PostgreSQL/fixture SeaweedFS159/159, merkez112/112, mobil92/92; typecheck/lint/build/export ve329 paket metadata kontrolü. Ayrıntılı koşullar ve çalıştırma kayıtları [TESTING](../TESTING.md) içindedir. Gerçek görsel/cihaz kabulü ve production dağıtımı yapılmadı; iş teknik teslim olarak In review'dır. Atama, tedarikçi, ödeme, dış bildirim ve yeni mobil faz kapsamı yoktur.


### 16.10.2026 yerel teslim

Teknik dört işpaketi tamamlandı. Son izole test159/159,FE112/112,mobil92/92; yerel migration sonrası36eski tablo hashleri aynı. API5005/5006health200,anonimtalep401,web/requests200,Metro8081running. GitHub#5Project3Inreview; gerçek UI/cihaz kabulü yapılmadı. Standart dev.sh api EF resolver sınırlaması ve doğrudan aynıayarlıno-buildruntime TESTING.md'de kayıtlıdır.

### İŞ17 fotoğraf rızası

Detay yanıtı `photoConsent` taşır (fotoğrafsız talepte `null`): `state` GRANTED/WITHDRAWN/NONE, `recordId`, `textVersion`, `approvedAt`, `withdrawnAt`. Merkez fotoğrafı rıza geri çekildikten sonra da görür; web detayında rıza sürümü/tarihi ya da geri çekme tarihi ve rıza belgesi bağlantısı gösterilir. Ayrıntı [mobile-consents.md](mobile-consents.md).
