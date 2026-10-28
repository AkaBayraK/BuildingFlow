# Ortak Alanlar — OA FAZ 02 / Issue #52

Kaynak plan: [MASTER_PROMPT](../../BuildingFlow-OrtakAlanlar/MASTER_PROMPT.md), [FAZ02](../../BuildingFlow-OrtakAlanlar/FAZ-02.md). Bu sürüm **alan tanımı ve salt okunur katalog** sağlar. Slot, uygunluk, rezervasyon, onay, ücret tahsilatı ve bakım bağlantısı sonraki fazlardadır. `isEnabled` uygunluk veya boş kapasite değildir.

## Veri sözleşmesi

`CommonArea` Property'ye bağlı BaseEntity'dir. `common_areas` sekiz ortak NOT NULL audit/version kolonu, Property FK ve `(property_id, building_id)` → Building composite FK taşır. Apartmanda `buildingId=null`; sahte blok yoktur. Diğer alanlarda bina konumu isteğe bağlıdır. Konumun aynı Property'de aktif olması yazımda doğrulanır.

- `name`: trim edilmiş 1–150; `description`: nullable ≤2000; `rules`: nullable düz metin ≤8000. Ham HTML render edilmez. Aynı isimli birden çok fiziksel tesis desteklenir.
- `type`: POOL/FITNESS/MEETING_ROOM/PLAYGROUND/BARBECUE/SPORTS_COURT/EVENT_HALL/OTHER.
- `visibility`: PROPERTY (alanın tüm uygun bağlamları) veya BUILDING (yalnız `buildingId` ile aynı bloktaki daire bağlamı). Fiziksel konum ile görünürlük ayrıdır; BUILDING seçimi blok gerektirir. Çoklu blok allowlist bu sürümde yoktur.
- `usage`: OPEN_USE/RESERVATION/APPROVAL. Yalnız tanımdır; bu sürüm rezervasyon veya onay oluşturmaz.
- `capacity`: 1–10000 kişi, tanım metadatası; eşzamanlı kapasite motoru değildir.
- `opensAt`, `closesAt`: ikisi null (saat henüz tanımlanmamış) veya dakika hassasiyetinde `HH:mm`, açılış < kapanış. Europe/Istanbul yerel saatidir. Null “24 saat açık” vaadi değildir. Geceyi aşan/haftalık/istisnai saatler ve gece yarısı slotları bu fazda uygulanmaz.
- `feeAmount`: API string, DB numeric(10,2), 0–99999999.99, en fazla iki ondalık hassasiyet; normalize yanıt `0.00`. `currency=TRY`. Bu tutar kullanım için tanımlanan referans ücrettir; fiyat teklifi, otomatik borç veya ödeme değildir. Saat/kişi bazlı ücret, vergi, depozito ve iade bu sürümde yoktur; FAZ07 sözleşmesi ayrıca gerekir.
- `isEnabled`: aktif/pasif; audit sunucudan, `version` optimistic concurrency. `createdBy/modifiedBy/fingerprint` API'ye verilmez. DTO kimlik, Property, requestId, tanım, createdAt/modifiedAt ve replay durumunu döndürür.

## Yönetim API

`/api/v1/properties/{propertyId}/common-areas`

| İstek | Sonuç |
|---|---|
| GET `?page=1&pageSize=20&search=&type=&buildingId=&isEnabled=` | `PagedRows`: items/page/pageSize/totalCount; sayfa1–1000000, boyut1–100; literal ad arama≤200; sıra name/id; filtreler sunucudadır. Pasif kayıtlar yöneticiye görünür. Soft-delete kayıtlar görünmez. |
| GET `/{areaId}` | Aynı kapsamdaki tanım, aksi404 |
| POST | 201+Location; aynı normalize oluşturma içeriğinin aynı UUID replay'i200 ve `replayed=true` |
| PUT `/{areaId}` | Aynı `requestId` + beklenen pozitif version;200 güncel kayıt; stale409 |
| DELETE `/{areaId}?version=` | version kontrollü soft-delete204; geçmiş fiziksel silinmez |

