# İŞ09 — Asenkron mesajlaşma analizi

10.10.2026 kullanıcı fazı başlatmayı onayladı; kayıt12/14, mevcut dört resmi görev milestones.ts kaynaklıdır. SMTP e-posta seçildi; SMS sağlayıcı entegrasyonu kullanıcı kararıyla ertelendi. SMS sözleşmesi korunur, gerçek SMS gönderilmiş gibi işaretlenmez. Tek agent; yeni skill/alt agent yok.

Mevcut RabbitMQ4.1.4 servisi hazır, IMessagePublisher yalnız sözleşme; FoundationWorker tüketici içermez. Domain EF/HTTP bağımlılığı almadan Application mesaj sözleşmelerini taşır; Infrastructure gerçek yayıncı sağlar. Kök MASTER_PROMPT/PHASES yok; frontend/buildingflow-ui-plan altındaki ayrı UI planı incelendi, tarihsel UI01 talimatı güncel iş kapsamını değiştirmez.

Adım01: EmailRequested/SmsRequested/NotificationRequested, UUID message/property/correlation, UTC zaman, schemaVersion1 ve açık tür allowlist'i; aynı nesne retry aynı MessageId. Küçük/bounded JSON, güvenli payload doğrulama, kalıcı mesaj/durable kuyruk, mandatory routing ve publisher confirms. Hassas içerik/alıcı/kimlik bilgisi loglanmaz; sınırsız generic CLR type serialization yok.

Adım02: DB ile dış broker tek transaction değildir. İlgili iş kaydıyla aynı DB transaction'ında BaseEntity Outbox; commit sonrası worker yayınlar. Şu an doğrudan business/API publish bağlanmaz: veri kaydedildi/mesaj gönderilemedi çelişkisi oluşturma. Tüketici için BaseEntity işlenmiş mesaj/inbox ve property scope gerekir; yeni entity/migration önce tüm sekiz alan/gerçek PG yaşam döngüsü testleriyle birlikte eklenir.

Adım03: kontrollü transient retry/backoff, failed queue, ACK ancak iş etkisi/kalıcı durum sonrası; aynı ID farklı payload reddi. SMTP gibi dış sağlayıcıda tam exactly-once iddiası yapılmaz; belirsiz teslim/çökme çift gönderim riski açıkça belgelenir. TTL/DLQ kuyruğuna yayın ACK alınmadan kaynak mesaj ACK edilmez.

Adım04: yerel Mailpit ile SMTP kabulü ve dış sırlarla SMTP yapılandırması; gerçek kullanıcıya mesaj göndermek kabul testine dahil değildir. SMS sağlayıcı seçilmediğinden kapalı kalır. API/rol/üye ve mevcut kullanıcı verisi korunur. Yeni SDK RabbitMQ.Client7.2.1 çift lisansından Apache2 seçilir; LICENSE/NOTICE ve envanter korunur.

Geliştirme öncesi testler: üç sözleşme roundtrip/type/scope/ID/locale/date/length, yanlış JSON/schema/type/oversize ve alıcı validasyonu; gerçek ayrı test namespace RabbitMQ routing/persistence/confirmation/mandatory-unroutable/concurrency/cancellation. Yeni kuyruk/exchange'ler yalnız buildingflow-test-* namespace altında oluşturulur/temizlenir; mevcut kuyruklar silinmez. Adım01 kabulü sonraki görevlerin tamamlandığı anlamına gelmez.

## Uygulanan yapı ve yapılandırma — 11.10.2026

- Akış: iş kodu `IMessageOutbox.Enqueue` ile aynı DbContext/transaction'a `outbox_messages` satırı ekler → Worker `OutboxRelayWorker` 2 sn'de bir `FOR UPDATE SKIP LOCKED` ile en fazla 20 mesajı confirm/mandatory yayınlar → `MessageConsumerWorker` yalnız etkin kanal türlerini tüketir → `MessageProcessor` `processed_messages` ile tekilleştirip handler'ı çağırır.
- RabbitMQ: `{prefix}.commands` (direct), `{prefix}.{type}` quorum (delivery-limit 10, at-least-once DLX), `{prefix}.{type}.failed`, `{prefix}.retry.{ms}` TTL kuyrukları. Başlıklar `x-buildingflow-attempt`, `x-buildingflow-failure` (`rejected`, `retry-exhausted`, `permanent-failure`). Varsayılan prefix `buildingflow.messaging.v1`, gecikmeler 5s/30s/2dk/10dk, en çok 5 deneme.
- SMTP ortam değişkenleri: `Smtp__Enabled`, `Smtp__Host`, `Smtp__Port` (587), `Smtp__Username`, `Smtp__Password`, `Smtp__UseStartTls` (true), `Smtp__AllowInsecureLocal` (yalnız localhost/mailpit), `Smtp__From`, `Smtp__FromName`, `Smtp__TimeoutSeconds`. Değerler secret/ortamdan verilir; depoya yazılmaz.
- Teslim garantisi en-az-bir-kezdir; SMTP gönderimi ile inbox commit'i arasında çökme çift mail üretebilir. DLQ mesajları elle incelenir; yeniden oynatma aracı yoktur.

