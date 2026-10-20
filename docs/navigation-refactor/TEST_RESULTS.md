# Sidebar test sonuçları ve manuel QA

20.10.2026 — SIDEBAR FAZ07.1 Aşama08 belge teslimi. **Bu aşamada uygulama testleri yeniden çalıştırılmadı.** Aşağıdaki sonuçlar tarihli [Aşama07](STAGE_07.md) kanıtından aktarılır; geçerli kayıt [TESTING](../TESTING.md)'dir.

| Son doğrulama | Sonuç | Kaynak |
|---|---|---|
| Frontend npm test | **177/177,0fail/skip** | Aşama07 |
| Frontend typecheck/production build | **Geçti** | Aşama07 |
| Lint | Komut manifestte yok, çalıştırılmadı | Aşama07 |
| .NET unit | **277/277,0fail/skip** | Aşama07 |
| Seçili PostgreSQL integration | **83/83,0fail/skip** | Aşama07; kapsam/filtre raporda |
| Dashboard/daire/kişi/finans/website navigasyon, geri-ileri, ikinci alan doğrudan URL,375px drawer | **Fixture geçti** | [Olay kanıtı](STAGE_07_EVIDENCE.json) |
| 320/375/768/1024/1440px,Tab/ShiftTab/focus/backdrop/Escape/scroll | **Fixture geçti** | [Aşama05](STAGE_05.md), [olaylar](STAGE_05_EVIDENCE.json) |
| Profil güncelleme, desktop/dar/mobile footer | **Fixture geçti** | [Aşama06](STAGE_06.md), [olaylar](STAGE_06_EVIDENCE.json) |
| Sidebar/üst menü12px | **Computed CSS geçti** | [Font görseli](MENU_FONT_FIX.jpg), Aşama06 |
| Gerçek OIDC/iş ekranı/tenant/çıkış/fotoğraf | **DOĞRULANAMADI** | Fixture gerçek kimlikli kabul yerine geçmez |
| Gerçek ağ kesintisinde offline shell | **DOĞRULANAMADI** | Offline emülasyonu yok; cache/offline güvence iddiası yok |
| Gerçek cihaz/ekran okuyucu/aktif reduced-motion | **DOĞRULANAMADI** | CSS desteği runtime cihaz kabulünden ayrıdır |

Yetkisiz/farklı alan/context iptali mevcut backend integration testlerinde; UI rol/flag/UUID/deduplikasyon/nested-route kuralları frontend testlerinde doğrulandı. Tam backend suite, yayın testi, SAST/CVE taraması veya tüm cihaz matrisi çalıştırıldı iddiası yok. Geçici DB/rol temizlendi; fixture sunucu/sekme kapalıdır. Komut ve kaynak temizliği ayrıntısı Aşama07'de.

## Açık davranış

Mevcut scoped `/properties/{id}/sms` OIDC dönüş allowlist kısıtı korunur; genel `/sms` desteklenir. Kök `/` dönüş davranışı da önceki testte kayıtlıdır. Bu auth sözleşmesi refactor kapsamında genişletilmedi. Sistem Ayarları ve diğer planlı öğeler routesuz disabled kalır.

## Manuel QA kontrol listesi

Aşağıdaki kutular **gerçek ortam kabulü için açık** tutulur; fixture başarısıyla işaretlenmedi. Test hesapları kullanılır; gerçek veriyi değiştiren finans/rol işlemleri gerekmez.

- [ ] Yetkili oturumla Dashboard, Daireler, Kişiler, Aidat/Tahsilat/Gider/Raporlar ve Web Sitesi Yönetimi içeriklerini aç; URL/başlık/aktif menü eşliğini kontrol et. Ayarlar planlı/pasif kalmalı.
- [ ] SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT ve sakin hesaplarında mevcut yetki matrisini doğrula; ACCOUNTANT kişi menüsü/verisi kazanmamalı. Görünmeyen hedefe doğrudan URL/API isteği backend tarafından reddedilmeli.
- [ ] İki üyelikli alanda alan değiştir; doğru modül seçicisi ve alan adı gelsin. Üye olunmayan alan UUID'siyle erişim reddedilsin; önceki alan verisi görünmesin.
- [ ] Doğrudan deep link, nested route, yenileme ve geri/ileri sonrası aktif grup/öğe ve içerik eşliğini doğrula; scoped SMS dönüş kısıtını ayrıca kaydet.
- [ ] Profil adı/fotoğrafı güncelle, farklı hesapla yeniden giriş yap; eski profil kalmasın. OIDC callback/refresh/logout mevcut davranışı korusun.
- [ ] Masaüstü geniş/dar mod ve yenilemede tercih, tooltip, TR/EN ve Türkçe arama/boş sonuç/komut kısayolunu kontrol et; storage kapalıyken kabuk çalışmalı.
- [ ] 320/375/768/1024/geniş ekran ve iOS/Android tarayıcıda yatay taşma, safe-area, footer erişimi,44px hedef ve eşit12px menü fontunu doğrula.
- [ ] Tab/ShiftTab/focus ring, drawer trap, Escape/backdrop/rota kapanışı, odağın içerik veya tetikleyiciye dönüşü, scroll lock ve masaüstüne geçişte overlay temizliğini kontrol et.
- [ ] Sistem reduced-motion açıkken geçişleri, ekran okuyucuda menü/grup/aktiflik/arama duyurularını kontrol et.
- [ ] Önceden yüklenmiş oturumda ağı gerçekten kes; aç/kapat/arama/daraltmayı dene. Tam offline yenilemenin destek durumunu kaydet. Eksik/eskimiş yetkiyle hassas ekran/veri erişimini başarılı sayma; yeniden bağlantıda sunucu reddini kontrol et.

## Aşama08 belge doğrulaması

Yerel bağlantılar, sekiz resmi adım sayacı, belge/yönerge eşliği, yeni bağımlılık olmaması ve başlangıç uygulama/indeks hash koruması teslimde doğrulandı (PASS). Uygulama testi bu tur tekrarlanmaz. Teknik sidebar teslimi8/8=%100,00; açık manuel kabul ayrı kalır.

Aşama08 değişen dosyaları: IMPLEMENTATION.md,TEST_RESULTS.md,LICENSE_CHECK.md; docs/frontend-rules.md,SKILLS_AND_AGENTS.md,TESTING.md ve progress STATUS/SESSION/CHANGELOG. Başlangıç uygulama/test/manifest/lock/lisans kaynak dosyaları ve Git indeksi aynı kaldı.

Token giriş/çıkış/toplam/kalan context: her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%21/kalan%79 reset20.10.2026 23:36:32;10080dk kullanılan%19/kalan%81 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.