Gövde `CommonAreaRequest`: requestId/name/type/buildingId/visibility/usage/capacity/description/rules/opensAt/closesAt/feeAmount/currency/isEnabled/version. POST version0, PUT version≥1. RequestId UUID zorunludur; oluşturma sırasında atanır, güncellemede değişmez. POST varsayılan isEnabled=true, ücret0.00/TRY.

Rol: SUPER_ADMIN/SITE_ADMIN/MANAGER. ACCOUNTANT ve salt sakin rollerine yönetim uçları403. Kimlikli rol tek başına yeterli değildir: legacy modda aktif PropertyMembership; mevcut tenant mapping varsa ManagementAccess politikası; SessionContexts açıkken mevcut `PropertyAllowedAsync(...,"MANAGER")` ile güncel tenant, issuer/subject, kişi–hesap bağı, üyelik, tarihli yönetim görevi ve gerçek JWT rolü doğrulanır. SUPER_ADMIN mevcut platform kuralını izler. İstemci tenant kimliği yetki sayılmaz. Gizli/yabancı scope veya kaynak404. Yazım aktif Property ister; pasif parent409 veya canonical yetki kapsamında404.

Property transaction kilidi altında yetki yeniden okunur. Yazım parent version'ını ve seçilen blok version'ını da yükseltir; eşzamanlı parent değişiklikleri sessizce kazanmaz. Oluşturma `(Property,requestId)` tekil; normalize fingerprint issuer/aktör/Property/içerik içerir. Aynı UUID farklı içerik/aktör409, silinmiş kayda replay409. Aynı oluşturma isteği daha sonra tekrarlandığında **güncel** tanım döner; eski değerler geri yazılmaz. PUT ve DELETE kendiliğinden replay edilmez; kayıp yanıt sonrası GET/409 kurtarma gerekir.

## Kullanıcının daire bağlamından okuma

`GET /api/v2/me/units/{unitId}/common-areas` ve `/{areaId}`; query: `propertyId`, `capacity=OWNER|TENANT|RESIDENT`, `relationId`; listede page/pageSize/search/type. Mevcut PersonRefactor Enabled+SessionContextsEnabled kapıları korunur; kapalıysa404.

Backend mevcut `IMyPortfolioStore.ResolveAsync` ile aktörün **kendi güncel** ilişkisinden context çözer. Tenant/Property/Unit ve bağlı hesap/kişi/üyelik/tarih kapsamı korunur. Bitiş günü iki uç dahil; bitmiş/pasif/yabancı ilişki404. Aynı Property'nin başka dairesi için bir ilişki kimliği ödünç kullanılamaz. Sonuç yalnız aktif ve silinmemiş tanımlar; blok konumu varsa o blok da aktif/silinmemiş olmalı. BUILDING görünürlüğü sunucudaki Unit.buildingId ile eşleşir. Liste/detay aynı predicate'i kullanır, sonuç yoksa detay404. Yanıtlar mevcut API `no-store` güvenlik katmanını kullanır.

OWNER/TENANT/RESIDENT'e bu sürümde yalnız tanım okuması verilir. **26.10.2026 kullanıcı kararı:** rezervasyon oluşturma/düzenleme/iptal yalnız yetkili yöneticidedir; yönetici dışı kullanıcılar web/merkez ve mobilde yalnız yetkili oldukları alanı ve saat takvimini görüntüler. ACCOUNTANT yönetici sayılmaz. FAZ04'te saat takvimi boş/dolu/kapalı durumunu sunacak; başkasının adı, telefonu, açıklaması veya ödeme bilgisi sunulmayacak. Bu faz henüz slot takvimi sağlamaz; kullanıcıya rezervasyon/talep gönderme veya ödeme hakkı açılmaz. Client kapsamı veya JWT rolü elle değiştirilerek tanım erişimi kazanılamaz.

## Galeri (OA FAZ03)

