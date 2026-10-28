# Müşteri demo rehberi (Test ve Kalite Merkezi ile)

Ön koşul: izole/demo ortamı, `QaCenter__Enabled=true`, SUPER_ADMIN hesabı. Production'da çalışmaz.

1. **Demo verisi hazırla** — Sistem Ayarları › Test ve Kalite Merkezi › *Test Verileri* › tür **Demo (korunur)** › *Veri oluştur*. 2 blok, 8 daire, 12 sentetik kişi (`@example.test`) ve malik/kiracı ilişkileri oluşur. *Ekranda aç* ile yönetim alanına geçin.
2. **Yönetim alanı turu** — Yönetim Alanları › demo sitesi: bloklar, daireler, kişiler ve ilişkiler; mobil görünüm için tarayıcıyı 375px'e daraltın.
3. **Kalite güvencesini gösterin** — *Test Senaryoları* › "Veritabanı Migration" › *Testi Başlat* › *Canlı İzleme*: adım adım beklenen/gerçekleşen. *Raporu Gör* ile geçmiş.
4. **AI Test Asistanı** (AkaAI bağlıysa) — "Veritabanı ve yetkileri kontrol et" yazın › plan önerisi (AI önerisi etiketi) › *Onayla ve çalıştır*; bitince *AI ile açıkla*. AI'ın sonucu değiştiremediğini vurgulayın.
5. **Kanıt raporu** — `artifacts/qa/reports/consolidated/report.pdf` (1082 kalem, ekran bazlı "Bu ekranda … yapıldı" satırları).
6. **Temizlik güvenliği** — bir *Test* partisi oluşturup *Verileri Önizle* › onay metni olmadan temizlenmediğini, Demo partisinin korunduğunu gösterin.

Gösterilmemesi gerekenler (henüz ürün eksiği, #89): online ödeme ekranı, 3D Secure, çift taraflı muhasebe; mobil cihaz canlı demosu (#103).
