# Merkezi API hata ve uyarı sözleşmesi — İŞ 07.1

Beklenen mesajların tek backend kaynağı `backend/src/BuildingFlow.Application/ErrorCatalog.cs` içindeki ErrorCatalog sınıfıdır. Domain HTTP veya UI bağımlılığı almaz. API `application/problem+json` ile ASP.NET ProblemDetails sözleşmesini korur; UI dilleri `messages/tr.json` ve `messages/en.json` içindeki apiErrors / apiFields sözlüklerindedir.

```json
{
  "status": 400,
  "title": "Alanları kontrol edin.",
  "code": "BF_VALIDATION_FAILED",
  "message": "Alanları kontrol edin.",
  "parameters": {},
  "severity": "error",
  "errors": {"name": ["Yönetim alanı adı en fazla 200 karakter olabilir."]},
  "fieldIssues": {"name": [{
    "code": "BF_FIELD_TOO_LONG",
    "message": "Yönetim alanı adı en fazla 200 karakter olabilir.",
    "parameters": {"field": "propertyName", "max": 200},
    "severity": "error"
  }]}
}
```

HTTP 404 yalnız bulunamayan veya kapsam dışı kayıttır. 400 doğrulama/binding, 401 oturum, 403 yetki, 409 version/domain çakışması, 500 beklenmeyen hatadır. 405/415/429 da ortak kod taşır. `severity=warning` çakışmayı başarılı işlem yapmaz: 409 kalır, otomatik overwrite/retry yapılmaz. 401 WWW-Authenticate başlığı korunur. Empty API 404/401/403 yanıtları StatusCodePages ile aynı sözleşmeye bağlanır; health yanıtları bu dönüşüme girmez.

`message/title` güvenli backend varsayılanıdır. FE ham message/title/detail okumaz; bilinen kodu mevcut locale ile çevirir. Code bulunmayan eski response yalnız eski bilinen mesaj allowlist'iyle desteklenir. Code mevcut ama bilinmiyorsa güvenli HTTP fallback kullanılır. Code/status/severity ve parametre şeması frontend registry ile test edilir. Dönen severity körü körüne kabul edilmez. Alan adları ve fieldIssues allowlist/tip/sınır kontrolünden geçer; errors geriye uyumluluk içindir.

Bugünkü parametreler `field` (apiFields içindeki sabit alan kimliği) ve `max` (1–1000 tamsayı) ile sınırlıdır. Kişi adı, token, bağlantı, SQL/exception metni parametre olamaz. Yeni değişken için BE tanımı + FE parametre şeması + TR/EN ICU + negatif test birlikte eklenir. Beklenmeyen hata ve binding exception içeriği maskelenir; logger yalnız tür/durum yazar. TraceId/OpenTelemetry bu teslimin garantisi değildir.

## Frontend sunumu

`useAuthorizedApi` her başarısız response'u merkezi bildirim sağlayıcısına gönderir. ApiError alan hatalarını forma taşıdığı için değerler ve aria-invalid/aria-describedby/ilk hata odağı korunur. MessageBanner genel error/warning metnini sağ üste taşır; sayfada yalnız düzeltme/yenileme eylemi ve ekran okuyucu açıklaması kalır. FieldError mesajları tek microtask'ta toparlanır. Success/info mevcut ortak sunumuyla kullanılabilir; toastların hepsi aynı süreyi kullanır.

Bildirimler 8 saniyede kapanır; hover/klavye odağı süreyi durdurur, kalan süreyle devam eder. Kapatma düğmesi erişilebilir; error CircleX + alert/assertive, warning TriangleAlert + status/polite. Kuyruk en fazla dört bildirimdir; aynı önem seviyesi/metin yinelenmez. Abort sessizdir. Mobile sağ üst konumu, açık drawer üstü görünüm, Radix focus/dismissable branch ve bildirim kapatınca panelin açık kalması ortak katmanda sağlanır. Branch API'leri mevcut, lisansı incelenmiş Radix bağımlılıklarından kullanılır; yeni paket kurulmadı.

## Kod envanteri

Kodlar sabittir; çeviri metni kimlik olarak kullanılmaz. Yeni kod için katalog, registry, sözlük ve test güncellemesi zorunludur.

