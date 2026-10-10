# 05 — Aidat ve borçlandırma API sözleşmesi

2026-10-08. Ortak model: Property → opsiyonel Building → Unit. Apartmanda doğrudan daireye, diğer türlerde binadaki bağımsız bölüme borç kaydedilir. Borç Unit’e aittir; malik/kiracı sorumluluğu veya ödeme otomatik oluşturulmaz.

## Yetki ve kapsam

Tüm uçlar Keycloak JWT ile SUPER_ADMIN / SITE_ADMIN / MANAGER / ACCOUNTANT gerektirir. SUPER_ADMIN dışındaki kullanıcı için aktif, silinmemiş PropertyMembership gerekir. Anonim 401, yetkisiz/sahte rol 403, kapsam dışı veya bulunmayan kayıt 404. ACCOUNTANT finans verisini görebilir; kişi/ilişki API’sinin yönetici rolü şartı korunur. Pasif alanın geçmişi okunur; yeni tahakkuk ve iptal 409 olur.

Kök: `/api/v1/properties/{propertyId}/dues`

| Yöntem / yol | Davranış |
| --- | --- |
| GET kök | Sunucuda arama, filtre, sıralama, sayfalama; filtre kapsamındaki toplamlar |
| GET `/{chargeId}` | Güncel version, tutar/bakiye, durum ve varsa iptal geçmişi |
| POST `/assessments` | Seçilmiş aktif bölümlere atomik toplu tahakkuk; yeni kayıt 201, aynı isteğin replay’i 200 |
| GET `/assessments/{assessmentId}` | Tahakkuk başlığı, bölüm kimlikleri ve gerçek toplam |
| GET `/unit-choices` | Aktif alan/bina/bölümden kişisel veri içermeyen sayfalı seçim listesi |
| PUT `/{chargeId}/cancel` | Gerekçe ve güncel version ile bir kez iptal; geçmiş tutulur |

## Tahakkuk oluşturma

```json
{
  "requestId": "d4ca0da3-eb8e-4da9-b0ea-c18416f9e34c",
  "unitIds": ["fe1bdfdd-962b-4b4c-aa29-ec132341c662"],
  "period": "2026-10",
  "amount": "1250.00",
  "currency": "TRY",
  "dueDate": "2026-10-31",
  "description": "Ekim 2026 aidatı"
}
```

Kimlikler örnektir. 1–500 farklı, mevcut, aynı alana bağlı ve aktif bölüm açıkça seçilir. `period` 2000 veya sonrası YYYY-MM; vade dönem başlangıcından önce olamaz. Açıklama trim sonrası 1–500 karakter. Tutar **ondalık string**, pozitif ve en fazla 99999999.99; en fazla iki ondalık. Virgül frontend girişinde kabul edilip noktaya çevrilir; API protokolü nokta kullanır. API/domain hassasiyet hatasını yuvarlayarak kabul etmez. TRY dışında para birimi bu sürümde yok.

Yanıt: `id`, `propertyId`, `requestId`, `period`, `amount`, `currency`, `dueDate`, `description`, `unitCount`, `totalAmount`, `unitIds`, `replayed`. Para alanları iki ondalıklı string; hesap backend decimal ile yapılır. Her bölüm için bir DuesCharge eklenir; herhangi bir satır hatalıysa hiçbir satır kaydedilmez.

Property satırı transaction içinde kilitlenir. Bir UUID + normalize edilmiş aynı içerik önceki sonucu döndürür; bölüm sırası, ondalık yazım ve çevresel açıklama boşlukları normalleştirilir. Aynı UUID farklı içerik ile 409. Aynı bölüm/dönem için iptal edilmemiş tek aidat vardır; DB partial unique index son savunmadır. Eşzamanlı ve tekrar istekler gerçek PostgreSQL testleriyle doğrulandı.

Frontend ağ/timeout/5xx sonucunda kör otomatik mutation retry yapmaz. Aynı form içeriğiyle elle yeniden gönderim aynı UUID’yi kullanır. İçerik değişirse yeni UUID üretilir; gönderim kilidi aynı anda ikinci tıklamayı önler. Dialog kapatılıp yeniden açıldığında yeni istek oluşur; bölüm/dönem tekillik kontrolü yine korunur. Sonuç belirsizse önce liste kontrol edilir.

## Liste / özet

`page=1`, `pageSize=10` (1–100), `search` (en fazla 200), `unitId`, `period`, `dueFrom`, `dueTo`, `status`, `sortBy`, `sortDirection`.

