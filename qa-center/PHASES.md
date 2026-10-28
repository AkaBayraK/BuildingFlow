# Faz bazlı uygulama planı
Her faz: issue aç > alt adımları checklist yap > küçük parçalar halinde uygula > her adımdan sonra test ve kanıt > kapsam kontrolü > issue'ya Türkçe ayrıntılı rapor > yalnızca tüm zorunlu maddeler PASS ise kapat > onay bekle.

## FAZ 00 — Salt okunur analiz ve envanter (ilk çalıştırma)
- [ ] BuildingFlow ve AkaAI repo yapısı, branch, CI, sürümler, bağımlılıklar, mimari haritası
- [ ] Web menü/alt menü/route/component/API/form/action envanteri
- [ ] Mobil ekran, navigasyon, SQLite, senkronizasyon ve iOS/Android test imkânı
- [ ] Finansal domain, ödeme sağlayıcı sandbox, webhook ve muhasebe kuralları
- [ ] Mevcut testler, eksik kapsam, test ortamı izolasyonu, güvenlik/KVKK/lisans analizi
- [ ] GitHub issue şablonu, faz bağımlılıkları, riskler, efor ve kabul kriterleri
- [ ] Onay bekle; değişiklik yapma

## FAZ 01 — İzole test altyapısı ve güvenlik
- [ ] Ayrı DB/realm/storage/queue/test secrets ve environment guard
- [ ] Sandbox ödeme, sahte SMS/e-posta, sentetik kullanıcı ve mobil test profili
- [ ] Prod'a yazma/silme engeli, rol/yetki ve tenant sınırları
- [ ] CI yerel/izole çalıştırma, lisans ve dependency taraması
- [ ] Entegrasyon smoke test ve kanıt

## FAZ 02 — Ekran/API keşfi ve test matrisi
- [ ] Web tüm route/menü, ekran, buton, form, grid ve iş aksiyonları
- [ ] Mobil tüm ekran, rol ve navigasyon yolları
- [ ] CRUD, arama, filtre, sıralama, sayfalama, validasyon, export, auth, error/empty/loading
- [ ] UI ↔ API ↔ DB eşlemesi ve kapsama raporu
- [ ] Eksik/erişilemeyen ekranlar için issue

## FAZ 03 — Test merkezi UI ve orkestrasyon
- [ ] Sistem Ayarları > Test ve Kalite Merkezi RBAC korumalı menü
- [ ] Genel bakış, envanter, senaryo, manuel başlat/durdur/yeniden dene
- [ ] İş kuyruğu, progress, timeout/cancel, koşum geçmişi, durum makinesi
- [ ] Web/mobil ekran görüntüsü, adım listesi, log/trace paneli
- [ ] Responsive ve erişilebilirlik testleri

## FAZ 04 — Deterministik web/API/DB CRUD otomasyonu
- [ ] Playwright fixture, stabil selector, seed, assertion, screenshot/video
- [ ] .NET unit/integration + Testcontainers veya uyumlu mevcut altyapı
- [ ] Her modül: create/read/update/delete veya soft-delete, filtre, validasyon, auth
- [ ] UI/DB tutarlılığı, negatif ve eşzamanlılık testleri
- [ ] Ekran bazlı madde madde rapor

## FAZ 05 — Kişi, alan, daire, rol ve KVKK akışları
- [ ] Yönetim alanı/site/blok/apartman/daire oluşturma
- [ ] Alan bağımsız kişi kaydı ve çoklu daire ilişkisi
- [ ] Malik/kiracı/sakin/yönetici çoklu rol ve farklı alan erişimi
- [ ] Keycloak, doğrulama, yetkisiz erişim ve maskeli veri
- [ ] Web ve mobil görünüm karşılaştırması

## FAZ 06 — Finans: tahakkuk, borçlandırma, ödeme, muhasebe
- [ ] Aidat parametreleri, tahakkuk, borçlu ataması, mükerrer tahakkuk önleme
- [ ] Web merkez paneli üzerinden malik/kiracı ödeme
- [ ] Mobil iOS/Android online sandbox ödeme; offline ödeme engeli
- [ ] Payment intent, 3DS simülasyonu, callback/webhook, imza, idempotency
- [ ] Timeout, ret, duplicate tap, duplicate/out-of-order webhook, retry, bağlantı kopması
- [ ] Kısmi/fazla ödeme, iade/iptal, bakiye, makbuz, muhasebe ve mutabakat
- [ ] Uçtan uca trace, log, ekran videosu ve veri doğrulama

