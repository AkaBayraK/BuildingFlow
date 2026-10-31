# Skill ve Agent durumu

Güncelleme: 2026-10-12 — AI FAZ 03. Tek Agent korunur; özel Agent veya MCP kurulmadı.
Yedi ortak Skill'in kanonik kaynağı `.claude/skills/`, Codex bağlantıları `.agents/skills/`.
Dosyalar statik olarak doğrulandı; bu tur yeni Skills ile gerçek görev/runtime testi yapılmadı.

| Skill | Kullanım bağlamı |
| --- | --- |
| `akadev-audit` | İstenen salt okunur depo veya AI yetenek denetimini kapsam ve dosya kanıtlarıyla yürütür. |
| `akadev-contract` | API veya mimari değişiklikte etkilenen backend ve frontend tüketicilerinin sözleşmesini belirler. |
| `akadev-phase` | Kullanıcının fazlara böldüğü BuildingFlow işinde doğru plan kimliği ve kabul kanıtlarını izler. |
| `akadev-verify` | Yetkili değişikliğin kabul kriterlerini BuildingFlow mevcut kalite prosedürleriyle kanıtlar. |
| `akadev-security` | İstenen güvenlik incelemesinde veya auth, üyelik, hassas veri yolları değişiminde sınırları kontrol eder. |
| `akadev-license` | Bağımlılık, imaj veya harici kaynak değişiminde BuildingFlow lisans politikasını ve dağıtım envanterini değerlendirir. |
| `akadev-docs` | Uygulanan davranış veya prosedür değişikliğini mevcut BuildingFlow belge ve kanıt kaynaklarıyla eşleştirir. |

Kural kaynağı [AGENTS.md](../AGENTS.md); kurulum/kapsam [ai-capabilities.md](ai-capabilities.md);
kabul kanıtı [TESTING.md](TESTING.md). Ürün roadmap ilerlemesi değiştirilmedi.
Yeni ortak prosedürler mevcut domain belgelerine yönlenir; kural metinlerini çoğaltmaz.
Bu kurulumda Skill Creator yönergeleri kullanıldı; alt Agent çalıştırılmadı.

## Tarihsel kullanım kayıtları

Aşağıdaki eski sayı ve durumlar kayıt tarihine aittir; güncel test sonucu değildir.

# Skill ve agent durumu

Güncelleme: 2026-10-09. Bu dosya durum kaydıdır; tek başına skill kurmaz veya agent başlatmaz. Depo kuralları: [AGENTS.md](../AGENTS.md).

| Alan | Durum | Kanıt / kullanım |
| --- | --- | --- |
| Ana geliştirme agent'ı | Tek Codex; iş 06 ve 06.1 kişi geçmişi/onaylı taşınma kapsamı tamamlandı | Ortak Property/Building/Unit modeli, migration, CRUD/rol/üyelik, shadcn/TanStack arayüz, belgeler ve TESTING.md'deki gerçek test kayıtları |
| Alt agent / paralel delegasyon | Kullanılmadı | Kullanıcı bu çalışma için paralel agent istemedi |
| Imagegen | Önceki tasarım seçiminde kullanıldı | Login seçenekleri üretildi; kullanıcı 03 çizilmiş binalı iki sütunlu görünümü seçti |
| Giriş illüstrasyonu | Kodla özgün SVG olarak tamamlandı | deploy/keycloak/themes/buildingflow/login/resources/img/buildings.svg; stock resim/font yok |
| Browser kontrolü | Doğrulama için kullanıldı | 03 apartman/doğrudan daire ve karma yapı/ofis; iki sekme 409 ve güncel kayıt; mobil form, detay/OIDC dönüşü, test kayıtlarını soft-delete |
| Projeye özel tekrar kullanılabilir skill | Henüz oluşturulmadı | Kullanıcıyla kalıplar sonraki adımda oluşturulacak; mevcut dosya SKILL.md değildir |
| Test rehberi | Güncellendi | TESTING.md; 171 .NET testi (28 unit + 143 integration), 3 frontend testi / 0 başarısız/atlanan, 17 Keycloak kontrolü, servis smoke ve tarayıcı kabulü |
| Lisans kontrolü | Çalıştırıldı | 262 paket sürümü; scripts/check-licenses.py, licenses.md ve package-inventory.json |
| 02 kullanılan hazır skill | Kullanılmadı | Resmî shadcn/TanStack kaynakları ve dokümanları okundu; harici skill veya alt agent çalıştırılmadı |
| 03 kullanılan hazır skill | Kullanılmadı | Yerel Next.js rehberleri okundu; mevcut bileşenler genişletildi, ek skill/alt agent çağrılmadı |
| Frontend Convention | AGENTS.md üzerinden zorunlu okuma | docs/frontend-rules.md; kullanıcının 17 başlığı, mevcut/planlı altyapı ayrımı |
| Frontend kural belgesi turunda skill | OpenAI Docs kullanıldı | Resmi AGENTS.md kapsam rehberi okundu; proje skill'i veya alt agent oluşturulmadı |

Sonraki her turda görevi, kullanılan skill/agent'ı, değişen dosyaları ve gerçek test sonucunu kaydet. Yeni bir agent veya skill gerektiğinde kapsam ve sorumlulukları somut olarak tanımla; isimden ibaret sahte agent listesi oluşturma.

2026-10-09 / Çalışma ve ilerleme protokolü: **tek Codex agent**; alt agent kullanılmadı. OpenAI Docs skill'i ile [resmî AGENTS.md rehberi](https://learn.chatgpt.com/docs/agent-configuration/agents-md) okundu. Mevcut kurallar korunarak kök AGENTS'ye zorunlu Türkçe terminal/çalışma mesajları, faz/adım/durum/yüzde, gerçek test/hata/düzeltme ve erişilebilir kota raporu eklendi; UI planındaki rapor maddesi bağlandı. [STATUS](progress/STATUS.md), [CHANGELOG](progress/CHANGELOG.md) ve [SESSION](progress/SESSION.md), mevcut milestones/TESTING/UI planının bağlı özetleri olarak hazırlandı; yeni bağımsız backlog/skill/agent/runtime servis eklenmedi. Codex kullanım aracı hesap pencerelerini sağladı; giriş/çıkış/toplam token ve kalan context ölçümleri **BİLGİ MEVCUT DEĞİL**. **84 yerel bağlantı, eski kurallar, gerçek faz/adım sayaçları, 315 dosya hash koruması/yedi izinli Markdown farkı ve whitespace** doğrulandı; tur 3/3 tamamlandı. Bu tur uygulama testleri yeniden çalıştırılmadı; uygulama kodu/DB/API/bağımlılık/.env değiştirilmedi. Yeni geliştirme fazı onayı bekleniyor.

