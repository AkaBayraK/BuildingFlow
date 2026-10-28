# [QA FAZ 11 | KAYIT 12/14] Canlı izleme, ayrıntılı kanıt ve raporlama — GitHub #107

```text
[BUILDINGFLOW]
[FAZ QA 11 | KAYIT 12/14] Kanıt ve raporlama
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR
[İLERLEME] FAZ 11 PHASES maddeleri 3/5 = %60,00 (video ve trace/ödeme korelasyonu BLOKE/KISMİ)
```

## Ne yapıldı
- `scripts/qa_report.py`: gerçek koşum klasörlerini (CDP web `report.json`, kimlik `report.json`, .NET TRX) tek kanıt setinde birleştirir. Alanlar TEST_MATRIX kanıt formatıyla: run_id, modül, ekran, senaryo, rol, platform (web-desktop/web-mobile/api/keycloak), başlangıç, süre, beklenen, gerçekleşen, durum, ekran görüntüsü, issue. Çıktılar: **JSON, Markdown ("Bu ekranda ... işlemi yapıldı; beklenen ..., gerçekleşen ..., kanıt ...")**, **CSV** (UTF-8 BOM, `;` — Excel doğrudan açar), **HTML** ve sistem Chrome ile **PDF**. Yeni paket yok.
- Tekrar koşum önceki sonucun yerine geçer, eski durum `history` alanında kalır; sonraki web koşumu aynı ekranı yeniden sınıflandırırsa eski web satırları düşer. `--previous` ile **geçmiş karşılaştırma**: yeni FAIL, düzelen, hâlâ FAIL. `qa-center/known-issues.json` ile PASS olmayan kalemlere GitHub issue bağlantısı.
- `test_qa_report.py` 7/7 fixture, CI'a bağlandı.

## Gerçek çıktılar (artifacts/qa/reports, git dışı)
| Rapor | Sonuç |
|---|---|
| `web-before` (tam tarayıcı koşumu) | 101 kalem: PASS 81 / FAIL 10 / SKIPPED 10 |
| `web-after --previous web-before` | 13 kalem: PASS 9 / FAIL 2 / SKIPPED 2; **düzelen 4** (sms ×2, /sites ×2), yeni FAIL 0, hâlâ FAIL 2 (#87) |
| `consolidated` (web + kimlik + kişi/finans/modül paketleri + tekrarlar) | **1082 kalem: PASS 1068 / FAIL 2 / SKIPPED 12**; FAIL yalnız #87 (Bloklar başlığı, masaüstü+mobil); SKIPPED: örnek kayıt gerektiren 10 ekran + bayrak kapalı 2 ekran |

ParkingMigrationTests düzeltmesi bu turda TRX ile yeniden koşuldu (5/5) ve raporda PASS olarak geçti.

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| Adım ekranı, zamanı, platformu, beklenen/gerçek | PASS |
| API, maskeli log, trace, ödeme işlemi, webhook, DB assertion | KISMİ: API/DB assertion'ları TRX'te, KVKK log taraması 0; API trace/correlation başlığı döndürmüyor, ödeme/webhook korelasyonu test içinde (ayrı kanıt satırı yok) |
| Ekran/modül/rol/platform bazlı PASS/FAIL/BLOCKED/SKIPPED | PASS |
| PDF/Excel/JSON, geçmiş karşılaştırma, issue bağlantıları | PASS (Excel = CSV; .xlsx yeni paket gerektirir) |
| "Bu ekranda ... yapıldı" Türkçe rapor | PASS |
| Video (FAZ 04'ten taşınan) | **BLOKE**: ffmpeg yok, yeni paket yasağı; ekran görüntüleri var |
| Ürün CSV/PDF export içerik testleri (FAZ 08'den taşınan) | KISMİ: uygulanmadı |

[TOKEN] BİLGİ MEVCUT DEĞİL.
