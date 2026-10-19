# PERSON FAZ05 — Yönetim görevlendirmeleri

17.10.2026. Kullanıcı FAZ04 teslimini kabul edip FAZ05'i açıkça başlattı. Önceki görev[#9](https://github.com/AkaBayraK/BuildingFlow/issues/9) Done; bu faz mevcut[#10](https://github.com/AkaBayraK/BuildingFlow/issues/10) üzerinde izlenir. Yeni task açılmadı. Toplam11faz; bu teslim yalnız FAZ05'tir. FAZ06 başlatılmadı.

## Önkoşul, kapsam ve karar

Paket MASTER_PROMPT/STANDARDS/PHASES/AGENTS/PHASE-05, kök AGENTS ve ilgili mimari/API/test/migration kuralları; önceki FAZ02/04 raporları, Person/tenant/account-link, UserService rol sınırı, People/transition erişim kodu incelendi. Mevcut kişiler için kullanıcının belirlediği tek Altunbayrak Rezidans müşteri sınırı korunur. Gerçek tenant/Property provisioning veya eski yöneticilere otomatik görev yaratma yapılmadı.

Yönetim kapsamı mevcut Property'dir; yeni yapay yönetim alanı/Unit ilişkisi eklenmedi. SITE_ADMIN/MANAGER/ACCOUNTANT mevcut rol sözlüğü kullanıldı. Site yöneticisinin başkasına SITE_ADMIN/SUPER_ADMIN yetkisi verme sınırı genişletilmedi. Yeni paket, DB extension, font veya görsel yoktur; mevcut .NET/EF/PostgreSQL/Keycloak adapter'ı kullanıldı. Tek agent; yerel phase/contract/verify/security yönergeleri uygulandı.

## Uygulanan davranış

- `ManagementAssignment`: tenant+Property+Person+role+tarih. Kişinin sabit rol alanı veya daire maliki/sakini değildir. Aynı kişi farklı alan/görevlerde çalışabilir. OWNER/UnitOwnership/UnitResidency üretilmez.
- DateOnly iki uç dahil; aynı kişi+alan+görev çakışamaz. Bitiş bir kez, gerekçe ve expectedVersion ile yazılır. Önceden planlı bitiş değiştirilmez. Mevcut/gelecek görev ayrıca iptal edilebilir; iptal anında CancelledAt ile etkili olur, başlangıç/bitiş geçmişi korunur. Geçmişte bitmiş görev yeniden iptal edilmez.
- SUPER_ADMIN aktif scope içinde platform istisnasını korur. SITE_ADMIN yalnız MANAGER/ACCOUNTANT görevi oluşturup kapatabilir. SITE_ADMIN görevine yalnız SUPER_ADMIN müdahale eder. MANAGER kendine veya başkasına yönetim görevi atayamaz. Diğer roller genel kişi/görev listesi okuyamaz; ACCOUNTANT kişi yetkisi kazanmaz.
- Diğer aktörlerde exact issuer+subject aktif hesabı, aktif tenant/Property üyeliği ve PersonPropertyScope birlikte aranır. Hedef aynı tenantta aktif NATURAL kişidir; görev tek başına realm rolü/hesap bağı/üyelik üretmez.
- Tenant→Property kilidi, kilit sonrası yetki ve version kontrolü; görev+audit+makbuz tek transaction. Aynı aktör/tenant/operation UUID ve normalize içerik tek etki üretir; eski replay güncel kaydı ve ilk appliedVersion'u döndürür. Farklı içerik/stale version409; güncel yetki replay'de de aranır.
- `ManagementAssignmentEvent` ve `ManagementAssignmentOperation` değişmez audit/makbuz tablolarıdır. Sıralı, sayfalı history yetkili yöneticiye açılır. Audit ve receipt SQL UPDATE/DELETE trigger'ıyla korunur; gerekçe/kimlik/ham request loglanmaz.
- Aktif/gelecek görev varken canonical kişinin pasife alınması veya LEGAL dönüşümü engellenir. Legacy gölge kaydı kaynak version'ından sapmışsa yeni atama/ENFORCED yetki ve yedek yönetici uygunluğu için kullanılamaz.

