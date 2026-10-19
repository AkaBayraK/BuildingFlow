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

Native ilişkiler henüz eski finans/mobil tüketicilere yansıtılmaz. Bu nedenle canlı verisi olan Altunbayrak Rezidans'ta bu yazım kapısı açılmadı ve v1 değiştirilmedi. Bu faz genel canlı geçiş veya tüm uygulamada ENFORCED tamamlandı iddiası taşımaz.

## Migration ve geri dönüş

`20261017153156_PersonCenterUnitOperations`: üç ilişki tablosuna additive composite alternate key, `person_unit_operations` sekiz ortak alanlı makbuz tablosu, scope/person/tür-hedef composite FK'ları ve immutable trigger. Dolu makbuz varsa Down hata verir. Boş şemada Down/reapply desteklenir. Eski migration değişmedi. Mevcut .env, canlı DB, tenant/hesap/rol seed ve servisler değiştirilmedi. Sonraki API başlangıcında AutoMigrate açıksa bekleyen migration uygulanabilir; yayın işlemi öncesinde bu etki ayrıca değerlendirilmelidir.

Ekranı kapatmak veriyi silmez. Makbuz oluşmuşsa fiziksel rollback yerine forward fix kullanılır; feature bayrakları veriyi eski finans/mobil modeline dönüştürmez.
