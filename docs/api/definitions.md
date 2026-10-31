# Tanımlar (TNM 01–04)

`GET /api/v1/definitions` — salt okunur, `CanManageHierarchy` (SUPER_ADMIN, SITE_ADMIN, MANAGER). Anonim 401; ACCOUNTANT/SECURITY 403.

Yanıt kod-sahipli `DefinitionRegistry`'dir (`backend/src/BuildingFlow.Application/Definitions.cs`); veritabanı tablosu yoktur. Değerler, yazımı doğrulayan domain enum/dizilerinden üretilir; böylece uç backend'in reddedeceği bir kodu listeleyemez.

```json
{
  "lists": [{"key": "REQUEST_CATEGORY", "group": "REQUEST", "level": "PROPERTY", "source": "MobileRequest.Categories", "plannedPhase": "TNM_02", "editable": true,
    "values": [{"code": "ELEVATOR", "nameTr": null, "nameEn": null, "iconKey": null, "tone": null, "sortOrder": 0, "isEnabled": true, "locked": false, "version": 0}]}],
  "catalogs": [{"key": "CONSENT_TYPES", "group": "COMMUNICATION", "scope": "SYSTEM", "route": "/consents"}]
}
```

- `group`: PROPERTY, UNIT, FINANCE, REQUEST, MAINTENANCE, PARKING, SUPPLIER, COMMUNICATION, DOCUMENT.
- `level`: FIXED (iş kuralı taşır, tanım olarak düzenlenmez; `plannedPhase` null), SYSTEM (sistem geneli düzenlenecek), PROPERTY (yönetim alanı ayrıca gizleme/sıralama/ek değer alacak).
- `catalogs`: kendi tablo/API/ekranı olan kataloglar; `route` null ise yönetim ekranı henüz yok (aidat iptal nedenleri, TNM 04).
- Belge türü listesi yoktur: `StoredDocument` tür alanı taşımaz.

Frontend `features/definitions/model.ts` yanıtı sıkı doğrular: bilinmeyen liste/katalog anahtarı yok sayılır; geçersiz kod, tekrar eden değer, FIXED↔plannedPhase uyumsuzluğu veya göreli olmayan route hata verir. Etiketler mevcut modül sözlüklerinden (`definitionLabels`), ikonlar modüllerin `*-style.ts` eşlemelerinden gelir.

Analiz ve sonraki fazlar: [DEFINITIONS_ANALYSIS.md](../analysis/DEFINITIONS_ANALYSIS.md).

## TNM 02 — sistem geneli düzenleme

`PUT /api/v1/definitions/{listKey}/values/{code}` — yalnız SUPER_ADMIN (`CanManageProperties`). Gövde: `nameTr`, `nameEn` (boş/null → modül varsayılanı, max 100), `iconKey` ve `tone` (null veya `DefinitionRules.IconKeys`/`Tones` listesinden), `sortOrder` 0–999 (zorunlu), `isEnabled` (zorunlu), `version` (ilk kayıt 0). Yanıt güncel değer.

- Yalnız `editable=true` listeler (FIXED değil ve `plannedPhase=TNM_02`): ortak alan türü, talep kategorisi/önceliği, bakım türü, araç tipi, duyuru ve anket kategorisi. Diğerleri 400 `BF_DEFINITION_INVALID`; bilinmeyen liste/kod 404.
- OTHER/GENERAL/NORMAL kilitlidir; pasife alma 400 `BF_DEFINITION_LOCKED`.
- Sürüm uyuşmazlığı 409 `BF_RECORD_CHANGED`; aynı içerik eski sürümle tekrar gönderilirse güncel değer döner. Liste başına advisory lock, silinmemiş `(list_key, code)` tekil index.
- Tablo `definition_values` yalnız override tutar; satırı olmayan kod varsayılan değerlerle (`version=0`) listelenir. Kayıtlardaki kod değişmez. TNM 03 ile pasif değer yeni kayıtta reddedilir ve formlar ayarları kullanır.

## TNM 03 — yönetim alanı

`GET /api/v1/properties/{propertyId}/definitions` — `CanReadProperties` + aktif üyelik (SUPER_ADMIN tümü); üye değilse 404. Düzenlenebilir 7 listenin alan için etkin hâli: değer başına sistem ad/ikon/ton, `sortOrder` (alan sırası → sistem sırası → kayıt sırası), `isEnabled` (sistem), `isHidden` (alan), `selectable` (`locked || isEnabled && !isHidden`), `locked`, `version` (alan ayarı).

`PUT /api/v1/properties/{propertyId}/definitions/{listKey}/values/{code}` — `CanManageHierarchy` + aktif üyelik. Gövde `isHidden` (zorunlu), `sortOrder` (null = sistem sırası, 0–999), `version`. Yalnız `level=PROPERTY` listeler (ortak alan türü, talep kategorisi, bakım türü, duyuru kategorisi); diğerleri 400. Kilitli kod gizlenemez (`BF_DEFINITION_LOCKED`). Sürüm uyuşmazlığı 409, aynı içerik tekrarı güncel değeri döndürür.

Yazım koruması (`DefinitionPolicy`): duyuru, anket, bakım planı, ortak alan, araç, merkez talebi ve mobil talep senkronu yeni veya değişen kodu sistemde pasif ya da alanda gizliyse `BF_DEFINITION_UNAVAILABLE` (400, alan hatası) ile reddeder. Kaydın mevcut kodu ve kilitli kodlar her zaman kabul edilir; geçmiş kayıtlar değişmez.

