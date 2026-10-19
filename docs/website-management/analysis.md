# WebsiteManagement — WEBSITE FAZ 00 analiz raporu

Tarih: 18.10.2026 (Europe/Istanbul) · Kapsam: yalnız analiz, uygulama kodu/veritabanı/servis değişmedi.
Kaynaklar: `BuildingFlow-WebsiteManagement-Prompt-Paketi/MASTER_PROMPT.md`, `PHASES.md`, `START_HERE.md`, mockup `Altunbayrak Rezidans Web Sitesi Mockup'ı.png`, kök `AGENTS.md`, `CLAUDE.md`, `docs/progress/STATUS.md`.
Yol haritası kaydı: İŞ21 "Web sitesi yönetimi" (`milestones.ts`, T21.0–T21.12). Paketin faz numaraları **WEBSITE FAZ 00–09** olarak ayrı yazılır; İŞ/UI/TEKNİK/PERSON/MOBİL sayaçlarına karışmaz.

```text
[BUILDINGFLOW]
[WEBSITE FAZ 00 | 10 faz] Mevcut projeyi analiz et
[ADIM 06/06] Analiz raporu
[DURUM] TAMAMLANDI
[İLERLEME] WEBSITE FAZ 00: 6/6 = %100.00 · İŞ21: 1/13 = %7.69
[TEST] standards.test.mjs 9/9; web npm test 148/149 (tek hata buttons.test.mjs, PERSON FAZ07 dosyalarında, bu turdan önce de var). Backend/build çalıştırılmadı (kod değişmedi).
[TOKEN] Giriş/çıkış/toplam token, kalan context, kota: BİLGİ MEVCUT DEĞİL
[SONRAKİ ADIM] Kullanıcı onayı → WEBSITE FAZ 01
```

## 1. Mevcut yapı (gerçek durum)

| Alan | Bulgu |
| --- | --- |
| Backend | .NET 10 modular monolith: `BuildingFlow.Api` (modül başına `*Endpoints.cs` minimal API), `Application` (modül başına tek dosya: sözleşme+servis), `Domain` (BaseEntity'den türeyen entity'ler), `Infrastructure` (`*Store.cs`, `*Configurations.cs`, EF Core 10 + Npgsql 10.0.3, 30 migration), `Worker` (RabbitMQ). Testler: `tests/BuildingFlow.UnitTests`, `tests/BuildingFlow.IntegrationTests` (ayrı PostgreSQL). |
| Yetki | Keycloak realm rolleri `SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT, RESIDENT`; `Program.cs` içinde `CanManage*` policy'leri; veri kapsamı `PropertyMembership` (kullanıcı→Property). PERSON refactor ile `PersonTenant`, `PersonPropertyScope`, `ManagementAssignment(TenantId, PropertyId, Role)` ve `ManagementAccess` (LEGACY/SHADOW/ENFORCED bayraklı) geliyor; FAZ07 kullanıcı kabulü bekliyor. |
| "Tenant" | Ürün içinde iki katman var: yönetim alanı = `Property` (APARTMENT/SITE/RESIDENCE/MIXED → opsiyonel Building → Unit) ve yeni `PersonTenant`. Web sitesi için doğal sahip **Property**'dir. |
| Dosya | SeaweedFS S3 özel bucket, `StoredDocument` (PDF/JPEG/PNG, imza+MIME+uzantı, 10 MiB), `AWSSDK.S3` mevcut (presigned URL desteği var). `MobileProfile.cs` içinde projeye ait JPEG yapısal doğrulama + EXIF/APP/yorum temizleyici var → web medyası için yeniden kullanılabilir. |
| Güvenlik | `ReleaseSecurity.cs`: production'da HTTPS issuer, wildcard'sız `AllowedHosts`, yalnız `Security:KnownProxies` IP'lerinden forwarded header, HSTS/nosniff/DENY/no-store, anonim IP + `sub` bazlı 429. FE nonce CSP `proxy.ts`. ErrorCatalog + TR/EN hata kodları. |
| Frontend | `frontend/buildingflow-web`: Next.js 16.4.0, React 19.3.0, Tailwind 4.3.3, next-intl, TanStack Table 8.21.3, shadcn/ui, lucide-react 1.52.0. Feature klasörleri `src/features/*`, workspace rotaları `src/app/(workspace)`, dönüş yolu allowlist'i `src/auth/return-path.ts`. |
| Mobil | `mobile/buildingflow-mobile` (Expo). Bu pakette değişmeyecek. |
| Altyapı | `deploy/docker-compose.yml`: web(3003), api(5005), worker, postgres, keycloak, redis, rabbitmq, seaweedfs, mailpit, grafana/otel/prometheus/loki; **tüm portlar 127.0.0.1**, ters vekil/TLS/alan adı yönlendirmesi yok. Keycloak teması `deploy/keycloak/themes/buildingflow`. |
| CI/Lisans | `.github/workflows/quality.yml` → `scripts/ci.sh --install`, `check-licenses.py`, yedek/geri yükleme provası, mobil işi. Lisans politikası: **yeni üçüncü taraf paket/font/ikon/görsel eklenmez** (16.10.2026 kararı). |
| İş takibi | Tek kaynak `frontend/buildingflow-web/src/features/roadmap/milestones.ts` + `messages/tr.json`/`en.json`, kanıt `docs/TESTING.md`, özet `docs/progress/STATUS.md`. Son iş fazı İŞ20 → yeni iş **İŞ21**. |

