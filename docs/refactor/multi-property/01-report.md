# BF-MULTI FAZ 01 — Domain model ve migration tasarımı

22.10.2026 (Europe/Istanbul). GitHub [#33](https://github.com/AkaBayraK/BuildingFlow/issues/33). Plan: `buildingflow-multi-property-prompts/PHASES.md`, toplam 10 faz (FAZ 00–09); bu faz listede 2/10. FAZ 00 kararları 1–3 (BF-PERSON canonical model üzerine, LeaseContract + LeaseParty, doğrulama alanları) uygulandı. Kararlar 4–5 sonraki fazlara aittir. **Durum: teknik teslim, kullanıcı onayı bekleniyor. FAZ 02 başlatılmadı.**

## 1) Yapılan değişiklikler ve dosya yolları

| Katman | Dosya | Değişiklik |
| --- | --- | --- |
| Domain | `backend/src/BuildingFlow.Domain/LeaseContract.cs` (yeni) | `RelationVerificationStatus` (LEGACY/PENDING/VERIFIED/REJECTED), ortak `VerifiableRecord : BaseEntity` (Verify/Reject bir kez, MarkLegacy), `LeaseContract` (tenant/property/unit, DateOnly başlangıç/bitiş, referans ≤100, bitiş bir kez), `LeaseContractRules.ValidateParties` |
| Domain | `backend/src/BuildingFlow.Domain/PersonUnitRelations.cs` | `UnitOwnership/UnitTenancy/UnitResidency` artık `VerifiableRecord`'dan türer. `UnitTenancy.LeaseContractId` + `AttachLease` (kapsam ve tarih aralığı denetimi, bir kez) ve sözleşmeden kurucu |
| Application | `backend/src/BuildingFlow.Application/PersonPortfolio.cs` (yeni) | Saf portföy read model: property → building → unit, kapasite ve türetilmiş durum, aktif sözleşme. Endpoint yok |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/PersonRelationConfigurations.cs` | Ortak `VerificationConfiguration`, `LeaseContractConfiguration`, tenancy→lease composite FK |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/PersonCenterStore.cs` | Yönetici ilişki yazımı aktörün `PersonUserAccount` kaydıyla VERIFIED; hesap yoksa 403 |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/PersonTransitionStore.cs` | Legacy aktarımı LEGACY işaretler; dry-run/apply/compare mantığı değişmedi |
| Migration | `backend/src/BuildingFlow.Infrastructure/Migrations/20261022083050_LeaseContractsAndRelationVerification{,.Designer}.cs`, `BuildingFlowDbContextModelSnapshot.cs` | Ekleyici şema + trigger + Down koruması |
| Test | `backend/tests/BuildingFlow.UnitTests/LeaseContractTests.cs`, `PersonPortfolioTests.cs` (yeni); `backend/tests/BuildingFlow.IntegrationTests/LeaseContractPersistenceTests.cs` (yeni), `PersonCenterTests.cs` (+1 test) | |
| Belge | bu rapor, `docs/api/person-center.md`, `docs/TESTING.md`, `docs/progress/{STATUS,SESSION,CHANGELOG}.md` | |

Model kararları:

- **Ownership / LeaseContract+LeaseParty / Occupancy ayrı kalır.** LeaseParty için ayrı tablo açılmadı: `UnitTenancy` sözleşme tarafıdır (nullable `lease_contract_id`). Ortak kiracı = aynı sözleşmede birden çok `UnitTenancy`; ikinci sözleşme/yenileme = yeni `LeaseContract`. İkinci bir kiracı tablosu ve çift yazıcı oluşmaz.
- **Örtüşme politikası:** aynı daire + aynı kişi + aynı tür örtüşmesi mevcut `bf_person_relation_guard` ile engelli (sözleşmeden bağımsız). Aynı dairede farklı kişilerin örtüşen sözleşmeleri **engellenmedi** (oda kiralama vb. için); bu bir iş kuralı kararı gerektirir.
- **Tarih kuralı:** taraf başlangıcı ≥ sözleşme başlangıcı; sözleşme bitişliyse taraf bitişi zorunlu ve ≤ sözleşme bitişi. DateOnly iki uç dahil.
- **Doğrulama:** PENDING → VERIFIED/REJECTED veya LEGACY → VERIFIED/REJECTED bir kez. Karar sonrası durum, doğrulayan, zaman, kanıt değişmez.
- **Backfill seçimi: ayrı `LEGACY` durumu.** Mevcut satırlar `verification_status='LEGACY'`, `verified_by_account_id=NULL`, `verified_at=NULL`. Gerekçe: "VERIFIED + verified_by NULL" seçeneği VERIFIED'ın anlamını (bir hesabın gerçekten doğruladığı) zayıflatır ve check constraint'i gevşetir; `verified_at=created_at` de gerçekleşmemiş bir doğrulama anı uydurur. LEGACY "doğrulama kavramından önce yönetici yolu/legacy aktarımıyla oluşmuş" kaydı dürüstçe ayırır. Portföy read model LEGACY'yi etkili sayar (bugünkü davranış korunur), PENDING/REJECTED'i saymaz. LEGACY satır ileride yönetici tarafından VERIFIED/REJECTED yapılabilir.
- **Portföy durumu:** OWNER_OCCUPIED (malik + sakin), RENTED_OUT (malik, dairede başka kişinin etkin kirası), OWNER_ONLY (malik, ne oturuyor ne kiraya verilmiş — brief'teki beş duruma ek gerekli altıncı durum), TENANT_RESIDENT, TENANT_NOT_RESIDENT, RESIDENT_ONLY. Etkin = enabled + VERIFIED/LEGACY + `start ≤ bugün ≤ end`. Bugün çağıran tarafından Europe/Istanbul iş günüyle verilir. Çıktıda yalnız kişinin kendi ilişkileri döner; diğer kişilerin satırları yalnız durum/sözleşme türetmede kullanılır (kişi verisi çıkmaz).

## 2) Yeni/etkilenen endpointler

Yeni endpoint yok. `POST /api/v2/tenants/{t}/people/{person}/unit-relations` JSON sözleşmesi değişmedi; davranış farkı: kayıt VERIFIED yazılır, aktörün canonical hesabı yoksa 403. Doğrulama/sözleşme alanları okuma yanıtlarında henüz yok (FAZ 02/03).

## 3) Yetki ve KVKK etkisi

- Malik/kiracı için yazma yolu açılmadı; yalnız mevcut yönetici yazım kapısı (bayrak + MANAGER + scope + Property kilidi).
- `verified_by_account_id` hesap UUID'sidir; issuer/subject, ad veya e-posta saklanmaz/dönmez.
- Kanıt belgesi composite FK ile aynı Property'nin `stored_documents` kaydı olmak zorunda; başka alan belgesi bağlanamaz.
- Tenant izolasyonu: sözleşme (tenant, property) scope FK'sı ve tenancy→sözleşme (tenant, property, unit) composite FK'sı çapraz tenant/alan/daire bağını DB'de reddeder.
- Read model başka kişinin kişisel verisini döndürmez; RENTED_OUT için yalnız sözleşme kimliği/tarih/referans/doğrulama durumu.

## 4) Migration/rollback durumu

`20261022083050_LeaseContractsAndRelationVerification`:

- Up: `lease_contracts` tablosu; üç ilişki tablosuna doğrulama kolonları (mevcut satırlar `LEGACY`); `unit_tenancies.lease_contract_id`; `stored_documents` (property_id, id) alternate key; FK/index/check'ler; `bf_verification_guard` (BEFORE UPDATE, dört tablo) ve `bf_lease_party_guard` (DEFERRABLE INITIALLY DEFERRED constraint trigger; taraf ve sözleşme tarafı, sözleşme satırı FOR SHARE kilidiyle yarışa karşı).
- Down: sözleşme/sözleşme bağı/LEGACY dışı doğrulama/kanıt varsa `RAISE EXCEPTION`; yalnız LEGACY satırlı şemada trigger/kolon/tablo kaldırılır.
- Dry-run: `bash scripts/db.sh sql` idempotent SQL üretir (DB'ye uygulamaz); throwaway DB'de iki kez uygulandı.
- **Production/canlı DB'ye uygulanmadı.** `.env`, gerçek DB, Keycloak ve bayraklar değişmedi. Uyarı: Development'ta `Database:AutoMigrate` açıksa yerel API bir sonraki açılışta bu migration'ı uygular; yerel DB'nin yedeği/onayı buna göre değerlendirilmelidir.
- Rollback stratejisi: bayraklar zaten kapalı; dolu şemada Down yerine forward fix.

## 5) Test komutları ve sonuçları

Ayrı geçici PostgreSQL rol/veritabanı (`../postgres-18.6/scripts/test-db.py create|cleanup`) ile; S3/RabbitMQ başlatılmadı.

| Komut | Sonuç |
| --- | --- |
| `dotnet test tests/BuildingFlow.UnitTests` | **292/292** geçti (yeni: LeaseContractTests 5, PersonPortfolioTests 2) |
| `dotnet test BuildingFlow.sln --filter "LeaseContractPersistenceTests\|PersonCenterTests\|PersonRelationPersistenceTests\|PersonTransitionTests\|PersonCenterMigrationTests\|SessionContextTests\|CentralPeopleTests\|PersistenceTests\|CentralPersonMigrationTests"` | **81/81** geçti, atlanan 0 |
| `dotnet test tests/BuildingFlow.IntegrationTests --filter "LeaseContractPersistenceTests\|ManagerWriteIsVerified"` | **4/4** geçti (yeni testlerin atlanmadığı kanıtı) |
| `dotnet test tests/BuildingFlow.IntegrationTests --filter "MobileAuthorizationTests\|MobileCanonicalSyncTests\|ManagementAssignment\|MigrationTests\|AutoMigrationTests\|MobileSessionTests\|PersonCodeApiTests"` | **85/86**; tek kırmızı `MobileCanonicalSyncTests.PhotoIsOptionalConsentProtectedAndBoundToCurrentContext` → 503, nesne deposu (S3) bu koşuda başlatılmadı; önceden belgelenmiş baseline, bu değişiklikle ilgisiz |
| `bash scripts/db.sh check` | model ↔ migration farkı yok |
| `bash scripts/check-idempotent-sql.sh artifacts/database/buildingflow.sql` | PASS (37 migration, iki kez) |

Kapsanan senaryolar: ortak kiracı, ikinci sözleşme, taraf aralık dışı (domain + DB commit), sözleşme bitişi/soft-delete/başlangıç değişimi açık tarafla, bağ değişmezliği, aynı kişi örtüşmesi, doğrulama kararı değişmezliği ve check, başka Property kanıtı, yabancı tenant ve başka daire composite FK, migration LEGACY backfill + reapply + boşta Down + dolu Down reddi, yönetici yazımında VERIFIED + aktör hesabı, hesapsız aktör 403, Ahmet portföy senaryosu, etkin ilişki sınırları. Frontend/mobil testleri çalıştırılmadı (değişiklik yok).

## 6) Bilinen riskler

1. Aynı dairede farklı kişilere örtüşen sözleşme engellenmiyor (iş kuralı kararı bekliyor).
2. Hesabı canonical `PersonUserAccount`'a bağlı olmayan SUPER_ADMIN artık native ilişki yazamaz (403). Bayrak kapalı olduğundan canlı etkisi yok; açılıştan önce yönetici hesap bağları hazırlanmalı (00-analysis §6.5 ile aynı önkoşul).
3. LeaseContract için yazma API'si yok; mevcut kiracı satırları sözleşmesiz. Sözleşme oluşturma/bağlama iş akışı FAZ 02/04.
4. LEGACY satırlar etkili sayılıyor; ileride "yalnız VERIFIED" politikası seçilirse portföy daralır, toplu doğrulama planı gerekir.
5. Evidence belgesi metadata soft-delete edilse de FK kalır; belge silme politikası FAZ 04'te.
6. AutoMigrate açık yerel ortamda migration otomatik uygulanabilir.

## 7) Sonraki faza geçiş koşulu

Kullanıcı bu raporu, LEGACY backfill seçimini ve risk 1'e (örtüşen sözleşme) kararını onaylarsa FAZ 02 (#34) başlar: yetkili kapsamlı kişi listesi, portföy (`PersonPortfolio`) okuma API'si, bağımlı seçim ve sunucu tarafı ilişki/sözleşme yazımı. Onay gelmeden FAZ 02'ye geçilmez.
