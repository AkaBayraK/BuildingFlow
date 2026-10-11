# Güncel oturum — İŞ09

10.10.2026 kullanıcı Faz09 başlatmayı onayladı; dört adımın sırayla yürütülmesi aynı onayın kapsamıdır. Kayıt12/14. **11.10.2026 adım01–03 TAMAMLANDI; adım04 DEVAM EDIYOR; faz 3/4=%75.00, iş 74/83=%89.16.** SMTP e-posta seçildi; SMS sağlayıcı sonra (kapalı kalır). Gerçek alıcıya mesaj gönderme. Tek agent; yeni skill/alt agent yok.

Dal: `faz-09-messaging` (main'den), adım adım commit; main'e merge ve push yok, remote tanımlı değil. `frontend/buildingflow-web/next-env.d.ts` farkı `next dev`in otomatik ürettiği `.next/dev/types` yoludur; commitlenmez, kaynak değişikliği değildir.

Adım01 kanıtı TESTING.md'de: 345 .NET (88 unit + 257 integration), 76 FE, typecheck, 318 lisans, 21 korunan dosya. RabbitMQ testleri sandbox içinden AMQP el sıkışmasında zaman aşımına uğruyor; yerel yetkili çalıştırma gerekir. Test RabbitMQ namespace'i yalnız `buildingflow-test-<uuid>`; mevcut kuyruk yok/silinmez.

Adım02 tamam: Domain `OutboxMessage`/`ProcessedMessage`, migration 20261011043002_MessagingOutboxAndInbox (yerel ana DB'ye uygulanmadı; `bash scripts/db.sh update` kullanıcı onayıyla), `EfMessageOutbox`, `OutboxRelay` (SKIP LOCKED, backoff), `MessageProcessor` (inbox tekilleştirme), `RabbitMessageConsumer`, Worker `OutboxRelayWorker`/`MessageConsumerWorker`; compose worker'a DB bağlantısı eklendi. RabbitMQ test sınıfları tek paralel olmayan xUnit koleksiyonunda (quorum Ra zaman aşımı nedeniyle).

Adım03 tamam: `RabbitMessagingOptions.RetryDelays/MaxAttempts`, gecikme başına `{prefix}.retry.{ms}` fanout exchange + TTL quorum kuyruğu, ana kuyrukta x-delivery-limit=10/at-least-once DLX, `RabbitRetryPolicy` başlıkları, consumer'da confirm'li retry/DLQ yayını sonrası ACK.

Adım04 planı: `SmtpEmailHandler` (System.Net.Mail veya lisansı uygun istemci; yeni paket gerekirse lisans kaydı), Smtp ayarları dışarıdan (.env/compose; sır yazılmaz), Mailpit (127.0.0.1:1025 compose'da yayınlı değilse yalnız test için erişim incelenir) ile başarılı/hatalı teslim testleri; yalnız example.test alıcıları. SMS kapalı kalır.

Korunan21 dosya hash'i /tmp/bf09-protected.json: .env, FE manifest/lockfile ve tüm önceki migration kaynak/Designer. Ana DB/user kayıtları/parola korunur; DB işi yalnız ayrı buildingflow_tests PostgreSQL ile. Yerel SDK /tmp/buildingflow-dotnet (10.0.401), DOTNET_ROOT/DOTNET_CLI_HOME=/tmp/buildingflow-dotnet-home/NUGET_PACKAGES=/tmp/buildingflow-nuget ve -m:1 --disable-build-servers gerekir. Sırları output/log/Git'e yazma.

Kullanım: giriş/çıkış/toplam token, kalan context, oturum/hesap limiti ve sıfırlanma: BİLGİ MEVCUT DEĞİL (bu turda Codex usage aracı yok).
