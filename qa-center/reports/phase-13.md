# [QA FAZ 13 | KAYIT 14/14] Tam regresyon, dokümantasyon ve devreye alma hazırlığı — GitHub #109

```text
[BUILDINGFLOW]
[FAZ QA 13 | KAYIT 14/14] Tam regresyon
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR — canlıya alma yapılmadı
[İLERLEME] FAZ 13 PHASES maddeleri 4/5 = %80,00 (uçtan uca kapsamda mobil cihaz ve ödeme istemcisi BLOKE)
```

## Tam regresyon (bu tur, run `qa-20261028T192449Z-1041bf`)
| Alan | Sonuç |
|---|---|
| Backend entegrasyon (izole PG, guard GEÇTİ) | **900/902 PASS**, 2 atlanan (gerçek OIDC/tarayıcı gerektiren opt-in: ContactNativeHost, PhoneContactBrowserHost) |
| Backend unit | **434/434** |
| Web `npm test` + typecheck | 308/308 + temiz |
| Mobil `npm run check` | 160/160 + typecheck + lint |
| Tarayıcı (masaüstü + 375px, geçici SUPER_ADMIN silindi) | PASS 84 / FAIL 3 / SKIPPED 12 |
| Geçici Keycloak realm | 11/11, realm silindi |
| QA araç fixture'ları | guard 16, envanter 11, KVKK 13, kapsam 3, rapor 7, GitHub 15, web-lib 3 — hepsi PASS |
| Lisans / KVKK log | 329 PASS / 0 bulgu |
| Konsolide rapor | 1446 kalem: PASS 1429 / FAIL 3 / SKIPPED 14; PDF `artifacts/qa/reports/regression-faz13/report.pdf` |
| Kalite kapısı | **RED**: regresyon 3 FAIL, güvenlik 1 SKIPPED, finans 184 PASS, performans kanıt yok |

## Kök neden kaydı
- **İlk tam koşum 399 FAIL, ikinci (paralellik 2) 504 FAIL — hepsi `53300`**: her test sınıfı kendi şemasıyla ayrı Npgsql havuzu açıyor, boşta bağlantılar 100'lük sunucu sınırını dolduruyordu. `scripts/test.sh` test bağlantısına `Maximum Pool Size=10;Connection Idle Lifetime=2;Connection Pruning Interval=1` ekliyor; üçüncü koşum 900/902. Kanıt: `backend-parallel-unlimited/`, `backend-parallel-2/`, `backend/` klasörleri.
- **FAIL 1–2:** #87 (APARTMENT Bloklar ekranında başlık yok) — açık ürün bulgusu.
- **FAIL 3 (yeni):** Test Merkezi UI koşumu. Ekran "bu ortamda kapalı" gösterdi: yerel API şu an `QaCenter__Enabled` olmadan çalışıyor (FAZ 03'te açık başlatılmıştı; sonradan dışarıdan yeniden başlatılmış). Kod hatası değil, ortam; `QaCenterData` migration'ı da yerel DB'ye uygulanmadı. Onayınızla API açık başlatılıp koşum tekrarlanacak.

## Belgeler
- `qa-center/OPERATIONS.md`: kurulum, işletim komutları, felaket kurtarma, test verisi temizliği, güvenlik, lisans.
- `qa-center/DEMO_GUIDE.md`: müşteri demo akışı ve gösterilmemesi gerekenler.
- `qa-center/FINAL_CHECKLIST.md`: faz issue durumu ve zorunlu kontroller.

## Eksik kapsam / açık kararlar
#87 a11y, #89 ödeme ürün eksikleri, #103 mobil cihaz, #104 export içerik testleri, #105 gerçek AkaAI, #106 finanslı demo, #107 video/trace, #108 performans testi ve `--apply`. Flaky: 53300 (düzeltildi, tekrar doğrulandı). Canlıya alma ayrı kullanıcı onayı gerektirir; otomatik deploy yok.

Not: Koşumlar ana klasörde yapıldığı için paralel oturumun commit edilmemiş kodu (tedarikçi) da derlendi ve testlere dahildir.

[TOKEN] BİLGİ MEVCUT DEĞİL.
