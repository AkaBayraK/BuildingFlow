# FAZ 08 — Final kalite, güvenlik, lisans ve yayın

## Hedef
Tüm fazların unit/integration/E2E/regresyon, tenant çapraz erişim, concurrency, KVKK, OWASP, performans, erişilebilirlik ve Docker smoke testlerini yürüt. SBOM ve transitive license raporu; ticari/SaaS/dağıtım uyumu, üçüncü taraf görsel hakları, migration rollback ve release checklist.

## Uygulama talimatı
- MASTER_PROMPT.md dosyasındaki tüm kurallara uy. Mevcut projeyi incele, yeniden kullan ve yalnız bu fazın kapsamını geliştir.
- GitHub Issue başlığı: `[Araçlar/Otopark][FAZ 08] Final kalite, güvenlik, lisans ve yayın`. Önce mevcut Issue'yu ara, yoksa aç; aynı iş için mükerrer Issue açma.
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