## RBAC/ABAC geçiş sınırı

[Uygulanmış API sözleşmesi](../../api/management-assignments.md). Tenant bazlı sunucu ayarı `PersonRefactor:ManagementModes:{tenantUuid}`: LEGACY(varsayılan), SHADOW veya ENFORCED. İstemci bu ayarı değiştiremez; bilinmeyen değer erişimi genişletmeden hata verir.

LEGACY mevcut rol+aktif üyelik sınırını korur. SHADOW aynı erişimi sürdürürken yalnız mevcut hesabın legacyAllowed/assignmentAllowed karşılaştırmasını verir. ENFORCED yeni v2 yönetim/kişi uçlarında mevcut rol+üyelik **ve** tarihli görev ister; claim OR assignment fallback yoktur. Görev iptali veya hesap/kişi/üyelik/alan kapanışı sonraki istekte etkili olur. Başka tenant/hesap bilgisi tanılamada açılmaz; erişilemeyen scope UNAVAILABLE/false döner.

Kişi okuması için bir yetkili Property'de geçerli SITE_ADMIN/MANAGER görevi tenantın merkezi kartlarına erişim sağlar; başka Property'de görev düzenleme izni vermez. ACCOUNTANT görevi kişi okuması açmaz. Self-read masked ve salt okunur kalır.

**ENFORCED bu fazda v2 ile sınırlıdır.** Eski v1 merkez, mobil session/finance ve offline outbox tüketicileri otomatik geçirilmedi. Tenant genelinde üretim enforcement tamamlandı veya eski erişim artık görev iptaliyle tüm sistemde kapanır iddiası yoktur. Ortak eski/yeni giriş ve mobil geçişleri sonraki onaylı fazlarda yapılır. `PersonRefactor:Enabled` ve yeni `AssignmentsEnabled` default kapalı; gerçek .env değiştirilmedi. Feature bayrağını kapatmak açıkça ENFORCED seçilmiş tenantın canonical gate'ini LEGACY'ye indirmez.

## Son yetkili yönetici koruması

ENFORCED altında SITE_ADMIN kapatmasında kaybolacak mevcut/gelecek dönem, diğer uygun yönetici dönemlerinin birleşimiyle boşluksuz kapsanmalıdır. İptal bugün, end ise son günün ertesi gününden itibaren kontrol edilir. Gelecek yedek ile bugün arasında boşluk reddedilir. Gerekli yedekler aktif/güncel doğal kişi, doğru issuer hesabı, kişi bağı ve tenant/Property üyelikleriyle değerlendirilir.

Yedek hesabın gerçek Keycloak enabled ve SITE_ADMIN/SUPER_ADMIN rolü mevcut adapter üzerinden salt okunur doğrulanır. Aynı hesap işlemde tekrar sorgulanmaz; yalnız sürekliliği karşılamak için gereken adaylar doğrulanır. Rolü olmayan görev yedek sayılmaz; kimlik servisi doğrulanamazsa işlem503 ile kapalı kalır. Keycloak rolü veya parola yazılmaz. Liste/detail sorgularında kişi/hesap başına HTTP çağrısı yoktur.

Bu koruma görev kapatma transaction'ına aittir. Harici Keycloak rol kaldırma veya diğer yaşam döngülerinden hesap/üyelik/Property kapatma ile dağıtık atomiklik sağlanmış değildir; bu değişiklikler erişimi kapatır ve üretim geçişinde ayrıca uzlaştırılmalıdır. Platform SUPER_ADMIN recovery istisnası korunur.

## Migration, veri ve rollback

`20261017150715_ManagementAssignments` eklemeli üç tablo + Designer + snapshot:

- `management_assignments`: tenant/Property/person composite FK, role/date/closure check, açık ve sonlu tarih çakışma kontrolü, değişmez görev geçmişi.
- `management_assignment_events`: assignment scope FK, appliedVersion tekilliği, reason/action/version check ve immutable trigger.
- `management_assignment_operations`: actor issuer+subject+tenant+UUID tekilliği, scope FK, fingerprint/appliedVersion ve immutable trigger.

