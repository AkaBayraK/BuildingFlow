# 03 — Property / Building / Unit API

Bütün rotalar Bearer JWT ve sabit sub gerektirir. İmzalı Keycloak realm rolleri kullanılır. SUPER_ADMIN global erişir; diğer kullanıcılar yalnızca aktif, silinmemiş PropertyMembership kapsamında okur. Property oluşturma/düzenleme/silme SUPER_ADMIN; Building/Unit yazma SUPER_ADMIN/SITE_ADMIN/MANAGER. Query/body userId/readAll/parentId erişimi değiştirmez.

| Kayıt | Liste / Oluşturma | Detay / Düzenleme / Silme |
| --- | --- | --- |
| Property | GET/POST /api/v1/properties | GET/PUT/DELETE /api/v1/properties/{propertyId} |
| Building | GET/POST /api/v1/properties/{propertyId}/buildings | GET/PUT/DELETE /api/v1/properties/{propertyId}/buildings/{buildingId} |
| Doğrudan Unit | GET/POST /api/v1/properties/{propertyId}/units | GET/PUT/DELETE /api/v1/properties/{propertyId}/units/{unitId} |
| Bina altındaki Unit | GET/POST /api/v1/properties/{propertyId}/buildings/{buildingId}/units | GET/PUT/DELETE /api/v1/properties/{propertyId}/buildings/{buildingId}/units/{unitId} |

POST 201 + Location; GET/PUT 200; DELETE 204. Property listesi varsayılan pageSize 12, alt listeler 10. Bütün listeler `{items,page,pageSize,totalCount}` döndürür.

Örnek oluşturma body'leri:

```json
{"name":"ABC Apartmanı","type":"APARTMENT","city":"İstanbul","district":"Kadıköy","address":"Örnek adres","floorCount":8}
```

```json
{"name":"Mavişehir Sitesi","type":"SITE","city":"İstanbul","district":"Ataşehir","address":"Örnek adres"}
```

```json
{"name":"A Blok","floorCount":12}
```

```json
{"number":"12","floor":3,"grossSquareMeters":120.50,"type":"APARTMENT"}
```

PropertyType: APARTMENT/SITE/RESIDENCE/MIXED. UnitType: APARTMENT/OFFICE/SHOP/VILLA/WAREHOUSE/WORKSHOP; oluşturma/düzenlemede UnitType varsayılan APARTMENT. Geçersiz/nümerik type değerleri 400. APARTMENT doğrudan APARTMENT Unit içerir, building_id NULL'dır; Building oluşturma 409. Diğer Property türleri Building gerektirir; doğrudan Unit oluşturma 409. Unit her zaman property_id taşır; composite FK bina ile yönetim alanını eşleştirir. Property PUT isteğinde isteğe bağlı type alanı dört türden biri olabilir; atlanır/null ise mevcut tür korunur. Tür değişimi yalnızca bina/blok veya bağımsız bölüm kaydı/geçmişi yoksa mümkündür. Pasif ve soft-delete edilmiş çocuklar da engeldir; oluşturan üyeliği engel değildir. İlişki varsa değişim 409 döner; aynı türde normal düzenleme yapılabilir.

Property floorCount yalnızca apartman için isteğe bağlı pozitif int32 metadata. Diğer türlerde kat sayısı Building üzerinde tutulur. UI sayaçları gerçek silinmemiş kayıt sayısıdır, pasifler dahildir. Kat sayısı veya tür seçimi boş Unit/Building üretmez. Section/etap ve ayrı Floor tabloları bu iterasyonda yok; Unit.floor negatif/zemin/pozitif katları taşır.

PUT bütün ilgili alanları, isEnabled ve son okunan pozitif version değerini gerektirir. DELETE query'de version zorunludur. Property PUT alanları name/city/district/address/isEnabled/version/floorCount/type; Building name/isEnabled/version/floorCount; Unit number/floor/grossSquareMeters/type/isEnabled/version. Audit/id/parent kimlikleri istemciden alınmaz. floorCount atlanırsa null olur; UI mevcut değeri gönderir.

## Apartman ve bağımsız bölüm detayları

11.10.2026 `ApartmentAndUnitDetails` ile eklenen alanlar POST/PUT ve liste/detay yanıtlarında ortak camelCase isimlerini kullanır. Hepsi isteğe bağlıdır; boş değer `null`, `false` ve `0` ayrı değerlerdir. PUT sırasında atlanan isteğe bağlı alanlar null'a döner; frontend mevcut değerleri okuyup yeniden gönderir, 409 sonrasında güncel kaydı yükler.

| Kayıt | Yeni alanlar | Sınırlar |
| --- | --- | --- |
| Property | `neighborhood` | Bütün türlerde, kırpılmış en fazla 100 karakter |
| APARTMENT Property veya Building | `constructionYear`, `heatingType`, `hasElevator`, `parkingType` | Yıl 1800–Europe/Istanbul güncel yılı; diğer Property türlerinde bina özellikleri Building üzerinde tutulur |
| Unit | `netSquareMeters`, `roomLayout`, `floorPosition`, `isMezzanine`, `bathroomCount`, `kitchenType`, `hasBalcony`, `heatingType` | Net alan pozitif, iki ondalık ve ≤brüt alan; banyo 0–6 (6+); nullable boolean seçimleri |

Enum değerleri büyük harfli isimlerdir, sayısal alias kabul edilmez:

