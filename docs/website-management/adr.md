# WebsiteManagement — mimari karar kayıtları (WEBSITE FAZ 01)

Durum: önerildi, kullanıcı onayı bekliyor · 18.10.2026

## ADR-W01 · Site Property'ye bağlıdır
**Bağlam:** Projede iki kapsam katmanı var (Property ve PERSON refactor ile gelen PersonTenant). Mockup tek rezidansı anlatıyor.
**Karar:** WebsiteSite → Property bire bir (opsiyonel). Tenant ilişkisi Property üzerinden türetilir.
**Sonuç:** Mevcut üyelik/görev kontrolleri doğrudan kullanılır. Çok Property'li tek tenant sitesi gerekirse sonradan `website_site_properties` ilişki tablosu eklenir; mevcut tablolar bozulmaz.

## ADR-W02 · Modüller mevcut modular monolith içinde
**Karar:** Yeni proje/servis yok. Domain/Application/Infrastructure/Api içinde `Website*` ve `PropertyListing*` dosyaları; mevcut dosya-başına-modül deseni.
**Sonuç:** Tek migration zinciri, tek audit/hata/telemetri altyapısı.

## ADR-W03 · Public API ayrı uç grubu ve ayrı DTO
**Karar:** `/api/public/v1/sites/{siteKey}` anonim, salt okunur; yönetim `/api/v1/properties/{id}/website`. DTO paylaşılmaz; public store yalnız izinli entity setlerini okur ve bu mimari testle korunur.
**Sonuç:** Yönetim modeline eklenen alan public'e kendiliğinden sızmaz.

## ADR-W04 · Yayın = değişmez snapshot
**Karar:** Site içeriği taslak olarak düzenlenir; yayın, public DTO'nun JSON snapshot'ını değişmez kayıt olarak yazar ve aktif yapar. Geri alma eski snapshot'tan yeni yayın üretir. Duyuru ve ilanlar kendi durum makineleriyle ayrı yayınlanır.
**Sonuç:** Public okuma tek satır; önbellek ETag'i snapshot özeti; geçmiş tam izlenebilir. Bedel: küçük içerik değişikliği de yeniden yayın gerektirir (bilinçli).

## ADR-W05 · Site çözümleme siteKey ile, Host ile değil
**Karar:** API Host'a güvenmez. Website uygulaması önce yol tabanlı siteKey, FAZ 07'de doğrulanmış domain → siteKey eşlemesi kullanır.
**Sonuç:** `AllowedHosts` wildcard yasağı ve KnownProxies kuralı bozulmaz; host injection yüzeyi website uygulamasında ve tek doğrulanmış tabloya sınırlı.

## ADR-W06 · Ayrı `website/` Next.js uygulaması, aynı sürümler
**Alternatif:** Mevcut `frontend/buildingflow-web` içine public rotalar. Reddedildi: yönetim uygulamasının CSP/AuthGate/oturum yüzeyi ile anonim public site karışır, SEO/önbellek ayarları çatışır.
**Karar:** `website/` ayrı uygulama; next 16.4.0, react 19.3.0, tailwind 4.3.3, lucide-react aynı sürüm; yeni paket yok. Ortak tasarım tokenları kopyalanmaz, `website/` kendi küçük token setini taşır (paylaşılan paket kurmak yeni workspace aracı gerektirir).
**Sonuç:** Ayrı `package-lock.json`, lisans kontrolü kapsamına eklenir; compose'a `website` servisi.

## ADR-W07 · Medya API üzerinden, içerik adresli URL ile
**Alternatif:** S3 presigned URL. Reddedildi (public için): iç depolama host'unu dışa açar, süre dolunca önbellekler bozulur.
**Karar:** Public medya `/api/public/v1/sites/{siteKey}/media/{id}/{sha256}.{ext}` ile API'den, `immutable` önbellekle; yalnız yayınlı içerikte referanslıysa. Yönetim önizlemesi kimlikli uç veya 10 dk HMAC imzalı bağlantı (prompt'taki "signed URL" gereği bu yolla karşılanır).
**Sonuç:** Depolama özel kalır; CDN eklenirse URL değişmez.

## ADR-W08 · Görsel işleme için yeni paket yok
**Alternatif:** ImageSharp (Six Labors Split License, ticari eşik), SkiaSharp (yerel bağımlılık). 16.10.2026 "yeni lisans yükü yok" kararı nedeniyle reddedildi.
**Karar:** Backend'de JPEG/PNG yapısal doğrulama ve metadata temizliği projeye ait kodla; backend yeniden boyutlandırma yapmaz, boyut/piksel sınırı var. Website'de Next.js `<Image>` optimizasyonu kullanılabilir: sharp/libvips Next'in mevcut bağımlılığı olarak `docs/licenses.md` envanterinde zaten kayıtlı (Apache-2.0 / LGPL-3.0-or-later, bildirim korunur) — yeni paket değildir. Kullanılıyorsa `images.remotePatterns` yalnız API origin'i, `minimumCacheTTL` uzun, boyut listesi sabit.
**Sonuç:** Core Web Vitals için responsive görsel sağlanır; libvips bildirimi `website/` container imajında da korunur (FAZ 03 lisans kontrolü). Bunu istemezsen `unoptimized` + yükleme önerisi (≤ 1920 px) alternatifi hazır.

## ADR-W09 · İlan yayını insan onaylı kapıdan geçer
**Karar:** DRAFT → PENDING_APPROVAL → APPROVED → PUBLISHED; malik onayı belgesi/beyanı ve mevzuat kontrol onayı olmadan APPROVED olmaz. Otomatik yayın yok; onaylı ilan düzenlenirse yeniden onay. En fazla 90 gün yayın.
**Sonuç:** Mevzuat netleşene kadar sorumluluk kayıt altında ve izlenebilir.

## ADR-W10 · Yeni Keycloak rolü yok
**Karar:** `CanManageWebsite`, `CanPublishWebsite`, `CanManageListings`, `CanApproveListings` policy'leri mevcut SUPER_ADMIN/SITE_ADMIN/MANAGER rolleriyle; veri kapsamı backend üyelik/görev kontrolüyle.
**Sonuç:** Realm değişmez; ilan onay yetkisini MANAGER'dan ayırmak SITE_ADMIN rolüyle sağlanır. Ayrı "ilan onaycısı" rolü gerekirse ManagementAssignment'a yeni görev türü olarak sonradan eklenir.

## ADR-W11 · Giriş public sitede yapılmaz
**Karar:** Mockup'taki giriş kartı aynı görsel düzende kalır, alanlar yerine Keycloak'a yönlendiren iki düğme olur.
**Sonuç:** PKCE kuralı ve "parola yalnız Keycloak'ta" ilkesi korunur; website OIDC istemcisi olmaz.

## WEBSITE FAZ06 uygulama notu —19.10.2026
Güncel sözleşme ve kabul sınırları [../api/listings.md](../api/listings.md). EIDS_VERIFIED kullanıcı beyanı uygulanmadı; varsayılan sunucu doğrulayıcısı yayın kapısını kapatır. Gerçek EİDS/hukuki kabul yoktur. Başvuru imzalı challenge kullanır, elapsedMs beyanı kabul edilmez. Privacy sürümü aktif publication SHA sürümüdür. Anonim/yönetim depoları ayrıdır. Proxy istemci IP bilgisini aktarmaz; IP kotası proxy arkasında paylaşılabilir. FAZ06 ve T21.10 tam işletim kabulü henüz tamamlanmadı.
