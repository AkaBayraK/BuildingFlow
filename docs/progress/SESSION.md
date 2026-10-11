# Güncel oturum — İŞ09

10.10.2026 kullanıcı Faz09 başlatmayı onayladı; dört adımın sırayla yürütülmesi aynı onayın kapsamıdır. Kayıt12/14. **11.10.2026 İŞ09 4/4 TAMAMLANDI; faz %100.00, iş 75/83=%90.36. Sonraki faz/kapsam için kullanıcı onayı beklenir.** SMTP e-posta seçildi; SMS sağlayıcı sonra (kapalı kalır). Gerçek alıcıya mesaj gönderme. Tek agent; yeni skill/alt agent yok.

Dal: `faz-09-messaging` (main'den), adım adım commit; main'e merge ve push yok, remote tanımlı değil. `frontend/buildingflow-web/next-env.d.ts` farkı `next dev`in otomatik ürettiği `.next/dev/types` yoludur; commitlenmez, kaynak değişikliği değildir.

Adım01 kanıtı TESTING.md'de: 345 .NET (88 unit + 257 integration), 76 FE, typecheck, 318 lisans, 21 korunan dosya. RabbitMQ testleri sandbox içinden AMQP el sıkışmasında zaman aşımına uğruyor; yerel yetkili çalıştırma gerekir. Test RabbitMQ namespace'i yalnız `buildingflow-test-<uuid>`; mevcut kuyruk yok/silinmez.

Adım02 tamam: Domain `OutboxMessage`/`ProcessedMessage`, migration 20261011043002_MessagingOutboxAndInbox (yerel ana DB'ye uygulanmadı; `bash scripts/db.sh update` kullanıcı onayıyla), `EfMessageOutbox`, `OutboxRelay` (SKIP LOCKED, backoff), `MessageProcessor` (inbox tekilleştirme), `RabbitMessageConsumer`, Worker `OutboxRelayWorker`/`MessageConsumerWorker`; compose worker'a DB bağlantısı eklendi. RabbitMQ test sınıfları tek paralel olmayan xUnit koleksiyonunda (quorum Ra zaman aşımı nedeniyle).

Adım03 tamam: `RabbitMessagingOptions.RetryDelays/MaxAttempts`, gecikme başına `{prefix}.retry.{ms}` fanout exchange + TTL quorum kuyruğu, ana kuyrukta x-delivery-limit=10/at-least-once DLX, `RabbitRetryPolicy` başlıkları, consumer'da confirm'li retry/DLQ yayını sonrası ACK.

Adım04 tamam: `SmtpOptions`/`SmtpEmailHandler` (System.Net.Mail, STARTTLS zorunlu, yerel Mailpit istisnası), `PermanentMessageFailureException` → doğrudan DLQ, Worker `Smtp:Enabled` ile email consumer'ı açar; compose worker yerel Mailpit'e yönlendirildi; `scripts/test.sh` geçici `buildingflow-test-mailpit-*` konteyneri açar/kapatır (testler BUILDINGFLOW_TEST_SMTP_PORT/MAILPIT_URL ister).

Açık kararlar (kullanıcı): yerel ana DB'ye migration (`bash scripts/db.sh update`) ve worker/API imajlarının yeniden derlenmesi; hangi iş olaylarının e-posta üreteceği; uygulama içi bildirim deposu; üretim SMTP sağlayıcısı; main'e merge.

Korunan21 dosya hash'i /tmp/bf09-protected.json: .env, FE manifest/lockfile ve tüm önceki migration kaynak/Designer. Ana DB/user kayıtları/parola korunur; DB işi yalnız ayrı buildingflow_tests PostgreSQL ile. Yerel SDK /tmp/buildingflow-dotnet (10.0.401), DOTNET_ROOT/DOTNET_CLI_HOME=/tmp/buildingflow-dotnet-home/NUGET_PACKAGES=/tmp/buildingflow-nuget ve -m:1 --disable-build-servers gerekir. Sırları output/log/Git'e yazma.

Kullanım: giriş/çıkış/toplam token, kalan context, oturum/hesap limiti ve sıfırlanma: BİLGİ MEVCUT DEĞİL (bu turda Codex usage aracı yok).
