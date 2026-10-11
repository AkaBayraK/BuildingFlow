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
