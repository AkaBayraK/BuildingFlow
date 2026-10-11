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
| MinIO | [AGPLv3](https://github.com/minio/minio/blob/master/LICENSE) | Ayrı servis; ticari alternatifleri aynı lisans varsayımıyla kullanma |
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
