# SMS (İŞ13)

Yönetim alanı (Property) başına bir SMS hesabı, kontör defteri, tipli şablonlar ve maskeli mesaj geçmişi. Model ve ekran akışı TraceFlow `masterdata/sms` modülünden uyarlanmıştır; kod .NET/Next.js için yeniden yazıldı. Gerçek sağlayıcı yoktur: **MOCK** sağlayıcı mesajı yalnız kaydeder, `SENT` işaretler ve kontör düşer; hiçbir telefona gönderim yapılmaz.

## Yetki

Tüm uçlar `/api/v1/properties/{propertyId}/sms` altında kişi yönetimi politikasına (SUPER_ADMIN, SITE_ADMIN, MANAGER) ve aktif Property üyeliğine bağlıdır; ACCOUNTANT/RESIDENT 403, kapsam dışı Property 404. Hesap tanımlama ve kontör işlemleri yalnız SUPER_ADMIN.

| Uç | Açıklama |
| --- | --- |
| GET `` | `{propertyId, account|null, types}` |
| PUT `/account` | `{name, provider:"MOCK", senderHeader(3–11), username?, secret?, apiUrl?(https), lowCreditThreshold, isEnabled, version}`; ilk kayıt version 0, sonra güncel version (aksi 409). `secret` yalnız yazılır, AES-GCM ile saklanır, yanıtta `hasSecret`. |
| POST `/account/credits` | `{type: LOAD|ADJUST, quantity, unitPrice?, currency?, referenceNo?, description?}`; bakiye 0–10.000.000, eksiye inen düzeltme `BF_SMS_CREDIT_INSUFFICIENT` (409). Hesap yoksa `BF_SMS_ACCOUNT_REQUIRED`. |
| GET `/account/credits?page&pageSize` | Kontör hareketleri (LOAD/CONSUME/REFUND/ADJUST), yeniden eskiye. |
| GET `/templates` | Her SMS tipi için Property şablonu, yoksa sistem varsayılanı (`isSystemDefault`, `id:null`). |
| POST `/templates`, PUT `/templates/{id}`, DELETE `/templates/{id}?version` | Tip başına tek şablon (`BF_SMS_TEMPLATE_CONFLICT`); yalnız tipin parametreleri (`BF_SMS_TEMPLATE_PARAMETER_INVALID`); ≤1000 karakter. Kapalı şablon o tipte gönderimi durdurur (`SKIPPED`). Silme varsayılana döndürür. |
| GET `/messages?page&pageSize&status` | Maskeli telefon (`+90********67`) ve maskeli metin; kişi kodu `******`. |
| POST `/messages` | Elle gönderim `{requestId, personId?|phoneNumber?, body}`; aynı `requestId` aynı kaydı döner. Hesap/kontör yoksa 409, geçersiz telefon `BF_SMS_PHONE_INVALID`. |

## Tipler

| Kod | Amaç | Parametreler | Varsayılan |
| --- | --- | --- | --- |
| `PERSON_REGISTERED` | TRANSACTIONAL | `code` (gizli), `fullName`, `propertyName` | `Sayın {{fullName}}, {{code}} kod ile kişi kaydınız kayıt altına alınmıştır. {{propertyName}}` |
| `MANUAL` | OPERATIONAL | `message` | `{{message}}` |

Segment hesabı GSM-7 (160/153) ve UCS-2 (70/67); Türkçe ı/ş/ğ UCS-2'ye geçirir. En çok 10 segment; maliyet = segment.

## Akış ve güvenlik

