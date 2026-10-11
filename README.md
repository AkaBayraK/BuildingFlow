# BuildingFlow

Site ve apartman yönetimi için **modular monolith** projesi. EF Core persistence, JWT doğrulama, Keycloak giriş/çıkış/şifre yenileme, worker host ve Next.js yönetim kabuğu; 02 yetkili oluşturma/listeleme, 03 ortak Property/Building/Unit CRUD, 04 kişiler ve tarihli malik/kiracı/sakin ilişkileri, 05 aidat ve borçlandırma, 06 manuel tahsilat ve borç kapatma tamamlandı.

Ana mimari dokümanı: [BUILDINGFLOW_PROJECT_CORE.md](docs/BUILDINGFLOW_PROJECT_CORE.md). İlk iterasyon sınırları: [CODEX_START_PROMPT.md](docs/CODEX_START_PROMPT.md). Uygulama kararları ve ertelenen işler: [decisions.md](docs/architecture/decisions.md).

Next.js kodlama standartları: [Frontend Convention](docs/frontend-rules.md). Kök ve frontend `AGENTS.md` bu belgeyi frontend değişikliklerinden önce zorunlu okuma olarak bağlar. Dil/TR-EN, UI/CRUD/form, API/yetki, tema/bildirim, state/route, formatlama ve test kuralları burada; kurulmuş özelliklerle planlı altyapı ayrı kayıtlıdır.

Veritabanı ilk kurulum, alan ekleme/değiştirme ve sürüm yükseltme rehberi: [MIGRATIONS.md](docs/MIGRATIONS.md). Liquibase karşılığı **EF Core Migrations**; C# migration dosyaları ve snapshot Git'te, uygulanan sürümler `__EFMigrationsHistory` tablosundadır. Proje kökünde `bash scripts/db.sh list`, `status`, `check`, `add MigrationAdi`, `sql` veya `update` kullanılır.

## Yerelde gereken uygulamalar

**Her şeyi Docker ile çalıştırmak için:**

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) açık olmalı. Docker Compose beraber gelir.
- Terminal ve tarayıcı. macOS/Linux başlangıç scriptleri Bash ve OpenSSL kullanır; macOS'ta mevcuttur.

PostgreSQL, Keycloak, Redis, RabbitMQ, MinIO veya Grafana'yı bilgisayara ayrıca kurmanız gerekmez; Compose başlatır. .NET ve Node.js de Docker ile çalıştırırken host makinede gerekmez.

