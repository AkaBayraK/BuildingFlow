# İŞ10 — Sayaçlar ve yönetim raporları

## Yetki ve kapsam

Her endpoint `/api/v1/properties/{propertyId}` altındadır; JWT realm rolü+aktif Property üyeliği backend tarafından kontrol edilir. SUPER_ADMIN scope dahil silinmiş Property okunmaz. Sayaç/okuma yazımı SUPER_ADMIN/SITE_ADMIN/MANAGER; okuma ve rapor ayrıca ACCOUNTANT. Kişi adı/id, DuesChargeParty veya Person arama/seçenek bilgisi raporlara eklenmez. Unit seçimleri mevcut `/dues/unit-choices` kaynağından; sayaç FK'sı aynı Property'nin aktif Unit'ine bağlanır. Ortak alan için unitId=null; apartmana sahte bina eklenmez.

## Endpointler

| Yöntem | Yol | Davranış |
|---|---|---|
|GET|`/meters?page=1&pageSize=10`|Sayaçlar; sayfa1–1000000, boyut1–100|
|POST|`/meters`|requestId(UUID),name1–100,kind(WATER/ELECTRICITY/GAS),unitId(nullable)|
|GET|`/meters/{meterId}/readings`|Yeni tarih önce; sayfalama ve revizyon geçmişi|
|POST|`/meters/{meterId}/readings`|Yeni tarih,endeks,gerekçe; version=0|
|PUT|`/meters/{meterId}/readings/{readingId}`|Aynı tarih için gerekçeli düzeltme; güncel version|
|GET|`/reports?period=YYYY-MM`|Tek alanın doğrulanmış aylık özeti|

Yazım sonucu200; aynı UUID/normalize içerik200 ve replayed=true. Farklı içerik veya sayaç/tarih/name tekilliği409. Liste sayfalama400, anonim401, rol403, erişilemeyen alan/kayıt404; HTTP semantiği korunur. Hatalar ErrorCatalog BF_METER_*/BF_READING_* ve mevcut ortak kodlarla TR/EN toast'a gider; ham exception render edilmez.

## Sayaç/okuma kuralları

WATER/GAS m³; ELECTRICITY kWh. Endeks API'de invariant string, `0.000`–`999999999.999`, en fazla3 ondalık; FE virgülü normalize eder, binlik ayırıcı kabul etmez. Tarih DateOnly,2000–bugün(Europe/Istanbul); gerekçe1–500. Sayaç/tarih tek; önceki≤yeni≤sonraki. Eşit endeks tüketim0, ilk baseline consumption=null. Araya okuma/düzeltme sonraki farkları tekrar hesaplatır; negatif tüketim/sayaç rollover desteklenmez, sayaç değişimi yeni kayıt gerektirir.

Düzeltme readingDate'i değiştirmez; immutable `meter_reading_revisions` satırı eski değer/gerekçe/tarih/sequenceleri korur. Mevcut reading value/version güncellenir; eski UUID replay eski değere döndürmez, güncel reading/history döner. Düzeltme409 sonrası FE güncel okumaları yükleyip yeniden açtırır; kör retry yapılmaz. Hiçbir endpoint finans/tahakkuk oluşturmaz, geçmişi silmez.

Üç tablo BaseEntity'nin sekiz NOT NULL alanını içerir. Property FOR UPDATE kilidi/audit/version her yazımda; composite Meter→Unit ve Reading→Meter, Revision→Reading FK. Revision SQL UPDATE/DELETE trigger'ıyla değişmez. Reading soft-delete/pasifleştirme check ile engellenir. Sayaç geçmişi Property silmeyi engeller. EF migration20261011064227_MetersAndReadings; dolu tablolarda Down reddedilir. Yerel sayaç edit/pasif/sil API'si bu kapsamda yoktur.

## Rapor anlamı

YYYY-MM2000–9998. Tek RepeatableRead snapshot; fan-out join ile parasal toplam katlanmaz. Sayılar mevcut silinmemiş Unit durumudur; aktif bölümlerde ay sonu gününü iki uç dahil kapsayan TENANT/RESIDENT ilişkisi bir kez sayılır. OWNER tek başına doluluk değildir. Ay sonu ilişkinin olmaması fiziksel binanın boşluğunun kanıtı değildir. Unit durumunun geçmiş snapshot'ı olmadığı için rapor tarihsel envanter bilançosu değildir.

- assessedAmount: o dönemin iptal edilmemiş tahakkuk toplamı.
- outstandingAmount: aynı tahakkukların **güncel** tutar−PaidAmount bakiyesi; geçmiş ay sonu bakiye snapshot'ı değildir.
- receiptsAmount: iptal edilmemiş Payment.paidDate o ay; tahakkuk nakit gelir sayılmaz.
- expensesAmount: iptal edilmemiş Expense.occurredDate o ay.
- consumption: endeks farkı **sonraki okumanın ayına** atanır; LAG önceki ay baseline'ını da kullanır. BaselineCount bilinmeyen tüketimler; ReadingCount dahil fakat toplam dışında. Üç ölçü türü ayrı, birimler toplanmaz. Düzeltilmiş güncel endeksler kullanılır; aylık proration/tarife değildir.

Para string iki ondalık, tüketim string üç ondalık; FE sayıları BigInt/ortak formatter ile gösterir. Boş veri0; bilinmeyen baseline ayrı belirtilir. CSV/PDF export, banka mutabakatı ve otomatik sayaç faturası bu teslimde yoktur.
