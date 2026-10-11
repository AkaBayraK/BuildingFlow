# Veritabanı değişiklikleri ve kurulum

BuildingFlow, PostgreSQL şema değişikliklerini **EF Core Migrations** ile sürümler. Liquibase XML changeset'lerinin görevini burada C# migration dosyaları üstlenir. Ayrı migration framework veya yeni paket eklenmedi.

| Java / Liquibase kavramı | BuildingFlow / EF Core |
| --- | --- |
| XML changeset | Infrastructure/Migrations içindeki tarihli `.cs` dosyası |
| İleri değişiklik | `Up(MigrationBuilder)` |
| Geri alma tanımı | `Down(MigrationBuilder)`; veri kaybını otomatik geri getirmez |
| Uygulanan değişiklik geçmişi | PostgreSQL `__EFMigrationsHistory` |
| Son modelin karşılaştırması | `BuildingFlowDbContextModelSnapshot.cs` |
| Kurulum/yükseltme | Tek seferlik `buildingflow-migrations` Docker servisi |

Mevcut sıra: `20261007131600_InitialFoundation` → `20261007154935_SiteMembershipAccess` → `20261007210343_UnifiedPropertyModel` → `20261008065215_PeopleAndUnitOccupancies` → `20261008185906_DuesAndCharges`. İlk iki migration site/blok/daire ve üyeliği kurar; üçüncü rename/backfill ile Property/Building/Unit modeline taşır; dördüncü People/UnitOccupancy ve composite FK/index/tarih kısıtlarını ekler. Yeni veritabanında sıra baştan, mevcut veritabanında yalnızca eksik migration'lar uygulanır. Kaynak, Designer ve snapshot birlikte Git'te tutulur. People migration Down, kişi veya ilişki geçmişi varsa tablo silmeyi reddeder; silinmiş geçmişi otomatik geri getirmez.

## Yeni kolon ekleme: entity → migration → çalıştır (otomatik)

Liquibase'de changeset yazıp uygulamayı başlatmak ne ise burada da aynısı geçerli: migration dosyası changeset'in karşılığıdır ve API açılırken bekleyen migration'ları kendisi uygular.

1. Alanı entity'ye ekleyin. Kolon adını `EntityConfigurations.cs` içinde `HasColumnName("snake_case")` ile verin; zorunlu (NOT NULL) bir alan için varsayılan değer veya backfill planlayın.
2. Migration'ı üretin:
   - `bash scripts/dev.sh api` bunu zaten yapar: açılmadan önce `bash scripts/db.sh sync` çalışır. Entity değiştiyse `AutoSchema_<zaman>` migration'ı üretilir.
   - Elle üretmek isterseniz: `bash scripts/db.sh sync` (otomatik ad) veya `bash scripts/db.sh add AnlamliAd`.
3. API'yi çalıştırın. `Database:AutoMigrate` açıksa bekleyen migration'lar açılışta uygulanır, log'da `BuildingFlow.Migrations: applying …` satırı görünür.
4. Üretilen `.cs`, `.Designer.cs` ve snapshot dosyalarını inceleyip entity değişikliğiyle **aynı commit'te** kaydedin.

`sync` yalnız **eklemeli** değişiklikleri otomatik üretir. Üretilen `Up` içinde kolon/tablo silme, yeniden adlandırma, tip değiştirme veya ham SQL varsa dosyaları siler, snapshot'ı geri yükler ve durur. Bu tür değişiklik için `bash scripts/db.sh add Ad` ile migration'ı elle yazıp inceleyin (rename için `RenameColumn`, veri kaybı değerlendirmesi).

### Açılışta otomatik migration

| Ayar | Varsayılan | Not |
| --- | --- | --- |
| `Database:AutoMigrate` (`Database__AutoMigrate`) | Development'ta `true`, diğer ortamlarda `false` | `dev.sh api` bunu `BUILDINGFLOW_AUTO_MIGRATE` (varsayılan `true`) ile ayarlar |
| `--migrate` argümanı | — | Yalnız migration uygular ve çıkar (compose `buildingflow-migrations`, üretim işi) |