- Arama açıklama, bölüm numarası ve bina adı üzerinde; `%` / `_` literal kabul edilir.
- Faz 06 sonrası durum OPEN / PARTIALLY_PAID / OVERDUE / PAID / CANCELLED. Kalan bakiyesi olan vadesi geçmiş kayıt OVERDUE; tam ödeme PAID. `status=OUTSTANDING` kalan bakiyeli borçları getirir. Vade gününde gecikme olmaz; Europe/Istanbul iş günü kullanılır. Durum DB’ye zamanla eskiyen ayrı alan olarak yazılmaz. Kalan bakiye amount − paid_amount; özetler kalan bakiyeyi toplar.
- Sıralama `dueDate`, `period`, `amount`, `createdAt`; yön `asc` / `desc`, son anahtar UUID ile kararlı. Varsayılan vade azalan.
- Yanıt: `items`, `page`, `pageSize`, `totalCount`, `summary`. Özet `outstandingAmount`, `overdueAmount`, `cancelledAmount`, ilgili üç kayıt sayısı ve TRY içerir. **Sayfadaki satırlar değil, filtreye uyan tüm kayıtlar** hesaplanır. Liste/count/özet aynı RepeatableRead snapshot’ında okunur.
- Charge DTO: kimlikler, bölüm/bina adları, dönem, amount/balance/currency/dueDate/description/status/version/createdAt ve iptal gerekçesi/tarihi. Kimlikler sabittir; görünen bölüm/bina adları mevcut kayıttan okunur, isim snapshot’ı değildir.

## İptal / geçmiş

```json
{"reason": "Yanlış tutar girildi", "version": 1}
```

Boş/500 üstü gerekçe 400; eski version, ikinci iptal veya ödemeli borçta iptal 409. Ödemeli borcu iptal etmeden önce ilgili tahsilatlar gerekçeyle terslenir. İptalde tutar tutulur, bakiye 0.00 olur, gerekçe ve timestamp/audit/version kaydedilir. İptal edilen dönem yeni UUID ile yeniden tahakkuk edilebilir. Finans API’sinde silme, tutarı üzerine yazma veya kaydı pasife alma işlemi yoktur. Finans tablolarının soft-delete / etkinlik check’leri bu yolları reddeder; fiziksel ilişkiler Restrict’tir. Daire/bina sonradan soft-delete edilse de borç listesi geçmişi gösterir; açık scope/üyelik/Property silinme filtreleri IgnoreQueryFilters tarafından kaldırılamaz. Finans geçmişi olan Property silinemez; geçmişi koruyarak pasifleştirilebilir.

## Persistence / sınır

`DuesAndCharges` migration ve snapshot: `dues_assessments`, `dues_charges`; her ikisi BaseEntity’nin sekiz NOT NULL alanını taşır. UUID, ortak audit interceptor, optimistic concurrency, numeric(10,2), composite Unit/Assessment + Property FK, unique/check/index kısıtları vardır. İlk kurulum, eski veriyi koruyan upgrade, tekrar uygulama ve veri varsa Down reddi ayrı PostgreSQL test şemalarında doğrulanır. Önceki uygulanmış migration’lar değişmez. Ayrıntılar [MIGRATIONS.md](../MIGRATIONS.md) ve [TESTING.md](../TESTING.md).

Faz 06 ile kısmi/tam ödeme, tahsilat dağıtımı, uygulama içi tahsilat detayı ve gerekçeli tersleme eklendi; [payments.md](payments.md). Otomatik dönem üretimi, gecikme faizi ve bildirim sonraki kapsamdır. Tek bölüm/dönemde tek aktif aidat ve toplu işlemde aynı tutar kuralı korunur.

