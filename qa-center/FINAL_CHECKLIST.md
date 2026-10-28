# Final onay kontrol listesi — Test ve Kalite Merkezi programı

Canlıya alma bu listenin dışındadır ve ayrı kullanıcı onayı gerektirir; otomatik deploy yoktur.

## Faz issue'ları
| Faz | Issue | Durum | Açık kalan |
|---|---|---|---|
| 00 Analiz | — | Tamamlandı | — |
| 01 İzole altyapı | #81 | Kapalı | — |
| 02 Envanter | #82 | Kapalı | bulgular #83 |
| 03 Test Merkezi UI | #84 | Kapalı | — |
| 04 Web otomasyonu | #85 | Kapalı | #87 düzeltildi ve kapatıldı |
| 05 Kimlik/KVKK | #86 | Kapalı | — |
| 06 Finans | #88 | Kapalı | ürün eksikleri #89 |
| 07 Mobil | #103 | **Açık** | iOS build ve simülatörde açılış tamam; Android SDK ve cihaz otomasyon aracı kararı |
| 08 Diğer modüller | #104 | Kapalı | export testleri + CSV negatif tutar düzeltmesi |
| 09 AkaAI | #105 | **Açık** | AkaAI yapılandırması hazır; canlı realm client secret + AkaAI yeniden başlatma (kullanıcı) |
| 10 Test verisi | #106 | **Açık** | finanslı demo tamam; rol değiştirme demo kullanıcıları kararı |
| 11 Raporlama | #107 | **Açık** | ekran kaydı tamam (paketsiz); API trace başlığı ürün değişikliği gerektirir |
| 12 Kalite kapısı | #108 | Kapalı | performans p95 22 ms |
| 13 Regresyon/belgeler | #109 | **Açık** | bu liste |

## Zorunlu kontroller
- [x] İzole test ortamı guard'ı her koşumda (Production, gerçek DB/servis, SMS, bayrak sızıntısı reddi)
- [x] Backend, web, mobil, QA araç ve lisans kontrolleri bu turda yeniden çalıştırıldı (bkz. `reports/phase-13.md`)
- [x] Konsolide kanıt raporu ve kalite kapısı üretildi
- [ ] Kalite kapısı PASS — **INCOMPLETE**: regresyon/finans/performans PASS, FAIL 0; 14 SKIPPED (2 opt-in gerçek cihaz/OIDC testi, örnek kayıt veya bayrak isteyen 12 ekran)
- [ ] Mobil cihaz uçtan uca — iOS açılış PASS, Android BLOKE (#103)
- [ ] Online ödeme istemcisi, 3DS, çift taraflı muhasebe — ürün kararı (#89)
- [ ] Gerçek AkaAI bağlantısı — yapılandırma bekliyor (#105)
- [x] Yeni üçüncü taraf paket yok; lisans 329 PASS
- [x] Yerel geliştirme verisi korundu; test verisi yalnız geçici DB/realm/kullanıcıda; geçici kaynaklar silindi
- [ ] Push ve GitHub Actions koşumu — kullanıcıda