2026-10-08 / Kalıcı Daireler ve Kişiler menüleri: tek Codex agent; ek skill/alt agent kullanılmadı. Kök/frontend AGENTS, Convention ve yerel Next.js sayfa/route-group rehberi okundu. WorkspaceShell alan seçilmeden de modülleri gösterir; yeni `/units` ve `/people` ortak PropertiesWorkspace listesini alan seçimi için tekrar kullanır. OIDC whitelist/test, eş TR/EN anahtarlar, README/core/Convention ve mevcut roadmap görevi güncellendi. 11 frontend testi, typecheck/build, 312 sürüm lisans metadata ve whitespace kontrolü geçti; paket manifest/lockfile SHA-256 değişmedi. CUA ana liste/genel bakış, iki modülün alan seçimi, apartmanda doğrudan 48 daire, aktif menü, TR/EN/yenileme ve 390 px taşmasız menü kabulünü doğruladı. Malik/Kiracı/Sakin seçenekleri ve varsayılan Malik gerçek formda görüldü; Vazgeç ile çıkıldı. Anonim people HTML'de incelenen property/person kimlikleri yok. Mevcut kullanıcı verisi/.env değiştirilmedi; şema/backend/Keycloak/Docker testleri bu tur yeniden çalıştırılmadı. Kanıtlar ve sonuçlar TESTING.md içinde.

2026-10-07 zorunlu tablo alanları: tek agent ile AGENTS.md ve ana mimari dokümanı güncellendi; tüm mapped entity ve gerçek PostgreSQL tablolarını tarayan BaseEntity sözleşmesi kontrolü eklendi. Yeni skill/alt agent kullanılmadı.

2026-10-07 migration yaşam döngüsü: tek agent, ek skill/alt agent yok. EF Core Migrations için db.sh ve MIGRATIONS.md, VS Code görevleri ve 3 yeni test eklendi. Gerçek PostgreSQL ilk kurulum/yükseltme/tekrar uygulama ile 60 test geçti; SQL artifact üretildi, DB'ye uygulanmadı. Yeni paket eklenmedi; lisans kontrolü 262 sürüm için geçti.

2026-10-08 / 03 teslimi: tek Codex agent; yeni skill veya alt agent kullanılmadı. İlk hiyerarşi ortak Property/Building/Unit modeline taşındı; apartmanda blok olmadan daire, diğer türlerde bina/blok ve altı bölüm türü destekleniyor. Yeni migration önceki tabloları rename/backfill etti; mevcut yerel 6 satırın verisi/audit/version'ı korundu. 121 backend testi (17 unit + 104 integration), 0 başarısız/atlanan; 17 Keycloak kontrolü, servis smoke, frontend build/typecheck ve 262 sürüm lisans kontrolü geçti. Tarayıcıda masaüstü/mobil, doğrudan daire, karma yapı/ofis ve iki oturum 409 kabulü tamamlandı. Geçici UI kayıtları soft-delete edildi; mevcut kullanıcı/veri/parola korundu. Yeni bağımlılık/harici asset eklenmedi. Kanıtlar ve kalan işler TESTING.md içinde.

2026-10-08 / İlişkisiz tür düzenleme: tek Codex agent, ek skill/alt agent yok. Boş yönetim alanında tür seçimi açıldı; bina/bağımsız bölüm geçmişi backend ve UI tarafından engellenir. canChangeType veri erişim kapsamını bozmayan ayrı geçmiş sorgusundan hesaplanır; parent version yarışı korunur. 146 test (21 unit + 125 integration), 0 başarısız/atlanan; typecheck/build ve 262 paket lisans kontrolü geçti. Tarayıcı Site → Apartman → Site ve aktif/silinmiş ilişki kilidini doğruladı. Test verileri soft-delete edildi; mevcut kullanıcı verisi değişmedi. Şema/paket değişmedi.

2026-10-08 / Frontend kuralları: tek Codex agent; OpenAI Docs skill'i kullanılarak resmi AGENTS.md kapsam rehberi okundu. Projeye özel skill veya alt agent oluşturulmadı. docs/frontend-rules.md içindeki 17 standart, kök/frontend AGENTS, mimari kaynak ve README üzerinden bağlandı; TESTING.md doğrulama kaydı tamamlandı. 17 başlık, 35 yerel bağlantı, Next.js yönerge bloğunun byte düzeyinde korunması, package.json/lockfile değişmezliği ve whitespace kontrolü geçti. 262 paket sürümünün lisans metadata kontrolü tekrar geçti. Uygulama/DB kodu değiştirilmedi; uygulama testleri bu tur yeniden çalıştırılmadı.


2026-10-08 / 04 kişiler ve daire ilişkileri: tek Codex agent; ek skill/alt agent kullanılmadı. Kök/frontend AGENTS ve Convention okundu. Property-scoped Person/UnitOccupancy, manager-only kişisel veri, inclusive tarih/primary/overlap, parent concurrency, geçmişi koruyan bitiş/silme kuralları eklendi. Yeni migration ve snapshot; gerçek veri bulunan Down reddi test edildi. Mevcut EntityEditor/API/shadcn/TanStack kaynakları genişletildi; ortak RecordList desktop tablo/mobil kart, date-only formatter ve OIDC people dönüş whitelist'i eklendi. 171 .NET (28+143), 3 frontend, 17 Keycloak kontrolü, servis smoke, typecheck/build ve 262 lisans metadata kontrolü geçti. CUA tarayıcıda kişi/ilişki/bitiş, gerçek error/retry, route yenileme, anonim SSR ve mobil kabul için kullanıldı. Tarih input düzeltmesi tekrar test edildi. Yeni bağımlılık/harici asset yok; lockfile tur başlangıcıyla aynı. Ana migration uygulandı; host FE/API güncel kodla çalışıyor. Yalnızca kendi ID+ad ile sınırlı UI fixture kayıtları soft-delete ile temizlendi. Yerel veri parmak izi sınırlaması ve gerçek kalan işler TESTING.md'de açıklandı.

