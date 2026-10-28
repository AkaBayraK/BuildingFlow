# [QA FAZ 02 | KAYIT 03/14] Ekran/API keşfi ve test matrisi — GitHub #82

```text
[BUILDINGFLOW]
[FAZ QA 02 | KAYIT 03/14] Ekran/API keşfi ve test matrisi
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#82 açık, bulgular #83)
[İLERLEME] FAZ 02 PHASES maddeleri 4/5 = %80,00 (mobil rol eşlemesi kısmi)
```

## Değişen dosyalar
- `backend/tests/BuildingFlow.IntegrationTests/QaEndpointInventoryTests.cs` (yeni): gerçek API host'unun `EndpointDataSource`'undan method/route/yetki dökümü. `/api/public/` dışındaki her `/api` endpoint'inin açık yetki kararı yoksa test FAIL olur. DB gerektirmez.
- `scripts/qa_inventory.py` (yeni): döküm + web sayfa/menü/feature API çağrıları + mobil ekran/sekme görünürlüğü/API çağrıları + EF snapshot tabloları + backend test referansları → `qa-center/inventory/inventory.json`, `coverage.md`. Zaman damgasız, deterministik.
- `scripts/test_qa_inventory.py` (yeni): 11 eşleştirici fixture'ı; `scripts/ci.sh`'e bağlandı.
- `qa-center/inventory/` (üretilmiş çıktı), `qa-center/reports/phase-02.md`.

## Sonuçlar
| Kalem | Sayı |
|---|---|
| API endpoint (çalışma zamanı) | 398 (statik sayım 388; döngüyle üretilen route'lar dahil) |
| Yetkili / açık anonim / public prefix | 385 / 5 / 8 |
| Web: tam eşleşme / yalnız taban yol | 131 / 212 |
| Mobil: tam eşleşme / yalnız taban yol | 20 / 30 |
| Backend testlerinde route metni geçen | 186 |
| Hiç tüketicisi olmayan | 12 |
| Web sayfası / menü bağlantısı / planlı menü | 55 / 8 / 2 |
| Mobil ekran / sekme (her zaman, bağlı kişi, gizli) | 9 / 2, 4, 1 |
| DB tablosu | 108 |

Ekran başına create/update/delete/search/filter/paging/export/table/form sinyalleri `coverage.md` tablosunda; **metin sezgisidir, test kanıtı değildir**.

## PHASES kontrol listesi
| Madde | Durum | Kanıt |
|---|---|---|
| Web route/menü/ekran/buton/form/grid/aksiyon | PASS | 55 sayfa, feature eşlemesi, form/table sinyali, API çağrıları |
| Mobil ekran, rol, navigasyon | **KISMİ** | 9 ekran ve sekme görünürlük kuralı (bağlı kişi bağlamı) çıkarıldı; ekran içi yönetici/malik/kiracı rol dalları eşlenmedi → FAZ 07 |
| CRUD/arama/filtre/sayfalama/validasyon/export/auth/durumlar | PASS (statik) | Sinyal tablosu + endpoint yetki politikası; davranış testi FAZ 04 |
| UI ↔ API ↔ DB eşleme ve kapsama raporu | PASS | `inventory.json`, `coverage.md`; DB tablo↔endpoint doğrudan eşlemesi FAZ 04 sorgu kanıtıyla |
| Eksik/erişilemeyen ekranlar için issue | PASS | #83 (mükerrer arandı, yok) |

## Testler (bu tur çalıştırıldı)
- `QaEndpointInventoryTests`: 1/1 PASS (ilk koşum 8 işaretsiz public route nedeniyle FAIL verdi; bunlar tasarım gereği public, ayrı sınıflandırıldı ve #83'e yazıldı).
- `test_qa_inventory.py` 11/11, `test_qa_guard.py` 15/15 (FAZ 01), iki üretim çalıştırması `cmp` ile aynı (deterministik).
- `check-licenses.py` 329 PASS; yeni bağımlılık yok. Tam backend/web/mobil regresyonu bu tur çalıştırılmadı.

[TOKEN] BİLGİ MEVCUT DEĞİL.
