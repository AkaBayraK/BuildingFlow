# Sidebar bağımlılık ve lisans değişiklik kontrolü

20.10.2026 — SIDEBAR FAZ07.1. Politika: [AGENTS](../../AGENTS.md) ve [lisans kaynağı](../licenses.md). Kullanıcının16.10.2026 kararı gereği açık lisanslı olsa da yeni üçüncü taraf paket/font/ikon/görsel eklenmez.

**Sonuç: yeni doğrudan veya transitif bağımlılık, sürüm, font, ikon paketi, harici asset ya da ücretli template yok.** `git diff HEAD -- frontend/buildingflow-web/package.json frontend/buildingflow-web/package-lock.json` boş; bu tur başlangıç kaynak/manifest hashleri korunur. Yeni bağımlılık olmadığı için yeni transitif lisans yükü yoktur; mevcut tüm dağıtım yükümlülükleri devam eder. Npm ci/install/dotnet restore yapılmadı; envanter yazan check-licenses.py sırf belge teslimi için yeniden çalıştırılmadı.

## Yeniden kullanılan mevcut bileşenler

Bu sürümler yerel kurulu package.json ve lisans dosyalarıyla okundu; yeni paket seçimi veya sürüm yükseltmesi yoktur.

| Bileşen | Kurulu sürüm/lisans | Yerel kanıt |
|---|---|---|
| radix-ui |1.7.0 MIT|frontend/buildingflow-web/node_modules/radix-ui/LICENSE|
| lucide-react |1.52.0 ISC; türetilen Feather ikonları için ayrıca MIT bildirimi|frontend/buildingflow-web/node_modules/lucide-react/LICENSE (tam metin korunur)|
| Next.js |16.4.0 MIT|node_modules/next/license.md|
| React/ReactDOM |19.3.0 MIT|node_modules/react/LICENSE;react-dom/LICENSE|
| next-intl |4.14.9 MIT|node_modules/next-intl/LICENSE; [yerel atıf](../../frontend/buildingflow-web/LICENSES/next-intl.txt)|
| keycloak-js |26.2.4 Apache-2.0|node_modules/keycloak-js/LICENSE.txt|
| Mevcut shadcn/ui kaynakları |MIT|[LICENSE](../../frontend/buildingflow-web/src/components/ui/LICENSE.md), [NOTICE](../../frontend/buildingflow-web/src/components/ui/NOTICE.md)|

Tabloda kısaltılan node_modules yolları frontend/buildingflow-web altındadır. Lucide metadata'sındaki ISC ifadesi dosyadaki Feather MIT bildirimini ortadan kaldırmaz. Radix MIT telif/izin metni ve tüm paket LICENSE/NOTICE dosyaları dağıtımda korunur. Mevcut Dialog'a yalnız reduced-motion sınıfı eklendi; yeni registry kaynağı veya Pro bileşen alınmadı. Sidebar özgün React/CSS kodudur; test ekran görüntüleri projedeki sentetik fixture çıktılarıdır, üçüncü taraf görsel değildir.

## Envanter ve sınırlar

Mevcut [paket envanteri](../licenses/package-inventory.json) ve [lisans prosedürü](../licenses.md) ana kaynaktır. Bu kontrol **değişiklik ve bildirim koruma kontrolüdür**; tam container/OS/binary/SBOM ya da hukuki/ticari dağıtım uygunluk sertifikası değildir. Mevcut libvips/LGPL,lightningcss/MPL,caniuse-lite/CC-BY ve diğer dağıtım yükümlülükleri bu sidebar işiyle ortadan kalkmaz. Yeni bağımlılık gerekirse önce mevcut platform/projeye ait kod alternatifi değerlendirilir, kullanıcı onayı olmadan eklenmez.