2026-10-08 / Mesajlar → TR/EN → ayrı yönetim menüleri: tek Codex agent, ek skill veya alt agent kullanılmadı. Kök/frontend AGENTS ve Convention ile yerel Next.js rehberleri okundu; next-intl resmî rehber/lisansı incelendi. MessageBanner/FieldError/NotificationProvider, doğrulanmış locale cookie, TR/EN sözlükler/formatter, güvenli ProblemDetails eşlemesi ve PropertyScope ile ayrı sayfalar uygulandı. CUA gerçek login/locale, alan hatası/focus/ikon, success/409/error/retry/info, site/bina/bölüm/kişi/ilişki ayrı adresleri, TR/EN tarihler ve 390 px mobil kabul için kullanıldı. **11 frontend testi**, typecheck/build, **17 Keycloak kontrolü**, servis smoke, frontend Docker imajı ve LICENSE/NOTICE kopyalama, anonim units HTML kontrolü ve **312 paket sürümü lisans kontrolü** geçti. Önceki 171 backend test sonucu bu tur yeniden çalıştırılmış sayılmaz. Şema/migration/.env/mevcut hesap parolası değiştirilmedi. Yalnızca oluşturulan kurgusal fixture kayıtları API soft-delete ile temizlendi; geçmiş ilişki saklandı. Gerçek sınırlar ve ekranlar TESTING.md'de.

2026-10-08 / 05 aidat ve borçlandırma: tek Codex agent; bu kodlamada ek skill/alt agent kullanılmadı. Kök/frontend AGENTS, Convention ve kurulu Next.js route/Server-Client rehberleri okundu. Dues domain/service/store/endpoints, iki yeni tablo/migration/snapshot, row-lock/idempotency/partial unique/composite FK, açık membership/history scope ve gerekçeli iptal eklendi. Mevcut API/shadcn Dialog/TanStack RecordList/Property seçim/feedback/formatter altyapısı genişletildi; ayrı Aidat menüsü, TR/EN tahakkuk/liste/filtre/özet/iptal ekranı. **196 backend + 18 frontend test**, 0 başarısız/atlanan; typecheck/build, 17 Keycloak, servis smoke ve 312 lisans metadata kontrolü geçti. Tarayıcıda menü/seçim, alan hatası/ilk odak, iki bölüm 24.70 TRY, tarih/sayfalar arasında seçim, TR/EN, 390 px mobil ve rota yenilemesi doğrulandı. Tarayıcı mutation’ı ana kullanıcı DB’sinde çalıştırılmadı; gerçek create/cancel/yetki/concurrency ayrı PostgreSQL test şemalarında doğrulandı. Yerel migration sonrası 80 eski satır özeti, .env ve npm manifest/lockfile aynı; hesap parolası korunur. Yeni bağımlılık/harici asset yok. 05 roadmap koyu/tikli tamamlandı; 06 planlı. Ayrıntılar TESTING.md, api/dues.md ve MIGRATIONS.md.

2026-10-08 / API port çakışması: tek agent, ek skill/alt agent yok. Kullanıcı hata çıktısı 5005 bind/address-in-use olarak teşhis edildi. Önceki doğrulama API’si kapatıldı; dev.sh port preflight, yalnız workspace apphost’unu durduran api-stop ve VS Code Stop BE görevi/README eklendi. Bash sözdizimi, yabancı süreç reddi, dolu portta anlaşılır uyarı, gerçek aynı başlatma komutu + PostgreSQL health 200 ve tekrarlı stop geçti. Teslimde API kapalı/5005 boş, kullanıcı komutu hazırdır. İş kodu, DB, frontend, .env/parola veya bağımlılık değiştirilmedi. Önceki business testleri tekrar çalıştırılmadı; lisans/hash/whitespace kontrolü yapıldı.

2026-10-09 / UI Aşama 06: tek Codex agent; alt agent veya yeni skill kullanılmadı. Kök/frontend AGENTS, Convention, görsel plan ve kurulu Next.js Server/Client rehberi okundu. Daire/Kişiler referansları CUA/görsel incelemeyle değerlendirildi; referans fotoğrafı/fontu/kişisel alanları kopyalanmadı. Mevcut shadcn/TanStack/API/EntityEditor genişletildi: ortak sunum, mobil kart/kompakt tablo, sağ detay/düzenleme paneli ve TR/EN. CUA mevcut kullanıcı verisini yalnızca okuyarak detay/ilişki/alan hatası/iptal/odak/date/404/mobil kabul için kullanıldı; viewport kaldırıldı ve Türkçe korundu. 23 frontend testi, typecheck/build, anonim SSR, API health 200, 312 lisans metadata ve hash kontrolleri geçti. Backend/DB/auth sözleşmesi ve .env/npm dosyaları değişmedi; önceki .NET/Keycloak testleri yeniden çalıştırılmış sayılmaz. Genel bakışta ayrı UI06 kanıt kaydı; iş fazı 06 ödemeler planlıdır. Tam kapsam ve gerçek sınırlar TESTING.md ile analysis/UI_STAGE06_REPORT.md'de.

