# Planlama & Bakımlar

Temel yol `/api/v1/properties/{propertyId}/maintenance-plans`. JWT `sub` ve aktif yönetim alanı üyeliği (veya SUPER_ADMIN) zorunludur. Okuma `CanManageDues` (SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT), yazma `CanManageHierarchy` (SUPER_ADMIN/SITE_ADMIN/MANAGER) ve aktif Property gerektirir. Silinmiş/pasif Property için yazım `BF_PARENT_INACTIVE`.

| İstek | Davranış |
| --- | --- |
| GET `?page=1&pageSize=10&search=&type=&status=&from=&to=` | Sayfalı liste; pageSize1–100, search≤200 (başlık/açıklama/konum/sorumlu, literal `%/_` kaçırılır); `from`/`to` `yyyy-MM-dd`, `from≤to`. Sıra plannedDate, createdAt, id. `typeCounts` beş türün tamamını sabit sırayla döner ve tür filtresini yok sayar (filtre çipleri için) |
| GET `/upcoming?limit=4` | Bugünden (Europe/Istanbul) itibaren PLANNED/IN_PROGRESS planlar, tarih sırasıyla; limit1–10. `nextDays`: önümüzdeki30 gündeki açık plan sayısı |
| GET `/{planId}` | Kapsamlı detay; başka Property veya silinmiş kayıt404 |
| POST | Gövde aşağıda; `version` yok sayılır. 201 + Location |
| PUT `/{planId}` | Aynı gövde + doğru `version`; yanlışsa409 `BF_RECORD_CHANGED` |
| DELETE `/{planId}?version=` | Soft-delete (BaseEntity), 204 |

Gövde: `title`1–200, `description`≤2000, `type` MAINTENANCE/CLEANING/INSPECTION/DRILL/OTHER, `plannedDate` 2000-01-01–2100-12-31, `location`≤200, `responsible`≤200, `status` PLANNED/IN_PROGRESS/COMPLETED/CANCELLED. Boş opsiyonel metin null kaydedilir. Geçersiz değer400 `BF_PLAN_INVALID`; aynı kurallar DB `ck_maintenance_plans_valid` ile de korunur.

Tablo `maintenance_plans` (BaseEntity kolonları, Property FK Restrict, index `(property_id, planned_date)`). Plan geçmişi olan Property silinemez. Migration `20261021103545_PlanningAndAnnouncementCategories` yalnız ekleme yapar; `Down` plan veya GENERAL dışı duyuru kategorisi varsa veri kaybını önlemek için durur.

Dashboard "Yaklaşan Önemli Tarihler" kartı mevcut dashboard `upcomingDues` ile `/upcoming?limit=4` sonuçlarını tarih sırasıyla birleştirir (aynı gün aidat önce, geçmiş tarih yok, en çok4). Plan isteği başarısız olursa yalnız kart içinde uyarı gösterilir; aidat satırları korunur.

## Ortak Alanlar FAZ05 bağlantısı — 26.10.2026

Mevcut plan `common_area_closures` üzerinden alana bağlanır; ikinci bakım planı üretilmez. Aktif kapanışla farklı güne taşıma409; COMPLETED/CANCELLED veya DELETE aynı Property kilitli transaction'da ilgili kapanışları kaldırır, önceki duyuruyu arşivler ve açılma duyurusu/bildirimini kaydeder. Rezervasyonlar kendiliğinden iptal edilmez. Bağlı olmayan bakım planlarının mevcut akışı korunur. [Kapanış sözleşmesi](common-areas.md), [bu turun kanıtı](../common-areas/05-report.md).
