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

İsim limitleri Property 200, şehir/ilçe 100, adres 500, Building 100, Unit.number 30. Blok adı aynı Property'de; Unit numarası aynı Building'de veya doğrudan aynı Property'de büyük/küçük harfe duyarsız benzersizdir. NULL-building kapsamı ayrı partial unique index ile korunur. Silinen numara yeniden kullanılabilir. Area 0'dan büyük, en fazla 99999999.99 ve en fazla iki ondalık basamak; floor int32 zorunlu, negatif olabilir.

Page 1–1000000, pageSize 1–100, search en fazla 200; %/_ literal aranır. sortDirection asc/desc. Property name/city/createdAt; Building name/createdAt; Unit number/floor/grossSquareMeters/createdAt. Kimlik ikinci sıralama alanıdır. PropertyResponse type/floorCount/buildingCount/unitCount/canChangeType; BuildingResponse floorCount/unitCount; UnitResponse type/propertyId/nullable buildingId içerir.

- 400: eksik/geçersiz alan, enum, kat/alan, liste/version parametresi. Alan hataları ValidationProblemDetails.errors.
- 401: anonim/geçersiz JWT. 403: yazma rolü veya sabit sub yok.
- 404: yok, silinmiş, kapsam dışı veya yanlış ebeveyn rotası. Direct Unit rotası Building altındaki Unit'i döndürmez.
- 409: duplicate, eski version/eşzamanlı yazma, pasif üst kayıt, tür/hiyerarşi kuralı veya dolu üst kaydı silme. ProblemDetails.title kullanıcıya açıklama sağlar.

Silme soft-delete'dir; otomatik çocuk silme yok. Unit/Building içeren Property ve Unit içeren Building silinemez; önce alt kayıtlar silinir. Pasif üst kayıtta alt kayıt değiştirilemez; okumaya izin verilir. Unit komutu Property ve varsa Building version/modified audit'ini, Building komutu Property version/modified audit'ini de değiştirir. SaveChanges tek transaction içinde üst kayıt concurrency kontrolü yapar; çakışmada alt işlem de rollback olur. 409 sonrası kullanıcı güncel kaydı yükler; otomatik üzerine yazma yapılmaz.

Eski frontend /sites bağlantıları yeni /properties rotalarına yönlenir; API'nin kanonik rotaları bu belgedekilerdir. Eski API sözleşmesi sites.md tarihsel kayıttır. Sekiz BaseEntity alanı bütün properties/buildings/units/property_memberships tablolarında zorunludur. UnifiedPropertyModel migration mevcut tabloları rename/backfill eder, eski migration'ları değiştirmez.
