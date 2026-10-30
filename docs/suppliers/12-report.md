# TEDARİKÇİ FAZ12 — Uçtan uca kalite ve yayın hazırlığı

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #102](https://github.com/AkaBayraK/BuildingFlow/issues/102). Plan FAZ12, kayıt13/13. Otomatik kalite kapıları tamam; **gerçek tarayıcı E2E kullanıcı onayı bekliyor** (yerel veritabanına 9 tedarikçi migration'ı uygulanması ve API yeniden başlatması gerekir). Bu nedenle #102 açık bırakıldı.

## Otomatik kalite kapıları (bu turda çalıştırıldı)

| Kapı | Kanıt | Sonuç |
| --- | --- | --- |
| RBAC matrisi | `SupplierReleaseTests.EverySupplierRouteRejectsAnonymousAndNonManagementRoles`: çalışma anında keşfedilen 67 tedarikçi ucu × anonim/RESIDENT/OWNER | **401/403, sızıntı 0** |
| Tenant izolasyonu | `AManagerOfAnotherTenantNeverReadsSupplierData`: 31 GET ucu + öneri ucu yabancı tenant ile | **2xx/5xx 0** |
| Uçtan uca sandbox | `ContractWorkOrderMaintenanceInvoiceAndReportsFlowEndToEnd`: sözleşme ACTIVE → iş emri CLOSED (yönetici/SITE_ADMIN ayrımı) → sözleşmeli bakım tamam → fatura kayıt (muhasebe) → onay (farklı yönetici) → ödendi → mutabık → Gider modülünde kayıt → performans/uyarı/öneri | **GEÇTİ** |
| Performans | `SupplierReadsStayWithinP95UnderConcurrentLoad`: 41 tedarikçi/200 iş emri, 100 istek, eşzamanlılık 10 (liste, mülk iş emirleri, uyarılar, performans, faturalar) | **p50 18 ms, p95 210 ms, hata 0** (sınır 2 s) |
| Migration/rollback | `SupplierMigrationChainTests`: 9 tedarikçi migration'ı, 24 tablo boş şemada tamamen geri alındı ve yeniden kuruldu; her migration'ın kendi dolu-Down koruma testi ayrıca var | **GEÇTİ** |
| İdempotent SQL | `check-idempotent-sql.sh`: 64 migration'lık idempotent betik iki kez | **GEÇTİ** |
| Model/snapshot | `dotnet ef migrations has-pending-model-changes` | **Değişiklik yok** |
| Yedekleme/geri yükleme | `test-backup-restore.py` (izole fixture; canlı veri okunmaz) | **8/8 GEÇTİ** |
| Erişilebilirlik (statik) | `supplier-accessibility.test.mjs`: her alanın Label'ı, ikonlarda aria-hidden, yalnız ikonlu düğmede ad, sekme/radyo durumları ve grup adları | **GEÇTİ** |
| Güvenlik | Tüm uçlar yetki kararı içerir (`QaEndpointInventoryTests`), SQL yalnız parametreli, loglarda kişisel veri yok, formül enjeksiyonu korumalı CSV, öneri çıktısı kapalı şema; `check-private-config.py` | **GEÇTİ** |
| Lisans | `check-licenses.py`; modül boyunca yeni paket eklenmedi | **329 PASS** |
| Kapsam kapısı | `qa_coverage_gate.py` (API 465) | **Yeni testsiz kalem 0** |
| Tam regresyon | `scripts/test.sh` | **Integration945/947 (2 opt-in) + Unit472/472, exit0** |
| Web | test + typecheck + production build | **347/347, GEÇTİ, GEÇTİ** |
| Mobil | `npm run check` | **170/170, GEÇTİ** |

**Test altyapısı düzeltmesi:** Paylaşılan PostgreSQL `max_locks_per_transaction=64`; 18 çekirdekte eşzamanlı şema kurma/silme kilit tablosunu doldurup FAZ07–11 tam koşularında aralıklı `53200 out of shared memory` üretiyordu. Paylaşılan sunucu ayarına dokunulmadı; `SiteApiFactory` şema migrate/drop işlemleri en fazla 4 eşzamanlı olacak şekilde sınırlandı. Sonraki tam koşum temiz (exit0), süre 14 dk 37 sn (önceki ~14 dk).

