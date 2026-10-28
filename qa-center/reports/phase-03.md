# [QA FAZ 03 | KAYIT 04/14] Test Merkezi UI ve orkestrasyon — GitHub #84

```text
[BUILDINGFLOW]
[FAZ QA 03 | KAYIT 04/14] Test Merkezi UI ve orkestrasyon
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#84 açık)
[İLERLEME] FAZ 03 PHASES maddeleri 4/5 = %80,00 (responsive/erişilebilirlik canlı kanıtı BLOKE)
```

## Ne yapıldı
- **Backend:** `QaRun`/`QaRunStep` (BaseEntity), durum makinesi, `QaCenterRuns` migration (audit kolonları, durum check'leri, aktif senaryo partial unique, adım sırası unique, dolu tabloda Down guard). `QaCenterEndpoints.cs`: 7 endpoint, `CanManageQaCenter` (yalnız SUPER_ADMIN), `QaCenter:Enabled` varsayılan kapalı, Production'da açılamaz. Tek, sıralı in-process runner; zaman aşımı, işbirlikçi iptal, yeniden başlatmada INTERRUPTED. Senaryolar kod sahipli ve salt okunur: `system.health`, `api.authorization`, `db.migrations`. 4 yeni hata kodu (BE ErrorCatalog + FE registry + TR/EN). Ayrıntı `docs/api/qa-center.md`.
- **Web:** `/system-settings/qa-center` ve Sistem Ayarları'nda yalnız SUPER_ADMIN'e görünen giriş kartı. İkonlu sekmeler: Genel Bakış, Ekran Envanteri, Test Senaryoları, Test Çalıştır, Canlı İzleme; sonraki fazların sekmeleri (AI Asistan 09, Test Verileri 10, Sonuçlar 11, Hata Yönetimi 12, Temizle 10, Ayarlar 12) "Planlı" ve devre dışı. Başlat/Durdur/Yeniden Dene/Raporu Gör gerçek API'ye bağlı; ikonlu durum filtresi, sunucu sayfalaması; canlı izleme 2 sn polling ve "canlı akış yok" açıklaması; adım başına beklenen/gerçekleşen. Kapalı ortam, yükleniyor, hata, yetkisiz, boş durumları. OIDC dönüş yolu whitelist'e eklendi.

## PHASES kontrol listesi
| Madde | Durum | Kanıt |
|---|---|---|
| RBAC korumalı menü | PASS | API 401 anonim, 403 SITE_ADMIN/MANAGER/ACCOUNTANT (okuma+yazma), 200 SUPER_ADMIN; FE rol kapısı |
| Genel bakış, envanter, senaryo, başlat/durdur/yeniden dene | PASS | Entegrasyon testleri + FE parser/aksiyon testleri |
| Kuyruk, ilerleme, timeout/cancel, geçmiş, durum makinesi | PASS | Unit 6 test; entegrasyon: replay, 409 aktif, iptal, timeout→retry, başarısız adım |
| Ekran görüntüsü, adım listesi, log/trace paneli | KISMİ | Adım listesi + beklenen/gerçek var; ekran görüntüsü/trace FAZ 04/11 (UI'da belirtildi) |
| Responsive ve erişilebilirlik testleri | **BLOKE** | Kod: role=tablist/tab, aria-selected/pressed, aria-live, sr-only durum, ikonlar aria-hidden, mobilde tek kolon. Canlı tarayıcı kontrolü için yerel DB'ye migration ve API'yi `QaCenter__Enabled=true` ile yeniden başlatma onayı gerekiyor |

## Testler (bu tur çalıştırıldı)
- `bash scripts/test.sh` filtreli (guard GEÇTİ): **Integration 31/31** (QaCenterApiTests 7, QaCenterMigrationTests 1, QaEndpointInventory, ErrorContract, Persistence, Migration, AutoMigration), **Unit 156/156**, atlanan 0. Log `/tmp/bf-qa03-backend.log`.
- `dotnet build` 0 hata/0 uyarı; `ef migrations has-pending-model-changes`: değişiklik yok.
- Web: `npm test` **300/300**, `npm run typecheck` temiz, `npm run build` başarılı (`/system-settings/qa-center` route'u üretildi).
- `check-licenses.py` 329 PASS; yeni paket yok. Tam backend regresyonu bu tur çalıştırılmadı.

## Sınırlamalar
- Migration yerel geliştirme DB'sine uygulanmadı; API yeniden başlatılmadı.
- Senaryolar yalnız okuma yapar; veri yazan CRUD/E2E senaryoları FAZ 04'te izole DB ile.
- Tek API örneği varsayımı; çoklu örnekte kuyruk kilidi gerekir.

[TOKEN] BİLGİ MEVCUT DEĞİL.
