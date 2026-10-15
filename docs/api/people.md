# 04 — Kişiler ve bağımsız bölüm ilişkileri

Kök: `/api/v1/properties/{propertyId}`. JWT ve SUPER_ADMIN/SITE_ADMIN/MANAGER gerekir; global SUPER_ADMIN dışında aktif PropertyMembership gerekir. Anonim 401, diğer roller 403, erişilemeyen property/person/unit 404. Person giriş hesabı değildir; 12.10.2026 kişi kimlik/iletişim genişletmesi kullanıcı tarafından onaylandı.

| Yöntem | Kök altındaki yol | Davranış |
| --- | --- | --- |
| GET | `/people` | Kapsamlı arama, sıralama, sayfalama |
| POST | `/people` | Kişi oluşturma, 201/Location |
| GET / PUT | `/people/{personId}` | Detay / version ile güncelleme |
| DELETE | `/people/{personId}?version=1` | Geçmişi koruyan soft-delete, 204 |
| GET | `/unit-choices` | Bu property içindeki gerçek daire/bölüm seçenekleri |
| GET | `/occupancies` | Tarihsel liste; personId/unitId/search filtreleri |
| POST | `/occupancies` | Tarihli malik/kiracı/sakin bağı, 201/Location |
| GET | `/occupancies/{occupancyId}` | İlişki detayı |
| PUT | `/occupancies/{occupancyId}/end` | Bitişi bir kez version ile kaydetme |

Liste sorguları: page ≥1, pageSize 1–100, search en fazla 200 karakter. People sortBy: `name` (varsayılan) veya `createdAt`; sortDirection: `asc`/`desc`. GET yanıtı `{items,totalCount,page,pageSize}`. ID ve audit alanları istemciden alınmaz. Alan hataları 400 ValidationProblemDetails; version/domain/tarih çakışmaları 409 ProblemDetails.

Kişi oluşturma:

```json
{"firstName":"Deniz","lastName":"Örnek","phoneNumber":"+905551234567","email":"deniz@example.test","personKind":"NATURAL","citizenship":"DOMESTIC","countryCode":"TR","identityNumber":"12345678901"}
```

Ad/kurum adı zorunlu ve en fazla100 karakter; soyad yalnız gerçek kişide zorunlu, tüzelde boş string. Telefon zorunlu/en fazla30 karakter ve8–15 rakam; e-posta zorunlu/en fazla254 ve geçerli adres. Güncellemede bu alanlara `isEnabled` ve son okunan `version` eklenir. Kişi yanıtı id/propertyId, bu alanlar, createdAt, version, activeOccupancyCount içerir. Occupancy yazımı parent/person/unit version değerlerini de değiştirebilir; güncelleme öncesi güncel kayıt okunur.

İlişki oluşturma:

```json
{
  "personId":"11111111-1111-4111-8111-111111111111",
  "unitId":"22222222-2222-4222-8222-222222222222",
  "type":"OWNER",
  "startDate":"2026-10-01",
  "endDate":null,
  "isPrimaryContact":true
}
```

type OWNER/TENANT/RESIDENT; başlangıç zorunlu, bitiş isteğe bağlı ve başlangıçtan önce olamaz. ISO `yyyy-MM-dd`, iki uç dahil. Aynı kişi/bölüm/rol ve bir bölümde birincil iletişim tarih çakışmaları engellenir. Birden fazla malik/sakin desteklenir. Apartment doğrudan Unit; diğer türlerde Building altında Unit. Seçeneklerde pasif daire görünür fakat yeni bağlantı kuramaz. Pasif property/person/unit/building yeni ilişkiyi engeller.

İlişki yanıtı etiketleri (personName/unitNumber/buildingName), type, tarihler, primary, version, status içerir. Europe/Istanbul gününe göre ACTIVE/UPCOMING/ENDED hesaplanır. Bitiş isteği `{"endDate":"2026-10-07","version":1}`. Bitiş günü hâlâ geçerlidir; sonraki gün ENDED olur. Bitiş sonradan değiştirilmez, ilişki DELETE endpoint'i yoktur. Geçmiş isimler soft-delete sonrasında korunur. Devam eden/gelecekte biten ilişkide kişi ve daire silme/pasife alma 409 döndürür.

UI: Yönetim Alanları → detay → **Kişileri yönet**. Hassas kayıtlar anonim SSR HTML içine alınmaz; arama URL'ye yazılmaz. Oturum dönüş whitelist'i `/properties/{id}/people` yolunu kapsar.

## 06.1 — Kiracı taşınmasında yönetici onayı

