# PERSON REFACTOR — FAZ 03: Person backend genişletmesi

16.10.2026. Kullanıcı FAZ02'yi onaylayıp FAZ03'e geçiş izni verdi. GitHub [#7](https://github.com/AkaBayraK/BuildingFlow/issues/7) Done; aktif [#8](https://github.com/AkaBayraK/BuildingFlow/issues/8) teknik teslim sonrası In review. Kanonik paket toplam11faz; FAZ04 başlamadı. [Uygulanmış API sözleşmesi](../../api/central-people.md).

## Uygulananlar

- Property'den bağımsız CentralPerson domain'i ve tenant scope temeli kuruldu. Mevcut Person/people tipi ve ilişkileri legacy olarak korundu; kör rename/drop veya gerçek veri taşıma yok.
- Minimal issuer+subject hesabı, tenant üyeliği ve aynı tenant içinde aktif tekil account↔Person bağı eklendi. Keycloak parolası/tokenı domain veya SQLite'a kopyalanmadı.
- Yönetici için tenant genelinde merkezi create/update/search/list/detail; sakin/ev sahibi/kiracı için yalnız server-resolved, maskeli, salt okunur kendi kartı. ACCOUNTANT genel kişilere erişmez. Ayrıntılı yeni kişi permission sistemi eklenmedi.
- Yeni API unknown grant/audit/role alanlarını reddeder. Aktif tenant/hesap/üyelik ve gerçek realm rolü kontrolü; global SUPER_ADMIN istisnası açık hedef tenantta korunur.
- Version409, UUID/fingerprint retry, aynı tenant parent lock, atomik record+receipt ve immutable makbuz eklendi. Güncel replay DTO ile uygulanan ilk version ayrı döner.
- Ortak PersonProfileValidation çıkarıldı; eski PeopleService aynı doğrulamayı çağırır. V1 zorunlu iletişim, v2 optional ortak iletişim; format/kimlik/ülke/tarih kuralları ortaktır. Aynı telefon/e-posta/kimlik numarasıyla ayrı kişiler engellenmez veya otomatik birleştirilmez.
- Sadece eklemeli migration+Designer+snapshot:6yeni tablo, sekiz zorunlu NOTNULL alan, scopeFK/tekillik/kısıtlar ve dolu Down koruması. Eski migration kaynak hashleri aynı.
- `PersonRefactor:Enabled` varsayılan kapalı. Örnek ortamda false; gerçek .env, alanlar, kişiler, roller veya üyelikler değiştirilmedi. V1 kullanıcıları yeni modele otomatik geçirilmedi.

## FAZ02'den bu faza kapsam ve uyarlamalar

Gerçek Tenant→Property eşlemesi kullanıcı tarafından henüz verilmedi. Bu faz boş canonical backend ve izole test fixture'ıyla tamamlandı; bütün mevcut alanları tek tenantta toplama, her alanı tenant sayma veya legacyPerson backfill yoktur. Bu nedenle mevcut merkez/mobil kişi ekranı henüz yeni merkezi API'ye bağlanmış değildir. Bu sınırlama saklanarak uçtan uca ürün geçişi tamamlandı denilmez.

Fiziksel hedef `persons`; eski `people` korunur. `CentralPerson` adı eski domain tipini aynı anda bozmayı engeller. Alan-özel PersonPropertyProfile,LegacyPersonMap/legacy_view_version,UnitOwnership/Tenancy/Residency/PrimaryContact ve ManagementAssignment bu fazda uygulanmadı; FAZ04/05 veri geçişi için tasarım kapılarıdır. İşlem ve yetki revizyonlu mobil DTO/SQLite geçişi FAZ08/09'dur. Eski API adapter'ı bu fazda davranışı değişmemiş mevcut v1 servistir; canonical alias adapter'ı tamamlanmış sayılmaz.

Canonical telefon/e-posta isteğe bağlı tenantın ortak varsayılan iletişim alanı olarak eklendi. Bu, alan-özel profile geçişinin yerine geçmez. Create/update/search ihtiyaçları ve ortak iletişimli kişiler bu aşamada gerçek API'de test edilebilir. V1 required-contact doğrulaması gevşetilmedi. Alan-özel görünürlük ve iletişim sahipliği FAZ04 mapper'ında korunmalıdır.

TenantMembership bu modülün explicit erişim temelidir. Gerçek üyelik veya görev oluşturma endpoint'i eklenmedi; test fixture'ı dışında tenant/hesap/bağ seed edilmedi. Yönetici kapsamı şu an gerçek realm rolü+aktif account/tenantmembership; tarihli assignment enforcement FAZ05'te eklenecek. Özellik kapalı olduğundan mevcut erişimler bu yeni modele geçirilmedi.

## Değişen dosyalar

Yeni domain: `backend/src/BuildingFlow.Domain/CentralPerson.cs`.

Yeni Application: `CentralPeople.cs`, `PersonProfileValidation.cs`. Mevcut `People.cs` ortak doğrulamaya delege eder; `Abstractions.cs` Issuer default property ile eski implementasyonları bozmaz.

Yeni Infrastructure: `CentralPeopleStore.cs`, `CentralPersonConfigurations.cs`; mevcut DI kaydı genişledi. Yeni migration `20261016122659_CentralPersonBackend.cs` + Designer + snapshot.

Yeni API: `CentralPeopleEndpoints.cs`; Program kayıtları; CurrentUserService imzalı issuer claim'i sağlar. `.env.example` yalnız default kapalı bayrağı belgeler.

Yeni testler: `CentralPeopleTests.cs`, `CentralPersonMigrationTests.cs`. API/progress/testing ve bu rapor güncellendi. Dependency manifestleri ve lock dosyaları aynı; yeni paket/font/ikon/görsel/DB extension yok. Next build'in ürettiği next-env.d.ts import değişikliği başlangıç hash'i doğrulanarak geri alındı; frontend kaynak davranışı değiştirilmedi.

## Gerçek test sonuçları

| Kontrol | Bu tur sonucu |
|---|---|
| .NET solution build | PASS,0hata/0uyarı |
| .NET birim testleri |150/150,0atlama |
| Gerçek izole PostgreSQL ve fixture storage integration |215/215,0başarısız/0atlama |
| Merkez npm test |112/112 |
| Merkez typecheck | PASS |
| Merkez production build | PASS |
| Lisans metadata kontrolü |329paket sürümü PASS; yeni bağımlılık yok |
| Kaynak/migration/paket koruması ve whitespace | PASS; önceki migrationlar ve paket manifest/lock hashleri korundu |

Loglar yalnız geçici `/tmp/bf-person03/` altında; sır veya gerçek kişi verisi rapora/GitHub'a yazılmadı. Koşum: `/tmp/bf-person03/test-backend.sh` geçici DB/role üretir, yalnız sahip olduğu kaynağı kaldırır. Ana kullanıcı DB'sine mutation veya rol ataması yapılmadı. Integration seçimi CentralPeople/CentralPerson,PeopleApi/PersonProfiles/PersonCode/UserManagement,Mobile,Migration/Persistence/PropertyModel/Authentication/ReleaseSecurity testleridir; **tam integration suite değildir**.

Yeni kabul kanıtları:

- Default bayrak kapalı; canonical endpoint404 ve canonical kayda yazım yok.
- Herhangi bir Property olmadan tenant içinde merkezi CRUD; iki ortak iletişim/aynı kimlik kaydı ayrıUUID; ad/güncelleme/version/audit korunur.
- Aynı operation'ın eşzamanlı create tekrarında tek record+receipt; farklı içerik409. Sonraki update sonrası replay güncel record ve ilk appliedVersion döndürür.
- Aynı expectedVersion ile iki güncellemeden biri başarılı,biri409; no-op yeni işlem version artırır.
- RESIDENT/OWNER/TENANT/ACCOUNTANT ve sahte realm/client rolü genel liste/detay/yazıma403; anonim401; foreign tenant/yanlış hedef/aynısubject farklıissuer404.
- Self yalnız kendi aktif,başlamış NATURAL link'inden çözülür; maske ve canEdit=false; başkaPersonId override400, selfPUT405, yönetici mutasyonu olmayan rollerde write403.
- Üyelik/hesap/tenant iptali veya soft-delete; aktif link bitişi; gelecekte başlayacak link; soft-deletePerson erişimi kapatır. Aktif link varken pasifleştirme veya LEGAL dönüşümü409.
- Literal yüzde/altçizgi araması, kararlı pagination, telefon/e-posta araması ve listede PII yokluğu. Bozuk tarih/ülke/kimlik/iletişim ve unknown grant alanı400.
- Gerçek DB'de yanlış tenant compositeFK ve aktif account↔Person tekilliği; receipt UPDATE trigger reddi.
- Önceki migration seviyesinde legacyPerson JSON/audit/version aynı; yeni alanlar bile eski tabloya eklenmez. İlk kurulum/upgrade/repeat,emptyDown/reUp,altıtablonun8NOTNULLalanı ve doluDownilkdropöncesireddi.

Fortify/SAST/SCA/CVE/secret scanner, native mobil binary/görsel/cihaz ve canlı production kabulü bu tur ÇALIŞTIRILMADI. Lisans kontrolü CVE taraması değildir. Mobil kaynak/DTO/SQLite değişmediğinden mobil export yeniden çalıştırılmadı; backend Mobile regresyonları çalıştırıldı.

## Migration ve rollback

Ana DB'ye migration update veya servis restart yapılmadı. Bu yeni migration yalnız fixture DB'lerine uygulandı. Feature flag API kapısıdır; Database:AutoMigrate ayrı ayardır. Kullanıcı daha sonra dev API'yi AutoMigrate açık başlatırsa yeni boş tablolar oluşturulabilir; bununla tenant assignment veya veri geçişi gerçekleşmez.

Boş modülde Down/Up test edildi. Yeni tabloda veri varsa Down ilk drop öncesi reddeder. Legacy tablolar untouched kaldığından rollback için onları geri yazma gerekmez; özellik kapalı tutulur. Canonical veri oluştuktan sonra silerek rollback değil, quiesce/ileri düzeltme veya doğrulanmış izole restore planı gerekir. Canlıya açma/tenant seed/backfill bu teslimle yapılmadı.

## Kalan gerçek işler ve riskler

1. Onaylı Tenant→Property haritası ve gerçek kullanıcı hesabının issuer eşlemesi olmadan feature açılmamalı. LAN ve localhost issuer'ları aynı subject diye otomatik birleştirilmez.
2. Legacy kimlik/card version adapter'ı ve veri backfill FAZ04. Eski kişi kaydı henüz merkezi canonical kayda dönüşmedi; yeni v2 boş, ayrı onaylı veriyle doldurulmalıdır.
3. İlişki/primary/pay migration'ı, yönetim ataması, eski PersonLinkedUserId girişlerinin ortak bağ servisine geçirilmesi ve grant/revoke FAZ04/05'tir. Yeni self-link yazım endpoint'i yoktur; yalnız DB kısıtları ve güvenli okuma temeli vardır.
4. Merkezi Person pasifleştirmesi bu fazda aktif hesap bağına karşı korumalıdır. Yeni Unit/Assignment tabloları eklendiğinde onların aktif/gelecek ilişkileri de kapanış kuralına katılmalıdır.
5. Merkezi/mobil ekranlar FAZ06–09 kapsamında bağlanacak; bugün ekranın tüm tenant kişilerini göstermesi teslim edilmiş sayılmaz. Mevcut kullanıcı akışları değiştirilmedi.
6. Aynı kimlik numaralı kayıtlar olası mükerrer olarak kalır; gerçek veri doğrulaması veya merge yapılmadı. Kişi numarası gerçekliği doğrulanmış iddiası yoktur.

## Görev ve faz kapısı

FAZ03 resmi6adımı tamamlandı: önkoşul/kapsam,kod planı,backend uygulama,gerçek testler,güvenlik/uyumluluk kontrolü,rapor/görev kaydı. Teknik teslim6/6=%100,00; kullanıcı kabulü beklenir. Önceki kabul edilmiş2/11faz=%18,18; bu sayaç ürün yayın hazır olma oranı değildir. Mevcut İŞ/UI/mobil roadmap paydaları değiştirilmedi; Person işi #6–#16 canonical faz görevleriyle izlenir.

GitHub#8'e yapılanlar ve test kanıtı maddelerle işlendi; In review. FAZ04 başlamadı. Bu raporun kabulü ve FAZ04 başlatma onayı beklenir. Dalmain; commit/push/merge/reset/stash yok. Kullanılan yerel yönergeler akadev-phase/contract/verify/security; alt agent yok.

Giriş/çıkış/toplamtoken ve kalancontext:BİLGİ MEVCUT DEĞİL. Hesap kotası bu turun token tüketimi değildir.
