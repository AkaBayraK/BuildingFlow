# BuildingFlow Test ve Kalite Merkezi — UI tasarım spesifikasyonu
Konsept: mevcut BuildingFlow kurumsal tasarımıyla tutarlı, koyu lacivert sol menü, açık içerik, mavi/turkuaz vurgu, oval kartlar, erişilebilir kontrast, responsive.

## Navigasyon
Sistem Ayarları > Test ve Kalite Merkezi
Alt ekranlar: Genel Bakış | Ekran Envanteri | Test Senaryoları | AI Test Asistanı | Test Çalıştır | Canlı İzleme | Test Verileri | Test Sonuçları | Hata Yönetimi | Temizle/Sıfırla | Ayarlar.

## Genel Bakış
Kartlar: modül/ekran/senaryo sayısı, PASS/FAIL/BLOCKED/SKIPPED, çalışan testler, son koşum, test verisi sayısı. Modül kapsam çubukları, son hata listesi ve ortam etiketi.

## AI Test Asistanı
Türkçe prompt kutusu, örnek şablonlar, kapsam seçici, test planı önizleme, riskli işlem uyarısı, maliyet/tahmini süre, onay ve çalıştır. AI önerisi ile doğrulanmış sonuç görsel olarak ayrı.

## Canlı İzleme
Sol: senaryo adımları ve durum; orta: web tarayıcı görüntüsü/video; sağ: iOS/Android görüntüsü ve geçiş; alt: API/trace/log, transaction zaman çizelgesi, ekran görüntüleri. Gerçek akış mümkün değilse son alınan ekran görüntüsünün zaman damgasını göster.

## Test Verileri
Demo/Test ayrımı, yönetim alanı, kişiler, daireler, tahakkuk, borç, ödemeler, makbuzlar ve muhasebe sekmeleri. Satırdan gerçek uygulama ekranına güvenli bağlamda git; dry-run cleanup, açık onay, test_run_id filtre.

## Test Sonuçları
Her ekran için şu örüntü: `1. [PASS] Kişiler > Yeni kişi oluşturuldu. Beklenen: ... Gerçek: ... Kanıt: ...`; başarısız testlerde hata kodu, adım, screenshot, trace ve GitHub issue. PDF/Excel/JSON export.

## Finans ekranı
`Tahakkuk > Borç > Payment Intent > Sandbox/3DS > Webhook > Ödeme Durumu > Bakiye > Makbuz > Muhasebe > Mutabakat` görsel zaman çizelgesi; idempotency, retry, duplicate ve timeout simülasyonları.

## Görsel doğrulama
Konsept ekranlarını doğrudan kod olarak yorumlama; gerçek uygulama veri ve endpointlerine bağla. Boş/yükleniyor/hata/erişim engellendi/başarılı durumlarını ve mobil responsive görünümü ayrı test et.