- Kapatmak için: `BUILDINGFLOW_AUTO_MIGRATE=false bash scripts/dev.sh api` ya da `Database__AutoMigrate=false`.
- Aynı anda birden fazla API örneği veya migration işi çalışsa da şemayı tek bir süreç değiştirir: PostgreSQL advisory lock (`DatabaseMigration.AdvisoryLockKey`) alınır, diğerleri bekler, kilit bırakılınca bekleyen migration kalmadığını görür. Worker migration yapmaz.
- Entity değişmiş ama migration üretilmemişse API açılmaz ve `Run: bash scripts/db.sh sync` mesajı verir. Bu, eksik kolon yüzünden ekranların çalışma anında hata vermesini önler.
- Migration başarısız olursa (ör. elle eklenmiş bir kolondaki veri yeni tipe sığmıyorsa) migration transaction'ı geri alınır, API açılmaz ve `BuildingFlow.Migrations: schema migration failed` yazar.
- Üretimde varsayılan kapalıdır. Önce yedek alınır, sonra `--migrate` işi veya incelenmiş idempotent SQL çalıştırılır ([RELEASE.md](RELEASE.md)).

### Veritabanına elle kolon eklemeyin

Elle eklenen kolon `__EFMigrationsHistory`'de görünmez ve migration "kolon zaten var" hatasıyla durur. `20261011082637_ApartmentAndUnitDetails` bu duruma karşı korumalıdır: her kolonu `ADD COLUMN IF NOT EXISTS` ile ekler, varsa tipini, NULL'luğunu ve varsayılanını modele hizalar. Yerel DB'de elle eklenen `properties.construction_year` (`bigint` → `integer`) ve `properties.neighborhood` (`varchar(200)` → `varchar(100)`) böyle düzeltildi. Yeni migration'larda bu koruma yoktur; kolonu her zaman migration ile ekleyin.

## Proje kökünden komutlar

.NET 10 SDK gerekir; `backend/dotnet-tools.json` EF CLI sürümünü **10.0.12** olarak sabitler. Helper tool restore yapar. Kaynak listesi, check ve SQL üretimi DB bağlantısı/şifresi istemez. `status` ve `update` yalnızca `.env` içindeki **yerel** veritabanını hedefler.

```bash
# Kayıtlı migration dosyalarını listele; DB'ye bağlanmaz
bash scripts/db.sh list

# Yerel DB'de uygulanan/bekleyen migration durumunu göster
bash scripts/db.sh status

# Entity/mapping değişmiş ama migration unutulmuşsa başarısız olur
bash scripts/db.sh check

# Model ve mapping değişikliklerinden yeni dosya üret; DB'ye uygulamaz
bash scripts/db.sh add AddSiteDescription

# Entity değiştiyse yalnız eklemeli migration'ı otomatik üret (dev.sh api bunu açılışta yapar)
bash scripts/db.sh sync

# İncelenebilir kurulum/yükseltme SQL'i üret; DB'ye uygulamaz
bash scripts/db.sh sql
# Çıktı: artifacts/database/buildingflow.sql (Git dışında)

# İncelenmiş migration'ları yerel DB'ye uygula; PostgreSQL açık olmalı
bash scripts/db.sh update
```

`AddSiteDescription` yalnızca isim örneğidir; bu turda örnek bir alan/migration eklenmedi. SQL dosyasını her sürüm için yeniden üretip dağıtım artifact'i olarak saklayın. Script migration geçmişine bakarak eksikleri uygular; yalnızca bu sırayla yönetilmiş şema için kullanılır, veritabanında elle yapılmış değişiklikleri uzlaştırmaz.

VS Code **Terminal → Run Task**: Migration list, Local migration status, Migration check, Migration SQL export, Local DB update görevleri aynı helper'ı çağırır. Entity değişikliği sonrası ekleme komutunu açıklayıcı bir migration adıyla terminalde çalıştırın.

## Yeni alan veya tablo geliştirme

1. Domain modelini ve Infrastructure EF mapping'ini değiştir. DTO/validasyon/API/UI etkisini de değerlendir.
2. `bash scripts/db.sh add AciklayiciMigrationAdi` çalıştır.
3. Üretilen **Up/Down** ve SQL'i incele. Zorunlu sekiz BaseEntity kolonunu, NOT NULL, index/FK ve audit kurallarını koru.
4. `bash scripts/db.sh check`, `bash scripts/test.sh` ve restore sonrası `python3 scripts/check-licenses.py` çalıştır. Test DB'si uygulama DB'sinden ayrıdır.
5. İncelenmiş değişiklikleri yerelde `bash scripts/db.sh update` ile uygula; migration dosyaları ve snapshot'ı birlikte sürümle.

