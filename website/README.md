# BuildingFlow public website — WEBSITE FAZ 05

Ayrı Next.js uygulaması, port 3004. Premium rezidans şablonu uygulanmıştır.

Depo kökünde `npm run setup --prefix website`, ardından `npm run dev --prefix website`.
`frontend/buildingflow-web/node_modules` mevcut olmalıdır; yoksa o uygulamanın kilitli kurulum prosedürünü kullanın. Setup yalnız mevcut bağımlılık klasörüne bağlantı kurar; yeni paket/sürüm/lock oluşturmaz. Next/React/TypeScript sürümleri mevcut yönetim uygulamasından alınmıştır; lisans/NOTICE kaynakları `../frontend/buildingflow-web/LICENSES` ve `../docs/licenses.md` altında korunur. Bağımsız deployment/kurulum FAZ 09 işidir.

Demo önizlemesi: http://localhost:3004 (noindex, gerçek veri içermez). Yayınlı site: `/{siteKey}`, alt sayfa: `/{siteKey}/{slug}`. Henüz yayın yoksa 404. Gerçek site/seed oluşturulmaz.

API varsayılanı http://localhost:5005; sunucu ortamında `WEBSITE_API_ORIGIN` ile değişir. Yalnız public API okunur, kimlik bilgisi iletilmez. Host/forwarded headers tenant seçmez; site anahtarı ve dönen DTO eşleşir. Redirect reddi, 8 saniye timeout, no-store ve istek başına React cache vardır. Metadata bloklanarak gerçek HTTP 404/500 korunur.

`npm test`, `npm run typecheck`, `npm run build`; build sonrası `npm run test:smoke` (izole fixture portları 15005/13004, kendi sunucularını kapatır). Fixture gerçek veritabanına bağlanmaz.

FAZ 04: ortak `Residence` şablonu hero/özellik/ilan/duyuru/sosyal alan/galeri/iletişim/istatistik/metin/video bağlantısı bloklarını sunar. Kök önizleme ayrı `demo.ts` verisidir. Yayınlı tenantlarda demo ilan, duyuru ve görseller kullanılmaz; ilan/duyuru API'si FAZ 06'dadır; bu alanlar boş durum gösterir. FAZ 05 ile yayın snapshot'ına referanslı medya ve gerçek galeri desteği eklenmiştir. HTML çalıştırılmaz, rich text güvenli düz metindir. Gerçek fiyat, telefon, adres veya sakin sayısı uydurulmaz. Demo kartları klavyeyle açılır; mobilde yatay kart şeridi kullanılır.

Tema renkleri public DTO'dan gelir. Tenant logosu ve hero görseli `src/lib/theme-assets.ts` içindeki siteKey bazlı `siteAssets` kaydına `/images/ornek.webp` gibi yerel yollarla eklenebilir. Önizleme görselleri yalnız preview dalındadır. CMS medya yükleme/değiştirme yönetim panelinde FAZ 05 ile uygulanmıştır. Özgün SVG ikon/logo kodu; üretilmiş iki özgün görselin kaynak/prompt kaydı `public/images/README.md`. Stok görsel/paket/font eklenmedi.

`WEBSITE_MANAGEMENT_ORIGIN` server-only sabit yönetim origin'idir, varsayılan `http://localhost:3003`. Yalnız `/login` bağlantısı üretilir, kullanıcı parolası toplanmaz, returnTo/tenant/rol parametresi eklenmez. Tam giriş yönlendirme entegrasyonu FAZ 08'dir. Gerçek hesap/servis ayarları değiştirilmez.

Hero preload, responsive next/image sizes, yerel WebP (296404+220584 bayt), lazy alt görseller, sabit görsel kutuları ve reduced-motion vardır. Core Web Vitals hedefleri LCP<2.5s/CLS<0.1; Lighthouse veya saha sonucu ölçülmedi. 390/768/1440 tarayıcı taşma kontrolleri ve mobil klavye menü/kart kabulü yapıldı; gerçek cihaz/ekran okuyucu kabulü yapılmadı. Canonical/domain/sitemap FAZ 07'de doğrulanmış origin ile hazırlanır. Arayüz Türkçe; çok dilli CMS sözleşmesi mevcut değildir.

## FAZ 05 public medya

Snapshot media[] ve tema logoMediaId/heroMediaId referansları kullanılır; AMENITIES/GALLERY öğeleri mediaId taşıyabilir. /media/{siteKey}/{id}/{sha}/{ext} proxy'si sabit server API origin'ine gider, MIME/boyut/SHA doğrular ve no-store döner. Görseller unoptimized sunularak iptal/askı denetiminin Next image cache ile atlanması önlenir. İçerik ve yayın yalnız yönetim panelinden değişir; kök demo hiçbir gerçek site oluşturmaz.
