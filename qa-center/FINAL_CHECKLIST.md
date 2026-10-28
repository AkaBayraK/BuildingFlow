# Final onay kontrol listesi — Test ve Kalite Merkezi programı

Canlıya alma bu listenin dışındadır ve ayrı kullanıcı onayı gerektirir; otomatik deploy yoktur.

## Faz issue'ları
| Faz | Issue | Durum | Açık kalan |
|---|---|---|---|
| 00 Analiz | — | Tamamlandı | — |
| 01 İzole altyapı | #81 | Kapalı | — |
| 02 Envanter | #82 | Kapalı | bulgular #83 |
| 03 Test Merkezi UI | #84 | Kapalı | — |
| 04 Web otomasyonu | #85 | Kapalı | a11y bulgusu #87 |
| 05 Kimlik/KVKK | #86 | Kapalı | — |
| 06 Finans | #88 | Kapalı | ürün eksikleri #89 |
| 07 Mobil | #103 | **Açık** | iOS dev build, Android SDK, cihaz otomasyon aracı kararı |
| 08 Diğer modüller | #104 | **Açık** | export içerik testleri |
| 09 AkaAI | #105 | **Açık** | gerçek AkaAI entegrasyon istemcisi |
| 10 Test verisi | #106 | **Açık** | finanslı demo verisi kararı |
| 11 Raporlama | #107 | **Açık** | video (ffmpeg), trace korelasyonu |
| 12 Kalite kapısı | #108 | **Açık** | performans testi kararı, `--apply` kullanımı |
| 13 Regresyon/belgeler | #109 | **Açık** | bu liste |

## Zorunlu kontroller
- [x] İzole test ortamı guard'ı her koşumda (Production, gerçek DB/servis, SMS, bayrak sızıntısı reddi)
- [x] Backend, web, mobil, QA araç ve lisans kontrolleri bu turda yeniden çalıştırıldı (bkz. `reports/phase-13.md`)
- [x] Konsolide kanıt raporu ve kalite kapısı üretildi
- [ ] Kalite kapısı PASS — **hayır**: #87 FAIL; performans kanıtı yok
- [ ] Mobil cihaz (iOS/Android) uçtan uca — BLOKE (#103)
- [ ] Online ödeme istemcisi, 3DS, çift taraflı muhasebe — ürün kararı (#89)
- [ ] Gerçek AkaAI bağlantısı — yapılandırma bekliyor (#105)
- [x] Yeni üçüncü taraf paket yok; lisans 329 PASS
- [x] Yerel geliştirme verisi korundu; test verisi yalnız geçici DB/realm/kullanıcıda; geçici kaynaklar silindi
- [ ] Push ve GitHub Actions koşumu — kullanıcıda