| Değişiklik | Uygulama yaklaşımı |
| --- | --- |
| Alan ekleme | `AddColumn`; eski kayıtlar varsa nullable/default veya gerekçeli backfill |
| Alan adı değiştirme | `RenameColumn`; üretilmiş DropColumn + AddColumn'u otomatik kabul etme |
| Zorunlu yeni alan | Önce nullable ekle → mevcut satırları doldur → NOT NULL yap |
| Tür/uzunluk değiştirme | `AlterColumn`; dönüşemeyen veya sınırı aşan mevcut veriyi önce incele |
| Alan kaldırma | Kullanımı kaldır, veri taşıma/koruma kararını ver, ardından `DropColumn`; Down kaybolan değerleri geri getirmez |
| Index / FK / kısıt | MigrationBuilder index/FK/check operasyonları |
| Sabit referans verisi | İncelenmiş `InsertData`/`UpdateData` ve sabit kimlikler; sekiz zorunlu kolonu doldur |
| Mevcut veriyi dönüştürme / view / function / trigger | Sürümlü `migrationBuilder.Sql(...)`; PostgreSQL SQL'i migration içinde tutulur |

Uygulanmış migration dosyasını veya migration geçmişini değiştirmeyin; yeni düzeltme dosyası ekleyin. Migration içinde güncel entity sınıflarıyla veri taşıma yapmayın; eski migration yeni modelden bağımsız çalışabilmeli. `EnsureCreated`/veritabanını silip yeniden oluşturma migration akışının parçası değildir. Sekiz BaseEntity kolonu alan kaldırma işlemlerinin kapsamına alınmaz.

## Kurulum ve dağıtım

- **Yerel Docker kurulumu:** `bash scripts/start.sh`. PostgreSQL hazır olduktan sonra migration servisi API imajını `--migrate` ile çalıştırır; başarıyla bitmeden API başlamaz. Normal API başlangıcı migration çalıştırmaz. Mevcut volume'lar korunur.
- **Host geliştirme:** `bash scripts/dev.sh prepare` build/migration/npm kurulumunu birlikte hazırlar; yalnız DB güncellemesi için `scripts/db.sh update` yeterlidir.
- **Üretim için seçenekler:** İncelenmiş idempotent SQL veya CI'da üretilen EF migration bundle ayrı dağıtım adımında çalıştırılabilir. Üretim pipeline/bundle bu turda kurulmadı. İŞ11: `TerminatedSqlMigrationsSqlGenerator` (`UseBuildingFlowNpgsql`) ham `Sql()` ifadelerini `;` ile sonlandırır; aksi halde uygulanmış `20261007210343_UnifiedPropertyModel` idempotent betikte PostgreSQL sözdizimi hatası verir. `scripts/check-idempotent-sql.sh` betiği geçici izole veritabanına iki kez uygular ve siler. Üretim DB'sini altyapı sağlar; dağıtım kimliği DDL yetkisine, normal API kimliği gerekli veri erişim yetkisine sahip olur. Şifreler artifact'e/Git'e yazılmaz.

Gerçek PostgreSQL kabul testleri: boş şemaya kurulum, ilk migration'daki kaydın yeni sürümde korunması, aynı sıranın tekrar çalıştırılması, son model/snapshot uyumu ve tüm tablo audit kolonları. Sonuçlar [TESTING.md](TESTING.md) içinde.

Resmî belgeler: [EF migration akışı](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/), [migration yönetimi](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing), [kurulum ve SQL/bundle dağıtımı](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).


## 03 ortak yönetim modeli — 2026-10-08

UnifiedPropertyModel, ilk iki uygulanmış migration'ı değiştirmeden sites→properties, blocks→buildings, apartments→units, site_memberships→property_memberships rename işlemleri yapar. Eski kimlik/version/audit/status/iş alanları korunur; PropertyType SITE, UnitType APARTMENT backfill edilir. units.property_id önce nullable eklenir, eski bina ilişkisinden doldurulur, sonra NOT NULL yapılır. building_id nullable olur; direct Unit için ayrı partial unique index ve bina/property uyumu için composite FK eklenir. Kaynak Designer ve snapshot son modeli içerir.

