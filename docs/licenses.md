# Lisans politikası ve envanter

Kontrol tarihi: 2026-10-08. Kullanıcının kuralı: lisanssız/korsan veya kullanım hakkı belirsiz ürün/kütüphane/asset kullanılmaz. Açık kaynak da lisans şartlarına tabidir. BuildingFlow'un kendi koduna kullanıcı kararı olmadan açık kaynak lisansı atanmadı.

## TR/EN standardında eklenen bağımlılık — 2026-10-08

- **next-intl 4.14.9 — MIT**, resmî npm paketi. Resmî [lisans](https://github.com/amannn/next-intl/blob/main/LICENSE) ve kurulu paketin LICENSE dosyası okundu; telif bildirimi [yerel kopyada](../frontend/buildingflow-web/LICENSES/next-intl.txt) korundu. Paket sürümü sabittir; transitive bağımlılıklar envantere dahildir.
- `npm install` sonrası `python3 scripts/check-licenses.py` **312 npm/NuGet paket sürümü** için geçti; bilinmeyen lisans istisnası eklenmedi. Yeni font/görsel/ikon paketi veya ücretli template yok; mevcut Lucide/shadcn/TanStack kaynakları kullanılıyor.
- Frontend container kopyası next-intl ve mevcut shadcn LICENSE/NOTICE bildirimlerini korur. Bu metadata kontrolü aşağıdaki container/dağıtım/ticari araç yükümlülüklerinin yerine geçmez.

## Önceki iterasyonlarda eklenenler

- keycloak-js **26.2.4 — Apache-2.0**, resmî npm paketi. [Sürüm lisansı](https://github.com/keycloak/keycloak-js/blob/26.2.4/LICENSE.txt); paket LICENSE.txt dosyası korunur.
- Microsoft.AspNetCore.Mvc.Testing **10.0.12 — MIT**, resmî NuGet metadata'sı ve paketteki lisansla doğrulandı. Test bağımlılığıdır.
- Mailpit **1.27.10 — MIT**, yerel e-posta yakalayıcı. [Resmî lisans](https://github.com/axllent/mailpit/blob/develop/LICENSE).
- Keycloak 26.7.0 base template uyarlaması — Apache-2.0. [Keycloak lisansı](https://github.com/keycloak/keycloak/blob/26.7.0/LICENSE.txt); tema kökünde LICENSE.txt ve NOTICE.txt var. Orijinal güvenlik/form template'leri parent theme'den alınır.
- buildings.svg özgün kod çizimidir; logo kullanıcı tarafından sağlandı. Harici stock görsel, ücretli font/icon paketi veya lisans gerektiren UI kit eklenmedi.

## Mevcut temel bileşenler

| Bileşen | Lisans / kaynak | Uygulama notu |
| --- | --- | --- |
| Next.js, React, React DOM | MIT; lockfile/package lisansları | Telif ve lisans dosyalarını dağıtımda koru |
| TypeScript | Apache-2.0 | Lisans/notice korunur |
| .NET / ASP.NET Core / EF Core | MIT; NuGet nuspec | Dağıtım lisans bildirimlerini koru |
| Npgsql / EF provider | PostgreSQL License; NuGet nuspec | Bildirimler korunur |
| xUnit | Apache-2.0; NuGet nuspec | Eski xunit.abstractions 2.0.3 licenseUrl kanıtı licenses/reviewed-packages.json'da |
| PostgreSQL 17.6 | [PostgreSQL License](https://www.postgresql.org/about/licence/) | Container bildirimlerini koru |
| Keycloak 26.7.0 | Apache-2.0 | Özelleştirilmiş template için lisans/NOTICE eklendi |
| RabbitMQ 4.1.4 | [MPL-2.0](https://github.com/rabbitmq/rabbitmq-server/blob/main/LICENSE) | Değiştirilen MPL dosyaları/dağıtım şartları ayrıca incelenir |
| Redis 8.2.1 | [AGPLv3 / RSALv2 / SSPLv1 seçenekleri](https://redis.io/legal/licenses/) | Bu projenin açık kaynak kullanım tercihi AGPLv3; yayın/dağıtım yükümlülükleri kontrol edilir |
| SeaweedFS4.48 | [Apache-2.0](https://github.com/seaweedfs/seaweedfs/blob/4.48/LICENSE) | BuildingFlow aktif nesne deposu; lisans/NOTICE ve container bağımlılık koşulları korunur |
| Grafana OSS | [AGPLv3](https://github.com/grafana/grafana/blob/main/LICENSE) | Enterprise eklentileri bu kapsama dahil değildir |
| sharp / libvips | Apache-2.0 / LGPL-3.0-or-later | Next image bağımlılığı; libvips copyleft bildirimi ve yeniden bağlama/dağıtım koşulları korunur |
| caniuse-lite | CC-BY-4.0 | Veri atıf bildirimleri korunur |

Docker Desktop, Rider ve C# Dev Kit gibi geliştirici araçlarının ticari kullanım koşulları paket lisansından ayrıdır. [Docker Desktop koşulları](https://docs.docker.com/subscription-billing/desktop-license/): kişisel/eğitim ve uygun küçük işletme kullanımı ücretsiz; diğer kullanımda abonelik gerekebilir. VS Code çalıştırma görevleri ücretli IDE/uzantıya bağımlı değildir.

## Her kodlama turunda kontrol

```bash
# Restore/install tamamlandıktan sonra proje kökünde:
python3 scripts/check-licenses.py
# Bağımlılık güvenlik kontrolü ayrıca:
cd frontend/buildingflow-web
npm audit
```

Kontrol tüm npm lockfile paketlerini ve restore edilmiş solution NuGet paketlerini tarar; tanınmayan lisansı hata yapar. Onaylı SPDX ifadeleri sürüm bazlı manuel kanıtla kaydedilir; boş lisansı otomatik onaylama. Çıktı: [package-inventory.json](licenses/package-inventory.json). Eski paket kanıtı yalnızca belirtilen sürüm için geçerlidir.

Bu script yasal uygunluk sertifikası değildir; Docker imajlarının bütün OS/transitive paketlerini veya ticari kullanım hakkını otomatik denetlemez. Yayın öncesinde imaj SBOM'u, telif/NOTICE dağıtımı, değişikliklerin copyleft koşulları ve kuruluşun araç abonelikleri ayrıca doğrulanır. Bilinmeyen yeni bileşen eklenmeden önce lisansı belirlenir; mevcut teknoloji omurgası sırf lisansı copyleft diye sessizce değiştirilmez.

## 02 — UI tercihi ve eklenen bağımlılıklar

Kullanıcının açık tercihi: **shadcn/ui + TanStack Table**. Resmî ücretsiz/açık kaynak bileşenler kullanıldı; ücretli template, stock görsel, Pro özellik veya SaaS hesabı eklenmedi.

- shadcn/ui: **MIT**, resmî CLI 4.21.4 ile button/input/label/textarea/table/badge/dialog/dropdown-menu/skeleton kaynakları alındı. Kaynak lisansı components/ui/LICENSE.md içinde; kopyalanan kod projenin parçasıdır. [Resmî MIT lisansı](https://github.com/shadcn-ui/ui/blob/main/LICENSE.md).
- @tanstack/react-table **8.21.3: MIT**; [v8 kaynak lisansı](https://github.com/TanStack/table/blob/v8.21.3/LICENSE). Server-side pagination/sorting API'si v8 belgeleriyle eşleştirildi.
- radix-ui **1.7.0: MIT**, clsx **2.1.1: MIT**, class-variance-authority **0.7.1: Apache-2.0**, tailwind-merge **3.7.0: MIT**: paket metadata/lisans dosyaları incelendi; envanter sürüm bazında kaydedilir.
- Tailwind CSS ve @tailwindcss/postcss **4.3.3: MIT**; tw-animate-css **1.4.0: MIT**.
- lucide-react **1.52.0: ISC**; resmî açık kaynak ikonlar; paket içindeki üçüncü taraf bildirimleri korunur.
- Tailwind build bağımlılığı lightningcss **1.32.0: MPL-2.0**: paketteki LICENSE ve [resmî lisans](https://github.com/parcel-bundler/lightningcss/blob/master/LICENSE) incelendi. Bilinmeyen lisans olarak ilk kontrolde durduruldu; doğrulamadan sonra MPL-2.0 kontrol listesine eklendi. Kaynak dosyaları değiştirilmedi; dağıtım bildirimleri korunur.

Bu sürümlerin açık kaynak lisansları için bir Pro aboneliği eklenmedi. Gelecekte üçüncü taraf temalar/servisler veya sürüm değişiklikleri aynı şartları otomatik taşıyor varsayılmaz; lisans kontrolü her değişiklikte tekrar edilir.

## 03 — Ortak yönetim modeli

Yeni paket, paket sürümü, ücretli ürün veya harici görsel/font eklenmedi. Mevcut shadcn/ui + TanStack Table kaynakları genişletildi; LICENSE/NOTICE korunur. Restore sonrası `python3 scripts/check-licenses.py` **262 npm/NuGet paket sürümünü** doğruladı ve envanteri güncelledi. Bu metadata sonucu, yukarıdaki container/dağıtım ve ticari araç incelemelerinin yerine geçmez.

İlişkisiz yönetim alanında tür düzenleme turunda da paket/sürüm/harici asset değişmedi. Restore sonrası aynı kontrol **262 paket sürümü** için yeniden geçti; yeni lisans istisnası eklenmedi.

2026-10-08 / 05 aidat turu: npm manifest/lockfile ve NuGet paket sürümleri değiştirilmedi; yeni bağımlılık/ücretli asset yok. Restore sonrası metadata kontrolü **312 paket sürümü** için geçti. UI mevcut lisanslı shadcn/TanStack/Radix/Lucide kaynaklarını kullandı; yerel LICENSE/NOTICE korundu. Bu tur container imajları yeniden build edilmedi; dağıtım yükümlülükleri yukarıdaki kapsamda devam eder.

## İŞ 08 — MinIO .NET SDK

Minio NuGet **7.0.0** eklendi. SDK Apache-2.0: [sürüme ait resmî lisans](https://github.com/minio/minio-dotnet/blob/7.0.0/LICENSE); tam metin [minio-dotnet-7.0.0-LICENSE.txt](licenses/minio-dotnet-7.0.0-LICENSE.txt) içinde korunur. Bu, mevcut MinIO sunucusunun AGPLv3 lisansını değiştirmez; container dağıtım yükümlülükleri yukarıdaki sunucu kaydıyla birlikte değerlendirilir. Yeni transitif bağımlılıklar lisans envanterinde doğrulandı; FE paketleri değişmedi.

## İŞ 09 — RabbitMQ .NET istemcisi

RabbitMQ.Client NuGet **7.2.1** eklendi. Paket `Apache-2.0 OR MPL-2.0` çift lisanslıdır; bu proje **Apache-2.0** seçeneğini kullanır. Tam metin [rabbitmq-client-7.2.1-LICENSE-APACHE2.txt](licenses/rabbitmq-client-7.2.1-LICENSE-APACHE2.txt) içinde korunur. Transitif System.Threading.RateLimiting 8.0.0 MIT'tir. RabbitMQ 4.1.4 sunucusunun MPL-2.0 kaydı değişmez. 11.10.2026 restore sonrası metadata kontrolü **318 paket sürümü** için geçti; FE paketleri değişmedi.

## İŞ10 — 11.10.2026

OpenTelemetry.Extensions.Hosting ve Exporter.OpenTelemetryProtocol **1.18.0 Apache-2.0**; resmî NuGet repository commit9db92a4e978b4ae432183c790259be48a475578f ve paket LICENSE/THIRD-PARTY-NOTICES okundu, yerel kopyalar korundu. Restore sonrası check-licenses.py **331** npm/NuGet sürümü için geçti. FE manifest/lockfile değişmedi.

Yeni sabit container sürümleri: [Collector contrib0.162.0 Apache-2.0](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/v0.162.0/LICENSE), [Prometheus3.15.0 Apache-2.0](https://github.com/prometheus/prometheus/blob/v3.15.0/LICENSE), [Blackbox exporter0.28.0 Apache-2.0](https://github.com/prometheus/blackbox_exporter/blob/v0.28.0/LICENSE), [Loki3.7.8 AGPL-3.0](https://github.com/grafana/loki/blob/v3.7.8/LICENSE). Resmî sürüm lisansları docs/licenses/phase10-*-LICENSE.txt'te. Loki ayrı servis; AGPL source/dağıtım ve değişikliklerin ağ kullanımı yükümlülükleri korunur; ticari lisans alınmış gibi gösterilmez. Container içindeki üçüncü taraf bildirimleri kaldırılmaz. Önceki Grafana AGPL politikası korunur. Dashboard/config özgün repo kodudur; ücretli template veya yeni chart/font paketi yok.

## İŞ11 CI yardımcıları

Yeni npm/NuGet/container bağımlılığı yok. GitHub tarafından yayımlanan actions/checkout, actions/setup-node, actions/setup-dotnet MIT lisanslıdır; CI yetkisi contents:read, sır/dump cache veya artifact yüklemesi yok. Resmî lisanslar: https://github.com/actions/checkout/blob/main/LICENSE, https://github.com/actions/setup-node/blob/main/LICENSE, https://github.com/actions/setup-dotnet/blob/main/LICENSE. Action'lar commit SHA ile sabitlenir: checkout v4.4.0, setup-node v4.4.0, setup-dotnet v4.3.1 (etiketler `git ls-remote` ile doğrulandı). Workflow geliştirmesi yerel kalite akışıyla doğrulanır; GitHub üzerinde çalıştırılmış sayılmaz.

Önceki İŞ11 incelemesinde resmî kaynaklardan alınan telif/lisans metinleri korunur: [checkout](licenses/phase11-checkout-LICENSE.txt), [setup-node](licenses/phase11-setup-node-LICENSE.txt), [setup-dotnet](licenses/phase11-setup-dotnet-LICENSE.txt). 11.10.2026 apartman/daire FE eşliği yeni paket, font, ikon veya dış görsel eklemedi; envanter **331** sürüm. Ekran kanıtları yerel uygulama görüntüleridir.

11.10.2026 Daire/kişi bağlantıları ve tablet bakımında yeni paket/asset/font eklenmedi. Mevcut shadcn/ui + TanStack/Next.js/Lucide kullanıldı; npm/NuGet metadata kontrolü331 geçti, manifest/lock aynı. Ekran kanıtları yerel uygulama kabul görüntüleridir.

## Yerel ortak PostgreSQL 18.6 (12.10.2026)

Yerel veriler bağımsız `../postgres-18.6` projesindeki `projects` veritabanına
taşındı; uygulama `buildingflow`, kimlik `buildingflow_keycloak` şemasındadır.
Yerel ortak yedek `bash ../postgres-18.6/scripts/backup.sh` ile alınır.
Eski proje bazlı PostgreSQL yedeği/restore yöntemi ortak DB üzerinde uygulanmaz.
Nesne deposu MinIO ayrıca yedeklenir. Ortak README, kesim/geri dönüş/izolasyon
kanıtının kaynağıdır. PostgreSQL18.6 ve pgvector0.8.7 PostgreSQL License kullanır;
uygulama paket/lock bağımlılıkları bu düzenlemede değişmedi.

## Ortak Keycloak yerel bakım teslimi — 13.10.2026

Tek `keycloak-26.7.5` sunucusu ve `projects.keycloak` şeması; üç realm ve
özgün kullanıcı/istemci/rol kimlikleri korunur. Ortak yönetim localhost8080;
TraceFlow mobil issuer güncel LAN192.168.2.95:8080 üzerinden.
Eski Keycloak konteynerleri/şemaları geri dönüş için korunur. Uygulama
şemaları değişmez. Kaynak ve ayrıntılı operasyon/test kanıtı:
`../keycloak-26.7.5/README.md` (AkaAI kökünden ../../keycloak-26.7.5).
LDAP/federationID, DB rol izolasyonu, üç API için kendi token kabulü ve
iki diğer realm token retleri; özel reset SPI anonim401/kendi204/diğer403.
BuildingFlow17gerçek auth kontrolü, AkaAI10yerel/token testi,35frontend
auth/güvenlik testi ve TypeScript geçti. Tam AkaAI frontend166/167;
mevcut İngilizce compact sayı testinde1.3M/1.3m farkı kaydedildi.
Test fixture'ları temizlendi. Domain/roadmap ve on-prem AkaAI değişmedi.

## 14.10.2026 depolama refactoru
Aktif MinIO sunucusu ve Minio7.0.0 NuGet bağımlılığı kaldırıldı. Tarihsel İŞ08 lisans ve test kayıtları geçmiş kanıt olarak korunur; yeni runtime bunları kullanmaz.
SeaweedFS4.48: Apache-2.0; resmi digest4e61d15... sabit. Tam metin seaweedfs-4.48-LICENSE.txt. Resmi AWSSDK.S3 4.0.104.3 ve transitif AWSSDK.Core: paket .nuspec Apache-2.0. Tam LICENSE ve NOTICE aws-sdk-Apache-2.0.txt / aws-sdk-NOTICE.txt ile API ve worker imajlarına kopyalanır. Yeni kod AWS bulutuna bağlanmaz; yalnız configured SeaweedFS gateway kullanılır. Paket metadata envanteri329, belirsiz0. Bu kayıt tüm container işletim sistemi paketlerinin aynı lisans olduğu veya hukuki uyumluluk garantisi anlamına gelmez.

## Mobil Faz15 — 15.10.2026

Resmi expo-splash-screen57.0.10 SDK57 expo install ile eklendi:MIT. Upstream https://github.com/expo/expo/blob/main/LICENSE ve kurulu LICENSE okundu; mobile/buildingflow-mobile/LICENSES/expo-splash-screen.txt içinde korundu. Başka paket sürümü değişmedi. Tam mobil lock envanteri871kayıt; mobile/docs/mobile/DEPENDENCIES.tsv ve release:licenses kontrolü. Tamifadeler korunur; MITAND Apache herikisi,MITORApache/MITORCC0 MITseçeneği, BSD3OR GPL BSD3seçeneğiyle incelenir. MPL/CC-BY/source/NOTICE ve container dağıtımı metadata onayından ayrıdır; paket lisansları korunur. Merkez329npm/NuGet kontrolü mobil envanteri kapsamıyordu; iki sonuç ayrıdır.29auditbulgusu ve gerçek üretim/mağaza kabulü açık.

## PERSON FAZ10 —18.10.2026 doğrulama
Yeni bağımlılık/sürüm/asset kurulmadı. Mevcut metadata checker329npm/NuGet sürüm kaydı ve mobil checker871lock kaydı için geçti. Mevcut AND/OR seçimleri ve LICENSE/NOTICE kaynakları korundu; yeni exception yok. Dockerfile lisans kopyalama kaynakları ve compose image listesi okundu; son native binary/container OS SBOM/dağıtım bildirimleri/ticari kullanım hakkı kabulü yapılmadı. Metadata sonucu tam lisans veya güvenlik onayı değildir. Mobil SCA29bulgu(18high/11moderate) ile başarısız; paket değişmeden kaldı. Kanıt ve açık dağıtım yükümlülükleri: [FAZ10 raporu](refactor/person/phase-10-report.md).

## WEBSITE FAZ 04 — 18.10.2026

Yeni üçüncü taraf paket/font/ikon/stok görsel eklenmedi; manifest/lock sürümleri korunur. Projeye özel SVG ikon/logo kodu ve built-in image_gen ile iki temsili görsel üretildi. WebP kaynak/prompt kaydı: [website görselleri](../website/public/images/README.md). Mevcut Next.js/React/Sharp lisans ve NOTICE kaynakları korunur; yeni npm kurulumu/restore yapılmadı, envanter yeniden yazılmadı. Bu kayıt gerçek binanın görsel kullanım hakkı veya üretim dağıtımı kabulü değildir.

## WEBSITE FAZ05 —18.10.2026

Yeni paket/font/ikon/stock görsel eklenmedi. JPEG mevcut ProfilePhoto kodunu parametrik sınırlarla kullanır; PNG CRC/zlib kodu .NET BCL ve projeye ait kaynakla yazıldı. FE mevcut shadcn/TanStack/Lucide kullanır. Manifest/lock/NOTICE korunur; npm ci/restore yapılmadı. Medya hak/kişisel veri izni beyanla saklanır; otomatik hukuki uygunluk garantisi değildir.

## WEBSITE FAZ09 —19.10.2026

Yeni üçüncü taraf paket/font/ikon/görsel veya yeni temel imaj ailesi eklenmedi; mevcut node:24-alpine ve frontend lock kullanıldı. Docker npmci sonrasında mevcut check-licenses.py329npm/NuGet metadata için PASS; envanter içerik farkı yok. CycloneDX1.6 mevcut frontend lock230npm kaydı içerir; development/platform-optional dahil, OS/NuGet/binary SBOM değildir. Website imajı mevcut LICENSES/shadcn LICENSE/NOTICE, görsel kaynak kaydı, npm SBOM ve kurulu paketlerin148kök license/notice/copying/copyright kaydını taşır. 7pakette kök bildirim dosyası ayrıca incelenmelidir; eksik metin uydurulmadı. Metadata incelemesi bu dağıtım/ticari/OS koşullarını kapatmaz; [#30](https://github.com/AkaBayraK/BuildingFlow/issues/30) açık kabul görevidir. Yeni tarayıcı audit aracı/Lighthouse/ücretli kaynak kurulmadı.


## 20.10.2026 Dashboard fotoğraflı banner

Yeni üçüncü taraf paket/font/görsel yok. Varsayılan `frontend/buildingflow-web/public/images/dashboard-residence.webp`,bu proje için18.10.2026 üretilmiş `website/public/images/residence.webp` dosyasının byteeş kopyasıdır;[özgün prompt/kaynak](../website/public/images/README.md) korunur. Bu turimagegen çalıştırılmadı. Kullanıcıfotoğrafları mevcut özelbelgeupload akışındadır. Manifest/lock ve lisans/NOTICEkorundu;restore/kurulum yok,envanter yeniden üretilmedi. Bu kayıt mevcutcontainer/OSdağıtım yükümlülüklerinin yerine geçmez.

## 21.10.2026 — Kullanıcı onaylı MET Norway hava verisi

Kullanıcı MET Norway bağlantısını lisans koşulları korunarak açıkça onayladı. Yalnız Locationforecast 2.0 **verisi** kullanılır; seçilen veri lisansı **CC BY 4.0 International**. Ticari kullanım bu lisansın atıf/koşullarıyla mümkündür; API kullanım koşulları da ayrıca uygulanır. [Resmî veri politikası](https://api.met.no/doc/License), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/), [API koşulları](https://api.met.no/doc/TermsOfService), [ücretsiz veri](https://www.met.no/en/free-meteorological-data) 21.10.2026 incelendi. Hizmet SLA garantisi vermez.

Uygulamada MET Norway kaynak bağlantısı ve CC BY4 bağlantısı görünür; seçilen zaman dilimi/tek ondalığa yuvarlama açıklaması eklenmiştir. Provider ikonları, fotoğrafları veya kodu kopyalanmaz: mevcut lucide ikonları ve platform HTTP istemcisi kullanılır. Yeni npm/NuGet paketi, font veya harici görsel yoktur. Paket envanteri bu nedenle değişmez; paket kurulumu/restore yapılmadı. Bu onay yalnız hava verisi bağlantısı içindir; genel “yeni lisans yükü yok” politikası kaldırılmadı.

API istekleri sunucudan sabit şehir merkezi koordinatlarına yapılır. BuildingFlow/1.0 uygulama kimliği kullanılır; Weather:ContactUrl yapılandırılırsa gerçek HTTPS iletişim bağlantısı User-Agent'a eklenebilir (hayali adres eklenmez). Expires/Last-Modified/If-Modified-Since, Retry-After ve şehir başına tek istek/cache korunur. Veri lisansı ile servis kullanım şartları birbirinin yerine geçirilmez. [Uygulama/kanıt](navigation-refactor/DASHBOARD_WEATHER.md).


## BF-VERIFY02 — 22.10.2026

Projeye ait ContactNoticeFactory Java kaynağı ve e-posta metinleri yazıldı; harici kaynak kopyalanmadı. Derleme mevcut Keycloak26.7.5 API jar'larını yalnız classpath'te kullanır; çıktı yalnız projenin sınıfları/service kaydıdır, üçüncü taraf jar içermez. Yeni JDK/paket/SDK/imaj/sürüm eklenmedi; mevcut JDK, Keycloak26.7.5 ve Mailpit1.27.10 kullanıldı. Tema LICENSE/NOTICE korundu; manifest/lock/project farkı yok. Restore/install yapılmadığı için metadata envanteri yeniden üretilmedi. Yeni bir açık kaynak lisansı projeye atanmadı; üretim binary/NOTICE dağıtım denetimi FAZ07 kapsamındadır.
