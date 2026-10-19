# Web sitesi CMS API'si (İŞ21 / WEBSITE FAZ 02)

Kaynak: `backend/src/BuildingFlow.Api/WebsiteEndpoints.cs`, `PublicWebsiteEndpoints.cs`, `Application/Website.cs`, `Infrastructure/WebsiteStore.cs`, `Infrastructure/PublicWebsiteStore.cs`. Tasarım ve kararlar: [docs/website-management](../website-management/design.md).

## Kararlar

- Property (yönetim alanı) başına en fazla bir site. Site anahtarı (`siteKey`) sistem genelinde tekildir, 3–63 karakter `a-z0-9-`, ayrılmış adlar (`api`, `admin`, `www`, `login` …) reddedilir.
- Public taraf yalnız **aktif yayın anlık görüntüsünü** (snapshot) okur. Taslak içerik, yönetim tabloları, kişi/borç/duyuru/doküman verisi anonim yoldan okunamaz; `WebsitePublicBoundaryTests` bunu kaynak kodu üzerinden doğrular.
- Yayın değişmezdir: `website_publications` satırı güncellenemez/silinemez (tetikleyici `bf_website_publication_guard`); yalnız `is_active` true → false olabilir. Geri yükleme yeni yayın numarası üretir.
- İçerik bloklarında HTML yoktur. Her blok türünün izinli alanları sabittir; bilinmeyen alan, kontrol karakteri, `<`/`>` ve `https://`, `/yol` veya `#bölüm` dışındaki bağlantılar reddedilir. Zengin metin (`RICH_TEXT.body`) en fazla 8000 karakter ve web tarafında kısıtlı markdown olarak işlenir.
- Yeni site, `[DOLDURUN]` yer tutuculu başlangıç sayfalarıyla (ana sayfa, KVKK aydınlatma, çerez politikası) oluşur; yer tutucu kalırsa yayın `BF_WEBSITE_PUBLISH_INCOMPLETE` ile reddedilir. Hukuki metni yönetici doldurur.
- `/api` altındaki `Cache-Control: no-store` korunur; public önbellek/CDN kararı FAZ 07'de verilecek. Public yanıt `ETag` (snapshot SHA-256) ve `If-None-Match` → 304 destekler.

## Veri modeli

| Tablo | Amaç | Önemli kısıtlar |
| --- | --- | --- |
| `website_sites` | Site kimliği, iletişim, durum, tema (şablon + iki renk). | Property başına tek (silinmemiş); `site_key` citext tekil; `request_id` tekil; durum `DRAFT/PUBLISHED/SUSPENDED`; renkler `#RRGGBB` ve beyaz yazıyla kontrast (birincil ≥4.5, vurgu ≥3). |
| `website_pages` | Sayfalar. | (site, slug) tekil; HOME/PRIVACY/COOKIES site başına tek ve silinemez; HOME slug boş; sıra 0–1000. |
| `website_sections` | Sayfa blokları (`jsonb`). | Tür listesi sabit; nesne olmalı, ≤16384 bayt; sayfa başına ≤30 blok. |
| `website_publications` | Değişmez yayın geçmişi. | (site, number) ve (site, request_id) tekil; site başına tek aktif; snapshot metin olarak tutulur (ETag baytları değişmesin diye). |

Tüm tablolar ortak 8 BaseEntity kolonunu (NOT NULL) taşır ve `(property_id, id)` bileşik FK ile Property dışına bağlanamaz. Migration `20261018063745_WebsiteCms`; Down, site veya yayın satırı varsa veri kaybını reddeder.

## Yönetim API'si — `/api/v1/properties/{propertyId}/website` (JWT)

Roller: okuma ve taslak düzenleme `SUPER_ADMIN`, `SITE_ADMIN`, `MANAGER` (kendi aktif Property üyeliği); site oluşturma, yayın listesi, yayınlama ve geri yükleme `SUPER_ADMIN`, `SITE_ADMIN`; askıya alma/yeniden açma (`/status`) yalnız `SUPER_ADMIN`. Başka Property her zaman 404 döner.

