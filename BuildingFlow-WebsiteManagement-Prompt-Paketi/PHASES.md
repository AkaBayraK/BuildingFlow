# BuildingFlow WebsiteManagement — Faz Planı

Her fazdan önce MASTER_PROMPT.md kurallarını uygula. Kullanıcı onayı olmadan sonraki faza geçme.

## FAZ 00 — Mevcut projeyi analiz et (kod değiştirme)
- Repo ağacı, backend .NET solution/projeleri, frontend Next.js sürümü, mevcut domain/entity, tenant, RBAC/Keycloak, dosya depolama, CI/CD, test ve lisans yapısını incele.
- `WebsiteManagement` ve `PropertyListings` için entegrasyon noktalarını bul.
- Çakışma/eksik bilgi, migration riski, yapılacak dosyalar ve faz sırası raporu hazırla.
- `docs/website-management/analysis.md` oluşturulmasına izin verilirse yalnız doküman yaz; aksi halde konsolda raporla. Dur.

## FAZ 01 — Domain ve güvenlik tasarımı
- WebsiteSite, WebsitePage, WebsiteSection, WebsiteTheme, WebsiteMedia, WebsiteDomain, WebsitePublication ve PropertyListing tasarımını mevcut modellere göre belirle.
- Tenant/site/yönetim alanı ilişkisi, public visibility, onay ve audit modelini tanımla.
- ADR, API kontratları, veritabanı planı ve test senaryolarını dokümante et. Kodlama öncesi onay iste.

## FAZ 02 — Backend CMS temel API
- Mevcut .NET mimarisinde website site/page/section/theme CRUD, validasyon, yetki, tenant filtreleri, audit, migration ve testleri uygula.
- Public read API ve admin write API ayrımını zorunlu tut.

## FAZ 03 — Website frontend iskeleti
- `website/` Next.js TypeScript uygulamasını mevcut repo araçları ve lisans kurallarına uygun oluştur.
- Mobil uyum, erişilebilirlik, SEO metadata, güvenli tenant çözümleme, 404 ve hata sayfaları, tema tokenları ve testler.

## FAZ 04 — Premium Rezidans şablonu
- Altunbayrak Rezidans örneği: hero, sosyal alanlar, galeri, duyurular, iletişim, satılık/kiralık kartları, yönetim girişi.
- Örnek içeriklerin demo olduğu açıkça belirtilsin; gerçek fiyat/daire/sakin verisi uydurulmasın.
- Site logosu/renk/görsel değişebilir; mobile-first ve Core Web Vitals hedefleri.

## FAZ 05 — Yönetim paneli WebsiteManagement
- `frontend/` menüsüne Web Sitesi Yönetimi ekle.
- Sayfalar, içerik blokları, tema, medya, taslak/önizleme/yayın ve yetki kontrollü ayarlar.
- Ortak bileşenleri yeniden kullan, mükerrer UI üretme.

## FAZ 06 — Satılık/Kiralık ilan yönetimi
- Mevcut bağımsız bölüm kaydına isteğe bağlı bağlanan ilan; ilan daire sahipliğini veya kiracılığı değiştirmez.
- Fotoğraf, fiyat, özellikler, taslak/onay/yayın/kaldırma, iletişim gizliliği ve ilan sahibi yetki doğrulaması.
- Mevzuat/izin gereksinimlerini onay kapısı olarak uygula; doğrulanmamış otomatik ilan yayınlama yok.

## FAZ 07 — Domain, SEO ve yayınlama
- Onaylı custom domain/subdomain, DNS sahiplik doğrulama, TLS, sitemap, robots, canonical, structured data, cache invalidation.
- Preview/publish/rollback; cross-tenant cache ve host injection testleri.

## FAZ 08 — Güvenli giriş ve yönlendirme
- Public sitedeki Yönetim/Sakin Girişi bağlantılarını mevcut Keycloak akışına bağla.
- Return URL allowlist, rol/tenant bağlamı, yetkisiz erişim ve session testleri.

## FAZ 09 — Test, operasyon ve dokümantasyon
- E2E, accessibility, SEO, tenant isolation, güvenlik, performans, migration/rollback, Docker/deploy ve lisans SBOM kontrolleri.
- Türkçe işletim ve yönetici kullanım dokümanları.
