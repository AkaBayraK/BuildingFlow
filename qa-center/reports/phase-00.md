# [QA FAZ 00 | 01/14] Test ve Kalite Merkezi — Salt okunur analiz

```text
[BUILDINGFLOW]
[FAZ QA 00 | KAYIT 01/14] Salt okunur analiz ve envanter
[DURUM] TAMAMLANDI (kullanıcı onayı bekleniyor)
[İLERLEME] FAZ 00 6/7 madde = %85,71 (7. madde "onay bekle"); QA akışı 0/14 kabul = %0,00
```

Kapsam: yalnız okuma. Hiçbir kod, veritabanı, CI, GitHub issue veya dosya değiştirilmedi. Paket `BuildingFlow/BuildingFlow_AkaAI_Test_Paketi/` altında (git'te izlenmiyor). Uygulama testi bu tur **çalıştırılmadı**; aşağıdaki sayılar dosya sayımıdır, PASS kanıtı değildir.

## 1. Depolar ve sürümler

| | BuildingFlow | AkaAI |
|---|---|---|
| Konum | `~/Desktop/Projects/BuildingFlow`, dal `main`, temiz (yalnız paket klasörü izlenmiyor) | `~/Desktop/Projects/AkaAI/aka-ai`, dal `feature/akaai-integration`, izlenmeyen `graphify-out/` ve bir doküman |
| Backend | .NET 10 (SDK 10.0.100), modular monolith: Api/Application/Domain/Infrastructure/Worker | **Java 21 + Spring Boot 4.1.1** (Maven), `akaai-app` + `akaai-mcp-sample` |
| Web | Next.js 16.4.3, React 19.3 | Next.js + TypeScript |
| Mobil | Expo 57 / React Native 0.86.3, expo-sqlite | yok |
| Diğer | PostgreSQL, Keycloak, MinIO/S3, RabbitMQ, OTLP/Grafana | PostgreSQL+pgvector, Valkey, SeaweedFS, Ollama, Keycloak |
| CI | `.github/workflows/quality.yml` (push/PR/manuel): init-local, `scripts/ci.sh`, backup-restore, mobil check/test/build | ayrı |

**Düzeltme:** MASTER_PROMPT AkaAI'yı ".NET/Next.js" diye tanımlıyor; gerçek depo Java/Spring Boot. Entegrasyon REST sözleşmesiyle yapılmalı, kod paylaşımıyla değil.

## 2. Web ve API envanteri

- **55 web sayfası** (`src/app/(workspace)/...`): dashboard, properties (+buildings/units/people/dues/payments/expenses/meters/reports/requests/sms/surveys/website/parking/common-areas/maintenance/documents/announcements), people, sites, users, consents, account, workspaces, system-settings, login.
- `Sistem Ayarları` mevcut: `/system-settings` = SystemStatusWorkspace + ProvidersPanel; menüde yalnız `permissions.administration` ile görünür (`navigation.ts:92`). Test Merkezi buraya alt ekran olarak eklenebilir.
- **48 endpoint dosyası, 388 route**: 194 GET, 111 POST, 62 PUT, 21 DELETE.

## 3. Mobil

- `src/` modülleri: auth, finance, owner, manager, requests, notices, consents, parking, common-areas, profile, offline (SQLite cache+queue), sync (engine+outbox).
- Test: 29 `node:test` dosyası (saf mantık). **UI otomasyon aracı yok** (Maestro/Detox yok).
- Bu Mac'te iOS 26.5 simülatör runtime var, açık cihaz yok; **Android SDK bulunamadı** → Android testi şu an BLOKE.
- Mobil finans kodunda online ödeme intent akışı bulunamadı (yalnız contracts/cache). Mobil online ödeme senaryoları için önce ürün özelliği gerekir.

## 4. Finans / ödeme

Mevcut (kod okundu):
- Aidat tahakkuku (DuesAssessment/DuesCharge, UUID replay, mükerrer engel), DuesChargeParty borçlu snapshot'ı, manuel Payment/PaymentAllocation, gecikme tazminatı (BF-LATE).
- Online ödeme: PaymentIntent durumları `PENDING/SUCCEEDED/REVIEW/FAILED/EXPIRED/CANCELLED/REFUNDED/CHARGED_BACK`, iade akışı, makbuz numarası, mutabakat kalemleri (DEBT_CHANGED, CHARGEBACK_UNREVERSED, PROVIDER_MISMATCH, REFUND_UNCONFIRMED), `PaymentLedgerEntry`.
- **SandboxPaymentProvider** var, varsayılan kapalı; HMAC-SHA256 imzalı webhook, `POST /api/v1/payment-webhooks/{provider}` (anonim, 4 KB sınır). İzole test betiği `scripts/test-payments.py` (geçici PostgreSQL konteyneri).

Eksik (paketin zorunlu zincirine göre):
- **3D Secure simülasyonu yok.**
- **Çift taraflı muhasebe yok**: ledger tek taraflı hareket kaydı (COLLECTION, REVERSAL, REFUND, CHARGEBACK...). Paketteki "çift taraflı muhasebe" maddesi mevcut domain ile karşılanamaz; test ancak mevcut ledger/mutabakat tutarlılığını doğrulayabilir.
- Mobil online ödeme (bkz. 3).

## 5. Mevcut testler ve izolasyon

- Backend: 175 test dosyası, 277 `[Fact]/[Theory]`. Entegrasyon testleri `BUILDINGFLOW_TEST_DATABASE` ile ayrı PostgreSQL; test Keycloak admin, Mailpit, RabbitMQ değişkenleri; MinIO `buildingflow-test-*` bucket kuralı.
- Web: 73 `node:test` dosyası (bileşen/mantık). **Tarayıcı E2E yok; Playwright kurulu değil** (lock'ta yalnız Next.js'in opsiyonel peer kaydı).
- Hazır betikler: `test.sh`, `smoke.sh`, `auth-smoke.mjs`, `test-payments.py`, `test-auth.sh`, `check-licenses.py`, `release-preflight.py`.
- Eksik: `test_run_id` etiketli test verisi, dry-run temizleme, koşum/adım/kanıt modeli, ekran görüntüsü/video toplama.

## 6. Güvenlik / KVKK / lisans

- Keycloak OIDC + PKCE, backend rol/üyelik kontrolü, ACCOUNTANT kişi verisi yasağı, telemetri etiket kısıtları, prod başlangıç kontrolleri (AGENTS.md §11) mevcut kurallar.
- **Kritik çakışma:** proje kuralı "yeni üçüncü taraf paket eklenmez (16.10.2026)". Playwright (Apache-2.0), Maestro (Apache-2.0) veya Testcontainers yeni bağımlılık olur. Lisanssız alternatif: sistem Chrome'u + Node'un yerleşik WebSocket'i ile Chrome DevTools Protocol (kendi kodumuz), iOS için `xcrun simctl` ekran görüntüsü/video; Android için SDK kurulumu gerekir. Bu karar sizin.
- AkaAI'ya giden içerik: maskelenmiş ekran/API envanteri ve test kataloğu; DB/shell erişimi verilmez.

## 7. AkaAI kapasitesi

- OpenAI uyumlu entegrasyon API'si: `POST /api/v1/integrations/chat/completions`, `GET /api/v1/integrations/models` (limitli).
- Agent, tool-approval (insan onayı), audit, usage raporu, knowledge base, MCP örneği; yerel Ollama, cloud LLM varsayılan kapalı.
- Yok: JSON-schema doğrulanmış test planı üretimi, BuildingFlow'a özel allowlist araç seti, test ajanı. Bunlar FAZ 09'da BuildingFlow tarafında şema doğrulayıcı + AkaAI chat API ile kurulabilir.

## 8. GitHub

- 39 açık issue; Test/Kalite Merkezi için eşdeğer issue **yok** (mükerrer açılmayacak). İlgili açık kalite issue'ları: #80 Otopark FAZ08, #58 Ortak Alanlar FAZ08, #65/#68 BF-SMS, #29 Website FAZ09.
- Önerilen issue başlığı: `[QA-CENTER][FAZ NN] ...`, gövde: kapsam, alt adımlar, bağımlılık, kabul kriterleri, riskler, kanıt, AGENTS.md kapanış kontrol listesi.

## 9. Faz bağımlılıkları, efor ve riskler

| Faz | Bağımlılık | Kaba efor | Ana risk |
|---|---|---|---|
| 01 İzole altyapı | — | M | prod guard, sandbox secret yönetimi |
| 02 Ekran/API keşfi | 01 | M | 388 route + 55 sayfa eşlemesi |
| 03 Test Merkezi UI | 02 | L | yeni entity/migration (koşum, adım, kanıt) |
| 04 Web/API/DB otomasyon | 01–03 | L | **tarayıcı aracı kararı** |
| 05 Kişi/alan/rol | 04 | M | Keycloak test realm |
| 06 Finans | 04–05 | XL | 3DS ve çift taraflı muhasebe yok; mobil ödeme yok |
| 07 Mobil | 01 | L | Android SDK yok, UI otomasyon aracı yok |
| 08 Diğer modüller | 04 | L | kapsam büyüklüğü |
| 09 AkaAI | 02–03 | L | ayrı repo/dal, prompt injection |
| 10 Demo/test verisi | 01 | M | demo/test ayrımı, temizlik güvenliği |
| 11 Kanıt/rapor | 03–04 | M | PDF/Excel için paket gereksinimi |
| 12 GitHub/kalite kapısı | 11 | M | otomatik issue kapama yetkisi |
| 13 Regresyon | hepsi | L | flaky testler |

Not: Paket AGENTS.md 8 ajan rolü tanımlıyor; proje kuralı tek Agent. Rolleri tek ajan sırayla üstlenir, paralel ajan açılmaz.

## 10. Onayınız gereken kararlar

1. **Test aracı:** (a) yeni bağımlılık yok, CDP + `xcrun simctl` ile kendi ince motorumuz (önerim), veya (b) Playwright/Maestro lisans onayıyla.
2. **Paket yeri:** START_HERE'e göre `qa-center/` altına taşınması ve `reports/phase-XX.md` kayıtları (main'e, yalnız bu dosyalar).
3. **Finans eksikleri:** 3DS simülasyonu, çift taraflı muhasebe ve mobil online ödeme bu programda ürün geliştirmesi mi olsun, yoksa "kapsam dışı / gap" olarak raporlanıp mevcut akış mı test edilsin?
4. **AkaAI tarafı:** FAZ 09 değişiklikleri hangi dalda (`feature/akaai-integration` mi `main` mi)?
5. **Android:** SDK kurulumu sizde mi, yoksa Android FAZ 07'de gerekçeli kapsam dışı mı?

[TOKEN] Giriş/çıkış/toplam token, kalan context: BİLGİ MEVCUT DEĞİL.
[SONRAKİ ADIM] Onayınızla FAZ 01: GitHub issue açılır, izole test altyapısı ve prod guard.