Down ancak eski hiyerarşide temsil edilebilen verilerde çalışır. Doğrudan Unit, yeni Property/Unit türü veya kat sayısı metadata varsa veri kaybını engellemek için hata üretir; silinmiş geçmiş de dahildir. Gerçek ayrı PG testinde eski dört tablonun verisi yükseltildi, güvenli Down/Up tekrarında audit korundu ve yeni apartman kaydıyla kayıplı Down reddedildi. Yerel DB update uygulandı; önce/sonra ortak alan ve parent kimlik özetleri 6 mevcut satırda aynıydı. .env ve kullanıcı parolası korunur.

## 05 aidat migration — 2026-10-08

DuesAndCharges iki yeni finans tablosu ekler; mevcut alan/tablolarda drop/rename/veri güncellemesi yoktur. Sekiz ortak NOT NULL audit kolonu, decimal(10,2), Unit/Assessment/Property uyumlu composite FK, istek UUID tekilliği, iptal edilmemiş daire/dönem partial unique index ve tutar/para birimi/dönem/iptal/geçmiş check’leri kurulur. Down, finans kaydı varsa silme işlemine başlamadan hata verir; otomatik veri kaybına izin verilmez. Önceki migration’lar değiştirilmedi.

Ayrı PostgreSQL test DB’sindeki ilk kurulum/yükseltme/tekrar ve veri bulunan Down kontrolleri geçtikten sonra yerel update uygulandı. Önce/sonra altı eski tablodaki **80 kaydın** sayısı ve satır/audit/version anonim özetleri birebir aynıydı: 7 Property, 4 Building, 53 Unit, 7 üyelik, 4 Person ve 5 ilişki (silinmiş geçmiş dahil). Yeni tablolar başlangıçta boştur. `.env` ve geliştirme hesabı korunur.

## 06 — PaymentsAndAllocations — 2026-10-09

`20261009040909_PaymentsAndAllocations` yeni migration/snapshot: payments + payment_allocations ve dues_charges.paid_amount numeric(10,2) NOT NULL DEFAULT 0. Eski borçlar sıfır ödeme ile başlar; audit/version/tutar değişmez. Property/Unit/Payment/Charge composite FK, request ve aktif havale referansı tekilliği, para/iptal/history check'leri ve sekiz ortak alan uygulanır. Önceki uygulanmış migration'lar değişmedi.

Ayrı gerçek PostgreSQL testleri eski Dues şemasında borç/audit backfill, tüm uygulama tablolarının ortak şeması, scope FK/check, snapshot eşliği, ilk kurulum/yükseltme/tekrar, boş Down/re-Up ve **ödeme geçmişi varsa silmeye başlamadan Down reddi** doğrular. Cancelled kayıt da finans geçmişidir. Down, daha önce silinmiş veriyi kurtarmaz; veri varsa yedek/ileri düzeltme gerekir. EF birden fazla migration'ı geri alırken boş yeni aşamayı geri alıp eski bir aşamanın geçmiş guard'ında durabilir; test bu durumda güncel migration'ları yeniden uygular.

Yerel DB migration'ı bu teslimde uygulandı; yeni kurulum/yükseltme için kökte `bash scripts/db.sh update` kullanın, çalışan API'yi güncel modelle yeniden başlatın. SQL'i dağıtım öncesi `bash scripts/db.sh sql` ile tekrar üretin. [Tahsilat sözleşmesi](api/payments.md) ve [gerçek test kanıtları](TESTING.md).

## 06.1 — DuesPartyHistoryAndCancellationReasons

20261009123058 migration yeni `dues_charge_parties` ve `dues_cancellation_reasons` tablolarını ortak sekiz NOT NULL BaseEntity alanıyla ekler. Composite FK kişi/ilişki/borç/Unit/Property kapsamını, partial unique index bir muhatabı korur. Assessment debtor_type mevcut veride UNIT; Charge parties_recorded false. Eski iptal metni, tutar, paid_amount, audit/version değiştirilmez; mevcut kişiden geçmiş snapshot uydurulmaz. Beş katalog kaydı sabit migration audit'iyle seed edilir.