Web formları `useDefinitionOptions` ile etkin listeyi okur (60 sn paylaşımlı önbellek, Tanımlar kaydında temizlenir); okunamazsa modülün sabit listesine döner. Mobil uygulama TNM 06'ya kadar sabit listeyi gösterir. Alana özel ek değer (`P_` kodu) bu fazda yok.

## TNM 04 — mevcut kataloglar

**Aidat iptal nedenleri** — `GET /api/v1/definitions/dues-cancellation-reasons` (`CanManageHierarchy`): tüm nedenler, `usageCount` (iptal edilmiş borç sayısı), `locked`. `POST` (SUPER_ADMIN): `code` `^[A-Z][A-Z0-9_]{1,49}$`, `name` 1–100, `requiresExplanation`, `isEnabled`, `version=0`; aynı kod + aynı içerik tekrarı mevcut kaydı döndürür, aksi 409 `BF_DEFINITION_DUPLICATE`. `PUT /{id}` (SUPER_ADMIN): kod değişmez (400), `version` zorunlu, uyuşmazlık 409 (aynı içerik tekrarı hariç). OTHER ve TENANT_MOVED_OUT pasife alınamaz ve açıklama kuralı değişmez (`BF_DEFINITION_LOCKED`); adları değişebilir. Borç iptali neden kod/adını o anda kopyalar; düzenleme geçmişi değiştirmez. Pasif neden alanın iptal listesinde ve iptal isteğinde kabul edilmez (mevcut davranış).

**Tedarikçi hizmet kategorileri** — ayrıntı [suppliers.md](suppliers.md): `GET /api/v2/tenants/{tenantId}/supplier-categories/manage` (sayfalı, pasifler dahil, `serviceCount`), `PUT .../supplier-categories/{id}` (`name`, `isEnabled`, `version`). Aynı tenant'ta normalize ad tekil (`BF_SUPPLIER_DUPLICATE`). Pasif kategori seçim listesinde ve yeni hizmet alanında kullanılamaz; mevcut hizmetler korunur ve listelenir.

## TNM 05 — finans kategorileri (30.10.2026)

Yukarıdaki TNM 02/03 liste sayıları tarihsel kapsamdadır. Artık 9 düzenlenebilir liste vardır; `EXPENSE_CATEGORY` (8 kod) ve `CHARGE_CATEGORY` (6 kod), `plannedPhase=TNM_05`, `level=PROPERTY`, `editable=true`. Sistem PUT yalnız SUPER_ADMIN; alan gizle/sırala PUT mevcut yönetici rolü + aktif Property üyeliği ister. ACCOUNTANT etkin listeleri okuyabilir, tanımları değiştiremez; kişi yetkisi açılmaz. Mevcut API yolları ve version/409/aynı içerik tekrar davranışı korunur. Kod ekleme/silme/yeniden adlandırma yok; OTHER kilitlidir. Yeni tablo/migration/paket yoktur.

Yeni gider, EXTRA tahakkuk, henüz plan olmayan kategori/dönem bütçesi ve yeni tedarikçi faturası pasif/gizli kodla 400 `BF_DEFINITION_UNAVAILABLE` (`category`) döndürür. Koruma scope ve parent kilidinden, UUID/fingerprint replay kontrolünden sonra çalışır. Eski istek aynı UUID/içerikle tekrarlandığında kategori artık gizli olsa da mevcut kayıt döner; farklı içerik eski 409 davranışını korur. Mevcut bütçe revize edilebilir (sıfır plan dahil), eski UUID tekrarı güncel bütçeyi + appliedRevision döndürür. Önceden kaydedilmiş faturanın onayı kayıtlı kategoriyle gider üretebilir; bu yeni kategori seçimi değildir. Düzeltme faturası yeni kayıt olduğundan etkin kategori ister.

Web: gider/EXTRA/fatura seçimleri etkin listeyi, etiket/ikon/renk mevcut tanımları okur. Gider filtresi ve bütçe tablosu pasif/gizli kategorileri de alan sırasıyla gösterir; yeni bütçe açma pasif/gizlide kapalı, mevcut plan revizyonu açıktır. Bütün 8 kategori aylık özette kalır; tutarlar ve yönetim raporu hesapları değişmez. Tarihsel kayıtlarda kod ve parasal geçmiş değişmez; kategori etiketi güncel sunum ayarıdır, tarihsel ad snapshot'ı değildir. Okuma hatasında mevcut TNM 03 sabit listeye dönüşü sürer; backend yazımı doğrular. Mobil liste/sunum uyumu TNM 06 kapsamındadır.

## TNM 06 — mobil uyum ve kalite kapısı

Mobil talep formu artık `GET /api/v1/mobile/sync/definitions` ile exact mobil bağlamda kategori/öncelik listesini okur; yönetici katalog uçları sakinlere açılmadı. Hesap/bağlam bazlı mevcut şifreli önbellek, 24 saat sınırı, offline güncellik uyarısı ve mevcut taslağı koruma uygulanmıştır. Ayrıntı [mobil senkron sözleşmesi](mobile-sync.md#tnm-06--etkin-talep-seçenekleri-30102026). Önceki TNM 03/05 mobil erteleme notları tarihsel kapsamdadır. Yeni alana özel kod ekleme ertelenmiş kalır.