Yönetim: `GET/POST /api/v1/properties/{propertyId}/common-areas/{areaId}/photos`, `DELETE .../photos/{photoId}?version=`, `GET .../photos/{photoId}/download`. POST gövdesi `{documentId, areaVersion, rightsConfirmed}`: önce mevcut belge yükleme ile aynı Property'de JPEG/PNG (≤10 MiB) StoredDocument oluşturulur, sonra alan version kontrolüyle bağlanır; hak beyanı zorunludur. Daire bağlamı: `GET /api/v2/me/units/{unitId}/common-areas/{areaId}/photos` ve `/download` aynı context query'leriyle yalnız okur. İndirme JWT kontrollü, `no-store`, SHA256/boyut doğrulamalı. `common_area_photos` Property+alan ve Property+belge composite FK taşır; kaldırma soft-delete'tir. Web ekranı: [03 raporu](../common-areas/03-report.md).

## Hatalar, tüketiciler ve veri yaşam döngüsü

`BF_COMMON_AREA_INVALID`400; `BF_COMMON_AREA_CONFLICT`409; mevcut `BF_RECORD_CHANGED`, `BF_PARENT_INACTIVE`, 401/403/404/429/500 kataloğu korunur. FE error-codes + TR/EN çevirileri birlikte eklenmiştir; ham exception/message render edilmez. Web/mobil iş ekranı FAZ03/06'dadır; mevcut menü placeholder'ı açılmamıştır. Yeni API geriye uyumlu eklemedir, eski bakım/finans DTO'ları değişmez.

Ortak alan geçmişi (pasif/soft-delete dahil) Property veya bağlı Building'in silinmesini engeller. Parent pasifleştirmede kullanıcı okuması kapanır; tanım geçmişi korunur. Yeni modül için rezervasyon geçmişi guard'ı henüz gerekmez; rezervasyon fazı kendi ilişkileriyle eklemelidir. Alan tanımı düzenlenebilir, audit son değişikliği gösterir; geçmiş rezervasyonların kural/fiyat snapshot'ı gelecekte tanımlanmalıdır.

Migration `20261026125717_CommonAreaDefinitions` yalnız yeni tablo/index/FK/check ekler; eski migration/satırlar değişmez. Designer ve snapshot birlikte bulunur. Down, tek bir common_areas kaydı bile varsa (soft-delete dahil) ilk drop'tan önce durur. Boş Down/reapply desteklenir; dolu üretimde ileri düzeltme veya yeni izole hedefe restore gerekir. Canlı DB'ye bu tur uygulanmadı; AutoMigrate açık sonraki API başlangıcı bekleyen migration'ı uygulayabilir.

[Bu turun kanıtı ve sınırlamaları](../common-areas/02-report.md), [test kayıtları](../TESTING.md).

## FAZ05 — Mevcut bakım planı ile kapanış

Yönetici uçları `/api/v1/properties/{propertyId}/common-areas/{areaId}/closures`:

- GET `?date=yyyy-MM-dd`: o günün aktif ve kaldırılmış kapanış geçmişi; yalnız yönetici.
- POST: `requestId`, `maintenancePlanId`, `maintenanceVersion`, `date`, `startsAt`, `endsAt`, `notice`. Mevcut aynı Property aktif PLANNED/IN_PROGRESS plan seçilir; plan günüyle aynı tek günlük15dk aralık. Plan oluşturulmaz/kopyalanmaz. UUID replay güncel sonucu döndürür. Çakışan rezervasyon/kapanış veya değişmiş plan409; bildirimi/duyuruyu kısmen kaydetmez.
- POST `/{closureId}/release`: `version`, `reason`; gerekçeli tek kaldırma, history silinmez.

Kapanış/rezervasyon aynı Property kilidini kullanır. Rezervasyon POST/PUT kapanışta409 `BF_COMMON_AREA_MAINTENANCE_CONFLICT`; bozuk istek400 `BF_COMMON_AREA_MAINTENANCE_INVALID`. Bakım tamamlanma/iptal/soft-delete aynı transaction'da ilgili kapanışları kaldırır. Aktif kapanış varken bakım günü sessiz değiştirilemez. Eski rezervasyon otomatik iptal edilmez. Takvimin mevcut busy DTO'suna yalnız `state=CLOSED,reserved=0` eklendi; sakine bakım/kişi/not bilgisi eklenmedi.

