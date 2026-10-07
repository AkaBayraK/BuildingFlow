# BuildingFlow

Site ve apartman yönetimi için **modular monolith** başlangıç projesi. İlk kapsam: Site → Block → Apartment domain modeli, EF Core persistence, JWT doğrulama temeli, worker host ve Next.js uygulama kabuğu.

Ana mimari dokümanı: [BUILDINGFLOW_PROJECT_CORE.md](docs/BUILDINGFLOW_PROJECT_CORE.md). İlk iterasyon sınırları: [CODEX_START_PROMPT.md](docs/CODEX_START_PROMPT.md). Uygulama kararları ve ertelenen işler: [decisions.md](docs/architecture/decisions.md).

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
| MinIO Console | http://localhost:9001 |
| MinIO API | http://localhost:9000 |
| Grafana | http://localhost:3002 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6389 |

Portlar `.env` üzerinden değişir. 3000 başka bir uygulama, 5000 macOS Control Center tarafından kullanıldığı için frontend 3003 ve API 5005 seçildi. Bu bilgisayarda başka projeler 6379 ve 3001 kullandığı için Redis 6389, Grafana 3002 seçildi. **WEB_PORT değişirse** Keycloak realm dosyasındaki redirect URI ve web origin de aynı portla güncellenmelidir. Realm import yalnızca ilk oluşturulurken uygulanır. Mevcut web client portunu güncellemek için `bash scripts/update-keycloak-web-origin.sh` çalıştırın (bootstrap yönetici hesabı hâlâ aktif olmalıdır); diğer realm değişikliklerini Keycloak yönetim ekranından yapın.

Yönetim ekranlarının kullanıcı adı ve parolaları `.env` içindedir. Parolaları Git'e eklemeyin. Keycloak realm: `buildingflow`; henüz kullanıcı ve frontend giriş akışı oluşturulmadı. API `/api/v1/status` geçerli JWT ister. Realm rollerinin tanımlanmış olması iş yetkilendirmesinin tamamlandığı anlamına gelmez.

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
docker compose --env-file .env -f deploy/docker-compose.yml up -d postgres keycloak redis rabbitmq minio grafana
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
npm run dev -- --port 3003
```

Frontend server tarafında API varsayılanı `http://localhost:5005`. Farklı API portu için `API_INTERNAL_URL` belirtin. OIDC yapılandırması `.env.example` dosyasında; bu değişkenler henüz oturum açma akışı gerçekleştirmez.

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
frontend/buildingflow-web   Next.js uygulama kabuğu ve gerçek API sağlık durumu
backend/src
  BuildingFlow.Domain         BaseEntity, Site, Block, Apartment; dış bağımlılık yok
  BuildingFlow.Application    Current user, storage, messaging sözleşmeleri
  BuildingFlow.Infrastructure EF Core/Npgsql, Fluent API, audit interceptor, migrations
  BuildingFlow.Api            JWT, ProblemDetails, OpenAPI, health endpoints
  BuildingFlow.Worker         BackgroundService başlangıç host'u
backend/tests                 Domain ve relational persistence testleri
deploy                        Docker Compose ve Keycloak realm tanımı
```

`BaseEntity` alanları: `Id`, `Version`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `IsDeleted`, `IsEnabled`. Audit ve version `SaveChangesInterceptor` içinde uygulanır. Silme fiziksel DELETE yerine soft delete'e dönüşür. FK delete davranışı Restrict'tir. Çocuk koleksiyonlar read-only; domain ve PostgreSQL unique index'leri tekrarları önler.

Henüz site CRUD ekranı/API'si, person/resident, aidat, ödeme, notification consumer, storage provider, frontend session veya kapsam/rol politikaları yok. Bunlar sonraki derslerde eklenir.

Grafana görselleştirme katmanıdır; şu anda datasource/dashboard yüklenmez. JSON loglar stdout'a yazılır, ASP.NET Core yerleşik request metrics üretir. Loki, Prometheus ve OpenTelemetry exporter bağlantıları sonraki aşamadadır. `/health` PostgreSQL bağlantısını ölçer; bütün altyapının sağlık garantisi değildir.
