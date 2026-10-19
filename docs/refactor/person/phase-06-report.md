# PERSON FAZ06 — Kişiler merkez ekranı

18.10.2026. Kullanıcı FAZ05'i kabul ederek FAZ06'yı başlattı; bu tur devam talebiyle aynı kapsam tamamlandı. Önceki görev #10 Done; mevcut [#11](https://github.com/AkaBayraK/BuildingFlow/issues/11) inceleme/kullanıcı kabulü için hazırlanmıştır. FAZ07 başlatılmadı.

## Önkoşul ve etki analizi

Paket MASTER_PROMPT, STANDARDS, PHASES, AGENTS ve PHASE06; kök/frontend AGENTS, önceki raporlar, merkezi kişi/geçiş/görev sözleşmeleri, mevcut form/liste/auth/error bileşenleri ve Next yerel dokümanları esas alındı. Kullanıcının Altunbayrak Rezidans için tek müşteri kararı korunur; gerçek tenant veya Property eşlemesi oluşturulmadı. Yerel akadev phase/contract/verify/security yönergeleri uygulandı; tek agent çalıştı.

Etki: merkezi listede müşteri seçimi ve kişi kartları, detayda genel/bölüm ilişkileri/yönetim görevleri/hesap sekmeleri. Alan bazlı eski ekran varsayılan olarak korunur. Yeni okumalar aktif tenant/Property yetki sorgularıyla; yazımlar mevcut canonical/görev servisleri ve sınırlı native ilişki servisiyle yapılır. Mevcut finans/mobil tüketicilere geçiş yapılmadı. Yeni üçüncü taraf bağımlılık, extension, font veya görsel eklenmedi.

## Uygulama

- `/people` için private özellik bayrağı, doğrulanmış UUID'li müşteri/kişi alt rotaları, await params ve login dönüş yolu. Menü kişi merkezi açıkken alandan bağımsız merkeze gider. Varsayılan false eski ekranı korur. Build ve runtime Docker ayarları birlikte taşınır; .env.example dışında gerçek .env korunur.
- Mevcut shadcn ManagementPanel/Dialog/form alanları, TanStack RecordList ve zaten kurulu Radix Tabs kullanıldı. TR/EN metinler, etiketler, loading/error/empty, sayfalama/arama/sıralama, mobil kartlar, ortak bildirimler bulunur. Kişi formunda rol/ilişki checkbox'ı yoktur; hesap bağı düzenlenmez. Merkezi iletişim alanları opsiyoneldir; eski formun zorunlulukları değişmez.
- Kişi oluşturma/düzenleme mevcut v2 API; version/409 yeniden yükleme. Liste yalnız özet alanları taşır. İstemci DTO tenant/kişi/tür/tarih/sayfalama ve exact payları doğrular. Aktör/rota değişiminde veri gizlenir ve eski istek iptal edilir. PII kalıcı istemci deposuna veya anonim SSR'a yazılmaz.
- Bölüm ilişkileri OWNER/TENANT/RESIDENT birleşik sayfalı görünüm; yönetim görevleri yalnız aynı kişinin izinli alanlarındaki kayıtlar. Query projeksiyonları DB'de birleştirilir; satır başına hesap/HTTP sorgusu yoktur. Hesap DTO yalnız bool ve bağlantı tarihi taşır.
- Yönetici tenanttaki merkezi kişileri görür, ilişki/alan eylemleri ayrıca Property yetkisi ister. OWNER/TENANT/RESIDENT/ACCOUNTANT genel merkeze erişemez. Önceki maskeli self-read korunur. ENFORCED v2 görev şartı kaldırılmadı; yönetim görev yazımı önceki SITE_ADMIN/SUPER_ADMIN sınırındadır.
- Native ilişki ekleme/bitirme ayrı defaultfalse bayrak ister. Native kişi ve **hiç eski kişi/occupancy geçmişi olmayan** Property şartı vardır; silinmiş eski veri de engeldir. İçe aktarılan kart/ilişkiler salt okunur, kaynak değişimi uyarılır, eski kaynak bağlantısı yalnız izinli alanda gösterilir. Otomatik kişi birleştirme veya yeni realm rolü yoktur.
- Native ilişki yazımı tenant→Property kilidi ve kilit sonrası yetki, expectedVersion, UUID/fingerprint makbuzu, mevcut exact pay/çakışma trigger'larıyla atomiktir. Aynı retry tek etki; replay güncel kayıt+ilk appliedVersion verir. Eski kişi oluşturma ve occupancy yazımı native geçmişte409; eski oluşturma yolu da aynı Property transaction kilidindedir. Property version artırılır. Native devam eden/gelecek ilişkiler parent kapatma/silmeyi ve kişi pasife almayı engeller; sakinlik LEGAL dönüşümünü engeller.
- `20261017153156_PersonCenterUnitOperations` additive migration: üç alternate composite key ve sekiz ortak alanlı immutable makbuz tablosu; scope/person/hedef FK/check/unique, dolu Down koruması. Önceden mevcut migration dosyalarının hashleri korunmuştur.

