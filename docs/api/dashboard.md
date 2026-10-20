# İŞ20 — Dashboard özeti

`GET /api/v1/properties/{propertyId}/dashboard` — tek yönetim alanının salt okunur özeti. Politika `CanManageDues` (SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT) ve aktif Property üyeliği; SUPER_ADMIN tüm alanlar. Anonim 401, başka rol 403, üyeliği olmayan veya silinmiş alan 404. Tek RepeatableRead okuma; yazım yok, kişi adı/kimliği yok.

| Alan | Anlam |
|---|---|
| `today` | Europe/Istanbul bugünü (YYYY-MM-DD) |
| `unitCount`, `occupiedUnitCount`, `vacantUnitCount` | Etkin bağımsız bölümler; dolu = bugünü kapsayan TENANT/RESIDENT ilişkisi (rapordaki anlam, bugün için). OWNER tek başına doluluk değildir |
| `receiptsThisMonth` | İptal edilmemiş tahsilatların bu ayki `paidDate` toplamı (= `trend` son ay) |
| `overdueAmount`, `overdueChargeCount` | İptal edilmemiş, bakiyesi olan ve `dueDate < today` borçların kalan bakiyesi ve sayısı |
| `upcomingAmount`, `upcomingChargeCount` | Aynı borçlardan `today ≤ dueDate ≤ today+30` |
| `requests` | `{recorded, inProgress}` talep sayıları; yalnız yönetici rollerine, ACCOUNTANT'a `null` |
| `trend` | Son 6 ay (en eskiden bugüne) `{period, receipts, expenses}`; gelir `paidDate`, gider `occurredDate` |
| `unitTypes` | Altı UnitType'ın sabit sırada etkin bölüm sayısı; toplam `unitCount` |
| `upcomingDues` | Bakiyeli ve `dueDate ≥ today` borçların vade tarihine göre en yakın 4 grubu `{dueDate, chargeCount, amount}` |
| `maintenanceAvailable` | Planlı bakım modülü yok: her zaman `false` (T20.6) |

Para string iki ondalık (TRY). Web `features/dashboard/model.ts` tutarlılığı (dolu+boş, ay sırası, tür toplamı, vade sırası, sıfır/sayı eşleşmesi) kontrol eder; bozuk yanıt gösterilmez.


## Web banner ve alan seçimi —20.10.2026

Header alan seçimi yalnız Dashboard'u değiştirir;erişilebilir aktif alan listesi aynı properties API'sinden okunur. `/dashboard?property=UUID` yalnız tek doğrulanmış UUID ile giriş dönüşünde korunur. Hero alanı Property buildingCount ve özetunitCount/occupiedUnitCount kullanır;sakin sayısı veya hava bilgisi bu DTO'da yoktur. Ortak JPEG banner mevcut özel StoredDocument yollarında saklanır;summary DTO/endpointi değişmedi. [Tam sözleşme ve kanıt](../navigation-refactor/DASHBOARD_HERO.md).