## Ekran ve işlem bazlı test kaydı

| Ekran / görünüm | İşlemler | Durum geçişleri | Negatif / RBAC / izolasyon | Kanıt |
| --- | --- | --- | --- | --- |
| Tedarikçiler listesi (web) | liste, arama, sıralama, sayfalama, oluştur, düzenle, pasif/engelli | DRAFT/ACTIVE/INACTIVE/BLOCKED gerekçeli | mükerrer ad 409, version 409, ACCOUNTANT 403, yabancı tenant 404 | SupplierApiTests, suppliers.test.mjs |
| Alanlar ve hizmetler sekmesi | alan bağla/kaldır, kategori/hizmet | — | yetkisiz alan 404, geçmişli alan silme 409 | SupplierRelationsTests |
| Belgeler sekmesi | yükle, indir, revizyon, kaldır | — | tür/boyut 400, genel uç bypass 404, kanıt belgesi kaldırma 409 | SupplierDocumentsContractsTests |
| Sözleşmeler sekmesi | oluştur, düzenle, etkinleştir, iptal, yenile, geçmiş | DRAFT→ACTIVE→CANCELLED | çakışan yenileme 409, iptal gerekçesi 400 | SupplierDocumentsContractsTests |
| Teklifler sekmesi | talep, teklif gir, karşılaştır, onay/red, iş emrine dönüştür | OPEN→AWARDED/CANCELLED | kendi teklifini onay 403, döviz ayrımı | SupplierQuoteTests |
| İş emirleri sekmesi | kaynaktan oluştur, düzenle, geçiş, fotoğraf, not, geçmiş | DRAFT→ASSIGNED→ACCEPTED→IN_PROGRESS→COMPLETED→VERIFIED→CLOSED; REJECTED/CANCELLED | MANAGER doğrulama 403, eşzamanlı 200+409, SLA'sız atama 400 | SupplierWorkOrderTests |
| Bakım planları sekmesi | önizle, oluştur, takvim, tamamla, atla, sonlandır, tatil | ACTIVE→ENDED; PLANNED→COMPLETED/SKIPPED | çakışma 409, sözleşme dışı 409, açık iş emriyle tamamlama 409 | SupplierMaintenanceTests |
| Faturalar sekmesi | kaydet, onayla, ödendi, mutabakat, iptal, düzeltme, geçmiş | RECORDED→APPROVED→PAID→RECONCILED; CANCELLED | mükerrer 409, kaydedenin onayı 403, ödenmişi iptal 409, Gider'den iptal 409 | SupplierInvoiceTests |
| Uyarılar görünümü | liste, tür süzme, tercih kaydet, CSV, PDF | — | tercih version 409, yabancı tenant 404 | SupplierInsightTests, supplier-insights.test.mjs |
| Performans görünümü | dönem/alan süzme, CSV, PDF | — | 366 gün/ters aralık 400, ACCOUNTANT 403 | SupplierInsightTests |
| Öneriler görünümü | onaylı öneri al (3 tür) | — | onaysız 400, injection etkisiz, yabancı kapsam 404, limit 429 | SupplierAdviceTests |
| Mobil Tedarikçi işleri | liste, geçiş, not, fotoğraf, kuyruk, vazgeç, yeniden dene | iş emri geçişleri | çakışma, 401/403/5xx, yetki kaybında temizlik | supplier-work.test.mjs |

## Bekleyen ve bilinen sınırlar

- **Gerçek tarayıcı/cihaz E2E yapılmadı.** Yerel veritabanına migration uygulanması ve API yeniden başlatması için kullanıcı onayı bekleniyor; onaydan sonra QA tarayıcı motoru (`scripts/qa-web.sh`) ile ekranlar gezilir.
- Canlı OIDC ile çalışan 2 opt-in test bu ortamda atlandı.
- Uyarılar uygulama içidir; e-posta/SMS/push yok. Excel çıktısı CSV'dir; PDF tarayıcı yazdırmasıdır.
- Öneri motoru kural tabanlıdır; dış model bağlamak ayrı onay ister.
- Muhasebeci için ayrı fatura ekranı yok (API ve Gider modülü üzerinden).

Loglar `/tmp/bf-sup12-{a,b,full,backup,webbuild,mobile}.log`.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