## 2. Modülün yerleşimi

| Katman | Eklenecek yer (desene uygun) |
| --- | --- |
| Domain | `Domain/Website.cs` (WebsiteSite, WebsitePage, WebsiteSection, WebsiteTheme, WebsiteMedia, WebsiteDomain, WebsitePublication, WebsitePublicNotice), `Domain/PropertyListing.cs` (PropertyListing, PropertyListingPhoto, PropertyListingApproval) |
| Application | `Application/Website.cs`, `Application/PropertyListings.cs`, `ErrorCatalog.cs` içine `BF_WEBSITE_*`, `BF_LISTING_*` kodları |
| Infrastructure | `WebsiteConfigurations.cs`, `WebsiteStore.cs`, `PropertyListingConfigurations.cs`, `PropertyListingStore.cs`, `DependencyInjection.cs` kaydı, tek yeni migration (yalnız yeni tablolar) |
| Api | `WebsiteEndpoints.cs` (yönetim, JWT + policy), `PublicWebsiteEndpoints.cs` (anonim, salt okunur, ayrı rate-limit, yalnız yayınlanmış snapshot), `PropertyListingEndpoints.cs`; `Program.cs` içinde `CanManageWebsite`, `CanApproveListings` policy'leri |
| Yönetim paneli | `src/features/website/`, `src/features/listings/`, rotalar `(workspace)/properties/[id]/website/*`, menü `features/navigation` içinde "Web Sitesi Yönetimi", `return-path.ts` allowlist'ine yeni yollar |
| Public site | Yeni `website/` Next.js uygulaması (aynı next/react/tailwind/lucide sürümleri, yeni paket yok), `deploy/docker-compose.yml` içinde `website` servisi (öneri 127.0.0.1:3004) |
| Belgeler | `docs/website-management/` (bu rapor, ADR, API, güvenlik, tenant, yayın, lisans), `docs/api/website.md`, `docs/TESTING.md` İŞ21 bölümü |

Sahiplik önerisi: **WebsiteSite ↔ Property (bire bir, opsiyonel)**; composite FK `(property_id, id)` ile tüm alt kayıtlar aynı Property'ye bağlanır. PERSON refactor ENFORCED olduğunda yetki `ManagementAccess` üzerinden, öncesinde `PropertyMembership` + rol policy'si ile yapılır; yeni kimlik deposu yok.

## 3. Mockup değerlendirmesi

Mockup bölümleri: üst menü (Ana Sayfa, Rezidansımız, Daireler, Sosyal Alanlar, Duyurular, Galeri, İletişim), hero + iki CTA, giriş kartı (Yönetim/Sakin), 6 özellik şeridi, Satılık/Kiralık kart karuseli, Duyurular, Sosyal Alanlar galerisi, alt istatistik bandı (3 Blok, 120 Daire, 5.000 m² Yeşil Alan, 300+ Sakin). Hepsi WEBSITE FAZ 04 şablonuyla karşılanabilir; aşağıdaki dört nokta değişmeli:

