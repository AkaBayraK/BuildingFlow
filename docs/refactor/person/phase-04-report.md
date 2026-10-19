# PERSON FAZ04 — Daire ilişkileri ve geçiş migrasyonu

17.10.2026. Kullanıcı FAZ03'ü kabul ederek bu fazı onayladı. Başlangıçta eksik olan tenant sınırı sorusuna **“tüm kişiler tek olan Altunbayrak Rezidans'a ait”** yanıtı geldi; önkoşul kapısı çözüldü, uygulamaya devam edildi. Altı resmî adım bu rapor ve test kanıtıyla teslim edilir; kullanıcı kabulü beklenir. [Mevcut görev #9](https://github.com/AkaBayraK/BuildingFlow/issues/9). FAZ05 başlatılmadı.

## Karar ve uygulama

Paket MASTER_PROMPT/STANDARDS/PHASES/AGENTS/PHASE-04, kök/frontend AGENTS, önceki raporlar, mevcut Person/UnitOccupancy/PeopleStore, mimari/API/migration/test kuralları incelendi. Yerel phase/contract/verify/security yönergeleri, tek agent. Yeni bağımlılık veya DB extension eklenmedi.

- Ayrı `UnitOwnership`, `UnitTenancy`, `UnitResidency` entity ve tabloları; hepsi BaseEntity. Aynı kişinin farklı birim/türlerde ilişkisi ve ortak malik/çoklu kiracı desteklenir. Malik/kiracı olmak otomatik sakinlik üretmez.
- DateOnly iki uç dahil; aynı kişi/birim/türde ve farklı türlerde ortak primary-contact çakışması engellenir. Bitiş bir kez kaydedilir; kişi, birim, başlangıç, primary ve pay geçmişi yeniden yazılamaz.
- Malik payı nullable pozitif numerator/denominator; ikisi null bilinmiyor demektir. Bilinen payların toplamı her dönem sınırında≤1. Domain BigInteger, PG numeric tam sayı kesir/GCD kullanır; floating point/eşit pay varsayımı yoktur. Legacy OWNER payı bilinmiyor taşınır.
- `PersonPropertyScope` açık tenant–Property eşlemesi; bir Property tek tenant. `LegacyPersonMap` kaynak UUID/sourceVersion, canonical kişi ve alan-özel ad/telefon/e-posta/adres snapshot'ı. `LegacyOccupancyMap` kaynak UUID/version ve tek bir tür hedefi; composite FK tenant/property/unit tutarlılığı sağlar.
- `PersonTransitionStore` önce salt okunur dry-run: kaynak hash'i, sayımlar, eksik profil/mapping/tarih/primary kontrolü. Apply tenant→Property kilit sırası, kilit sonrası tekrar hash/yetki kontrolü ve tek transaction kullanır. Eşzamanlı/tekrar apply aynı mapping'e gider. Eksik profil, değişmiş kaynak veya çelişkili mapping atomik409; sahte kimlik/otomatik merge/atlayarak kısmi başarı yoktur.
- Kaynak legacy satırlar, audit/version/UUID, LinkedUserId/kişi kodu ve finans snapshot/mobil makbuzlar yazılmaz. Silinmiş/pasif kaynaklar history olarak kopyalanır. Hesap bağı otomatik oluşturulmaz.
- Compatible read eski UUID ve legacy sourceVersion'u concurrency değeri olarak döndürür; mapping BaseEntity.version veya canonical.version yerine geçirilmez. Yerel profil ayrı kalır. Kaynak sonradan değişirse read409, comparison matches=false; gölge kopya sessizce tazelenmez. Import canonical kişinin v2 update'i409 ile kapalıdır.

## API ve yetki

[Uygulanmış sözleşme](../../api/person-transition.md). Kök `/api/v2/tenants/{tenantId}/properties/{propertyId}/person-transition`.

- `GET /preview`, `POST /apply`: yalnız SUPER_ADMIN; açık aktif tenant ve Property.
- `GET /comparison`, `GET /people/{legacyPersonId}`: gerçek yönetici rolü + exact issuer/subject aktif hesap/tenant üyeliği + aktif Property üyeliği. SUPER_ADMIN mevcut aktif scope bypass'ı korunur.
- OWNER/TENANT/RESIDENT/ACCOUNTANT kişi/geçiş listesi okuyamaz veya kayıt değiştiremez. Mevcut canonical self-read salt okunur kalır.
- `PersonRefactor:Enabled` ve ayrıca `PersonRefactor:TransitionEnabled` default kapalı. Gerçek .env değiştirilmedi. Endpoint'ler yeni yönetim ekranı değildir.

## Migration ve ortak checkout

Bu sohbet ara verdikten sonra ortak checkout'ta `20261016124132_AutoSchema_20261016154130` migration'ının bu fazın altı tablosunu zaten içerdiği görüldü. Ardından Consents/DefaultConsentTexts ve başka çalışma dosyaları bulunuyordu. Bunlar silinmedi, yeniden üretilmedi veya bu fazın geliştirmesi olarak sahiplenilmedi.

Yeni `20261017144809_PersonUnitRelations` + Designer güncel modelden üretildi; snapshot model eşliği gerçek testte doğrulandı. Yeni migration, mevcut tablolar üzerine ortak view ve Unit kilitli tarih/primary/pay/history trigger'larını ekler; CreateTable/legacy veri UPDATE/DROP içermez. Önceki otomatik migration'a dokunmadan ek koruma sağlar. Altı tablonun sekiz ortak NOT NULL alanı gerçek PG'de kontrol edildi.

Down altı tabloda soft-delete dahil veri varsa **ilk drop öncesi** reddeder. Boş Down yalnız yeni view/function/trigger'larını kaldırır; tablolar önceki migration'a aittir. Empty down/up ve repeated migrate doğrulandı. İleri düzeltme tercih edilir; eski binary ile veya manuel DDL ile korumaları atlayan geri alma güvenli sayılmaz.

Bu tur yalnız geçici PG fixture'ına migrate/backfill yapıldı. Ana DB migrate/backfill, gerçek tenant provisioning, servis restart veya gerçek kullanıcı/rol yazımı yapılmadı. Ortak checkout'taki önceki migrationların canlı uygulama durumu bu tur denetlenmedi. Feature bayrakları migration kapısı değildir; AutoMigrate açık sonraki API açılışı pending SQL'i uygulayabilir.

## Doğrulama

| Kontrol | Sonuç |
|---|---|
| Seçili gerçek PostgreSQL/fixture storage entegrasyonu | **288/288 PASS; 0 fail, 0 skip** |
| Tüm mevcut unit test projesi | **174/174 PASS; 0 fail, 0 skip** |
| Mevcut frontend npm test | **117/117 PASS; 0 fail, 0 skip** |
| Son .NET çözüm derlemesi | **0 hata, 0 uyarı** |
| Lisans metadata kontrolü | **329 mevcut npm/NuGet paket sürümü PASS** |
| Paket/lock/eski migration SHA256 ve diff whitespace | **PASS** |

Loglar bu koşuya ait `/tmp/bf-person04/` altındadır. İlk geniş koşular225/225,231/231 ve287/287 geçti; nihai288/288 aynı seçili kapsamın son eklenen dry-run testiyle tekrar doğrulamasıdır. Frontend typecheck/build ve mobil/native build bu fazda yeniden çalıştırılmadı; ilgili istemci kaynakları bu fazda değişmedi. Son geçici test DB/rolü temizlendi.


Kapsam: gölge dry-run salt okunurluğu; eksik profil/stale hash/finite primary çatışması; atomik ve concurrent replay; eski UUID/yerel profil/JSON korunması; ortak kimlik/telefonla ayrı canonical kişiler; unknown share; tenant/Property/issuer/rol/üyelik/revoke; pasif/silinmiş history; v1 kişinin değişmesiyle drift; DB finite overlap/cross-kind primary yarışı; long.MaxValue kesin paylar; composite FK; end history; guard migration/reapply/model ve sekiz NOT NULL alanı. Finans Dues/DuesParty/Payment ve Report regresyonu, People/User/Mobile/Authentication/ReleaseSecurity/persistence ve migration sınıfları seçili entegrasyon kapsamındadır; tüm backend suite değildir.

İlk birim koşusu sandbox socket izninde durdu; gerekli izinle yeniden çalıştırıldı. Bir entegrasyon tekrarında mevcut AutoMigrationTests.FreshDatabaseIsMigratedOnStartup testi, oluşturduğu geçici alt DB temizliğinde `42501 permission denied to terminate process` verdi; koşu287pass/1fail olarak kaydedildi. Bu koşunun yalnız manifest-owned test DB/alt DB/rolü admin ile temizlendi ve aynı kapsam yeniden çalıştırıldı. Hata saklanmadı; test/uygulama kaynağı bu cleanup sorunu için değiştirilmedi.

Yeni relation SQL korumaları gerçek yarış testinden geçti. Fortify/SAST/SCA/CVE/secret scanner çalıştırılmadı; lisans metadata kontrolü güvenlik taraması değildir. UI/cihaz/native/offline/production kabulü yapılmadı. Frontend/mobil kaynak bu fazda değiştirilmedi; görünür merkez/mobil geçiş sonraki fazlardır.

## Bu fazın dosyaları

| Katman | Dosyalar |
|---|---|
| Domain | `backend/src/BuildingFlow.Domain/PersonUnitRelations.cs` |
| Application | `backend/src/BuildingFlow.Application/PersonTransition.cs` |
| Infrastructure | `PersonRelationConfigurations.cs`, `PersonTransitionStore.cs`, `CentralPeopleStore.cs` import-write koruması, `DependencyInjection.cs` kayıt |
| API | `PersonTransitionEndpoints.cs`, `Program.cs` endpoint kaydı |
| Migration | `20261017144809_PersonUnitRelations.cs`, `.Designer.cs`, güncel `BuildingFlowDbContextModelSnapshot.cs` model eşliği |
| Test | `PersonUnitRelationTests.cs`, `PersonTransitionTests.cs`, `PersonRelationPersistenceTests.cs` |
| Konfigürasyon/belge | `.env.example`, `docs/api/person-transition.md`, central-people/MIGRATIONS/TESTING/SKILLS_AND_AGENTS, progress STATUS/SESSION/CHANGELOG, bu rapor |

Tablodaki Infrastructure/API/Test dosyaları ilgili backend proje klasörlerindedir. Önceki kaynak/migration/paket hash baseline'ı kontrol edildi: başlangıçta bulunan paket/lock ve eski migration hashleri aynı. Başka turdaki consent/request/mobile dosyaları korunmuştur; toplam checkout diff'i yalnız bu fazın değişikliği değildir. Lisans kontrolü mevcut envanteri güncelledi; yeni paket eklenmedi. Commit/push/merge/reset/stash yapılmadı.

## Riskler, rollback ve sonraki adım

1. **Bu teslim gölge expand/migrate mekanizmasıdır; v1 canonical cutover değildir.** V1 tek authoritative yazım kaynağı kalır. Eski yazımlar açıkken snapshot zamanla eskir; drift409 bilinçli güvenli duruştur. Otomatik refresh/merge veya ikinci yazım yolu açılmadı. Cutover öncesi quiesce, son dry-run, uzlaştırma ve kayıt/FK/negatif yetki eşliği gerekir.
2. Kullanıcının tek Altunbayrak Rezidans kararı kabul edildi; canlı tenant/provisioning ve gerçek Property UUID'si bu tur atanmadı. Müşteri adından eşleme veya başka alanı otomatik dahil etme yoktur. Eksik legacy profiller veri sahibi tarafından tamamlanmadan apply yapılamaz. Canlı önkoşul: yedek ve restore provası, kaynak sayımı/hash, doğru tenant–Property eşlemesi, sıfır drift.
3. Hesap issuer eşlemesi, eski tüzel temsil/çelişkili hesap bağları, yönetim görevlendirmeleri ve merkez/mobil ekran geçişi ayrı kabul kapsamındadır. Yeni genel relation CRUD ekranı bu fazda yoktur; domain/DB kuralları ve legacy geçişi hazırlanmıştır.
4. Gölge import uygulanmışsa veriyi silerek geri dönülmez; özelliği kapat, legacy authoritative okumayı sürdür, quiesce ve ileri düzeltme/izole restore planı yap. Yeni yazım başladıktan sonra ters aktarım ayrıca tasarlanır. Destructive contract/drop ayrı onay ister.
5. Ortak checkout'ta başka çalışmalar vardır; snapshot/model ortak olduğundan diğer oturumların üretmiş olduğu migrationları incelemeden geri alma yapılmaz.

FAZ04 teknik adımlar:1 inceleme,2 önkoşul kararı,3 etki/dosya planı,4 uygulama,5 test/güvenlik kontrolü,6 rapor/görev teslimi. Nihai teslim sonrası6/6=%100,00; kullanıcı kabulü beklenir. Kabul edilmiş önceki fazlar3/11=%27,27. Ürün İŞ/UI/mobil yol haritası sayacı sırf bu backend genişletmesiyle işaretlenmedi. FAZ05 başlatılmadı.

Token giriş/çıkış/toplam, kalan context ve tur tüketimi: BİLGİ MEVCUT DEĞİL. Hesap kota penceresi sohbet token tüketimi değildir.

17.10.2026 son Codexusage okuması:300dk kullanılan%19/kalan%81,reset17.10.2026 22:37:38 Europe/Istanbul;10080dk kullanılan%66/kalan%34,reset22.10.2026 10:57:02 Europe/Istanbul. Hesap pencereleri bu sohbetin token tüketimi değildir.