Sözleşme ve uçlar: [person-center.md](../../api/person-center.md).

## Değişen dosya grupları

- Backend: `Application/PersonCenter.cs`, `Domain/PersonUnitOperation.cs`, `Infrastructure/PersonCenterStore.cs`, `PersonUnitOperationConfiguration.cs`, `PersonRelationConfigurations.cs`, `ManagementAccess.cs`, `DependencyInjection.cs`, `CentralPeopleStore.cs`, `PeopleStore.cs`, `HierarchyStore.cs`; API `PersonCenterEndpoints.cs`, `Program.cs`; yeni migration/Designer/snapshot.
- Frontend: `features/person-center/{model,hooks,Choices,PersonEditor,RelationForms,CenterList,CenterDetail}`, `PersonFormFields.tsx`, workspace layout/people rotaları, `WorkspaceShell.tsx`, `auth/return-path.ts`, TR/EN sözlükleri. Frontend/root env örnekleri, frontend Dockerfile ve compose bayrakları.
- Kanıt: `PersonCenterTests.cs`, `PersonCenterMigrationTests.cs`, frontend `person-center.test.mjs`; bu rapor, API/MIGRATIONS/TESTING ve mevcut ilerleme/skill kayıtları.

Paylaşılan checkout'taki diğer kullanıcı/consent/mobile/hizmet değişiklikleri bu fazın teslimi olarak sahiplenilmedi, silinmedi. Commit/push/merge/reset/stash/branch değiştirme yapılmadı.

## Doğrulama — GEÇTİ

| Kontrol | Bu tur sonucu |
| --- | --- |
| Seçili gerçek PostgreSQL + fixture storage entegrasyon | **329/329**, başarısız0, atlanan0 |
| Backend birim | **187/187**, başarısız0, atlanan0 |
| Frontend Node | **135/135**, başarısız0, atlanan0 |
| Frontend typecheck + Next production build | Başarılı; merkez rotaları ve eski Property rotaları birlikte üretiliyor |
| .NET solution build | 0 hata, 0 uyarı |
| Lisans metadata | **329** mevcut npm/NuGet sürümü; yeni paket/lock/proje manifest değişikliği yok |
| Migration | Mevcut veriyi koruyan upgrade, repeat/reapply, model eşliği, sekiz NOT NULL alan, immutable receipt ve dolu Down reddi gerçek ayrı test şemasında geçti |
| Diff | `git diff --check` başarılı; eski migration hashleri aynı |

Seçili entegrasyon: PersonCenter, ManagementAssignment, CentralPeople/CentralPerson, PersonTransition/PersonRelation, People/PersonProfiles/PersonCode/UserManagement, Mobile, Migration/Persistence/PropertyModel, Authentication/ReleaseSecurity ve Dues/DuesParty/Payment/Report grupları. Bu bütün backend testlerinin çalıştırıldığı iddiası değildir. Komut: `.NET test` ilgili integration csproj, `--no-restore --disable-build-servers -m:1 --filter` bu gruplar; unit csproj ayrı. Frontend: `npm test`, `npm run typecheck`, `npm run build`. Lisans: `python3 scripts/check-licenses.py`.

