# Gider ve bütçe API sözleşmesi — İŞ 07

Temel yol: `/api/v1/properties/{propertyId}`. JWT realm rolleri SUPER_ADMIN, SITE_ADMIN, MANAGER veya ACCOUNTANT; SUPER_ADMIN dışındakiler için aktif, silinmemiş PropertyMembership gerekir. Property silinmişse kaynak 404; pasif alanda geçmiş okunur, yeni finans yazımı/iptal 409 döner. Bu modül muhasebeciye kişi verisi yetkisi açmaz. Liste, tek kayıt ve bütçe geçmişi aynı kapsamı uygular.

## Endpoint'ler

| Metot | Yol | Davranış |
| --- | --- | --- |
| GET | `/expenses` | Filtreli, sunucuda sayfalı gider listesi ve tüm filtre sonuçlarının özeti |
| POST | `/expenses` | Yeni gider: 201; aynı UUID/içerik replay: 200 |
| GET | `/expenses/{expenseId}` | Güncel gider ve iptal geçmişi |
| PUT | `/expenses/{expenseId}/cancel` | Version/gerekçeyle tek iptal; 200 |
| POST | `/budgets` | Alan/dönem/kategori planı oluştur veya beklenen version ile revize et; 200 |
| GET | `/budgets/{budgetId}` | Güncel bütçe ve değişmez revizyon geçmişi |
| GET | `/finance-summary?period=YYYY-MM` | Seçili ayın tahsilat, gider ve bütçe karşılaştırması |

Finans kaydı için DELETE/pasife alma/üzerine gider tutarı yazma endpoint'i yoktur. Hatalı gider önce gerekçeyle iptal edilir, sonra yeni kayıt oluşturulur; eski tutar ve geçmiş saklanır.

## Gider kaydı

```json
{
  "requestId": "88fc025d-e9a1-4917-b4c0-6e67e5aec9cd",
  "category": "MAINTENANCE",
  "amount": "1250.00",
  "currency": "TRY",
  "occurredDate": "2026-10-01",
  "description": "Asansör bakım gideri",
  "documentReference": "BAKIM-2026-10"
}
```

Örnek yalnız sözleşmedir; gerçek veri/ödeme emri değildir. Kategoriler sabittir: MAINTENANCE, CLEANING, STAFF, ENERGY, SECURITY, INSURANCE, ADMINISTRATION, OTHER. Sayısal enum değerleri kabul edilmez.

Tutar noktalı ondalık **string**, 0.01–99,999,999.99 TRY, en fazla iki ondalık; bilimsel gösterim, binlik ayırıcı, virgül, negatif ve sıfır gider reddedilir. FE virgülü normalize eder, API'ye noktayla gönderir. Açıklama trim sonrası 1–500, referans opsiyonel/en fazla 100 karakter. Tarih DateOnly, 2000-01-01 ile Europe/Istanbul iş günü arasında; gelecekte gider kaydı yoktur. Dosya yükleme bu fazın parçası değildir; referans bir numara/metin alanıdır.

Yanıt: `id`, `propertyId`, `requestId`, `category`, `amount` (iki ondalık string), `currency`, `occurredDate`, `description`, `documentReference`, `status` (RECORDED/CANCELLED), `version`, `createdAt`, `cancellationReason`, `cancelledAt`, `replayed`. İptal metadata'sı yalnız CANCELLED durumunda doludur.

UUID yönetim alanında tekildir. Kategori, normalize tutar/tarih, trim edilmiş açıklama ve referans SHA-256 fingerprint'e girer. Aynı UUID + aynı içerik yeni satır oluşturmaz; aynı UUID + farklı içerik 409 BF_EXPENSE_REQUEST_CONFLICT. İptal edilmiş giderin replay'i iptal durumunu döndürür ve FE uyarır. Belirsiz ağ sonucunda yalnız aynı normalize içerik/UUID ile manuel retry; farklı UUID ile kör tekrar yoktur.

İptal gövdesi `{"version":1,"reason":"Hatalı kayıt"}`. Gerekçe trim sonrası 1–500; version >=1. Eski version 409 BF_RECORD_CHANGED, güncel version ile ikinci iptal 409 BF_EXPENSE_ALREADY_CANCELLED. Tutar/açıklama/tarih değiştirilmez. Parent kilidi altında tek transaction/audit; finans geçmişi olan yönetim alanı silinemez.

## Gider listesi

`page` 1–1,000,000, `pageSize` 1–100 (varsayılan 10); `search` en fazla 200, açıklama/referansta literal büyük/küçük harf bağımsız arama; `%` ve `_` joker olarak çalışmaz. `period` opsiyonel YYYY-MM (2000–9998); `category` opsiyonel yukarıdaki kod; `status` RECORDED/CANCELLED. `sortBy`: occurredDate/amount/createdAt, `sortDirection`: asc/desc; varsayılan occurredDate desc, eşitlikte ID ile kararlı sıra.

