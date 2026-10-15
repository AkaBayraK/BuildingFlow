## 15.10.2026 — BAKIM: BuildingFlowMobil mobil modül olarak depoya eklendi

Ayrı yerel klasörde (`buildingflow-mobile`, GitHub remote yok) duran Expo uygulaması `mobile/` altına taşındı: uygulama `mobile/buildingflow-mobile`, 15 fazlık plan `mobile/buildingflow-mobile-prompts`, faz raporları `mobile/docs`, tasarım `mobile/design`. Göreli bağlantılar korunsun diye klasör düzeni aynen alındı; yalnız `scripts/mobile-api.sh` backend kökünü artık depo kökü olarak bulur. Mobilin kendi `.github` iş akışı kök `quality.yml` içinde `mobile` işi olarak birleştirildi. Mobil git geçmişi (tek "Initial commit") ve `node_modules`/`ios`/`dist`/`.env` taşınmadı. Doğrulama (Linux, Node 22): `npm ci`, `npm run check` (typecheck + lint + **54/54** test), `project:test` **7/7**, `expo export` geçti. Uygulama kodu ve yol haritası değişmedi; yeni ürün fazı açılmadı.

## 11.10.2026 — BAKIM: Genel Bilgiler / Daireler frontend detay alanları


## 11.10.2026 — BAKIM: Daire/kişi bağlantıları ve tablet

Güncel kiracı/malik bilgisi için yetki korumalı bounded batch People read (Application/Store/PeopleEndpoints); daire GET listelerinde tam UnitId filtresi (Hierarchy/Store/direkt+binalı endpoints). Yeni UnitPeople/unit-links, HierarchyRows kolon/kart bağlantıları; PersonPage iletişim özeti, PersonOccupancies ve RecordDetails bölüm bağlantıları; ManagementPage+unit route query, auth return-path/AuthGate, strict management DTO, RecordList tablet breakpoint ve TR/EN. İŞ04 mevcut görev kanıtı güncellendi; yeni faz/payda yok. PeopleApiTests negatifleri +13 UnitPeopleNavigation PG senaryosu +8 FE davranış testi. **464.NET/99FE**, typecheck/build,331 lisans,28/28 koruma; tablet/telefon/masaüstü kabulü ve3 screenshot. API yerelde yeniden açıldı, Chrome tablet önizlemesi açık. Şema/live business mutation/commit/merge/push yok. Belgeler: people/hierarchy/frontend-rules/FE AGENTS/licenses/TESTING/progress. Kanıt [TESTING.md](../TESTING.md).

`features/property-details` ortak alan/seçenek/payload/doğrulama ve DetailInputs/DetailValues; EntityEditor, Property oluştur/detay, Building detay ve HierarchyRows/RecordDetails yeni nullable alanları okur/yazar/gösterir. RecordList TanStack columnVisibility; net alan/oda düzeni ve seçilebilir diğer özellikler, mobil kartlar, TR/EN, scope/DTO doğrulama. Altı yeni anlamlı test ve mevcut İŞ03 roadmap kanıtı; hierarchy/frontend-rules/AGENTS/lisans/ilerleme belgeleri ve dört ekran kanıtı.

**91/91 FE, typecheck/build, 331 lisans, TR/EN ve 390 px tarayıcı kabulü geçti; 149 korunan dosya aynı. Bu tur3/3 = %100.00, genel83/83 değişmedi.** BE/DB/migration ve bağımlılık manifesti değiştirilmedi; önceki451 .NET bu tur yeniden koşulmadı. Canlı başarılı kayıt mutation'ı yok; commit/merge/push yok. İŞ11 incelemesinden resmî MIT action lisans metinleri korunup bağlandı. Ayrıntı [TESTING.md](../TESTING.md).

## 11.10.2026 — BAKIM: açılışta otomatik migration ve apartman/daire kolonları

`DatabaseMigration` (advisory lock, entity-migration uyum kontrolü), API `Database:AutoMigrate`, `db.sh sync`, `dev.sh api` entegrasyonu, 6 yeni otomatik migration testi. `ApartmentAndUnitDetails` elle eklenmiş kolonlara karşı idempotent; 8 eski yükseltme testi `LegacySchema` ile düzeltildi. Ana DB yedeklendi ve migration API açılışında uygulandı. **451 .NET geçti.** Dal `feature/apartment-unit-details`; push/merge yok.

## 11.10.2026 — main birleştirme ve yerel servis güncellemesi

`faz-10-11-release` → `main` (`--no-ff`, push yok). main'de ci.sh 426 .NET / 85 FE geçti. Ana DB'de bekleyen migration yoktu, DB'ye yazılmadı. Worker yeni kodla yeniden başlatıldı; API kullanıcı tarafından yeniden başlatılmıştı. Smoke 15/15, gözlemlenebilirlik 11/11. Gözlemlenebilirlik sayısı 12/12 → 11/11 düzeltildi.

## 11.10.2026 — İŞ11 adım04 ve faz teslimi: kabul ve yayın rehberi

