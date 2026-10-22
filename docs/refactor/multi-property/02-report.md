# BF-MULTI FAZ 02 — Backend ilişkiler ve kişi listesi API

22.10.2026 (Europe/Istanbul). GitHub [#34](https://github.com/AkaBayraK/BuildingFlow/issues/34). Plan toplamı 10 faz (FAZ 00–09); bu faz listede 3/10. **Durum: teknik teslim, kullanıcı onayı bekleniyor. FAZ 03 başlatılmadı.** Mevcut canonical `/api/v2/tenants/{t}/people` ve kişi merkezi API'leri temel alındı; yeniden yazılmadı.

Kullanıcı kararı (22.10.2026 08:47, #34 yorumu): **bir dairede aynı dönemde tek kira sözleşmesi.** Ortak kiracı aynı sözleşmeye `UnitTenancy` tarafı olarak eklenir.

## 1) Yapılan değişiklikler ve dosya yolları

| Katman | Dosya | Değişiklik |
| --- | --- | --- |
| Domain | `backend/src/BuildingFlow.Domain/LeaseContract.cs` | `LeaseContractRules.Overlaps/ValidateUnit` (daire başına dönem tekilliği, iki uç dahil); değişmez `LeaseContractOperation` makbuzu |
| Application | `backend/src/BuildingFlow.Application/LeaseContracts.cs` (yeni) | Sözleşme DTO/istekleri, `ILeaseContractStore` |
| Application | `backend/src/BuildingFlow.Application/PersonCenter.cs` | `PersonDirectoryRow/Query`, `PersonPortfolioView`, `PersonBuildingChoice`; `PersonUnitChoice.BuildingId`; `PersonUnitRelationRow.VerificationStatus/LeaseContractId` (ekleyici) |
| Application | `backend/src/BuildingFlow.Application/ErrorCatalog.cs` | `BF_LEASE_OVERLAP`, `BF_LEASE_PARTY_OUTSIDE` |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/PersonCenterStore.cs` | `DirectoryAsync`, `PortfolioAsync`, `BuildingsAsync`, bina filtreli `UnitsAsync`; legacy alan kontrolü paylaşılabilir statik yardımcı |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/LeaseContractStore.cs` (yeni) | Liste/detay/oluştur/taraf ekle/bağla/bitir; kilit, makbuz, VERIFIED aktör |
| Infrastructure | `PersonUnitOperationConfiguration.cs`, `DependencyInjection.cs` | Makbuz mapping, DI |
| Api | `backend/src/BuildingFlow.Api/PersonCenterEndpoints.cs` | Yeni rotalar |
| Migration | `20261022085436_LeaseContractExclusivityAndOperations{,.Designer}.cs`, snapshot | Ekleyici |
| Frontend sözleşme | `frontend/buildingflow-web/src/services/error-codes.ts`, `messages/tr.json`, `messages/en.json` | İki yeni kod TR/EN |
| Test | `backend/tests/BuildingFlow.IntegrationTests/PersonPortfolioApiTests.cs` (yeni), `LeaseContractPersistenceTests.cs` (+1, FAZ 01 rollback assert'i yeni migration sırasına uyarlandı), `backend/tests/BuildingFlow.UnitTests/LeaseContractTests.cs` (+1) | |
| Belge | bu rapor, `docs/api/person-center.md`, `docs/api/errors.md`, `docs/TESTING.md`, `docs/progress/*` | |

## 2) Yeni/etkilenen endpointler

Yeni (ayrıntı `docs/api/person-center.md`):

- `GET /api/v2/people` — alan seçimsiz, yetkili kapsamlı, sayfalı/arama/sıralama/tenant-alan-kapasite filtreli, maskeli dizin.
- `GET /api/v2/tenants/{t}/people/{person}/portfolio` — FAZ 01 `PersonPortfolio` read model'i.
- `GET /api/v2/tenants/{t}/properties/{p}/person-building-choices`.
- `GET|POST /api/v2/tenants/{t}/properties/{p}/units/{u}/lease-contracts`, `GET …/{id}`, `POST …/{id}/parties`, `POST …/{id}/attach`, `POST …/{id}/end`.

Etkilenen (geri uyumlu): `person-unit-choices` isteğe bağlı `buildingId` ve satırda `buildingId`; erişim için ayrıca `PropertyScopes` kontrolü. `unit-relations` satırlarında `verificationStatus`, `leaseContractId`. Mevcut alan/rotalar silinmedi veya yeniden adlandırılmadı.

## 3) Yetki ve KVKK etkisi

- Dizin: SUPER_ADMIN tüm etkin müşteriler; SITE_ADMIN/MANAGER yalnız `PersonTenants()` (müşteri üyeliği + ENFORCED modda aktif görev) ve alan filtresinde yalnız `PropertyScopes()`. Yetkisiz `tenantId`/`propertyId` boş sonuç döner, varlık sızdırmaz. ACCOUNTANT ve daire rolleri 403.
- Maskeleme: dizinde kimlik numarası yalnız son 4 hane, telefon/e-posta dönmez (arama bu alanlarda çalışmaya devam eder, mevcut tenant listesiyle aynı). Kişi detayının tam profili yalnız yönetici rollerine açık ve değişmedi.
- Portföy: başka kişilerin satırları yalnız RENTED_OUT türetmek için okunur; yanıtta ad/kimlik/kişi id yoktur.
- Sözleşme yazımı: yalnız yetkili yönetici, kilit sonrası yetki tekrarı, legacy alanda ikinci yazıcı yok, aktör hesabı VERIFIED olarak kaydedilir, makbuz issuer/subject saklar ama yanıtta dönmez. Malik/kiracı yazma yolu yok.

## 4) Migration/rollback durumu

`20261022085436_LeaseContractExclusivityAndOperations` (ekleyici):

- Up: örtüşen mevcut sözleşme varsa upgrade durur (veri sessizce kabul edilmez). `bf_lease_overlap_guard` AFTER INSERT/UPDATE trigger'ı daire satırını `FOR UPDATE` kilitleyip örtüşmede SQLSTATE 23P01 verir. `lease_contract_operations` makbuz tablosu (BaseEntity, composite FK, check, benzersiz UUID+aktör+tenant) ve değişmezlik trigger'ı.
- Down: makbuz varsa hata; yoksa trigger/fonksiyon/tablo kaldırılır. FAZ 01 Down koruması aynen geçerli.
- Canlı DB'ye uygulanmadı; `.env`, Keycloak, bayraklar değişmedi; yeni paket yok. Development AutoMigrate açıksa yerel API bu iki migration'ı uygulayabilir.

## 5) Test komutları ve sonuçları

Ayrı geçici PostgreSQL rol/DB (`../postgres-18.6/scripts/test-db.py`), test sonunda silindi. S3/RabbitMQ başlatılmadı.

| Komut | Sonuç |
| --- | --- |
| `dotnet test BuildingFlow.sln --filter "LeaseContract\|PersonPortfolio\|PersonCenter\|PersonRelation\|PersonTransition\|SessionContext\|CentralPe\|PersistenceTests\|ErrorContract\|MigrationTests\|MobileAuthorization\|ManagementAssignment\|MobileSession\|PersonCodeApi\|PersonUnitRelationTests"` | Entegrasyon **172/172**, unit **15/15** |
| `dotnet test tests/BuildingFlow.UnitTests` | **293/293** |
| `dotnet test tests/BuildingFlow.IntegrationTests --filter "MobileCanonicalSync\|UnitPeopleNavigation\|PeopleApiTests\|HierarchyApi"` | **74/75**; tek kırmızı `MobileCanonicalSyncTests.PhotoIsOptionalConsentProtectedAndBoundToCurrentContext` 503, S3 başlatılmadı (bilinen baseline) |
| `npm test` (web) | **221/221** (backend katalog ↔ FE registry eşliği dahil); `npm run typecheck` PASS |
| `bash scripts/db.sh check` | fark yok |
| `bash scripts/check-idempotent-sql.sh` | PASS (38 migration, iki kez) |

Yeni senaryolar: Ahmet portföyü API'de (iki alan, iki blok, APARTMENT `buildingId=null`), başka kişi verisinin sızmaması, yabancı yönetici 404, daire rolleri 403; dizinde müşteri sınırı, SUPER_ADMIN çoklu müşteri, maske, alan/kapasite filtresi tekilleştirme, yetkisiz tenant/alan boş, geçersiz sorgu 400; bina→daire bağımlı seçim ve yabancı blok 404; sözleşme oluşturma (ortak kiracı, VERIFIED), replay/farklı içerik 409, örtüşme `BF_LEASE_OVERLAP`, aralık dışı `BF_LEASE_PARTY_OUTSIDE`, eski version 409, aynı kişi örtüşmesi 409, mevcut kiracıyı bağlama, bitirme ve tekrar bitirme 409, yenileme; eşzamanlı iki örtüşen oluşturma (API ve DB) yalnız biri; yabancı daire/kişi/kapsam, kapalı bayrak, legacy alan, tanımsız JSON alanı; dizin/portföy/sözleşme listesi için 1 ve çok kayıtta **eşit SQL komut sayısı** (N+1 yok).

## 6) Bilinen riskler

1. Mevcut (sözleşmesiz) kiracı satırları tek-sözleşme kuralına tabi değildir; yalnız `LeaseContract` kayıtları için geçerlidir. Toplu bağlama/temizlik FAZ 04 ekranında yönetici kararıyla yapılmalı.
2. Sabit bitişli sözleşmenin erken feshi desteklenmiyor (domain bitişi bir kez); ihtiyaç halinde ayrı fesih modeli kararı gerekir.
3. Dizin aramasında telefon/e-posta ile eşleşme maskeli satır döndürür; arama yoluyla varlık doğrulama mevcut tenant listesindeki davranışla aynıdır.
4. Portföy yanıtı sayfalanmaz; kişi başına ilişki sayısıyla sınırlıdır.
5. Native yazım yalnız legacy geçmişi olmayan alanlarda; canlı Altunbayrak Rezidans legacy olduğundan bu API'ler orada yazım için kapalı kalır (FAZ 09 geçişi).
6. Bayraklar kapalı; canlı etkisi yok. Bayrak açılmadan önce yönetici hesap/görev bağları hazırlanmalı.

## 7) Sonraki faza geçiş koşulu

Kullanıcı bu raporu onaylarsa FAZ 03 (#35) başlar: Keycloak hesabı → doğrulanmış kişi bağı, `/me/portfolio` ve sunucu doğrulamalı aktif bağlam. Onay gelmeden FAZ 03'e geçilmez.
