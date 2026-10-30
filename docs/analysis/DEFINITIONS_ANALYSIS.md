# Yönetim → Tanımlar — analiz ve faz önerisi

Durum: TNM 01–03 TAMAMLANDI (30.10.2026); TNM 04–06 kullanıcı onayı bekler. Alana özel ek değer ertelendi.
Faz kimlikleri (TNM 01–06) öneridir; onaylanırsa milestones.ts ve TESTING.md'ye aynı kimlikle bağlanır.

## 1. Menü neden pasif?

`frontend/buildingflow-web/src/features/navigation/navigation.ts` içinde Sistem Yönetimi
grubu şöyle kurulur:

```ts
planned("definitions", "shellDefinitions", "SlidersHorizontal")
```

`planned()` öğeyi `disabled: true` ve `href` olmadan üretir. Yani Tanımlar bir hata değil,
bilinçli "planlandı" yer tutucusudur: route, sayfa (`app/(workspace)/definitions` yok) ve
backend API'si yoktur. Görünürlük `permissions.administration` (SUPER_ADMIN, ya da oturum
bağlamı kapalıyken SITE_ADMIN/MANAGER) ile sınırlıdır. Kayıt: docs/navigation-refactor/ANALYSIS.md
("Tanımlar | Hedef yok | disabled, Planlandı").

## 2. Mevcut envanter

### 2.1 Veritabanında zaten tablo olan kataloglar (yeniden yaratılmaz)

| Katalog | Tablo/entity | Kapsam | API | Ekran | Tanımlar'daki rolü |
|---|---|---|---|---|---|
| Rıza türleri + metinleri | `ConsentType`, `ConsentText` | Sistem | `/consents/types` CRUD | `/consents` | Bağlantı kartı; yeniden yazılmaz |
| Aidat iptal nedenleri | `DuesCancellationReason` (seed, `OTHER`, `TENANT_MOVED_OUT` sabit ID) | Sistem | yalnız `GET …/dues/cancellation-reasons` | Yok (yalnız iptal diyaloğunda seçim) | Yönetim ekranı eksik → TNM 04 |
| Tedarikçi kategorileri | `SupplierCategory` (TenantId + NameKey tekil) | Yönetim şirketi (tenant) | `GET/POST /api/v2/tenants/{id}/supplier-categories` | SupplierRelations içinde satır içi | Düzenle/pasif eksik → TNM 04 |
| SMS şablonları | `SmsTemplate` (PropertyId) | Yönetim alanı | `/properties/{id}/sms/templates` CRUD | SMS ekranı | Bağlantı kartı; şablon tanım değildir |
| Ödeme sağlayıcıları | `PaymentProvider` | Sistem | `/api/v1/payment-providers` | Sistem Ayarları | Bağlantı kartı |

`ParkingPolicy`, `LatePenaltyPolicy`, `LatePenaltySwitch` ayar/politikadır; Tanımlar kapsamına alınmaz.

### 2.2 Kodda sabit enum olan sınıflandırmalar (tablo yok)

Kayıtlar kodu metin olarak saklar; ikon/renk FE `*-style.ts` dosyalarında, etiketler i18n'de;
talep kategorileri mobilde (`mobile/.../sync/contracts.ts`) ayrıca doğrulanır.

| Liste | Kaynak | Değerler | Etki alanı | Aday mı? |
|---|---|---|---|---|
| Talep/arıza kategorisi | `MobileRequest.Categories` (string dizi) | 11 | Web + mobil doğrulama, ikon | Evet (düşük risk, mobil sözleşmesi değişir) |
| Duyuru kategorisi | `AnnouncementCategory` | 6 | Duyuru, mobil bildirim | Evet |
| Anket kategorisi | `SurveyCategory` | 7 | Anket özeti | Evet |
| Bakım türü | `MaintenanceType` | 5 | Bakım planı | Evet |
| Ortak alan türü | `CommonAreaType` | 8 | Ortak alan, rezervasyon | Evet |
| Araç tipi | `VehicleType` | 4 | Otopark | Evet (yalnız sistem seviyesi) |
| Gider kategorisi | `ExpenseCategory` | 8 | Gider, **bütçe (kategori başına tekil)**, aylık özet, raporlar | Evet ama ayrı faz (TNM 05) |
| Ek ödeme kategorisi | `ChargeCategory` | 6 | Tahakkuk, mobil finans | Evet ama ayrı faz (TNM 05) |
| Isıtma/oda/mutfak tipi | `HeatingType`, `RoomLayout`, `KitchenType` | — | Bina/ilan detayı | Sonra; yalnız sistem seviyesi |

### 2.3 Tanım yapılmayacak enumlar (davranış taşır)

Durum enumları (tüm `*Status`), `PropertyType`, `UnitType`, `OccupancyType`, `MeterKind`
(birim m³/kWh), `ChargeKind` (DUES/EXTRA), `AllocationKind`, `PaymentMethod`, `ConsentKind`,
`LatePenalty*`, `ParkingSpaceType`. Bunlar iş kuralı ve migration kontrolü taşır; kullanıcı
tanımına açmak geçmişi ve sözleşmeleri bozar.

### 2.4 Olmayanlar

- **Belge türü** alanı yok: `StoredDocument` yalnız dosya adı/MIME/boyut/görünürlük tutar.
  Belge türü istenirse önce alan + migration gerektirir (TNM 03 sonrası ayrı karar).
- **Aidat türü** ayrı tanım değil: `ChargeKind` DUES/EXTRA sabit, EXTRA'nın alt sınıfı `ChargeCategory`.