Kullanıcı **otomatik iptal yerine yönetici onayı** seçmiştir. İlişkinin bitişini kaydetmek tek başına borç iptal etmez.

GET `/occupancies/{id}/end-preview?endDate=2026-10-15&version=1` aynı yönetici rolü + Property üyeliğiyle `{occupancyId,version,endDate,eligibleCount,totalAmount,skippedPaymentCount,items:[{id,version,period,amount,dueDate}],approvalAvailable}` döndürür. Date-only son gün dahildir. Yalnız bu TENANT ilişkisinin muhatap olarak kaydedildiği, **period ilk günü endDate'den büyük**, iptal edilmemiş ve hiçbir PaymentAllocation geçmişi bulunmayan sıfır ödemeli borçlar uygundur. Malik/daire muhatapları, eski veya aynı dönem, başka ilişki/kiracı, legacy borçlar korunur. Terslenmiş ödeme geçmişi bile otomatik toplu iptal kapsamına alınmaz; ayrı inceleme gerekir.

Uygun sayı ≤500 ise tutar/listenin tamamı gelir. Daha büyük sayıda toplam/sayı fakat boş items ve approvalAvailable=false gelir; UI toplu onay vermez. Önizleme hiçbir veriyi değiştirmez. Eski version 409; kapsam dışı kayıt 404.

PUT `/occupancies/{id}/end` gövdesi:

```json
{
  "endDate": "2026-10-15",
  "version": 1,
  "cancellationApprovals": [
    {"chargeId":"11111111-1111-4111-8111-111111111111","version":1}
  ]
}
```

UUID örnektir. Onay listesi gönderilmez veya boşsa yalnız bitiş kaydedilir. En fazla 500 farklı ID ve güncel version; sunucu kapsam/uygunluk/ödeme geçmişini tekrar denetler. Bir onay hatalı/stale ise **bitiş dahil hiçbir mutation kaydedilmez**. Listeye önizlemeden sonra oluşan borçlar sessizce eklenmez. Property FOR UPDATE kilidi tahakkuk/ödeme/iptal/ilişki oluşturma ve bitişini sıralar. Onaylanan iptaller TENANT_MOVED_OUT katalog ID/kod/adı + ilişki ID + bitiş gerekçesi + trusted audit ile ilişki bitişiyle aynı transaction'da yazılır. Borç tutarı ve snapshot'lar korunur.

Bitiş yanıtındaki `tenantDebtResult={cancelledCount,pendingCount,skippedPaymentCount}` işlem sonucudur; tekrar GET'te yeni iptal işlemi yapılmaz. UI'da açık onay başlangıçta kapalıdır; tarih/version/yenileme değişimi onayı temizler. Varsayılan eylem yalnız bitiş kaydıdır. Önizleme listesi onaysız kapatılırsa borçlar korunur. Manuel onaylı bitiş, ödeme/borç iptali kabul testleri kullanıcı verisinde çalıştırılmaz; gerçek ayrı PostgreSQL testlerinde doğrulanır.

## Daire listesinde bugünün kiracı ve malik özeti

`GET /api/v1/properties/{propertyId}/unit-parties?unitIds={uuid}&unitIds={uuid}`: 1–100 farklı dolu UUID, yönetici kişisel veri policy'si + aktif Property üyeliği (SUPER_ADMIN genel kapsamı mevcut kurala uyar). ACCOUNTANT dahil diğer roller403, anonim401. Eksik/bozuk/tekrarlı/aşırı seçim400; tek bir yabancı/silinmiş bölüm veya yanlış üst kapsam bütün batch'i404 yapar. Satır başına tüm kişi listesini indiren/N+1 API akışı yoktur.

Yanıt `{propertyId,asOfDate,items:[{unitId,buildingId,owners:[{personId,personName}],tenants:[{personId,personName}]}]}`; asOfDate Europe/Istanbul iş günü. OWNER/TENANT yalnız bu tarihte iki uç dahil geçerli ilişkilerden gelir; çoklu malik/kiracı ayrı döner. RESIDENT, geçmiş ve gelecek ilişki dahil değildir. Soft-delete edilen kişi/ilişki/ebeveyn için güncel isim gösterilmez. Pasif kayıt okuma politikası değişmez; etkinlik yeni ilişki kurulmasıyla ilgilidir. İsimler güncel Person adıdır; finans tahakkuk snapshot'ı veya tarihsel malik kanıtı değildir. Bir repeatable-read transaction bütün batch'i tutarlı okur, yazım/audit yoktur. Boş owners/tenants ilişki yok anlamındadır; hata bu boş sonuçla karıştırılmaz.

