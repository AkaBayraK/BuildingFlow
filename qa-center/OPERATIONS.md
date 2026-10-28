# Test ve Kalite Merkezi — kurulum, işletim, kurtarma, temizlik, güvenlik ve lisans

Bu belge yalnız QA programının eklediği parçaları anlatır. Uygulamanın genel yayın, yedekleme ve gözlemlenebilirlik
kuralları `docs/RELEASE.md`, `docs/BACKUP_RESTORE.md` ve `docs/OBSERVABILITY.md` içindedir ve aynen geçerlidir.

## 1. Kurulum
| Parça | Nasıl açılır | Varsayılan |
|---|---|---|
| Test Merkezi API ve ekranı | `QaCenter__Enabled=true` (API ortamı) | Kapalı; **Production'da açık ayar API'yi başlatmaz** |
| Runner sıklığı | `QaCenter__PollMilliseconds` (100–60000) | 2000 |
| AI Test Asistanı | `QaCenter__AkaAi__BaseUrl`, `__TokenUrl`, `__ClientId`, `__ClientSecret`, isteğe bağlı `__Model`, `__DailyRequestLimit`, `__TimeoutSeconds` | Eksik ayar = kapalı. Secret yalnız `.env`/secret store'da durur; Git'e ve loglara yazılmaz |
| Veritabanı | Migration'lar `QaCenterRuns`, `QaCenterData`; API açılışta uygular (`Database__AutoMigrate`) | Dolu tabloda Down reddedilir |

AkaAI tarafında BuildingFlow için `INTEGRATION_CLIENT` rollü bir entegrasyon istemcisi (client-credentials) tanımlanır;
AkaAI kodu değiştirilmez.

## 2. Günlük işletim
| İş | Komut |
|---|---|
| İzole backend testleri (guard zorunlu) | `bash scripts/test.sh [--filter …] --logger trx --results-directory <dir>` |
| Tarayıcı koşumu (geçici SUPER_ADMIN, sonunda silinir) | `bash scripts/qa-web.sh` — alt küme: `BF_QA_ROUTES="/dues,/sites" bash scripts/qa-web.sh` |
| Geçici Keycloak realm kimlik testi | `bash scripts/qa-identity.sh` |
| Envanter ve kapsam kapısı | `python3 scripts/qa_inventory.py <dir>` → `python3 scripts/qa_coverage_gate.py <dir>` (`--update` yalnız boşluk kapatınca) |
| KVKK log taraması | `python3 scripts/qa_log_scan.py <log…>` |
| Birleşik kanıt raporu | `python3 scripts/qa_report.py <run dir…> --out artifacts/qa/reports/<ad> [--previous <report.json>]` |
| Kalite kapısı / bug / faz kapatma | `python3 scripts/qa_github.py gate|bugs|close-phase … [--apply]` (varsayılan dry-run) |
| CI | `.github/workflows/qa-gate.yml` yalnız elle tetiklenir; planlı/gece koşumu kapalıdır |

Kanıtlar `artifacts/qa/<run_id>/` altında, Git dışıdır. `run_id` biçimi `qa-YYYYMMDDTHHMMSSZ-xxxxxx`.

## 3. Felaket kurtarma
- QA tabloları (`qa_runs`, `qa_run_steps`, `qa_data_batches`, `qa_data_records`) normal veritabanı yedeğinin parçasıdır; `docs/BACKUP_RESTORE.md` prosedürü (quiesced yedek, yalnız yeni izole hedefe restore) aynen uygulanır.
- API yeniden başlarsa yarım kalan koşum `FAILED / INTERRUPTED` olur; kuyruk kendiliğinden devam eder. Tek API örneği varsayılır.
- Kanıt klasörleri yerel diskte; saklama gerekiyorsa koşum sonrası arşivlenir (kişisel veri içermez, KVKK taraması ile doğrulanır).
- Rollback: QA migration'larının Down'u tablolarda veri varken reddeder; önce kayıtlar arşivlenip silinmeli, uygulanmış migration değiştirilmez.

## 4. Test verisi temizliği
- Yalnız **TEST** partileri temizlenir; **DEMO** partileri korunur (API 409 + DB check).
- Akış: *Verileri Önizle* (sıra, sayılar, engeller) → onay metni `TEMIZLE <kod>` birebir → tek transaction'da soft-delete (bağımlılık sırası: ilişki → kişi → daire → blok → yönetim alanı). Parti ve kayıt defteri audit için kalır.
- Finans geçmişi, partiye ait olmayan sonradan eklenmiş kayıt veya eski version temizliği durdurur; hiçbir satır değişmez. Finanslı test verisi için `scripts/test.sh`'in geçici veritabanı kullanılır (koşum sonunda DB ve rol silinir).

## 5. Güvenlik
- Test Merkezi uçları yalnız `SUPER_ADMIN` (`CanManageQaCenter`); senaryolar kod sahiplidir, kullanıcı girdisinden komut/SQL/script çalıştırılmaz.
- `scripts/qa_guard.py` her test koşumundan önce Production ortamını, loopback dışı veya ana/test dışı veritabanını, loopback dışı S3/Mailpit/Keycloak/RabbitMQ/SMTP/sandbox URL'ini, gerçek SMS OTP'yi ve `PersonRefactor__*` bayrak sızıntısını reddeder.
- AkaAI yalnız öneri üretir: kapalı şema + allowlist; onaysız koşum yok; açıklama kayıtlı sonucu değiştirmez; kullanıcı kimliği SHA-256 ile maskelenir; istek/yanıt loglanmaz.
- Tarayıcı ve kimlik koşumları yalnız geçici hesap/realm kullanır ve silindiğini doğrular; mevcut kullanıcı ve parolalar değişmez.

## 6. Lisans
QA programı **yeni üçüncü taraf paket eklemedi** (kullanıcı kararı 16.10.2026). Tarayıcı otomasyonu sistem Chrome + DevTools Protocol, raporlama Python standart kütüphanesi, PDF Chrome `--print-to-pdf`, mobil simülatör `xcrun simctl`. `python3 scripts/check-licenses.py` 329 paket PASS. Video (ffmpeg), .xlsx, Maestro/Playwright yeni lisans kararı gerektirir.
