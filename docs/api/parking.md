# Araçlar / Otopark — veri modeli ve iş kuralları (FAZ 02)

Bu belge FAZ 02–05 domain, HTTP API ve web sözleşmesidir.

## Kullanıcı kararları (27.10.2026)

1. Araç kişiye bağlıdır (`vehicles.person_id`); daire ilişkisi ayrı alan olarak tutulur (`vehicles.unit_id`) ve otomatik türetilmez.
2. Araç kaydı `PENDING` başlar; yönetici onayı (`APPROVED`) veya gerekçeli reddi (`REJECTED`) zorunludur.
3. Daire ve kişi başı araç kotası ile daire başı park yeri kotası `parking_policies` içinde tutulur.
4. Misafir aracını yönetici ve güvenlik görevlisi (`SECURITY` realm rolü, FAZ 03) oluşturur.
5. Kamera, ANPR ve QR ikinci aşamadır. Bu fazda giriş-çıkış (AccessEvent) tablosu eklenmedi.
6. Saklama süresi `parking_policies.retention_days` alanında tutulur, varsayılanı 30 gündür (1–365).

## Tablolar

Tüm tablolar `BaseEntity` sekiz NOT NULL alanını (`id`, `version`, `created_at`, `created_by`, `modified_at`, `modified_by`, `is_deleted`, `is_enabled`) taşır.

| Tablo | Amaç | Kapsam / bütünlük |
|---|---|---|
| `parking_policies` | Kota, misafir azami süresi, saklama süresi | Property başına tek aktif kayıt; araç 1–20, park yeri 0–20, misafir 1–168 saat |
| `parking_facilities` | Otopark | Property'ye ait; `building_id` NULL ise site ortak, dolu ise aynı Property'nin bloğu (composite FK). Ad Property içinde tekil (citext) |
| `parking_levels` | Kat | Aynı Property'nin otoparkına bağlı; ad otopark içinde tekil, sıra −10…50 |
| `parking_spaces` | Park yeri | `STANDARD/ACCESSIBLE/VISITOR/EV_CHARGING/MOTORCYCLE`; kat aynı otoparka ait olmalı (`facility_id, level_id` FK); kod otopark içinde tekil |
| `vehicles` | Kayıtlı araç | Kişi ve daire aynı Property'de (composite FK). Plaka normalize edilir (`A-Z0-9`, 2–12). `PENDING/APPROVED` durumunda plaka Property içinde tekil |
| `parking_allocations` | Park yeri tahsisi | Daireye, isteğe bağlı araca; DateOnly aralık, iki uç dahil. Bir park yerinde tek açık uçlu tahsis (partial unique). Ziyaretçi yeri tahsis edilemez |
| `guest_vehicles` | Misafir aracı ve geçici park | Ev sahibi daire, isteğe bağlı park yeri; `PLANNED → ARRIVED → DEPARTED` veya `PLANNED → CANCELLED`. Süre politikanın azami saatini aşamaz |

## Modelleme kararları

- **Park hakkı (ParkingEntitlement):** ayrı tablo yerine daire başı kota (`max_spaces_per_unit`) ve tarihli tahsis kaydı kullanıldı. Daireye özel istisna gerekirse sonraki fazda eklenir.
- **Rezervasyon:** misafir/geçici park `guest_vehicles` ile modellendi (görseldeki "Rezervasyonlar (Misafir/Geçici)"). FAZ05 kullanıcı kararıyla mevcut yetki korunur: sakin self-servis rezervasyonu eklenmez.
- **Giriş-çıkış:** misafir aracının geliş ve çıkışı güvenlik görevlisi tarafından elle `arrived_at/departed_at` olarak kaydedilir. Kamera/ANPR olay tablosu ikinci aşamadadır.
- **Çakışma:** tarih aralığı çakışmaları (kapalı tahsis aralıkları, aynı yerdeki misafir pencereleri) mevcut `UnitOccupancy` kalıbıyla domain `Overlaps` kontrolü ve FAZ 03'te Property kilitli transaction ile engellenir. DB, açık uçlu tahsis ve aktif plaka tekilliğini kendisi korur. `btree_gist` exclusion constraint eklenmedi.
- Mevcut `properties/buildings.parking_type` özniteliği değiştirilmedi.