| Yöntem | Yol | Not |
| --- | --- | --- |
| GET | `` | Site, tema, sayfalar ve bloklar. |
| POST | `` | `{requestId, siteKey, displayName}`; aynı `requestId` ile tekrar = aynı yanıt (201→200). |
| PUT | `` | Ad, slogan, kurumsal e-posta/telefon, demo bayrağı; `version` zorunlu. |
| PUT | `/theme` | `{template: "PREMIUM_RESIDENCE", primaryColor, accentColor, version}`. |
| PUT | `/status` | `{status, version}` (PUBLISHED veya SUSPENDED); askıdaki site public'te 404 olur. |
| POST/PUT/DELETE | `/pages`, `/pages/{pageId}` | Silmede `?version=`. |
| PUT | `/pages/order` | Toplu sıra, her öğe sürümüyle. |
| POST/PUT/DELETE | `/pages/{pageId}/sections[/{sectionId}]` | Blok verisi türün şemasına uymalı. |
| PUT | `/pages/{pageId}/sections/order` | Toplu sıra. |
| GET | `/preview` | Yayınlanacak snapshot'ın aynısı (kaydetmeden). |
| GET/POST | `/publications` | Liste / `{requestId, expectedSiteVersion, note}` ile yayın. |
| POST | `/publications/{id}/restore` | Eski snapshot'ı yeni numarayla yeniden yayınlar. |

Eşzamanlılık: her yazma `version` ister, uyuşmazlık `BF_RECORD_CHANGED` (409). Yayın, site kilidi (`FOR UPDATE`) altında `expectedSiteVersion` ile kontrol edilir.

## Public API — `/api/public/v1/sites/{siteKey}` (anonim, salt okunur)

Yanıt yalnız beyaz listedeki alanlardır: `siteKey, displayName, tagline, contactEmail, contactPhone, isDemo, theme, pages[kind, slug, title, menuLabel, showInMenu, sortOrder, seoTitle, seoDescription, sections[type, data]]`. Gizli bloklar snapshot'a girmez. Bilinmeyen anahtar, taslak, askıda, pasif/silinmiş Property veya geçersiz anahtar biçiminin hepsi **aynı gövdesiz 404**'tür (site varlığı sızdırılmaz). Host başlığına güvenilmez; site yalnız yoldaki anahtarla seçilir.

## Hata kodları

`BF_WEBSITE_SITE_KEY_INVALID`, `BF_WEBSITE_DETAILS_INVALID`, `BF_WEBSITE_THEME_INVALID`, `BF_WEBSITE_PAGE_INVALID`, `BF_WEBSITE_SECTION_INVALID` (400, alan hatası); `BF_WEBSITE_STATE`, `BF_WEBSITE_SITE_EXISTS`, `BF_WEBSITE_SITE_KEY_TAKEN`, `BF_WEBSITE_PAGE_SLUG_TAKEN`, `BF_WEBSITE_PAGE_REQUIRED`, `BF_WEBSITE_PUBLISH_INCOMPLETE`, `BF_WEBSITE_REQUEST_CONFLICT` (409). TR/EN metinleri `frontend/buildingflow-web/messages/*.json` → `apiErrors`.

## Testler

- Birim: `WebsiteDomainTests` (anahtar, kontrast, sayfa/blok şemaları, bağlantı güvenliği, snapshot), `WebsitePublicBoundaryTests` (anonim yol başka tablo okuyamaz).
- Entegrasyon (ayrı test DB): `WebsiteApiTests` (rol/kapsam, idempotent oluşturma, sürüm çakışması, yayın/geri yükleme, ETag/304, tek tip 404), `WebsiteMigrationTests` (yükseltme veriyi korur, ortak kolonlar, değişmezlik tetikleyicisi, dolu Down reddi).

## WEBSITE FAZ 05 — yönetim ekranı ve medya (18.10.2026)

Yönetim rotaları `/website` (alan seçimi) ve `/properties/{id}/website`. Menü SUPER_ADMIN/SITE_ADMIN/MANAGER içindir; API rol ve aktif Property üyeliğini tekrar doğrular. MANAGER site oluşturamaz, yayınlayamaz veya yayın geçmişini okuyamaz; askı/açma yalnız SUPER_ADMIN. Sayfa/blok CRUD, sıralama, tema, ayarlar, içerik önizlemesi, yayın ve geri yükleme mevcut CMS API'sini kullanır. Önizleme yalnız yetkili oturumda içerik/görsel dökümüdür; birebir premium render veya public taslak URL değildir. 409'da girdiler korunur, güncel sürüm elle yüklenir ve ayrıca Kaydet gerekir. Create/publish/restore/upload aynı payload retry için UUID korur.

