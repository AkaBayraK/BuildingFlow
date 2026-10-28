# FAZ 07 — Entegrasyon, bildirim ve raporlar

## Hedef
Mevcut Aka Integration Hub veya projedeki bildirim servislerini yeniden kullan; onay/iptal, yaklaşan süre ve ihlal bildirimleri. Giriş-çıkış ve doluluk raporları, CSV/Excel varsa mevcut altyapı. Kamera/ANPR/bariyer entegrasyonu opsiyonel, varsayılan kapalı ve ayrı KVKK etki değerlendirmesi.

## Uygulama talimatı
- MASTER_PROMPT.md dosyasındaki tüm kurallara uy. Mevcut projeyi incele, yeniden kullan ve yalnız bu fazın kapsamını geliştir.
- GitHub Issue başlığı: `[Araçlar/Otopark][FAZ 07] Entegrasyon, bildirim ve raporlar`. Önce mevcut Issue'yu ara, yoksa aç; aynı iş için mükerrer Issue açma.
- UI için `assets/otopark-ui-referans.png` görselini incele; marka diline uyarlayarak erişilebilir responsive tasarım yap.
- Bu fazda gerekli testleri geliştir ve çalıştır. Test çıktısı olmadan tamamlandı yazma.

## Kabul kriterleri
- [ ] Kapsam ve mevcut kod bağımlılıkları analiz edildi
- [ ] Hedef işler eksiksiz yapıldı (FAZ 01: yalnız rapor, kod değişmedi)
- [ ] Tenant/rol/kişi-daire güvenliği değerlendirildi
- [ ] İlgili testler gerçekten çalıştırıldı, sonuçları Issue'da kayıtlı
- [ ] Yeni doğrudan ve transitif bağımlılıklar ve varlıkların ticari/SaaS lisansları kontrol edildi
- [ ] Değişiklikler ve commit/PR/ekran görüntüleri (varsa) Issue'da belgelendi
- [ ] Açık risk ve rollback planı yazıldı
- [ ] Kullanıcı onayı alındı

## GitHub kapanış kuralı
Yukarıdaki tüm maddeler kanıtlı tamamlanmadan Issue açık kalır. Kullanıcı onayı sonrası `completed` olarak kapat; sonra diğer faza geç.
