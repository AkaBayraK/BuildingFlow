# BF-MULTI FAZ 00 — Çoklu yönetim alanı / çoklu daire portföyü analizi

22.10.2026 (Europe/Istanbul). Durum: **analiz teslim edildi, kullanıcı onayı bekleniyor.** Plan: `buildingflow-multi-property-prompts/PHASES.md`, toplam **10 faz (FAZ 00–09)**. Bu plan İŞ/UI/AI, 15 fazlı mobil ve 11 fazlı BF-PERSON planlarından ayrıdır. GitHub: [#32](https://github.com/AkaBayraK/BuildingFlow/issues/32) (FAZ 00), #33–#41 (FAZ 01–09). FAZ 01 başlatılmadı.

Bu turda yalnız bu rapor yazıldı. Uygulama kodu, migration, snapshot, yapılandırma, `.env`, bağımlılık ve ortak progress belgeleri değiştirilmedi; canlı DB/Keycloak/kişisel veri okunmadı. Çalışma ağacındaki önceden var olan 18 değişiklik korundu. Uygulama testi çalıştırılmadı (analiz fazı).

## 1. Ana sonuç

**İstenen hedef modelin büyük kısmı 16–18.10.2026'da BF-PERSON refactor'ı (#6–#16) ile zaten kodlandı, ancak tamamı özellik bayraklarıyla kapalı ve gerçek veriye geçirilmedi.** Bugün çalışan sistem hâlâ eski (legacy) modeldir. Yeni paketi sıfırdan uygulamak ikinci bir kişi/ilişki modeli üretir; MASTER_PROMPT'un "sil baştan yazma" ve AGENTS.md'nin "ikinci bağımsız backlog oluşturma" kurallarıyla çelişir.

Önerim: BF-MULTI fazlarını **BF-PERSON canonical modeli üzerine "eksikleri tamamla + devreye al" planı** olarak yürütmek. FAZ 01–03'ün çoğu doğrulama ve küçük genişletme; asıl yeni iş FAZ 05 (malik/kiracı web portalı), FAZ 06 (mobil portföy UI), FAZ 08 (finans/duyuru/belge bağlam entegrasyonu) ve FAZ 09 (geçiş/aktivasyon) olur.

Ahmet Yılmaz senaryosu iki modelde de kısmen destekleniyor:

| Senaryo parçası | Legacy (çalışan) | Canonical (BF-PERSON, kapalı) |
| --- | --- | --- |
| Tek Keycloak hesabı | Var (`sub`) | Var (`PersonUserAccount` issuer+subject) |
| Farklı yönetim alanlarında kişi | Her Property'de **ayrı** `Person` satırı; aynı `LinkedUserId` ile bağlanır | Tenant içinde **tek** `CentralPerson`; `PersonPropertyScope` ile alanlara açılır |
| Aynı alanda çok daire | Aynı Person'a çok `UnitOccupancy` | Aynı kişiye çok `UnitOwnership/UnitTenancy/UnitResidency` |
| Malik + kiraya verilmiş | OWNER satırı; "kiraya verilmiş" türetilmiyor | OWNER satırı; başka kişide TENANT varlığından türetilebilir, read model yok |
| Kiracı + fiilen oturuyor | TENANT + RESIDENT iki satır | `UnitTenancy` + `UnitResidency` iki kayıt |
| İkinci kira sözleşmesi | Ayrı TENANT satırı; sözleşme kavramı yok | Ayrı `UnitTenancy`; **LeaseContract/LeaseParty yok** |
| Bağlam seçimi | Mobil `/api/v1/mobile/session` contexts (property+unit+role) | `/api/v2/session/contexts` (kind+relationId) web ve mobil ortak |
| Kendini daireye ekleyememe | Var: kişi kodu yalnız yöneticinin önceden kurduğu ilişkiye hesap bağlar | Var: ilişki yazımı yalnız yönetici; ancak **hesap↔kişi bağı kuran üretim akışı yok** |

## 2. Terim eşlemesi (isim varsayma kuralı)

| Paket terimi | Koddaki gerçek ad | Not |
| --- | --- | --- |
| ManagementArea (yönetim alanı) | `Property` (`properties`), tip APARTMENT/SITE/RESIDENCE/MIXED | "Site/Apartman" ayrımı PropertyType'tır |
| Building/Block | `Building` (opsiyonel; APARTMENT'ta `building_id NULL`) | Sahte blok eklenmez kuralı korunur |
| Unit | `Unit` (her zaman `property_id` taşır) | |
| Tenant (SaaS müşterisi) | `PersonTenant` | **Dikkat:** kod "Tenant" = müşteri, `TENANT` = kiracı. UI/rapor metinlerinde "müşteri" ve "kiracı" ayrı yazılmalı |
| Person | Legacy `Person` (Property'ye bağlı) ve canonical `CentralPerson` (tenant'a bağlı) | İkisi paralel yaşıyor |
| Ownership | Legacy `UnitOccupancy(OWNER)`; canonical `UnitOwnership` (pay numerator/denominator) | |
| LeaseContract + LeaseParty | **Yok.** Legacy `UnitOccupancy(TENANT)`; canonical `UnitTenancy` | Sözleşme kavramı yeni iş |
| Occupancy (fiili oturum) | Legacy `UnitOccupancy(RESIDENT)`; canonical `UnitResidency` | |
| Yönetici rolü | Keycloak realm rolleri SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT + `PropertyMembership`; canonical `ManagementAssignment` | Daire rollerinden ayrı (paket kuralıyla uyumlu) |
| Active context | Legacy mobil `propertyId:unitId:role`; canonical `tenantId:kind:contextId` | Sunucu her istekte yeniden doğrular |

## 3. Mevcut envanter

### 3.1 Veritabanı tabloları (ilgili)

| Grup | Tablolar / entity | Durum |
| --- | --- | --- |
| Hiyerarşi | `properties`, `buildings`, `units`, `property_memberships` | Çalışıyor, tüm modüller buna bağlı |
| Legacy kişi | `people` (PropertyId zorunlu, LinkedUserId, PersonCodeHash), `unit_occupancies` | Çalışıyor; merkez/mobil/finans buradan okur |
| Canonical kişi (BF-PERSON F03) | `PersonTenant`, `PersonUserAccount`, `PersonTenantMembership`, `CentralPerson`, `CentralPersonAccountLink`, `CentralPersonOperation` | Şema var, gerçek veri yok |
| Canonical ilişki (F04) | `UnitOwnership`, `UnitTenancy`, `UnitResidency`, `PersonPropertyScope`, `LegacyPersonMap`, `LegacyOccupancyMap`, `PersonUnitOperation` | Şema var, dry-run/apply geçiş servisi var, uygulanmadı |
| Görev (F05) | `ManagementAssignment` | Şema var, veri yok |
| Finans | `DuesAssessment/DuesCharge`, `DuesChargeParty` (borç kişi snapshot'ı, legacy Person'a bağlı), `Payment/PaymentAllocation` | Çalışıyor; canonical kişiye bağlı değil |
| İletişim | `Announcement` (DRAFT/PUBLISHED/ARCHIVED, ALL_MEMBERS), `StoredDocument` | Property düzeyi; daire düzeyinde görünürlük yok |
| Talep | `MobileRequests` + mobil outbox makbuzları | Legacy ve canonical (request-v3) bağlamla |
| Diğer bağlam tüketicileri | `Survey*`, `Consent*`, `MobileProfile`, `MobileNotices`, `Meter*` | Legacy mobil oturum bağlamı |

Tüm tablolar BaseEntity (id, version, audit, is_deleted, is_enabled) kuralına uyuyor; BF-PERSON migration'ları yalnız ekleyici ve dolu Down korumalı.

### 3.2 API rotaları

| Alan | Rotalar | Model |
| --- | --- | --- |
| Legacy kişi/ilişki | `/api/v1/properties/{propertyId}/people…`, `/occupancies…`, `/unit-choices`, `/unit-parties`, `/people/{id}/code` | **Yönetim alanı seçimi zorunlu** (paketin şikâyet ettiği bağımlılık) |
| Hiyerarşi | `/api/v1/properties/{siteId}/buildings/{blockId}/units…`, `/api/v1/properties/{id}/units…` | Bağımlı seçim için yeterli |
| Canonical kişi | `/api/v2/tenants/{tenantId}/people` (liste/arama/sayfa), `/people/{id}`, `/me/person` | Alan seçimi yok, tenant kapsamı; bayrak kapalı |
| Kişi merkezi | `/api/v2/…/people/{personId}/center`, `/unit-relations` (GET/POST/end), `/management-duties`, `/account-status`, `/person-properties`, `/person-unit-choices` | Portföy ve ilişki CRUD (FAZ 02 hedefinin çoğu) |
| Geçiş | `/api/v2/tenants/{t}/properties/{p}/person-transition/preview|comparison|apply` | Dry-run + kilitli apply |
| Görev | `/api/v2/tenants/{t}/properties/{p}/management-assignments…` | |
| Bağlam | `GET /api/v2/session/contexts`, `/{kind}/{id}`, `/website/{siteKey}/{audience}` | FAZ 03'teki `/me/portfolio` karşılığı |
| Mobil | `/api/v1/mobile/session`, `/owner`, `/tenant`, `/finance`, `/notices`, `/sync/commands|requests`, `/person-links`, `/registrations`; `/api/v2/mobile/authorization` | v1 legacy; v2 canonical yetki kopyası |
| Ödeme | `/api/v1/properties/{id}/payments` | Yalnız yönetici tahsilat kaydı; **online ödeme sağlayıcısı yok** |

### 3.3 Merkez web sayfaları

| Sayfa | Durum |
| --- | --- |
| `/properties/[id]/people`, `/properties/[id]/people/[personId]` | Legacy, alan seçimi zorunlu |
| `/people`, `/people/[tenantId]/[personId]` (`features/person-center`) | Canonical kişi listesi + detay (Genel / Bölüm ilişkileri / Görevler / Hesap); `BUILDINGFLOW_PERSON_CENTER_ENABLED=false` |
| `/workspaces`, `/workspaces/[kind]/[id]` (`features/session-workspaces`) | Giriş sonrası bağlam seçimi; malik/kiracı için yalnız salt okunur kendi kartı; `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED=false` |
| Malik/kiracı portalı (Portföyüm, aidat, ödeme, belge, duyuru, talep) | **Yok.** Web şu an yönetici uygulaması |

### 3.4 Mobil (Expo 57 / RN 0.86, SQLCipher, SecureStore)

| Parça | Durum |
| --- | --- |
| Sekmeler | `(tabs)/index, announcements, finance, requests, profile` |
| Bağlam seçimi | `auth/AuthProvider` + `preferences.selected_key`; legacy contexts çok alan/çok daire destekliyor |
| SQLite şema 8 | `account`, `cache_records`, `preferences`, `sync_outbox`, `sync_requests`, `sync_state`, `owner_snapshots`, `request_drafts/files/photo_cache`, `notice_jobs`, `profile_*`, `consent_*`; satırlar `user_id + context_key` ile ayrılmış |
| Canonical mod | `person-authorization/PersonModelScreen` salt okunur; `EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED` ve `_SYNC_ENABLED` false. Canonical modda finans/duyuru/yönetici ekranları **kapalı**, yalnız talep sync'i bağlandı |
| Offline giriş | Native'de SecureStore cihaz bağı + biyometri; parola/token saklanmıyor; lease 1 sa (yönetim) / 24 sa (malik/kiracı/sakin) |
| Ödeme | Mobil finans salt okunur; offline ödeme yok (kural zaten sağlanıyor) |

### 3.5 Testler

Backend entegrasyon (gerçek ayrı PostgreSQL): `CentralPeopleTests`, `PersonCenterTests`, `PersonTransitionTests`, `PersonRelationPersistenceTests`, `SessionContextTests` (yönetici+malik+sakin, kiracı+sakin, iki blokta uzaktan malik), `MobileAuthorizationTests`, `MobileCanonicalSyncTests`, `ManagementAssignmentTests`, `MobileSessionTests`, `MobileOwnerTests`, `MobileTenantTests`, `MobileFinanceTests`, `PersonCodeApiTests` ve migration testleri. Unit: `PersonUnitRelationTests`, `PersonCodeTests`, `ManagementAssignmentTests`. BF-PERSON F11 son kanıtı: backend seçili entegrasyon 366/366, web 149/149 (bugün 221/221), mobil 126/126. **Eksik:** iki farklı tenant'ta aynı hesap, aynı kişi için iki farklı yönetim alanında bağlam + finans izolasyonu, web malik/kiracı portalı e2e.

## 4. Faz bazında örtüşme ve gerçek eksikler

| BF-MULTI fazı | BF-PERSON'da var olan | Gerçek eksik (önerilen kapsam) |
| --- | --- | --- |
| 01 Kişi modeli + migration | CentralPerson, üç ilişki tipi, pay/örtüşme kuralları, geçiş dry-run/apply, LegacyMap'ler | `verifiedBy/verifiedAt/verificationStatus` ve belge referansı ilişkilerde yok; **LeaseContract + LeaseParty** (ortak kiracı, ikinci sözleşme, sözleşme dönemi) yok; "Kiraya verilmiş / Kendisi oturuyor / Aktif sözleşme" read model yok |
| 02 İlişkiler + kişi listesi API | Tenant kapsamlı liste/arama/sayfa, portföy (`/center`, `/unit-relations`), bağımlı seçim, audit/version | Maskeleme seviyesi gözden geçirme; SUPER_ADMIN için "tüm yetkili tenant'lar" çoklu tenant listesi (şu an tenant tek tek); N+1/cross-tenant ek testleri |
| 03 Yetki + aktif bağlam | `/api/v2/session/contexts`, sunucu tarafı yeniden doğrulama, negatif testler, yönetim rolü ≠ daire rolü | **Hesap↔CentralPerson doğrulanmış bağ kuran üretim akışı yok** (kişi kodu yalnız legacy Person'a bağlıyor). Portföy yanıtı ilişki başına; daire başına gruplu görünüm + eylem yetkisi (capability) matrisi |
| 04 Merkez Kişiler ekranı | `/people` listesi alan seçmeden, detayda ilişki sekmesi, ilişki ekle/sonlandır | Alan→blok→daire seçim UX'i kontrolü, doğrulama/belge kolonları, sözleşme iş akışı, çoklu alan UI testleri; legacy `/properties/[id]/people`'dan geçiş |
| 05 Malik/kiracı web portalı | Bağlam seçimi + kendi kartı | **Neredeyse tamamı yeni:** Portföyüm, Tüm Dairelerim / Seçili Daire, aidat/ödeme/belge/duyuru/talep görünümleri, permission matrix |
| 06 Mobil portföy | Legacy çoklu bağlam seçimi, canonical salt okunur ekran | Canonical bağlamda gruplu Portföyüm UI, her ekranda bağlam göstergesi, rol-bazlı sekmeler |
| 07 Offline SQLite | Hesap+bağlam anahtarlı cache, outbox, idempotency, sunucu reauth, logout temizliği, offline ödeme yok | Canonical modda finans/duyuru/belge metadata cache'i; iptal edilmiş ilişkide kuyruk UX'i; schema 9 gerekirse sürüm testi |
| 08 İş modülleri | Talep canonical bağlamla | Finans (`DuesChargeParty` legacy Person'a bağlı), duyuru (Property geneli), belge, anket, rıza, bildirim derin bağlantısı canonical bağlama taşınmadı; çok daire toplamlarında mükerrer sayım koruması yok. **Online ödeme sağlayıcısı yok** — "online ödeme" ayrı karar gerektirir |
| 09 Güvenlik + geçiş | F10 bulguları, F11 rollout listesi | F10 açık bulguları (mobil audit 18 high/11 moderate, geçmişte takipli SeaweedFS erişim bilgisi rotasyonu); gerçek tenant/kişi eşleştirmesi ve bayrak açılışı; kullanıcı kabulü (#16 hâlâ açık) |

## 5. Mevcut kişi filtrelerinin yönetim alanı bağımlılığı

- Legacy tüm kişi API'leri `/api/v1/properties/{propertyId}/…` altında; liste, arama, ilişki ve kişi kodu Property'ye kilitli. Merkezde "Kişiler" açılınca önce alan seçimi bu yüzden zorunlu.
- Finans (`DuesChargeParty`), mobil v1 session, anket yanıtı, rıza kapısı ve duyuru okuma legacy `Person.LinkedUserId + PropertyMembership + UnitOccupancy` üçlüsüne dayanıyor.
- Canonical `/api/v2/tenants/{tenantId}/people` bu bağımlılığı kaldırıyor ama tenant seçimi gerektiriyor; tek müşteri (Altunbayrak Rezidans) kararıyla pratikte tek tenant.

## 6. Veri taşıma ve mükerrer kişi riskleri

1. **Mükerrer kişi:** Legacy'de aynı insan her Property'de ayrı `Person` satırı. Ahmet iki alandaysa iki satır olur. `PersonTransitionStore` kaynak başına bir canonical eşleme yapıyor; **otomatik birleştirme yok** (paket kuralıyla uyumlu). Aynı kişinin iki legacy kaydını tek CentralPerson'a bağlamak için yönetici onaylı, kimlik numarası + hesap eşleşmesiyle önerilen, audit'li bir "eşleştir" adımı gerekir (FAZ 01/04 kapsamı). Yalnız ad/e-posta/telefon eşleşmesi kullanılmamalı.
2. **Hesap bağı:** Legacy `LinkedUserId` kişi koduyla doğrulanmış bir bağdır. Canonical'a taşırken bu doğrulanmış bağ `CentralPersonAccountLink`'e yönetici onaylı backfill ile aktarılabilir; yeni bağlar için canonical kişi kodu akışı gerekir.
3. **Finans geçmişi:** `DuesChargeParty` snapshot'ları legacy Person'a işaret ediyor; yeniden yazılmamalı (06.1 kuralı). Canonical tarafta `LegacyPersonMap` üzerinden okunmalı.
4. **Tenant eşlemesi:** Kullanıcı kararı: tüm kişiler tek müşteri "Altunbayrak Rezidans". Gerçek tenant/Property UUID eşlemesi henüz oluşturulmadı; uydurulmayacak.
5. **Bayrak sıkılığı:** `SessionContextsEnabled=true` olduğu anda merkez ManagementAccess ENFORCED olur; eksik canonical hesap/görev kaydı yöneticiyi dışarıda bırakır. Geçiş öncesi tüm yönetici hesapları için görev+hesap bağı hazırlanmalı.
6. **AutoMigrate:** Önceki fazlardan bekleyen migration'lar API başlangıcında otomatik uygulanabilir; canlı DB'de onaysız migration riski F07 raporunda da not edilmiş.
7. **Rollback:** Canonical tablolar ekleyici, legacy kaynak satırları değişmiyor; geri dönüş = bayrakları kapatmak. Dolu şemada Down çalıştırılmamalı. Yeni LeaseContract/verification alanları da ekleyici tasarlanmalı.

## 7. Test, etki ve migration riski özeti

| Risk | Etki | Önlem |
| --- | --- | --- |
| İki paralel kişi modeli | Çift yazıcı, tutarsız ekranlar | Tek yazıcı: canonical açıldığında legacy yazımları adaptörden geçsin; çift yazım yok |
| Kiracı/Tenant isim çakışması | Yanlış yetki kodu veya UI metni | Kod incelemesinde ve sözlükte ayrım |
| İlişki başına bağlam | Aynı daire için iki kart (TENANT + RESIDENT) | Portföy read model'i daire başına gruplasın, kapasiteler rozet olsun |
| Online ödeme yok | FAZ 08 "online ödeme" maddesi karşılanamaz | Ödeme sağlayıcısı ayrı kullanıcı kararı; yeni paket/lisans politikası (16.10.2026) gereği onay şart |
| Açık güvenlik bulguları | Yayın kapısı kapalı | FAZ 09 önkoşulu; FAZ 01–08 geliştirmesini engellemez |
| Yeni bağımlılık | Lisans politikası ihlali | Gerekmiyor: .NET/EF/Next/Expo kurulu modüller yeterli |

## 8. Bağımlılık ve lisans

Bu analiz için yeni paket gerekmiyor; FAZ 01–09 hedefleri mevcut .NET 10/EF Core 10/Npgsql, Next.js + shadcn/TanStack, Expo SQLite/SecureStore ile karşılanabilir. Lisans politikası (yeni üçüncü taraf yok) korunur. Online ödeme entegrasyonu dış sağlayıcı/SDK isterse kodlamadan önce ayrıca sorulacak. Mevcut açık lisans/güvenlik durumu F10 raporundadır ve bu turda yeniden taranmadı.

## 9. Onay için kararlar

1. **Temel model:** BF-MULTI, BF-PERSON canonical modeli üzerine kurulsun ve fazlar yalnız eksikleri kapsasın (önerilen) — yoksa ayrı yeni model mi?
2. **Sözleşme kavramı:** Kira için `LeaseContract + LeaseParty` eklensin mi (ortak kiracı, ikinci sözleşme), yoksa `UnitTenancy` yeterli mi? Öneri: ekle, `UnitTenancy`'yi sözleşme tarafı olarak bağla.
3. **Doğrulama alanları:** İlişkilere `verificationStatus/verifiedBy/verifiedAt` + isteğe bağlı belge referansı eklensin (önerilen).
4. **Mükerrer kişi birleştirme:** Yönetici onaylı, öneri tabanlı eşleştirme ekranı FAZ 04 kapsamına alınsın (önerilen); otomatik birleştirme yok.
5. **Online ödeme:** FAZ 08'de mevcut tahsilat kaydı görünürlüğüyle sınırlı kalsın; gerçek sanal POS ayrı iş olarak sonra (önerilen).

## 10. Sonraki faza geçiş koşulu

Kullanıcı bu raporu ve 9. bölümdeki kararları onaylarsa FAZ 01 (#33) başlar: yalnız domain genişletmesi (LeaseContract/LeaseParty, doğrulama alanları, portföy read model), ekleyici migration + dry-run + rollback tasarımı ve domain/geriye uyumluluk testleri. Production migration çalıştırılmaz.
