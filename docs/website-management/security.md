# WebsiteManagement — güvenlik, tenant ve KVKK tasarımı (WEBSITE FAZ 01)

Durum: tasarım · 18.10.2026. Mevcut kurallar (AGENTS.md 08/11, ReleaseSecurity, ErrorCatalog) aynen geçerlidir; bu belge yalnız eklemeleri tanımlar.

## 1. Yetki matrisi

Yeni Keycloak realm rolü **eklenmez**. Policy'ler mevcut rollerle tanımlanır; veri kapsamı her istekte backend'de `IWebsiteAccess` ile doğrulanır (LEGACY modda `PropertyMembership`, ENFORCED modda `ManagementAccess` aktif görev). SUPER_ADMIN üyelik gerektirmez (mevcut davranış).

| Yetenek | SUPER_ADMIN | SITE_ADMIN | MANAGER | ACCOUNTANT | RESIDENT / anonim |
| --- | --- | --- | --- | --- | --- |
| Site içeriği, tema, sayfa, medya, public duyuru düzenleme (`CanManageWebsite`) | ✓ | ✓ | ✓ | – | – |
| Site oluşturma, yayınlama, geri alma, alan adı isteme (`CanPublishWebsite`) | ✓ | ✓ | – | – | – |
| Site askıya alma, CUSTOM alan adı onayı | ✓ | – | – | – | – |
| İlan taslağı, fotoğraf, malik onay belgesi, başvuruları görme (`CanManageListings`) | ✓ | ✓ | ✓ | – | – |
| İlan onay/red/yayın/kaldırma (`CanApproveListings`) | ✓ | ✓ | – | – | – |
| Public site okuma, başvuru formu | ✓ | ✓ | ✓ | ✓ | ✓ |

Ek kural: ilanı **gönderen** kişi aynı zamanda SITE_ADMIN ise onaylayabilir, ama onay isteği `complianceConfirmed=true` ve not olmadan reddedilir; olay günlüğü gönderen ve onaylayanı ayrı kaydeder. İkinci onaylayan zorunluluğu site ayarıyla açılabilir (varsayılan kapalı; küçük apartmanlarda tek yönetici gerçeği).

## 2. Tenant (site) çözümleme

- Public API site kimliğini **Host'tan değil, yol parametresindeki `siteKey`den** alır. `AllowedHosts` wildcard yasağı korunur.
- `website/` uygulaması:
  - FAZ 03–06: `https://<WEBSITE_ORIGIN>/{siteKey}/…` — siteKey regex ile doğrulanır, API'den 404 gelirse genel 404.
  - FAZ 07: Host → normalize (küçük harf, port at, punycode, uzunluk ≤ 253, etiket regex) → `GET /api/public/v1/domains/{host}` → yalnız `VERIFIED` kayıt. Host yalnız güvenilen ters vekilin ilettiği değerdir; `X-Forwarded-Host` yalnız `Security:KnownProxies` (website için ayrı ayar) IP'lerinden kabul.
- Alan adı sahipliği: `_buildingflow-verify.<host>` TXT kaydında oluşturma anında bir kez gösterilen rastgele token; DB'de yalnız sha256. Doğrulama sunucu tarafında DNS sorgusuyla (.NET `System.Net.Dns` TXT desteklemez → FAZ 07'de projeye ait küçük DNS-over-HTTPS istemcisi veya SUPER_ADMIN manuel onayı; yeni paket yok). Doğrulanmamış host 404.
- Önbellek: Next.js fetch/ISR anahtarları ve `revalidateTag` etiketleri `site:{siteKey}` içerir; yanıtlar arası paylaşılan modül düzeyi değişken yok. API `ETag` site bazlı.

## 3. Public/özel içerik ayrımı (KVKK)

- Public uçlar yalnız `website_*` ve `property_listings` tablolarındaki **yayınlanmış** kayıtlardan, beyaz liste DTO ile okur. `people`, `unit_occupancies`, `dues_*`, `payments`, `announcements`, `stored_documents`, `requests`, `consents` vb. tablolara public kod yolundan erişim yok — mimari test: `PublicWebsiteEndpoints` ve public store yalnız izinli entity setlerini kullanır (reflection/kaynak taraması).
- İstatistik bandı (blok, daire, m², sakin) **elle girilen metin**; otomatik sayım yok.
- İlan: kişi adı/telefon yok; ziyaretçi formu yönetime düşer. `building_label`/`floor_label` yönetici tarafından yazılır, Unit'ten otomatik doldurulup onay ekranında gösterilir.
- Public duyuru iç duyurudan ayrı tablo; kopyalama bilinçli eylem ve yeniden düzenlenebilir.
- Galeri: yüz/plaka içeren görsel için yükleyen beyanı + "kişilerin izni alındı" onayı; yoksa yükleme reddi.

## 4. KVKK süreçleri (T21.10)

