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