Down yeni parti kaydı, recorded bayrağı, neden metadata'sı veya kişisel muhatap varsa veri kaybını reddeder. Yalnız seed içeren ve yeni tarihçe olmayan kurulum önceki sürüme güvenli dönebilir. Yeni alanları drop etmekte kullanılan Down korumasını atlamayın. Önceki uygulanmış migration dosyaları korunmuştur. PaymentMigrationTests eski finans migration Down'ını denediğinde daha yeni boş modülün önce geri alınabileceğini hesaba katıp güncel şemayı tekrar yükseltir; eski migration değiştirilmez.

Ayrı PostgreSQL testleri eski satırın JSON içeriğini yeni kolonlar hariç tam karşılaştırır; migration tekrar uygulama/Down koruması, tüm tabloların zorunlu alanları ve pending model testi çalışır. Yerel yükseltmede 10 eski uygulama tablosunun tüm satır/audit/version hashleri karşılaştırılır; TESTING.md sonuç kaydıdır.

## 07 — ExpensesAndBudgets

`20261009213050_ExpensesAndBudgets`, önceki `20261009123058_DuesPartyHistoryAndCancellationReasons` üzerine eklenir. `expenses`, `property_budgets`, `budget_revisions` ve ortak sekiz NOT NULL kolon, decimal(10,2), kategori/dönem/TRY/para/iptal/history check, UUID ve alan/ay/kategori tekilliği, scope composite FK kurar. Up eski tablo/satırları değiştirmez. Uygulanmış migration kaynakları değişmedi; snapshot ve Designer yeni sürümle kaydedilir.

Down, üç tablodan herhangi birinde kayıt varsa **ilk drop öncesinde** reddeder. İptal gider ve sıfır plan/revizyon geçmişi de korunur; ileri düzeltme/yedek gerekir. Ana DB üzerinde rollback denenmez. Ayrı PostgreSQL şemalarında eski satır/audit JSON eşliği, ilk kurulum/yükseltme/tekrar, boş Down/re-Up, dolu geçmişte reddetme, tüm BaseEntity sütunları/FK/check ve model eşliği doğrulanır. Tarihsel DuesPartyMigrationTests kendi migration hedefini açıkça seçer; ilerideki migration'ların boş Down'ını eski fazın atomiklik kanıtıyla karıştırmaz. Yerel yükseltme öncesi/sonrası 12 eski tablonun tüm satır/audit hash'leri aynı kaldı. [API sözleşmesi](api/expenses.md) ve [test kanıtı](TESTING.md).


## 08 — AnnouncementsAndDocuments — 10.10.2026

Yeni son migration `20261010043250_AnnouncementsAndDocuments`: `announcements` ve `stored_documents`, sekiz ortak BaseEntity NOT NULL alanı, aynı Property duyuru/ek composite FK ve sorgu/istek tekilliği indexleri. Önceki migration'ları değiştirme. Snapshot/Designer birlikte kaydedildi; metadata DB'de, dosya içeriği özel MinIO'dadır. Down, soft-delete dahil herhangi bir duyuru/belge geçmişi varsa ilk drop öncesinde reddeder; nesne içeriğini geri alma işlemi değildir.

Ayrı gerçek PostgreSQL kurulum/yükseltme/tekrar/boş Down/re-Up ve veri/audit/model eşliği testleri geçti. Yerel `bash scripts/db.sh update` uygulandı; tekrarında eksik migration yok ve15 eski tablo hash'i aynı. İlk canlı yükseltme öncesi hash alınamadı; ilk upgrade veri koruma kanıtı ayrı test DB'sindedir. SQL dağıtımı için mevcut idempotent script akışını kullanın. Ayrıntı [TESTING.md](TESTING.md), [API](api/communication.md).

## İŞ10 sayaç geçmişi

20261011064227_MetersAndReadings: meters/meter_readings/meter_reading_revisions, ortak audit/version, composite FK, precision12/3, tarih tekilliği, immutable SQL revizyon trigger'ı. Dolu sayaç/okuma tablolarında Down veri kaybını engeller. İlk kurulum/yükseltme/tekrar ve tablo sayısı modelden alınan NOT NULL şema taraması ayrı PostgreSQL'de test edilir; sabit sayı kabul kriteri değildir. Yerel11.10.2026 yükseltmesinde17 önceki tablo hash'i eşit; yeni messaging+meter tabloları eklendi.