1. İş kaydı (kişi oluşturma, kod yenileme `sendSms:true`, elle gönderim) aynı DB transaction'ında `sms_messages` satırı ekler. Telefon E.164'e normalize edilir (0 5xx…, 5xx…, 0090…). Hesap yok/pasifse `FAILED NO_ACCOUNT`, kontör yetersizse `FAILED INSUFFICIENT_CREDIT` kaydı kalır; telefon geçersizse veya şablon kapalıysa satır yazılmaz ve yanıtta `registrationSms` sonucu döner.
2. Gerçek telefon ve düz metin yalnız `protected_payload` kolonunda AES-GCM ile (anahtar `PersonCode:HmacKey`'den HKDF ile türetilir) tutulur; mesaj `SENT`/`FAILED` olunca silinir. Geçmişte yalnız maskeli değerler kalır.
3. Worker `SmsDispatchWorker` 2 sn'de bir kuyruğu `FOR UPDATE SKIP LOCKED` ile işler; her mesaj ayrı transaction'da, hesap satırı kilitliyken bakiye kontrolü → sağlayıcı → başarıda `CONSUME` hareketi + `SENT`. Hata: 16 sn'den başlayan geri çekilme, en çok 5 deneme; kalıcı hatada hemen `FAILED`. Başarısız gönderim kontör düşmez.

Not: Faz09 RabbitMQ outbox'ı kayıtlı payload'ı yayından sonra da sakladığı için kişi kodu ve telefon outbox'a yazılmaz; SMS kendi şifreli tablo kuyruğunu kullanır. Teslim en-az-bir-kezdir: sağlayıcı çağrısı ile commit arasında çökme olursa mesaj tekrar gönderilebilir (gerçek sağlayıcıda sağlayıcı tarafı tekilleştirme gerekir).

Yerel: `bash scripts/dev.sh worker` dispatcher'ı da çalıştırır; worker'a `PersonCode__HmacKey` verilmelidir (compose ve dev.sh ayarlı).

## BF-SMS 02 — SMS kategori politikası

Her `SmsTypes` türü bir `SmsCategory` taşır; politika `ISmsPolicyCatalog` (`Application/SmsPolicies.cs`) arkasındadır, ileride Aka Integration Hub'a taşınabilir. Overview yanıtındaki tür satırına `category` alanı eklendi.

| Kategori | Purpose | Hukuki dayanak | Kullanıcı tercihi | Ticari onay | RET/alt bilgi | Tanıtım içeriği |
| --- | --- | --- | --- | --- | --- | --- |
| `SECURITY_OTP` | TRANSACTIONAL | SECURITY | kapatılamaz | hayır | yok | yasak |
| `PASSWORD_RESET` | TRANSACTIONAL | SECURITY | kapatılamaz | hayır | yok | yasak |
| `ACCOUNT_VERIFICATION` | TRANSACTIONAL | SECURITY | kapatılamaz | hayır | yok | yasak |
| `DUES_NOTIFICATION` | OPERATIONAL | SERVICE | operasyonel tercih | hayır | yok | yasak |
| `MAINTENANCE_NOTIFICATION` | OPERATIONAL | SERVICE | operasyonel tercih | hayır | yok | yasak |
| `RESERVATION_NOTIFICATION` | OPERATIONAL | SERVICE | operasyonel tercih | hayır | yok | yasak |
| `EMERGENCY_NOTIFICATION` | OPERATIONAL | VITAL_INTEREST | kapatılamaz | hayır | yok | yasak |
| `MARKETING` | MARKETING | COMMERCIAL_CONSENT (6563 m.6) | ticari onay | evet | zorunlu (MERSİS/unvan/ücretsiz RET) | serbest |

Mevcut eşleme: `PERSON_REGISTERED` → `ACCOUNT_VERIFICATION`, `MANUAL` → `MAINTENANCE_NOTIFICATION`. Platform OTP hattı (`NetgsmPhoneOtpSender`) `SECURITY_OTP` kapsamındadır ve alt bilgi almaz. Ticari onay ve alt bilgi motoru BF-SMS 03/04'te gelene kadar `RequiresCommercialConsent` olan bir tür kuyruğa alınmaz (`SKIPPED COMMERCIAL_CONSENT_REQUIRED`, satır yazılmaz). Operasyonel tercih ve tanıtım içeriği kontrolü henüz uygulanmaz; politika alanı tanımlıdır. Şema değişmedi (`purpose` string kolon).

## BF-SMS 03 — Ticari ileti onay/ret veri modeli

Migration `20261026142437_SmsCommercialConsents` iki BaseEntity tablosu ekler; ticari ileti onayı KVKK `user_consents` kaydından ayrıdır (6563 m.6 ≠ KVKK açık rıza).

- `sms_legal_senders`: ticari iletinin hukuki hizmet sağlayıcısı. `property_id` NULL ise platform, dolu ise site/yönetim (tenant bazlı; her kapsamda tek aktif kayıt). Unvan, MERSİS (16 hane, opsiyonel; tacir değilse boş), iletişim bilgisi, `RET` ile başlayan ret kelimesi, rakamlı ret kısa numarası, `is_opt_out_enabled`, `is_iys_integration_enabled` + `iys_brand_code` (İYS açıksa zorunlu).
- `commercial_consent_records`: append-only onay/ret olayları. Gönderici, kanal (SMS), E.164 alıcı, doğrulanmış telefon bayrağı, `APPROVED/REJECTED`, kaynak (`WEB/MOBILE/ADMIN/SMS_REPLY/PROVIDER_CALLBACK/IYS`), `occurred_at`, opsiyonel `owner_user_id`/`person_id`, metin sürümü + SHA-256, `operation_id` (tekil) ve kanıt SHA-256, İYS durumu (`NOT_REQUIRED/PENDING/SYNCED/FAILED`) + referans/zaman/hata kodu.
- Kurallar: onay SMS cevabı/sağlayıcı geri bildirimiyle alınamaz; onay doğrulanmış telefon ve metin kanıtı ister (domain + `chk_commercial_consent_records_approval`). Güncel durum gönderici/kanal/alıcı için en yeni kayıttır; ret kaydı anında geçerli olur. Karar kolonları ve `is_deleted` trigger ile değiştirilemez, satır silinemez; yalnız İYS alanları ilerler. Dolu tabloda Down engellenir.
- `ICommercialConsentStore` (yalnız sunucu içi; yetki çağıran katmanda): `RecordAsync` gönderici satırını `FOR UPDATE` kilitler, aynı `operationId` + aynı içerik replay, farklı içerik `BF_CONSENT_OPERATION_CONFLICT`; `CurrentAsync`, `HistoryAsync` (sayfalı, maskeli telefon).
- Henüz yok: HTTP uçları ve ekranlar (FAZ 05), gönderim kapısında onay kontrolü ve alt bilgi (FAZ 04), RET/İYS entegrasyonu (FAZ 06). Onay metni kataloğu FAZ 05'te; şimdilik kayıt metin sürümü ve hash'ini taşır.