Yanıt `items`, `page`, `pageSize`, `totalCount`, `summary`. Özet `recordedAmount`, `cancelledAmount`, `recordedCount`, `cancelledCount`, `currency`; yalnız görünen sayfanın değil **bütün filtre kapsamının** toplamıdır. Aylık finans görünümü bu arama/kategori/durum filtrelerinden bağımsızdır ve ekranda açıklanır.

## Bütçe ve revizyonlar

```json
{
  "requestId": "ccfeaf24-c54b-40f7-8765-15672955a22e",
  "period": "2026-10",
  "category": "MAINTENANCE",
  "amount": "1500.00",
  "currency": "TRY",
  "explanation": "Ekim bakım planı",
  "version": 0
}
```

Her Property + dönem + kategori için tek PropertyBudget. İlk oluşturma version=0; mevcut planı değiştirme için GET'teki güncel version. Tutar 0.00–99,999,999.99; **tanımlanmış sıfır plan, plan yok ile aynı değildir**. Dönem 2000-01…9998-12; açıklama 1–500. Her yazım immutable BudgetRevision üretir; mevcut tutar son revizyonu gösterir, eski açıklama/tutar/audit silinmez. Dönem/kategori değiştirilmez; sıfır revizyonla yeni plan kaydedilebilir. Eşzamanlı ilk oluşturma/revizyonda bir istek başarılı, eski version kullanan diğer istek 409 olur.

Yanıt `id`, `propertyId`, `period`, `category`, `amount`, `currency`, `version`, `revisions`, `replayed`, `appliedRevision`. Revizyonlar yeni→eski: `id`, `amount`, `sequence`, `explanation`, `createdAt`. requestId bu yönetim alanının bütün bütçe revizyonlarında tekildir. Fingerprint dönem/kategori/tutar/açıklama ve **beklenen version** içerir. Aynı UUID farklı içerik 409 BF_BUDGET_REQUEST_CONFLICT. Eski isteğin aynı içerik replay'i bütçeyi eski tutara geri döndürmez: güncel plan + bütün geçmiş döner, `appliedRevision` o isteğin geçmişte uygulanmış sıra numarasıdır. GET'te appliedRevision güncel son sıradır. FE 409'da açık “güncel bütçeyi yükle” akışıyla yeni version alır; kullanıcı açıklaması korunur, kör overwrite yapılmaz.

## Aylık hesaplar

`period` zorunlu, YYYY-MM. Bir RepeatableRead transaction içinde yetki, iptal edilmemiş Payment.paidDate tahsilatları, iptal edilmemiş Expense.occurredDate giderleri ve seçili dönemin güncel bütçeleri okunur. Tarih aralığı ay başı dahil/sonraki ay başı hariç. Tahakkuk gelir sayılmaz; iptal tahsilat/gider toplam dışında kalır.

Yanıt `propertyId`, `period`, `currency`, `incomeAmount`, `expenseAmount`, `budgetAmount`, `netAmount` (tahsilat−gider), `budgetVariance` (plan toplamı−gider) ve sekiz `categories`. Kategori: `category`, `expenseAmount`, `budgetAmount`, `variance`, `hasBudget`, `budgetId`, `budgetVersion`. Plan yoksa hasBudget=false, kimlik/version null; sıfır plan varsa hasBudget=true. FE plansız kategoride farkı “—” olarak gösterir, bütçe aşımı iddiasında bulunmaz. Global fark bütün giderleri ve tanımlanan plan toplamını kapsar; banka bakiyesi/gerçek banka mutabakatı değildir. Tüm tutarlar string; BE decimal, FE toplam/formatta kesin BigInt kuruş kullanır.

## Şema, güvenlik ve kanıt

`expenses`, `property_budgets`, `budget_revisions`: BaseEntity'nin sekiz NOT NULL alanı; decimal(10,2), UUID tekilliği, budget kategori/dönem tekilliği, scope composite FK, tarih/TRY/para/iptal/history check'leri. Her yazım Property row lock ve parent audit/version ile atomiktir. Revision append-only uygulama davranışıdır; doğrudan yetkili DB operatörüne karşı tarihçeyi kilitleyen trigger yoktur. Silinmiş Property/üyelik filtresi tarihsel okumada kaldırılmaz.

Yeni migration `20261009213050_ExpensesAndBudgets`; eski migration'lar korunur. Up yalnız üç tabloyu ekler. Down üç tablodan herhangi birinde tarihçe varsa hiçbirini silmeye başlamadan reddeder; iptal gider ve sıfır plan da tarihçedir. Ana DB'de rollback yapılmaz. Kurulum/yükseltme akışı [MIGRATIONS.md](../MIGRATIONS.md), test/kabul ve gerçek sınırlar [TESTING.md](../TESTING.md).

400/401/403/404/409/500 HTTP anlamları korunur; error/warning merkezi [ErrorCatalog ve TR/EN bildirim sözleşmesi](errors.md) üzerinden gelir. Yeni yedi kod ve alan çevirileri bu ortak sözleşmeye eklenmiştir. Yeni paket/ücretli bileşen kullanılmadı. Kategori CRUD, dosya eki, banka mutabakatı, gider dağıtımı/tedarikçi/ödeme emri bu fazın kapsamı değildir.
