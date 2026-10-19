# WebsiteManagement — API kontratı (WEBSITE FAZ 01 tasarımı)

Durum: tasarım. Uygulama FAZ 02 (CMS), FAZ 05 (medya), FAZ 06 (ilan/başvuru), FAZ 07 (domain). Kodlandığında `docs/api/website.md` ve `docs/api/listings.md` bu belgeden üretilir.

Ortak kurallar: JSON camelCase; hata `ProblemDetails` + ErrorCatalog kodu; 400/401/403/404/409/429 mevcut anlamlarıyla. Yazımlarda `version` (optimistic concurrency) zorunlu; oluşturmada `requestId` (UUID) + normalize fingerprint ile replay (aynı içerik → aynı sonuç, farklı içerik → 409), mevcut finans/belge deseni. Liste uçları `page`, `pageSize` (≤ 50), `search`, izinli `sort`.

## 1. Yönetim API'si (JWT zorunlu)

Kök: `/api/v1/properties/{propertyId}/website` — Property üyeliği/görevi backend'de doğrulanır; kapsam dışı kayıt **404** (varlık sızdırılmaz).

| Metot ve yol | Policy | Not |
| --- | --- | --- |
| `GET /` | CanManageWebsite | Site + tema + sayfa özeti; site yoksa 404 |
| `POST /` | CanPublishWebsite | `{requestId, siteKey, displayName, template}` → 201; varsayılan sayfalar (HOME, PRIVACY, COOKIES…) ve demo içerik |
| `PUT /` | CanManageWebsite | ad, tagline, kurumsal iletişim, isDemo, version |
| `PUT /theme` | CanManageWebsite | renk, logo/hero medya, version |
| `GET/POST /pages`, `PUT/DELETE /pages/{pageId}` | CanManageWebsite | PRIVACY/COOKIES silinemez (409 `BF_WEBSITE_PAGE_REQUIRED`) |
| `PUT /pages/order` | CanManageWebsite | `[{id, sortOrder, version}]` atomik |
| `GET/POST /pages/{pageId}/sections`, `PUT/DELETE /pages/{pageId}/sections/{sectionId}` | CanManageWebsite | `data` tip şemasıyla doğrulanır |
| `GET /media`, `POST /media` (multipart), `PUT/DELETE /media/{mediaId}` | CanManageWebsite | yükleme rate-limit; referanslı medya silinemez (409) |
| `GET /media/{mediaId}/content` | CanManageWebsite | `no-store`, yönetim önizlemesi |
| `GET /preview` | CanManageWebsite | taslaktan public DTO üretir (kaydetmez) |
| `POST /preview-token` | CanManageWebsite | 10 dk geçerli HMAC önizleme bağlantısı (website uygulaması için) |
| `GET /publications`, `POST /publications` | CanPublishWebsite | `{requestId, note, expectedSiteVersion}` → yeni değişmez snapshot, aktif yapar |
| `POST /publications/{id}/restore` | CanPublishWebsite | eski snapshot'tan yeni yayın (geri alma) |
| `PUT /status` | SUPER_ADMIN | SUSPENDED / PUBLISHED |
| `GET/POST /notices`, `PUT /notices/{id}`, `PUT /notices/{id}/publish`, `PUT /notices/{id}/withdraw` | CanManageWebsite | `POST` gövdesinde opsiyonel `copyFromAnnouncementId` (yalnız kopya) |
| `GET/POST /domains`, `DELETE /domains/{id}`, `POST /domains/{id}/verify` | CanPublishWebsite (CUSTOM onayı SUPER_ADMIN) | FAZ 07 |

### İlanlar — `/api/v1/properties/{propertyId}/listings` (FAZ 06)

| Metot ve yol | Policy |
| --- | --- |
| `GET /`, `GET /{id}` | CanManageListings |
| `POST /`, `PUT /{id}`, `DELETE /{id}` (yalnız DRAFT) | CanManageListings |
| `PUT /{id}/photos` | CanManageListings |
| `POST /{id}/owner-consent` (multipart belge + tür) | CanManageListings |
| `POST /{id}/submit` | CanManageListings |
| `POST /{id}/approve` `{version, complianceConfirmed:true, complianceNote}` | CanApproveListings |
| `POST /{id}/reject` `{version, reason}` | CanApproveListings |
| `POST /{id}/publish`, `POST /{id}/withdraw` `{version, reason}` | CanApproveListings |
| `GET /{id}/events` | CanManageListings |
| `GET /api/v1/properties/{propertyId}/website/inquiries`, `PUT …/{id}` (durum) | CanManageListings |