## 3. Önerilen yaklaşım: iki seviyeli tanım

Tek genel amaçlı EAV yerine **kod-sahipli liste kaydı + tek değer tablosu**:

- **Liste anahtarı kodda** (`REQUEST_CATEGORY`, `ANNOUNCEMENT_CATEGORY`, …): hangi listenin
  tanımlanabildiği, alan özel değere açık olup olmadığı ve hangi rolün düzenleyebildiği
  kodda sabittir. Kullanıcı yeni liste türü açamaz.
- **Seviye 1 — Sistem geneli (SUPER_ADMIN):** mevcut enum kodları seed edilir; kod değişmez
  ve silinmez. Düzenlenebilen: TR/EN ad, ikon (mevcut lucide setinden, kayıtlı ikon anahtarı),
  ton, sıra, aktif/pasif. Pasif değer yeni kayıtta seçilemez, geçmiş kayıtta okunur.
- **Seviye 2 — Yönetim alanı (SITE_ADMIN/MANAGER + aktif Property üyeliği):** sistem değerini
  o alanda gizle/göster ve sırala; listenin izin verdiği yerde alan özel ek değer
  (`P_` önekli kod, ikon zorunlu). Kullanılmış alan değeri silinmez, yalnız pasife alınır.
- **Etkin liste** = sistem değerleri − alanda gizlenenler + alan özel değerler, alan sırasıyla.
  Formlar ve mobil bu etkin listeyi okur; backend yazımda kodu etkin listeye karşı doğrular.
- Kayıtlar kodu saklamaya devam eder; geçmiş kayıt değeri silinse/pasifleşse de ad
  çözümlenir (yeniden yazma yok). Mevcut 2.1 tabloları kendi tablolarında kalır, Tanımlar
  ekranı onları kendi API'leriyle gösterir (mükerrer tablo yok).

Taslak tablolar (BaseEntity + `BaseEntityConfiguration.Configure`, EF migration ile):

- `definition_values`: list_key, code, property_id NULL (NULL = sistem), name_tr, name_en,
  icon_key, tone, sort_order; `uidx_definition_values_list_property_code` (partial, silinmemiş),
  composite FK property_id → properties.
- `property_definition_settings`: property_id, list_key, code, is_hidden, sort_order;
  sistem değerinin alan üzerindeki görünümü.

API taslağı:

- `GET /api/v1/definitions` — liste kaydı + sistem değerleri (yetkili roller).
- `PUT /api/v1/definitions/{listKey}/values/{code}` — SUPER_ADMIN, version + operationId.
- `GET /api/v1/properties/{propertyId}/definitions/{listKey}` — etkin liste (form/mobil).
- `PUT /api/v1/properties/{propertyId}/definitions/{listKey}` — alan gizle/sırala/ek değer, version.
- Hatalar ErrorCatalog'a yeni kodlarla; ProblemDetails ve 400/403/404/409 korunur.

Lisans: yeni paket yok. İkonlar mevcut lucide-react setinden, ekran shadcn/ui + TanStack Table.

## 4. Fazlar

| Faz | Kapsam | Kabul özeti |
|---|---|---|
| TNM 01 | Menü aktif, `/definitions` sayfası; salt okunur hub: 2.2 listeleri ikonlarıyla, 2.1 kataloglarına bağlantı kartları. Backend `GET /definitions` kod kaydından. | Menü testi, RBAC görünürlük, ikon kuralı, mükerrer route testi |
| TNM 02 | `definition_values` + seed migration; sistem seviyesi düzenleme (ad TR/EN, ikon, sıra, aktif/pasif). Düşük riskli listeler: talep, duyuru, anket, bakım, ortak alan, araç. | Şema sözleşme testi, version/409, SUPER_ADMIN negatif yetki, pasif değerin geçmişte okunması |
| TNM 03 | Yönetim alanı seviyesi: gizle/sırala + alan özel değer; formlar etkin listeyi okur; backend kod doğrulaması enum yerine kayıttan. | Üyelik/scope negatif testleri, IgnoreQueryFilters koruması, alan A değerinin alan B'de görünmemesi |
| TNM 04 | Mevcut kataloglar: aidat iptal nedeni yönetimi (sabit ID'ler kilitli), tedarikçi kategorisi düzenle/pasif; rıza/SMS/sağlayıcı bağlantıları. | Kullanılmış neden/kategori silinmez, snapshot bozulmaz |
| TNM 05 | Finans kategorileri (gider, ek ödeme): bütçe tekilliği, aylık özet ve raporların alan özel kategoriyle davranışı. Ayrı analiz onayı ister. | Bütçe/rapor regresyonu, tarihçe Down guard |
| TNM 06 | Mobil senkron (talep kategorisi doğrulaması sunucu listesinden), kalite kapısı: RBAC matrisi, migration zinciri, E2E. | `npm run check`/`build`, PostgreSQL entegrasyon, tarayıcı E2E |

## 5. Açık kararlar

1. Seviye 2 için kapsam Property mi, yoksa tedarikçiler gibi yönetim şirketi (tenant) + Property mi?
   Öneri: Property; tedarikçi kategorisi tenant kapsamında kalır.
2. Alan özel ek değere hangi listeler açılsın? Öneri: talep, duyuru, bakım, ortak alan; anket ve araç yalnız sistem.
3. Finans kategorileri (TNM 05) bu yol haritasına girsin mi, yoksa ayrı karar mı?
4. Belge türü alanı istenir mi?
