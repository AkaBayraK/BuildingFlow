# [QA FAZ 08 | KAYIT 09/14] Diğer tüm modüllerin iş akışları — GitHub #104

```text
[BUILDINGFLOW]
[FAZ QA 08 | KAYIT 09/14] Diğer modüller
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#104 açık)
[İLERLEME] FAZ 08 PHASES maddeleri 4/5 = %80,00 (export davranış testi FAZ 11'e)
```

## Modül paketleri (izole PostgreSQL, guard GEÇTİ, run `qa-20261028T184812Z-49a6ac`)
Kapsam: talep/arıza (RequestManagement, MobileRequest), duyuru/belge (Communication, MobileNotice), anket (Survey), bakım (Maintenance), ortak alan (CommonArea*), otopark (Parking, MobileParking), web sitesi/alan adı/medya (Website*), ilan (Listing), SMS/izin (Sms*, SmsConsent), e-posta (EmailIdentity), mesajlaşma (Messaging*), rapor (ReportApi), sayaç (Meter), onay metinleri (Consent*, CommercialConsent, DefaultConsentText), sistem durumu ve hava durumu.

| Koşum | Sonuç |
|---|---|
| İlk koşum | Integration **281/302** (21 FAIL), Unit 237/237 |
| Yalnız 21 FAIL tekrar | 20/21 PASS; kalan 1 = ParkingMigrationTests retention |
| Düzeltme sonrası Parking + QaCenter migration | **4/4 PASS** |

**Kök nedenler (tekrar üretilerek):**
1. **20 test, paylaşılan PostgreSQL bağlantı sınırı** (`53300 remaining connection slots`, max_connections=100): eşzamanlılık testleri 500 aldı. Tek başına tekrar koşumda PASS. Flaky, ürün hatası değil; aynı sunucuyu kullanan paralel iş yükü + tek koşumda ~300 entegrasyon testi. Öneri: test sunucusunda max_connections artırımı veya test paralelliğinin sınırlanması (karar sizin).
2. **ParkingMigrationTests.Retention...** benim FAZ 03 migration'ım yüzünden kırılmıştı: test `migrations[^2]`'yi retention migration'ı varsayıyordu. Kanıt: aynı test d508154'te PASS, HEAD'de FAIL. Test, retention migration'ına isimle bağlandı; reddedilen Down sonrası (EF her migration'ı ayrı commit eder) mevcut ParkingClosure testindeki kalıpla doğrulanıp yeniden uygulanıyor. Aynı kırılganlık kendi QaCenterMigrationTests'imde de vardı (paralel tedarikçi oturumunun yeni migration'ı ile ortaya çıktı), aynı şekilde düzeltildi.

Not: Ana klasörde paralel bir oturumun commit edilmemiş tedarikçi kodu (SupplierFoundation/SupplierAreaServices migration'ları dahil) bu koşumlara derlendi; sonuçlar o kodu da içerir.

## Yeni kapsam kapısı (otomatik boşluk tespiti)
- `scripts/qa_coverage_gate.py` + `coverage-baseline.json`: mevcut 207 testsiz endpoint taban; **yeni** testsiz endpoint veya web testi olmayan **yeni** sayfa CI'da RED. Kapatılan kalemler raporlanır, taban yalnız küçülür (`--update`). Fixture 3/3.
- `ci.sh`: test koşumu endpoint dökümünü `artifacts/ci/inventory`'ye yazar, envanter yeniden üretilir, kapı çalışır.
- Canlı deneme: paralel tedarikçi çalışmasındaki `GET/PUT /api/v2/tenants/{id}/suppliers/{id}` testsiz olduğu için **RED** verdi (kapı çalışıyor; o çalışma commit edilmeden test eklemeli veya tabanı bilinçli güncellemeli).

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| Talep/arıza, duyuru, anket, planlama, bakım, temizlik | PASS (temizlik ayrı modül değil: `MaintenanceType.CLEANING`) |
| Araç/otopark, web sitesi, ilan, iletişim, raporlar | PASS |
| SMS/e-posta/bildirim, export PDF/Excel, audit, sistem ayarları | KISMİ: SMS/e-posta/mesajlaşma/audit/sistem durumu PASS; CSV/PDF dışa aktarım bazı ekranlarda var (gecikme tazminatı, ledger, ilan, anket, portföy) ama dosya içeriği doğrulama testi yok → FAZ 11 |
| Envanterdeki tüm menüler | PASS (FAZ 04 tarayıcı koşumu + bu paketler) |
| Yeni ekranlar için otomatik kapsam boşluğu | PASS (kapı + CI) |

KVKK log taraması 0 bulgu; yeni paket yok.

[TOKEN] BİLGİ MEVCUT DEĞİL.
