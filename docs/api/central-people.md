# Merkezi Person backend — PERSON FAZ 03

16.10.2026. GitHub [#8](https://github.com/AkaBayraK/BuildingFlow/issues/8). Tasarım: [FAZ02](../refactor/person/phase-02-report.md). Bu belge uygulanmış **backend genişletmesini** açıklar; gerçek veri geçişi veya ekran teslimi değildir.

## Açılma ve veri sahipliği

`PersonRefactor:Enabled` varsayılan false. Ortam karşılığı `PersonRefactor__Enabled=false`, `.env.example` içinde belgeli; gerçek `.env` değiştirilmedi. Bayrak kapalıyken kimlikli v2 çağrı404; auth policy401/403 kuralları korunur. Mevcut `/api/v1/properties/{id}/people`, kişi kodu, onboarding, MobileProfile, finans ve mobil makbuzlar aynı sözleşmeyle devam eder. Eski veriyi v2'ye otomatik bağlayan fallback veya tenant seed yoktur.

`CentralPerson`, PropertyId taşımaz; `persons` tablosunda tenant içinde merkezidir. Mevcut `Person`/`people` legacy otoritesi bu aşamada korunur. Legacy ile canonical arasında çift yazım, otomatik kişi birleştirme veya kaynaklardan alan kopyalama yapılmadı. Yeni tenantlar, üyelikler ve doğrulanmış hesap bağları yalnız test fixture'larında kuruldu; gerçek tenant→Property haritası henüz onaylanmadı. Feature'ı açmak bu haritayı üretmez.

FAZ03'te tenantın ortak varsayılan telefon/e-posta alanları canonical kayıtta optional olarak tutulur. Bunlar alan-özel PersonPropertyProfile yerine geçmez; ortak telefon/e-posta/kimlik numarası benzersiz veya merge anahtarı değildir. Profile/alan ayrımı ve legacy view version adapter'ı FAZ04 geçişinin kabul kapısıdır. Hedef tasarımda bu detayın uygulama uyarlaması FAZ03 raporunda kayıtlıdır.

## Uçlar

Kök `/api/v2/tenants/{tenantId}`. Yeni API mevcut ErrorCatalog/ProblemDetails ve yetkilendirme altyapısını kullanır; yeni bir HTTP client veya hata sistemi yoktur.

| Method ve yol | Uygulanan davranış |
|---|---|
| GET `/people` | Yönetici tenant genelinde liste. page1–1000000,pageSize1–100,search≤200,name/createdAt ve asc/desc. Literal ILIKE ad/telefon/e-posta araması, UUID tie-break ve filtre kapsamındaki totalCount. RepeatableRead liste snapshot'ı. |
| GET `/people/{personId}` | Aynı tenant içinde yönetici detayı; foreign/soft-delete404. |
| POST `/people` | `{operationId,profile}`. Kimlikli yönetici ve aktif tenant kapsamı; yeni server UUID,version1;200 `{record,replayed,appliedVersion}`. |
| PUT `/people/{personId}` | `{operationId,expectedVersion,profile,isEnabled}`. Stale version409. Yeni UUID ile no-op güncelleme bile audit/version artırır. |
| GET `/me/person` | Sunucuda issuer+subject+tenant üyeliği+aktif hesap bağıyla kendi NATURAL kaydını çözer. Liste/detay yönetici izni vermez. Query parametresiyle başka kişi seçilemez400. Başkasının kartına override yok. |

`profile`: kind(NATURAL/LEGAL),citizenship(DOMESTIC/FOREIGN),countryCode,identityNumber,firstName,lastName; optional birthDate/gender/phoneNumber/email. Ülke/kimlik/tarih/gerçek-tüzel kuralları v1 ile ortak `PersonProfileValidation` üzerinden uygulanır; v1 telefon/e-postayı zorunlu tutmaya devam eder, v2 optional kabul eder. Profile/create/update DTO'ları tanımsız alanı reddeder: tenantId/audit/rol boolean'ı/user link payload ile verilemez. İstemci verified durumu üretemez. Kayıt doğrulaması gerçek kimlik doğrulama servisinden teyit değildir.

Liste DTO: id,tenantId,displayName,kind,isEnabled,version,createdAt. Kimlik numarası/telefon/e-posta veya subject listede dönmez. Yönetici detayında profile bulunur. Self DTO: id,tenantId,displayName,kind,maskedIdentity,canEdit=false. Kimlik numarasının yalnız son4hanesi görünür; başka kişilerin verisi, e-posta/telefon, subject, HMAC veya iç makbuz dönmez. Self PUT/PATCH/POST yoktur; merkezi Person mutasyonları yönetici policy'siyle korunur. MobileProfile düzenlemesi canonical Person'a bağlanmadı.

## Yetki ve kimlik

Gerçek JWT doğrulaması sonrası `CurrentUserService.Issuer`, imzalı `iss` claim'inden okunur. Account unique(issuer,subject); aynı subject farklı issuer'da aynı hesap sayılmaz. Eski ICurrentUserService uygulamalarında default Issuer=null, v1 davranışı değişmez; yeni store issuer yoksa erişim vermez.

- SUPER_ADMIN mevcut platform istisnasıyla açıkça seçilen aktif tenantı yönetebilir.
- SITE_ADMIN/MANAGER, aktif PersonUserAccount + aktif PersonTenantMembership + aktif tenant ile tenantın **tüm** canonical kişilerini okuyup bu modülün create/update işlemlerini yapar. Sahte realm/client rol claim'i yeterli değildir. Bu aşamadaki tenant üyeliği açıktan provision edilmesi gereken backend temelidir; otomatik üyelik/rol verilmez.
- OWNER/TENANT/RESIDENT/ACCOUNTANT genel liste, yönetici detay ve mutasyonda403. Kimlikli hesap yalnız doğrulanmış kendi bağıyla self-read alabilir. Hesap/tenant/üyelik silinmesi veya pasifleşmesi self dahil erişimi kaldırır.
- Self link enabled,endedAt=null,linkedAt≤şimdi ve Person enabled,NATURAL ister. Bağ yoksa404. Aktif hesap bağı varken kişiyi pasifleştirmek veya LEGAL'e çevirmek409; önce yetkili bağ yaşam döngüsü gerekir.

Tarihli yönetim görevleri ve Property→Tenant mapping FAZ04/05 kapsamındadır. Bu aşama bunların yerine sessiz yetki üretmez. Geçiş sonrasında assignment/alan kısıtları aynı tenant yetki kontrolüne eklenmelidir; görev/üyelik provisioning endpoint'i bu fazda açılmadı.

## Transaction, idempotency ve veri koruması

Yeni yazım, tenant parent row lock altında yeniden yetkilendirilir. Tenant+actor issuer+subject+operationId receipt namespace'i; normalize tenant/hedef/version/profile/enabled fingerprint'e dahildir. Aynı içerik replay tek iş+tek receipt, farklı içerik409. Replay yetkiyi yeniden denetler ve **güncel kaydı**, appliedVersion ise ilk işlemin uyguladığı sürümü döndürür. Record+receipt tek transaction. Revoked kullanıcı replay okuyamaz. SQL trigger receipt UPDATE/DELETE'yi reddeder.

Eski mobile fingerprint/makbuz serileştirmesine dokunulmadı. Yeni CentralPerson API'si eski outbox'a yönlendirilmez. Soft-delete edilen canonical kayıt yeniden yaratılmaz; makbuz replay hedefi kayıp/soft-delete ise409. Aynı isim/telefon/e-posta/kimlik numarasıyla iki kayıt ayrı UUID alabilir; otomatik merge yoktur.

## Migration ve kabul

`20261016122659_CentralPersonBackend` yalnız6yeni tablo: person_tenants,person_user_accounts,person_tenant_memberships,persons,person_account_links,person_operation_receipts. Ortak8NOTNULL alan, tenant/person ve tenant/membership compositeFK, issuer+subject tekilliği, aktif account↔Person bağ tekilliği, profile/tarih/receipt kısıtları. Uygulanmış eski migrationlar değişmedi. Boş Down/Up mümkün; yeni tablolardan herhangi birinde geçmiş varsa ilk drop öncesi Down reddi.

Migration yalnız izole test veritabanına uygulandı; ana DB'ye update veya servis restart yapılmadı. Dikkat: feature bayrağı yalnız endpoint erişim kapısıdır; `Database:AutoMigrate` ayrı ayardır. Sonraki dev startup AutoMigrate açıksa yeni **boş** tabloları oluşturabilir; tenant atama/backfill yine yapılmaz.

Bu tur215izole PostgreSQL/fixture storage integration +150unit,112FEtest; backend build0hata/uyarı;FEtypecheck/build ve329lisansmetadata PASS. Tam integration suite çalıştırılmadı;215seçilen kapsamdadır. Cihaz/görsel kabul, gerçek tenant geçişi ve production aktivasyonu yapılmadı. Ayrıntılı kanıt [TESTING](../TESTING.md) ve [FAZ03 raporu](../refactor/person/phase-03-report.md).


## FAZ04 gölge import koruması —17.10.2026

LegacyPersonMap ile bağlı import kişileri henüz legacy yazım kaynağını izleyen gölge snapshot'tır. Canonical update bu kişilerde409 BF_RECORD_CHANGED döndürür; bağımsız ikinci yazım otoritesi açılmaz. V1 PeopleStore ve mevcut ekranlar değişmedi. Ayrı admin dry-run/apply ve yönetici scoped compatible read: [person-transition.md](person-transition.md). Bu bir canlı cutover değildir.


## FAZ05 yönetim görevi —17.10.2026
Tenant bazında açıkça ENFORCED seçilmişse merkezi kişi yönetimi gerçek rol/aktif üyelik yanında en az bir yetkili Property'de güncel SITE_ADMIN/MANAGER görevi ister. ACCOUNTANT kişi yetkisi kazanmaz; self-read readonly kalır. Aktif/gelecek görevi olan kişi pasife/LEGAL'e çevrilemez. Legacy/SHADOW varsayılan erişimi korur. [Yönetim görevlendirmeleri](management-assignments.md).