## FAZ 07 — Mobil ve offline-first otomasyonu
- [ ] iOS ve Android kurulum, oturum, rol/daire değiştirme, ekran CRUD
- [ ] SQLite offline okuma/yazma izinli işlemler, sync, çakışma ve tekrar deneme
- [ ] Bağlantı kopması, uygulama kapanması, background/foreground
- [ ] Bildirim, fotoğraf/KVKK izinleri, mobil ekran kanıtı
- [ ] Online ödeme kuralı ve finansal tutarlılık

## FAZ 08 — Diğer tüm modüllerin iş akışları
- [ ] Talep/arıza, duyuru, anket, planlama, bakım, temizlik
- [ ] Araç/otopark, web sitesi, ilan/yönlendirme, iletişim, raporlar
- [ ] SMS/e-posta/notification, export PDF/Excel, audit ve sistem ayarları
- [ ] Mevcut repoda bulunan diğer tüm menülerin envantere göre testleri
- [ ] Yeni ekranlar için otomatik kapsam boşluğu tespiti

## FAZ 09 — AkaAI entegrasyonu ve AI Test Agent
- [ ] AkaAI mevcut gateway/model routing/agent/MCP kapasitesi analizi
- [ ] Güvenli API kontratı, RBAC, allowlist araçlar, tenant izolasyonu
- [ ] Türkçe prompt → schema-validated plan → kullanıcı onayı → deterministic run
- [ ] AI hata/kök neden analizi, maliyet ve token bütçesi, cache/fallback
- [ ] Prompt injection, yanlış plan, tool misuse ve kanıtsız PASS testleri
- [ ] AkaAI bağımsız ürün olarak diğer projelere yeniden kullanılabilirlik

## FAZ 10 — Demo/test verileri ve temizleme
- [ ] Gerçekçi sentetik demo site, blok, daire, kişi, borç, ödeme ve makbuz üretimi
- [ ] Ekranlardan veriyi görüntüleme, rol değiştirerek müşteri demosu
- [ ] test_run_id, tenant, environment etiketli veri ayrımı
- [ ] Dry-run temizleme önizlemesi, açık onay, bağımlılık sırası, rollback
- [ ] Demo verisi ve audit retention korunması; prod guard testi

## FAZ 11 — Canlı izleme, ayrıntılı kanıt ve raporlama
- [ ] Her adımın ekranı, zamanı, kaynak platformu, beklenen/gerçek sonuçları
- [ ] API, masked log, trace, payment transaction, webhook, DB assertion
- [ ] Ekran/modül/rol/platform bazlı PASS/FAIL/BLOCKED/SKIPPED ayrımı
- [ ] PDF/Excel/JSON rapor, geçmiş karşılaştırma, GitHub issue bağlantıları
- [ ] `Bu ekranda ... yapıldı` maddeli Türkçe rapor

## FAZ 12 — Hata analizi, GitHub otomasyonu ve kalite kapısı
- [ ] Kanıtlı GitHub bug issue, tekrar üretim adımları ve severity
- [ ] Her faz issue checklist'i ve adım sonrası Türkçe durum güncellemesi
- [ ] Tüm zorunlu kriterler PASS ise issue otomatik kapama; aksi halde açık
- [ ] Güvenlik, regresyon, performans ve finansal mutabakat kalite kapısı
- [ ] CI entegrasyonu; varsayılan manuel tetikleme

## FAZ 13 — Tam regresyon, dokümantasyon ve devreye alma hazırlığı
- [ ] Tüm ekran, rol, API, mobil ve finans senaryolarını uçtan uca çalıştır
- [ ] Eksik kapsam, flaky test ve açık hataları raporla
- [ ] Kurulum/işletim, felaket kurtarma, temizlik, güvenlik ve lisans belgeleri
- [ ] Demo sunum rehberi ve final onay kontrol listesi
- [ ] Canlıya alma ayrı kullanıcı onayı gerektirir; otomatik deploy yapma
