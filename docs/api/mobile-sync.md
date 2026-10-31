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

## PERSON FAZ09 —18.10.2026
Canonical contextKind/contextId, scope anahtarı, güncel görev/ilişki denetimi, korunmuş legacy kuyruk ve fingerprint biçimleri için [yeni sözleşme](mobile-canonical-sync.md). Bayraklar kapalıyken mevcut legacy akış korunur; canlı geçiş yapılmadı.

## TNM 06 — etkin talep seçenekleri (30.10.2026)

`GET /api/v1/mobile/sync/definitions?propertyId=...&role=...&unitId=...&contextKind=...&contextId=...` yalnız GET; query kapsamı mevcut `/sync/requests` ile aynıdır. Yanıt `userId`, `contextKey`, `propertyId`, yalnız `REQUEST_CATEGORY` ve `REQUEST_PRIORITY` listeleri. Her değer kod, TR/EN ad, ikon/ton, sıra, aktif/gizli/seçilebilir/kilitli ve sürüm taşır. Seçilemeyen kodlar tarihsel ad çözümü için yanıtta kalır.

Aynı MobileSyncStore.Authorize sınırı her okumada doğrulanır: legacy aktif Property üyeliği + tam rol/Unit ve gerekiyorsa güncel kişi/ilişki; canonical açık bayraklar + issuer/subject/tenant/aktif context. Başka alan/Unit/role/context veya iptal edilmiş üyelik/ilişki 403; anonim 401; ACCOUNTANT talep bağlamı kapalıdır. Yetki doğrulamasından sonra yalnız o Property'nin iki listesi okunur; genel yönetim/katalog yetkisi verilmez. Var olan komut UUID/fingerprint, kod doğrulama ve eski kodu koruma davranışları değişmez.

Mobil form bu listeden sıra/seçilebilirlik/TR ad/ikon/ton kullanır. Tanınmayan/mükerrer/eksik kod, yanlış hesap/bağlam veya tutarsız seçilebilirlik reddedilir; yeni kategori ekleme bu sözleşmede yoktur. Metadata, mevcut şifreli cache içindeki owner_snapshots tablosunda `userId + contextKey + :request-definitions` alanına kaydedilir; yeni SQLite/PG şeması yok. Kopya en fazla 24 saat, saat geri alınmışsa geçersiz; canonical modun daha kısa yetki süresi ayrıca requestCache tarafından korunur. Güncel okuma başarısızsa uygun son kopya veya varsayılan seçenekle taslak hazırlanabilir ve güncellik uyarısı görünür; merkezde yetki/aktiflik yeniden doğrulanır. Çevrimdışı kopya o andaki sunucu ayarını garanti etmez.

Gizli/pasif seçimli mevcut taslak açıklama/fotoğrafıyla kalır; etkin seçim istenir, otomatik kategori veya operationId dönüşümü yok. Hesap/bağlam değişiminde eski taslak ekranda gösterilmez ve eski async autosave yeni bağlama yazamaz. Önceden gönderilen taleplerin kategori adı güncel metadata ile çözülür; kayıt içeriği/sürümü yeniden yazılmaz. Native SQLCipher/galeri/gerçek cihaz görsel kabulü bu turun export/test kanıtından ayrı tutulur.
