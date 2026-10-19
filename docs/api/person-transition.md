# PERSON FAZ04 — Daire ilişkileri ve gölge geçiş

Bu modül legacy `people`/`unit_occupancies` yazımını değiştirmeden yeni modelin karşılaştırılmasını sağlar. `PersonRefactor:Enabled` **ve** `PersonRefactor:TransitionEnabled` varsayılan kapalıdır. Bu ayarlar API erişim kapılarıdır; migration uygulanmasını engellemez. Canlı cutover veya eski tabloların kaldırılması değildir.

Kök: `/api/v2/tenants/{tenantId}/properties/{propertyId}/person-transition`.

| Yöntem/yol | Yetki ve davranış |
|---|---|
| GET `/preview` | SUPER_ADMIN; salt okunur, repeatable-read sayım ve SHA256 kaynak özeti |
| POST `/apply` | SUPER_ADMIN; açık tenant+Property, önizleme hash'i, atomik gölge backfill |
| GET `/comparison` | Yönetici + aktif tenant hesabı/üyeliği ve aktif Property üyeliği; SUPER_ADMIN aktif üst kayıtlarda mevcut bypass |
| GET `/people/{legacyPersonId}` | Aynı okuma yetkisi; eski UUID/version ve yerel profil + canonical kimlik |

Anonim401, yetkisiz rol403, erişilemeyen/silinmiş/pasif tenant veya Property404. MANAGER/SITE_ADMIN geçiş başlatamaz; OWNER/TENANT/RESIDENT/ACCOUNTANT genel kişi/geçiş okumasına erişemez. Tenant üyeliği exact issuer+subject hesabına bağlıdır. Başka tenantın mapping'i read üzerinden açılamaz.

`POST /apply` gövdesi: `{"expectedSourceHash":"GET /preview içindeki 64 haneli sourceHash"}`. Ek alanlar400; hash biçimi400. Önizleme eskiyse, profil eksikse, tarih/primary çakışması varsa, mapping başka tenanta aitse veya mevcut gölge kopya değişmişse409 `BF_RECORD_CHANGED`. Önizleme kişi iletişim/kimliği döndürmez: tenantId,propertyId,sourceHash,people,ownerships,tenancies,residencies,unprofiledPeople,existingPersonMaps,existingOccupancyMaps,canApply içerir. `unprofiledPeople` zorunlu canonical profile dönüştürülemeyen kişi sayısıdır. `canApply=false` tarih/primary/mapping/sayım veya gölge drift nedeniyle de oluşabilir; karar öncesinde kaynak sorunu giderilip yeniden preview alınır. Sessiz atlama/kısmi başarı yoktur.

Apply önce tenant, sonra legacy Property parent row lock alır; eski yazımlar aynı Property kilidini kullanır. Kaynak hash'i ve yetki kilit sonrası tekrar kontrol edilir. Aynı snapshot'ın eşzamanlı/tekrar uygulanması aynı mapping'i döndürür; fazladan canonical kişi/ilişki yaratmaz. Eski kayıt, version, audit, kişi kodu, LinkedUserId, finans snapshot'ı veya mobil makbuz değiştirilmez. Her eski kişi ilk geçişte ayrı canonical kişiye gider; aynı kimlik, telefon, e-posta veya adla otomatik birleştirme yapılmaz. Profil eksikliği sahte kimlikle doldurulmaz.

`PersonPropertyScope` bir Property'nin tek açık tenant eşlemesidir. `LegacyPersonMap` kaynak UUID, sourceVersion, canonical kişi ve alan-özel ad/iletişim/adres snapshot'ını tutar. `LegacyOccupancyMap` kaynak UUID/version ile tam bir Ownership/Tenancy/Residency hedefi taşır; composite FK tenant/property/unit tutarlılığını korur. Soft-delete ve pasif kaynaklar tarihçe olarak kopyalanır. Silinmiş kaynak kişisi compatible person endpoint'inden dönmez.

