# İŞ11 — Yayın hazırlığı ve kalite

11.10.2026 kullanıcı Faz11 geliştirmesini onayladı. İş yol haritası kayıt14/14, dört resmi görev; mevcut83 görev korunur. Önceki Faz10 çalışma ağacı korunur. Tek agent; ek skill yok.

## Onaylı kapsam ve kabul planı

1. Güvenlik/erişim: Production HTTPS origin/issuer/metadata ve dar AllowedHosts doğrulaması, yalnız açıkça güvenilen proxy, HSTS/güvenli başlıklar/no-store, API authenticated/anonymous istek sınırı ve kodlu429. FE nonce CSP, güvenli locale cookie; OIDC/PKCE/token depolama mevcut kalır. Negatif JWT/rol/üyelik, sahte forwarded header, HTTP/HTTPS ve rate-limit testleri.
2. CI: FE test/typecheck/build; gerçek ayrı PostgreSQL/MinIO/RabbitMQ/geçici Mailpit backend regression; model/migration+idempotent SQL, lisans, dependency audit. Aynı komutlar yerelde çalıştırılabilir. CI cache/log/artifact'ine .env veya sır koyma; main'e deploy/merge/push yok.
3. Yedek/dönüş: quiesced uygulama/Keycloak + PostgreSQL logical dump ve durdurulmuş MinIO cold snapshot; SHA256 manifest, özel izinler, yalnız yeni izole target'a restore. Kendi fixture'ıyla schema/veri/kimlik ve dosya hash provası; canlı volume/veri değiştirilmez. SQL backup tek başına dosya/kimlik tutarlılığı sağlamaz.
4. Kabul/yayın rehberi: mevcut uçtan uca domain akışları/negatif API testleri ve TR/EN/mobile/form-feedback/anonim SSR; gerçek pipeline smoke; kurulum/migration/rollback/backup/SMTP/HTTPS/yayın preflight/runbook. Dış domain, TLS sertifikası, gerçek SMTP ve CI sunucusu bu bilgisayarda yoktur; bunları hazır üretim dağıtımı olarak gösterme. Teslim yayın adayı ve doğrulanmış yerel araçlardır.

Her adım kanıtı TESTING.md'de, tamamlanan görev mevcut milestones.ts içinde işaretlenir. Faz12 uydurulmaz; production go-live ayrıca kullanıcının hedef ortamıyla yapılır.
