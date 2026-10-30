# Tanımlar (TNM 01–03)

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