Transaction uygulaması için kullanılan resmî kaynaklar: [EF Core transaction yönetimi](https://learn.microsoft.com/en-us/ef/core/saving/transactions), [PostgreSQL satır kilitleri](https://www.postgresql.org/docs/current/explicit-locking.html).

## Toplu seçim — 2026-10-09

Yeni tahakkukta **Tüm sonuçları seç (N)**, mevcut aramaya uyan tüm sayfaları aynı yetkili unit-choices API'sinden 100'er kayıtla okur. Diğer filtrelerdeki seçimler korunur/dedup edilir; görünür sayfayı seç/çıkar, tek seçimi çıkar ve seçilenleri temizle eylemleri vardır. Seçili sayı, bölüm başına tutar ve toplam birlikte gösterilir; buton kaç bölüme borç oluşturacağını söyler. 500'den fazla sonuçta tümünü seç kapalıdır; kullanıcı aramayı daraltır veya sayfa bazında açık seçim yapar.

Toplu okuma bitmeden seçime parça parça ekleme yapılmaz. Count/page/duplicate/pasif kayıt veya network hatasında mevcut seçim korunur; iptal/unmount isteği durdurur. Mevcut seçimle birleşimin 500'ü aşması da bütün eklemeyi reddeder. Çok sayfalı seçim veritabanı snapshot'ı değildir; kaydetmede backend halen tüm Unit/Property/üyelik/dönem tekilliğini transaction içinde doğrular. Seçim sadece form state'idir; borçlar **tek mevcut POST** ile atomik kaydedilir. Yeni endpoint, şema veya paket yok.

## 06.1 — Kişi geçmişi ve muhatap (2026-10-09)

Tahakkuk isteğine `debtorType` eklenir: `UNIT` (eski istemciler için varsayılan), `TENANT`, `OWNER`. UNIT kişiye borç atamaz. TENANT/OWNER yalnız yönetici rollerine açıktır ve her seçilen bölümde **dönemin ilk gününü kapsayan tam bir ilişki** gerektirir. Eksik/çoklu muhatapta 400/debtorType; tüm batch geri alınır. Çoklu malik/sakin mevcut modelde geçerlidir; böyle bir durumda UNIT seçilebilir. Mode fingerprint'e dahildir; eski UNIT fingerprint'i aynı kaldığından önceki UUID retry sözleşmesi korunur.

Yeni her borçta o günkü ilgili OWNER/TENANT/RESIDENT ilişkilerinin `personId`, `occupancyId`, ad/soyad birleşimi, rol, başlangıç/bitiş ve `isDebtor` snapshot'ı saklanır. Ad tahakkuk oluşturulduğu anda kaydedilen addır; dönem tarihindeki geçmiş isim ayrıca bilinmiyorsa iddia edilmez. Sonradan kişi düzenleme/silme veya ilişki bitişi bunu değiştirmez. DTO ekleri: `partiesRecorded`, `partiesVisible`, `debtorType`, `parties`, `cancellationReasonCode`, `cancellationReasonName`. ACCOUNTANT için parties boş, partiesVisible false; kişisel arama, rol/kişi filtresi ve kişi seçenekleri 403. Genel borç araması bu role kişi eşleşmesi üzerinden sonuç ifşa etmez.

`personId` + `partyRole=OWNER|TENANT|RESIDENT` aynı snapshot ilişkisine uygulanır. Özet/count bütün filtre kapsamını izler. Yönetici araması ayrıca snapshot adında literal arar. GET `/person-choices?page=1&pageSize=20&search=...` yalnız borca kaydedilmiş farklı kişileri getirir: `{items:[{id,propertyId,name}],page,pageSize,totalCount}`. Güncel People listesinden alınmaz; en son kaydedilmiş eşleşen snapshot adı kullanılır, isim değişse de seçilen PersonId bütün tarihsel adlarını filtreler.

**Eski borçlar için güncel kişiler backfill edilmez.** `partiesRecorded=false`, UNIT, boş parties; UI açıkça geçmişin kaydedilmediğini belirtir. Bu bilgiye sonradan gerçeği belirsiz isim yazan düzenleme endpoint'i yoktur.

### Kayıtlı iptal nedenleri

GET `/cancellation-reasons` aktif katalog döndürür: `{id,code,name,requiresExplanation}`. Başlangıç beş nedeni: DUPLICATE_ASSESSMENT, WRONG_AMOUNT, WRONG_PERIOD, TENANT_MOVED_OUT, OTHER. `dues_cancellation_reasons` ortak BaseEntity sekiz zorunlu alanını taşır. Bu fazda katalog yönetim CRUD ekranı yok; sürümlü seed kullanılır.

PUT `/{id}/cancel` artık `{"reasonId":"katalog UUID","reason":"isteğe bağlı açıklama","version":1}` kabul eder. OTHER açıklama gerektirir; bilinmeyen/silinmiş/pasif neden 400. Diğer nedenlerde boş açıklama katalog adıyla doldurulur. Eski gerekçeli istemci ReasonId göndermiyorsa OTHER olarak kaydedilir. İptal anındaki neden kodu/adı da borçta saklanır; katalog adı sonradan değişirse geçmiş değişmez. Eski iptal metinleri/audit sürümleri aynen kalır, neden metadata'sı bilinmiyor olarak NULL kalır.

Taşınma iptali otomatik değildir; [people.md](people.md) onay listesiyle atomik ilişki bitişini açıklar. Migration `DuesPartyHistoryAndCancellationReasons`: iki yeni tablo, yeni alan/indeks/check/FK ve snapshot. Yeni tarihçe varsa Down bunu silmek yerine reddeder; ilk kurulum/yükseltme/tekrar/boş Down, audit ve gerçek şema sözleşmesi testleri TESTING.md içindedir.