2026-10-09 / Ortak sağ panel: tek Codex agent; ek skill/alt agent kullanılmadı. ManagementPanel mevcut shadcn DialogContent üzerinde bütün kayıt form/detaylarında ortak sağ yön, genişlik, başlık ve alt eylem kullanır. Kapanış odağı her açılışta yakalanır; kalıcı oluşturma formu odağı düzeltildi. CUA yönetim alanı oluşturma/düzenleme, daire, tahakkuk, boş alan hataları, seçim/tarih/önizleme, iptal/Escape/odak ve TR/EN/390 px mobil kabulünü doğruladı. 23 frontend testi, typecheck/build ve 312 sürüm lisans kontrolü geçti; yeni paket/asset yok. Canlı bina/borç detayı için mevcut kayıt yok; bu sınır TESTING.md’de kayıtlı. Kullanıcı verisi/.env/manifest/lockfile korundu; backend/DB değişmedi.

2026-10-09 / İş Fazı 06 ödeme ve borç kapatma: **tek Codex agent**; ek skill/alt agent kullanılmadı. Kök/frontend AGENTS, Convention, migration/dues/roadmap ve kurulu Next.js rehberi okundu. Domain/Application/Infrastructure/API, PaymentsAndAllocations migration/snapshot, ortak sağ Tahsilatlar form/detay ve borç geçmişi, TR/EN, merkezi API/shadcn/TanStack/feedback/formatter genişletildi. **221 .NET (44 unit + 177 gerçek PostgreSQL integration), 31 frontend test** geçti; yeni paket/asset yok, **312 sürüm lisans metadata** kontrolü geçti. CUA form doğrulama/alan seçimi/odak/tarih/TR–EN/responsive için kullanıldı; kullanıcı DB'sine ödeme/iptal mutation yapılmadı. Yerel migration uygulandı ve yalnız workspace API'si yeniden başlatıldı. .env/npm/önceki migration hashleri korundu; gerçek manuel kapsam ve final build kanıtı TESTING.md'de kaydedilir. İş 06 ile UI06 ayrı kayıtlar; online sağlayıcı/gerçek banka iadesi/fiskal belge uygulanmadı.

2026-10-09 / Toplu tahakkuk seçimi: **tek Codex agent**, ek skill/alt agent kullanılmadı. Kök/frontend AGENTS, Convention ve kurulu Next.js use-client rehberi okundu. Mevcut AssessmentEditor/shadcn sağ panel, merkezi yetkili API ve TR/EN sözlükler genişletildi; çok sayfalı tüm sonuçlar, sayfayı seç/çıkar, seçim temizle/tek çıkar, tekrarsız/atomik 500 sınırı, iptal ve hata koruması eklendi. Dues/Payments TanStack render callback/kolonları sabitlenerek borçtan ödeme paneli kapanış odağı korundu. **36/36 frontend testi, typecheck/build, 312 sürüm lisans metadata ve whitespace kontrolü** geçti; 13 korunan .env/npm/önceki migration hash'i değişmedi. CUA 48 × 1.250 = 60.000 TRY, sayfa/tek çıkarma, filtreler, TR/EN/390 px, tarih ve odak kabulünü kullanıcı DB'sine tahakkuk/ödeme kaydetmeden doğruladı. Mevcut 40 borç ve daire 10'un borç → ödeme/kısmi tutar önizlemesi yalnız okundu; temizlenmedi. İş 05'e doğrulanmış toplu seçim görevi eklendi; iş 06 dört görevi tamamlanmış kalır. Bu UX ilavesinde API/DB/auth veya dependency değişmedi; önceki 221 backend test yeniden çalıştırılmış sayılmaz. Ayrıntılar ve ekran kanıtları TESTING.md'de.

2026-10-09 / FE yeniden başlatmada sekme kullanımı: tek Codex agent; yeni skill/alt agent yok. Dev başlatıcısındaki her Ready sonrası OS open çağrısı teşhis edildi; AuthGate aynı sekmedeki Keycloak akışını kullanıyor. macOS Chrome/Safari için mevcut frontend veya aynı client/redirect OIDC sekmesini bulup URL'sini koruyan native JXA adapter eklendi; bulunmazsa normal açıcı, izin/hata/timeout durumunda kopya açmayan uyarı; false/always kontrolleri. **44 frontend testi (8 yeni), typecheck/build, native JXA derleme, 312 lisans metadata, whitespace ve korunan dosya hashleri** geçti. CUA yalnız açık tarayıcı/sekme envanterini okudu; native otomasyon çalıştırılmadı, OS izni değiştirilmedi ve canlı GUI tekrar kullanım kabulü tamamlanmış sayılmadı. API/auth/DB/paket değişikliği yok; kullanıcı FE süreci ve giriş formu korundu. README ve TESTING.md kapsam/ilk izin/diğer tarayıcı sınırlarını açıklar.

2026-10-09 / **Faz 06.1 tahakkuk kişi geçmişi ve onaylı taşınma:** tek Codex agent; yeni skill/alt agent kullanılmadı. Kök/frontend AGENTS, Convention, dues/people/migration ve kurulu Next.js use-client rehberi okundu. Domain/Application/EF/API + yeni migration/snapshot; borç listesi/kişi combo/rol filtresi/sağ detay, tahakkuk muhatabı ve iptal nedenleri; ilişki bitişi önizleme ve varsayılan kapalı açık onay TR/EN ile kodlandı. **243 .NET testi (47 unit + 196 gerçek PostgreSQL), 51 frontend testi, typecheck/build, 312 lisans metadata** kontrolü geçti. Yerel migration ve API yeniden başlatma, 10 eski tablo veri hash'i ve 15 korunan dosya hash'i doğrulandı. Kullanıcı girişinden sonra CUA kişi kolonu/legacy uyarısı/rol filtresi, beş neden/Diğer açıklama hatası ve odak, EN/TR tahakkuk muhatabı, kiracı bitişinde sıfır uygun borç önizlemesi, **390 px taşmasız mobil panel ve Escape/odak** kabulünü kaydetmeden doğruladı; ekran kanıtları saklandı, viewport kaldırıldı ve Türkçe korundu. Canlı kullanıcı verisinde dolu snapshot/onay adayı olmadığından bunların başarılı kayıt davranışı ayrı PostgreSQL/DTO testleriyle doğrulandı. Kullanıcı verisine tahakkuk/ödeme/iptal/bitiş mutation'ı yapılmadı. Detay ve sınırlar TESTING.md ve analysis/PHASE_06_1_DUES_PARTIES.md'de; yol haritasında ayrı 06.1 dört görev tamamlandı, Faz 07 başlamadı.

