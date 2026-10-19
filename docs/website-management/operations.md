# Website — Türkçe işletim ve yayın rehberi

WEBSITE FAZ09, 19.10.2026. Teknik yayın adayıdır; gerçek yayın kabulü [GitHub #30](https://github.com/AkaBayraK/BuildingFlow/issues/30) içinde açık izlenir. Kanıt [TESTING.md](../TESTING.md). Gerçek ortam onayı olmadan EİDS, TLS veya güvenlik kapıları kaldırılmaz.

## Yerel çalıştırma

Depo kökünde mevcut yapılandırmayı koruyarak ayrı terminallerde:

```bash
bash scripts/dev.sh api
bash scripts/dev.sh web
bash scripts/dev.sh website
```

Varsayılanlar API5005, yönetim3003, website3004. `api` mevcut migration prosedürünü kullanır; gerçek veritabanını yükseltmeden önce [MIGRATIONS.md](../MIGRATIONS.md) ve yedek politikasını inceleyin. `website` yalnız mevcut frontend node_modules bağlantısını kurar, yeni paket kurmaz. Yalnız public uygulama için `npm run setup --prefix website` ve `npm run dev --prefix website` de geçerlidir. İlk bağımlılık hazırlığı mevcut kilitli frontend kurulum prosedürüdür; npm ci/restore sonrasında `python3 scripts/check-licenses.py` çalıştırılır.

Kök3004 demo/noindex'tir; gerçek tenant `/{siteKey}` yolundadır. Yayınlı site yoksa404 beklenir. Demo genel3003 girişini, gerçek site kendi site/all girişini yeni sekmede açar; public sitede parola alınmaz. [Giriş sözleşmesi](../api/website-login.md).

## Test ve yayın adayı

```bash
python3 scripts/check-licenses.py
bash scripts/check-website.sh
python3 scripts/test_ci_gates.py
```

Website kapısı test/typecheck/build/public HTTP smoke ve `artifacts/website/npm.cdx.json` CycloneDX1.6 dökümü üretir. Port15005/13004 fixture'ları yalnız test içindir; script kendi süreçlerini kapatır. Üretim sunucusuyla aynı `.next` klasöründe eşzamanlı build yapmayın; temiz checkout/CI kullanın. Geliştirme ve build ortak Node bağımlılıklarını kullanır. Global `scripts/ci.sh` artık website kapısını da çalıştırır; başka modül audit hataları website başarısıyla kapanmaz.

SBOM mevcut frontend lock dosyasının incelenmiş npm metadata envanteridir; development/platform-optional bileşenleri içerir. Container OS/NuGet/binary envanteri ve ticari dağıtım/telif kabulünün yerine geçmez. Mevcut LICENSE/NOTICE kopyaları, kurulu paketlerin kök license/notice/copying/copyright dosyaları ve projeye ait görsel kaynak kaydı imajda LICENSES altında tutulur. npm-notices/notice-manifest.json kopyalananları, platform nedeniyle kurulu olmayanları ve kök bildirim dosyası olmayan paketleri ayrı gösterir; eksik dosyanın metni uydurulmaz ve dağıtım incelemesi #30 kapsamında kalır. [Lisans politikası](../licenses.md).

## Konteyner

```bash
docker build -f website/Dockerfile -t buildingflow-website:local .
python3 scripts/test-website-container.py buildingflow-website:local
```

Dockerfile mevcut Node24 Alpine tabanını ve frontend lock kurulumunu kullanır; bağımsız website lock/sürüm eklemez. Next standalone çalışır, USER node ve healthcheck vardır. Test konteyneri UUID adıyla ağsız/read-only/cap-drop/no-new-privileges kurulur; yalnız kendi konteynerini siler, gerçek servis/volume değiştirmez. Kök demo200, public WebP200, health204, yanlışHost fail-closed, CSP ve SBOM kontrol edilir. Health yalnız uygulama canlılığıdır; API/DB/TLS/EİDS hazır oluşu değildir. Health, yapılandırılmış platform Host başlığıyla `/health` ister; domain çözümlemesini atlayan global Host istisnası yoktur.

Mevcut yerel Compose'a isteğe bağlı profil eklendi. Servisleri gerçekten başlatmak kullanıcı/operatör işlemidir:

```bash
docker compose --env-file .env -f deploy/docker-compose.yml --profile website config --quiet
docker compose --env-file .env -f deploy/docker-compose.yml --profile website up -d --build buildingflow-website
```

Yerel profil loopback3004 yayınlar; API ve mevcut altyapı bağımlılıkları gerekebilir. Bu komut gerçek üretim için hazır bir TLS/proxy dağıtımı değildir; otomatik çalıştırılmadı. Yönetim imajı NEXT_PUBLIC_WEBSITE_ORIGIN build arg'ını da alır; preview form-action exact website origin olmalıdır.

## Üretim kapısı ve sırlar

Sunucu ortamında açıkça tanımlanır:

- WEBSITE_PUBLIC_ORIGIN: güvenilir HTTPS platform origin'i; Host'tan türetilmez.
- WEBSITE_MANAGEMENT_ORIGIN: ayrı HTTPS yönetim origin'i.
- WEBSITE_API_ORIGIN: sabit HTTPS API veya özel ağdaki http://buildingflow-api:8080. Cookie/JWT public read'e aktarılmaz; preview bearer yalnız özel POST gövdesinde kullanılır.
- Admin NEXT_PUBLIC_WEBSITE_ORIGIN: exact public origin; yeniden build gerektirir.
- API Website:PlatformDomain, Website:PreviewHmacKey ve Website:InquiryHmacKey: [domain](../api/website-domains.md) ve [ilan](../api/listings.md) sözleşmesine göre sunucu/sır deposunda. HMAC anahtarları public değişkene, Git'e, URL'ye veya loga konmaz.

Hazırlanmış environment ile:

```bash
node website/scripts/release.mjs --check-env
```

Bu offline kontrol HTTP public origin, eksik origin, userinfo/path/query/hash ve public/admin aynı origin'i reddeder; yalnız alan adlarını yazar, değer yazmaz. Başarılı konfigürasyon gerçek DNS/TLS/sağlık kabulü değildir. Asıl API/FE yayın kapıları [RELEASE.md](../RELEASE.md) ile birlikte uygulanır; backend güvenilir proxy, AllowedHosts/CORS, HTTPS issuer ve rate limit koşulları korunur. Keycloak callback yalnız yönetim uygulamasındadır; custom website domain'ini wildcard callback olarak eklemeyin.

## Yayın, geri alma ve kurtarma

CMS yayın öncesinde zorunlu KVKK/çerez metinleri, görünür sayfa/blok, medya hakları ve version doğrulanır. Yayın yeni immutable snapshot oluşturur. Geri yükleme eski kaydı değiştirmez; yeni yayın numarası üretir. No-store stratejisi nedeniyle yayın/askı/domain iptali sonraki istekte uygulanır; CDN cache'i ayrıca açmayın. 404 ile API503 arızasını birbirine karıştırmayın.

Kod rollout'u sabit ve kabul edilmiş imaj etiketi/digest ile yapılır; önce health, public tenant/SEO/media ve anonim özel veri yokluğu kontrol edilir. Kod rollback'i önceki kabul edilmiş imaja dönüş ve CMS restore işlemidir. Şema rollback'i otomatik değildir: website/listing/domain dolu Down guard'ları veri kaybını önlemek için reddeder. Yeni additive düzeltme migration'ı ve izole restore tercih edilir; uygulanmış migration değiştirilmez. [BACKUP_RESTORE.md](../BACKUP_RESTORE.md) quiesced yedek ve yeni izole hedef kuralı geçerlidir.

Domain DNS/TXT/TLS doğrulaması24saat güncel olmalıdır; süre geçince fail-closed olur. Operatör yenileme/sertifika süresini izler ve verify tekrarlar; bu teslim otomatik sertifika üretimi/yenileme servisi içermez. Domain rotate/disable/askı ile erişim kapanışı test edilir. Gerçek EİDS/hukuki ve KVKK/UAT/performans/ekran okuyucu/OS güvenlik kabulü #30 kapanmadan üretim onayı verilmez.
