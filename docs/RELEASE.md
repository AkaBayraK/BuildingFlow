# Yayın rehberi ve preflight

İŞ11 adım04. Bu belge yayın adayının hedef ortama nasıl kurulacağını anlatır. Bu bilgisayarda dış domain, TLS sertifikası, gerçek SMTP hesabı ve CI sunucusu yoktur; burada doğrulanan şey yerel araçlar ve yayın adayıdır, üretime yapılmış bir dağıtım değildir. Kanıtlar: [TESTING.md](TESTING.md).

## 1. Yayın öncesi kalite kapısı

```bash
bash scripts/ci.sh
python3 scripts/test-backup-restore.py
```

İkisi de çıkış 0 vermelidir. `ci.sh` lisans, FE test/typecheck/build/audit, izole altyapıyla backend regresyonu, EF model eşliği, idempotent SQL provası ve NuGet audit'i çalıştırır. Workflow karşılığı [.github/workflows/quality.yml](../.github/workflows/quality.yml).

## 2. Üretim yapılandırması

1. [deploy/production.env.example](../deploy/production.env.example) dosyasını Git dışına kopyalayın, değerleri sır deposundan doldurun.
2. Preflight çalıştırın; yalnız kural adlarını yazar, değer yazdırmaz:

```bash
python3 scripts/release-preflight.py /güvenli/konum/production.env
```

Kurallar: `Production` ortamı; HTTPS issuer/metadata; açık HTTPS CORS origin'leri; `*` içermeyen AllowedHosts; forwarded header yalnız tek tek IP olarak listelenen proxy'den; rate limit açık; web `BUILDINGFLOW_PUBLIC_HTTPS=true`, HTTPS API/OIDC ve API ile aynı issuer; en az 16 karakterlik DB/RabbitMQ/MinIO sırları; SMTP açıksa dış host, STARTTLS, yerel olmayan gönderen ve parola.

API de aynı güvenlik kurallarını başlangıçta kendisi denetler ve geçersiz yapılandırmada açılmaz (`ReleaseSecurity.ValidateProduction`).

## 3. HTTPS ve reverse proxy

- TLS, reverse proxy'de sonlanır. Proxy'nin API'ye bağlandığı IP adresi `Security__KnownProxies__0` olarak verilir. Proxy `X-Forwarded-For` ve `X-Forwarded-Proto` başlıklarını kendisi yazmalı, istemciden geleni silmelidir.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED` açılmaz; açıksa API başlamaz.
- API HTTPS olmayan `/api` isteğini `400 BF_INVALID_REQUEST` ile reddeder. HSTS 180 gün; `includeSubDomains` ve preload açık değildir.
- Web imajında `NEXT_PUBLIC_*` değişkenleri **build argümanıdır**. `BUILDINGFLOW_PUBLIC_HTTPS` ise **çalışma zamanında** okunur ve HSTS ile `upgrade-insecure-requests` ayarlarını açar. Bu değişken `true` olduğu halde build HTTP adreslerle yapılmışsa sayfalar 500 döner (bilinçli olarak kapalı başarısızlık).
- Rate limit süreç içindedir ve tek API örneği için geçerlidir. Birden fazla API örneği çalışacaksa proxy veya gateway seviyesinde de limit uygulanmalıdır.

## 4. Kurulum ve migration

1. Hedef PostgreSQL'i ve Keycloak'ı hazırlayın; Keycloak realm/istemci ayarları için `scripts/configure-identity.sh` akışını hedef ortama uyarlayın (PKCE S256, password grant kapalı).
2. **Yükseltmeden önce yedek alın** (bölüm 6).
3. Migration için iki yol vardır:
   - Uygulama migrator'ı: API imajı `--migrate` argümanıyla tek seferlik iş olarak çalıştırılır (compose'daki `buildingflow-migrations` hizmeti gibi), DDL yetkili kimlik kullanır.
   - SQL: `dotnet ef migrations script --idempotent` çıktısı DBA tarafından incelenip uygulanır. Betik, sonlandırılmamış ham SQL düzeltmesiyle birlikte geçici bir DB'ye iki kez uygulanarak CI'da doğrulanır.
4. API, worker ve web'i başlatın; aşağıdaki smoke'ları çalıştırın.

## 5. Geri dönüş

- Kod geri dönüşü: önceki imaj etiketine dönün. Şema geriye uyumluysa (yalnız yeni tablo eklendiyse) DB'ye dokunmak gerekmez.
- Şema geri dönüşü: `Down` migration'ları veri varsa bilerek hata verir (finans, kişi ve sayaç geçmişi korunur). Veri içeren bir sürümü geri almanın yolu, yükseltme öncesi yedeği **yeni bir hedefe** geri yükleyip trafiği oraya çevirmektir ([BACKUP_RESTORE.md](BACKUP_RESTORE.md)).
- Canlı volume'un veya veritabanının üzerine restore yapılmaz.

## 6. Yedek

[BACKUP_RESTORE.md](BACKUP_RESTORE.md): yazıcılar durdurulmuşken `backup --confirm-maintenance`, ardından `restore-check`. Yedekler kişisel veri ve parola hash'leri içerir; şifreli, erişimi kısıtlı bir konumda saklanır.

## 7. SMTP

Worker'da `Smtp__Enabled=true`, dış host, 587/STARTTLS ve sır deposundan gelen kimlik bilgileri kullanılır. İlk gönderim yalnız sahibi bilinen bir test adresine yapılır. SMS sağlayıcısı yoktur; uygulama içi bildirim kanalı bulunmaz.

## 8. Yayın sonrası smoke ve kabul

| Kontrol | Komut | Not |
| --- | --- | --- |
| Servis ve kimlik smoke'u | `bash scripts/smoke.sh` | Salt-okunur; web/API/DB sağlığı, anonim ve geçersiz JWT 401, Keycloak discovery, broker, MinIO, Grafana, worker |
| Gözlemlenebilirlik | `python3 scripts/test-observability.py` | Salt-okunur; metrik, log, trace ve hassas sorgu dışlama |
| Anonim SSR | Oturumsuz `curl` ile yönetim rotaları | HTML'de kayıt adı veya kişi verisi bulunmamalı |
| Güvenlik başlıkları | `curl -I` | API: HSTS, `no-store`, `nosniff`, `DENY`. Web: nonce'lu CSP, HSTS |
| Rol ile kabul | Gerçek hesaplarla TR/EN, mobil, form geri bildirimi | Yerel kabul fazlarda yapıldı; hedef ortamda tekrarlanmalı |

## Bilinen sınırlar

Dış domain/TLS, gerçek SMTP, CI sunucusu ve uzak yedek hedefi bu ortamda yoktur. Online ödeme, SMS, Tempo trace deposu, PITR yedeği ve çok örnekli rate limit kapsam dışıdır. Production go-live, kullanıcının hedef ortamıyla ayrıca yapılır.