## 09–10.10.2026 — İŞ 07.1 merkezi API hata / uyarı altyapısı

**Tek Codex agent; alt agent veya yeni skill kullanılmadı.** Kök/frontend AGENTS, frontend Convention, UI planındaki MASTER_PROMPT/PHASES ve kurulu Next.js/Radix yerel rehber/kaynakları incelendi. Yeni bağımlılık kurulmadı; mevcut Radix focus-scope/dismissable-layer branch API'leri ortak Dialog/bildirim entegrasyonunda kullanıldı. Lisans 312 paket sürümünde PASS; 19 korunan dosya aynı. 252 .NET/59 frontend, typecheck/build ve salt-okunur TR/EN/390 px tarayıcı kontrolüyle 07.1 5/5 tamamlandı; ayrıntılı kanıt TESTING.md. Kullanıcının protokolü terminal/çalışma mesajları ve mevcut progress dosyalarından sürdürüldü; bir sonraki faz açılmadı.

## 10.10.2026 — İŞ 07 Gider ve Bütçe

**Tek Codex agent; yeni skill/alt agent kullanılmadı.** Kök/frontend AGENTS, frontend Convention, UI planındaki MASTER_PROMPT/PHASES ve kurulu Next.js/Radix kaynakları incelendi. Domain/Application/EF/API üç yeni tablo/migration, kapsamlı finans rol/scope/kilit/version/UUID ve revizyon geçmişi; ayrı menü/alan seçimi, TR/EN sağ ManagementPanel, RecordList/TanStack, kesin BigInt formatter ve yedi merkezi hata kodu eklendi. Yeni paket/asset/ücretli ürün yok; 312 lisans metadata PASS. **292 .NET /67 FE, typecheck/build, EF model/SQL, 12 eski DB tablosu ve18 korunan dosya** kontrolleri geçti. Mevcut parola/.env/kullanıcı verisi korundu, yeni yerel migration/API uygulandı. CUA TR/EN/1280 sağ512px/390 mobil panel, ilk alan odağı/değerler/sıfır plan/bildirim kapatma kabulünü kaydetmeden doğruladı. Dolu finans kayıt davranışı ayrı gerçek PostgreSQL testlerindedir; ana kullanıcı DB'sinde başarılı finans mutation yapılmadı. Resmi dört görev ve TESTING/progress/roadmap aynı teslim kaydına bağlandı; İŞ08/UI07 başlamadı.


## 10.10.2026 — İŞ 08 Duyurular ve dosya yönetimi

**Tek Codex agent; yeni skill/alt agent kullanılmadı.** Kök/frontend AGENTS, Convention, mimari/test/progress ve UI planı önce incelendi. Announcement/StoredDocument, özel MinIO sağlayıcısı, EF migration ve kapsamlı yetki/üyelik/version/UUID/ek sınırı; bağımsız menü, TR/EN sağ panel ve ortak multipart/Blob API katmanı geliştirildi. Yeni SDK Minio7.0.0 resmi Apache2 lisansı kaydedildi;316 paket metadata PASS,19 korunan dosya değişmez.

**319 .NET /76 FE, son typecheck/build; ayrı PostgreSQL ve özel geçici MinIO testleri geçti.** CUA TR512px/EN390px panel/boş form bildirimi kaydetmeden doğrulandı; Türkçe/viewport geri yüklendi ve geçici sekme kapandı. Yerel migration ve API güncellendi; ilk canlı before-hash eksikliği ile sonraki tekrar-migration15 tablo eşliği ayrı kanıt olarak bildirildi. TESTING/progress/roadmap aynı dört göreve bağlandı. İş09 başlatılmadı; Docker imajı ve dış sağlayıcı smoke bu tur çalıştırılmış gibi raporlanmadı.

2026-10-11 / İŞ10: **tek Codex agent; ek skill/alt agent kullanılmadı**. Kök/frontend AGENTS, Convention ve gerçek UI planı okundu; sayaçlar/raporlar + Prometheus/Grafana/Loki/OTLP geliştirildi. **397 .NET,82 FE,typecheck/build,11 canlı pipeline,331 lisans ve23 korunan dosya** doğrulandı. TR/EN/512px sağ panel/390px mobil kabulü salt-okunur Chrome'da; [ekran kanıtı](screenshots/phase10-meter-panel-tr.png). Mevcut backend/frontend ortak bileşenleri ve lisanslı resmi OpenTelemetry SDK kullanıldı; sonraki İŞ11 başlatılmadı. [Detay](TESTING.md).


2026-10-15 / Merkez Talepler ve Arızalar: tek Codex agent; ek skill veya alt agent kullanılmadı. Kök/frontend/mobil AGENTS,Convention,planlar,Next.js kurulu use-client rehberi ve mevcut API/test kalıpları okundu. RequestManagement store/DTO/endpoint ve migration; ortak menü/RecordList/ManagementPanel/authorizedAPI/TR–EN; mobil ortak durum/yanıt.159PG+S3/112FE/92mobil,build/typecheck/lint/export/329lisans PASS; gerçek UI/cihaz kabulü yok. GitHub#5Project3,ayrıntı TESTING ve api/request-management.md.


16.10.2026 / PERSON FAZ03: tekagent; yerel akadev-phase/contract/verify/security yönergeleri kullanıldı. Yeni üçüncü taraf paket/skill/altagent yok.215izoleintegration+150unit+112FE,typecheck/build/329lisansmetadata;gerçek rol/tenant/issuer/self/version/replay/migration kanıtı TESTING ve PersonFAZ03raporunda. Featuredefaultoff,gerçektenantseed/backfill yok;FAZ04 başlamadı.