## API (FAZ 03)

Kök: `/api/v1/properties/{propertyId}/parking`. Tüm çağrılar JWT `sub` ve aktif Property üyeliği ister (`SUPER_ADMIN` hariç); üye olmayan veya başka Property kaydı **404** döner. Yazımlar Property satır kilidi (`FOR UPDATE`) ve üst kayıt version artışıyla tek transaction'da yapılır. Güncelleme/silme/durum değişikliği beklenen `version` ister; uyuşmazlık **409 `BF_RECORD_CHANGED`**.

| Yetki | Rol | Uç noktalar |
|---|---|---|
| Yönetim | `SUPER_ADMIN`, `SITE_ADMIN`, `MANAGER` | `GET/PUT /policy`; `GET/POST /facilities`, `PUT/DELETE /facilities/{id}`; `POST /levels`, `PUT/DELETE /levels/{id}`; `GET/POST /spaces`, `PUT/DELETE /spaces/{id}`; `GET/POST /vehicles`, `PUT /vehicles/{id}`, `POST /vehicles/{id}/approve|reject|deactivate`; `GET/POST /allocations`, `POST /allocations/{id}/end`; `GET /units/{unitId}/residents` (bugün geçerli malik/kiracı/sakin adı ve ilişkisi) |
| Güvenlik | yönetim rolleri + `SECURITY` | `GET /units?search=` (daire no + blok, en fazla 50; kişi verisi yok); `GET/POST /guests`, `POST /guests/{id}/arrive|depart|cancel`; `GET /plates?search=` (yalnız onaylı plaka, daire no, blok, tür; kişi verisi yok) |
| Sakin | token OWNER/TENANT/RESIDENT veya yönetim rolü; aktif `OWNER/TENANT/RESIDENT` ilişkisi ve `people.linked_user_id = sub` | `GET /api/v1/parking/my-vehicles?propertyId&unitId`, `POST /api/v1/parking/my-vehicles`, `POST /api/v1/parking/my-vehicles/{id}/deactivate?propertyId` |

`ACCOUNTANT` ve diğer roller **403** alır; anonim **401**. Listeler sayfalıdır (`page`, `pageSize` ≤ 100); plaka araması 2–12 karakter, en fazla 50 sonuç.

İş kuralları:
- Yönetici araç kaydı onaylı (`APPROVED`) oluşur; sakin kaydı `PENDING` oluşur ve yönetici onaylar veya gerekçeyle reddeder. Onay sırasında kota onaylı araçlara göre yeniden kontrol edilir.
- Araç kaydı için kişinin seçilen daireyle bugün geçerli `UnitOccupancy` ilişkisi şarttır (`BF_PARKING_RELATION_REQUIRED`). Politika yoksa `BF_PARKING_POLICY_REQUIRED`; kota dolu ise `BF_PARKING_QUOTA_EXCEEDED`; aktif plaka varsa `BF_PARKING_PLATE_TAKEN`.
- `requestId` (UUID): aynı içerikle tekrar gönderim aynı kaydı döndürür; farklı içerik `BF_PARKING_REQUEST_REUSED`. Araç ve misafir kaydında zorunludur.
- Tahsis: ziyaretçi yeri tahsis edilemez; aynı yerde çakışan aralık veya daire park yeri kotası aşımı 409. Araç pasife alınınca açık tahsisi bugün kapanır, gelecekteki tahsisi silinir.
- Misafir: süre politikadaki azami saati aşamaz; park yeri seçilirse `VISITOR` türünde ve aynı aralıkta boş olmalıdır.
- Kullanımdaki otopark/kat/park yeri silinemez (`BF_PARKING_IN_USE`). Uygun olmayan durum geçişi `BF_PARKING_STATE_INVALID`.
- Rate limit genel API politikasından gelir (kimlikli okuma/yazma, kodlu 429).