**Kod geliştirmek veya testleri host makinede çalıştırmak için ek olarak:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- [Node.js](https://nodejs.org/) 24 LTS ve npm (Docker imajı Node 24 kullanır).
- Bir editör: VS Code + C# Dev Kit veya Rider; Git.

## Tek komutla başlatma

Proje klasöründe:

```bash
bash scripts/start.sh
```

İlk çalıştırmada `.env.example` dosyasından `.env` oluşturulur ve her servis için ayrı rastgele parola üretilir. Mevcut `.env` korunur. Ardından imajlar derlenir, altyapı başlar ve **migration servisi şemayı uygular**. Migration başarıyla tamamlandıktan sonra API başlar.

İlk indirme/build internet bağlantısı gerektirir. Başlatma sonrası servisleri kontrol edin:

```bash
docker compose --env-file .env -f deploy/docker-compose.yml ps
bash scripts/smoke.sh
```

`buildingflow-migrations` servisinin `Exited (0)` olması normaldir: tek seferlik şema uygulayıcısıdır. Diğer servisler çalışıyor olmalıdır. Keycloak ilk açılışta birkaç saniye sonra hazır olur; smoke kontrolü erken yapılırsa tekrar çalıştırın.

| Uygulama | Yerel adres / port |
| --- | --- |
| BuildingFlow Web | http://localhost:3003 |
| API readiness | http://localhost:5005/health |
| API liveness | http://localhost:5005/health/live |
| OpenAPI JSON | http://localhost:5005/openapi/v1.json |
| Keycloak | http://localhost:8080/admin/ |
| RabbitMQ UI | http://localhost:15672 |
| MinIO Console | http://127.0.0.1:9001 |
| MinIO API | http://127.0.0.1:9000 |
| Grafana | http://localhost:3002 |
| Yerel e-posta kutusu (Mailpit) | http://localhost:8025 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6389 |

Portlar `.env` üzerinden değişir. 3000 başka bir uygulama, 5000 macOS Control Center tarafından kullanıldığı için frontend 3003 ve API 5005 seçildi. Bu bilgisayarda başka projeler 6379 ve 3001 kullandığı için Redis 6389, Grafana 3002 seçildi. **WEB_PORT değişirse** Keycloak realm dosyasındaki redirect URI ve web origin de aynı portla güncellenmelidir. Realm import yalnızca ilk oluşturulurken uygulanır. Mevcut web client portunu güncellemek için `bash scripts/update-keycloak-web-origin.sh` çalıştırın (bootstrap yönetici hesabı hâlâ aktif olmalıdır); giriş/şifre yenileme ayarları ve mevcut realm için `bash scripts/configure-identity.sh` kullanın.

Yönetim ekranlarının kullanıcı adı ve parolaları `.env` içindedir. Parolaları Git'e eklemeyin. Keycloak realm: `buildingflow`. Giriş/çıkış ve şifre yenileme hazır; geliştirme hesabı aşağıda açıklanıyor. API `/api/v1/status` geçerli JWT ister. Realm rollerinin tanımlanmış olması iş yetkilendirmesinin tamamlandığı anlamına gelmez.

## Durdurma ve loglar

```bash
bash scripts/stop.sh
# Veriler named volume'larda korunur.
docker compose --env-file .env -f deploy/docker-compose.yml logs -f buildingflow-api buildingflow-worker
```

`.env` parolalarını değiştirmek mevcut volume'lardaki kullanıcı parolalarını otomatik değiştirmez. Önce ilgili servisin yönetim aracıyla parolayı değiştirin.

## Docker altyapısı + host üzerinde geliştirme

```bash
bash scripts/init-local.sh
docker compose --env-file .env -f deploy/docker-compose.yml up -d postgres keycloak redis rabbitmq minio grafana mailpit
bash scripts/configure-identity.sh
```

Backend için proje kökünden yeni terminalde:

```bash
set -a
source .env
set +a
export ConnectionStrings__Database="Host=localhost;Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export Authentication__Authority="http://localhost:${KEYCLOAK_PORT}/realms/buildingflow"
export Authentication__Audience=buildingflow-api
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:${API_PORT}"
cd backend
dotnet tool restore
dotnet build BuildingFlow.sln
dotnet ef database update --project src/BuildingFlow.Infrastructure
dotnet run --project src/BuildingFlow.Api --no-launch-profile
```

Frontend için yeni terminalde:

```bash
cd frontend/buildingflow-web
npm ci
npm run dev
```

Frontend server tarafında API varsayılanı `http://localhost:5005`. Farklı API portu için `API_INTERNAL_URL` belirtin. OIDC ve port ayarları `scripts/dev.sh web` tarafından `.env` dosyasından okunur. Varsayılan ayarlarda frontend klasöründen `npm run dev` yeterlidir.

Worker'ı host üzerinde açmak için:

```bash
cd backend
RabbitMQ__Host=localhost dotnet run --project src/BuildingFlow.Worker
```

Worker cancellable idle host olarak çalışır; henüz RabbitMQ mesajı tüketmez veya mail/SMS göndermez.

## Testler

```bash
cd backend
dotnet test BuildingFlow.sln
```

Bu komut domain ve mapping testlerini çalıştırır. `BUILDINGFLOW_TEST_DATABASE` verilmediyse gerçek PostgreSQL testi **atlanır**; bunu tam entegrasyon doğrulaması olarak değerlendirmeyin.

Ayrı `buildingflow_tests` PostgreSQL veritabanını oluşturarak tüm testleri çalıştırmak için proje kökünden:

```bash
bash scripts/test.sh
```

Test şeması migration ile uygulanır. Soft-delete filtresi, audit alanları, optimistic concurrency ve case-insensitive unique constraint gerçek PostgreSQL üzerinde doğrulanır. Testler uygulama veritabanını silmez.

Frontend doğrulaması:

```bash
cd frontend/buildingflow-web
npm run typecheck
npm run build
```

## Mimari ve mevcut kapsam

```text
frontend/buildingflow-web   Next.js, shadcn/ui + TanStack yönetim tabloları, OIDC ve sağlık durumu
backend/src
  BuildingFlow.Domain         BaseEntity, Property, Building, Unit, PropertyMembership; dış bağımlılık yok
  BuildingFlow.Application    Yönetim CRUD/DTO/validasyon, current user, storage/messaging sözleşmeleri
  BuildingFlow.Infrastructure EF Core/Npgsql, Fluent API, audit interceptor, migrations
  BuildingFlow.Api            JWT/rol/üyelik kapsamı, yönetim endpointleri, ProblemDetails ve health
  BuildingFlow.Worker         BackgroundService başlangıç host'u
backend/tests                 Domain, kimlik/CORS, yönetim API ve gerçek PostgreSQL testleri
deploy                        Docker Compose ve Keycloak realm tanımı
```

`BaseEntity` alanları: `Id`, `Version`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `IsDeleted`, `IsEnabled`. Audit ve version `SaveChangesInterceptor` içinde uygulanır. Silme fiziksel DELETE yerine soft delete'e dönüşür. FK delete davranışı Restrict'tir. Çocuk koleksiyonlar read-only; domain ve PostgreSQL unique index'leri tekrarları önler.

01–06 tamamlandı. Ortak Property → opsiyonel Building → Unit modeli; apartmanda doğrudan daire, diğer türlerde bina/blok ve bölümler. Yönetim alanına özel kişiler ve tarihli malik/kiracı/sakin ilişkileri var. Rol/üyelik, version ve soft-delete backend tarafından korunur. Aidat/borçlandırma ve manuel tahsilat/borç kapatma uygulanmıştır; online ödeme sağlayıcısı, notification consumer ve storage provider sonraki kapsamda planlı.

Grafana görselleştirme katmanıdır; şu anda datasource/dashboard yüklenmez. JSON loglar stdout'a yazılır, ASP.NET Core yerleşik request metrics üretir. Loki, Prometheus ve OpenTelemetry exporter bağlantıları sonraki aşamadadır. `/health` PostgreSQL bağlantısını ölçer; bütün altyapının sağlık garantisi değildir.

## VS Code görevleri ile çalıştırma

VS Code'da **File → Open Folder** ile bu projenin kök klasörünü (`BuldingFlow`) açın. `.NET 10 SDK`, `Node.js 24` ve Docker Desktop kurulu olmalı. SDK/Node kurulumu sonrası VS Code'u yeniden açın. C# Dev Kit uzantısı önerilir.

1. **Terminal → Run Task → BuildingFlow: Prepare local development**: Docker'daki FE/BE'yi durdurur, altyapıyı başlatır, backend build/migration ve `npm ci` çalıştırır. İlk kullanımda veya bağımlılıklar/migration değiştiğinde çalıştırın.
2. Hazırlık başarıyla bitince **Terminal → Run Task → BuildingFlow: FE + BE**: FE ve BE'yi iki ayrı VS Code terminalinde host makinede çalıştırır.
3. [http://localhost:3003](http://localhost:3003) adresini açın. API: [http://localhost:5005/health](http://localhost:5005/health).

`BuildingFlow: FE`, `BuildingFlow: BE`, `BuildingFlow: Worker` ve `BuildingFlow: Infrastructure only` görevleri de ayrı kullanılabilir. Bunlar çalıştırma görevleridir; managed debugger yapılandırması içermez. Port ve bağlantı bilgileri `.env` üzerinden okunur, parolalar görev dosyalarında tutulmaz. Scriptler macOS/Linux içindir.

Host süreçlerini durdurmak için ilgili terminalde **Ctrl+C** veya **Terminal → Terminate Task** kullanın. Backend terminali erişilemiyorsa proje kökünde `bash scripts/dev.sh api-stop` veya VS Code **BuildingFlow: Stop BE** görevi yalnızca bu checkout’a ait host API’yi durdurur. `api` komutu port doluysa başlamadan Türkçe uyarı verir; başka uygulama/Docker süreci otomatik kapatılmaz. `scripts/stop.sh` yalnızca Docker servislerini durdurur; VS Code terminalindeki FE/BE süreçlerini durdurmaz.

Aynı komutların terminal karşılıkları (proje kökünden):

```bash
bash scripts/dev.sh prepare
# İki ayrı terminal:
bash scripts/dev.sh api
bash scripts/dev.sh web
```

Frontend klasöründe yalnızca `npm run dev` çalıştırmanız yeterlidir. Varsayılan port **3003**. macOS'ta Next.js hazır olduğunda Chrome/Safari'deki açık BuildingFlow sekmesi öne getirilir; aynı OIDC client ve dönüş adresiyle Keycloak giriş ekranındaki sekme de kullanılır. Mevcut URL, form ve giriş işlemi korunur; zorla yenilenmez. Eşleşen sekme yoksa varsayılan tarayıcı açılır. macOS Terminal/VS Code için tarayıcı otomasyon izni isteyebilir; izin reddi veya kontrol hatasında kopya sekme açılmaz, terminalde elle açılacak URL gösterilir. Bu kontrol Chrome/Safari içindir; Codex iç tarayıcısı ve diğer tarayıcılar bu tespit kapsamına girmez. Windows/Linux mevcut OS açıcı davranışını kullanır.

Portu `WEB_PORT` veya `npm run dev -- --port 3010` ile değiştirebilirsiniz. Tarayıcı kontrolü/açılmasını tamamen kapatmak için `BUILDINGFLOW_OPEN_BROWSER=false npm run dev`; özellikle yeni sekme açmak için `BUILDINGFLOW_OPEN_BROWSER=always npm run dev` kullanın. FE durup tekrar başladığında mevcut sekmede bağlantı hatası kalırsa aynı sekmeyi yenileyin. Dev başlatıcısı değişikliği bir sonraki `npm run dev` çalıştırmasında geçerli olur.

## 01 — Giriş ve şifre yenileme

Geliştirme hesabı **buildingflow-dev**, e-posta **dev@buildingflow.local**. Rastgele şifre proje kökündeki Git dışında tutulan `.env` dosyasının **BUILDINGFLOW_DEV_PASSWORD** alanındadır. Keycloak yönetici parolası uygulama kullanıcısının parolası değildir. Mevcut kullanıcı şifresi yeniden hazırlıkta korunur; kullanıcı reset ile şifreyi değiştirirse `.env` artık eski başlangıç şifresini içerir.

1. Docker Desktop'ı açın; proje kökünde `bash scripts/dev.sh infrastructure` çalıştırın.
2. İki ayrı terminalde `bash scripts/dev.sh api` ve `bash scripts/dev.sh web` çalıştırın. Frontend için `cd frontend/buildingflow-web` ardından `npm run dev` de kullanılabilir.
3. [BuildingFlow](http://localhost:3003/) girişe yönlendirir. Hesap bilgileriyle giriş yapın.
4. **Şifremi unuttum** → `dev@buildingflow.local` → Gönder. [Mailpit](http://localhost:8025/) içinde gelen maili açıp sıfırlama bağlantısına tıklayın. Yeni şifre ve tekrarını girin. En az 12 karakter, büyük/küçük harf ve rakam gerekir.

**Yerel mail gerçek Gmail/Outlook posta kutusuna gönderilmez; Mailpit'e düşer.** Gerçek teslim için Keycloak → Realm settings → Email altında gerçek SMTP hesabı ve TLS ayarları yapılandırılır; mevcut local SMTP ayarı yayın için kullanılmaz.

Çalışan eski BE terminalini **Ctrl+C** ile durdurup `bash scripts/dev.sh api` ile yeniden başlatın; yeni CORS kodu başlangıçta yüklenir. Frontend HMR değişiklikleri alır; sorun halinde FE terminalini de yeniden başlatın.

Test ve gelecekteki kontrol listesi: [TESTING.md](docs/TESTING.md). Agent/skill durumu: [SKILLS_AND_AGENTS.md](docs/SKILLS_AND_AGENTS.md). Kalıcı kodlama kuralları: [AGENTS.md](AGENTS.md). Lisans envanteri/politikası: [licenses.md](docs/licenses.md).

```bash
# Proje kökünden; API ve Keycloak hazır olmalı:
bash scripts/test-auth.sh
python3 scripts/check-licenses.py
```

01–06 tamamlandı. [Property/Building/Unit API sözleşmesi](docs/api/hierarchy.md) ve [kişi/ilişki sözleşmesi](docs/api/people.md).

## 02 — Oluşturma ve listeleme temeli

[Giriş sonrası Yönetim Alanları](http://localhost:3003/properties) ekranında **Yeni yönetim alanı** ile tür/ad/şehir/ilçe/adres kaydedilir. Liste sunucuda arama/sıralama/sayfalama yapar; **Görünüm** adres ve tarih sütunlarını açıp kapatır. Ada tıklayınca detay açılır. Sayfayı yenilemek mevcut detay adresini korur. 02'deki site akışı 03'te bu ortak modele taşındı; eski `/sites` tarayıcı bağlantıları yönlenir.

Oluşturma yalnızca **SUPER_ADMIN** yetkisidir. SUPER_ADMIN tüm silinmemiş yönetim alanlarını; diğer kullanıcılar yalnızca aktif PropertyMembership kayıtlarını görür. Oluşturana üyelik otomatik eklenir. Üyelik atama ekranı/API'si bu fazda yok; global SITE_ADMIN rolü tek başına veri erişimi sağlamaz.

Yeni migration veya paketlerle mevcut checkout'u hazırlamak için kökten `bash scripts/dev.sh prepare`, ardından iki terminalde `bash scripts/dev.sh api` ve `bash scripts/dev.sh web` kullanın. Frontend'de yeni paket/PostCSS kurulumu sonrası çalışan eski `npm run dev` sürecini yeniden başlatın.

UI kaynakları **shadcn/ui (MIT)** ve **TanStack Table 8.21.3 (MIT)**; telif/lisans dosyaları korunur. Detaylı bağımlılık ve dağıtım şartları [licenses.md](docs/licenses.md) içinde.

UI Aşama 06 ile Daireler ve Kişiler ekranları yenilendi: kompakt tablo, mobil kart, gerçek sayılar, arama/sıralama/sayfalama ve sağ detay/düzenleme paneli. `http://localhost:3003/units` veya `/people` üzerinden alan seçin; mevcut `npm run dev` komutu ve port 3003 kullanılır. Kişi detayında gerçek malik/kiracı/sakin ilişkileri görünür. [Uygulama raporu](docs/analysis/UI_STAGE06_REPORT.md) ve [test kanıtları](docs/TESTING.md) mevcut API sınırlarını açıklar.


## 03 — Yönetim Alanları

[http://localhost:3003/properties](http://localhost:3003/properties) → **Yeni yönetim alanı**. İlk dört tür: Apartman, Site, Rezidans, Karma yapı. Apartman seçilirse isteğe bağlı kat sayısı girilir ve detay doğrudan **Daireler** açılır; blok oluşturulmaz. Diğer türlerde **Binalar / Bloklar** → **Bağımsız bölümler** akışı vardır. Daire/ofis/mağaza/villa/depo/atölye ortak Unit modelidir. Toplamlar girilmiş, silinmemiş gerçek kayıtlardan hesaplanır; pasif kayıtlar dahildir.

Yönetim alanı oluşturma/düzenleme/silme SUPER_ADMIN; bina/blok ve bölüm yazma SUPER_ADMIN/SITE_ADMIN/MANAGER, global olmayan kullanıcıda aktif üyelik kapsamındadır. Düzenleme ve silme version gerektirir. Dolu üst kayıt silinemez; önce alt kayıtlar silinir. Pasif üst kayıtta alt kayıt değiştirilemez. 409 halinde güncel kaydı yükleyin. Tür, bina/blok veya bağımsız bölüm kaydı/geçmişi yoksa düzenleme ekranından değiştirilebilir; pasif ve silinmiş alt kayıtlar da tür değişimini engeller; etap ve ayrı kat tabloları sonraki kapsamdır.

`UnifiedPropertyModel` migration eski tabloları rename/backfill eder; uygulanmış önceki migration'lar değişmez. `bash scripts/db.sh update` yerel güncelleme; `.env`/parola/veri korunur. Yeni tablolar: properties, buildings, units, property_memberships. Her birinde sekiz BaseEntity kolonu zorunludur. Model kaydı: [ana mimari](docs/BUILDINGFLOW_PROJECT_CORE.md), [migration rehberi](docs/MIGRATIONS.md), [test kanıtları](docs/TESTING.md).

MinIO API için **127.0.0.1:9000** kullanın. Docker IPv4 loopback'e yayın yapar; aynı bilgisayardaki Java uygulaması IPv6 9000 dinlerse localhost başka servise gidebilir. Smoke kontrolü bu yüzden MinIO'yu IPv4 adresinde kontrol eder.


## 04 — Kişiler, malik/kiracı/sakin ilişkileri

**Soldaki Kişiler menüsü → yönetim alanı seçimi** veya **Yönetim Alanları → alan seçimi → Kişiler**. Önce kişi ekleyin; **Daire ilişkileri (ayrı kişi sayfası) → Daireye bağla** ile mevcut daire/bölüm, Malik/Kiracı/Sakin ve tarihleri seçin. Varsayılan ilişki Malik; bir daireye birden fazla malik bağlanabilir. Apartmanda doğrudan daireler, diğer türlerde bina/blok içindeki bölümler kullanılabilir.

Ad/soyad zorunlu, telefon/e-posta isteğe bağlıdır. TC kimlik numarası alınmaz; kişi eklemek giriş hesabı oluşturmaz. Kişisel veri yalnızca SUPER_ADMIN/SITE_ADMIN/MANAGER ve backend üyelik kapsamına açıktır. Bitiş günü ilişkinin son geçerli günüdür; geçmiş silinmez. Aynı kişi/bölüm/rol ve birincil iletişim tarih çakışması engellenir. Devam eden/gelecekte biten ilişki varken kişi/daire silinemez veya pasif yapılamaz.

Kurulumda yeni migration uygulanır: proje kökünde bash scripts/db.sh update; Docker akışında buildingflow-migrations servisi. [API sözleşmesi](docs/api/people.md), [migration kuralları](docs/MIGRATIONS.md) ve [test kanıtları](docs/TESTING.md). 04 fazının teslim sonucu: **171 .NET + 3 frontend test**, **17 Keycloak kontrolü**; typecheck/build, servis smoke ve 262 lisans metadata kontrolü geçti.

Frontend saf dönüş rotası/tarih testleri (TS import destekli Node 24+):

    cd frontend/buildingflow-web
    npm test

TR/EN ve ortak ikonlu mesajlar uygulanmıştır. RHF/Zod/Query ve dark tema henüz kurulu/uygulanmış değildir; ortak standartların mevcut/planlı durumu [Frontend Convention](docs/frontend-rules.md) içinde.

## Ayrı menüler, TR/EN ve ikonlu mesajlar

Yerel arayüz: **http://localhost:3003/properties**. **Daireler / Bağımsız Bölümler** ve yetkili kullanıcı için **Kişiler** menüleri her zaman görünür. Alan seçili değilken `/units` veya `/people` üzerinden erişebildiğiniz alanı seçin; ilgili modül doğrudan açılır. Seçili alanın sol menüsünde **Genel Bilgiler**, **Binalar / Bloklar**, **Daireler / Bağımsız Bölümler**, **Kişiler** ayrı sayfalardır. Apartman türünde Binalar/Bloklar gösterilmez; Daireler doğrudan açılır. Site/Rezidans/Karma türünde bölüm sayfasında önce bina seçilir. Kişi ilişkisi listeden kendi sayfasına gider. Oluşturma/düzenleme/silme formları mevcut ortak dialog olarak kalır.

Üstte **Türkçe / English** seçici vardır; tercih yenileme ve çıkışta korunur. Başarı/hata/uyarı/bilgi farklı ikon, başlık ve renkle sunulur. Alan hatası alan altında kalır, 409 güncel kayıt eylemini korur; ham sunucu hata/SQL mesajı gösterilmez.

Bu tur: **11 frontend testi**, typecheck/build, **17 gerçek Keycloak kontrolü**, servis smoke, frontend Docker imajı derleme ve **312 paket sürümü lisans metadata kontrolü**. Backend şeması/migration değiştirilmedi; 171 backend sonucu önceki 04 tesliminindir. Kabul ve ekran kanıtları [TESTING.md](docs/TESTING.md) içinde. Sonraki ekranlar [Frontend Convention](docs/frontend-rules.md) ve AGENTS kurallarını izler.

## 05 — Aidat / Borçlar

Menüden **Aidat / Borçlar** → yönetim alanını seçin. **Yeni tahakkuk** ile aktif daireleri seçip dönem, bölüm başına TRY tutar, vade ve açıklama girin; toplamı kontrol ederek oluşturun. Borçları dönem/vade/durum ve bölüm bazında filtreleyin. Detaydan gerekçe ile iptal edebilirsiniz; geçmiş silinmez. Borç daireye aittir, malik/kiracıya otomatik atanmaz. Faz 06 Tahsilatlar menüsünde kısmi/tam ödeme kaydı ve geçmişi koruyan tersleme uygulanır.

Yerel mevcut kurulumda yeni şema için proje kökünde `bash scripts/db.sh update`; ardından API terminalinde `bash scripts/dev.sh api`. Frontend klasöründe `npm run dev` varsayılan 3003 portunda çalışır ve tarayıcıyı açar. Docker altyapısı açık olmalıdır. Güncel API: [aidat sözleşmesi](docs/api/dues.md); gerçek testler ve sınırlar: [TESTING.md](docs/TESTING.md).

## 06 — Tahsilatlar / borç kapatma

[http://localhost:3003/payments](http://localhost:3003/payments) → yönetim alanı → **Yeni tahsilat**. Daire/bölüm ve kalan bakiyeli borçları seçin; her borca dağıtılacak tutarı, alınan ödemenin tarihini, nakit/havale yöntemini ve açıklamayı girin. Havalede referans zorunludur. Toplam seçilen dağıtımdan hesaplanır; kısmi ödeme yapılabilir. Kaydı görüntüleyip gerekçeyle iptal etmek bakiye dağıtımını geri alır ve geçmişi saklar. Aidat detayında **Ödeme kaydet** ve tahsilat geçmişi de vardır. Önce aidat tahakkuku olmayan bölüm için borç oluşturulur; sistem örnek borç/ödeme eklemez.

Yeni tahsilat ve detay ortak sağ panelde açılır. İşlem alınmış paranın **uygulama içi kaydıdır**; gerçek banka transferi, online kart/POS, banka iadesi veya fiskal makbuz/PDF yoktur. [Tahsilat API sözleşmesi](docs/api/payments.md).

Yerelde şema/API güncellendi. Başka kurulumda proje kökünde `bash scripts/db.sh update`; API durdurmak için `bash scripts/dev.sh api-stop`, başlatmak için `bash scripts/dev.sh api`. Ayrı terminalde `cd frontend/buildingflow-web` ardından `npm run dev`: varsayılan **3003**, tarayıcı açılır. Docker altyapısı ve .NET 10 / Node.js mevcut hazırlıkta gereklidir; yeni uygulama/paket kurulmadı. Güncel doğrulamalar ve gerçek kapsam [TESTING.md](docs/TESTING.md).

Yeni tahakkukta **Tüm sonuçları seç (N)** bütün arama sonuçlarını seçer; **Bu sayfayı seç/çıkar**, **Seçilenleri temizle** ve bölüm etiketinden tek tek çıkarma hazırdır. En fazla 500 bölüm; toplam ve kaç bölüme kaydedileceği görünür. Filtre değişince diğer seçimler korunur; sadece formda seçim yapmak borç kaydetmez.

### 06.1 — Borç kişi geçmişi ve taşınma onayı

Aidat / Borçlar listesi ve sağ detay paneli yeni tahakkukların kayıtlı kiracı/sakin ve malik bilgilerini gösterir. Yeni tahakkukta Daire / Kiracı / Malik muhatabı seçilir; kişisel seçimde her bölüm için dönem başında tek ilişki gerekir. Kişi combo ve rol filtresi borç snapshot'larından gelir. Önceki borçlara bugünkü kişi ataması yapılmaz; geçmiş kaydedilmemiş olarak gösterilir. Muhasebecinin kişi yetkisi genişletilmez.

İptal nedenini listeden seçin; Diğer için açıklama girin. Kişiler → kişinin ilişkileri → İlişkiyi bitir ekranında gelecekteki uygun borçları inceleyin. **Onay kutusu başlangıçta kapalıdır.** İşaretlemezseniz yalnız ilişki biter, borçlar kalır. Onaylanan listedeki ID/version'lar için iptal ve bitiş birlikte kaydedilir; ödemeli, terslenmiş ödeme geçmişi olan, eski dönem veya malik borçları korunur. Tarih/version değişirse onay yeniden gerekir.

Yeni migration: `DuesPartyHistoryAndCancellationReasons`; `bash scripts/db.sh update`. Yerel FE `http://localhost:3003`, BE `http://localhost:5005`; mevcut VS Code görevleri ve çalıştırma komutları geçerli. Ek paket gerekmez. Kapsam ve doğrulama: [analiz](docs/analysis/PHASE_06_1_DUES_PARTIES.md), [testler](docs/TESTING.md), [API](docs/api/dues.md). Yol haritasında ayrı **06.1**; Faz 07 bu teslimin parçası değildir.

## İŞ 07 — Gider ve Bütçe

Sol menüde **Gider ve Bütçe** → yönetim alanını seçin. Giderleri kategori/tarih/tutar/açıklama ve isteğe bağlı belge referansıyla aynı sağ panelden kaydedin; arama/filtre/sayfalama, detay ve gerekçeli iptal kullanın. **Bütçe ve gerçekleşen** görünümünde aylık kategori planı ve revizyon geçmişi bulunur. Tahsilat−gider ve bütçe−gider gerçek aylık kayıtlardan hesaplanır; tahakkuk gelir sayılmaz. Tanımlanmış sıfır plan ile planı olmayan kategori ayrı gösterilir.

Yerel kurulumda proje kökünden `bash scripts/db.sh update` (yeni ExpensesAndBudgets migration), ardından ayrı terminallerde `bash scripts/dev.sh api` ve `bash scripts/dev.sh web`. `http://localhost:3003/expenses` alan seçimini açar; API `http://localhost:5005`. Mevcut `npm run dev` varsayılan port/sekme davranışı ve VS Code görevleri aynı şekilde geçerlidir. Bu teslimde yerel migration uygulandı ve API güncel kodla yeniden başlatıldı; frontend dev süreci korundu.

[API](docs/api/expenses.md), [testler ve sınırlar](docs/TESTING.md), [güncel ilerleme](docs/progress/STATUS.md). Dosya yükleme Faz 08, banka mutabakatı/ödeme emri bu fazın parçası değildir.

### Faz08 — Duyurular ve Belgeler

Menüler `http://localhost:3003/announcements` ve `http://localhost:3003/documents`; önce yönetim alanı seçin. API başlatma `bash scripts/dev.sh api`, FE `bash scripts/dev.sh web`. Mevcut yerel MinIO çalışmalıdır; `docker compose --env-file .env -f deploy/docker-compose.yml up -d minio`. Kullanıcı parolalarını değiştirmeden .env’den bağlantı alınır. Yerel MinIO endpoint127.0.0.1:9000, console127.0.0.1:9001. Migration `bash scripts/db.sh update`, ayrı gerçek PostgreSQL/MinIO testleri `bash scripts/test.sh`. Yayınlanan duyuru içerik ve ekleri korunur; e-posta/SMS gönderimi sonraki fazdır. [API sözleşmesi](docs/api/communication.md).

### Faz 10: Sayaçlar, raporlar ve gözlemlenebilirlik

`/meters` ve `/reports` menülerinden alan seçilir. Sayaç okuması ilk endeks, sonraki tüketim farkı ve gerekçeli düzeltme geçmişini tutar; otomatik borç üretmez. Raporlar dönem borcu, güncel bakiye, tahsilat/gider tarihleri ve ilişki bazlı doluluğu ayırır. [API ve kurallar](docs/api/meters-reports.md).

Grafana http://localhost:3002/d/buildingflow-operations; Prometheus19090, Loki13100, OTLP14318. [Çalıştırma ve doğrulama](docs/OBSERVABILITY.md). Yeni schema için `bash scripts/db.sh update`; eski migration dosyalarını değiştirmeyin. Bu teslimde yerel Faz09+10 migrations uygulanmıştır.

Yayın adayı (İŞ11): kalite kapısı `bash scripts/ci.sh`, üretim yapılandırma kontrolü `python3 scripts/release-preflight.py <env>`, yedek/geri dönüş provası `python3 scripts/test-backup-restore.py`. Rehberler: [RELEASE.md](docs/RELEASE.md), [BACKUP_RESTORE.md](docs/BACKUP_RESTORE.md).
