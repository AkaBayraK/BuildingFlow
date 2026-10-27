# Kişiler merkezi — PERSON FAZ06

18.10.2026. Merkezi kişi kartı tenant kapsamındadır. Property ilişkileri yalnız aktörün yetkili olduğu alanlardan gelir. Mevcut `/api/v1` Property kişi ekranı aktarılmış verinin yazma kaynağı olmaya devam eder.

## Açılış ve yetki

Web: `BUILDINGFLOW_PERSON_CENTER_ENABLED=true` hem build hem server ortamında gerekir; varsayılan false. Değişiklikten sonra web yeniden derlenir. `/people`, `/people/{tenantId}` ve `/people/{tenantId}/{personId}` rotaları; bayrak kapalıyken mevcut alan seçimi korunur, yeni alt rotalar `/people` sayfasına döner. Sunucu bileşenleri kişi verisi okumaz; istemci AuthGate ve backend kontrollü istek kullanır. URL'de UUID dışında kişi adı/kimlik numarası yoktur.

API: `PersonRefactor:Enabled` gerekir. SUPER_ADMIN aktif tenantlarda; SITE_ADMIN/MANAGER exact issuer+subject aktif hesap ve tenant üyeliği kapsamında kişi okur. İlişkilerde ayrıca aktif Property üyeliği ve PersonPropertyScope gerekir. ENFORCED tenantlarda önceki fazın tarihli görev şartı devam eder. OWNER/TENANT/RESIDENT/ACCOUNTANT genel merkeze erişemez. Önceki `/me/person` maskeli salt okunur davranışı korunur.

## Okuma sözleşmesi

Tüm uçlar `/api/v2` altında, kimlik doğrulaması ve `CanManagePeople` politikasıyla korunur. Erişilemeyen nesne404; yanlış rol403; hatalı sayfalama400. Sayfalama page>=1, page<=100000, pageSize1–100; sıralama kararlı UUID bağlayıcısı içerir.

| GET yolu | Sonuç |
| --- | --- |
| `/person-workspaces` | Yetkili müşteri seçenekleri; sayfalı |
| `/tenants/{t}/person-workspace` | Yetkili müşteri id/ad |
| `/tenants/{t}/person-properties` | Alan seçenekleri; `canManageAssignments`, `canWriteRelations` |
| `/tenants/{t}/properties/{p}/person-unit-choices` | Aynı alandaki aktif bölüm seçenekleri |
| `/tenants/{t}/people/{person}/center` | Kişi kartı ve sunucunun hesapladığı yetenekler |
| `/tenants/{t}/people/{person}/account-status` | Yalnız `isLinked`, `linkedAt` |
| `/tenants/{t}/people/{person}/unit-relations` | Üç türün tek sayfalı görünümü; yalnız izinli alanlar |
| `/tenants/{t}/people/{person}/unit-relations/{kind}/{id}` | Aynı kapsamda tek ilişki |
| `/tenants/{t}/people/{person}/management-duties` | Kişinin izinli alanlardaki görevleri; sayfalı |

Merkez DTO: `person`, `canEdit`, `isImported`, `sourceChanged`, `legacyPropertyId`, `legacyPersonId`, `assignmentsAvailable`, `canCreateUnitRelations`, `canManageAssignments`. Eski kaynak bağlantısı yalnız o Property'ye de yetki varsa döner. Kaynak değiştiğinde gölge kart güncel kabul edilmez; arayüz uyarı ve kaynak bağlantısı verir. İstemci bozuk/başka tenant veya kişi DTO'sunu reddeder; aktör/rota değişiminde önceki veri temizlenir, istek iptal edilir. PII localStorage'a yazılmaz.

İlişki satırı OWNER/TENANT/RESIDENT, başlangıç/bitiş, birincil iletişim, tam sayı pay/payda **ondalık string**, version, status ve `canEnd` taşır. Number'a dönüştürülmeyen paylar 64 bit doğruluğunu korur; bilinmeyen pay null/null kalır. Yönetim satırı `assignment`, `propertyName`, `canManage` içerir. Hesap sekmesi issuer/subject/rol/parola ve hesap değiştirme komutu sunmaz.

## Yazım ve geçiş sınırı