`CompatiblePerson` eski `legacyId` ve `legacyViewVersion` (bu aşamada authoritative legacy sourceVersion), canonicalId, localProfile,identity,isEnabled içerir. Mapping BaseEntity.version veya canonical.version v1 concurrency version yerine kullanılmaz. Mevcut v1 API değişmedi; bu yeni uç gölge karşılaştırma içindir. Legacy kaynak değişirse okuma409 ve comparison `matches=false` döner. Gölge kopyayı otomatik üzerine yazma/merge yoktur; yeni preview ve kontrollü uzlaştırma/cutover gerekir. Canonical API, import edilmiş gölge kişiyi düzenlemeyi409 ile reddeder. Böylece iki bağımsız authoritative yazım yolu oluşmaz. V1 kart/finans/mobil akışları legacy üzerinden çalışmaya devam eder.

## İlişki kuralları

Üç ayrı domain/entity/tablo: `UnitOwnership`/`unit_ownerships`, `UnitTenancy`/`unit_tenancies`, `UnitResidency`/`unit_residencies`. Kişi rol boolean'ı yoktur. OWNER→Ownership, TENANT→Tenancy, RESIDENT→Residency; kiracılıktan veya malik oluşundan otomatik residency üretilmez. Çoklu malik ve çoklu kiracı desteklenir.

DateOnly aralıklar iki uç dahil; açık bitiş sınırsızdır. Aynı tenant+Property+Unit+kişi+tür aralıkları çakışamaz. Birimin tüm ilişki türlerinde yalnız bir birincil iletişim aralığı olabilir. Bitiş bir kez yazılır; başlangıç/kişi/birim/primary/pay geçmişi yeniden yazılamaz. Yeni ilişki yönetim ekranı ve genel relation CRUD bu fazın gölge geçiş API'si değildir.

Malik payı iki nullable pozitif long ile tutulur; ikisi birlikte null **bilinmiyor** demektir. Bilinen pay 0<pay≤payda; her çakışan dönem için toplam≤1. Bilinmeyen pay sıfır veya eşit pay olarak varsayılmaz. Legacy OWNER payları null taşınır. Domain .NET BigInteger ile, PostgreSQL trigger numeric tam sayı kesir/GCD hesabıyla doğrular; floating point yoktur. Her başlangıç sınırı incelenir. Üç tabloyu ortak kontrol eden trigger Unit satırını kilitler; sonlu tarih ve farklı türde primary yarışları DB'de de reddedilir. Yeni extension/paket yoktur.

## İşletim sınırı

Kullanıcı mevcut kişilerin tek Altunbayrak Rezidans'a ait olduğunu bildirdi. Kod müşteri adından otomatik tenant yaratmaz veya diğer alanları dahil etmez. Bu tur yalnız fixture tenant/Property eşlemesiyle test edildi. Gerçek tenant provisioning, gerçek Property UUID eşlemesi, yedek/restore, dry-run ve sıfır drift kanıtı olmadan canlı backfill/cutover yapılmaz. Keycloak hesabı aktarılmadı; issuer çözümü ve tüzel temsil çelişkileri sonraki kontrollü adımdır. Feature kapalı tutulur; eski yazımlar açıkken gölge snapshot zamanla eskir.

[FAZ04 raporu](../refactor/person/phase-04-report.md), [migration akışı](../MIGRATIONS.md), [testler](../TESTING.md).


## FAZ05 erişim eklemesi —17.10.2026
Non-SUPER_ADMIN compatible read/comparison, tenant için ENFORCED seçilmişse ilgili Property'deki güncel yönetim görevini de arar. Preview/apply SUPER_ADMIN kapsamı korunur. Kaynak version'ından sapmış import kişisi ENFORCED yönetim yetkisine dönüştürülmez. [Görev sözleşmesi](management-assignments.md).