1. **Giriş kartındaki kullanıcı adı/parola alanları uygulanmaz.** Proje kuralı Authorization Code + PKCE; parola yalnız Keycloak sayfasına girilir. Kart aynı görünümle iki düğme olur ("Yönetim Girişi", "Sakin Girişi") ve mevcut temalı Keycloak sayfasına yönlendirir; "Beni hatırla/Parolamı unuttum" Keycloak'ta kalır.
2. **Duyurular** iç `Announcement` tablosundan okunmaz (sakinlere özel içerik). Ayrı `WebsitePublicNotice` veya iç duyurudan açık "web sitesinde yayınla" kopyası + onay.
3. **İstatistik bandı** elle girilen içeriktir; "300+ Sakin" gibi sayılar veritabanından otomatik hesaplanıp yayınlanmaz.
4. **Görseller** mockup'tan alınamaz (kullanım hakkı doğrulanmamış). Demo için projeye ait SVG/çizim yer tutucular, gerçek sitede yöneticinin hak beyanıyla yüklediği fotoğraflar. Fiyat/daire örnekleri "DEMO" etiketli olur.

## 4. Riskler

### Lisans
- Yeni paket gerekmez: next 16.4.0, react 19.3.0, tailwind 4.3.3, lucide-react (ISC) zaten envanterde. `website/` ayrı `package-lock.json` getirir → `check-licenses.py` ve `docs/licenses.md` kapsamına eklenmeli.
- **Görsel işleme**: ImageSharp (Six Labors Split License, ticari eşik) ve SkiaSharp eklenmez; JPEG temizleme mevcut projeye ait koddan, PNG için projeye ait chunk filtresi (tEXt/iTXt/zTXt/eXIf/tIME atılır). Yeniden boyutlandırma/WebP üretimi yeni paket olmadan yapılamaz → yükleme sınırı (boyut/piksel) ile çözülür veya kullanıcı kararı gerekir.
- **Font**: mockup yazı tipi bilinmiyor; sistem font yığını veya projede zaten lisanslı font. Google Fonts çağrısı yapılmaz (KVKK: yurt dışı aktarım + yeni lisans).
- **Ters vekil** (custom domain + TLS için Caddy/Traefik/nginx): compose'da yok; eklenirse yeni lisans yükü → kullanıcı kararı gerekir (WEBSITE FAZ 07).
- **Tanıtım filmi**: YouTube/Vimeo gömme üçüncü taraf çerez ve yurt dışı aktarım doğurur; öneri kendi depolamadan MP4 veya yalnız bağlantı.

### Güvenlik
- Public API ayrı uç grubu (`/api/public/v1/sites/{siteKey}/…`), yalnız `PUBLISHED` snapshot okur; yönetim DTO'ları yeniden kullanılmaz (aşırı veri sızıntısı riski).
- Production `AllowedHosts` wildcard yasağı çok alan adıyla çakışır: public site ayrı servis olmalı; API'ye host değil doğrulanmış `siteKey` gider. Alan adı → site eşlemesi yalnız `WebsiteDomain.VerifiedAt` dolu kayıtlardan (DNS TXT doğrulaması).
- Next.js önbelleği (ISR/fetch cache) anahtarı site kimliği içermeli; cross-tenant cache zehirlenmesi ve host header enjeksiyonu testleri WEBSITE FAZ 07'de zorunlu.
- Dönüş URL'si yalnız mevcut `return-path.ts` allowlist'iyle; açık yönlendirme yok. Keycloak `buildingflow-web` istemcisine public site origin'i yalnız gerekiyorsa eklenir (yalnız yönlendirme yapılırsa gerekmez).
- Medya: imza+MIME+uzantı, boyut/piksel sınırı, EXIF/konum temizliği, sunucu UUID anahtarı, kısa ömürlü presigned URL veya API üzerinden cache'li servis, yükleme rate-limit. SVG yüklemesi kapalı (XSS).
- Zengin metin: HTML saklanmaz; yapılandırılmış blok + düz metin/markdown alt kümesi, CSP nonce korunur.