### PERSON FAZ04 —17.10.2026
Yerel `.claude/skills/akadev-phase/SKILL.md`, `akadev-contract/SKILL.md`, `akadev-verify/SKILL.md`, `akadev-security/SKILL.md` okundu/kullanıldı; tek agent, yeni skill/plugin/paket yok. Faz ve test kanıtı [phase-04-report.md](refactor/person/phase-04-report.md). Kullanıcının Altunbayrak Rezidans cevabıyla D01 önkoşulu çözüldü; önceki ara raporun engeli güncel teslimde geçerli değildir.


### PERSON FAZ05 —17.10.2026
Yerel akadev-phase, akadev-contract, akadev-verify, akadev-security SKILL.md kaynakları okunup kullanıldı; tek agent. Görev kapsamı ve mevcut rol sınırı, negatif JWT/tenant/Property testleri, gerçek ayrı PostgreSQL migration/history/rollback ve lisans metadata doğrulaması [FAZ05 raporunda](refactor/person/phase-05-report.md). Yeni skill/plugin/paket eklenmedi.

### 18.10.2026 — PERSON FAZ06
Yerel akadev-phase/contract/verify/security yönergeleri mevcut yetkili kişi merkezi kapsamına uygulandı. Tek agent; yeni araç/skill/paket kurulumu yok. Kanıt: person phase06 raporu ve TESTING. Otomatik güvenlik taraması veya gerçek UI kabulü yapıldığı iddia edilmedi.

### 18.10.2026 — PERSON FAZ07
Mevcut yerel akadev phase/contract/security/verify yönergeleri yetkili merkez giriş/context kapsamına uygulandı; tek agent ve mevcut araçlar. Yeni skill/agent/paket kurulmadı. Yetki incelemesinde finans içi kişi görünürlüğü Property göreviyle daraltıldı; gerçek19 PG senaryosu ve geniş348 regresyon kanıtı phase07 raporunda. Otomatik scanner veya gerçek UI kabulü yapılmış sayılmadı.

### 18.10.2026 — PERSON FAZ08
Mevcut yerel akadev phase/contract/security/verify yönergeleri tek agent ile uygulandı; yeni skill/plugin/paket yok. Expo57 resmî SQLCipher/SecureStore belgeleri ve kurulu API tipleri kontrol edildi. Minimum readonly offline yetki ve native kabul sınırı [FAZ08 raporunda](refactor/person/phase-08-report.md); port testleri gerçek cihaz biyometrisi kanıtı sayılmadı.354integration+187unit+119mobil,lint/typecheck/export kanıtları TESTING kaydındadır.

### 18.10.2026 — PERSON FAZ09
Mevcut yerel akadev-phase/contract/security/verify yönergeleri canonical mobil talep/profil sync kapsamına tek agent ile uygulandı. Yeni skill/plugin/altagent/paket yok. Expo57 SQLite/ImagePicker resmî kaynakları kontrol edildi; önceki outbox/receipt/şifreli dosya yeniden kullanıldı.363integration+187unit+126mobil ve gerçek cihazda doğrulanmayanlar [FAZ09 raporunda](refactor/person/phase-09-report.md). Otomatik scanner veya native cihaz kabulü yapılmış sayılmadı.

### 18.10.2026 — PERSON FAZ10
Yerel akadev-phase/security/license/verify yönergeleri tek agent ile kullanıldı; yeni agent/plugin/skill/araç kurulmadı. SCA gerçek npm/NuGet kaynaklarında çalıştı; mobil29advisory kaydı ile kapı başarısızdır. Fortify ve tam secret scanner kurulu olmadığından kullanılmış sayılmaz. Sınırlı kaynak/env kopya kontrolü trackedgeneratedconfig buldu; disk korunarak indeks temizlendi, CI özelconfig ve mobilhighaudit kapıları eklendi. Native MASVS/SAST/history/SBOM/dağıtım eksikleri ve çalıştırılacak komutlar [FAZ10 raporunda](refactor/person/phase-10-report.md). Gerçek sır değerleri log/rapor/GitHub'a taşınmadı.

## PERSON FAZ11 —18.10.2026

Mevcut akadev-phase, akadev-verify ve akadev-docs prosedürleri kullanıldı. Tek agent; yeni Skill/MCP/paket kurulmadı. 366 integration,149 web,126 mobil ve fixture backup8kontrol; kapsam/sınırlar [raporda](refactor/person/phase-11-report.md).

### 18.10.2026 — WEBSITE FAZ 04 kullanım kaydı

Tek agent; akadev-phase/contract/verify/license/docs ve imagegen kullanıldı. Kanıt: TESTING.md WEBSITE FAZ 04; görsel prompt/kaynakları website/public/images/README.md. Yeni agent/MCP/paket kurulmadı.

### 18.10.2026 — WEBSITE FAZ05

Tek agent; akadev-phase/contract/security/verify/license/docs kullanıldı. Model/migration ve gerçek PG/storage fixture, ortak UI ve izole tarayıcı kanıtı TESTING.md içinde. Yeni MCP/agent/skill/paket kurulmadı.

## 19.10.2026 — WEBSITE FAZ06 kullanımı
Tek agent ile akadev-phase/contract/security/verify/license/docs mevcut skill akışı uygulandı. Kapsam T21.6/T21.10; gerçek kanıt TESTING.md, API docs/api/listings.md. Yeni agent/MCP/globalayar/bağımlılık kurulmadı. Gerçek EİDS/hukuki kabul açıkken roadmapdone işaretlenmedi.

## 19.10.2026 — WEBSITE FAZ07

Tek agent; mevcut akadev-phase/contract/security/verify/license/docs kullanıldı. Domain/preview sözleşmesi docs/api/website-domains.md, kanıt TESTING.md. Yeni agent/MCP/skill/paket kurulmadı. Canlı DNS/TLS kabulü yapılmadı.

## 19.10.2026 — WEBSITE FAZ08

Tek agent; mevcut akadev-phase/contract/security/verify/docs/license prosedürleri kullanıldı. Yeni agent/MCP/skill/paket kurulmadı. Kanıt TESTING.md; sözleşme docs/api/website-login.md. Canlı Keycloak kabulü yapılmadı.

## 19.10.2026 — BAKIM tek website girişi