| Kod | HTTP | Önem | Parametreler |
| --- | --- | --- | --- |
| BF_PAYMENTS_ERROR_ALLOCATIONS | 400 | error | — |
| BF_DUES_ERROR_UNIT_SELECTION | 400 | error | — |
| BF_DUES_ERROR_PERIOD | 400 | error | — |
| BF_APARTMENT_NO_BUILDING | 409 | warning | — |
| BF_APARTMENT_DIRECT_UNITS_ONLY | 409 | warning | — |
| BF_ERRORS_SEARCH_BOUNDS | 400 | error | — |
| BF_UNIT_NUMBER_EXISTS | 409 | warning | — |
| BF_DUES_ERROR_DESCRIPTION | 400 | error | — |
| BF_UI_SELECT_AUNIT | 400 | error | — |
| BF_UI_ASTART_DATE_IS_REQUIRED | 400 | error | — |
| BF_PROPERTY_TYPE_LOCKED | 409 | warning | — |
| BF_UI_THE_END_DATE_CANNOT_PRECEDE_THE_START_DATE | 400 | error | — |
| BF_OCCUPANCY_ALREADY_ENDED | 409 | warning | — |
| BF_END_DATE_REQUIRED | 400 | error | — |
| BF_BUILDING_HAS_UNITS | 409 | warning | — |
| BF_ERRORS_AREA_BOUNDS | 400 | error | — |
| BF_BUILDING_UNIT_NUMBER_EXISTS | 409 | warning | — |
| BF_OCCUPANCY_OVERLAP | 409 | warning | — |
| BF_DUES_ERROR_CURRENCY | 400 | error | — |
| BF_PAYMENTS_ERROR_UNIQUE | 409 | warning | — |
| BF_DUES_ERROR_REQUEST_CONFLICT | 409 | warning | — |
| BF_PAYMENTS_ERROR_REQUEST_CONFLICT | 409 | warning | — |
| BF_PERSON_OCCUPANCY_OVERLAP | 409 | warning | — |
| BF_BUILDING_NAME_EXISTS | 409 | warning | — |
| BF_PRIMARY_CONTACT_OVERLAP | 409 | warning | — |
| BF_BUILDING_REQUIRED | 409 | warning | — |
| BF_DUES_ERROR_DEBTOR | 400 | error | — |
| BF_PAYMENTS_ERROR_BALANCE | 409 | warning | — |
| BF_PERSON_HAS_OCCUPANCY | 409 | warning | — |
| BF_PERSON_DELETE_HAS_OCCUPANCY | 409 | warning | — |
| BF_UNIT_HAS_OCCUPANCY | 409 | warning | — |
| BF_UNIT_DELETE_HAS_OCCUPANCY | 409 | warning | — |
| BF_DUES_OTHER_REASON_REQUIRED | 400 | error | — |
| BF_DUES_ERROR_APPROVAL | 400 | error | — |
| BF_ENABLED_REQUIRED | 400 | error | — |
| BF_DUES_ERROR_PROPERTY_HISTORY | 409 | warning | — |
| BF_DUES_ERROR_UNIT | 400 | error | — |
| BF_UI_ENTER_AVALID_EMAIL_ADDRESS | 400 | error | — |
| BF_ERRORS_VALID_PROPERTY_TYPE | 400 | error | — |
| BF_DUES_ERROR_STATUS | 400 | error | — |
| BF_DUES_ERROR_REQUEST_ID | 400 | error | — |
| BF_VERSION_REQUIRED | 400 | error | — |
| BF_DUES_ERROR_PERSON_CHOICES | 400 | error | — |
| BF_PERSON_INVALID | 400 | error | — |
| BF_DUES_ERROR_CHOICES | 400 | error | — |
| BF_PAYMENTS_ERROR_STATUS | 400 | error | — |
| BF_PAYMENTS_ERROR_DATE | 400 | error | — |
| BF_INVALID_PAGE | 400 | error | — |
| BF_INVALID_SORT_FIELD | 400 | error | — |
| BF_INVALID_SORT_DIRECTION | 400 | error | — |
| BF_PAYMENTS_REFERENCE_REQUIRED | 400 | error | — |
| BF_DUES_ERROR_AMBIGUOUS_DEBTOR | 400 | error | — |
| BF_BUILDING_FLOOR_METADATA | 409 | warning | — |
| BF_UI_THE_NUMBER_OF_FLOORS_MUST_BE_POSITIVE | 400 | error | — |
| BF_ERRORS_APARTMENT_FLOORS_ONLY | 400 | error | — |
| BF_ERRORS_APARTMENT_POSITIVE_FLOORS | 400 | error | — |
| BF_ERRORS_FLOOR_REQUIRED | 400 | error | — |
| BF_RECORD_CHANGED | 409 | warning | — |
| BF_DUES_ERROR_MOVE_OUT_REASON | 409 | warning | — |
| BF_PERSON_REQUIRED | 400 | error | — |
| BF_OCCUPANCY_TYPE_INVALID | 400 | error | — |
| BF_PAYMENTS_ERROR_METHOD | 400 | error | — |
| BF_DUES_ERROR_STALE_APPROVAL | 409 | warning | — |
| BF_DUES_ERROR_INACTIVE | 409 | warning | — |
| BF_PAYMENTS_INACTIVE_PROPERTY | 409 | warning | — |
| BF_ERRORS_PAGE_BOUNDS | 400 | error | — |
| BF_ERRORS_PAGE_SIZE_BOUNDS | 400 | error | — |
| BF_PAGE_SIZE_BOUNDS | 400 | error | — |
| BF_DUES_ERROR_DUPLICATE | 409 | warning | — |
| BF_DUES_ERROR_BUILDING | 409 | warning | — |
| BF_DUES_ERROR_DUE_RANGE | 400 | error | — |
| BF_PAYMENTS_ERROR_DATE_RANGE | 400 | error | — |
| BF_DUES_ERROR_UNIQUE | 409 | warning | — |
| BF_PHONE_TOO_LONG | 400 | error | — |
| BF_DUES_ERROR_AMOUNT | 400 | error | — |
| BF_DUES_ERROR_DUE | 400 | error | — |
| BF_PROPERTY_HAS_CHILDREN | 409 | warning | — |
| BF_PAYMENTS_ERROR_FUTURE_DATE | 400 | error | — |
| BF_PAYMENTS_ERROR_TOTAL | 400 | error | — |
| BF_PAYMENTS_DEBT_HAS_PAYMENTS | 409 | warning | — |
| BF_OCCUPANCY_PARENT_INACTIVE | 409 | warning | — |
| BF_PARENT_INACTIVE | 409 | warning | — |
| BF_DUES_ERROR_CANCELLED | 409 | warning | — |
| BF_PAYMENTS_ERROR_CANCELLED | 409 | warning | — |
| BF_DUES_ERROR_REASON | 400 | error | — |
| BF_DUES_CHOOSE_REASON | 400 | error | — |
| BF_VALIDATION_FAILED | 400 | error | — |
| BF_INVALID_REQUEST | 400 | error | — |
| BF_SIGN_IN_REQUIRED | 401 | error | — |
| BF_ACCESS_DENIED | 403 | error | — |
| BF_RESOURCE_NOT_FOUND | 404 | error | — |
| BF_UNEXPECTED_FAILURE | 500 | error | — |
| BF_METHOD_NOT_ALLOWED | 405 | error | — |
| BF_UNSUPPORTED_MEDIA | 415 | error | — |
| BF_RATE_LIMITED | 429 | error | — |
| BF_FIELD_REQUIRED | 400 | error | field |
| BF_FIELD_TOO_LONG | 400 | error | field, max |
| BF_FIELD_REQUIRED_MAX | 400 | error | field, max |