Üç tabloda ortak BaseEntity8alanı NOT NULL; UUID, optimistic version, değişmez oluşturma audit'i ve soft-delete/etkinlik korunur. SQL closure check, iptal zamanını gerekçesiz bırakmayı da reddeder. Dolu Down soft-delete dahil herhangi veri varsa ilk drop öncesi durur. Boş Down/Up, tekrar migrate ve model/snapshot eşliği gerçek PG'de doğrulandı. Önceki migration dosyaları değiştirilmedi; eski kişi/ilişki/finans/mobil kayıtlarına backfill yapılmadı.

Yalnız geçici test DB/role/schema kullanıldı ve temizlendi. Ana DB migration/görev oluşturma/tenant seed, servis restart veya gerçek Keycloak rol/hesap yazımı yoktur. Feature bayrakları migration kapısı değildir; AutoMigrate açık sonraki API açılışı bekleyen migration'ı uygulayabilir. Dolu modülde rollback için kayıtları silme yerine özelliği kapatma, quiesce, ileri düzeltme veya doğrulanmış izole restore planlanmalıdır. ENFORCED→LEGACY konfigürasyon değişikliği yetki gevşetmesidir; normal rollback adıyla otomatik yapılmaz.

## Test ve doğrulama

| Kontrol | Sonuç |
|---|---|
| Seçili gerçek PG/fixture storage entegrasyonu | **314/314 PASS;0 fail/skip** |
| Mevcut tüm unit test projesi | **187/187 PASS;0 fail/skip** |
| Mevcut frontend npm test | **119/119 PASS;0 fail/skip** |
| Son .NET çözüm derlemesi | **0 hata,0 uyarı** |
| Mevcut paket lisans metadata kontrolü | **329 npm/NuGet sürümü PASS** |
| Eski migration/paket/lock SHA256, belge bağlantısı ve diff | **PASS** |

Yeni kabul26entegrasyon senaryosu ve3domain unit testini kapsar. Mevcut toplamlar diğer önceki çalışmalardan gelen testleri de içerir; hepsi bu fazda yeniden yazılmış değildir. Entegrasyon seçimi ManagementAssignment/PersonTransition/PersonRelation/CentralPeople/CentralPerson/People/User/Mobile, Authentication/ReleaseSecurity, migration/persistence/PropertyModel ve Dues/DuesParty/Payment/Report regresyonudur; tüm backend suite değildir.

Kanıtlar: scope/issuer/rol/üyelik, alanlar arası görev, kendine yetki yükseltme reddi, accountant PII yasağı, gelecekte/bitmiş/pasif görev ve pasif kişi/hesap/tenant/Property, UUID replay/yarış/stale version, sonlu inclusive overlap, audit/receipt değişmezliği, doğal kişi guard'ı, LEGACY/SHADOW/ENFORCED karşılaştırma ve aynı tokenla iptal sonrası ret, son yönetici/gap ve gerçek rolü olmayan yedek, stale legacy shadow, JSON veri koruması, composite FK, BaseEntity/model/rollback. Keycloak rol kontrolü FakeKeycloak fixture'ıyla yapıldı; gerçek sunucu kabulü çalıştırılmadı. Fake yazım metodları desteklenmez; beklenmeyen rol/parola yazımı testleri geçiremez.

Ara koşular307/307,309/309,310/310 ve314/314 geçti; nihai kaynak/migration ile son314/314 tekrar doğrulandı. Gölge uygunluk predicate'i eklenirken ara build'de parantez derleme hatası oluştu; ortak EligiblePeople sorgusuna çıkarılarak düzeltildi ve sonraki derleme/testler geçti. Başarısız build başarı diye sayılmadı. Görev başlangıcında yanlış Project item ID ile başarısız güncelleme, manifestteki gerçek ID ile düzeltildi; başka göreve yazılmadı.