`docs/RELEASE.md` (preflight, HTTPS/proxy, kurulum/migration, geri dönüş, yedek, SMTP, smoke/kabul), `deploy/production.env.example`, `scripts/release-preflight.py` + 21 negatif varyantlı testi (CI'da). HSTS build-zamanından `proxy.ts` çalışma zamanına taşındı. `test-observability.py` boşta worker log penceresi genişletildi. Sürüm etiketi v0.11, roadmap 4/4, AGENTS/README bağlantıları. **ci.sh: 426 .NET, 85 FE, build, audit, 331 lisans; smoke 15/15, gözlemlenebilirlik 11/11, anonim SSR 30 rota sızıntı 0.** **İŞ11 4/4 = %100.00; genel 83/83 = %100.00.** Push/merge yok.

## 11.10.2026 — İŞ11 adım03: yedekleme ve geri dönüş

`scripts/backup_restore.py` (quiesced PG/Keycloak dump + MinIO cold snapshot, SHA256 manifest, yalnız yeni internal hedefe `restore-check`; init-zamanı readiness yarışı düzeltildi) ve `scripts/test-backup-restore.py` fixture provası (gerçek Keycloak/MinIO, negatifler, temizlik ve canlı compose durumu kontrolü); workflow adımı, [BACKUP_RESTORE.md](../BACKUP_RESTORE.md), roadmap adım03. **Prova 8/8 iki kez geçti.** Canlı veri yedeklenmedi/restore edilmedi; .env/migration/manifest değişmedi.

## 11.10.2026 — İŞ11 adım02: CI ve otomatik kontroller

`scripts/ci.sh` sonuç dizinini sıfırlar, CI kapı testlerini (`scripts/test_ci_gates.py`) ve idempotent SQL provasını (`scripts/check-idempotent-sql.sh`) çalıştırır; `check-test-results.py` TRX kapısı; `.github/workflows/quality.yml` SHA-sabit action'larla. Uygulanmış migration'daki sonlandırılmamış ham SQL nedeniyle bozuk idempotent betik `TerminatedSqlMigrationsSqlGenerator` ile düzeltildi + gerçek PG testi; MIGRATIONS/lisans kaydı. **426 .NET, 85 FE, build, audit, lisans geçti.** Eski migration/.env/manifest değişmedi.

## 11.10.2026 — İŞ11 adım01: güvenlik ve erişim

`faz-10-11-release` dalı açıldı; Faz10 ayrı commit. API `ReleaseSecurity` (production HTTPS/CORS/AllowedHosts doğrulaması, yalnız tanımlı proxy, HSTS/güvenli başlıklar/no-store, anonim/kimlikli okuma/yazma kodlu 429), 28 güvenlik testi; FE `proxy.ts` nonce CSP, `security/policy.ts`, güvenli başlıklar ve test edilebilir locale cookie seçenekleri; roadmap adım01 tiki ve TR/EN metin. **425 .NET, 85 FE, typecheck/build geçti.** DB/migration/.env/manifest değişmedi.

## 11.10.2026 — İŞ11 başladı

Kullanıcı onayı; güvenlik/CI/yedek-restore/yayın kabul kapsamı ve test planı hazırlandı. Faz10 değişiklikleri korunur; yeni göreve henüz tik yok.

## 11.10.2026 — İŞ10 adım04 ve faz teslimi

API/worker güvenli OTLP logları, Loki trace kimliği, Prometheus metrik/TCP health, Grafana iki datasource/8 panel ve `scripts/test-observability.py` canlı kontrolü tamamlandı. Sayaç tarih alanı gerçek takvimle doğrulandı; stale409 sonrası güncel okuma yükleme eklendi. TR/EN ve512px/390px sağ panel tarayıcı kabulü, ekran kanıtı, API/OBSERVABILITY/migration/lisans/AGENTS/README/skill-agent/progress belgeleri ve son roadmap4/4 güncellendi.

**397 .NET,82 FE,typecheck/build,11 canlı smoke,331 lisans,23 korunan dosya ve17 eski DB tablo hash eşliği başarılı.** Yeni idempotent SQL üretildi; uygulama Docker imajları derlenmedi, kullanıcı iş verisine mutation yok. İŞ10 **4/4=%100.00**, genel **79/83=%95.18**. Yeni İŞ11 başlatılmadı; dal faz-09-messaging, commit/merge/push yapılmadı.

## 11.10.2026 İŞ10 adım03

OpenTelemetry1.18 SDK; düşük cardinality request/histogram ve worker/retry/DLQ ölçümleri; Collector/Prometheus/blackbox/Loki compose, Grafana datasource/dashboard, license+NOTICE, dev workerDB düzeltmesi. Gerçek pipeline smoke,397.NET ve331 lisans; yerel upgrade17 eski tablo hash korundu.

## 11.10.2026 İŞ10 adım02

Rapor Application/Store/API, scope/RepeatableRead/SQL LAG, doluluk sayımı ve finans tarih kuralları; ReportsWorkspace ayrı menü, TR/EN/DTO ve testler. Kanıt8 entegrasyon+82FE,typecheck/build.

## 11.10.2026 İŞ10 adım01

Sayaç/okuma/revizyon BaseEntity modeli, EF migration + SQL immutable/Down guard, API UUID/version/scope ve ortak sağ panel/TanStack/TR-EN ekranları. TESTING.md:36 PostgreSQL ve80 FE; typecheck/build. Faz09 hızlı tekrar41/41. Paket değişmedi.

# BuildingFlow çalışma değişiklik geçmişi

Bu dosya yalnız yeni çalışma turlarında gerçekten yapılan değişiklikleri ekleyerek kaydeder. Önceki iş fazlarının ayrıntılı değişiklik/test kanıtı [TESTING.md](../TESTING.md) ve ilgili teslim raporlarında korunur; buraya yeniden kopyalanmaz. Güncel durum [STATUS.md](STATUS.md), devam bağlamı [SESSION.md](SESSION.md).

## 2026-10-09 — Çalışma ve ilerleme protokolü

Onaylı kapsam: yalnız yönerge ve takip belgeleri. Uygulama/DB/API geliştirme fazı açılmadı.

| Dosya | Gerçek değişiklik |
| --- | --- |
| [AGENTS.md](../../AGENTS.md) | Mevcut kuralları koruyan zorunlu başlangıç/ara/sonuç raporları, faz ayrımı, adım durumları ve yüzdesi, kaynaklara bağlı takip, gerçek token/kota ve onay sınırı eklendi |
| [UI planı AGENTS.md](../../frontend/buildingflow-ui-plan/AGENTS.md) | Mevcut ilerleme maddesi kök protokolün Türkçe rapor biçimine bağlandı; kapsam kuralları korundu |
| [STATUS.md](STATUS.md) | Aktif tur ve gerçek yol haritasının tarihli özeti; 13 iş fazı kaydı, 58/78 adım, ayrı 12 UI ve 8 teknik faz bilgisi |
| [CHANGELOG.md](CHANGELOG.md) | Bu gerçek belge değişiklik kaydı başlatıldı |
| [SESSION.md](SESSION.md) | Sonraki tur için onay/kapsam, korunacak kurallar, kaynak ve kullanım bağlamı hazırlandı |
| [TESTING.md](../TESTING.md) | Belge kontrolleri uygulamadan önce planlandı; doğrulanmış sonuçlar ve uygulama testlerinin yeniden çalıştırılmadığı kaydedildi |
| [SKILLS_AND_AGENTS.md](../SKILLS_AND_AGENTS.md) | Bu turun gerçek tek-agent/OpenAI Docs kullanımı ve belge doğrulama sonucu kaydedildi |

**TAMAMLANDI: 3/3, %100.** 84 yerel Markdown bağlantısı, eski kök kuralların içerik/sırası, UI kural bağlantısı, iş/UI/teknik sayaçları, zorunlu rapor alanları ve whitespace kontrolü geçti. Başlangıç hashleriyle **315 mevcut dosyanın korunması ve yalnız 7 izinli Markdown farkı** doğrulandı.

Bu tur uygulama kodu, yapılandırma, migration, veritabanı, .env, npm/NuGet bağımlılıkları veya çalışma süreçleri değiştirilmedi. Önceki kapsamlı çalışma ağacı korundu; commit/push yapılmadı. Uygulama testleri/restore/lisans taraması yeniden çalıştırılmadı; önceki test sonuçları bu tura mal edilmedi. Yeni geliştirme fazı başlatılmadan kullanıcı onayı beklenir.

## 2026-10-09 — İŞ 07.1 merkezi API feedback

Onaylı analiz/kodlama: merkezi ErrorCatalog, kod/mesaj/parametre/önem seviyesi ve TR/EN sağ üst bildirim. Beş resmi adım; mevcut iş 07/UI 07 ayrı.

- 1/5 adım doğrulandı; 86 beklenen mesaj merkezi 98 kod kataloğuna taşındı; analize ve TR/EN sözlüklere bağlandı. Backend/FE testleri henüz çalıştırılmadı.

- Adım 04 kaydı (3/5): Backend 9/9 kodlu HTTP/alan/redaksiyon testi; FE 56/56 kod/parametre/TR–EN/timer/dedup testi ve typecheck başarılı. Lisans 312 paket sürümünde PASS. Tam backend regresyonu ve tarayıcı henüz bekliyor.
- Adım 04 kaydı (3/5): Tam .NET regresyonu 252/252 (47 unit, 205 integration, atlanan 0), 56 FE testi ve production build başarılı; 9 yeni HTTP sözleşme testi dahil. Tarayıcı kabulü sürüyor.
- Adım 04 kaydı (3/5): 252 .NET testi ve FE kod/parametre/timer sözleşmeleri geçti; bildirim kapatma/panel odağı düzeltmesi yapıldı. Son typecheck başarılı; yeni yerel oturumla tarayıcı kabulü devam ediyor.
- Adım 05 kaydı (4/5): Tarayıcı TR/EN sağ üst error/warning, gerçek kodlu 404/dedup/retry, 390 px taşmasız görünüm, bildirim odağında duraklama ve kapatınca panelin açık kalması doğrulandı. Son regressyon/build ve belge teslimi sürüyor.
- Adım 05 kaydı (5/5): 252/252 .NET, 59/59 FE, typecheck/build, 312 lisans, TR/EN/mobil tarayıcı ve 19 korunan dosya kontrolü başarılı. İŞ 07.1 5/5 tamamlandı; yeni faz onayı bekleniyor.

### İŞ 07.1 teslim — 10.10.2026

- Backend: Application/ErrorCatalog.cs (98 kod), Properties/Hierarchy/People/Dues/Payments validator'ları ve Infrastructure Hierarchy/People/Dues/PaymentStore beklenen mesajları; Api/ApiProblems.cs, ExceptionHandler.cs, Program.cs tek kodlu ProblemDetails sözleşmesi. ErrorContractTests.cs 9 yeni senaryo. Domain/DB/migration değişmedi.
- Frontend: services/error-codes.ts, problem.ts, authorized-request.ts ve api.ts; feedback Context/Notifications/model/text, MessageBanner/FieldError, ortak ui/dialog.tsx ve globals.css. Code/parametre TR/EN apiErrors/apiFields, locale sonrası gruplu alan çevirisi ve test edilebilir abort/refresh/body transport. Yeni paket kurulmadı; mevcut Radix branch kaynakları kullanıldı.
- Takip: milestones.ts 07.1 beş doğrulanmış görev; 14 faz/63–83 adım. TESTING, analiz, api/errors/README, frontend-rules, kök/frontend AGENTS, SKILLS_AND_AGENTS ve progress kayıtları. Tarayıcı ekran kanıtları (form TR, warning TR/EN, 404 mobil EN/desktop TR ve tamamlanan yol haritası) dokümanlara bağlandı. SESSION içindeki eski sayaç da 63/83 olarak eşitlendi.
- **252 .NET / 59 FE; typecheck/build; 312 lisans; 19 korunan dosya; TR/EN/390 px/focus/dismiss/404/dedup kabulü başarılı.** Sandbox soket/SWC engeli izinli yeniden çalıştırmayla aşıldı. İlk errors JSON, UTF-8/locale template ve bildirim/panel kapanma hataları testle yakalanıp düzeltildi.
- Son teslim kontrolünde nihai yol haritası koduyla üretim derlemesi, 115 yerel Markdown bağlantısı, 19 korunan dosya ve 14 faz/63–83 adım sayacı doğrulandı. Güncel kullanım okuması SESSION.md'ye yazıldı; sonraki faz başlatılmadı.

Yeni faz başlamadı. Kullanıcı finans/ilişki verisinde başarılı mutation yok; canlı 400/409 başarılı mutation kabulü ve full screen-reader ürünü/OS hover testleri yapılmış sayılmaz. Özel token/context sayıları BİLGİ MEVCUT DEĞİL; hesap kota okuması SESSION içindedir.

## 2026-10-10 — İŞ 07 başlangıç

- Kullanıcı Faz 07 kodlama onayı; dört resmi görev ve finans sözleşmesi analiz edildi. Analiz/kabul planı, STATUS/SESSION oluşturuldu/güncellendi. Koruma hashleri alındı; test sonuçları henüz bekliyor.

## 10.10.2026 — İŞ 07, adım 01/04 tamamlandı

Expense/PropertyBudget/BudgetRevision, Application/EF/store/API ve yeni ExpensesAndBudgets migration/snapshot eklendi. Sekiz ortak NOT NULL alan, immutable revizyon, UUID/fingerprint, scope/version/kilit doğrulandı. 292 .NET test (59 unit + 233 integration), 67 FE test ve typecheck geçti. Tarihsel migration testi kendi hedefini açıkça seçiyor; uygulanmış migration dosyaları değişmedi. Finans endpoint sahte rol/JWT kontrolleri son regresyonda geçti. İlerleme 1/4 %25.00; tarayıcı kabulü sürüyor.

## 10.10.2026 — İŞ 07, adım 02–03/04 tamamlandı

Yeni /expenses ve property/expenses menü/alan seçimi, TR/EN gider listesi/filtre/sayfalama/sağ form/detay; kategori bütçe formu/geçmiş ve gerçek aylık tahsilat/gider/plan hesapları eklendi. BigInt locale para formatı, DTO scope/para/tarih/özet/revizyon doğrulaması, 409/replay ve yedi yeni merkezi hata kodu (toplam105). Ortak SummaryCard dar masaüstü kartı seçeneği ekledi. 1280×900 form sağ 512px, ilk alan odağı/tarih-tutar koruma; EN 390×844 panel/sağ üst bildirim ve sıfır plan kabulü doğrulandı. Gider/bütçe kaydedilmedi. Yerel yeni migration/API başlatma ve 12 eski tablo hash eşliği geçti. API/mimari/frontend/AGENTS/migration/README belgeleri aynı sözleşmeye bağlandı. 3/4 %75.00; final frontend derleme ve teslim sürüyor.

## 10.10.2026 — İŞ 07, adım 04/04 teslim

292 .NET (59 unit +233 integration) ve67 FE hata/atlama0; typecheck/build/EF model eşliği/idempotent SQL, 312 lisans, 18 korunan dosya,12 eski tablo veri/audit hash ve yerel API Healthy. API sözleşmesi, kanıt/ekranlar ve AGENTS/standart/README/mimari/migration kayıtları güncellendi. Roadmap dört görev koyu/tikli; faz4/4 %100.00, iş67/83 %80.72. Ana DB'de başarılı finans mutation yok; dosya/kategoriCRUD/banka/ödeme emri gelecekteki sınırlar. İŞ08 ve ayrıUI07 onaylanmadı, başlatılmadı.

## 10.10.2026 — İŞ 08 başladı

Kullanıcı onayıyla duyuru/dosya kapsamı analiz edildi; STATUS/SESSION ve PHASE_08_ANNOUNCEMENTS_DOCUMENTS.md hazırlandı. Henüz uygulama veya yeni test kabulü tamamlanmadı.

### İŞ08 adım01/02 — TAMAMLANDI

Announcement/StoredDocument, EF ortak audit/composite FK ve metadata migration; CommunicationService/Store/Endpoints; özel Minio7.0.0 sağlayıcısı; bağımsız Duyurular/Belgeler menüleri, sağ panel ve TR/EN; ortak transport multipart/blob. 317 .NET,76 FE,typecheck/build,316 lisans geçti. Yerel MinIO IPv4 düzeltmesi; SDK LICENSE container dağıtımına dahil edildi.

### İŞ08 adım03 — TAMAMLANDI

10 ek sınırı ve yayın/yükleme yarışı, beklenmeyen bucket politikasının reddi gerçek MinIO’da doğrulandı; yeni27 test geçti. Migration yerelde uygulandı; API yeni kodlarla yeniden başlatıldı. İlk canlı migration öncesi hash kontrolü Docker socket iznine takıldığı için bu kanıt başarılı sayılmadı; ilk yükseltme ayrı PostgreSQL’de doğrulandı.


### İŞ 08 adım04 — TAMAMLANDI — 10.10.2026

**4/4 resmi görev=%100.00; genel iş71/83=%85.54.** Duyuru ekleri ve özel belge arşivi;9 yeni domain/18 integration/9 frontend testi dahil **319 .NET/76 FE, başarısız/atlanan0**. Son roadmap TR/EN metninden sonra FE test/typecheck/build tekrar geçti;316 lisans,19 korunan dosya SHA256 ve15 eski tablo tekrar-migration hash eşliği doğrulandı. Yerel API5005/FE3003 sağlıklı. TR512px sağ panel/boş form uyarısı, EN390px mobil belge paneli taşmasız; Türkçe/normal viewport geri yüklendi, geçici kabul sekmesi kapandı. Kullanıcı DB'sinde başarılı duyuru/dosya mutation yapılmadı; ilk canlı upgrade öncesi hash eksikliği açıkça TESTING/SESSION'da kaydedildi.

Etkilenen dosyalar: backend Announcement/StoredDocument, Application Communication/ErrorCatalog, Infrastructure CommunicationStore/Configurations/MinioFileStorage/DI/DbContext/migration+snapshot, API CommunicationEndpoints/Program/ExceptionHandler/ApiProblems; iletişim unit/API/migration testleri ve mevcut factory/ExpenseMigrationTests; FE features/communication, bağımsız rota/menüler, ortak authorized-request/useAuthorizedApi, problem/error-codes, TR/EN sözlükler ve communication/api-feedback testleri; scripts/dev.sh/test.sh, Infrastructure/UnitTests csproj ve API/Worker Dockerfile lisans kopyaları. Son teslimde milestones.ts dördüncü görevi, TESTING/progress, API errors/communication, core/MIGRATIONS/README/AGENTS/Convention/skill-agent/lisans kayıtları ve gerçek UI screenshot kanıtları güncellendi. Minio7.0.0 SDK lisansı resmi kaynaktan korundu; FE manifest/lockfile/eski uygulanmış migration kaynakları değiştirilmedi. İŞ09/UI yeni aşama başlatılmadı.


## 10.10.2026 — İŞ09 başladı

Kullanıcı fazı başlatmayı onayladı; SMTP seçildi, SMS sağlayıcı ertelendi. Kök kurallar/progress/mimari/RabbitMQ/Worker ve ayrı UI planı incelendi; PHASE_09_MESSAGING analizi, TESTING ön planı, STATUS/SESSION güncellendi. Uygulama görevi henüz tiklenmedi.

## 2026-10-11 — İŞ 09 adım01: mesaj sözleşmeleri ve RabbitMQ yayıncı

Dal `faz-09-messaging`. Application `Messaging.cs` (3 sürümlü sözleşme, zarf, doğrulama, bounded JSON), Infrastructure `RabbitMessaging.cs` (durable quorum topoloji, confirm+mandatory yayıncı), 20 unit + 6 RabbitMQ integration testi; RabbitMQ.Client 7.2.1 lisans metni/envanter, roadmap adım01 tiki ve TR/EN doğrulama metni, analiz/TESTING/STATUS/SESSION güncellendi. **345 .NET, 76 FE, typecheck, 318 lisans geçti.** DB/migration/.env değişmedi.

## 2026-10-11 — İŞ 09 adım02: Outbox ve tüketiciler

BaseEntity `outbox_messages`/`processed_messages` + migration/snapshot, EfMessageOutbox, OutboxRelay, MessageProcessor, RabbitMessageConsumer, Worker relay/consumer host'ları (FoundationWorker kaldırıldı), compose worker DB bağlantısı, 10 yeni PG+RabbitMQ testi; RabbitMQ test koleksiyonu ve sağlam temizlik; communication migration testi yeni geri alma sırasına uyarlandı. **355 .NET, 318 lisans geçti.** Yerel ana DB/.env değişmedi.

## 2026-10-11 — İŞ 09 adım03: Retry, dead-letter ve idempotency

Gecikmeli TTL retry kuyrukları, deneme limiti, `x-buildingflow-attempt/failure` başlıkları, confirm'li retry/DLQ yayınından sonra ACK, ana kuyruk delivery-limit/at-least-once DLX; 5 RabbitMQ + 9 politika testi, test temizliği retry kaynaklarını kapsar. **369 .NET, 318 lisans geçti.** DB/migration/.env değişmedi.

## 2026-10-11 — İŞ 09 adım04: SMTP e-posta ve faz teslimi

`SmtpOptions`/`SmtpEmailHandler`, kalıcı teslim hatası için doğrudan DLQ, Worker SMTP kaydı, compose worker Mailpit ayarları, `scripts/test.sh` geçici Mailpit; 5 SMTP/uçtan uca test; analiz yapılandırma bölümü, roadmap 4/4. **374 .NET, 76 FE, typecheck, 318 lisans geçti.** Yeni paket, DB değişikliği veya .env değişikliği yok. **İŞ09 4/4 = %100.00.**

## 2026-10-11 — BAKIM: Sabit menüler, geniş Okumalar ve tüketim toplamı

WorkspaceShell/globals.css:100dvh sabit çerçeve, orta scroll, sol/üst/alt menü, mobil320px sol Dialog, kapsam/rol korunan alt kısa yollar ve skip-link. MetersWorkspace: Okumalar sağ paneli896px; diğer form panel genişlikleri korunur. ReportsWorkspace/lib/format.ts: ayrı üç tür toplam kartı, BigInt üç ondalık formatter, fatura/gider açıklaması ve mevcut Expense bağlantısı. TR/EN sözlükleri, iki report testi ve mevcut roadmap doğrulama anahtarları güncellendi; frontend-rules/API açıklaması/TESTING/progress ve altı ekran kanıtı. **101FE,typecheck/build,331lisans,196/196koruma ve TR/EN masaüstü/tablet/mobil kabulü geçti.** API/DB/finans modeli, .env, paket/lock ve mevcut Next config farkları değişmedi; kaynakta otomatik maliyet/tarife yok. .NET tekrar çalıştırılmadı; canlı iş verisi mutation/commit/merge/push yok.

## UI03 sol/üst menü teslimi — 12.10.2026

**Onaylanan dar kapsam 3/3 adım, %100.00 tamamlandı.** Koyu lacivert248px sol menü, mavi aktif satır,58px üst bar, menü arama dialogu, gerçek oturum adı/rolü ve profil içinde Keycloak çıkışı. Masaüstü daraltma; mobil mevcut sol Dialog ve sabit çerçeve korunur. Yeni paket/DB/API değişikliği yok; tek agent, ek skill yok.

**Bu tur101/101 FE, typecheck/build,331 lisans kontrolü geçti.** Tarayıcı profil menüsünde Çıkış yap görünür; aramada Rapor yalnız Raporlar sonucunu verdi;390×844 mobil panel tüm menüleri gösterdi, yatay taşma0 ve son header58px doğrulandı. Kullanıcının oturumu kapatılmadı; gerçek logout redirect bu tur yeniden çalıştırılmadı. Rol koşulları kod incelemesiyle korundu; ayrı rollerle tarayıcı kabulü yapılmadı. Backend testleri çalıştırılmadı. İlk son kontrol komutu yanlışlıkla kökten çalıştırıldığı için package.json bulunamadı; doğru frontend klasöründen tekrar başarılı.

Görsel birebir eşlik sınırlamaları: referans fotoğraf/logosu yerine Lucide bina işareti ve çizim; avatar gerçek oturum baş harfleri. Bildirim sayısı uydurulmaz; merkez henüz mevcut değildir. API'si bulunmayan menüler Planlandı açıklamalı disabled. Üst arama mevcut modülleri bulur, kişi/daire verilerinde global API araması değildir. UI03 geniş planın breadcrumb/global veri araması tamamlandı sayılmaz; yalnız kullanıcının bu tur istediği menü kapsamı teslim edilir. İş yol haritası83/83 %100.00 korunur; yeni görev/tik yok. UI toplam ilerlemesi BİLGİ MEVCUT DEĞİL.

Kanıt: [Profil ve menü](../screenshots/ui03-navigation-profile.png). Sonraki adım: yeni kullanıcı talebi; diğer ekranlara geçilmedi.

Kota kaynağı Codex usage/rateLimitsByLimitId.codex, teslim öncesi okuma:300dk kullanılan%43/kalan%57, reset12.10.2026 04:04:09 Europe/Istanbul;10080dk kullanılan%38/kalan%62, reset18.10.2026 09:28:33. Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Son build tekrarında SWC native cache sandbox erişim engeli oluştu; izinli `npm run build` tekrarı uygulandı. İlk başarılı build kanıtı ayrı korunur. Son izinli build başarılı; son sözlük/erişilebilir ad değişikliklerinden sonra101/101 FE tekrar geçti.

## BAKIM bayraklı dil teslimi — 12.10.2026

**3/3 %100.00: LanguageSwitcher TR/EN bayraklı; /login ve ana ekran aynı HttpOnly uygulama dil cookie'sini kullanır. Keycloak tema menüsünde bayrak, script ile yalnız whitelist dil tercihi cookie'si; aynı hostname/farklı port yerel girişte dil ana ekrana taşınır.** Kimlik bilgisi/token/auth URL saklanmaz, auth politikası değişmedi. Ayrı auth hostname entegrasyonu bu host cookie mekanizmasıyla desteklenmez; dağıtımda ayrıca ele alınmalıdır.

**102/102 FE, typecheck/build,331 lisans ve git diff --check başarılı.** Tarayıcı /login English→ana ekran en; gerçek Keycloak giriş menüsünde English seçimi→ana ekran en doğrulandı. Kullanıcı parolası girilmedi/oturum kapatılmadı; yeniden parola gönderimli login testi yapılmadı. Test sonrası dil Türkçe'ye döndürüldü. Yeni paket/DB/backend yok; tek agent, skill yok. Çalışma ağacındaki önceki değişiklikler korundu. Geçersiz tercih için whitelist/fallback testi eklendi.

Dosyalar: LanguageSwitcher.tsx, i18n/{locale,actions,request}.ts, globals.css, standards.test.mjs; Keycloak template.ftl/theme.properties/resources/js/language-preference.js; TESTING ve progress kayıtları. İş yol haritası83/83 %100.00 korunur; UI toplam yüzdesi BİLGİ MEVCUT DEĞİL. Sonraki adım yeni kullanıcı kapsamı.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Son build sandbox SWC önbellek erişim engelinden sonra izinli tekrar ile başarılı tamamlandı.

## BAKIM yol haritası teslimi —12.10.2026

**2/2 %100.00 tamamlandı. Roadmap.tsx ilk00–03 bölümlerindeki varsayılan open kaldırıldı;15 bölüm kapalı başlangıç.** Tarayıcı yeni ana sayfada açık details0→tıklama1→kapatma0; ekran docs/screenshots/roadmap-collapsed.png. Typecheck ve build başarılı; build sandbox SWC önbellek engeli sonrası izinli tekrar geçti. Yeni test eklenmedi/npm test bu tek sunum değişikliğinde tekrar çalıştırılmadı; paket/lock değişmedi. Önceki kullanıcı değişiklikleri korunur. İş83/83 %100.00; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, skill yok. Sonraki işlem yeni talep. Giriş/çıkış/toplam token ve kalan context ayrı ayrı BİLGİ MEVCUT DEĞİL.

## BAKIM menü görselleri teslimi —12.10.2026

**2/2 %100.00 tamamlandı. WorkspaceShell.tsx alt bina dekorasyonu/House import kaldırıldı, globals.css kullanılmayan dekorasyon stili kaldırıldı, next.config.ts devIndicators:false.** Mevcut config ayarları korunur. Typecheck/build geçti; ilk typecheck dev restart sırasında rota tipleri eksik, ilk build SWC sandbox cache engeli: build izinli tekrar, typecheck tekrar başarılı. Tarayıcı dekorasyon0/Next dev düğmesi0; ekran docs/screenshots/sidebar-clean.png. Mobil aynı navigation JSX kaynağı, ayrı mobil kabul bu tur çalıştırılmadı. npm test bu görsel/config değişikliğinde tekrar çalıştırılmadı, yeni paket/lock değişimi yok. İş83/83 %100.00 korunur; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, ek skill yok. Sonraki işlem yeni kullanıcı talebi.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim12.10.2026:300dk kullanılan%73/kalan%27 reset12.10.2026 04:04:09;10080dk kullanılan%43/kalan%57 reset18.10.2026 09:28:33 Europe/Istanbul.

## BAKIM kişi profili ve iletişim teslimi — 12.10.2026

**Onaylı kapsam 5/5 adım = %100.00 tamamlandı.** Mevcut İŞ04 (05/14), İŞ06.1 (08/14) ve İŞ09 (12/14) ile ilişkilidir. Yeni faz/backlog veya görev paydası oluşturulmadı; kişi görevinin açıklama ve kanıtı aynı kimlikle güncellendi. İş yol haritası **83/83 = %100.00**; bütün UI planının adım yüzdesi **BİLGİ MEVCUT DEĞİL**. Tek agent; ek skill veya alt agent yok.

- **Kişi profili:** gerçek/tüzel, yerli/yabancı; ülke, kombinasyona uygun kimlik, doğum tarihi, cinsiyet, adres. Yerli gerçek kişide T.C. numarası (11 rakam, ilk rakam 0 değil); yerli tüzelde vergi numarası (10 rakam); yabancı gerçek kişide pasaport; yabancı tüzelde ülke ve kuruluş kimlik numarası zorunlu. Telefon ve e-posta yeni POST/PUT'ta zorunlu. Tüzelde kurum adı kullanılır; doğum tarihi/cinsiyet null. Kullanıcı bu kapsamı, yabancı kuruluş alanlarını ve önceki TC toplamama kuralının değişmesini onayladı. Resmî kimlik doğrulama servisi yok; yalnız biçim kontrolü yapılır.
- **Liste ve detay:** gerçek daire, blok, site/apartman, kişi türü, ilişki rolü, uyruk, telefon, e-posta kolonları; birden fazla daire/rol birlikte gösterilir. İsim/şirket/telefon/e-posta sunucuda aranır. Legacy profil bilgileri null kalır, güncellemede kullanıcı eksikleri tamamlar. TanStack masaüstü tablo, tablet/mobil kart ve ortak sağ panel kullanılır.
- **İletişim:** aynı Property'deki aktif üyelik hesapları arama/sayfalama ile seçilir; hesap veya yetki oluşturulmaz. Kalıcı PersonMessage, başlık/mesaj, UUID/fingerprint replay, kişi version ve Property kilidi; alıcının JWT sub + aktif üyelik kapsamındaki bildirim kutusu ve okundu kaydı. SMS kullanıcı kararıyla sağlayıcı bağlanana kadar kapalı. Bildirim uygulama içidir; SMS, e-posta veya OS push değildir. Hesap seçenekleri mevcut subject kimliğini gösterir; Keycloak ad/directory entegrasyonu yok.
- **Arama/aidat:** `tel:` bağlantısı cihazın arama uygulamasını açar; bu tur çağrı başlatılmadı. Kişi aidat geçmişi DuesChargeParty snapshot filtresinden okunur. Legacy borç sonradan kişiye atanmaz; ACCOUNTANT kişi verisi yetkisi kazanmaz.
- **Persistence:** `20261011223446_PersonProfilesAndMessages` source/Designer/snapshot; people üzerinde 8 nullable yeni kolon, profil check, PersonMessage BaseEntity ve Property/Person composite FK + UUID index. Ortak sekiz NOT NULL kolon korunur. Profil veya mesaj geçmişi varsa Down ilk drop öncesinde reddedilir. Yerel update sonrası **22 eski tablonun mevcut alan/row/audit/version hashleri eşleşti**. Kimlik verisi backfill edilmedi; eski uygulanmış migration değiştirilmedi.

**Son otomatik sonuç: 481/481 .NET (109 unit + 372 integration), 104/104 FE; hata ve atlama 0.** Typecheck, production build, EF model/migration eşliği, 331 lisans ve `git diff --check` geçti. Yeni paket veya lock değişikliği yok; restore sonrası lisans kontrolü çalıştı. İlk tam backend çalışmasında 3 eski migration fixture testi başarısız oldu; gerçek migration adına bağlama ve eski Down sonrası model sorgusundan önce son şemayı geri yükleme düzeltildi, sonraki tam çalışma geçti. İlk geçici SDK derlemesi yerine kurulu .NET SDK kullanıldı; sandbox SWC önbellek engeli izinli build tekrarıyla çözüldü. Yanlış cwd'deki npm denemesi başarılı kontrol sayılmadı.

**Ayrı PostgreSQL kanıtı:** dört profil kombinasyonu, eksik kontak/kimlik/ülke, gelecekte DOB, yanlış hesap, version/ilişki/rol/scope; hesap seçenekleri, SMS reddi, aynı/farklı UUID içerikleri, alıcı izolasyonu, okundu ve üyelik iptali sonrası kutunun gizlenmesi. İlk kurulum/yükseltme/tekrar, boş Down/re-Up, dolu Down reddi, model ve bütün BaseEntity tablolarında kolon kontrolü geçti.

**Gerçek Chrome kabulü:** TR/EN alanları ve zorunlu telefon/e-posta; boş kayıtta 5 invalid alan ve ilk Ad odağı. Dört kombinasyonda doğru kimlik etiketi, tüzelde DOB/cinsiyetin gizlenmesi. 390×844 sağ panel genişliği 390, iç taşma 0; 820×1180 tablo sarmalayıcısı computed display `none`, kart görünümü, body taşma 0. Mesaj panelinde SMS açıklaması, arama bağlantısı, snapshot aidat metni; ESC kapanış. İngilizce bildirim kutusunun boş durumu yüklendi. Geçici sekmeler kapandı, viewport sıfırlandı ve dil Türkçe'ye döndürüldü.

Kullanıcının iş kayıtlarında form save veya mesaj gönderimi yapılmadı. Gerçek kişiye SMS/bildirim gönderilmedi; başarılı gönderim/teslim/readAt kanıtı ayrı fixture API testlerindedir. Tarayıcıda başarılı mesaj mutation'ı ve gerçek telefon görüşmesi kabulü yapılmış sayılmaz. Ekran kanıtları boş yeni formu gösterir; masaüstü görseli mevcut kişisel veri içeren arka planı dışarıda bırakır. API5005 `/health/live` 200 ve FE3003 çalışır. Docker uygulama imajları bu tur derlenmedi.

Değişen kaynaklar: Domain Person/PersonMessage; Application People/PersonMessages/ErrorCatalog; Infrastructure PeopleStore/PeopleConfigurations/PersonMessageStore/DbContext/DI ve üç migration dosyası; API PeopleEndpoints/Program. FE EntityEditor/RecordDetails/PeopleWorkspace/PersonPage/types/management-contracts/AuthGate; yeni PersonProfileDetails/PersonAccountPicker/PersonActionsPanel/NotificationInbox/messages; TR/EN, error registry ve mevcut roadmap kanıtı. Domain/API/migration/legacy fixture testleri ve FE person-profiles testi. AGENTS, core, people, MIGRATIONS, frontend-rules ve progress/test kayıtları güncellendi. Önceki kullanıcı farkları ve staging korundu; commit/push yapılmadı.

Kanıtlar: [API](../api/people.md), [yabancı tüzel form](../screenshots/person-foreign-legal-form.png), [mobil form](../screenshots/person-profile-mobile.png). Sonraki işlem yeni kullanıcı kapsamıdır.

Son kota okuması: 12.10.2026 07:39:22 Europe/Istanbul, Codex usage / rateLimitsByLimitId.codex. 300 dk kullanılan %50/kalan %50, reset 12.10.2026 12:21:12; 10080 dk kullanılan %55/kalan %45, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**; çıkış token: **BİLGİ MEVCUT DEĞİL**; toplam token: **BİLGİ MEVCUT DEĞİL**; kalan context: **BİLGİ MEVCUT DEĞİL**; oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

## BAKIM görsel taslak — Yeni kişi formu (12.10.2026)

**2/2 %100.00 tamamlandı:** kullanıcının ekran görüntüsü incelendi; imagegen skill'i ve built-in image_gen ile iki sütunlu/gruplu sağ panel taslağı üretildi. Kimlik, iletişim, bildirim hesabı; son görselde hesap eşleştirmesi isteğe bağlı olarak düzeltildi. Başlık, zorunlu alanlar ve alt kaydet/vazgeç görsel olarak kontrol edildi. Görsel: docs/analysis/design/person-create-proposal-v1.png. Bu yalnız görsel öneridir; uygulama kodu değiştirilmedi, typecheck/build/API testleri bu görsel turunda yeniden çalıştırılmadı. Yeni paket yok, roadmap tıkları ve83/83 sayacı korunur. Sonraki adım kullanıcının görsel hakkındaki talebidir. Giriş token/çıkış token/toplam token/kalan context: her biri BİLGİ MEVCUT DEĞİL.


### 2026-10-12 — BAKIM / UI06 kişi formu tasarımı teslimi

**Tamamlandı: 3/3 adım, bu tur %100,00.** İş yol haritası 83/83 (%100,00), UI planı toplam 12 aşama; mevcut UI06 kapsamına sunum iyileştirmesi, yeni faz/tik eklenmedi. Kişi formu 768px gruplu iki sütun, mobilde tek sütun ve sabit eylem alanı; cinsiyet/uyruk ikonları, bayraklı ülke adları ve tek aranabilir hesap seçicisi eklendi. Konum entegrasyonu kullanıcının kararıyla sonraya bırakıldı, adres manuel. API/veritabanı/bağımlılık değişmedi.

**Kanıt:** typecheck başarılı; frontend 104/104 test başarılı; üretim derlemesi mobil CSS düzeltmesi sonrası tekrar başarılı; git diff --check başarılı; paket manifest/lock değişikliği yok. Chrome masaüstü ve390×844 mobil: dört kimlik varyantı, zorunlu alanlarda5 hata/ilk alana odak, hesap popover Escape/odak dönüşü doğrulandı. Mobil son ölçüm388px içerik/388px scrollWidth, footer görünür. Görseller docs/screenshots/person-form-design-desktop.png ve person-form-design-mobile.png. Gerçek kişi kaydı/mesaj gönderimi yapılmadı.

Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL. Codex hesap son okuma: 300dk kullanılan%93/kalan%7, reset12.10.2026 12:21:12 Europe/Istanbul; 10080dk kullanılan%62/kalan%38, reset18.10.2026 09:28:33. Hesap kotası bu turun token tüketimi değildir.

## PROTOKOL — AkaDev AI FAZ 03 ortak çekirdek — 12.10.2026

**Bu tur 4/4 adım = %100.00:** mevcut kaynak analizi, hazırlık/metadata kontrolü,
izinli kurulum, statik kabul/teslim. Yedi ortak Skill ve yedi repo içi Codex bağlantısı;
kök/frontend Claude import girişleri, salt okunur audit istisnası ve güncel katalog.
Tek Agent kullanıldı; ayrı Agent veya MCP kurulmadı.

**Statik kabul:** yedi YAML manifesti, ad/açıklama, proje içi referanslar ve bağlantı
hedefleri, Claude importları, izinli değişiklik kapsamı, kaynak/manifest/lockfile/CI/roadmap
SHA256 koruması ve git diff --check doğrulandı. PyYAML mevcut olmadığından hazır
quick_validate.py kullanılamadı; YAML Ruby/Psych ile ayrıştırıldı.
Uygulama testleri, restore/install, servis/DB ve runtime keşif/tetikleme denenmedi;
geçmiş 481.NET/104FE sonuçları bu tur yeniden çalıştırılmış sayılmaz.
Ürün/UI yol haritası ve resmi iş paydası korunur. FAZ 04 başlatılmadı.
Token/context ölçümleri: BİLGİ MEVCUT DEĞİL; hesap kotası tur tüketimi değildir.

## BAKIM: ortak PostgreSQL 18.6 — 12.10.2026

Tek bağımsız `../postgres-18.6` konteyneri, `projects` veritabanı,
`buildingflow` uygulama şeması ve `buildingflow_keycloak` kimlik şeması.
Yerel host `localhost:5436`; Docker host `postgres-18.6:5432`.
Bağlantı Search Path ile şema seçer; uygulanmış EF migration dosyaları korunur.
API/Worker/migration/Keycloak harici `shared-postgres-net` ağına katılır.
Yerel Compose PostgreSQL oluşturmaz; eski volume’ler geri dönüş için saklanır.
Yeni ürün fazı veya roadmap tıkı eklenmedi. Doğrulama sonuçları ortak README’de.

Ortak DB bakım kabulü:4/4=%100.00 (bağımsız kurulum,412 tablo veri koruması,
üç proje bağlantıları,gerçek doğrulama). PostgreSQL18.6 üzerinde372 integration
+109unit=481test, hata/atlama0;331lisans kontrolü geçti. Test DB/rol temizlendi.
API /health200; üç Keycloak realm discovery200. TraceFlow ve AkaAI güncel
Java paketleri derlendi. Eski PGvolume'leri korunur; yeni şema yazımları nedeniyle
geri dönüşte kesim sonrası değişiklikler ayrıca korunmalıdır. Yeni ürün fazı/tik yok.
Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL.

## Ortak Keycloak yerel bakım teslimi — 13.10.2026

Tek `keycloak-26.7.5` sunucusu ve `projects.keycloak` şeması; üç realm ve
özgün kullanıcı/istemci/rol kimlikleri korunur. Ortak yönetim localhost8080;
TraceFlow mobil issuer güncel LAN192.168.2.95:8080 üzerinden.
Eski Keycloak konteynerleri/şemaları geri dönüş için korunur. Uygulama
şemaları değişmez. Kaynak ve ayrıntılı operasyon/test kanıtı:
`../keycloak-26.7.5/README.md` (AkaAI kökünden ../../keycloak-26.7.5).
LDAP/federationID, DB rol izolasyonu, üç API için kendi token kabulü ve
iki diğer realm token retleri; özel reset SPI anonim401/kendi204/diğer403.
BuildingFlow17gerçek auth kontrolü, AkaAI10yerel/token testi,35frontend
auth/güvenlik testi ve TypeScript geçti. Tam AkaAI frontend166/167;
mevcut İngilizce compact sayı testinde1.3M/1.3m farkı kaydedildi.
Test fixture'ları temizlendi. Domain/roadmap ve on-prem AkaAI değişmedi.

## BAKIM — TraceFlow Gateway / MinIO port ayrımı — 13.10.2026

TraceFlow Gateway9000 ile çakışan MinIO host portu9030'a taşındı.
.env/.env.example ve Compose fallback hizalandı; Docker içi minio:9000
bağlantısı ve mevcut veri volume'u korunur. README ve iletişim API rehberi
güncellendi. GEÇTİ: MinIO9030/minio/health/live200, BuildingFlow5005/health200,
TraceFlow9000/actuator/health200 (IPv4 ve localhost). 9000 yalnız Java Gateway
dinliyor; MinIO9030 Docker eşlemesi doğrulandı. TraceFlow gerçek Chrome
S256 PKCE parola girişi ve originalclient code exchange geçti. IntelliJ
servislerinde eski Keycloak IP ayarı bulundu; yeniden başlatma kullanıcıda
bekliyor, mevcut API token401 sonucu başarı sayılmadı. Ürün fazı/yol haritası değişmedi; yeni bağımlılık yok.


## MOBİL Faz 04 — 13.10.2026

Kullanıcı onayıyla ayrı mobil uygulamaya OIDC/PKCE + SecureStore ve hesap/rol/mülk bağlamı. Yeni GET /api/v1/mobile/session; mevcut Person.LinkedUserId/PropertyMembership/UnitOccupancy okunur, şema/migration yok. Keycloak yalnız buildingflow-mobile public client; basic scope ve API audience, S256 zorunlu. Development issuer explicit allowlist, Production kuralları korunur. Ana API5005 durdurulmadı; mobil geliştirmeAPI5006 ayrı süreç. Mevcut kullanıcı parolaları/iş verileri değişmedi.
Bu tur45/45 seçili integration, hata/atlama0; full regresyon yeniden çalıştırılmadı. Mobil TypeScript/lint8/8, üç platform export, ExpoDoctor21/21; backend331 lisans. Chrome geçici fixture PKCE giriş/API session/profil/çıkış ve yenileme kontrolü. Native cihaz/Keychain kabulü yapılmadı; ExpoGo OAuth desteklemez, development build gerekir. Önceki npm28 audit uyarısı açık. Ürün83/83/UI roadmap sayacı değişmedi; mobil plan buildingflow-mobile-prompts/PHASES.md'dir. Faz05 başlatılmadı; token tüketimi/kalan context BİLGİ MEVCUT DEĞİL.
Sözleşme docs/api/mobile-session.md; ayrıntı ayrı mobil docs/mobile/PHASE04.md.


### Mobil Faz04 devam — kullanıcı görsel referansı / native build
13.10.2026: kullanıcı BuildingFlow Mobil Uygulama Tanıtımı.png referansını tüm UI fazları için onayladı. Ayrı mobil DESIGN_REFERENCE.md + AGENTS güncellendi; login lacivert hero/tipografi/Ionicons, alt menu çizgi ikonlarına geçti. TypeScript/lint8/8 ve üç platform export başarılı;690 mobil üst seviye lisans metadata belirsiz0. Geçici fixture remote logout sonrası uygulama login'e döndü; girişte Vazgeç popup'ı kapattı. Fixture hesabı ve yerel credential dosyaları temizlendi.
iOS development build0hata/1Expo-script-uyarısı ile iPhone17Pro simulator'a kuruldu/açıldı; bu oturumdaki CUA Simulator'a erişemediği için native OAuth/Keychain görsel kabulü DOĞRULANAMADI. Fiziksel/Android kabulü açık, mağaza yayını yok. LAN104→6 değişimi gitignored mobil env ve ayrıAPI5006 issuer listesine uygulandı, Metro8081devclient açık. Ana API5005 ve iş/UI roadmap korunur; Faz05 başlamadı. Ayrıntı ayrı mobil docs/mobile/PHASE04.md.

## MOBİL Faz06 — 14.10.2026

Kullanıcı mobil API önkoşulu eksiğinin backend'de tamamlanmasını onayladı. Yeni mobile/sync command/pull, actor-scope idempotence receipts, version409 ve tombstone; migration20261014062447_MobileRequestSync yeni iki tabloyla yerel dev DB'ye uygulandı. Existing domain/UI83/83 sayacı değiştirilmedi. Mobile talep formu/photo sonraki Faz12'de; yalnız altyapı ve profil sync paneli eklendi.

Bu tur52/52 seçili integration,0atlama; backend build0hata/0uyarı ve EF model check geçti. Mobil26/26, TypeScript/lint/üç platform export; iPhone şifreli SQLite nativeQueueReplay fixturetrue (HTTP simülasyonu, live business write yok). API5006/Metro8081 güncel LAN192.168.2.95; test modu kapalı. Main API5005 korunur. Gerçek uçak modu/soğuk açılış ve Android native UI kabulü açık. Yeni dependency yok; önceki npm28audit uyarısı devam ediyor. Kapsam docs/api/mobile-sync.md ve ayrı mobil docs/mobile/PHASE06.md. Commit/push/yayın yok. Token tüketimi/kalan bütçe BİLGİ MEVCUT DEĞİL.


## BAKIM — ortak BuildingFlow marka simgesi — 14.10.2026
Kullanıcı referansındaki üç bina işareti SVG olarak yeniden çizildi. WorkspaceShell markası yeni BuildingFlowMark bileşeni ve src/brand/buildingflow-mark.json kullanır; public/çıkış ve Keycloak giriş teması aynı buildingflow-logo.svg çıktısını kullanır. Mobil canonical kaynak buildingflow-mobile/mobile/assets/brand/mark.json; marka değişikliklerinde merkez aynası aynı üretimden yenilenir. Eski PNG varlıkları korunur; yeni bağımlılık yok.
Bu bakım3/3=%100: referans/varlıklar, kod bağlantıları, doğrulama/belge. Merkez typecheck ve Next production build geçti; ayrı127.0.0.1:3004 önizlemede SVG complete=true. Ana3003 Docker web yeniden dağıtılmadı. Mobil26/26 test, TypeScript/lint, iOS build/install/launch ve üç platform export; ekran kanıtları buildingflow-mobile/docs/mobile/brand-*.png. Fiziksel telefon görsel kabulü ve Android runtime henüz yok. Backend iş testleri bu marka bakımı nedeniyle yeniden çalıştırılmadı. İş/UI yol haritası değişmedi, Mobil Faz07 başlatılmadı. Ayrıntı kardeş mobil repo docs/mobile/BRAND.md.
Token/context: BİLGİ MEVCUT DEĞİL. Son Codex usage okuması14.10.2026:300dk kullanılan%12/kalan%88;10080dk kullanılan%64/kalan%36. Hesap kotası bu turun token tüketimi değildir.


## BAKIM — MinIO yerine SeaweedFS4.48 — 14.10.2026
Tamamlandı3/3=%100: kaynak/lisans ve veri incelemesi; S3/servis/yedek refactoru; gerçek test ve yerel geçiş. API/worker/web ve mobil API yeni depoya bağlı; MinIO SDK/konteyneri kaldırıldı. Legacy volume/0600 kaynak ayarı korunur; kaynakta0nesne.96/96seçili.NET,104/104FE,8/8izolebackup/restore;24negatifpreflight;329lisans metadata; Dockerbuild/smoke başarılı. Aynı servis datasetlerinde gerçek kullanıcı dosyası/mesaj yazılmadı; yeniDBmigration yok. Kanıt SEAWEEDFS_MIGRATION.md ve artifacts/storage/migration-report.json. Faz07fotoğrafAPI/UI henüz tamamlanmadı; kullanıcı onaylı akış mobilPHASE07.md'de, önce bu refactor istendi. İş/UI roadmap paydası korunur.
Token/context: BİLGİ MEVCUT DEĞİL. Son Codexusage okumasında300dk kullanılan%84/kalan%16,10080dk%75/kalan%25; hesap kotası bu turun token tüketimi değildir.


## MOBİL Faz 07 — profil ve merkezi fotoğraf — 14.10.2026
Kullanıcı onaylı profil düzenleme, isteğe bağlı kamera/galeri/kırpma/önizleme ve kaldırma uygulandı. Merkez own-account API; özel SeaweedFS4.48 upload/download, SHA256, EXIF metadata temizliği, UUID makbuzu/version yarış koruması. Yeni iki BaseEntity tablo ve composite FK/Down veri koruması; migration20261014162556 yerelde uygulandı. Mobil SQLCipher schema4 metadata ve AES-GCM fotoğraf/komut dosyaları; sabitUUID retry,403purge/409conflict, makbuz sonrası indirme hatası tekrar yazım üretmez. Onaylı hukuki metin yok; teknik tercih rıza sayılmadı.
Bu tur27/27 seçili gerçek PostgreSQL/SeaweedFS integration test,0atlama; mobil36/36/lint/typecheck/üç platform export. iPhone build0hata/1toolchainuyarı; install/launch ve SQLCipher/AES-GCM dosya round-trip/AAD/yanlış anahtar/cleanup fixture geçti. Test modu kapatıldı. Kullanıcı fotoğrafı/canlı profil yazımı veya görsel inceleme yapılmadı (kullanıcı bakmana gerek yok dedi). Merkez API/worker/web Dockerbuild/deploy ve5005/5006/8081/3003-login200;329lisansmetadata. Önceki signed-out UI iyileştirmesi bu turda dağıtıldı;104/104FE testi önceki UI turunda geçmişti.
Faz08 başlatılmadı; gerçek kamera/galeri/uçak modu ve Android native UI kabulü açık. Ayrıntı docs/api/mobile-profile.md ve mobil docs/mobile/PHASE07.md. İş/UI roadmap sayacı değişmedi; commit/push/yayın yok. Token tüketimi/kalan context BİLGİ MEVCUT DEĞİL.


## MOBİL Faz08 — Ev sahibi — 14.10.2026
Kullanıcı onayıyla own-unit owner snapshot API ve mobil ana sayfa/Borçlar/Duyurular/Talepler bağlandı. Aktif linkedPerson/OWNEROccupancy/üyelik/property/unit/building; yönetici claim’i bypass değildir. Minimal kiracı adı/ilişki tarihleri; sözleşme/rent modeli bulunmadığı açıkça gösterilir. UNIT+own-owner borçlar; tenant/başkamalik kişiselborcu yok. Duyuru PUBLISHED/ALL_MEMBERS, talepler own-account/OWNER/unit. Sınırlar50charge/10tenant/20duyuru-talep, morebayrakları; totals tüm yetkili satırlar. Yeni backend entity/migration yok; mobil SQLCipher schema5scope/TTL/403purge,4→5profil taslağı koruması.
Bu tur59/59 seçili gerçek izolePostgreSQL integration,0atlama;41/41mobil, lint/typecheck/üç platformexport; build0hata/0uyarı. API/workerDockerbuild/deploy;5005/5006health200,anonimowner401,Expo8081status200. Yeni dependency yok. Görsel/gerçek telefonuçakmodu/Androidruntime kontrolü bu tur yapılmadı; gerçek iş kaydı/OWNER rol ataması yok. Faz09başlatılmadı; ödeme/tahsilat işlem geçmişi11,talepformu12kapsamında. İŞ/UI roadmap sayacı/commit/push/yayın yok. API docs/api/mobile-owner.md ve ayrı mobilPHASE08.md. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## MOBİL Faz09 — Kiracı — 14.10.2026
Kullanıcı onaylı Kiracı ana/finans/duyuru/talep ekranları: ortak owner snapshot servisinin roleTENANTscope’u; yalnız linkedownTenant/aktifOccupancy+üyelik/unit/property/building. UNIT+ownTenantborç; owner/coTenantkişiselborç hariç. Minimal Allocationtarih/tutar/terslenme,bankref/payerPII yok. Kira/kontrat/onlineödeme modeli yokluğu dürüstçe gösterilir. Native1–2000metinli talep, mevcut Faz06SQLCipherqueue/stableUUID/idempotentAPI/SyncPanel; webreadonly, fotoğraf/kategori12kapsamında. Schema5user/unit/rolecache/generationguard/24h/403purge. Merkezi migration/dependency yok.
Bu tur63/63izolePostgreSQLintegration,45/45mobil,lint/typecheck/üçplatformexport; .NETbuild0hata/0uyarı. API/workerDockerbuild/deploy;5005/5006health200,anonimtenant401,Metro200. Kişisel canlıtalep/borç/rol yazımı, görsel ve fizikselUI/uçakmodu kontrolü yok. Faz10başlatılmadı; Commit/push/yayın ve İŞ/UIroadmap sayacı korunur. Sözleşme docs/api/mobile-tenant.md; ayrı mobilPHASE09.md. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## BAKIM — BuildingFlow klasör ve yol adı düzeltmesi — 15.10.2026
Kullanıcı klasör ve tüm ilgili yerel proje yollarını düzeltmeyi açıkça onayladı. Merkez kökü /Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow. Mobil başlatıcı, IDE module/modules.xml, root lock metadata, README/analiz bağlantıları, mobilFaz08–09merkez referansları, sharedPostgreSQL prepare-env ve Keycloak compose/theme sync kaynak yolları güncellendi. IntelliJ eski proje penceresi kapatılıp yeni BuildingFlow penceresi açıldı; yeniden üretilen eski IDE-only ayarı artifacts/ide-rename-backup altında korundu. Kaynak/Git değişiklikleri taşındı; reset/commit/push veya işverisi/rol/parola değişikliği yok. Mevcut gerçek GitHub remote hedefi yeniden adlandırılmadı.
Yeni yolda .NETbuild0hata/0uyarı, mobileTypeScript ve shell/Python syntax geçti. Merkez5005/mobil5006health,web3003/login,KeycloakrealmOIDC veExpo8081status200. Docker veri hacmi adları önce/sonra aynı; API/worker/web/SeaweedFSgateway/core/rabbitmq/mailpit yeni compose yoluyla ayağa kaldırıldı. Keycloak tema mount yeni yola taşındı; yönetici/parola bootstrap çalıştırılmadı. Yeni ürün fazı başlamadı; iş testleri isim/yol bakımı için yeniden çalıştırılmadı. Codex proje etiketi/kayıt yolu için desteklenen güncelleme aracı yok; Codex UI otomasyonu güvenlik nedeniyle kapalıdır. Kullanıcı yeni klasörü Codex proje kaydında açmalıdır. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## Mobil Faz10 — Yönetici deneyimi — 15.10.2026

Kullanıcı sıradaki mobil fazı onayladı.4işpaketi: kapsam/yetki,API+mobil,hedefli doğrulama,belgeleme/çalıştırma. Kod ve otomatik doğrulama teslim edildi; gerçek iPhone/Android küçük-büyük ekran/rol/offline kabulü açık (In review). Merkezin ayrı İŞ/UI/AI sayacı değişmedi; Faz11başlatılmadı.

MobileManager application/store/endpoint; gerçek rol+Property aktif üyelik (SUPER_ADMIN mevcut global scope), RepeatableRead,50satırlık deterministic pagination. Bina/daire/aktif ilişki, tüm bina borç/gecikmiş toplamı, duyuru/talep özetleri; PII-minimal sakin listesi, ACCOUNTANT'a kişi/yönetim verisi açılmaz. Mobil yönetici hero/kartlar/sekme listeleri, context/sectionsonsayfa ayrımlı24saatşifrelicache,403purge/late-responseguard. Readonly kritik kapsam; finans/duyuru/talep mutation yok.

Bu tur48/48mobil;20/20gerçekayrıPG Manager/Owner/Tenant/Session integration0skip; geçiciDB/roletemizlendi. TypeScript başarılı,lint0hata1öncekiuseOwnercleanupuyarısı,3platformexport,merkezbuild0hata0uyarı. Yeni dependency/migration/nativebinary yok; canlı kişisel/finans/rol mutation,commit/push/mağaza yayını yok. Mobil API5006/Metro8081 yeni kodla çalıştırıldı. Önceki kullanıcı talimatıyla görsel inceleme yapılmadı. Detay docs/api/mobile-manager.md ve kardeş mobil docs/mobile/PHASE10.md. Token/context/tur tüketimi:BİLGİ MEVCUT DEĞİL.


## Mobil Faz11 — Aidat/borç/işlem geçmişi — 15.10.2026

Kullanıcı Faz11'i onayladı. Bu tur4işpaketi: finans sözleşmesi,API+mobil,hedefli doğrulama,belge/çalıştırma. Kod ve otomatik kontroller teslim edildi; cihaz/UI kabulü ve online sağlayıcı açık (In review). Faz12başlamadı; merkezin ayrı İŞ/UI/AI sayacı değişmedi.

MobileFinance DTO/query/store/endpoint; OWNER/TENANT gerçek ilişki+aktif üyelik/parent+ownrole/person/UNIT borç filtresi; yönetici/muhasebe gerçek JWTrolü+Property kapsamı.50kayıtsayfalama,chargeId yetki kontrolü,repeatableread,tüm dönem summary ve minimal allocation/reversal geçmişi. PII/banka/ödeme toplamı dönmez. Online ödeme sağlayıcısı mevcut değil; ödeme başlatma/mutation veya başarı üretilmedi.

Mobil ortak finans ekranı,detay/geçmiş geçişi,paraBigInt/ortakparseCharge,24saat/sonsayfa/detailcache,403purge,geçyanıtguard. Yönetici+finans ortak useScopedRead. Yeni dependency/migration/nativebinary yok; canlı finans/rol/kişisel veri yazımı ve commit/push/yayın yok.

Bu tur54/54mobil;42/42gerçekizolePGintegration(MobileFinance6+MobileManager/Owner/Tenant/Session+PaymentApi),8/8PaymentDomain0skip; geçiciDB/roletemizlendi. TypeScript,lint0hata1öncekiuyarı,3platformexport,merkezbuild0hata0uyarı;7GitHubscript testi. Görsel/gerçektelefonofflinekabulü yapılmadı. Kanıt docs/api/mobile-finance.md ve kardeş mobil docs/mobile/PHASE11.md. Token/context/tur tüketimi:BİLGİ MEVCUT DEĞİL.