Duyuru mevcut Announcement/MAINTENANCE'dır; Property-wide ise ALL_MEMBERS, blok sınırlıysa MANAGEMENT. Kişisel kutu mevcut legacy Person+aktif üyelik+bağlı hesap kapsamındadır, blok sınırlı duyuru bu geniş kanala verilmez. Canonical-only hesapta kişisel kutu kaydı yoksa takvim/duyuru tüketilir; yeni kişi veya bildirim platformu uydurulmaz. `notificationCount` DB'de yazılan kümülatif kayıt sayısıdır; gerçek cihaz teslimi/okundu garantisi değildir. Gerçek SMS/e-posta/push yoktur. [Kabul ve sınırlar](../common-areas/05-report.md).


## OA-06 mobil istemci — 27.10.2026

Mobil seçili doğrulanmış context ile aynı v1 yönetim/v2 kendi-daire API'lerini kullanır; yeni backend endpoint yok. Yönetici alan oluşturma/düzenleme, rezervasyon oluşturma/version+gerekçeyle iptal online; diğer kullanıcılar yalnız yetkili daireden katalog ve FREE/PARTIAL/FULL/CLOSED takvimi okur. ACCOUNTANT yönetim yetkisi kazanmaz. Saf DTO/rezervasyon sözleşmesi `shared/common-areas.ts` üzerinden web ile ortaktır. Native cache hesap+context ve son liste/alan-tarih slotlarına bağlı,24saat üst sınır ve görünür zaman damgası taşır; offline yetki süresi ayrıca geçerlidir. Taslak kişi/daire/not tutmaz, UUID korunur, otomatik outbox gönderimi yok; online onay gerekir.403/404 kopya/taslağı kaldırır. Web kalıcı cache tutmaz. Alan-blok atama/galeri yönetimi merkezde; mobil gelişmiş rezervasyon düzenleme/ödeme yok. Canlı API/native kabul bu tur yapılmadı. Kanıt: [OA06 raporu](../common-areas/06-report.md).


## OA-07 yönetici takvimi / rapor — 27.10.2026

`GET /api/v1/properties/{propertyId}/common-areas/{areaId}/report?from=YYYY-MM-DD&to=YYYY-MM-DD` yalnız üç yönetici rolü + mevcut aktif Property/tenant kapsamına açıktır; sakin uçlarına eklenmedi. İki uç dahil2000–2100/en fazla93gün;100000grup üzeri400, anonim401/rol403/kapsam404. RepeatableRead içinde alan ve gruplu rezervasyon/aktif kaldırılmamış bakım okunur; özel not/Unit/Person seçilmez. Yanıt scope,from/to/asOf/areaVersion,capacity/opensAt/closesAt,currentConfiguration=true,paymentSupported=false,measuredDays,days[].{date,metrics},totals taşır. Metrics: confirmed,cancelled,openingMinutes,closedMinutes,availableMinutes,bookedMinutes,personMinutes,capacityMinutes,occupancyPercent,capacityUsePercent. İptal süreye girmez; booking/bakım süreleri union ve saate kırpılır, bakım uygun süreden düşer; kişi-dakika eşzamanlı yükü toplar. Yüzdeler toplam dakikadan iki ondalık/yarımda sıfırdan uzağa; saat yoksa süre null, sıfır payda oran null. Sayfalama/durum filtresi yalnız liste içindir.

Bu mevcut saat/kapasite tanımından hesaplanan rezervasyon raporudur; tarihsel kapasite/pasiflik ve fiilî kullanım ölçümü değildir. Geçmiş kapasite düşerse kişi oranı%100üstü olabilir. Finans/gelir raporu veya tahsilat yok: ödeme yalnız borçChargeId sözleşmesinde ve mevcut adapter Sandbox; gerçek sağlayıcılar planda, ortak alan ücret-borç snapshot bağı yok. Web yönetici PUT düzenleme mevcutRequestId/UnitId/version korur,409yenidenyükler; iptal version+gerekçe ile geçmişi korur. Yeni bekleyen sakin talebi/PENDING/onay entity'si yok; yöneticinin online kaydı onaydır. Yeni migration yok. [Kanıt ve sınırlar](../common-areas/07-report.md).