FE typecheck/production build, mobil/native export, görsel/cihaz/offline ve production kabulü bu fazda yeniden çalıştırılmadı; istemci kaynakları bu fazda değişmedi. Fortify/SAST/SCA/CVE/secret scanner çalıştırılmadı. Lisans metadata kontrolü bunların yerine geçmez. Loglar `/tmp/bf-person05/` altındadır.

## Değişen dosyalar

| Alan | Dosyalar |
|---|---|
| Domain | `backend/src/BuildingFlow.Domain/ManagementAssignment.cs` |
| Application | `backend/src/BuildingFlow.Application/ManagementAssignments.cs` |
| Infrastructure | `ManagementAssignmentConfigurations.cs`, `ManagementAssignmentStore.cs`, `ManagementAccess.cs`, `CentralPeopleStore.cs`, `PersonTransitionStore.cs`, `DependencyInjection.cs` |
| API | `ManagementAssignmentEndpoints.cs`, `Program.cs` |
| Migration | `20261017150715_ManagementAssignments.cs`, `.Designer.cs`, `BuildingFlowDbContextModelSnapshot.cs` |
| Test | UnitTests/`ManagementAssignmentTests.cs`; IntegrationTests/`ManagementAssignmentTests.cs`, `ManagementAssignmentMigrationTests.cs` |
| Belge/ayar | `.env.example`, bu rapor, `docs/api/management-assignments.md`, central-people/person-transition/MIGRATIONS/TESTING/SKILLS_AND_AGENTS, progress STATUS/SESSION/CHANGELOG |

Infrastructure/API/Migration/Test dosyaları ilgili backend proje klasörlerindedir. Mevcut checkout main ve önceki faz/diğer çalışma değişiklikleri korundu; toplam diff yalnız bu faz değildir. Yeni bağımlılık/extension, commit/push/merge/reset/stash yoktur.

## Riskler ve sonraki faz

1. Gerçek müşteri/Property/account-link/görev kurulumu ve üretim rol doğrulaması yapılmadı. Canlı ENFORCED öncesi SHADOW farkları, gerçek issuer/realm rol/üyelik, yönetim sürekliliği, recovery hesabı, yedek/restore ve negatif erişim kanıtı gerekir. Otomatik eski yönetici backfill'i yoktur.
2. Eski v1/mobil legacy geçişi tamamlanmadan bütün uygulamada anlık görev iptali iddiası kurulamaz. Yeni UI ve mobil görev/cache/outbox tüketicileri sonraki fazlardır; bu faz yeni ekran getirmedi.
3. Son yönetici kontrolündeki Keycloak okumaları dış servise bağlıdır; servis kesilirse devir fail-closed503 olur. Harici rol/account değişimleri dağıtık transaction değildir; dış sistemle uzlaştırma gerekir.
4. Legacy gölge kişi drift'i güvenli biçimde yetkiyi kapatır; veri sahibi düzeltmesi ve kontrollü uzlaştırma olmadan otomatik üzerine yazılmaz. Import canonical yazım koruması korunur.
5. Önceki paket güvenlik/audit bulguları bu fazda paket güncelleyerek giderilmedi; yeni paket yasaklığı korunur. Bu teslim üretim güvenlik sertifikası değildir.

Resmî adımlar:1inceleme,2önkoşul,3etki/dosya planı,4uygulama,5test/güvenlik kontrolü,6rapor/görev teslimi. Teknik teslim6/6=%100,00; kullanıcı FAZ05 kabulü beklenir. Kabul edilmiş önceki fazlar4/11=%36,36. İŞ/UI/mobil yol haritası sayacı değiştirilmedi. FAZ06 başlatılmadı.

Token giriş/çıkış/toplam, kalan context ve bu turun token tüketimi: BİLGİ MEVCUT DEĞİL. Hesap kotası sohbet token tüketimi değildir.

Son17.10.2026Codexusage okuması:300dk kullanılan%63/kalan%37, reset17.10.2026 22:37:38;10080dk kullanılan%73/kalan%27, reset22.10.2026 10:57:02 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir.
