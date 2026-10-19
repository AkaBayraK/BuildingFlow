# PERSON FAZ10 — Güvenlik ve Lisans Doğrulaması

18.10.2026. Kullanıcı FAZ09'u kabul edip FAZ10'u başlattı; #14 Done. Mevcut [#15](https://github.com/AkaBayraK/BuildingFlow/issues/15) inceleme görevidir. **Doğrulama çalışması teslim edildi; güvenlik/yayın kapısı GEÇMEDİ.** Açık bulgu veya çalıştırılmamış tarama başarı sayılmadı. FAZ11 başlamadı.

## Kapsam, yöntem ve önkoşul

Person paketinin MASTER_PROMPT/STANDARDS/PHASES/PHASE10 talimatları, kök/ilgili AGENTS, önceki raporlar, kişi/context/mobil sync sözleşmeleri, lisans/env/CI/release belgeleri ve mevcut kod incelendi. Yerel akadev-phase/security/license/verify yönergeleri tek agent ile uygulandı. Yeni agent, tarayıcı, bağımlılık, lisans veya araç kurulmadı.

FAZ09 kaynakları ve test altyapısı mevcut. Etki planı: mevcut npm/NuGet kaynaklarını sorgula; erişim/rollback/log kontrollerini izole testlerle doğrula; native/MASVS, LICENSE/NOTICE ve secret sınırlarını kaynakta incele; bulunan dar kaynak takibi sorununu çalışan dosyayı değiştirmeden düzelt; çalıştırılamayan araçlar için komut/eksik raporu hazırla. Genel ürün geliştirmesi ve canlı güvenlik operasyonu bu kapsamda yoktur.

Makine tarafından okunabilir, sır değeri içermeyen özet: [phase-10-evidence.json](phase-10-evidence.json). Ham audit/test çıktıları ve sınırlı kaynak inceleme script'i `/tmp/bf-person10/` altındadır; kalıcı sonuç bu rapor ve JSON'dur.

## Bulgular ve karar

| Kimlik | Önem / güven | Kanıt ve etki | Durum / gereken işlem |
| --- | --- | --- | --- |
| P10-S01 | Yüksek / doğrulandı | `deploy/seaweedfs/private-s3.json` ignore kuralına rağmen HEAD/indexte takipliydi. Çalışan `.env` anahtarlarının dosyada bulunduğu ve dosyanın HEAD ile aynı olduğu değerleri göstermeden karşılaştırıldı. Depo geçmişi/klonları erişim bilgisi yayılımı riski taşır; deponun herkese açık olduğu varsayılmadı. | **Kısmi düzeltme:** yalnız `git rm --cached` ile staged deletion hazırlandı; gerçek dosya ve .env hash'leri değişmedi. Commit/push, geçmiş yeniden yazma veya anahtar yenileme yapılmadı. Yeni takip engeli CI'de. Eski anahtarların kontrollü yenilenmesi, geçmiş/klon incelemesi ve düzeltmenin ilgili commit'e alınması açık. Ignore etmek eski kaydı kaldırmaz. |
| P10-S02 | Yüksek / advisory eşleşmesi doğrulandı | Mobil lockfile audit29 paket kaydı:18high/11moderate;4kök advisory. Uygulamada exploit gösterilmedi; bağımlılık eşleşmesi ile erişilebilir exploit farklı kanıtlardır. | **AÇIK; mobil güvenlik/yayın kapısı GEÇMEDİ.** SDK uyumlu çözüm ve yeniden audit gerekli; aşağıdaki tabloya bakın. |
| P10-S03 | Eksik doğrulama / doğrulandı | Fortify/sourceanalyzer/fcli, Semgrep, Gitleaks, TruffleHog, OSV scanner, Trivy, Syft PATH'te yok. Tam SAST, Git geçmişi/entropy taraması, container OS SBOM taraması yapılmadı. | **DOĞRULANAMADI.** Kurulum yapılmadı; aşağıda hazırlanmış komutlar ve önkoşullar var. Sınırlı regex veya kod incelemesi tam tarama sayılmaz. |
| P10-S04 | Orta / tasarım sınırı ve eksik runtime kanıtı | SQLCipher anahtarı `offline/cache.native.ts` içinde SecureStore WHEN_UNLOCKED_THIS_DEVICE_ONLY ile okunur; biyometri `person-authorization/device.native.ts/device-policy.ts` içinde ayrı cihaz bağı üzerindedir. Şifreli cache açılışı biyometriden önce olabilir. UI kilidi tek başına donanımsal biyometriye bağlı veritabanı anahtarı kanıtı değildir. | **KISMİ.** Normal uygulama kilit akışı port testli; native anahtar/biometri değişimi, OS backup/picker/background/screenshot ve gerçek cihaz MASVS kabulü açık. Runtime kodu değiştirebilen/root cihaz saldırısına dayanıklılık gösterilmedi; doğrulanmış exploit iddiası yok. |
| P10-L01 | Yayın kabulü eksik / doğrulandı | npm/NuGet ve mobil lock lisans metadata'sı doğrulandı; tam native binary/container dağıtımında bütün telif/NOTICE/OS/copyleft koşulları incelenmedi. | **KISMİ.** Mevcut lisanslar korundu; artifact SBOM ve dağıtım bildirimlerinin son binary/imaj üzerinden kabulü açık. Metadata sonucu hukuki uygunluk sertifikası değildir. |