Kişi oluşturma/düzenleme önceki [merkezi API](central-people.md), görev ekleme/bitirme/iptal [yönetim API'si](management-assignments.md) üzerindendir. Kişide malik/kiracı/yönetici checkbox'ı yoktur. Görev yazımında yalnız SITE_ADMIN/SUPER_ADMIN ve önceki yükseltme/son yönetici korumaları geçerlidir.

Yeni bölüm ilişkisi yazımı ayrıca `PersonRefactor:UnitRelationWritesEnabled=true` ister, varsayılan false. Yalnız **native kişi + eski kişi/ilişki geçmişi hiç bulunmayan Property** kabul edilir; silinmiş eski kayıt da engeldir. Aktarılmış gölge ilişki/kart salt okunurdur. Native ilişki geçmişi bulunan alanda eski kişi oluşturma/ilişki yazımı kilit altında409 verir. Eski oluşturma ve yeni ilişki işlemi aynı Property kilidini paylaşır; kontrol ile yazım arasında ikinci yazar açılamaz.

- POST `/tenants/{t}/people/{person}/unit-relations`: `operationId`, `propertyId`, `unitId`, `kind`, `startDate`, nullable `endDate`, `isPrimaryContact`, nullable string `shareNumerator`, `shareDenominator`.
- POST aynı yol `/{kind}/{id}/end`: `operationId`, `expectedVersion`, `endDate`. DateOnly uçları dahil; bir kez bitirme. Bir tür diğerini otomatik üretmez. LEGAL sakin olamaz.
- Transaction tenant→Property kilidi alır; kilit sonrası yetki tekrar doğrulanır, aktif kişi/bölüm/üst bina ve scope denetlenir. Exact share/overlap/birincil iletişim mevcut DB trigger'larıyla korunur. Parent version artırılır. Güncel/gelecek native ilişkiler Property/Unit kapatma/silmeyi ve kişi pasife almayı; sakinlik NATURAL→LEGAL dönüşümünü engeller.
- UUID+aktör+tenant ve fingerprint değişmez makbuzda saklanır. Aynı içerik retry tek etki; farklı içerik veya staleversion409. Replay güncel kayıtla ilk `appliedVersion` döndürür. Girdide tanımsız alan reddedilir. UI belirsiz sonucu otomatik tekrar etmez; aynı içerik/UUID ile kullanıcı retry yapabilir. 409'da bitirme/düzenleme güncel version açıkça yüklenir. Kalıcı offline outbox bu fazda yoktur; panel kapanınca önce sonucu yenileyerek kontrol etmek gerekir.

- BF-MULTI FAZ 01 (22.10.2026): yeni ilişki aktörün etkin `PersonUserAccount` kaydıyla `VERIFIED`, `verifiedByAccountId` ve `verifiedAt` ile yazılır. Aktörün canonical hesabı yoksa (ör. hesabı bağlanmamış SUPER_ADMIN) yazım 403 olur; doğrulayan uydurulmaz. İstek/yanıt JSON alanları **değişmedi**; doğrulama ve sözleşme alanları API'de henüz gösterilmez (FAZ 02/03). Malik/kiracının kendi ilişkisini yazma yolu yoktur.

Native ilişkiler henüz eski finans/mobil tüketicilere yansıtılmaz. Bu nedenle canlı verisi olan Altunbayrak Rezidans'ta bu yazım kapısı açılmadı ve v1 değiştirilmedi. Bu faz genel canlı geçiş veya tüm uygulamada ENFORCED tamamlandı iddiası taşımaz.

## Migration ve geri dönüş

`20261017153156_PersonCenterUnitOperations`: üç ilişki tablosuna additive composite alternate key, `person_unit_operations` sekiz ortak alanlı makbuz tablosu, scope/person/tür-hedef composite FK'ları ve immutable trigger. Dolu makbuz varsa Down hata verir. Boş şemada Down/reapply desteklenir. Eski migration değişmedi. Mevcut .env, canlı DB, tenant/hesap/rol seed ve servisler değiştirilmedi. Sonraki API başlangıcında AutoMigrate açıksa bekleyen migration uygulanabilir; yayın işlemi öncesinde bu etki ayrıca değerlendirilmelidir.

Ekranı kapatmak veriyi silmez. Makbuz oluşmuşsa fiziksel rollback yerine forward fix kullanılır; feature bayrakları veriyi eski finans/mobil modeline dönüştürmez.

## BF-MULTI FAZ 01 — sözleşme ve doğrulama şeması

`20261022083050_LeaseContractsAndRelationVerification` (yalnız ekleyici):

- `lease_contracts`: BaseEntity sekiz ortak alanı, `tenant_id`/`property_id`/`unit_id`, `start_date`, nullable `end_date`, nullable `contract_reference` (≤100) ve doğrulama alanları. Composite FK: (tenant, property) → `person_property_scopes`, (property, unit) → `units`. Kapsam, başlangıç, referans değişmez; bitiş bir kez.
- `unit_tenancies.lease_contract_id`: nullable, composite FK (tenant, property, unit, lease) → `lease_contracts`. Kiracı satırı sözleşme tarafıdır; ortak kiracı = aynı sözleşmede birden çok satır, yenileme = yeni sözleşme. Bağ bir kez kurulur. Taraf tarihleri sözleşme aralığında olmalı; sözleşme bitişi/soft-delete açık tarafla çelişemez (ertelenmiş constraint trigger, commit anında). Aynı kişi/daire örtüşmesini mevcut ilişki trigger'ı engeller.
- Dört tabloda `verification_status` (LEGACY/PENDING/VERIFIED/REJECTED), `verified_by_account_id` (→ `person_user_accounts`), `verified_at`, `evidence_document_id` (composite FK aynı Property `stored_documents`). Check: LEGACY/PENDING doğrulayansız, VERIFIED/REJECTED doğrulayan+zaman zorunlu. Karar verildikten sonra değişmez; LEGACY/PENDING'e geri dönülmez.
- Backfill: mevcut satırlar kolon varsayılanıyla `LEGACY`, doğrulayan ve zaman NULL. Geçiş (person-transition apply) ile aktarılan satırlar da LEGACY yazılır.
- Down: sözleşme, sözleşme bağı, LEGACY dışı doğrulama veya kanıt varsa hata verir; yalnız LEGACY satırlı şemada geri alınır.
- Portföy read model `PersonPortfolio.Build` (Application, saf fonksiyon): endpoint yoktur; FAZ 02/03'te bağlanır.

## BF-MULTI FAZ 02 — kişi dizini, portföy, bağımlı seçim ve kira sözleşmesi API'si

Hepsi `/api/v2`, `PersonRefactor:Enabled` ve `CanManagePeople` (SUPER_ADMIN/SITE_ADMIN/MANAGER) ister. OWNER/TENANT/RESIDENT/ACCOUNTANT 403 alır; malik/kiracı yalnız mevcut `/tenants/{t}/me/person` maskeli kendi profilini okur.

| Yöntem ve yol | Açıklama |
| --- | --- |
| GET `/people?page&pageSize(≤100)&search(≤200)&sortBy=name\|createdAt&sortDirection&tenantId&propertyId&relation=OWNER\|TENANT\|RESIDENT` | Alan seçmeden kişi dizini. Kapsam `PersonTenants()`: SUPER_ADMIN tüm etkin müşteriler, yönetici yalnız üyelik/görev kapsamı. `propertyId`/`relation` yalnız yetkili alanlarda bugün etkin (REJECTED hariç) ilişki veya görev EXISTS ile süzer; kişi bir kez döner. Satır: id, tenantId, tenantName, displayName, kind, `maskedIdentity` (son 4 hane), isEnabled, version, createdAt, photoId, `units` (en çok 5: propertyId, propertyName, unitId, unitNumber, capacities OWNER/TENANT/RESIDENT; yalnız yetkili ve `propertyId` verilmişse o alandaki bugün etkin, REJECTED olmayan ilişkiler; alan adı ve bölüm numarasına göre sıralı) ve `unitCount` (toplam bölüm). Özet sayfa başına sabit 4 sorguyla toplanır. Telefon/e-posta/tam kimlik dönmez. |
| GET `/tenants/{t}/people/{person}/portfolio` | `PersonPortfolio` read model: alan → blok → daire, kapasiteler, durum (OWNER_OCCUPIED/RENTED_OUT/OWNER_ONLY/TENANT_RESIDENT/TENANT_NOT_RESIDENT/RESIDENT_ONLY), aktif sözleşme. Yalnız yöneticinin yetkili alanları; başka kişinin kimliği/adı dönmez. `asOf` Europe/Istanbul. |
| GET `/tenants/{t}/properties/{p}/person-building-choices` | Etkin bina/blok, sayfalı. APARTMENT için boş. |
| GET `/tenants/{t}/properties/{p}/person-unit-choices?buildingId` | Etkin daire, sayfalı; satırda `buildingId`. Alana ait olmayan blok 404. Kapsam artık `PropertyScopes` + MANAGER değerlendirmesi. |
| GET `/tenants/{t}/properties/{p}/units/{u}/lease-contracts` ve `/{id}` | Sözleşme + taraflar (ad, tarih, birincil, doğrulama, version); sayfalı, sabit sorgu sayısı. |
| POST `.../lease-contracts` | `operationId`, `startDate`, `endDate?`, `contractReference?` (≤100), `parties[1..10]` (`personId`, `startDate`, `endDate?`, `isPrimaryContact`). Sözleşme ve taraflar aktör hesabıyla VERIFIED. |
| POST `.../lease-contracts/{id}/parties` | `operationId`, `expectedVersion`, `party` — ortak kiracı ekler. |
| POST `.../lease-contracts/{id}/attach` | `operationId`, `expectedVersion`, `tenancyId`, `tenancyVersion` — sözleşmesiz mevcut native kiracı satırını bir kez bağlar. |
| POST `.../lease-contracts/{id}/end` | `operationId`, `expectedVersion`, `endDate` — açık bitişli sözleşmeyi bir kez bitirir; açık taraflar aynı tarihte biter. |

Kurallar: yazım `PersonRefactor:UnitRelationWritesEnabled=true` ister (yoksa 404); tenant→Property kilidi, kilit sonrası yetki tekrarı, legacy (v1) alanda 409, aktör canonical hesabı yoksa 403. UUID+aktör+tenant makbuzu (`lease_contract_operations`, değişmez): aynı içerik replay `replayed=true`, farklı içerik veya eski version 409 `BF_RECORD_CHANGED`. **Bir dairede aynı dönemde tek sözleşme** (kullanıcı kararı 22.10.2026): çakışma 409 `BF_LEASE_OVERLAP`, DB trigger daire satırı kilidiyle eşzamanlı yazarı da reddeder (SQLSTATE 23P01). Taraf sözleşme aralığı dışında veya bitişle çelişirse 409 `BF_LEASE_PARTY_OUTSIDE`. Aynı kişinin aynı dairede örtüşen kiracılığı ve birincil iletişim çakışması mevcut trigger ile 409. Sabit bitişli sözleşme yeniden bitirilemez (erken fesih ayrı iş akışıdır). Tanımsız JSON alanı 400.

`unit-relations` satırına ekleyici alanlar: `verificationStatus`, `leaseContractId`.

Migration `20261022085436_LeaseContractExclusivityAndOperations`: mevcut örtüşen sözleşme varsa Up durur; `bf_lease_overlap_guard` ve makbuz tablosu + değişmezlik trigger'ı; dolu makbuzda Down hata verir.

## BF-MULTI FAZ 03 — hesap ↔ kişi bağlama kodu

- POST `/tenants/{t}/people/{person}/account-link-codes` (yönetici, `Cache-Control: no-store`): tek kullanımlık 6 haneli kod üretir; yanıt `code`, `hint` (son 2 hane), `expiresAt` (72 saat). Kod yalnız bu yanıtta görünür; DB'de sunucu anahtarlı HMAC (`IPersonCodeHasher`) tutulur. Yeni kod önceki açık kodu iptal eder. Kişi etkin NATURAL olmalı, yöneticinin yetkili alanında güncel ilişkisi/görevi bulunmalı, bağlı hesabı olmamalı; aksi halde 409 `BF_PERSON_LINK_UNAVAILABLE`. Kapsam dışı kişi 404; aktör canonical hesabı yoksa 403.
- POST `/tenants/{t}/people/{person}/account-link/end`: aktif bağı sonlandırır (`ended_at`), açık kodları iptal eder; idempotent.
- GET `/account-status` ek alanları: `codeHint`, `codeExpiresAt` (bağlı değilken açık kod varsa). Kod/issuer/subject dönmez.
- Tablo `person_link_codes` (BaseEntity; composite FK kişi; üreten/kullanan hesap FK; açık kod için kişi başına ve kod özeti başına partial unique; check). Migration `20261022091337_CentralPersonLinkCodes`; dolu tabloda Down hata verir.

## BF-MULTI FAZ 04 — web tüketimi

`features/person-center`: liste `GET /api/v2/people` (müşteri/alan isteğe bağlı, `relation` çipleri); detay "Bağımsız bölüm ilişkileri" sekmesi `…/portfolio` + `…/unit-relations`; malik/sakin `RelationEditor` + `UnitPicker` (`person-building-choices` → `person-unit-choices?buildingId`); kiracılık `LeasePanel` (`…/lease-contracts`, `/parties`, `/attach`, `/end`, hep `expectedVersion`); hesap sekmesi `AccountLink` (`…/account-link-codes`, `…/account-link/end`). Tüm yanıtlar `model.ts` strict doğrulayıcılarından geçer; dizin satırında tam kimlik/iletişim alanı gelirse yanıt reddedilir.
