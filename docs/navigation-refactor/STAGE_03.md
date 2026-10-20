# SIDEBAR FAZ07.1 — Aşama03 teslimi

19.10.2026. Yalnız buildingflow-sidebar-phase/PHASE_07_1.md Aşama03/08 onaylandı. Aşama04 başlamadı.

**Mevcut navy/blue tokenlarıyla 272px sidebar ve 76px ikon modu; 44px satırlar, ikon/etiket taşma koruması, mavi aktif satırda ince cyan sol vurgu, hover/focus ve pasif durumlar uygulandı.** Yeni grup şeması oluşturulmadı. Mevcut Ana Menü/İşlemler/Yönetim grupları geniş modda açılıp kapanır; aktif rotanın grubu açık kalır. Dar modda grup düğmeleri gizlenir ve bütün mevcut öğeler erişilebilir kalır. Pasif öğeler aktif hale getirilmez; erişilebilir adları etiket+Planlandı açıklamasını taşır.

Dar masaüstü modunda mevcut radix-ui Tooltip kullanılır; portal sayesinde sidebar scroll alanı tarafından kesilmez. Klavye/fare odağı ve Escape davranışı mevcut primitive'e aittir. Mobil drawer'da tooltip açılmaz. Genişlet/daralt kontrolü TR/EN etiket, aria-expanded ve hedefe uygun aria-controls taşır. Yeni animasyon eklenmedi.

Görünüm tercihi `buildingflow-sidebar-v1` anahtarında yalnız `collapsed` veya `expanded` sabit değeridir. Kimlik, tenant, arama, route, token veya kişi verisi saklanmaz. SSR ve ilk client render geniş görünümle eşleşir; tercih hydration sonrasında okunur. Storage getter/okuma/yazma reddedilirse bellek içi kontrol kullanılabilir kalır. Eski/bozuk değer dar modu açmaz. Grupların aç/kapat state'i kalıcı depolanmaz.

Değişen uygulama dosyaları: WorkspaceShell.tsx; globals.css; messages/tr.json ve en.json; yeni features/navigation/SidebarHint.tsx ve sidebar-preference.ts. Yeni tests/sidebar-preference.test.mjs iki anlamlı storage testini içerir. Önceki Aşama02 navigation şeması/rol koşulları ve backend/API/DB değişmedi.

**169/169 frontend testi,0fail/skip; typecheck ve production build GEÇTİ.** Storage testleri yalnız sabit sürümlü anahtar/değer, geçersiz/eski değer ve erişim/kota hatalarında fallback'i doğrular. Web manifestinde lint scripti yok. Native ekran okuyucu, tüm viewportlar ve offline/backend testleri bu tur koşulmadı.

**Gerçek oturumlu Chrome /properties üzerinde:** geniş→dar, sayfa yenilemesi sonrası dar modun korunması, Tab ile Dashboard odağında tooltip ve cyan focus çizgisi, Escape ile tooltip kapanması, geniş moda dönüş, İşlemler grubunu kapatınca bağlantıların erişilebilirlik ağacından çıkması/yeniden açılması, aktif Ana Menü grubunun görünür kalması doğrulandı. Aynı route/oturum ve Türkçe korunarak ekran geniş modda bırakıldı. Gerçek iş kaydı oluşturma/güncelleme/silme yapılmadı; parola girilmedi, çıkış yapılmadı. Screenshot görsel olarak incelendi; kullanıcıya ait alan verileri içerdiği için rapora yeni ekran görüntüsü dosyası eklenmedi. Ölçüler CSS sözleşmesidir; DOM üzerinden sayısal viewport/kontrast ölçümü yapılmadı. Genel WCAG AA/gerçek cihaz/tüm hover durumları kabulü iddia edilmez.

Yeni paket/font/ikon/görsel eklenmedi. Kurulu Radix Tooltip, mevcut Lucide ve proje kodu kullanıldı. Manifest/lock ve başlangıçta değiştirilmiş iki next-env dosyası SHA256 ile korundu; build'in ürettiği next-env değişikliği yalnız başlangıç byte'larına geri döndürüldü. Restore/npmci olmadığından lisans envanteri yeniden üretilmedi; LICENSE/NOTICE korunur. Yeni skill/MCP/agent kurulmadı; tek agent phase/verify/docs ve bağımlılık koruma kontrolü kullandı.

**Sidebar3/8=%37,50; bu tur1/1=%100,00.** İŞ07.1 hata mesajları ayrı görevdir; ürün roadmap veya UI12faz sayaçları değiştirilmedi. Kaynak planın Aşama03 kabulü bu rapor/TESTING ile bağlanır. Sonraki Aşama04 kullanıcı onayı bekler. Aşama05 mobil testleri veya Aşama06 footer işi başlatılmadı.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Codexusage19.10.2026:300dk kullanılan%31/kalan%69 reset20.10.2026 00:50:09;10080dk kullanılan%99/kalan%1 reset25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.
