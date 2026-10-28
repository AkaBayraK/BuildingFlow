# Test ve Kalite Merkezi API (QA FAZ 03)

`QaCenter:Enabled` varsayılan `false`; kapalıyken route eşlenmez (404). Production ortamında açık ayar API başlatmayı durdurur. Tüm route'lar `CanManageQaCenter` (yalnız `SUPER_ADMIN`).

| Method | Route | Açıklama |
|---|---|---|
| GET | `/api/v1/qa-center/overview` | Ortam, senaryo/endpoint sayısı, durum dağılımı, son koşum |
| GET | `/api/v1/qa-center/scenarios` | Kod sahipli senaryolar (`READ_ONLY`) |
| GET | `/api/v1/qa-center/runs?page&pageSize(1-100)&status` | Sayfalı koşum geçmişi, en yeni önce |
| GET | `/api/v1/qa-center/runs/{id}` | Koşum + adımlar (beklenen/gerçek/süre) |
| POST | `/api/v1/qa-center/runs` `{scenarioKey, requestId}` | 201 yeni, 200 aynı UUID replay; bilinmeyen senaryo 404 |
| POST | `/api/v1/qa-center/runs/{id}/retry` `{requestId, version}` | Yalnız FAILED/CANCELLED/TIMED_OUT, en fazla 5 deneme |
| PUT | `/api/v1/qa-center/runs/{id}/cancel` `{version}` | QUEUED hemen CANCELLED; RUNNING adım sonunda durur |

Durumlar: `QUEUED → RUNNING → PASSED | FAILED | CANCELLED | TIMED_OUT`. Adımı olmayan veya başarısız adımı olan koşum PASSED olamaz. API yeniden başlarsa RUNNING kalan koşum `FAILED/INTERRUPTED` olur. Tek API örneği varsayılır.

Hata kodları (409): `BF_QA_RUN_ACTIVE` (aynı senaryoda aktif koşum), `BF_QA_RUN_STATE` (durum/version uyumsuz), `BF_QA_REQUEST_REUSED` (UUID başka senaryoda), `BF_QA_RETRY_LIMIT`.

Senaryolar: `system.health` (SystemStatus bileşenleri UP), `api.authorization` (public prefix dışındaki her endpoint'te açık yetki kararı), `db.migrations` (bağlantı, bekleyen migration 0). Senaryolar kullanıcı girdisinden komut/SQL/script çalıştırmaz.

Tablolar `qa_runs`, `qa_run_steps` (BaseEntity, migration `QaCenterRuns`, dolu tabloda Down reddedilir).

## AI Test Asistanı (QA FAZ 09)

AkaAI'ya yalnız mevcut entegrasyon API'si (`POST /api/v1/integrations/chat/completions`, client-credentials JWT, `X-Actor-Ref` = kullanıcı `sub` SHA-256 ilk 32 hex, `X-Request-Id`) üzerinden gidilir; AkaAI deposu değişmedi. Ayarlar `QaCenter:AkaAi:{BaseUrl,TokenUrl,ClientId,ClientSecret,Model,DailyRequestLimit=20,TimeoutSeconds=60}`; secret yalnız ortam/secret store'dan, loglanmaz. Eksik ayar = kapalı.

| Method | Route | Açıklama |
|---|---|---|
| GET | `/api/v1/qa-center/ai/status` | `enabled`, günlük limit, bugünkü kullanım |
| POST | `/api/v1/qa-center/ai/plans` `{prompt}` | 1-1000 karakter, kontrol karakteri yok. Yanıt yalnız `{scenarios,summary,risks}` kapalı şeması, en fazla 5 benzersiz allowlist senaryosu; `requiresApproval=true`, hiçbir koşum başlatmaz |
| POST | `/api/v1/qa-center/ai/runs/{id}/explanation` | Bitmiş koşumun kayıtlı adımlarını açıklar; `runStatus` DB'den gelir, AI değiştiremez |

Hatalar: `BF_QA_AI_PROMPT_INVALID` 400, `BF_QA_AI_BUDGET` 429 (süreç içi günlük sayaç; çoklu örnekte paylaşılmaz), `BF_QA_AI_PLAN_INVALID` 502 (bilinmeyen senaryo, fazla alan, mükerrer, boş, JSON dışı), `BF_QA_AI_UNAVAILABLE` 503 (kapalı, token/bağlantı/zaman aşımı hatası; UI katalogdan elle seçime döner). Kullanıcı metni `<kullanici_istegi>` içinde veri olarak gönderilir; asıl koruma çıktı doğrulamasıdır.

## Demo/test verisi ve temizlik (QA FAZ 10)

| Method | Route | Açıklama |
|---|---|---|
| GET | `/api/v1/qa-center/data/batches` | Son 50 parti (kod, tür, ortam, `runCode`, durum, kayıt sayısı, yönetim alanı) |
| POST | `/api/v1/qa-center/data/batches` `{kind: DEMO\|TEST, requestId, runCode?, blocks 1-5, unitsPerBlock 1-20}` | SITE + bloklar + daireler + malik/kiracı kişileri (`@example.test`, telefon/kimlik yok) + ilişkiler; her satır `qa_data_records`'a yazılır. 201 yeni, 200 replay, farklı türle aynı UUID 409 |
| GET | `.../{id}/cleanup-preview` | Dry-run: sıra (UnitOccupancy → Person → Unit → Building → Property), sayılar, engeller (`DEMO_PROTECTED`, `FINANCE_HISTORY`, `UNREGISTERED_ROWS`, `ALREADY_CLEANED`), onay metni `TEMIZLE <kod>`, version |
| POST | `.../{id}/cleanup` `{version, confirmation}` | Yalnız TEST + engelsiz + birebir onay; kayıtlı satırları tek transaction'da soft-delete eder; parti ve kayıt defteri (audit) korunur |

Hatalar (409): `BF_QA_DATA_PROTECTED`, `BF_QA_DATA_CONFIRM`, `BF_QA_DATA_BLOCKED`, `BF_QA_DATA_STATE`. Finans kaydı silinmez kuralı nedeniyle finanslı test verisi bu uçla temizlenmez; izole test veritabanı (`scripts/test.sh`, geçici DB/rol silinir) kullanılır. Tablolar `qa_data_batches`, `qa_data_records` (migration `QaCenterData`, `kind='TEST' OR status='ACTIVE'` check'i demo temizliğini DB düzeyinde de engeller, dolu tabloda Down reddedilir).