### KVKK
- Public sitede asla: kişi adı/telefon/e-posta, borç/aidat, sakin listesi, daire-kişi eşleşmesi, iç duyuru/belge, talep fotoğrafları.
- İlan iletişimi: malik telefonu gösterilmez; ziyaretçi formu yönetime/ilan sahibine iletilir, aydınlatma metni + gerekiyorsa açık rıza (İŞ16 rıza altyapısı yeniden kullanılır), form verisi için saklama süresi.
- Çerez: yalnız zorunlu çerez; analitik/üçüncü taraf izleyici yok → çerez bandı gerekmeyebilir, çerez politikası sayfası yine de var.
- Galeri fotoğraflarında yüz/plaka: yükleme anında yöneticiden beyan ve kontrol işareti.

### Multi-tenant
- Her sorguda `property_id` filtresi + üyelik/görev kontrolü; `IgnoreQueryFilters` scope'u atlayamaz (mevcut kural).
- Bir Property'nin silinmesi/pasife alınması siteyi otomatik yayından kaldırır; yayınlanmış site Property silmeyi engeller.
- PERSON refactor iki yetki modelini paralel yürütüyor; website yetkisi tek bir `IWebsiteAccess` arkasında iki moda da uyumlu yazılmalı.

### Mevzuat (Türkiye, hukuki görüş değildir)
- Satılık/kiralık ilan yayını: Taşınmaz Ticareti Hakkında Yönetmelik ve internet ilanlarında Elektronik İlan Doğrulama Sistemi (e-Devlet üzerinden malik/yetki doğrulaması) yükümlülüğünün bu siteye uygulanıp uygulanmadığı hukukçuyla doğrulanmalı. O zamana kadar ilan **otomatik yayınlanmaz**: malik onay belgesi/beyanı + yönetici onayı + "mevzuat kontrolü yapıldı" işareti olmadan PUBLISHED olamaz.
- İlan sahipliği/kiracılığı değiştirmez; UnitOccupancy'e dokunulmaz.

### Migration ve repo
- Yalnız yeni tablolar (BaseEntity 8 alan, composite FK, partial unique index); mevcut tablo değişmez → düşük risk. Down guard: dolu tabloda veri kaybı uyarısı.
- **Çalışma ağacında 387 commit edilmemiş değişiklik var (main, PERSON FAZ07 kabul bekliyor).** `milestones.ts`, `tr.json`, `en.json`, `STATUS.md`, `TESTING.md` zaten değişmiş durumda. WEBSITE FAZ 01 öncesi bu değişikliklerin commit edilmesi ve `feature/website-management` dalı açılması önerilir; aksi halde iki iş birbirine karışır.

## 5. Eksik bilgi / kullanıcı kararı

| # | Soru | Varsayılan (onaylanmazsa) |
| --- | --- | --- |
| 1 | Site Property başına mı, PersonTenant başına mı? | Property başına bir site |
| 2 | İlanı kim girer? | Yönetici girer; malik onayı belge/beyan olarak eklenir; malik self-servis sonraya |
| 3 | Custom domain için ters vekil eklensin mi? | Önce tek alan adı altında alt alan/yol (`site.example.com/altunbayrak`), custom domain sonra karar |
| 4 | Gerçek logo/fotoğraf/metin kim sağlayacak? | Demo içerik, açık "DEMO" etiketi |
| 5 | Yeniden boyutlandırma/WebP | Paket yok; yükleme sınırı (≤ 5 MiB, ≤ 4096 px) |

## 6. Faz sırası ve görevler (İŞ21)

| Görev | WEB faz | Özet |
| --- | --- | --- |
| T21.0 | 00 | Analiz ve risk raporu — **tamamlandı** |
| T21.1 | 01 | Domain/güvenlik tasarımı, ADR, API kontratı, DB planı, test senaryoları |
| T21.2 | 02 | Backend CMS: site/sayfa/bölüm/tema CRUD, public/admin ayrımı, audit, migration, testler |
| T21.3 | 03 | `website/` Next.js iskeleti: tenant çözümleme, SEO metadata, 404/hata, tema tokenları, erişilebilirlik |
| T21.4 | 04 | Premium Rezidans şablonu (mockup), demo içerik etiketi, mobile-first |
| T21.5 | 05 | Yönetim paneli "Web Sitesi Yönetimi": sayfa, blok, tema, medya, taslak/önizleme/yayın |
| T21.6 | 06 | Satılık/kiralık ilan: Unit'e opsiyonel bağ, fotoğraf, onay kapısı, iletişim gizliliği |
| T21.7 | 07 | Alan adı doğrulama, sitemap/robots/canonical/structured data, cache invalidation, rollback |
| T21.8 | 08 | Yönetim/Sakin girişi Keycloak yönlendirmesi, return URL allowlist, session testleri |
| T21.9 | 09 | E2E/a11y/SEO/tenant/güvenlik/performans, Docker/deploy, SBOM, TR kullanım dokümanı |
| T21.10 | ek | KVKK: aydınlatma, çerez politikası, iletişim/ilan başvuru formu, saklama süresi, spam/rate-limit |
| T21.11 | ek | Medya hakları ve kişisel veri içeren görsel kontrolü (beyan, yüz/plaka uyarısı, EXIF) |
| T21.12 | ek | Kullanıcı kabulü (gerçek yönetim alanı, masaüstü/telefon) |

