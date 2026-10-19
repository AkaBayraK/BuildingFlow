# WebsiteManagement — domain ve veritabanı tasarımı (WEBSITE FAZ 01)

> FAZ 05 uygulama notu (18.10.2026): WebsiteMedia, nullable tema logo/hero referansları, aynı site medya doğrulaması ve yönetim ekranları uygulandı. Güncel sözleşme ve PNG/önizleme/orphan operasyon sınırları [API belgesinde](../api/website.md); test kanıtı TESTING.md içinde. Eski FAZ 01/02 paragrafları tarihsel tasarım/uygulama kaydıdır.


Durum: tasarım, kodlanmadı · 18.10.2026 · GitHub [#21](https://github.com/AkaBayraK/BuildingFlow/issues/21) · Yol haritası T21.1
İlgili: [analysis.md](analysis.md), [api.md](api.md), [security.md](security.md), [adr.md](adr.md), [test-plan.md](test-plan.md)

> **FAZ 02 uygulama notu (18.10.2026):** `WebsiteCms` migration'ı yalnız `website_sites`, `website_pages`, `website_sections`, `website_publications` tablolarını ekledi. Tema ayrı `website_themes` tablosu yerine site satırındaki `template`, `primary_color`, `accent_color` kolonlarında (site başına tek tema olduğu için ek tablo gereksizdi). `website_media` FAZ 05, `website_domains` FAZ 07, `website_public_notices` ve ilan tabloları FAZ 06 migration'larına kaldı. `/api` altındaki `Cache-Control: no-store` korunuyor, public önbellek kararı FAZ 07'de. Uygulanan kontrat: [docs/api/website.md](../api/website.md).

## 1. Onaylanan varsayılanlar (FAZ 00 karar soruları)

| # | Karar | Uygulanan |
| --- | --- | --- |
| 1 | Site sahibi | **Property başına en fazla bir site** (`website_sites.property_id` tekil). PersonTenant bağı Property üzerinden türetilir, ayrıca saklanmaz. |
| 2 | İlanı kim girer | SITE_ADMIN/MANAGER taslak girer, **yalnız SITE_ADMIN/SUPER_ADMIN onaylar**; malik onayı belge/beyan olarak kaydedilir. Malik self-servis sonraki iterasyon. |
| 3 | Alan adı | FAZ 03–06: tek platform host'u altında yol tabanlı `/{siteKey}`. FAZ 07: doğrulanmış alt alan/özel alan adı (ters vekil kararı ile). |
| 4 | İçerik/görsel | Demo içerik "DEMO" etiketli; gerçek görseller yöneticinin hak beyanıyla yüklenir. |
| 5 | Görsel işleme | Yeni paket yok: JPEG/PNG, ≤ 5 MiB, ≤ 4096×4096 px, metadata temizliği projeye ait kodla; yeniden boyutlandırma/WebP yok. |

## 2. Kavramsal model

```text
Property (mevcut) 1 ── 0..1 WebsiteSite
WebsiteSite 1 ── 1 WebsiteTheme
            1 ── * WebsitePage 1 ── * WebsiteSection
            1 ── * WebsiteMedia
            1 ── * WebsiteDomain
            1 ── * WebsitePublication   (değişmez snapshot; en fazla bir aktif)
            1 ── * WebsitePublicNotice  (iç Announcement'tan bağımsız)
            1 ── * PropertyListing ── 0..1 Unit (mevcut, aynı Property)
PropertyListing 1 ── * PropertyListingPhoto ── 1 WebsiteMedia
                1 ── * PropertyListingEvent (değişmez onay/yayın günlüğü)
WebsiteSite 1 ── * WebsiteInquiry ── 0..1 PropertyListing
```

Bütün tablolar `BaseEntity` (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled — NOT NULL) ve `BaseEntityConfiguration.Configure` ile eşlenir. Her alt tablo `property_id` taşır; üst kayda `(property_id, site_id)` / `(property_id, id)` composite FK ile bağlanır, böylece bir kayıt başka Property'nin sitesine bağlanamaz (mevcut Announcement/StoredDocument deseni). Tüm FK'ler `Restrict`; silme soft-delete.

## 3. Entity'ler

### WebsiteSite — `website_sites`
| Alan | Tip | Kural |
| --- | --- | --- |
| property_id | uuid FK properties | partial unique (`is_deleted=false`) |
| site_key | citext(63) | `^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$`, global tekil, rezerve kelimeler (`api`, `admin`, `www`, `login`, `static`, `_next`) yasak |
| display_name | varchar(120) | zorunlu |
| tagline | varchar(200) | opsiyonel |
| status | varchar(20) | `DRAFT`, `PUBLISHED`, `SUSPENDED` |
| default_locale | varchar(5) | `tr` (ilk sürüm yalnız TR) |
| contact_email / contact_phone | varchar | yalnız kurumsal yönetim ofisi; kişi adı içeremez uyarısı UI'da |
| is_demo | bool | true ise tüm public sayfalarda "Demo içerik" bandı |

`SUSPENDED`: Property pasif/silinmiş veya SUPER_ADMIN askıya aldı → public 404. Alternate key `(property_id, id)`.

### WebsiteTheme — `website_themes` (site başına tek)
template (`PREMIUM_RESIDENCE`), primary_color/accent_color (`#RRGGBB`, kontrast ≥ 4.5:1 doğrulaması), logo_media_id (aynı site medyası), font_preset (`SYSTEM`), hero_media_id.

### WebsitePage — `website_pages`
kind (`HOME, ABOUT, UNITS, AMENITIES, NOTICES, GALLERY, CONTACT, PRIVACY, COOKIES, CUSTOM`), slug (`^[a-z0-9-]{1,60}$`, site içinde tekil, HOME için boş), title (120), menu_label (40), show_in_menu, sort_order, seo_title (70), seo_description (160). `PRIVACY` ve `COOKIES` silinemez (KVKK).

### WebsiteSection — `website_sections`
page_id (composite FK), type (`HERO, FEATURES, LISTINGS, NOTICES, AMENITIES, GALLERY, STATS, CONTACT, LOGIN_CARD, RICH_TEXT, VIDEO_LINK`), sort_order, is_visible, data `jsonb` (≤ 16 KiB). `data` her tip için Application katmanındaki kapalı şemayla doğrulanır: bilinmeyen alan reddedilir, HTML yok, metinler düz metin veya sınırlı markdown alt kümesi (kalın, italik, liste, bağlantı; bağlantı yalnız `https:` veya site içi yol), medya referansları yalnız aynı sitenin `website_media` kimlikleri. STATS değerleri elle girilen metindir; veritabanından hesaplanmaz.

### WebsiteMedia — `website_media`
| Alan | Kural |
| --- | --- |
| content_type | `image/jpeg`, `image/png` (SVG/GIF/WebP yok) |
| size | 1 B – 5 MiB |
| width/height | 1–4096 |
| sha256 | 64 hex, temizlenmiş içeriğin özeti |
| object_key | `websites/{site:N}/media/{id:N}` (sunucu üretir, kullanıcı adı yok) |
| alt_text | zorunlu, 1–200 (erişilebilirlik) |
| rights_basis | `OWN_WORK`, `LICENSED`, `PERMISSION` |
| rights_note | ≤ 500, LICENSED/PERMISSION için zorunlu |
| contains_people | bool, yükleyen beyan eder; true ise `people_consent_confirmed` zorunlu |
| visibility | `PUBLIC_WHEN_REFERENCED` (yalnız aktif yayın/yayınlı ilan referansıyla public) |

### WebsiteDomain — `website_domains` (tablo FAZ 02'de, kullanım FAZ 07'de)
host_name (citext, IDN punycode, küçük harf, port yok, global partial unique), kind (`PLATFORM_SUBDOMAIN`, `CUSTOM`), status (`PENDING`, `VERIFIED`, `DISABLED`), verification_token_hash (sha256; düz token yalnız oluşturma yanıtında bir kez), verified_at, last_checked_at, is_primary (site başına bir, partial unique).

### WebsitePublication — `website_publications`
number (site içinde artan), snapshot `jsonb` (yalnız public DTO; iç kimlikler hariç medya kimliği), snapshot_sha256, published_at, note (≤ 200), is_active (site başına partial unique `is_active=true`), source_publication_id (geri alma kaynağı). **Değişmez**: oluşturulduktan sonra yalnız `is_active` değişir. Geri alma = eski snapshot'tan yeni numaralı yayın.

### WebsitePublicNotice — `website_public_notices`
title (120), summary (300), body (≤ 5000, sınırlı markdown), priority (`NORMAL`, `IMPORTANT`), notice_date (DateOnly), expires_on (opsiyonel), media_id (opsiyonel), status (`DRAFT`, `PUBLISHED`, `WITHDRAWN`), published_at, source_announcement_id (opsiyonel composite FK; yalnız kopyalama kaynağı, içerik bağlı değil). İç `Announcement` tablosu public API tarafından **hiç okunmaz**.

### PropertyListing — `property_listings` (FAZ 06 migration)
| Alan | Kural |
| --- | --- |
| unit_id | opsiyonel composite FK `(property_id, unit_id)` → units; ilan sahiplik/kiracılığı değiştirmez |
| kind | `SALE`, `RENT` |
| title (120), description (≤ 4000 sınırlı markdown) | |
| price | numeric(14,2) > 0, currency `TRY`; RENT için `price_period = MONTHLY` |
| room_layout | `^\d{1,2}\+\d$` (ör. 3+1) |
| gross_area / net_area | numeric(8,2) > 0, net ≤ brüt |
| floor_label, building_label | public gösterim metni (ör. "A Blok · 5. Kat") |
| features | jsonb kod dizisi (`CLOSED_PARKING`, `BALCONY`, `POOL_VIEW`…) kapalı katalog |
| status | `DRAFT → PENDING_APPROVAL → APPROVED → PUBLISHED → WITHDRAWN/EXPIRED`, `REJECTED` (taslağa döner) |
| owner_consent_kind | `WRITTEN_STATEMENT`, `EIDS_VERIFIED`, `AUTHORIZED_AGENT` |
| owner_consent_document_object_key | özel nesne (public değil), PDF/JPEG/PNG ≤ 10 MiB |
| agent_license_no | AUTHORIZED_AGENT için zorunlu (Taşınmaz Ticareti yetki belgesi no) |
| compliance_checked_at / compliance_checked_by / compliance_note | onay için zorunlu |
| publish_until | yayından en fazla 90 gün; Worker süresi dolanı `EXPIRED` yapar |

Public'te asla: malik/kiracı adı, telefon, e-posta, unit_id, kişi kimliği, borç bilgisi.

### PropertyListingPhoto — `property_listing_photos`
listing_id, media_id (aynı site), sort_order, is_cover (ilan başına bir). En fazla 20 fotoğraf.

### PropertyListingEvent — `property_listing_events` (değişmez)
action (`SUBMIT, APPROVE, REJECT, PUBLISH, WITHDRAW, EXPIRE, EDIT_AFTER_APPROVAL`), actor_sub (audit'ten), reason (≤ 500), listing_version. Silinmez/güncellenmez.

### WebsiteInquiry — `website_inquiries` (FAZ 06 migration, T21.10)
listing_id (opsiyonel), full_name (100), phone/email (en az biri), message (≤ 2000), privacy_notice_version (aydınlatma metni sürümü), created_ip_hash (HMAC, ham IP saklanmaz), status (`NEW, HANDLED, SPAM`), retain_until (varsayılan +180 gün; Worker anonimleştirir). Yalnız SITE_ADMIN/MANAGER görür; ACCOUNTANT görmez.

## 4. Durum makineleri

```text
Site:       DRAFT --publish--> PUBLISHED --suspend--> SUSPENDED --resume--> PUBLISHED
Listing:    DRAFT --submit--> PENDING_APPROVAL --approve--> APPROVED --publish--> PUBLISHED
            PENDING_APPROVAL --reject(reason)--> DRAFT
            PUBLISHED --withdraw(reason)--> WITHDRAWN ; PUBLISHED --(publish_until)--> EXPIRED
            APPROVED/PUBLISHED içerik/fiyat düzenlemesi --> PENDING_APPROVAL (yeniden onay)
Notice:     DRAFT --publish--> PUBLISHED --withdraw--> WITHDRAWN
```

Her geçiş `version` beklenen değeriyle yapılır (409 yarış). Yayınlanmış ilan/duyuru/yayın geçmişi silinmez.

## 5. Veritabanı ve migration planı

| Migration | Faz | Tablolar |
| --- | --- | --- |
| `WebsiteCms` | 02 | website_sites, website_themes, website_pages, website_sections, website_media, website_domains, website_publications, website_public_notices |
| `PropertyListings` | 06 | property_listings, property_listing_photos, property_listing_events, website_inquiries |

- Mevcut tablo değişmez; yalnız yeni tablo, index, check constraint.
- Partial unique: site/property, site_key, page slug, aktif yayın, birincil domain, kapak fotoğrafı.
- Check: renk biçimi, boyut/piksel sınırları, fiyat > 0, RENT⇔MONTHLY, durum kümeleri, jsonb boyutu (`pg_column_size(data) <= 16384`).
- Index: `(property_id, status)`, `(site_id, sort_order)`, `(status, publish_until)` (Worker).
- Down: tablo doluysa `MigrationSql` guard ile durur (mevcut Down guard deseni); veri otomatik geri getirilmez.
- Doğrulama: ilk kurulum, mevcut veriyle yükseltme, tekrar uygulama ve idempotent SQL (`check-idempotent-sql.sh`) ayrı PostgreSQL şemasında; model/şema sözleşme testleri yeni tabloları kapsar.

## 6. Nesne depolama

Mevcut SeaweedFS özel bucket ve `IFileStorage` soyutlaması (`Application/Abstractions.cs`) kullanılır; yeni bucket gerekmez. Anahtarlar `websites/{site:N}/media/{id:N}` ve `websites/{site:N}/listing-consents/{id:N}`. DB ve nesne dağıtık transaction değildir: önce nesne yazılır, sonra metadata; metadata commit edilemezse yetim nesne Worker'ın günlük temizliğiyle (24 saatten eski, metadata'sız) silinir — belirsiz commit durumunda otomatik silme yapılmaz (mevcut 08 kuralı).

## 7. Audit

Oluşturma/değişiklik audit'i mevcut `AuditInterceptor` ile. Ek olarak değişmez günlükler: `website_publications` (kim, ne zaman, hangi snapshot), `property_listing_events`. Medya hak beyanı `created_by` + `rights_basis` ile kanıtlanır.

## WEBSITE FAZ06 uygulama notu —19.10.2026
Güncel sözleşme ve kabul sınırları [../api/listings.md](../api/listings.md). EIDS_VERIFIED kullanıcı beyanı uygulanmadı; varsayılan sunucu doğrulayıcısı yayın kapısını kapatır. Gerçek EİDS/hukuki kabul yoktur. Başvuru imzalı challenge kullanır, elapsedMs beyanı kabul edilmez. Privacy sürümü aktif publication SHA sürümüdür. Anonim/yönetim depoları ayrıdır. Proxy istemci IP bilgisini aktarmaz; IP kotası proxy arkasında paylaşılabilir. FAZ06 ve T21.10 tam işletim kabulü henüz tamamlanmadı.
