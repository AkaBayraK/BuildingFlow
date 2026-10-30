# Tanımlar (TNM 01)

`GET /api/v1/definitions` — salt okunur, `CanManageHierarchy` (SUPER_ADMIN, SITE_ADMIN, MANAGER). Anonim 401; ACCOUNTANT/SECURITY 403.

Yanıt kod-sahipli `DefinitionRegistry`'dir (`backend/src/BuildingFlow.Application/Definitions.cs`); veritabanı tablosu yoktur. Değerler, yazımı doğrulayan domain enum/dizilerinden üretilir; böylece uç backend'in reddedeceği bir kodu listeleyemez.

```json
{
  "lists": [{"key": "REQUEST_CATEGORY", "group": "REQUEST", "level": "PROPERTY", "source": "MobileRequest.Categories", "plannedPhase": "TNM_02", "values": ["ELEVATOR", "..."]}],
  "catalogs": [{"key": "CONSENT_TYPES", "group": "COMMUNICATION", "scope": "SYSTEM", "route": "/consents"}]
}
```

- `group`: PROPERTY, UNIT, FINANCE, REQUEST, MAINTENANCE, PARKING, SUPPLIER, COMMUNICATION, DOCUMENT.
- `level`: FIXED (iş kuralı taşır, tanım olarak düzenlenmez; `plannedPhase` null), SYSTEM (sistem geneli düzenlenecek), PROPERTY (yönetim alanı ayrıca gizleme/sıralama/ek değer alacak).
- `catalogs`: kendi tablo/API/ekranı olan kataloglar; `route` null ise yönetim ekranı henüz yok (aidat iptal nedenleri, TNM 04).
- Belge türü listesi yoktur: `StoredDocument` tür alanı taşımaz.

Frontend `features/definitions/model.ts` yanıtı sıkı doğrular: bilinmeyen liste/katalog anahtarı yok sayılır; geçersiz kod, tekrar eden değer, FIXED↔plannedPhase uyumsuzluğu veya göreli olmayan route hata verir. Etiketler mevcut modül sözlüklerinden (`definitionLabels`), ikonlar modüllerin `*-style.ts` eşlemelerinden gelir.

Analiz ve sonraki fazlar: [DEFINITIONS_ANALYSIS.md](../analysis/DEFINITIONS_ANALYSIS.md).