UI kişi adını `/properties/{id}/people/{personId}` bilgileri ve ilişkilerine bağlar. Ayrı kişi sayfası ve sağ detayda bölüm bağlantısı tam UUID query'siyle doğru doğrudan/binalı listeyi açar; silinmiş geçmiş bölümde ilgili liste boş kalır. Query isim/telefon/e-posta taşımaz. Kabul [TESTING.md](../TESTING.md).


## 12.10.2026 — Onaylı kişi profili genişletmesi

PersonKind NATURAL (gerçek) / LEGAL (tüzel), Citizenship DOMESTIC / FOREIGN. CountryCode iki harfli geçerli ülke kodu; yerli TR, yabancı TR dışı. IdentityNumber kişi kombinasyonuna göre: yerli gerçek11 rakam/ilk rakam0 değil T.C. numarası, yerli tüzel10 rakam vergi numarası, yabancı gerçek6–30 ASCII harf/rakam/tire pasaport, yabancı tüzel1–50 karakter ülke kuruluş/vergi/sicil numarası. Biçim doğrulanır; resmî kimlik/vergi/pasaport doğrulama servisi yoktur. Numara yalnız yönetici kişi sözleşmesinde taşınır; liste görünür kolonu değildir, log/URL arama değeri yapılmaz.

BirthDate isteğe bağlı1900–Europe/Istanbul bugün DateOnly; Gender null/FEMALE/MALE/OTHER/UNSPECIFIED. Tüzel kişide doğum tarihi/cinsiyet null olmalı. Address isteğe bağlı500 karakter. LinkedUserId mevcut aynı Property aktif üyeliğin kullanıcı subject'idir; kendiliğinden giriş hesabı oluşturmaz veya yetki/üyelik vermez. GET `/people/account-choices?page=1&pageSize=20&search=...` yönetici+scope ile aktif üyelik subject seçenekleri; ad/e-posta için Keycloak yönetim API'sine gidilmez. Şu an seçenek etiketi subject kimliğidir.

Legacy kişi alanları null kalır, geçmiş/audit/version/tahakkuk snapshot'ı değiştirilmez. Yeni POST/PUT tam profili ve zorunlu kontakları doğrular; legacy güncellemesinde eksikler kullanıcı tarafından tamamlanır. Telefon/email/name/şirket araması sunucuda aynı bounded pagination ve literal arama kuralıyla çalışır. People yanıtındaki propertyName ve units[] bugünün tüm OWNER/TENANT/RESIDENT ilişkilerinden gerçek bölüm/blok/property isimlerini içerir; çoklu roller gösterilir. Apartmanda building null; sahte blok üretilmez.

## Kişi mesajları ve giriş hesabı bildirim kutusu

Yönetici kökü altında POST `/people/{personId}/messages`: `{requestId:UUID,version,title,body,channel:"IN_APP"}`. Title1–200/body1–2000 düz metin; Property kilidi, kişi version/etkinlik, aktif parent ve alıcı üyeliği yeniden denetlenir. RequestUUID+normalize içerik fingerprint ile aynı istek aynı mesaj200 döner, değişik içerik409; başka gönderen eski mesajı replay ederek okuyamaz. Başarı kalıcı alıcı kutusuna kaydı bildirir; e-posta, SMS, broker/OS push veya cep cihazı teslim makbuzu değildir. SMS sağlayıcı bağlanana kadar kapalı; channel SMS400. Finans ve kişi mesajı içeriği loglanmaz.

GET `/people/{personId}/messages?page=1`: aynı yönetici+Property scope,10 satır mesaj geçmişi. `/api/v1/notifications?page=1` tüm girişli roller için yalnız JWT sub alıcısı ve hâlâ aktif Property üyeliği+aktif/silinmemiş parent;10 satır. PUT `/api/v1/notifications/{id}/read`: aynı alıcı scope404 izolasyonu ve idempotent readAt. Farklı alıcı/yetki query'si desteklenmez. PersonMessage BaseEntity, aynı Property composite FK, tek UUID ve sekiz NOT NULL kolonu taşır. Yeni migration Down, profile veya mesaj geçmişi varsa ilk drop öncesi reddeder.

Kişi işlem paneli `tel:` ile cihazın arama uygulamasını açar; uygulama kendi telefon görüşmesini yapmaz. Aidat geçmişi mevcut yönetici yetkili Dues personId filtresiyle DuesChargeParty snapshot'ını gösterir; güncel occupancy üzerinden eski borç kişiye atanmaz. ACCOUNTANT kişi/profile/account-choices yetkisi almaz. Rapor/finans DTO'larına T.C./vergi/pasaport/telefon bilgisi eklenmedi.