Tek agent; phase/contract/security/verify/docs/license mevcut prosedürleri. Yeni bağımlılık/agent/MCP yok. Kanıt TESTING.md; gerçek Keycloak yalnız giriş ekranına kadar doğrulandı.

## 19.10.2026 — WEBSITE FAZ09

Tek agent; mevcut phase/contract/security/verify/license/docs prosedürleri. Docker/CI/SBOM/API sınırları ve yerel fixturekanıtı TESTING.md/phase09raporunda. Kullanıcı GitHub proje3 issuegövde/durum güncellemesini açıkça yetkilendirdi; #30açık kabul. Yeni agent/MCP/skill/paket kurulmadı.

## 19.10.2026 — SIDEBAR FAZ07.1 Aşama02

Tek agent ile mevcut akadev-phase/contract/verify/docs/license uygulandı;167/167 FE,typecheck/build ve kaynak/dependency koruması kanıtı [teslimde](navigation-refactor/STAGE_02.md). Yeni skill/agent/MCP veya global ayar kurulmadı.

## SIDEBAR FAZ07.1 Aşama03 —19.10.2026

Tek agent mevcut phase/verify/docs prosedürlerini uyguladı; bağımlılık koruması kontrol edildi. Yeni skill/MCP/agent kurulmadı. [Test ve gerçekChrome kanıtı](navigation-refactor/STAGE_03.md).

## SIDEBAR FAZ07.1 Aşama04 —20.10.2026

Tek agent mevcut phase/verify/docs prosedürlerini uyguladı; dependency/source koruması kontrol edildi. Yeni skill/MCP/agent yok. [Otomatik kanıt ve açık runtime sınırı](navigation-refactor/STAGE_04.md).

## SIDEBAR FAZ07.1 Aşama05 —20.10.2026

Tek agent phase/verify/docs; mevcut bağımlılık/kaynak koruması. Yeni skill/agent/MCP yok. [Fixture ve otomatik kanıt](navigation-refactor/STAGE_05.md).

BAKIM20.10.2026 menüfont eşliği: tekagent verify/docs, typecheck/build ve computedCSS kontrolü; yeniSkill/paket yok.

Aşama06 SIDEBAR20.10.2026: tekagent phase/verify/docs/security;177FE ve fixture kanıtı [teslimde](navigation-refactor/STAGE_06.md). YeniSkill/MCP yok.


## SIDEBAR FAZ07.1 Aşama07–08 —20.10.2026

Aşama07 tek agent phase/verify/security/docs;177FE,277unit,83izoleintegration ve fixture kanıtı [raporda](navigation-refactor/STAGE_07.md). Aşama08 tek agent phase/docs/license; yalnız belge/bağlantı/sayaç/kaynak koruması, uygulama testleri tekrarlanmadı. Yeni skill/agent/MCP/paket yok. [Teslim](navigation-refactor/IMPLEMENTATION.md).

20.10.2026 BAKIM sağ üst profil: tekagent mevcut verify/docs;177FE,typecheck/build ve1440pxfixture görünürlük/menü kabulü. YeniSkill/MCP/paket yok.

20.10.2026 BAKIM Dashboard kompaktKPI: tekagent mevcut verify/docs;typecheck/build ve temsili markup/gerçekCSS1440/375px kabulü. YeniSkill/MCP/paket yok.


20.10.2026 BAKIM Dashboard fotoğraflıhero/headeralan:tekagent mevcut contract/security/verify/docs/license yönergeleri;182FE,23izolePGstorageintegration,typecheck/build ve gerçekReactfixture kanıtı[raporda](navigation-refactor/DASHBOARD_HERO.md). imagegen skill okundu;mevcut özgünkonutasset yeterli olduğundan yeni görsel üretimi yapılmadı. Yeniagent/MCP/skill/paket yok.

20.10.2026 BAKIMgelirgiderikilisütun:tekagent mevcutverify/docs;184FE,typecheck/build,1440/320Reactfixture. YeniSkill/MCP/paket yok.

21.10.2026 BAKIM sidebaraltalan kaldırma:tekagent mevcutverify/docs;184FE,typecheck/build ve indeks/dependencykoruması. YeniSkill/MCP/paket yok.

21.10.2026 BAKIM referanssidebar:tekagent mevcutverify/docs;186FE,typecheck/build,192modelkombinasyonu ve desktop/rail/mobilefixture. YeniSkill/agent/MCP/paket yok. [Kanıt](navigation-refactor/SIDEBAR_REFERENCE.md).

21.10.2026 BAKIM doluluk halkası: tek agent mevcut verify/docs;188FE,typecheck/build,iki alan ve320px gerçek React fixture kabulü. Yeni skill/MCP/paket yok.

21.10.2026 Dashboard hava şehri BAKIM: tek agent; akadev-verify/docs yönergeleri ve akadev-license kapsamında resmî sağlayıcı koşulları incelendi. Yeni servis/paket/agent kurulmadı. [Kabul kaydı](TESTING.md), [seçenekler](navigation-refactor/DASHBOARD_WEATHER.md).

21.10.2026 MET Norway BAKIM: tek agent, akadev-contract/security/license/verify/docs yönergeleriyle kullanıcı onaylı hava servisi tamamlandı.195/195 FE ve11/11 backend; [teslim/kanıt](navigation-refactor/DASHBOARD_WEATHER.md). Yeni paket/agent/MCP yok.

21.10.2026 tema BAKIM: tek agent, mevcut verify/docs ve güvenlik yönergeleriyle nonce/ilk boya/enum tercih kontrolü. Yeni agent/MCP/paket kurulmadı. [Kabul](TESTING.md).

22.10.2026 kullanıcı fotoğrafı bakımında mevcut akadev-contract/security/verify/docs prosedürleri uygulandı; yeni Agent/Skill/MCP kurulmadı. Yetki, ortak mobil/web sözleşmesi ve gerçek doğrulama [kanıtı](navigation-refactor/USER_PHOTO_EVIDENCE.md).