Kabul ve gerçek sınırlamalar: [TESTING.md](../TESTING.md), [analiz](../analysis/PHASE_07_1_API_FEEDBACK.md).

## İŞ 07 ile eklenen kodlar — 10.10.2026

Katalog artık **105 kod** içerir (07.1 teslimindeki 98 + bu fazdaki 7). Bütün kod/HTTP/severity ve TR/EN anahtar eşliği frontend testinde korunur. `category`, `occurredDate`, `documentReference`, `explanation` güvenli alan çeviri kaydına eklendi; error/warning aynı sağ üst 8 saniye/hover-focus/dedup altyapısını kullanır.

| Kod | HTTP | Severity | Parametre |
| --- | --- | --- | --- |
| BF_EXPENSE_CATEGORY_INVALID | 400 | error | — |
| BF_EXPENSE_DATE_INVALID | 400 | error | — |
| BF_EXPENSE_REFERENCE_INVALID | 400 | error | — |
| BF_BUDGET_AMOUNT_INVALID | 400 | error | — |
| BF_EXPENSE_REQUEST_CONFLICT | 409 | warning | — |
| BF_EXPENSE_ALREADY_CANCELLED | 409 | warning | — |
| BF_BUDGET_REQUEST_CONFLICT | 409 | warning | — |

Gider/bütçe version, para, açıklama ve sayfalama için mevcut ortak katalog kodları yeniden kullanılır. API doğrulama alan sorunları BF_VALIDATION_FAILED + fieldIssues olarak gelir. Ayrıntı [gider/bütçe sözleşmesi](expenses.md); eski teslimdeki test sayıları tarihsel kanıttır.

## İŞ 08 ile eklenen kodlar — 10.10.2026

Katalog **113 kod** içerir: önceki105 +8 duyuru/belge kodu. `content`, `audience`, `file`, `announcementId` güvenli alan çeviri kaydında; bütün kod/HTTP/severity/TR/EN eşliği test edilir. Genel error/warning aynı sağ üst8 saniye bildirimidir; HTTP413 ve503 de korunur.

| Kod | HTTP | Severity | Parametre |
| --- | --- | --- | --- |
| BF_CONTENT_INVALID | 400 | error | — |
| BF_CONTENT_STATE | 409 | warning | — |
| BF_DOCUMENT_INVALID | 400 | error | — |
| BF_DOCUMENT_TOO_LARGE | 413 | error | — |
| BF_DOCUMENT_REQUEST_CONFLICT | 409 | warning | — |
| BF_DOCUMENT_LIMIT | 409 | warning | — |
| BF_STORAGE_UNAVAILABLE | 503 | error | — |
| BF_CONTENT_PROPERTY_HISTORY | 409 | warning | — |

Validasyon sorunları BF_VALIDATION_FAILED+fieldIssues içinde kodlanabilir; ham SDK/exception mesajı gösterilmez. Ayrıntı [duyuru/belge sözleşmesi](communication.md).