Öneri: T21.10 ve T21.11 ilgili oldukları faza (06 ve 05) bağlanır, ayrı faz açılmaz.

## 7. Değişen dosyalar (bu tur)

- `docs/website-management/analysis.md` (yeni)
- `frontend/buildingflow-web/src/features/roadmap/milestones.ts` (İŞ21 eklendi, mevcut kayıtlar değişmedi)
- `frontend/buildingflow-web/messages/tr.json`, `en.json` (yalnız `phase21*` anahtarları eklendi)
- `docs/progress/STATUS.md` (İŞ21 özet bölümü), `docs/TESTING.md` (İŞ21 WEBSITE FAZ 00 kaydı)

Backend, veritabanı, Keycloak, `.env` ve çalışan servisler değişmedi.

## 8. GitHub görevleri

Repo issue'ları açıldı (#20–#29). Ek görevler ilgili faza bağlandı: T21.11 → FAZ 05, T21.10 → FAZ 06, T21.12 → FAZ 09. Her faz sonunda **aynı issue** yapılanlar, kanıt ve risklerle güncellenir; yeni kopya açılmaz. Project 3 panosuna ekleme bu oturumdan yapılamadı (GraphQL erişimi yok).

| Faz | Issue | Durum |
| --- | --- | --- |
| 00 | [[BF-WEBSITE][FAZ 00] Mevcut Projeyi Analiz Et](https://github.com/AkaBayraK/BuildingFlow/issues/20) | In review — kullanıcı kabulü bekleniyor |
| 01 | [[BF-WEBSITE][FAZ 01] Domain ve Güvenlik Tasarımı](https://github.com/AkaBayraK/BuildingFlow/issues/21) | Backlog — başlatılmadı |
| 02 | [[BF-WEBSITE][FAZ 02] Backend CMS Temel API](https://github.com/AkaBayraK/BuildingFlow/issues/22) | Backlog — başlatılmadı |
| 03 | [[BF-WEBSITE][FAZ 03] Website Frontend İskeleti](https://github.com/AkaBayraK/BuildingFlow/issues/23) | Backlog — başlatılmadı |
| 04 | [[BF-WEBSITE][FAZ 04] Premium Rezidans Şablonu](https://github.com/AkaBayraK/BuildingFlow/issues/24) | Backlog — başlatılmadı |
| 05 | [[BF-WEBSITE][FAZ 05] Yönetim Paneli WebsiteManagement](https://github.com/AkaBayraK/BuildingFlow/issues/25) | Backlog — başlatılmadı |
| 06 | [[BF-WEBSITE][FAZ 06] Satılık/Kiralık İlan Yönetimi](https://github.com/AkaBayraK/BuildingFlow/issues/26) | Backlog — başlatılmadı |
| 07 | [[BF-WEBSITE][FAZ 07] Domain, SEO ve Yayınlama](https://github.com/AkaBayraK/BuildingFlow/issues/27) | Backlog — başlatılmadı |
| 08 | [[BF-WEBSITE][FAZ 08] Güvenli Giriş ve Yönlendirme](https://github.com/AkaBayraK/BuildingFlow/issues/28) | Backlog — başlatılmadı |
| 09 | [[BF-WEBSITE][FAZ 09] Test, Operasyon ve Dokümantasyon](https://github.com/AkaBayraK/BuildingFlow/issues/29) | Backlog — başlatılmadı |