22.10.2026 oval menü bakımında mevcut akadev-verify/docs kullanıldı; tek agent, yeni paket/Agent/MCP kurulmadı. [Kanıt](navigation-refactor/ROUNDED_MENUS.md).

22.10.2026 hesap iletişim bakımında mevcut akadev-contract/security/verify/docs uygulandı; tek agent,yeni kurulum yok. [Kanıt](navigation-refactor/ACCOUNT_CONTACT.md).


## 22.10.2026 — BF-VERIFY FAZ 00 prompt/görev/analiz turu

Tek agent;akadev-phase ile8faz kimliği/3adım/kapanış,akadev-contract ile ortak hesap üretici-tüketici ve uyumluluk planı,akadev-docs ile mevcut TESTING/progress kanıt bağlantıları uygulandı. Belgeye uygun statik doğrulama yapıldı;uygulama/güvenlik taraması veya test sonucu iddia edilmedi. Yeni agent/Skill/MCP kurulmadı;uygulama kodu ve bağımlılık değişmedi. Kanıt:[00-analysis.md](refactor/contact-verification/00-analysis.md).


## 22.10.2026 — BF-VERIFY FAZ 01

Tek agent;akadev-phase/contract/security/verify/docs kapsamında ortak actor/scope ve eski istemci sözleşmesi,gerçek izole PostgreSQL/API/şema testleri ve kanıta dayalı görev kapanışı.Uygulama testleri14+6 geçti;gerçek gönderim/cihaz testi iddia edilmedi.Yeni Skill/agent/MCP/bağımlılık kurulmadı.[Rapor](refactor/contact-verification/01-report.md).


## 22.10.2026 — BF-VERIFY FAZ02

Tek agent; akadev-phase/contract/security/verify/docs ve lisans politikası uygulandı. Mevcut hesap/Keycloak modülü, doğrudan web tüketicisi ve izole IdP testleriyle sınırlandı; yeni Skill/agent/MCP/paket kurulmadı. [Kanıt](refactor/contact-verification/02-report.md).

## 23.10.2026 — BF-VERIFY03

Tek agent;phase/contract/security/verify/license prosedürleri uygulandı. Yalnız SMS/contact modülü ve doğrudan bağımlılıkları incelendi;mevcut makbuz,kilit,şifreleme ve worker tekrar kullanıldı.32izole API/PG/yerelHTTP+16unit geçti;gerçek Netgsm teslimi yapılmadı. Yeni paket/Skill/MCP/agent kurulmadı. [Rapor](refactor/contact-verification/03-report.md).


### BF-VERIFY04 — 23.10.2026

Bu tur akadev-phase/contract/security/verify/docs yönergeleri ortak web iletişim doğrulamasına uygulandı. Mevcut kendi-hesapAPI/IdP/OTP sınırı,3resmiadım ve8faz ayrımı,32API+16unit/250web/12IdP/gerçekbileşenPGkanıtı ve rapor/kayıt eşliği kullanıldı. YeniAgent/MCP/paket kurulmadı;tekAgent. GerçekSMSFAZ03 ve mobilUIFAZ05 ayrı kaldı.

26.10.2026 OA-02: phase/contract/security/license/verify/docs mevcut yerel prosedürleri domain/API/migration tesliminde kullanıldı; tek Agent, yeni kurulum yok. [Kanıt](common-areas/02-report.md).

26.10.2026 OA-05: phase/contract/security/license/verify/docs prosedürleri bakım-rezervasyon-iletişim sözleşmesinde kullanıldı; tek Agent. Yeni agent/MCP/bağımlılık kurulmadı. [Kanıt](common-areas/05-report.md).


27.10.2026 OA-06: akadev-phase/contract/security/license/verify/docs mevcut prosedürleri tek agent ile kullanıldı; yeni skill/MCP/agent kurulmadı. Kanıt [OA06](common-areas/06-report.md).


27.10.2026 OA-07: mevcut akadev-phase/contract/security/license/verify/docs prosedürleri tek agent ile kullanıldı. Yeni agent/MCP/skill kurulmadı; [kanıt](common-areas/07-report.md).


27.10.2026 OTOPARKFAZ05: mevcut akadev-phase/contract/security/verify/docs yönergeleri uygulandı; tekagent. Yeniagent/skill/MCP/paket kurulmadı. Kullanıcı kararıyla sakin yetkisi genişletilmedi. Kanıt[05-report](parking/05-report.md).


27.10.2026 OTOPARKFAZ06: mevcut akadev-phase/contract/security/verify/docs yönergeleri tekagent ile uygulandı. Yeniagent/skill/MCP/paket kurulmadı. Mobil person/context, offline ve scope sözleşmesi yeniden kullanıldı; ayrı ürün fazı/backlog oluşturulmadı. [Kanıt](parking/06-report.md).


28.10.2026 OTOPARK-07: akadev-phase/contract/security/verify/docs mevcut yönergeleri tek agent ile uygulandı. Yeni agent/skill/MCP/paket kurulmadı. Mevcut mesaj kutusu/işlem makbuzu, Property scope, opt-in worker ve ayrı fiziksel donanım sınırı korundu. [Kanıt](parking/07-report.md).

28.10.2026 OTOPARK-08: akadev-phase/verify/security/license/docs mevcut yönergeleri tek agent ile uygulandı. Yeni agent/MCP/Skill/tarayıcı paketi kurulmadı. Yeni paket adı yok; mevcut node-forge güvenlik yaması resmi lisansla incelendi. Gerçek yerel Docker/OIDC/API/PG ile kayıtlı senaryolar; üretim/native/CI ve Zod audit engeli ayrı tutulur. [Rapor](parking/08-report.md).

30.10.2026 TNM 05: `akadev-contract`, `akadev-verify`, `akadev-docs` mevcut yerel yönergeleri uygulandı; yeni skill/alt agent yok. Gerçek doğrulama [TESTING.md](TESTING.md) TNM 05 kaydındadır.

30.10.2026 TNM06: mevcut akadev-phase/contract/verify/security/docs yönergeleri kullanıldı; yeni skill/agent kurulmadı. Kanıt TESTING.md TNM06.
