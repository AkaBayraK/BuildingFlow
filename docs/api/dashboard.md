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


## 21.10.2026 — Doluluk kartı tüketicisi

Tür dağılımı yerine doluluk halkası gösterilir. Oran occupiedUnitCount/unitCount'tur; sıfır toplamda —. vacantUnitCount gerçek boş bölüm sayısıdır; API'de pending/waiting alanı bulunmadığından beklemede sayısı ve halka payı üretilemez. unitTypes sözleşmesi/validasyonu korunur ve uygun daire/bölüm etiketini belirler. Endpoint/DTO değişmedi.


## 21.10.2026 — Aidat tahsilat kartı (BAKIM)

`collections` ek alanı eski istemciler için geriye uyumludur: önceki ay ve bu ay, artan sırada iki kayıt. Her kayıtta `period` (YYYY-MM), `totalAmount`, `collectedAmount`, `pendingAmount`, `overdueAmount` (TRY / iki ondalıklı string) bulunur. Aynı Property'nin iptal edilmemiş tahakkukları `DuesCharge.Period` üzerinden gruplanır; `totalAmount=collectedAmount+pendingAmount+overdueAmount`. Collected güncel PaidAmount; pending dueDate>=today olan bakiye; overdue dueDate<today olan bakiye. İptal/tersleme mevcut domain alanlarını izler. Nakit trendinin paidDate kapsamından bağımsızdır; önceki ay seçimi de bugün okunan bakiyedir, tarihsel kapanış değildir. Aynı RepeatableRead transaction, rol/aktif üyelik/silinmiş alan kontrolü korunur; kişi bilgisi eklenmez. Domain/EF/migration değişmedi.

Frontend seçili dönem oranını collected/total olarak BigInt ile hesaplar; sıfır toplamda — gösterir. Negatif/bozuk tutar, tutarsız toplam ve yanlış/sırasız dönem reddedilir. Eski API'de eksik/null collections için açıklamalı kullanılamaz görünümü, devre dışı dönem seçimi vardır. [Bu tur kabul kanıtı](../navigation-refactor/DASHBOARD_COLLECTION_EVIDENCE.json); **DashboardApiTests 7/7**, **frontend 191/191**, typecheck/build PASS.

## 21.10.2026 — MET Norway kişisel hava tahmini (BAKIM)

`GET /api/v1/weather?city=istanbul` yalnız CanManageDues (SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT), JWT/sub zorunlu; anonim401, diğer roller403. Property/kişi verisi içermeyen kişisel şehir tahminidir; üyelik bazlı finans özeti değildir. city yalnız istanbul/ankara/izmir/artvin; geçersiz/eksik değer400 ve mevcut BF_INVALID_REQUEST. Mevcut no-store ve okuma rate limit korunur. Serbest URL veya kullanıcı koordinatı kabul edilmez. Anahtar/ücretli abonelik gerekmez.

DTO: city (kanonik şehir adı), status FRESH/STALE/UNAVAILABLE, temperatureC (JSON decimal, yoksa null), symbol (MET kodu veya null), forecastAt (model zaman dilimi), updatedAt (MET veri güncellemesi), checkedAt (başarılı sunucu kontrolü), nextRefreshAt (cache/retry zamanı); tüm tarihler DateTimeOffset ISO8601. UNAVAILABLE durumunda sıcaklık/simge/başarılı tarihler null'dır. Sağlayıcı kesintisi normal widget veri durumu olarak200/UNAVAILABLE veya son başarılı değerle200/STALE döner; exception/yanıt gövdesi kullanıcıya açılmaz. FRESH bir istasyon ölçümü değil güncel şehir merkezi model tahminidir.

Uygulama → Infrastructure MetWeatherProvider, sabit HTTPS host/path, yönlendirme kapalı,8sn timeout,1MiB gövde sınırı. Dört sabit cache girdisi ve şehir başına SemaphoreSlim; manuel yenileme Expires'ı atlamaz. Last-Modified varsa koşullu GET;304 önceki veriyi korur.429/503 Retry-After ve hatada5dk bekleme; bozuk veri/eksi sınır/yanlış birim reddedilir. En yakın ±90dk tahmin saati seçilir,12saatten eski model veya güncel olmayan dilim STALE. Uygulama adı UA'da tanımlıdır; Weather:ContactUrl gerçek HTTPS iletişim adresi için isteğe bağlıdır. API hostname/iletişim bilgisi frontend girdisi değildir. Erişim logları HTTP istemcisinde kapalı, kullanıcı token/IP/konumu dış sağlayıcıya gönderilmez.

Frontend görünür sayfada cache zamanına göre1–30dk+jitter ile kontrol eder; gizli sayfada sorgu yapılmaz. Görünür olunca yeniden kontrol, Yenile ile anlık API kontrolü yapılır; sunucu cache geçerliliği korunur. İstek şehir/hesap değişiminde abort edilir, eski şehrin sıcaklığı gösterilmez. Kaynak/CC BY4 bağlantıları ve seçme/yuvarlama açıklaması korunur. Şehir adı üzerinden mevcut kişisel tercih penceresi açılır. [Teslim](../navigation-refactor/DASHBOARD_WEATHER.md), [lisans](../licenses.md).