Keycloak: `deploy/keycloak/realm-buildingflow.json` içine `SECURITY` realm rolü eklendi. Mevcut kurulu realm'e import otomatik yapılmaz; rol Keycloak yönetim konsolundan eklenmeli ve görevliye atanmalıdır. Kullanıcı yönetimi ekranından rol atama FAZ 04 kapsamındadır.

Bilinen sınır: sakin uç noktaları legacy `people.linked_user_id` bağını kullanır; merkezi kişi (PersonRefactor) bağlamı FAZ 06'da mobil ile birlikte bağlanacaktır.

## Web (FAZ 04)

`/parking` alan seçimi, `/properties/{id}/parking` modülü (`features/parking`). Menü öğesi yönetim rolleri ve `SECURITY` için görünür; ACCOUNTANT için gizlidir. Sekmeler: Genel Bakış (KPI + harita), Araçlar (onay/ret/pasif, yönetici kaydı), Otopark Yönetimi (otopark/kat/park yeri, harita), Tahsisler, Misafir Araçları (plaka sorgu dahil), Kurallar. Güvenlik görevlisi yalnız Misafir Araçları sekmesini görür. Harita doluluğu bugünkü tahsis kayıtlarından gelir; kamera/sensör verisi kullanılmaz, gösterilmez. Her durum ikon + metin taşır. Araç ve misafir formları içerik değişmedikçe aynı `requestId`'yi korur; 409 sonrası "Güncel kaydı yükle" akışı vardır. FAZ05: güvenlik görevlisi yalnız `/guest-policy` ve `/guest-spaces` üzerinden süre kuralını ve ziyaretçi yerlerini okur; `/policy` ve genel `/spaces` yönetim yetkisinde kalır.

## Yetki ve KVKK

- Yazım: aktif Property üyeliği ve `SUPER_ADMIN/SITE_ADMIN/MANAGER`. `ACCOUNTANT` plaka ve kişi verisine erişmez.
- Sakin: yalnız tarihli `UnitOccupancy` ilişkisinin geçerli olduğu daireleri ve kendi araçlarını görür. Taşınma veya bitmiş ilişki erişimi kapatır.
- Güvenlik görevlisi: misafir aracı oluşturma ve geliş-çıkış işaretleme, onaylı araçların plaka listesi; kişi iletişim verisi yok.
- Plaka; log, metrik etiketi ve telemetriye yazılmaz.
- Saklama: KVKK m.4/2-(d) ve m.7 gereği veri amaç için gereken süre kadar tutulur. Kanunda genel bir "1 ay" süresi yoktur. Kişisel Verilerin Silinmesi, Yok Edilmesi veya Anonim Hale Getirilmesi Hakkında Yönetmelik periyodik imha aralığının 6 ayı aşmamasını ister. Varsayılan 30 gün; Faz07 terminal kayıtlarda plaka ve serbest metin alanlarını maskeler. Kişi/daire/audit ilişkileri kaldığından bu tam anonimleştirme değildir. Zamanlanmış işlem varsayılan kapalıdır; aşağıdaki Faz07 sınırları geçerlidir. Bu bilgi hukuki görüş değildir.

## Migration

