# [QA FAZ 04 | KAYIT 05/14] Deterministik web/API/DB otomasyonu — GitHub #85

```text
[BUILDINGFLOW]
[FAZ QA 04 | KAYIT 05/14] Deterministik web/API/DB otomasyonu
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#85 açık; bulgu #87)
[İLERLEME] FAZ 04 PHASES maddeleri 3/5 = %60,00 (modül CRUD ve video kısmi)
```

## Ne yapıldı
- `scripts/qa-web-e2e.mjs`: yeni paket olmadan sistem Chrome + DevTools Protocol (Node yerleşik WebSocket). Gerçek PKCE girişi, ekran başına 1280px ve 375px, ekran görüntüsü, JS istisnası, API 5xx/4xx, belge HTTP durumu, yönlendirme, yatay taşma, temel a11y (alt, adsız kontrol, etiketsiz alan, tab aria-selected, başlık). Sonuç `report.json` + "Bu ekranda ... yapıldı" biçiminde `report.md`.
- `scripts/qa-web-lib.mjs` (saf kurallar, 3 test), `scripts/qa-web.sh`: yerel realm'de geçici SUPER_ADMIN (kcadm), koşum sonunda silme ve silindiğinin sorgulanması.
- Bilinçli eski yönlendirme `/sites → /properties` beklenen sayılır; `/workspaces*` bayrak kapalıyken SKIPPED.
- **Bulunan ve düzeltilen hata:** `/properties/{id}/sms` OIDC dönüş whitelist'inde yoktu; sayfa yenilenince kullanıcı `/`'a düşüyordu. `return-path.ts` düzeltildi, navigation testindeki "bilinen boşluk" istisnası kaldırıldı, yeni test eklendi; canlı tekrar koşumda PASS.
- **Bulunan ve düzeltilen test izolasyonu hatası:** `test.sh` .env'deki `PersonRefactor__*` bayraklarını test host'una sızdırıyordu (`CentralPeopleTests.FeatureDefaultsOff...` bayrakla FAIL, bayraksız PASS — iki koşumla kanıtlandı). test.sh bu bayrakları temizliyor; guard `FEATURE_FLAG_LEAK` kuralıyla engelliyor (16/16).
- Açık bulgu #87: APARTMENT alanında Bloklar ekranında h1/h2 yok.

## Gerçek tarayıcı koşumları (bu tur)
| Koşum | Sonuç |
|---|---|
| Tam: `qa-20261028T175351Z-23a92d` | PASS 81 / FAIL 10 / SKIPPED 10. FAIL: buildings başlık (2), sms yönlendirme (2), /sites (2), /workspaces* (4) |
| Düzeltme sonrası alt küme: `qa-20261028T181943Z-9b5fe4` | PASS 9 / FAIL 2 / SKIPPED 2. Kalan FAIL yalnız #87; sms ve /sites PASS; Test Merkezi UI koşumu PASS (Geçti, 2 adım, 375px taşma 0) |

Geçici Keycloak kullanıcısı her koşumda silindi (`[QA WEB] Geçici Keycloak test kullanıcısı silindi.`). Tam koşum düzeltmeden önce alındı; düzeltme sonrası tam koşum tekrarlanmadı. 10 ekran örnek kayıt kimliği (people/[tenantId], buildings/[buildingId] vb.) gerektirdiği için SKIPPED → FAZ 10 demo verisi.

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| Tarayıcı fixture, stabil selector, seed, assertion, screenshot/video | KISMİ: screenshot + assertion var; video yok (CDP screencast FAZ 11), seed FAZ 10 |
| .NET unit/integration (mevcut altyapı) | PASS: test.sh guard'lı izole PG (FAZ 05 paketi 244/244 + 28/28) |
| Her modül CRUD/filtre/validasyon/auth | KISMİ: API tarafında test referansı olmayan endpoint'ler modül bazında: parking 36/39, website 29/33, tenants 19/37, sms 14/18, listings 13/15, late-penalties 12/13, common-areas 7/18 (metin eşleşmesi; FAZ 06/08'de kapatılacak) |
| UI/DB tutarlılığı, negatif ve eşzamanlılık | PASS (Test Merkezi koşumu UI→API→DB, FAZ 03 eşzamanlılık testleri) |
| Ekran bazlı madde madde rapor | PASS (`report.md`) |

## Testler
`node --test scripts/test_qa_web_lib.mjs` 3/3; web `npm test` 301/301, typecheck; lisans 329 PASS; yeni paket yok.

[TOKEN] BİLGİ MEVCUT DEĞİL.