- HeatingType: `NONE`, `STOVE`, `GAS_STOVE`, `FLOOR_HEATER`, `CENTRAL`, `CENTRAL_METERED`, `COMBI`, `UNDERFLOOR`, `AIR_CONDITIONING`, `OTHER`.
- ParkingType: `OPEN`, `CLOSED`, `OPEN_AND_CLOSED`, `NONE`.
- RoomLayout: `STUDIO`, `R1_1`, `R1_5_1`, `R2_0`, `R2_1`, `R2_5_1`, `R3_1`, `R3_5_1`, `R4_1`, `R4_5_1`, `R5_1`, `R5_2`, `R6_PLUS`.
- KitchenType: `OPEN`, `CLOSED`.
- FloorPosition: `BELOW_ENTRANCE`/`BASEMENT` için floor<0; `GROUND`/`GARDEN`/`ENTRANCE`/`HIGH_ENTRANCE` için floor=0; `REGULAR`/`ROOF` için floor≥1. Kat ayrıca üst kaydın floorCount değerini aşamaz; bu kural backend tarafından doğrulanır.

Arayüz: Genel Bilgiler ve sağ düzenleme/oluşturma paneli apartman özelliklerini, bina detayları bina özelliklerini, Daireler tablosu ve detay/form/mobil kartlar Unit özelliklerini gösterir. Net alan ve oda düzeni varsayılan görünür; diğer daire özellikleri **Sütunlar** menüsünden açılır. Yeni alanlar için API sıralama seçenekleri genişletilmedi; bu sütunlarda sort kapalıdır. Boş değer “Belirtilmedi / Not specified” gösterilir. Kanıt [TESTING.md](../TESTING.md).

## Mevcut kapsam ve hata davranışı

İsim limitleri Property 200, şehir/ilçe 100, adres 500, Building 100, Unit.number 30. Blok adı aynı Property'de; Unit numarası aynı Building'de veya doğrudan aynı Property'de büyük/küçük harfe duyarsız benzersizdir. NULL-building kapsamı ayrı partial unique index ile korunur. Silinen numara yeniden kullanılabilir. Area 0'dan büyük, en fazla 99999999.99 ve en fazla iki ondalık basamak; floor int32 zorunlu, negatif olabilir.

Page 1–1000000, pageSize 1–100, search en fazla 200; %/_ literal aranır. sortDirection asc/desc. Property name/city/createdAt; Building name/createdAt; Unit number/floor/grossSquareMeters/createdAt. Kimlik ikinci sıralama alanıdır. PropertyResponse type/floorCount/buildingCount/unitCount/canChangeType; BuildingResponse floorCount/unitCount; UnitResponse type/propertyId/nullable buildingId içerir.

- 400: eksik/geçersiz alan, enum, kat/alan, liste/version parametresi. Alan hataları ValidationProblemDetails.errors.
- 401: anonim/geçersiz JWT. 403: yazma rolü veya sabit sub yok.
- 404: yok, silinmiş, kapsam dışı veya yanlış ebeveyn rotası. Direct Unit rotası Building altındaki Unit'i döndürmez.
- 409: duplicate, eski version/eşzamanlı yazma, pasif üst kayıt, tür/hiyerarşi kuralı veya dolu üst kaydı silme. ProblemDetails.title kullanıcıya açıklama sağlar.

Silme soft-delete'dir; otomatik çocuk silme yok. Unit/Building içeren Property ve Unit içeren Building silinemez; önce alt kayıtlar silinir. Pasif üst kayıtta alt kayıt değiştirilemez; okumaya izin verilir. Unit komutu Property ve varsa Building version/modified audit'ini, Building komutu Property version/modified audit'ini de değiştirir. SaveChanges tek transaction içinde üst kayıt concurrency kontrolü yapar; çakışmada alt işlem de rollback olur. 409 sonrası kullanıcı güncel kaydı yükler; otomatik üzerine yazma yapılmaz.

Eski frontend /sites bağlantıları yeni /properties rotalarına yönlenir; API'nin kanonik rotaları bu belgedekilerdir. Eski API sözleşmesi sites.md tarihsel kayıttır. Sekiz BaseEntity alanı bütün properties/buildings/units/property_memberships tablolarında zorunludur. UnifiedPropertyModel migration mevcut tabloları rename/backfill eder, eski migration'ları değiştirmez.

## Kişiden daire listesine tam kimlik filtresi

Direkt `/properties/{id}/units` ve bina altı `/properties/{id}/buildings/{buildingId}/units` GET listeleri opsiyonel `unitId` (dolu GUID) kabul eder. Filtre mevcut Property/Building scope ve soft-delete kurallarıyla birlikte, count ve pagination öncesi uygulanır. Yanlış kapsam/bilinmeyen/silinmiş Unit için boş liste; erişilemeyen üst kayıt404; bozuk veya Guid.Empty400. Arama metni UnitId yerine kullanılmaz, aynı numaralı bölümler karışmaz. Mevcut istemciler query göndermeden aynı listeyi alır; schema/snapshot değişmez.

FE canonical listede `?unitId={uuid}` korur; aktif filtre ve tüm bölümleri göster eylemi vardır. Tek, doğrulanmış UUID query OIDC dönüş whitelist'ine dahildir; başka query/fragment/kullanıcı metni oturum dönüşüne eklenmez. Daire tablet/mobil görünümü1280px altında kart, masaüstü TanStack tablodur. Detay özellikleri ve görünür Kiracı/Malik kolonları korunur. Kişisel özetin ayrı yetkisi [people.md](people.md)'dedir.
