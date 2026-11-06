# Otomasyon ve Çok Kanallı Bildirim Merkezi (BF-AUTO P0)

Kaynak: kullanıcı master promptu ve "kodlamaya başlayabilirsin" onayı (06.11.2026). Bu belge P0 kapsamını (FAZ 00 analiz özeti, FAZ 01 çekirdek, FAZ 02 aidat kuralları, ödeme alındı bildirimi, FAZ 04'ün web ekranları) ve sonraki fazları anlatır.

## FAZ 00 — Mevcut altyapı ve yeniden kullanım

| İhtiyaç | Mevcut bileşen | Karar |
| --- | --- | --- |
| Arka plan çalışma | `BuildingFlow.Worker` (BackgroundService + PeriodicTimer) | Yeni `AutomationWorker` (1 dk tik). |
| Kalıcı iş/zamanlama | PostgreSQL | Zamanlamanın tek kaynağı `automation_definitions` + `automation_executions`. |
| Asenkron dağıtım/retry/DLQ | `outbox_messages` + `OutboxRelay` + RabbitMQ consumer (inbox, retry, dead-letter) | E-posta `EmailRequested` olarak outbox'a yazılır; yeni kuyruk eklenmedi. |
| SMS | `ISmsQueue` (BF-SMS izin/RET/footer politikası, kontör, segment) + `SmsDispatcher` (MOCK sağlayıcı) | Üç yeni SMS türü `DUES_REMINDER`, `DUES_OVERDUE`, `PAYMENT_RECEIVED` (`DUES_NOTIFICATION`, OPERATIONAL tercih kuralı). Gerçek sağlayıcı yok. |
| Uygulama içi bildirim | `person_messages` (Person + bağlı hesap, `/api/v1/notifications`, mobil `MESSAGE` notice) | Otomasyon mesajları buraya yazılır; yeni tablo yok. |
| Mobil | Notice ekranı açılışta, uygulamaya dönüşte ve aktifken 30 sn'de sunucudan çeker; `PushAvailable=false`; expo-notifications kurulu değil | **Mobil push P0'da yok.** Bkz. aşağıdaki karar. |
| Alıcı | `DuesChargeParty` borçlu snapshot'ı (06.1), yoksa bölümün güncel birincil iletişim kişisi | Kişi hâlâ bölümde güncel ilişkiye sahip olmalı. |
| Borç/ödeme | `DuesCharge.Amount-PaidAmount`, `Payment`/`PaymentAllocation`, Property `FOR UPDATE` kilidi | Hatırlatma aynı kilidi alır; ödeme ile yarışmaz. |

### Mobil bildirim kararı: çekme (pull) modeli

Kullanıcının önerdiği yaklaşım (mobil uygulama, kullanıcı girdiğinde veya uygulama aktifken kendi bildirimlerini sunucudan alır) mevcut kodla doğrudan uyumludur ve P0 için seçildi:

- `mobile/.../src/notices/useNotices.ts` ve `unread.ts` zaten açılışta, `AppState=active` olunca ve aktifken periyodik olarak `MESSAGE` türünü çeker; yetki `MobileNoticeStore` içinde oturum bağlamı + üyelik ile backend'de doğrulanır, yanlış daire/kullanıcıya gösterim riski backend'dedir.
- Push için cihaz token tablosu, FCM/APNs hesabı, yeni paket (expo-notifications; "yeni lisans yükü yok" kararı gereği onay ister) ve işletim sistemi izni gerekir; teslim garanti değildir. Bu nedenle FCM/APNs **sonraki faz** (FAZ 03 push) olarak kaldı; ekranda "push sonraki faz" açıkça yazılır, gönderildi gibi gösterilmez.
- Sınırlama: uygulama hiç açılmazsa kullanıcı telefonda anlık uyarı görmez; o durumda SMS/e-posta kanalları kural bazında açılabilir.

## Veri modeli (migration `20261106132519_AutomationNotificationCenter`)

Hepsi `BaseEntity` + `BaseEntityConfiguration`; Down, geçmiş varsa reddeder.

- `automation_definitions`: Property + tür başına tek kural (`uidx_..._property_id_type`), `ACTIVE/PAUSED`, başlangıç/bitiş, gönderim saati, sessiz saat (Europe/Istanbul), vade gün listesi (`integer[]`), kanal bayrakları, başlık/gövde şablonu, `rule_version`, `activated_at`. Yeni kural PAUSED başlar.
- `automation_revisions`: her oluştur/güncelle/devreye al/duraklat için değişmez gerekçeli snapshot.
- `automation_executions`: tetik (`SCHEDULE/EVENT/MANUAL`), iş günü, kural sürümü, correlation id, sayılar, deneme, `next_attempt_at`; `SCHEDULE` için kural+gün tekil.
- `notification_dispatches`: kanal başına tek satır, `dedup_key` (Property|tür|borç/ödeme|gün|kanal|kişi SHA-256) tekil; durum `DELIVERED/QUEUED/SKIPPED/FAILED/CANCELLED` + neden kodu; alıcı yalnız maskeli; bağlı `person_message_id`, `sms_message_id`, `outbox_message_id`.

## Çalışma kuralları

1. Worker her dakika aktif kuralları okur. Zamanlanmış kural yalnız yerel saat ≥ gönderim saati ve sessiz saat dışındayken, o gün başarılı/dead-letter çalışması yoksa çalışır.
2. Her çalışma tek transaction: Property satırı `FOR UPDATE` (ödeme ile aynı kilit) → kural yeniden okunur (hâlâ aktif mi) → güncel bakiye `Amount > PaidAmount`, iptal yok, aktif bölüm → alıcı ve kanal → dedup → PersonMessage/SMS/outbox satırları ve dispatch satırları birlikte commit.
3. Gün eşleşmesi: `vade = bugün - gün`. Kaçırılan gün sonradan gönderilmez (kesinti sonrası toplu gönderim yok). Ödeme bildirimi son 72 saat ve kuralın devreye alındığı andan sonraki tahsilatları değerlendirir.
4. Kısmi ödemede mesaj güncel kalanı yazar; tam ödenen borç aday olmaz.
5. Kuyrukta bekleyen SMS: `ISmsSendGuard` (`AutomationSmsGuard`) sağlayıcı çağrısından hemen önce borç ödendi/iptal/kural duraklatıldı ise SMS'i `CHARGE_PAID`/`CHARGE_CANCELLED`/`AUTOMATION_PAUSED` ile reddeder, dispatch `CANCELLED` olur, kontör düşmez. Sağlayıcının kabul ettiği mesaj geri alınamaz; bu pencere kalır.
6. Hata: çalışma geri alınır, `automation_executions` FAILED + üstel bekleme; 5 denemede `DEAD_LETTER`, yönetici "Şimdi çalıştır" ile yeniden başlatır. E-posta teslim retry/DLQ mevcut RabbitMQ consumer'ındadır.
7. Sessiz saatte olay kuralı yalnız uygulama içi kanalı üretir; SMS/e-posta sonraki uygun tikte gelir. Sessiz saatte SMS/e-posta içeren zamanlanmış kural elle çalıştırılamaz (`BF_AUTOMATION_QUIET_HOURS`).
8. E-posta yalnız SMTP etkinse outbox'a yazılır; değilse `CHANNEL_UNAVAILABLE` ile atlanır (kuyrukta birikip sonradan topluca gitmez).
9. İki veya daha fazla Worker: Property kilidi + yürütme tekil indeksi + dedup indeksi tek gönderimi garanti eder (`ConcurrentSchedulersProduceOneReminderAndOneExecution`).

## API (`/api/v1/properties/{propertyId}/automations`, SUPER_ADMIN/SITE_ADMIN/MANAGER + aktif üyelik)

ACCOUNTANT'a açılmaz (alıcı/kişi verisi). Başka Property'nin kuralı 404.

| Yöntem | Yol | Açıklama |
| --- | --- | --- |
| GET | `` | Genel bakış: tür kataloğu, kurallar (sonraki çalışma, son sonuç), bugünkü kanal sayıları, sağlık. |
| POST | `` | Kural oluştur (PAUSED, gerekçe zorunlu). Tür başına bir kural; ikinci 409 `BF_AUTOMATION_CONFLICT`. |
| PUT | `/{id}` | Güncelle (version + gerekçe; `rule_version` artar). |
| POST | `/{id}/activate`, `/{id}/pause` | Durum (version + gerekçe). |
| POST | `/{id}/run` | Elle çalıştır (aktif kural; dedup sayesinde tekrar güvenli). |
| GET | `/{id}/preview?date=` | Gerçek veriyle önizleme; yazma/gönderim yok. |
| GET | `/{id}/revisions` | Sürüm geçmişi. |
| GET | `/executions`, `/dispatches` | Sayfalı geçmiş; dispatch filtreleri `automationId`, `channel`, `status`. Kişi adı dönmez, alıcı maskeli. |

Şablon değişkenleri yalnız türün katalogdaki listesinden (`propertyName, unitNumber, periodLabel, chargeLabel, amount, balance, dueDate, daysLeft, dueText, daysOverdue, paidAmount, paidDate, unitBalance`); kişi adı/iletişim değişkeni yoktur. SMS metni SMS ekranındaki tür şablonundan gelir (segment kontrolü orada).

Hata kodları: `BF_AUTOMATION_INVALID` (400), `BF_AUTOMATION_CONFLICT` (409), `BF_AUTOMATION_QUIET_HOURS` (409).

## Web

`/automations` alan seçimi → `/properties/{id}/automations`: özet kartlar, ikonlu sekmeler (Otomasyonlar, Gönderim geçmişi, Çalışmalar, Sistem sağlığı); kural paneli (gün listesi, saatler, geçerlilik, kanallar, değişken ekleme, örnek veriyle canlı önizleme, SMS segment notu, gerekçe), devreye al/duraklat, şimdi çalıştır, gerçek veri önizleme, sürüm geçmişi.

## Otomasyon Türleri (TÜR 01–05, 06.11.2026)

Kullanıcı kararı: yeni tür kod değişikliği olmadan eklenir; SUPER_ADMIN ve SITE_ADMIN tanımlar, platform türü (tüm yönetim alanları) yalnız SUPER_ADMIN. Analiz: [Claude Doc](https://claude.ai/artifact/YEQq2Xk5vHok2rsrAm8N7C).

- Tür yalnız koddaki yapı taşlarından kurulur (`AutomationSources`): kaynak `REQUEST`, `SUPPLIER_CONTRACT`, `UNIT`, `MAINTENANCE_PLAN`, `METER`, `RESERVATION`; her kaynağın izinli alan/değer listesi, operatörler `EQ/IN/NOT_IN`, tarih alanları, tetikler (`DATE_OFFSET`, `EVENT`, `CALENDAR`), alıcı grupları (`MANAGERS`, `OWNERS`, `TENANTS`, `RESIDENTS`, `PRIMARY_CONTACT`, `REQUESTER`) ve değişkenler. Serbest sorgu/ifade yoktur; sorgular `AutomationSourceReader` içinde Property filtresiyle yazılıdır, koşullar bellekte izinli alanlara uygulanır.
- Değişkenlerde kişi adı, telefon, kimlik ve talep metni yoktur (talep için yalnız kategori/öncelik/durum).
- Tablolar: `automation_types` (taslak/yayında/arşiv, `type_version`, jsonb koşullar), `automation_type_revisions`; `automation_definitions.custom_type_id/custom_trigger` (tür başına tek kural); `notification_dispatches.unit_id` artık nullable + `source_code/source_id`; `person_messages.person_id` nullable (Person satırı olmayan yönetici hesapları). Migration `20261106141717_AutomationCustomTypes`, Down geçmiş varsa reddeder.
- Yayındaki türün kaynağı ve tetik türü değişmez; arşivlenen türle yeni kural eklenemez ve mevcut kurallar çalışmaz. Taslak tür kurala bağlanamaz (409).
- Yönetici alıcıları: aktif `ManagementAssignment` (SITE_ADMIN/MANAGER) → bağlı hesap (`CentralPersonAccountLink` → `PersonUserAccount.Subject`) → aktif Property üyeliği. Bildirim web bildirim kutusunda (`/api/v1/notifications`) ve mobil yönetici bağlamındaki MESSAGE listesinde görünür.
- SMS: özel türler `AUTOMATION_NOTICE` (OPERATIONAL, `MAINTENANCE_NOTIFICATION` kategorisi; alıcının operasyonel SMS tercihine uyar).
- API `/api/v1/properties/{id}/automation-types`: `GET /sources`, `GET`, `POST`, `PUT /{typeId}`, `POST /{typeId}/publish|archive`, `GET /{typeId}/preview`, `GET /{typeId}/revisions`. Okuma üç yönetici rolü; yazma SUPER_ADMIN/SITE_ADMIN.
- Web: üst menüde **Otomasyon** grubu (Otomasyonlar, Otomasyon türleri, Gönderim geçmişi, Sistem sağlığı); `/properties/{id}/automations/types|history|executions|health`. Tür paneli 5 adım: kaynak+ikon, koşullar, tetik, alıcılar, varsayılan mesaj + önizleme.

## Yerel demo verisi

`bash scripts/dev.sh automation-demo [--property <id>]` yalnız localhost veritabanında çalışır ve tekrar çalıştırıldığında çoğaltmaz (`AUTO-DEMO` etiketi). Yaptıkları:

- MOCK SMS hesabı + 1000 demo kontör (yoksa); gerçek SMS gönderilmez, SMS ekranında "gönderildi (MOCK)" görünür.
- Uygulama hesabına bağlı kişilerin bölümlerine üç ek tahakkuk: vadeye 2 gün, bugün son gün, 7 gün gecikmiş; 2 gün kalana 250 TL kısmi tahsilat.
- Asansör arıza talebi, 2 gün sonraya bakım planı, son okuması 30 gün önce sayaç, yarın için ortak alan rezervasyonu.
- Aktif yönetici ataması yoksa üyeliği olup Person satırı olmayan ilk hesaba demo yönetici ataması.
- Altı kaynak için yayında özel tür ve dokuz aktif kural (uygulama içi + SMS, vade hatırlatmada e-posta; sessiz saat yok), ardından otomasyonu, MOCK SMS göndericiyi ve outbox → RabbitMQ yayınını bir kez çalıştırır.
- `Automations:PublishEvents=true` (dev.sh varsayılanı) ile her uygulama içi bildirim `notification.requested.v1` kuyruğuna da yazılır; bu kuyruğun tüketicisi yoktur (push sonraki faz), RabbitMQ arayüzünde birikir. Üretimde varsayılan kapalıdır.
- E-posta için SMTP tanımlı değilse gönderim satırı `CHANNEL_UNAVAILABLE` ile atlanır.

## Sonraki fazlar (kodlanmadı)

- FAZ 03 push: cihaz kaydı, FCM/APNs, izin, deep link borç detayı (yeni paket onayı gerekir); gerçek SMS sağlayıcısı / Aka Integration Hub adaptörü; e-posta HTML şablonu.
- FAZ 05 mobil: bildirim tercihleri ekranı, bildirimden borç detayına yönlendirme (person_messages'a kaynak bağlantısı), SQLite çevrimdışı geçmiş.
- FAZ 06: aylık tahakkuk (önizleme + onay), yönetici özeti, SMS kontör uyarısı, talep SLA, bakım, tedarikçi sözleşme, sayaç okuma, otomasyon hata alarmı.
- Gecikme tazminatı otomasyonu mevcut BF-LATE kurallarına bağlı kalır; yeni oran/uygulama eklenmedi.