`20261027092528_ParkingRequestIds`: `vehicles` ve `guest_vehicles` için NOT NULL `request_id` (mevcut satırlara sunucu UUID'si) ve `(property_id, request_id)` tekil index.

`20261027085437_ParkingFoundation`: yedi tablo, composite FK, partial unique index ve check kısıtları. Down, otopark verisi varsa (`vehicles`, `parking_allocations`, `guest_vehicles`, `parking_spaces`, `parking_policies`) reddeder; boş şemada tabloları siler.


## FAZ05 — Misafir/geçici park ve bakım operasyonu (27.10.2026)

Kullanıcı kararı: sakin yetkileri korunur; yeni sakin rezervasyonu, otomatik park hakkı, kamera/ANPR/QR veya bariyer eklenmez. Rezervasyon mevcut `guest_vehicles` kaydıdır. Park yeri seçilmemiş kayıt kapasite ayırmaz. Seçilen VISITOR yeri fiziksel kapasitenin tekil bir yeridir; aynı yerde çakışan iki aktif rezervasyon yapılamaz. Ayrı ziyaretçi sayısı kotası bu fazda eklenmez.

- Saat pencereleri `[startsAt, endsAt)`; eşit bitiş/başlangıç bitişiktir ve çakışmaz. Web tarih girişleri ve gün filtreleri İstanbul saatidir; backend offsetli timestamp'ı UTC'ye normalize eder. Yeni rezervasyonun bitişi geçmişte olamaz; misafir azami süresi mevcut policy'den gelir.
- Giriş yalnız PLANNED ve o an ayrılan aralığın içindeyken işaretlenir; çıkış yalnız ARRIVED durumundan, iptal yalnız PLANNED durumundan yapılır. Durum işlemi beklenen version ister; 409 sonrası kör retry yoktur. İptal geçmişi saklanır, ücret/ceza üretmez. Web manuel geliş/çıkış zamanlarını gösterir.
- Çıkışı kaydedilmemiş ARRIVED araç süresi geçse de aynı yer/plaka için yeni rezervasyon veya girişe engeldir. Kendiliğinden çıkış/doluluk tahmini üretilmez; görevli önce gerçek çıkışı kaydeder. Aynı plaka için çakışan aktif misafir kaydı başka yer veya yersiz olsa da reddedilir.
- Misafir UUID replay normalize plaka, daire, yer, başlangıç/bitiş, kırpılmış ad ve notun tümünü karşılaştırır. Farklı içerik `BF_PARKING_REQUEST_REUSED`409; aynı içerik güncel kaydı döndürür. Tahsis kotası aynı anda kullanılan yerlerin tepe sayısını denetler; birbirine değmeyen kayıtlar uzun aralıkta toplanmaz.

### Bakım kapatmaları

Yeni `parking_closures` tablosu BaseEntity8NOTNULL ve aynı Property/park yeri compositeFK taşır. Oluşturulan yer/saat/gerekçe değiştirilmez; iptal zamanı/gerekçesi/version saklanır. Kayıt silme ucu yoktur. UUID Property içinde tekildir.

| Uç | Rol/işlem |
|---|---|
| `GET /closures?page&pageSize` | Yönetici ve SECURITY; sayfalı, aktif üyelik ve Property scope |
| `POST /closures` | Yalnız yönetim; `{requestId,spaceId,startsAt,endsAt,reason}`; gerekçe1–500, bitiş başlangıçtan ve şimdiden sonra |
| `POST /closures/{id}/cancel` | Yalnız yönetim; `{version,reason}`; sona ermeden yalnız bir kez |
| `GET /guest-policy` | Yönetici/SECURITY; yalnız `{configured,guestMaxHours}` |
| `GET /guest-spaces?page` | Yönetici/SECURITY; yalnız VISITOR türü;100 kayıt/sayfa |

Tüm uçlar mevcut `/api/v1/properties/{propertyId}/parking` kökündedir. ACCOUNTANT403, üye olmayan/başka alan kaydı404, anonim401. Yazımlar mevcut Property kilidi ve version artışıyla tek transaction'dır. Kapatma↔misafir ve kapatma↔tahsis iki yönde çakışmayı reddeder; paralel işlemlerde yalnız biri kazanır. DateOnly tahsis iki uç dahil, kapatma kontrolünde İstanbul gece yarısına göre `[başlangıç,bitiş+1gün)` olur. Mevcut tahsis/rezervasyon otomatik iptal edilmez. Bakıma kapalı yerin geçmişi silinmez. Aktif/geçmiş kapatması olan park yeri silinemez.

API `ParkingSpaceResponse.isClosed` ek alanını döndürür; yalnız şu anda yürürlükte ve iptal edilmemiş kapatmadır. Harita bunu ikonlu Bakımda olarak gösterir. Tahsis yok demek ölçülmüş boşluk değildir; misafirlerin manuel İçeride durumu operasyon listesindedir. Güvenlik bakımı yalnız okur. İstemci scope/path değişince önceki plaka verisini temizler; geç cevap AbortController ile atılır.

`20261027104618_ParkingMaintenanceClosures`: yalnız yeni tablo/index/check/FK, Designer/snapshot. Önceki migration kaynakları korunur. Dolu geçmişte (iptal dahil) Down ilk DROP öncesi reddeder. Yalnız izole test veritabanında uygulandı; canlıya uygulanmadı. Ayrıntılı kanıt ve sınırlar [05-report.md](../parking/05-report.md).


## FAZ06 — Mobil otopark sözleşmesi

Mobil kök `/api/v1/mobile/parking`. Seçili bağlam query alanları `propertyId`, `role`, opsiyonel `unitId`, canonical modda `contextKind/contextId`, `page`1–10000. Page size20 sabittir. Query alanları istemcide seçili context'ten yeniden kurulur; URL/rol/daire değiştirilemez. Backend yetkiyi ayrıca doğrular.

- `GET /`: hesap/context/property/unit/role/asOf/page bağlı snapshot; kendi araçları, daireye açıkça verilmiş tahsisler veya yönetici için misafirler, daire ve VISITOR yer seçenekleri. Her liste aynı sayfa numarasıyla20 kayıt ve bağımsız total döndürür; sonraki sayfa tüm listelerin en büyük toplamına göre açılır. Mobil ad/özel misafir notu döndürmez. Daire seçenekleri varsa blok adını içerir. Tahsis listesi başka kişilerin plakalarını içermez; otomatik park hakkı üretmez.
- `POST /vehicles`: yalnız OWNER/TENANT/RESIDENT bağlamı; `ResidentVehicleRequest` gövdesindeki property/unit seçili bağlamla eşleşmeli. Kayıt PENDING oluşur. UUID replay plaka/tür/marka/model/renk/person/unit dahil normalize içeriği karşılaştırır. Aynı içerik aynı kaydı; farklı içerik409 döndürür.
- `POST /guests`: yalnız yönetici bağlamı. `GuestVehicleRequest`, mevcut süre/kota/çakışma/UUID kurallarını yeniden kullanır. Sakin rezervasyonu açılmaz.
- `POST /guests/{id}/arrive|depart|cancel`: yönetici, expected version, mevcut manuel saat/durum kuralları.409 sonrası kullanıcı yenilemeden işlem tekrar edilmez. Bu uçlar donanım geçiş kontrolü veya gerçek doluluk ölçümü değildir.

Legacy modda kullanıcı/aktif Property üyeliği/rol/bugün geçerli kişi–daire ilişkisi mevcut mobile session ile doğrulanır. Canonical modda `PersonRefactor:Enabled`, `SessionContextsEnabled`, `MobileAuthorizationEnabled` ve sunucuda exact kind/id/property/unit/role doğrulaması gerekir. Sakinde canonical Self → aynı tenant/property'de tek etkin `LegacyPersonMap` → güncel source version ve etkin legacy ilişki şarttır. Eksik/çoklu/eski alias403; isim/telefonla eşleme veya otomatik backfill yoktur. Canonical-only legacy eşlemesi olmayan kişiler bu aşamada otoparka alınmaz. Property üyeliği kontrolü kaldırılmaz. ACCOUNTANT403, anonim401; hatalı bağlam girdisi400. Mobil SECURITY bağlamı mevcut mobile-session sözleşmesinde yoktur; bu faz realm/rol oluşturmaz.

Yazımda canonical tenant kilidi → Property kilidi → güncel yetkiyi tekrar doğrulama uygulanır. Mevcut ortak iş kuralları kullanılır, ayrı rezervasyon modeli üretilmez. Yeni entity/migration yoktur.

Mobil yol `src/app/(tabs)/parking.tsx`, mevcut başlıktaki ikonlu kısayol; beş ana tab korunur. Canonical çalışma alanında bağımsız eski menü açılmadan otopark paneli açılır. Hesap/context değişiminde bileşen anahtarı, request epoch/lastContext, AbortController ve scoped-reader eski cevabı atar.

Cache/taslak mevcut SQLCipher + SecureStore hesap deposunun `owner_snapshots` slotlarında tutulur; token içermez. Mobil browser önizlemede kalıcı hassas depo kapalıdır. Tek context'in tek güncel sayfası saklanır; yönetici1saat, sakin24saat üst sınır, saat geri alma ve canonical yetki kopyası süresi ayrıca kontrol edilir.403/404 hesabın/context'in kopya ve taslağını temizler; yeniden doğrulanana kadar yerel erişimi kapatır. Fiziksel temizlik başarısızsa oturum ve şifreleme anahtarı kapatılır. Offline iken sunucudaki iptal anında öğrenilemez; süreli kopya sınırı açıkça gösterilir.

Taslak kayıt oluşturmaz ve outbox'a alınmaz. Geçerli UUID ile elle kaydedilir; native online gönderimden önce de aynı taslak saklanır. Bağlantı gelince otomatik gönderilmez; kullanıcı ayrıca sunucu onayı ister. Cevap kaybında aynı içerik/UUID ile yeniden gönderilir. İçerik değişirse yeni UUID gerekir. Başarılı sunucu sonucu doğrulanmadan tamamlandı gösterilmez; sunucu onayından sonra taslak temizliği başarısızsa durum ayrıca bildirilir. Backend her gönderimde güncel yetki/kota/müsaitliği yeniden denetler.

Doğrulama ve gerçek cihaz sınırları [06-report.md](../parking/06-report.md). Native SQLite/UUID/router sürüm referansları: [Expo57 SQLite](https://docs.expo.dev/versions/v57.0.0/sdk/sqlite/), [Crypto](https://docs.expo.dev/versions/v57.0.0/sdk/crypto/), [Router](https://docs.expo.dev/versions/v57.0.0/sdk/router/).


## OTOPARK-07 — Uygulama içi bildirim, rapor ve saklama

`GET /api/v1/properties/{propertyId}/parking/report?from=YYYY-MM-DD&through=YYYY-MM-DD` yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER ve mevcut Property kapsamına açıktır. SECURITY/ACCOUNTANT/sakin403, başka alan404. Başlangıç2000 veya sonrası, bitiş9998 veya öncesi, iki uç dahil en fazla366 gün. RepeatableRead altında günler Europe/Istanbul (tarihsel yaz/kış saati dahil) ile gruplanır. Günlük giriş/çıkış, manuel ArrivedAt/DepartedAt; rezervasyon adedi iptal edilmemiş kaydın StartsAt günüdür. Terminal maskelenmiş kayıtlar toplama dahildir. Plaka/kişi/daire kimliği veya serbest metin rapora verilmez.

Kapasite, bugün iki uç dahil tahsisli farklı yerler, şu an PLANNED aralığındaki farklı misafir yerleri, ARRIVED kayıtlar ve süresi geçmiş ARRIVED kayıtlar **güncel** özettir; seçilen günlerin tarihsel doluluk bilançosu değildir. Sonraki tarihli tahsis bugün sayılmaz. Sensör yoktur; fiziksel doluluk yüzdesi üretilmez. AsOf sorgu zamanını taşır. Web CSV yalnız doğrulanmış ISO gün ve pozitif/sıfır integer kolonlarıyla bütün seçili günleri (ekran sayfasından bağımsız) indirir; Excel uyumlu CSV'dir, XLSX üretimi değildir.

Araç PENDING/APPROVED/REJECTED/PASSIVE ve misafir PLANNED/ARRIVED/DEPARTED/CANCELLED olayları mevcut PersonMessage kutusuna park yazımıyla **aynı transaction** içinde eklenir. Araçta ilgili kişi; misafirde ev sahibi dairenin güncel OWNER/TENANT/RESIDENT ilişkisi, etkin kişi/daire ve etkin Property üyeliği bulunan bağlı hesaplar alıcıdır; aynı hesap tek mesaj alır. Hesap/eşleme oluşturulmaz. Mesaj başlık/gövdesi sabit TR/EN metindir, plaka/ad/not/kimlik taşımaz. İşlem+olay için mevcut ProcessedMessage consumer/message UUID tekilliği kullanılır; alıcı yoksa SKIPPED kaydı oluşur. Sonradan üyelik verilmesi geçmiş olayı yeniden göndermez. UUID replay ve başarısız yazım ilave mesaj üretmez. Bu doğrudan uygulama içi mesaj kaydıdır; broker/push/SMS/e-posta teslimi iddia edilmez. Mevcut web kutusu ve legacy mobil mesaj akışı kullanılır; canonical native notices kapısı genişletilmedi.

`Parking:Operations:Enabled=true` yalnız yapılandırmayla etkinleşir; varsayılan kapalıdır. API raporundaki ScheduledOperationsEnabled yapılandırmayı gösterir, worker sağlık/teslim kanıtı değildir. Aynı ayarın worker ortamında da açık olması ve worker'ın çalışması gerekir. Worker dakika aralığıyla Property UUID cursor taraması yapar. Her çağrı bir alan ve her tür için en fazla100 kayıt işler; kilit alındıktan sonra etkin alan/politika yeniden kontrol edilir. Bir saat içinde bitecek PLANNED/ARRIVED için bir yaklaşma, bitmiş ARRIVED için bir süre aşımı mesajı kaydedilir. Tekrar tarama çoğaltmaz; başarısız transaction sonraki turda tekrar denenir. Kaçırılmış geçmiş yaklaşma bildirimi geriye dönük üretilmez. Yönetici raporunda güncel süre aşımı sayılır; doğrulanmamış fiziksel ihlal/fiskal ceza üretilmez.

Aynı opt-in işlem, politikanın RetentionDays süresi dolan PASSIVE/REJECTED araçlar ile DEPARTED/CANCELLED misafirlerin plakasını REDACTED yapar; marka/model/renk, misafir adı/notu temizlenir; reddetme gerekçesi nötr REDACTED olur. Kesim zamanı terminal olay zamanıdır (<=now-retentionDays); aktif kayıtlar, ARRIVED geç çıkışlar ve henüz kapanmamış geçmiş PLANNED kayıtlar otomatik kapatılmaz/maskelenmez. PersonalDataRedactedAt, version ve değişiklik audit'i kaydedilir; oluşturma audit'i/UUID/kişi/daire/tarihler/ilişkiler korunur. Bu veri azaltmadır, anonimleştirme veya tüm KVKK imha yükümlülüğünün yerine getirildiği iddiası değildir. Yedekler ve önceden indirilmiş/offline kopyalar uzaktan silinmez; mevcut mobil cache üst süreleri geçerlidir. Maskelenen kayıtlar web/mobil kişi listelerinden çıkar, tarihsel tahsiste plaka null olur; eski UUID ile yeniden oluşturma409'dur.

Kamera, ANPR, QR ve bariyer için adapter/SDK/endpoint/credential/otomatik açma eklenmedi; HardwareEnabled sabitfalse. Ayrı teknik ve KVKK etki değerlendirmesi olmadan etkinleştirme yolu yoktur. Veri minimizasyonu, amaç/hukuki dayanak, bilgilendirme, erişim/saklama, sağlayıcı ve aktarım, fiziksel güvenlik/yanlış okuma/fail-safe konuları [Faz07 raporunda](../parking/07-report.md) kayıtlıdır.


## OTOPARK-08 yetki ve erişilebilirlik düzeltmesi

Legacy Araçlarım üç ucunda token rolü OWNER/TENANT/RESIDENT veya SUPER_ADMIN/SITE_ADMIN/MANAGER olmalıdır. ACCOUNTANT/SECURITY tek başına, bağlı kişi ve güncel ilişki olsa da403 alır. İlişki/alan kapsamı ayrıca korunur; mobil context yetkisi genişletilmez. Negatif test eski GET200 davranışını yeniden üretti; rol kontrolü tek Resident yardımcı metodunda düzeltildi. Çoklu role sahip hesap uygun sakin/yönetim rolüyle yalnız kendi ilişkisi kapsamında ilerler. Sekmeler tek Tab durağı ve sol/sağ/Home/End ile seçim+odak taşır.