### Mobil SCA ayrıntısı

`npm audit --json --package-lock-only` exit1; web aynı sorgu exit0. Npm kayıt toplamı29, bağımsız29 açık anlamına gelmez. Aşağıdaki4 advisory dependency zincirlerinde üst paketlere yayılır. Önceki MOBİL FAZ14 raporundaki28 sayısı tarihsel sonuçtur; bugünkü güncel sonuç29'dur.

| Kurulu kök paket | Advisory / güncel durum | Yerel bağımlılık yolu ve değerlendirme |
| --- | --- | --- |
| braces3.0.3 | [GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm), high;<=3.0.3, bildirilmiş yama yok; registry latest3.0.3 | Expo57→@expo/metro→metro-file-map→micromatch. Desen işleme/stack exhaustion; geliştirme/build yüzeyi. Yalnız güvenilen dosya/desen ve dar dev-server erişimi sınırlayıcı önlemdir, paket yaması değildir. |
| node-forge1.4.0 | [GHSA-86w9-cpqp-85rv](https://github.com/advisories/GHSA-86w9-cpqp-85rv), high;<=1.4.0, bildirilmiş yama yok; registry latest1.4.0 | Expo→@expo/cli/@expo/code-signing-certificates. RSA imza doğrulaması advisory'si; API JWT doğrulaması ASP.NET Core'dadır. Expo sertifika/build akışı ayrıca değerlendirilmelidir. |
| decode-uri-component0.2.2 | [GHSA-vcc3-ghjq-m6fr](https://github.com/advisories/GHSA-vcc3-ghjq-m6fr), moderate;<=0.4.2, yama0.5.0 | query-string7.1.3→Expo Router57.0.26. URL çözümleme DoS yüzeyi. Uygulamanın native incoming guard'ı4096boyut ve malformed percent kontrolü taşır; bütün SDK yollarının kapandığı kanıtlanmadı.0.5.0 ESM modülüdür; CJS/Metro/Router uyumu doğrulanmadan override yapılmadı. |
| uuid7.0.3 | [GHSA-w5hq-g745-h8pq](https://github.com/advisories/GHSA-w5hq-g745-h8pq), moderate;<11.1.1; yamalı11.1.1/12.0.1/13.0.1; registry latest14.0.3 | xcode3.0.1→@expo/config-plugins/config. Build/prebuild yolu; advisory v3/v5/v6 dış buffer sınırlarıyla ilgilidir. Uygulama exploit'i gösterilmedi; kontrolsüz ana sürüm override'ı yapılmadı. |

Npm `fixAvailable` çıktıları Expo44.0.6 ve Router58.0.23 ana sürüm değişimlerini öneriyor. Bunlar Expo57/RN0.86 için doğrulanmış güvenli otomatik yama değildir. `npm audit fix`, force/override, paket sürümü veya lockfile değişimi yapılmadı; bulgu bastırılmadı. Güncel registry sürüm/lisans okumaları kurulum yapmadı. node-forge'un BSD-3-Clause seçimi mevcut mobil lisans politikasında kayıtlıdır; GPL kolu seçilmedi.

### Secret kontrolü ve dar düzeltme

Geçerli Git dosya listesiyle takipli+ignore edilmeyen untracked metinler,4MiB sınırı, dört token/private-key regex ailesi ve yerel .env credential değerlerinin tam kopyaları kontrol edildi. Başlangıçta906metin dosyasında tek private S3 dosyasındaki iki alan eşleşti; değerler hiçbir çıktıya yazılmadı. İndeks düzeltmesinden sonra aynı sınırlı listede905dosya/0eşleşme görüldü. Bu sayılar o tarama anına aittir; sonraki belge/CI dosyaları ayrı eklenmiştir.

Yeni `scripts/check-private-config.py`, dört özel runtime yolunun Git takibinde olmamasını ve ignore kuralını doğrular; içerik/parola okumaz. `.env`, web `.env.local`, mobil `.env`, private S3 ayarı kapsamdadır. `scripts/test_private_config.py` gerçek geçici Git fixture'larında ignored/untracked kabulü, force-track regresyonunun reddi ve eksik ignore reddini test eder. İki komut `scripts/ci.sh` başına eklendi. Ayrıca mevcut CI akışına mobile lockfile için `npm audit --audit-level=high --package-lock-only` eklendi; bugün bilinen yüksek bulgularla bu kapı exit1 verir ve altyapı testlerini başlatmadan akışı durdurur. Bulgu bastırma yoktur. Bu gate bilinen özel konfigürasyonun tekrar commit edilmesini engeller; başka yoldaki bütün sırları veya eski geçmişi taramaz.

Diskteki generated dosya korunur; git status'ta staged D görülmesi bilinçlidir. Çalışan servisin dosyası silinmedi, chmod/anahtar/rol veya container değişmedi. Git HEAD ve uzak depo bu tur değiştirilmedi; dolayısıyla geçmişteki sır riski kapanmış sayılmaz. Ham config, secret değerleri veya hassas git diff'i rapora eklenmedi.

## Erişim, log ve migration kontrolleri

İzole PostgreSQL/storage grubunda merkezi kişi/tenant/Property/scope, exact issuer+subject, aktif account/link/üyelik, tarihli görev/ilişki, OWNER/TENANT/RESIDENT readonly self ve ACCOUNTANT genel kişi reddi; native/legacy source sapması; yabancı UUID, push/replay/pull/photo sahipliği, rol yükseltme reddi ve iptal sonrası yetki kapanışı senaryoları çalıştı. Negatif JWT issuer/audience/lifetime/signature/malformed testleri ve güvenlik başlıkları gruba dahildir. Çalışma alanı UUID/rol seçimi backend grant'i değildir. İncelenen/testlenen sınırda yeni doğrulanmış BOLA/auth bypass bulunmadı; bu tam penetrasyon testi değildir.

`RequestTelemetry` metrik/trace etiketleri route template/method/status kullanır; `ExceptionHandler` kendi logunda exception türü+status yazar.6telemetri testi query/path sırlarının metrik etiketlerine gitmemesini, credential/query içeren exporter config'inin reddini ve kapalı provider'ı doğrular. Bunlar bütün uygulama/altyapı log akışlarının dinamik PII denetimi sayılmaz; gerçek pipeline/device log capture bu tur yapılmadı.

CentralPerson/PersonCenter/Assignment/Transition ve diğer migration testleri ayrı fixture şemasında upgrade/reapply/model eşliği, ortak audit alanları, geçmiş/makbuz değişmezliği ve dolu Down/rollback korumasını çalıştırdı. Canlı DB'de Down, yedek/restore, veri taşıma veya migration apply yapılmadı.

## MASVS eşlemesi — sertifika değildir

[OWASP MASVS](https://mas.owasp.org/MASVS/) kategorileri kontrol çerçevesi olarak kullanıldı; native cihaz/dinamik test olmadığı için tam uyum veya seviye sertifikası verilmez.

| Alan | Mevcut kaynak/test kanıtı | Açık doğrulama |
| --- | --- | --- |
| STORAGE / CRYPTO | SQLCipher sürüm kontrolü; SecureStore cihaz anahtarı; şifreli dosya/AAD; clear kuyruk nesli; snapshot whitelist | Native dosya/backup extraction, OS anahtar politikası ve gerçek şifreleme kabulü |
| AUTH | Keycloak Authorization Code+S256 PKCE, SecureStore token; server context kontrolü; biyometri/TTL/rollback/account port testleri | Gerçek biyometri/anahtar iptali, donanımsal bağ ve offline anahtar erişimi |
| NETWORK | Release config HTTPS/cleartext ve secret-public reddi; release negatif testleri | Son native binary/TLS/MITM/pinning tehdit profili testi |
| PLATFORM / PRIVACY | Incoming URL sınırı/malformed reddi; isteğe bağlı fotoğraf/rıza; minimum self DTO; gizlilik örtüsü | OS modal/picker/background/accessibility/screenshot ve gerçek cihaz veri izi |
| CODE | Web/NuGet audit temiz; mobil audit29kayıt ile başarısız | Tam SAST; mobil upstream/uyumlu yama ve yeniden audit |
| RESILIENCE | Root/jailbreak/attestation savunması vaat edilmedi | Risk profiline göre native runtime bütünlük/cihaz saldırısı kabulü |

## Lisans ve NOTICE

- Mevcut `scripts/check-licenses.py` **329npm/NuGet sürüm kaydında** tanınan/önceden incelenmiş metadata ile geçti. `npm run release:licenses` **871mobil lock kaydında** geçti. Bu sayılar ayrı envanterlerdir; toplanıp tekil bağımlılık sayısı diye sunulmaz.
- MIT AND Apache iki yükümlülüğü, MIT OR Apache/MIT OR CC0 için mevcut MIT seçimi ve BSD3 OR GPL için BSD3 seçimi mevcut politika/PHASE15 kaydıyla incelendi. Mevcut LICENSE/NOTICE kaynakları korunur; yeni lisans istisnası eklenmedi.
- Keycloak tema, next-intl/shadcn kaynak bildirimleri, Expo scaffold/splash lisans kopyaları ve SDK NOTICE kaynakları kontrol edildi. Frontend/API/worker Dockerfile'larındaki mevcut lisans kopyalama adımları okundu. Nihai native binary veya container filesystem'inin tüm bildirimleri karşılaştırılmadı.
- Compose image listesi okundu; imaj pull/build/servis restart yapılmadı. Redis/Grafana/Loki/libvips/MPL ve container OS gibi mevcut copyleft/dağıtım şartları [licenses.md](../../licenses.md) kapsamında ayrıca kalır. Geliştirici araçlarının kurumsal ticari kullanım hakkı/aboneliği doğrulanmadı. Kaynak metadata kontrolü bunların yerine geçmez.

## Bu tur sonuçları

| Kontrol | Sonuç |
| --- | --- |
| Seçili izole PG+fixture storage integration | **364/364**,0fail/skip |
| Backend unit | **244/244**,0fail/skip |
| Telemetry gizlilik/config | **6/6**,0fail/skip |
| Mobil check | **126/126**, TypeScript/lint PASS,0fail/skip |
| Seçili web security/person/session/return-path | **23/23**,0fail/skip |
| Mobil iOS/Android/web export | PASS; default bayraklar, native binary değildir |
| .NET solution build | **0hata/0uyarı** |
| Web npm audit | **0bulgu**,exit0 |
| NuGet vulnerable/transitive/no-restore | **7proje**, bildirilmiş0açık,exit0 |
| Mobil npm audit | **BAŞARISIZ**,29kayıt(18high/11moderate),exit1 |
| Lisans metadata |329npm/NuGet ve871mobil kayıt PASS |
| Private config gate |4runtime yol PASS; yeni **3/3** black-box fixture |
| Mevcut CI result / release preflight |6CI gate senaryosu PASS; valid template kabulü ve29unsafe varyant reddi PASS; sır çıktısı yok |
| Shell/diff |`bash -n scripts/ci.sh`, `git diff --check` PASS |
| Tam SAST/history/native/container scan |**ÇALIŞTIRILMADI / DOĞRULANAMADI**; araç/cihaz/artefakt erişimi yok |

Geniş backend filtre Person/Session/Mobile/Assignment/Transition/Relation, authentication/release-security, migration/persistence/property ve ilgili finans gruplarıdır; tüm backend suite değildir. Bir önceki fazın sayıları bu turun sonucu olarak kopyalanmadı. Shared checkout'taki eşzamanlı WEBSITE kod/testleri nedeniyle bu turdaki test toplamı ve model snapshot'ı önceki turdan farklıdır; bu faz yeni backend unit/ürün kodu eklemedi. Kaynak korumasında manifest/lock/proje hash'leri aynı; başka sohbetin EF snapshot/migration değişiklikleri korunur, bu fazın geliştirmesi olarak sahiplenilmez.

Tam CI (`scripts/ci.sh`) çalıştırılmadı: servis hazırlama/kurulum/temizleme etkileri vardır. İlgili gate'ler ayrı çalıştırıldı. Ana DB/.env/runtime private dosya/Keycloak/hesap/rol ve çalışan servisler korunur. Test kaynakları yalnız fixture DB/role/schema ve bucket'tır; temizlendi. Ağ sorguları advisory/registry metadata içindir; yeni paket veya araç indirilmedi/kurulmadı.

## Erişim sağlanınca çalıştırılacak komutlar

Aşağıdakiler **hazırlandı; çalıştırılmadı**. Yetkili/lisanslı uygun sürüm zaten kurulu runner ve güncel rulepack gerekir; bu görev yeni kurulum yetkisi vermez. .NET10/TS/Expo desteği ve kurumsal Fortify lisansı önce doğrulanır. macOS ortamında .NET Fortify çevirisi yapıldığı varsayılmaz; desteklenen Windows/Linux runner kullanılmalıdır. Çıktılar ignore edilmiş, erişimi sınırlı `artifacts/security` altında tutulur; otomatik dış yükleme yapılmaz.

```sh
mkdir -p artifacts/security
# Fortify: SDK/framework uyumlu, lisanslı runner üzerinde; backend örneği
sourceanalyzer -version
sourceanalyzer -b bf-person10 dotnet build backend/BuildingFlow.sln --no-restore
sourceanalyzer -b bf-person10 -scan -f artifacts/security/person-backend.fpr
# JS/TS ve her binary için vendor'un uygun çeviri/kapsam profili ayrıca gerekir.

# Tam secret/history kontrolü: kurulu Gitleaks ile, değerleri redact ederek
# --all bütün erişilebilir ref'leri kapsar; findings susturulmaz.
gitleaks git --redact --log-opts="--all" --report-format json --report-path artifacts/security/history.json .
gitleaks dir --redact --report-format json --report-path artifacts/security/worktree.json .
# Runtime .env/generated dosyalar ve fixture'lar bağlamıyla incelenir; kör allowlist yok.
```

Fortify komut biçimi [OpenText resmî .NET çeviri belgesinden](https://www.microfocus.com/documentation/fortify-static-code-analyzer-and-tools/2520/sast-ugd-html-25.2.0/Content/dotnet/cl-syntax.htm), secret komutları [Gitleaks resmî kullanımından](https://github.com/gitleaks/gitleaks) alınmıştır. Tam kurumsal SAST raporu ve kapsam manifest'i olmadan SAST PASS denmez. Native MASTG kabulü için kontrollü iOS/Android cihaz/binary, fake test account, dosya/backup ve OS key davranışı gerekir. Container taraması için sabit image digest'lerinin onaylı scanner/SBOM kaynağı ayrıca sağlanmalıdır; bu tur Docker scanner imajı çekilmedi.

Tekrar çalıştırılabilen mevcut kontroller:

```sh
python3 scripts/check-private-config.py
python3 scripts/test_private_config.py
python3 scripts/check-licenses.py
npm --prefix mobile/buildingflow-mobile run release:licenses
npm --prefix frontend/buildingflow-web audit --json --package-lock-only
npm --prefix mobile/buildingflow-mobile audit --json --package-lock-only
dotnet list backend/BuildingFlow.sln package --vulnerable --include-transitive --no-restore --format json
```

Audit exit0/JSON parse/feed hatası ve vulns ayrı değerlendirilir; ağ hatası temiz sonuç sayılmaz. Mobile exit1 bugün beklenen başarısız güvenlik sonucudur. Credential yenileme/geçmiş operasyonu için servis etkisi/geri dönüş planı ayrıca hazırlanmalıdır; bu rapor gerçek erişim bilgisini veya canlı yenileme komutuna gömülü sır içermez.

## Değişiklik, geri dönüş ve teslim

Bu fazın değişikliği: yeni `scripts/check-private-config.py`, `scripts/test_private_config.py`, `scripts/ci.sh` private config gate/testi ve yüksek seviyeli mobile audit kapısı; private generated config'in **yalnız indeksten çıkarılması**; bu rapor/JSON ve TESTING/progress/SKILLS_AND_AGENTS/GitHub görev kayıtları. Mevcut `.gitignore` doğru kuralı zaten içerdiğinden değiştirilmedi. Uygulama, kişi şeması, migration, paket/lock ve gerçek ortam ayarları bu faz tarafından değiştirilmedi. Başka sohbetteki WEBSITE dosyaları/migration'ı korundu.

Git indeksindeki deletion'ı geri alıp gerçek anahtarı tekrar takip etmek güvenlik düzeltmesini geri alır; önerilmez. Gate geri dönüşü kaynak CI değişikliklerinin normal incelemesiyle yapılabilir; DB/servis rollback gerekmez. Commit/push/merge/reset/stash/branch veya geçmiş yeniden yazma yapılmadı. Diskteki private config ve .env korunur.

Resmî altı adım inceleme/önkoşul/etki planı/uygulama/test-ve-eksik-raporu/teslim **6/6=%100,00 çalışma teslimi**; güvenlik kabulü/yayın hazırlığı yüzdesi değildir. Kullanıcının kabul ettiği önceki fazlar **9/11=%81,82**. #15 In review; açık S01–S04/L01 kullanıcıya bildirildi. FAZ11 onaysız başlamaz; başlatılması bu açık riskleri kapatmaz veya yayın onayı oluşturmaz.

Token giriş/çıkış/toplam/kalan context: **BİLGİ MEVCUT DEĞİL**. Son18.10.2026 Codexusage300dk kullanılan%84/kalan%16 reset18.10.2026 14:28:36;10080dk kullanılan%29/kalan%71 reset25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.