- Her sitede silinemeyen `PRIVACY` (aydınlatma metni) ve `COOKIES` sayfası; şablon metin "taslak — hukukçu kontrolü gerekir" uyarısıyla gelir, yönetici veri sorumlusu bilgilerini doldurmadan site ilk kez yayınlanamaz.
- Çerez: website uygulaması yalnız zorunlu çerez kullanır (önizleme oturumu yok; tema/dil URL veya localStorage değil, sunucu). Üçüncü taraf script, analitik, font, harita, video gömme **yok** → çerez rıza bandı gerekmez; COOKIES sayfası bunu beyan eder. Harita için yalnız "Haritada aç" dış bağlantısı.
- Başvuru formu: aydınlatma metni bağlantısı ve sürümü kaydedilir; pazarlama amaçlı kullanım yok (açık rıza gerekmez). İleride pazarlama istenirse İŞ16 rıza altyapısı kullanılır. Saklama 180 gün, sonra Worker ad/telefon/e-posta/mesajı anonimleştirir. Ham IP saklanmaz (HMAC, günlük tuz).
- Yurt dışı aktarım yok: tüm varlıklar kendi sunucudan.

## 5. Kimlik doğrulama (FAZ 08)

- Public sitede parola/kullanıcı adı alanı **yok**. "Yönetim Girişi" ve "Sakin Girişi" düğmeleri mevcut web uygulamasının `/login` akışına (Authorization Code + PKCE) gider; dönüş yolu mevcut `return-path.ts` allowlist'iyle sınırlı (`/workspaces`, `/dashboard`). Website origin'i Keycloak redirect URI'lerine eklenmez (website OIDC istemcisi değildir).
- Website uygulaması token/çerez saklamaz; yönetim önizlemesi yalnız HMAC önizleme token'ı ile (10 dk, salt okunur, noindex).

## 6. Medya yükleme

1. Boyut (≤ 5 MiB) istek gövdesi sınırıyla, sonra dosya imzası (JPEG `FF D8 FF`, PNG `89 50 4E 47…`) + MIME + uzantı üçlüsü.
2. JPEG: mevcut `MobileProfile` yapısal doğrulayıcısı genelleştirilir (boyut sınırı parametreli), EXIF/APP1–15/COM segmentleri atılır.
3. PNG: projeye ait chunk ayrıştırıcı; CRC doğrulanır, yalnız `IHDR, PLTE, IDAT, IEND, tRNS, gAMA, sRGB` tutulur (`tEXt, iTXt, zTXt, eXIf, tIME` atılır), bit derinliği/boyut sınırı.
4. sha256 temizlenmiş içerikten; nesne anahtarı sunucu UUID'si.
5. Yükleme uç noktasına kimlikli `sub` bazlı yazma limiti (mevcut) + site başına günlük 200 dosya / 500 MiB kotası.
6. SVG, GIF, HEIC, WebP, video yükleme yok (video yalnız `VIDEO_LINK` olarak kendi sunucudaki MP4 sonraki karar).

## 7. Diğer korumalar

- İçerik: HTML saklanmaz; markdown alt kümesi sunucuda ayrıştırılıp yapılandırılmış token olarak döner, website React ile render eder (`dangerouslySetInnerHTML` yasak). Bağlantılar `https:` veya site içi; `rel="noopener noreferrer nofollow"` dış bağlantıda.
- CSP (website): `default-src 'self'; img-src 'self' <API origin>; script-src 'self' 'nonce-…'; frame-ancestors 'none'; form-action 'self'; base-uri 'none'`. HSTS, nosniff, `Referrer-Policy: strict-origin-when-cross-origin`.
- Rate-limit: public GET anonim IP okuma limitiyle (mevcut), `POST /inquiries` ayrı sıkı bölüm (IP başına 5/saat, site başına 100/gün), honeypot + minimum doldurma süresi (3 sn).
- Açık yönlendirme yok; `returnTo` sabit.
- Loglama: başvuru içeriği, ad, telefon, e-posta loglanmaz; metrik etiketlerinde siteKey yok (sınırsız kardinalite).
- Host header enjeksiyonu: website mutlak URL'leri (canonical, sitemap) yalnız doğrulanmış domain kaydından üretir, istek Host'undan değil.

## 8. Mevzuat kontrol noktası (hukuki görüş değildir)

İlan yayını için zorunlu: (a) malik onayı türü ve belgesi, (b) aracı ise yetki belgesi numarası, (c) SITE_ADMIN'in "mevzuat kontrolü yapıldı" onayı ve notu. Elektronik İlan Doğrulama Sistemi (EİDS) ve Taşınmaz Ticareti Hakkında Yönetmelik yükümlülüklerinin bu siteye uygulanıp uygulanmadığı hukukçuyla netleşene kadar `EIDS_VERIFIED` seçeneği yalnız beyan niteliğindedir ve sistem otomatik yayın yapmaz. Hukuki teyit FAZ 06 kabul koşuludur.

## WEBSITE FAZ06 uygulama notu —19.10.2026
Güncel sözleşme ve kabul sınırları [../api/listings.md](../api/listings.md). EIDS_VERIFIED kullanıcı beyanı uygulanmadı; varsayılan sunucu doğrulayıcısı yayın kapısını kapatır. Gerçek EİDS/hukuki kabul yoktur. Başvuru imzalı challenge kullanır, elapsedMs beyanı kabul edilmez. Privacy sürümü aktif publication SHA sürümüdür. Anonim/yönetim depoları ayrıdır. Proxy istemci IP bilgisini aktarmaz; IP kotası proxy arkasında paylaşılabilir. FAZ06 ve T21.10 tam işletim kabulü henüz tamamlanmadı.