### Medya sözleşmesi

Yönetim kökü `/api/v1/properties/{propertyId}/website/media`: GET liste, POST multipart, PUT `/{id}`, DELETE `/{id}?version=`, GET `/{id}/content` (kimlikli, no-store). Site başına en fazla100 silinmemiş görsel. Multipart: `file, requestId, altText, rightsBasis, rightsNote, containsPeople, peopleConsentConfirmed`. PUT aynı beyanları ve version taşır. Hak türleri OWN_WORK/LICENSED/PERMISSION; son ikisinde kaynak/izin notu zorunlu. Tanınabilir kişi varsa peopleConsentConfirmed zorunlu. UI ayrıca kullanım hakkını onaylatır, yüz/plaka/daire bilgisi uyarısı gösterir. Otomatik yüz/plaka gizleme yoktur.

JPEG/PNG,1–5MiB,1–4096px; MIME/uzantı/imza/yapı kontrolü yapılır. JPEG mevcut ProfilePhoto parser'ı ile APP/EXIF/comment temizler. PNG BCL parser'ı CRC/zlib/piksel uzunluğu doğrular, metadata temizler; yalnız8-bit/non-interlaced PNG desteklenir. SHA256 temizlenmiş baytlardandır. Depo anahtarı `websites/{site:N}/media/{id:N}` sunucuda üretilir, dosya adı kullanılmaz. Upload Property kilidi ve UUID/fingerprint; düzenleme/silme version kontrollüdür. Silme metadata soft-delete'dir. Belirsiz commit'te nesne otomatik silinmez; orphan nesne uzlaştırma/temizliği henüz otomatik değildir ve üretim operasyonu öncesi ele alınmalıdır.

Tema PUT/yanıt nullable logoMediaId/heroMediaId taşır; tüm tema değerleri korunarak gönderilmelidir. AMENITIES/GALLERY öğeleri opsiyonel mediaId kabul eder. Aynı site/Property ve aktif medya şartı yazımda doğrulanır; temada ayrıca composite FK bulunur. Taslak veya herhangi bir yayın geçmişinde kullanılan medya silinemez; geri yükleme korunur. Metadata düzenleme eski snapshot'ı yeniden yazmaz.

Public snapshot opsiyonel `media[]`: id,contentType,width,height,sha256,altText. Hak beyanı/depo anahtarı/Property/audit/version açılmaz. Anonim GET `/api/public/v1/sites/{siteKey}/media/{id}/{sha}.{jpg|png}` yalnız aktif yayın referansı, yayınlı site, aktif/silinmemiş Property ve doğru sha/türde200; diğer durumlarda gövdesiz404. Ayrı public store sınırı birim testindedir. no-store korunur; CDN/cache kararı FAZ07'dedir.

Website `/media/{siteKey}/{id}/{sha}/{ext}` proxy'si sabit API origin'ine gider, kimlik/cookie iletmez, redirect reddeder;8sn timeout/5MiB/MIME/SHA256 doğrular. Public görseller optimizer cache'ine alınmaz; askı/yayından kaldırma her okumada kontrol edilir. Demo yerel görseller ayrı kalır.

Migration `20261018172114_WebsiteMedia`: BaseEntity8kolonlu website_media, nullable tema referansları, composite FK/check/UUID index. Down medya varsa veri kaybını reddeder. Canlı DB'ye bu tur uygulanmadı; API yeniden başlatıldığında mevcut AutoMigrate prosedürü işler. Yeni kodlar BF_WEBSITE_MEDIA_INVALID400/BF_WEBSITE_MEDIA_IN_USE409; FE/TR/EN eşleşir. Yeni paket yok.

## WEBSITE FAZ06 —19.10.2026
İlan ve başvuru uçları, yayın engeli ve işletim koşulları [listings.md](listings.md) içindedir. Public/özel başvuru depoları ayrıdır; önceki CMS/medya davranışı korunur.