Yeni kanıtlar: scope/IDOR/rol reddi, boş birleşik sorgu, üç türde create/list/detail/end/current replay, aynı UUID eşzamanlı tek etki, farklı payload409, yabancı bölüm404, 64 bit pay doğruluğu, bayrak kapalı read-only, imported/sourceChanged ve eski Property write reddi, native Person/Unit koruması ve eski ikinci yazar409. Migration senaryosu kendi şemasını; API grubu yalnız ayrılmış geçici DB/rol ve fixture bucket'ını kullandı. Test kaynakları temizlendi, ana projects DB erişimi test rolüne kapalıydı.

Ara hatalar saklanmadı: ilk test derlemesinde internal BusinessDate/Unit constructor erişimi düzeltildi; EF constructor-projeksiyon birleşimi500 verdi, member-init projeksiyonla düzeltildi; son negatif test eski CreateAsync yolundaki eksik kilidi yakaladı, transaction/LockProperty ile düzeltildi. Son 329 testte hata yoktur. Frontend derlemesinin sandbox SWC cache erişim engeli gerçek izin mekanizmasıyla giderildi; paket yüklenmedi. İlk i18n anahtar/TypeScript scope ve bigint hedef hataları düzeltildi, son kontroller geçti.

## Sınırlar, risk ve geri dönüş

- **Canlıya geçiş yapılmadı.** Yeni web ve ilişki yazım bayrakları defaultfalse. Mevcut Altunbayrak verisi aktarılmadı; DB/rol/hesap seed ve servis restart yoktur. Sonraki API açılışında AutoMigrate açıksa bekleyen migration uygulanabilir; yayın öncesi bu etki ayrıca değerlendirilmelidir.
- Native ilişki kayıtları eski finans/mobil okuma modeline yansıtılmaz. Eski veri taşıyan alanlarda yeni yazımı açan genel cutover yoktur; bu kısıt kodda uygulanır. Finans/mobil tüketiciler sonraki onaylı fazların konusudur.
- Hesap sekmesi salt okunurdur; kişi-hesap bağlama/rol atama UI'sı eklenmedi. Belirsiz mutation retry panel açıkken aynı UUID'yi tutar; kalıcı offline outbox yoktur. Panel kapatılırsa işlem sonucunu yenileyerek doğrulamak gerekir.
- Erişilebilirlik ve i18n için DTO/source/anahtar/parametre testleri ve mevcut accessible primitives doğrulandı. Kullanıcının önceki tercihi doğrultusunda canlı tarayıcı/klavye/ekran okuyucu/telefon kabulü yapılmadı: **DOĞRULANAMADI**. SAST/Fortify/SCA/CVE/secret scanner, gerçek Keycloak görevlendirme ve üretim kabulü çalıştırılmadı.
- Bayrakları kapatmak ekran/yeni yazımı kapatır; veri korunur. Makbuzlu şemada Down yasaktır, forward fix gerekir. Feature kapatma eski/yeni modelleri otomatik birleştirmez. Yıkıcı temizleme yoktur.

## İlerleme ve sonraki adım

Altı resmî adımın teknik teslimi **6/6=%100,00**. Kullanıcı kabulü ayrı bekler; #11 In review. Kabul edilmiş fazlar **5/11=%45,45**; FAZ06 kabul edilmeden bu sayaca eklenmez. Mevcut İŞ/UI/mobil roadmap sayaçları değiştirilmedi. Sonraki adım yalnız kullanıcı FAZ06 kabulü ve FAZ07 başlangıç onayıdır.

Codex usage (18.10.2026): 300dk kullanılan %28/kalan %72, reset 18.10.2026 05:15:34; 10080dk kullanılan %4/kalan %96, reset 25.10.2026 00:15:34 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir. Giriş/çıkış/toplam token ve kalan context: BİLGİ MEVCUT DEĞİL.

Kabul güncellemesi —18.10.2026: kullanıcı FAZ06'yı açıkça kabul etti; #11 Done. Sonraki FAZ07 ayrıca onaylanarak başlatıldı. Yukarıdaki testler FAZ06 tesliminin tarihsel kanıtıdır.
