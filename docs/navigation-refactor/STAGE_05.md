# SIDEBAR FAZ07.1 — Aşama05 teslimi

20.10.2026. Yetkili kaynak: buildingflow-sidebar-phase/PHASE_07_1.md Aşama05/08. Aşama06 başlamadı.

## Değişiklik

**Mobil drawer pathname değişiminde ve 1024px masaüstü geçişinde kapanır.** Açılışta aktif sayfa bağlantısına, yoksa ilk bağlantıya odaklanır. Escape/backdrop/kapat ile trigger düğmesine; navigasyon veya masaüstüne geçişte ana içeriğe odak döner. Menü seçimi, farklı programatik pathname ve browser back/forward kapsanır. Query/hash-only geçişleri bu kapsamda ayrıca izlenmedi/test edilmedi.

Mevcut Radix Dialog backdrop, focus trap ve scroll lock kullanılır; ikinci kilit/modal sistemi yazılmadı. Panel genişliği min(320px,100vw−16px), iç scroll ve güvenli alt boşluk korunur. Desktop collapse tercihi mobil etiketleri gizlemez. Mevcut aria-current/expanded/controls, 2px focus ve 44px satırlar korunur. Reduced-motion halinde backdrop/drawer/tooltip animasyon ve geçişleri kapatılır; ortak Dialog overlay'e motion-reduce sınıfı eklendi.

Üretim dosyaları: `src/components/WorkspaceShell.tsx`, `src/components/ui/dialog.tsx`, `src/app/globals.css`. Yeni paket, auth/rol/API/DB/migration veya mobil istemci değişikliği yok.

## Tekrarlanabilir izole UI kabulü

Gerçek WorkspaceShell, navigation/CSS ve Radix bileşenleri mevcut Next/TypeScript derleyicisiyle ayrı loopback fixture'da çalıştırıldı. **Üretim AuthGate veya route'u değiştirilmedi.** Fixture kimlik, scope, router ve SessionControls profilini simüle eder; header profil alanı temsilidir. Gerçek hesap/üyelik/API kabulü değildir.

Frontend klasöründe:

```sh
npm run build
node scripts/sidebar-fixture.mjs
```

Adres `http://127.0.0.1:13005/properties`; yalnız loopback dinler. Kaynaklar `tests/fixtures/sidebar-entry.tsx`, `sidebar-mocks.tsx`, `sidebar-loader.cjs`; üretim bunları import etmez. Derleyici çıktısı OS geçici dizinine yazılır; script kapanışta kendi çıktısını temizler. Bu turun fixture sunucusu ve browser sekmesi kapatıldı; viewport override sıfırlandı.

| Viewport | Belge scrollWidth | Panel/sidebar | Panel scroll/client | Sonuç |
|---|---:|---:|---|---|
|320×844|320|304px drawer|302/302|**GEÇTİ**|
|375×844|375|320px drawer|318/318|**GEÇTİ**|
|768×844|768|320px drawer|318/318|**GEÇTİ**|
|1024×900|1024|272px sidebar|drawer kapalı|**GEÇTİ**|
|1440×900|1440|272px sidebar|drawer kapalı|**GEÇTİ**|

**Doğrulanan etkileşimler:** aktif-link açılış odağı; Tab/Shift+Tab trap; Escape/kapat/backdrop sonrası trigger odağı ve kilidin kalkması; link seçimi/back-forward/farklı programatik rota/desktop geçişinde kapanış ve main odağı. Son Genel bakış bağlantısına klavyeyle erişimde menü scrollTop662, scrollHeight1410/clientHeight748 ve 2px outline görüldü. Backdrop wheel arka planı kaydırmadı. Uzun içerikte End ile main scroll1035 olurken headerY0/footerBottom844/bodyScroll0 korundu. 320px header menü/arama/dil/bildirim/profil kontrolleri viewport içinde kaldı; aktif Raporlar linki aria-current=page taşıdı.

Kanıtlar: [DOM ölçümleri](STAGE_05_EVIDENCE.json), [375px mobil](STAGE_05_MOBILE.jpg), [1440px masaüstü](STAGE_05_DESKTOP.jpg). Görseller yalnız kurgusal fixture verisi içerir.

## Testler ve sınırlamalar

**173/173 frontend testi, 0fail/skip; typecheck ve production build GEÇTİ.** Önceki testler bu tur yeniden koştu; UI kontrolleri test sayısına eklenmedi. Manifestte lint scripti yok. `git diff --check`, yeni belge bağlantıları, manifest/lock ve iki next-env başlangıç SHA256 koruması kontrol edildi. Git indeksi `git ls-files --stage` başlangıcıyla aynı; commit/push yok. Build'in next-env üretimi yalnız başlangıç byte'larına döndürüldü.

Fixture ilk derlemesinde DefinePlugin export erişimi düzeltildi. Programatik rota testinin ilk denemesi zaten açık /reports yolunu tekrar istedi; pathname değişmediğinden panel açık kaldı. Başka rotadan tekrarlandığında gerçek değişimde kapanış geçti. Belge yazımındaki ara Python sözdizimi hatası düzeltilerek kayıtlar yazıldı.

**Reduced-motion desteği uygulandı; azaltılmış hareketin canlı runtime kabulü DOĞRULANAMADI.** Test tarayıcısında tercih false; derlenmiş conditional CSS kuralları doğrulandı, sağlanan browser API'sinde motion emülasyonu yok. Gerçek cihaz/Safari/iOS, ekran okuyucu, gerçek Keycloak/profil/tenant ve tam WCAG AA kabulü iddia edilmez. Genel smoke/yetki kabulü Aşama07 kapsamındadır.

Yeni üçüncü taraf paket/font/ikon/görsel veya lisans yükü yok. Mevcut React/Next/Radix ve proje kodu kullanıldı; LICENSE/NOTICE korundu. npmci/restore olmadığı için lisans envanteri üretilmedi. .env/hesap/parola/DB/API ve üretim auth kapısı değişmedi. Tek agent; phase/verify/docs ve kaynak/dependency koruması; yeni agent/MCP/Skill yok.

**Bu tur1/1=%100,00; sidebar5/8=%62,50.** Uygulama ve belirtilen fixture kabulü tamamlandı; gerçek ortam/reduced-motion doğrulama sınırları yukarıda ayrıdır. İŞ07.1 hata mesajları farklı görevdir; genel İŞ/UI roadmap sayaçları değiştirilmedi. Genel iş yol haritası güncel toplamı BİLGİ MEVCUT DEĞİL. Aşama06 kullanıcı onayı bekler.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codexusage20.10.2026:300dk kullanılan%58/kalan%42 reset20.10.2026 18:32:07;10080dk kullanılan%9/kalan%91 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.