## 2. Public API (anonim, salt okunur + tek form)

Kök: `/api/public/v1/sites/{siteKey}`. Ayrı endpoint dosyası, ayrı DTO'lar (yönetim DTO'su yeniden kullanılmaz), ayrı anonim rate-limit bölümü, CORS yalnız website origin'i. Site yok / DRAFT / SUSPENDED / Property pasif → aynı genel **404**.

| Metot ve yol | Yanıt |
| --- | --- |
| `GET /` | aktif yayın snapshot'ı: site adı, tema, menü, sayfalar, bölümler; `ETag` = snapshot_sha256, `Cache-Control: public, max-age=60` |
| `GET /notices?page=` | PUBLISHED ve süresi geçmemiş public duyurular |
| `GET /listings?kind=&page=` | PUBLISHED ilan kartları (başlık, tür, fiyat, oda, m², kat/blok etiketi, kapak) |
| `GET /listings/{listingId}` | ilan detayı; iletişim yalnız "yönetime sor" formu |
| `GET /media/{mediaId}/{sha256}.{jpg|png}` | yalnız aktif yayında veya yayınlı ilanda referanslı medya; `Cache-Control: public, max-age=31536000, immutable`, `nosniff`, `Content-Disposition: inline` |
| `POST /inquiries` | `{listingId?, fullName, phone?, email?, message, privacyNoticeVersion, website(honeypot boş), elapsedMs}` → 202 (gövde döndürmez) |
| `GET /api/public/v1/domains/{host}` | FAZ 07: yalnız VERIFIED kayıt için `{siteKey}`; aksi 404 |

Önizleme: `GET /api/public/v1/preview/{token}` — token HMAC imzalı, site kimliği + bitiş süresi; yanıt `no-store`, `X-Robots-Tag: noindex`.

## 3. Public DTO sınırları (kara liste değil, beyaz liste)

Snapshot ve public yanıtlar yalnız şu alanları içerir: site adı/tagline/kurumsal iletişim, tema, sayfa/bölüm içeriği, medya URL+alt+boyut, public duyuru alanları, ilan kart/detay alanları. Property adresi yalnız yöneticinin bölüm içeriğine yazdığı kadar. `propertyId`, `unitId`, kişi kimliği, audit alanları, iç durumlar, `version` public'e çıkmaz.

## 4. ErrorCatalog eklemeleri (FAZ 02/06)

`BF_WEBSITE_NOT_FOUND`, `BF_WEBSITE_SITE_EXISTS`, `BF_WEBSITE_SITE_KEY_INVALID`, `BF_WEBSITE_SITE_KEY_TAKEN`, `BF_WEBSITE_PAGE_SLUG_TAKEN`, `BF_WEBSITE_PAGE_REQUIRED`, `BF_WEBSITE_SECTION_INVALID` (param: field), `BF_WEBSITE_MEDIA_INVALID`, `BF_WEBSITE_MEDIA_IN_USE`, `BF_WEBSITE_MEDIA_RIGHTS_REQUIRED`, `BF_WEBSITE_PUBLISH_EMPTY`, `BF_WEBSITE_DOMAIN_INVALID`, `BF_WEBSITE_DOMAIN_UNVERIFIED`, `BF_LISTING_STATE_INVALID`, `BF_LISTING_CONSENT_REQUIRED`, `BF_LISTING_COMPLIANCE_REQUIRED`, `BF_LISTING_PHOTO_LIMIT`, `BF_LISTING_UNIT_SCOPE`, `BF_INQUIRY_REJECTED`. Her kod TR/EN sözlük ve negatif testle eklenir (07.1 kuralı).

## WEBSITE FAZ06 uygulama notu —19.10.2026
Güncel sözleşme ve kabul sınırları [../api/listings.md](../api/listings.md). EIDS_VERIFIED kullanıcı beyanı uygulanmadı; varsayılan sunucu doğrulayıcısı yayın kapısını kapatır. Gerçek EİDS/hukuki kabul yoktur. Başvuru imzalı challenge kullanır, elapsedMs beyanı kabul edilmez. Privacy sürümü aktif publication SHA sürümüdür. Anonim/yönetim depoları ayrıdır. Proxy istemci IP bilgisini aktarmaz; IP kotası proxy arkasında paylaşılabilir. FAZ06 ve T21.10 tam işletim kabulü henüz tamamlanmadı.
