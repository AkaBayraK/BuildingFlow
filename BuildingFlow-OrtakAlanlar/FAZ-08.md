# FAZ 08 — Final kalite ve yayın hazırlığı

## Hedef
E2E, güvenlik, KVKK, SBOM, lisans, Docker smoke, performans, dokümantasyon.

## Referans görsel
![Ortak Alanlar tasarım akışı](assets/ortak-alan-ekran-akisi.png)

## Codex / Claude Code talimatı
MASTER_PROMPT.md kurallarını uygula. Önce bu fazla ilişkili mevcut kodu, testleri, issue'yu ve bağımlılıkları incele. Yapılacak işleri küçük adımlara ayır; her adımı Türkçe konsola yaz. Mevcut bileşenleri yeniden kullan. Uygulamanın rol, tenant ve blok modeline uyum sağla. Gerçek kodu geliştir, migration ve dokümantasyonu tamamla. Sonraki faza geçme.

## Kontrol listesi
- [ ] Mevcut kod ve etkilenen modüller analiz edildi
- [ ] Tasarım ve güvenlik/rol etkisi doğrulandı
- [ ] Gerekli kod, doküman ve testler tamamlandı
- [ ] Tam CI ve kabul raporu
- [ ] Lisans ve dolaylı bağımlılık kontrolü raporlandı
- [ ] GitHub Issue ayrıntılı kanıtlarla güncellendi
- [ ] Kullanıcı kabulü alındı ve kapanış kriterleri sağlandı

## Faz sonu raporu
Değişen dosyalar, commit/PR, test komutları ve sonuçları, ekran görüntüleri, açık riskler, rollback, sonraki faz ön koşulları. Test geçmeden Issue kapatma.
