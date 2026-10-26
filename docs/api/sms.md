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

## BF-SMS 04 — Politika çözümleyici ve yasal alt bilgi

`ISmsPolicyResolver` (`Infrastructure/SmsPolicyResolver.cs`) her SMS kuyruğa girmeden `SmsQueue.EnqueueAsync` içinde çağrılır; Hub'a taşınırken yalnız bu uygulama değişir.

- Ticari olmayan kategoriler (OTP, şifre sıfırlama, hesap doğrulama, aidat, bakım, rezervasyon, acil) veritabanına bakmadan aynen geçer; alt bilgi eklenmez. Platform OTP hattı (`NetgsmPhoneOtpSender`) kuyruğu hiç kullanmaz.
- Ticari (MARKETING) SMS sırası: Property'nin etkin göndericisi, yoksa platform göndericisi → yoksa `LEGAL_SENDER_MISSING`; ret kapalı/pasif gönderici `OPT_OUT_UNAVAILABLE`; alıcının bu göndericide kaydı yoksa `COMMERCIAL_CONSENT_REQUIRED`; en son karar ret ise `OPTED_OUT` (ret kaydı yazıldığı anda sonraki gönderim durur); İYS açık ve onay `SYNCED` değilse `IYS_NOT_SYNCED`. Engellenen SMS için satır yazılmaz, `SKIPPED` + kod döner.
- Alt bilgi: `\n{unvan}\nSMS almak istemiyorsanız {RET kelimesi} yazıp {kısa numara} numarasına ücretsiz gönderiniz.` + `\nMERSIS: {numara}` (tacir değilse `\n{iletişim bilgisi}`). Alt bilgi gövdeye ve maskeli geçmişe eklenir; segment/kontör alt bilgi dahil hesaplanır, 10 segmenti aşan ileti `TOO_LONG`.
- `SmsText.Measure` kodlama (`GSM7`/`UCS2`), karakter ve segment döndürür; Türkçe ı/ş/ğ/İ UCS-2'ye geçirir (70/67). Kontör maliyeti = segment.
- Henüz yok: MARKETING tipi `SmsTypes.All` içinde yayınlanmadı (web tip doğrulaması FAZ 05'te genişletilecek); operasyonel tercih (`SmsPreferenceRule.OPERATIONAL`) ve operasyonel şablonda tanıtım içeriği denetimi uygulanmıyor.

## BF-SMS 05 — İletişim tercihleri ekranları ve API

| Uç | Rol | Açıklama |
| --- | --- | --- |
| `GET/PUT /api/v1/properties/{id}/sms/legal-sender` | okuma SUPER_ADMIN/SITE_ADMIN/MANAGER + üyelik; yazma SUPER_ADMIN/SITE_ADMIN | Property göndericisi; `{sender:null}` tanımsız; `version` ile 409 |
| `GET/PUT /api/v1/sms/platform-sender` | SUPER_ADMIN | Platform göndericisi |
| `POST /api/v1/properties/{id}/sms/preview` | yönetici | `typeCode` (`MANUAL`, `PERSON_REGISTERED`, `MARKETING`), metin, opsiyonel telefon → son metin, alt bilgi, `GSM7/UCS2`, karakter, segment, `allowed/blockedCode` |
| `GET /api/v1/properties/{id}/sms/commercial-consents` | yönetici | Etkin göndericinin onay/ret kayıtları, sayfalı, maskeli telefon |
| `POST /api/v1/properties/{id}/sms/opt-outs` | yönetici | `operationId` + telefon → `REJECTED/ADMIN`, anında geçerli |
| `GET /api/v1/account/communication-preferences` | kimlikli | Maskeli doğrulanmış telefon, güncel onay metni (sürüm+SHA-256), platform + aktif üyelikli Property göndericileri |
| `PUT /api/v1/account/communication-preferences/{senderId}` | kimlikli | `{operationId, approved, textVersion, textSha256, channel WEB}`; onay doğrulanmış telefon (`BF_SMS_PHONE_NOT_VERIFIED`) ve güncel metin (`BF_CONSENT_TEXT_OUTDATED`) ister |
| `GET .../{senderId}/history` | kimlikli | Kendi geçmişi |
| `GET /api/v1/mobile/communication-preferences`, `POST .../{senderId}` | kimlikli | Mobil eşdeğeri; kanal sunucuda `MOBILE` |

- Onay metni `CommercialConsentTexts` (sürüm 1) kodda sürümlenir; istemci metnin SHA-256'sını doğrulamadan onay düğmesi açılmaz. İzin önceden işaretli değildir ve hizmet şartı değildir.
- Web: SMS çalışma alanında **Yasal gönderici**, **Önizleme**, **Ticari izinler** (ret kaydı dahil) sekmeleri; Hesabım'da **İletişim tercihlerim** kartı (izin ver/geri çek, geçmiş).
- Mobil: Hesabım'da **İletişim tercihlerim** kartı; yalnız çevrimiçi, önbelleğe/kuyruğa yazmaz, sunucu yanıtı gelmeden tercih değişmiş sayılmaz. Taşıma beyaz listesi yalnız bu iki yolu açar.
- Sınırlar: kişi listesi hesabın aktif `PropertyMembership` kayıtlarına ve platforma dayanır; yalnız kişi bağıyla (PropertyMembership olmadan) bağlı sakinlerin site göndericisi henüz listelenmez. Operasyonel SMS tercihi ve gönderim raporu grafikleri yoktur.

## BF-SMS 06 — Güvenlik, ret ve İYS

**Uygulananlar**
- **Ret sonrası engel iki noktada:** kuyruğa alırken (`SmsQueue`) ve sağlayıcı çağrısından hemen önce (`SmsDispatcher`, yalnız `MARKETING`). Kuyrukta beklerken ret gelen ileti `FAILED OPTED_OUT` olur, şifreli gövde silinir, kontör düşmez. Böylece ret, 6563/Yönetmelik'teki 3 iş günü üst sınırını beklemeden anında uygulanır.
- **SMS ile ret:** `POST /api/v1/sms/opt-out-callbacks` (anonim, sağlayıcı için). `Sms:OptOutCallbackKey` (≥32 karakter, `.env`/secret store; Git'e yazılmaz) yoksa 404. Gövde ≤4096 bayt, `X-BuildingFlow-Signature` = gövdenin HMAC-SHA256 hex'i; sabit süreli karşılaştırma, yanlış imza 401. `{phoneNumber,text,shortCode,providerMessageId}`; metin `RET` veya `RET <kelime>` değilse kayıt yok. Kısa numara + normalleştirilmiş kelime (büyük harf, boşluk, İ→I) eşleşen her göndericiye `REJECTED/SMS_REPLY`; yalnız `RET` o kısa numaradaki tüm göndericileri durdurur. Aynı `providerMessageId` tekrarları aynı operationId'ye düşer (tek kayıt). Log yalnız eşleşen gönderici sayısını yazar; telefon/metin loglanmaz.
- **Yetki ve izolasyon:** yönetici uçları rol + aktif Property üyeliği (yabancı alan 404), gönderici yazımı SUPER_ADMIN/SITE_ADMIN, platform göndericisi SUPER_ADMIN, hesap uçları yalnız kendi kaydı. Toplu/kampanya gönderim ucu yoktur; tekil elle gönderim yönetici rolündedir ve tanıtım tipi gönderemez.
- **Veri minimizasyonu:** yanıtlarda telefon `+90********67` maskeli; düz telefon yalnız ret/onay kaydında (İYS ve ret eşleşmesi için gerekli) ve şifreli SMS yükünde. Rate limit: mevcut global `/api` sınırı (anonim IP, kimlikli `sub` okuma/yazma) yeni uçları da kapsar; ek limit eklenmedi.

**İYS ihtiyaç ve yükümlülük analizi (uygulama yok, karar gerekiyor)**
- İYS (İleti Yönetim Sistemi) kaydı, ticari elektronik ileti gönderen hizmet sağlayıcılar için zorunludur; onaylar İYS'ye yüklenmeli ve gönderimden önce İYS'de onay sorgulanmalıdır. Ret İYS üzerinden de gelebilir ve tüm kanallarda geçerlidir. Belirli eşiklerin altındaki/istisnai durumlar ve güncel süreler resmî İYS/Ticaret Bakanlığı kaynaklarından teyit edilmelidir; bu belge hukuki görüş değildir.
- Hizmet SMS'leri (OTP, aidat, bakım, rezervasyon, acil) ticari ileti olmadığı için İYS onayı gerektirmez; bu modelde `Footer=NONE`, İYS kontrolü yapılmaz.
- Modelde hazır olan: gönderici başına `is_iys_integration_enabled` + marka kodu, kayıt başına `iys_status` (`PENDING/SYNCED/FAILED`), referans ve hata kodu; İYS açık göndericide `SYNCED` olmayan onayla gönderim `IYS_NOT_SYNCED` ile durur (fail-closed).
- Eksik olan (gerçek entegrasyon fazı): İYS hesabı ve API erişimi (hizmet sağlayıcı/aracı kurum), `PENDING` kayıtları yükleyen ve İYS'den gelen retleri `IYS` kaynağıyla yazan worker, imzalı İYS geri bildirimi. Hesap olmadan doğrulanamayacağı için bu fazda kodlanmadı. Gönderici başına İYS'yi açmak, entegrasyon gelene kadar o göndericinin tanıtım SMS'ini durdurur.

## BF-SMS 07 — Kabul matrisi

| Senaryo | Kanıt |
| --- | --- |
| Tenant izolasyonu | `SmsConsentApiTests` yabancı SITE_ADMIN/RESIDENT 404/403, başka hesabın göndericisi 404; `SmsApiTests` mevcut kapsam |
| İzinsiz pazarlama SMS'i | `SmsPolicyResolverTests` `COMMERCIAL_CONSENT_REQUIRED`, satır yazılmaz |
| İzin geri çekme / ret | Hesap geri çekme, yönetici reti, imzalı SMS reti; kuyruktaki ileti gönderimde `OPTED_OUT` (`SmsSecurityTests`) |
| Eksik MERSİS / RET | Ret kanalı kapalı `OPT_OUT_UNAVAILABLE`, gönderici yok `LEGAL_SENDER_MISSING`; MERSİS'siz gönderici iletişim bilgisiyle alt bilgi (`SmsFooterTests`) |
| OTP | Ticari olmayan 7 kategori alt bilgisiz ve DB'siz; `PhoneOtp*` testleri ayrı hattı korur |
| Sağlayıcı hatası | `SmsApiTests` geçici/kalıcı sağlayıcı hatası, kontör düşmez |
| İYS senkron hatası | `IYS_NOT_SYNCED` hem `PENDING` hem `FAILED` için; `SYNCED` sonrası gönderilir |
| Eşzamanlı izin değişikliği | `CommercialConsentStoreTests` 6 eşzamanlı karar gönderici kilidiyle sıralı, operationId replay/409 |
| Mobil ve masaüstü görünüm | Web/mobil sözleşme testleri ve mobil export geçti; gerçek tarayıcı/cihaz görsel kontrolü **yapılmadı** |

## BAKIM — 26.10.2026 (görevler #66–#70)

- **#69 Sakin göndericileri:** iletişim tercihleri platform + aktif `PropertyMembership` + hesabın oturum bağlamlarındaki (malik/kiracı/sakin, `IMobileSessionStore`) Property göndericilerini listeler. Katı oturum modunda kanonik bağı olmayan hesap yalnız üyelik/platform görür.
- **#70 Hizmet SMS tercihi:** `sms_operational_preferences` (hesap başına tek, doğrulanmış telefon). Kapalıyken `DUES/MAINTENANCE/RESERVATION` kategorileri (MANUAL dahil) `OPERATIONAL_OPTED_OUT` ile kuyruğa alınmaz; güvenlik, hesap doğrulama ve acil durum etkilenmez. `PUT /api/v1/account/communication-preferences/operational` ve mobil `POST .../operational` `{enabled}`; doğrulanmamış telefon 409. Overview yanıtında `operationalSmsEnabled` (kayıt yoksa `true`). Dolu tabloda Down engellenir.
- **#66 Web build:** `turbopack.root` ve `outputFileTracingRoot` depo kökü; standalone çıktı `.next/standalone/frontend/buildingflow-web/server.js`. Dockerfile `/app/shared` kopyalar ve bu yolu kullanır.
