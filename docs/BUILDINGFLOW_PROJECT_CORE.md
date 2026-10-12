# BuildingFlow — Project Core Architecture

## 1. Project Identity

**Project Name:** BuildingFlow  
**Purpose:** Apartment / residential complex / property management platform
**Primary Goal:** Manage sites, buildings/blocks, apartments, residents, owners, tenants, dues, debts, payments, expenses, announcements, documents, meters, notifications and operational workflows.

**Frontend:** Next.js  
**Backend:** .NET / ASP.NET Core  
**Database:** PostgreSQL  
**Object Storage:** MinIO  
**Authentication & Authorization:** Keycloak  
**Cache:** Redis  
**Messaging:** RabbitMQ  
**Observability / Dashboard:** Grafana  
**Container Runtime:** Docker / Docker Compose  

**Logo asset:** `buildingflow-logo.png`

---

## 2. High-Level Architecture

```text
                         ┌───────────────────────┐
                         │      BuildingFlow      │
                         │        Next.js         │
                         └───────────┬───────────┘
                                     │
                              HTTPS / REST
                                     │
                                     ▼
                         ┌───────────────────────┐
                         │   BuildingFlow.Api     │
                         │   ASP.NET Core API     │
                         └───────────┬───────────┘
                                     │
                 ┌───────────────────┼───────────────────┐
                 │                   │                   │
                 ▼                   ▼                   ▼
        ┌────────────────┐   ┌──────────────┐   ┌──────────────┐
        │  Application   │   │    Redis      │   │   RabbitMQ    │
        │ Use Cases      │   │ Cache / Lock  │   │ Async Events  │
        └───────┬────────┘   └──────────────┘   └──────┬───────┘
                │                                       │
                ▼                                       ▼
        ┌────────────────┐                     ┌─────────────────┐
        │     Domain      │                     │ Notification     │
        │ Rules / Models  │                     │ Worker           │
        └───────┬────────┘                     │ Mail / SMS       │
                │                              └─────────────────┘
                ▼
        ┌────────────────┐
        │ Infrastructure │
        │ EF Core        │
        └───┬─────────┬──┘
            │         │
            ▼         ▼
    ┌────────────┐  ┌─────────────┐
    │ PostgreSQL │  │    MinIO     │
    │ relational │  │ documents   │
    └────────────┘  └─────────────┘

 Authentication / Authorization:
 Next.js + API → Keycloak → OAuth2 / OIDC / JWT

 Observability:
 API / Worker / Infrastructure → structured logs / metrics → Grafana
```

---

## 3. Solution Structure

```text
buildingflow/
│
├── frontend/
│   └── buildingflow-web/
│       ├── src/
│       ├── public/
│       │   └── buildingflow-logo.png
│       ├── package.json
│       └── Dockerfile
│
├── backend/
│   ├── BuildingFlow.sln
│   │
│   ├── src/
│   │   ├── BuildingFlow.Api/
│   │   ├── BuildingFlow.Application/
│   │   ├── BuildingFlow.Domain/
│   │   ├── BuildingFlow.Infrastructure/
│   │   └── BuildingFlow.Worker/
│   │
│   └── tests/
│       ├── BuildingFlow.UnitTests/
│       └── BuildingFlow.IntegrationTests/
│
├── deploy/
│   ├── docker-compose.yml
│   ├── postgres/
│   ├── keycloak/
│   ├── rabbitmq/
│   ├── redis/
│   ├── minio/
│   └── grafana/
│
├── docs/
│   ├── architecture/
│   ├── domain/
│   └── api/
│
├── .env.example
├── README.md
└── BUILDINGFLOW_PROJECT_CORE.md
```

---

## 4. Backend Architecture

Use **Clean Architecture + DDD-friendly boundaries**.

```text
BuildingFlow.Api
        │
        ▼
BuildingFlow.Application
        │
        ▼
BuildingFlow.Domain

BuildingFlow.Infrastructure
        │
        ├── depends on Application
        └── depends on Domain
```

### 4.1 BuildingFlow.Domain

Contains:

- Entities
- Value Objects
- Enums
- Domain Rules
- Domain Exceptions
- Domain Events
- Aggregate boundaries

Must NOT depend on:

- EF Core
- ASP.NET Core
- Keycloak
- Redis
- RabbitMQ
- MinIO
- Grafana
- HTTP

### 4.2 BuildingFlow.Application

Contains:

- Use cases
- Services
- DTOs
- Interfaces / abstractions
- Validation
- Authorization abstractions
- Current user abstraction
- Repository/query abstractions only when actually useful

Initial interfaces may include:

```text
ICurrentUserService
IApplicationDbContext
IFileStorage
ICacheService
IMessagePublisher
INotificationService
```

Avoid creating an interface for every class without a real abstraction need.

### 4.3 BuildingFlow.Infrastructure

Contains implementations for:

- EF Core
- PostgreSQL
- Redis
- RabbitMQ
- MinIO
- Keycloak integration helpers
- Audit interceptor
- Soft delete behavior
- Persistence configuration
- External services

### 4.4 BuildingFlow.Api

Contains:

- Controllers / endpoints
- Authentication
- Authorization policies
- ProblemDetails
- Global exception handling
- OpenAPI
- Dependency registration
- API middleware

### 4.5 BuildingFlow.Worker

Background processing:

- RabbitMQ consumers
- Mail sending
- SMS sending
- notification jobs
- retry / dead-letter handling

---

## 5. Mandatory BaseEntity

Every domain entity MUST inherit from the common base structure.

User confirmation (2026-10-07): all BuildingFlow application tables, including explicit relationship/membership entities, MUST contain the eight columns below as NOT NULL. Every new entity/migration must pass the model-wide and real PostgreSQL schema contract tests in `PersistenceTests`. EF migration history and third-party managed database schemas are outside the application entity model; do not silently exempt new custom tables.

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    public long Version { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public string CreatedBy { get; protected set; } = null!;

    public DateTimeOffset ModifiedAt { get; protected set; }

    public string ModifiedBy { get; protected set; } = null!;

    public bool IsDeleted { get; protected set; }

    public bool IsEnabled { get; protected set; } = true;
}
```

Database columns MUST be:

```text
id
version
created_at
created_by
modified_at
modified_by
is_deleted
is_enabled
```

### Rules

- `Id`: Guid / PostgreSQL uuid
- `Version`: optimistic concurrency token
- `CreatedAt`, `CreatedBy`: immutable after insert
- `ModifiedAt`, `ModifiedBy`: updated automatically
- `IsDeleted`: soft delete
- `IsEnabled`: business enable/disable state

### EF Core

Use:

- Fluent API
- `IEntityTypeConfiguration<T>`
- `SaveChangesInterceptor`
- Global Query Filter
- Concurrency token

Do NOT spread EF Core attributes into Domain entities.

---

## 6. Initial Domain Modules

Start with:

```text
Property (APARTMENT / SITE / RESIDENCE / MIXED)
 ├── Unit directly (APARTMENT; no Building)
 └── Building (SITE / RESIDENCE / MIXED)
      └── Unit

Person
 └── UnitOccupancy
      ├── Owner
      ├── Tenant
      └── Resident
```

Then grow into:

```text
Fee
Debt
Payment
Expense
Announcement
Document
MeterReading
Notification
```

### Initial entities

#### Property

- Type (APARTMENT / SITE / RESIDENCE / MIXED)
- FloorCount (optional; APARTMENT only)
- Name
- City
- District
- Address

#### Building

- PropertyId
- Name
- FloorCount (optional)

#### Unit

- PropertyId (required)
- BuildingId (nullable for standalone apartment)
- Type (APARTMENT / OFFICE / SHOP / VILLA / WAREHOUSE / WORKSHOP)
- Number
- Floor
- GrossSquareMeters

#### Person

- PropertyId (required; kişi kaydı yönetim alanına özeldir)
- FirstName
- LastName
- PhoneNumber (optional)
- Email (optional)

04 kararı: 12.10.2026 onaylı kişi genişletmesi: yerli gerçek T.C. kimlik, yerli tüzel vergi, yabancı gerçek pasaport, yabancı tüzel ülke+kuruluş kimlik numarası kaydedilir. Person bir Keycloak giriş hesabı değildir; eklenmesi kullanıcı hesabı veya otomatik bildirim oluşturmaz.

#### UnitOccupancy

- PropertyId (required)
- UnitId
- PersonId
- Type (OWNER / TENANT / RESIDENT)
- StartDate
- EndDate
- IsPrimaryContact

Do NOT use only `owner_id` and `tenant_id` fields on Unit because relationship history must be preserved.

04 uygulama sözleşmesi: tarihler `DateOnly`/PostgreSQL `date`, iki uç dahil; iş günü Europe/Istanbul'dur. Aynı kişi/bölüm/rolün tarihleri çakışamaz. Aynı bölümde tarihleri çakışan iki birincil iletişim olamaz; ortak malik ve birden çok sakin desteklenir. Bitiş bir kez version kontrolüyle kaydedilir, geçmiş silinmez. Bitiş günü hâlâ geçerlidir; ertesi gün ENDED olur. ACTIVE/UPCOMING/ENDED başlangıç ve bitişten hesaplanır.

Kişisel veri okuma ve yazma yalnızca SUPER_ADMIN/SITE_ADMIN/MANAGER için açıktır; global SUPER_ADMIN dışında aktif yönetim alanı üyeliği zorunludur. RESIDENT/ACCOUNTANT bu kişi ekranını/API'sini kullanamaz. Person/Unit yanlış Property ile birleştirilemez: composite FK ve backend scope kontrolü vardır. Audit ve sekiz BaseEntity alanı ilişki tablolarında da zorunludur. Üst kayıtlar aynı transaction içinde version ile kilitlenir; tarih çakışması yarışları 409 döndürür.

Devam eden veya bitişi bugün/gelecekte olan ilişkide Person/Unit silme ya da pasife alma engellenir. Geçmişte biten ilişkiler ad ve bölüm bilgisini soft-delete sonrasında da saklar; history sorgusunda scope/üyelik filtreleri açıkça korunur. Person bulunan Property silinemez. `PeopleAndUnitOccupancies` yeni migration/snapshot ile kurulur; önceki uygulanmış migration değiştirilmez. Yeni migration Down, kişi/ilişki verisi varsa veri kaybını engeller. Veri içeren downgrade bakım/yedekleme kararı gerektirir.

---

## 7. Frontend Architecture

Use modern Next.js.

Suggested structure:

```text
src/
├── app/
├── components/
├── features/
│   ├── sites/
│   ├── blocks/
│   ├── apartments/
│   ├── residents/
│   ├── fees/
│   └── payments/
├── lib/
├── services/
├── auth/
├── hooks/
├── types/
└── styles/
```

### Frontend principles

Frontend implementation conventions: [Frontend Convention](frontend-rules.md). Root and frontend AGENTS.md require reading it before frontend work. It records the user's 17 standards and separates implemented behavior from implemented TR/EN/message/navigation infrastructure and planned forms/query/theme/permission infrastructure; document adoption does not mean those libraries or integrations are installed.

- Feature-based modules
- API contracts typed
- Do not embed authorization logic only in UI
- UI may hide unauthorized actions but backend remains authoritative
- Use Keycloak/OIDC client flow
- Access Token sent to backend
- Refresh token managed by the authentication/session layer
- Never log tokens

---

## 8. Keycloak

Keycloak responsibilities:

- Authentication
- OAuth2
- OpenID Connect
- JWT issuance
- Roles
- Claims

Initial roles:

```text
SUPER_ADMIN
SITE_ADMIN
MANAGER
ACCOUNTANT
RESIDENT
```

Initial policies:

```text
CanManageProperties
CanManageUnits
CanManageResidents
CanManageFinance
CanReadOwnUnit
```

### Important rules

- Domain MUST NOT know Keycloak
- Application sees `ICurrentUserService`
- API/Infrastructure maps Keycloak claims
- Use `sub` as stable external identity id
- `preferred_username` may be used for readable audit data
- Backend authorization is mandatory

---

## 9. PostgreSQL

Use PostgreSQL through EF Core + Npgsql.

Rules:

- snake_case table/column names
- migrations committed to source control
- use indexes deliberately
- unique constraints where appropriate
- no automatic cascade delete for historical business data
- prefer `Restrict` / `NoAction` when history must be preserved
- use relational integration tests where DB behavior matters

---

## 10. MinIO

Use MinIO for:

- announcement attachments
- apartment/site documents
- invoices
- receipts
- contracts
- images
- reports

Application MUST depend on an abstraction such as:

```csharp
public interface IFileStorage
{
    Task<FileResult> UploadAsync(...);
    Task<Stream> DownloadAsync(...);
    Task DeleteAsync(...);
}
```

Infrastructure implements this using MinIO.

Database stores metadata, not raw file contents.

Example metadata:

```text
document
--------
id
bucket
object_key
original_file_name
content_type
size
entity_type
entity_id
...
```

---

## 11. Redis

Initial Redis use cases:

- cache reference/read-heavy data
- distributed cache
- authorization / profile cache when justified
- idempotency keys
- distributed locks only when necessary

Do NOT introduce Redis simply because it exists.

Cache must never become source of truth.

---

## 12. RabbitMQ

Use RabbitMQ for asynchronous workflows.

Initial events:

```text
NotificationRequested
EmailRequested
SmsRequested
PaymentReceived
FeeCreated
AnnouncementPublished
```

Suggested flow:

```text
BuildingFlow.Api
      │
      │ publish
      ▼
RabbitMQ
      │
      ▼
BuildingFlow.Worker
      │
      ├── Email Sender
      └── SMS Sender
```

### Rules

- use retry strategy
- define dead-letter queues
- consumers should be idempotent
- do not block API request waiting for mail/SMS
- include correlation id
- log message processing safely

---

## 13. Grafana / Observability

Grafana is the visualization layer.

Recommended local observability stack:

```text
ASP.NET Core / Worker
        │
        ├── Structured Logs
        ├── Metrics
        └── Traces
        │
        ▼
OpenTelemetry
        │
        ├── Loki / log backend
        ├── Prometheus / metrics
        └── Tempo / tracing
        │
        ▼
Grafana
```

If the first iteration needs to stay smaller, start with:

- structured logging
- health checks
- metrics
- Grafana dashboard

Then add distributed tracing.

### Minimum observability

- request count
- request duration
- errors
- DB health
- RabbitMQ health
- Redis health
- Keycloak health
- MinIO health
- worker failures

---

## 14. Docker Compose

Local development MUST start through Docker Compose.

Services:

```text
buildingflow-web
buildingflow-api
buildingflow-worker
postgres
keycloak
redis
rabbitmq
minio
grafana
```

Optional observability services:

```text
prometheus
loki
tempo
```

All services must use environment variables.

Never hardcode:

- passwords
- client secrets
- DB credentials
- Keycloak admin password
- MinIO secret
- SMTP credentials
- SMS API secret

Provide `.env.example`.

---

## 15. Initial Docker Network

```text
buildingflow-network
```

All internal services communicate by container service name.

Examples:

```text
postgres:5432
redis:6379
rabbitmq:5672
minio:9000
keycloak:8080
```

Frontend reaches API through a configured public URL.

---

## 16. Initial Ports for Local Development

Suggested defaults:

```text
Next.js        3000
API            5000
PostgreSQL     5432
Keycloak       8080
Redis          6379
RabbitMQ       5672
RabbitMQ UI    15672
MinIO API      9000
MinIO Console  9001
Grafana        3001
```

Avoid exposing unnecessary ports in production.

---

## 17. API Standards

Use:

- REST
- `/api/v1/...`
- DTOs
- ProblemDetails
- cancellation tokens
- asynchronous I/O
- pagination for list endpoints
- OpenAPI
- centralized exception handling

Examples:

```text
POST   /api/v1/sites
GET    /api/v1/sites
GET    /api/v1/sites/{id}
POST   /api/v1/sites/{siteId}/blocks
POST   /api/v1/blocks/{blockId}/apartments
POST   /api/v1/apartments/{apartmentId}/occupancies
```

HTTP conventions:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

---

## 18. Testing Strategy

### Unit Tests

Focus on:

- domain rules
- entity behavior
- date overlap rules
- validation
- authorization handlers where applicable

### Integration Tests

Focus on:

- PostgreSQL
- EF Core mappings
- migrations
- concurrency
- soft delete
- API endpoints
- authentication/authorization
- RabbitMQ workflows
- MinIO integration when necessary

Do not rely on EF Core InMemory provider for relational behavior that requires real PostgreSQL semantics.

---

## 19. Security Rules

Mandatory:

- Keycloak validates identity
- backend validates JWT
- validate issuer
- validate lifetime
- validate audience where applicable
- HTTPS in production
- never trust only decoded JWT payload
- never log access/refresh tokens
- secrets out of source control
- backend authorization required
- validate file uploads
- rate limit sensitive endpoints later
- restrict CORS
- use secure headers
- apply least privilege

---

## 20. Initial Development Phases

### Phase 1 — Core Skeleton

Create:

- repository structure
- Next.js frontend
- .NET solution
- Dockerfiles
- Docker Compose
- PostgreSQL
- Redis
- RabbitMQ
- MinIO
- Keycloak
- Grafana
- health endpoints
- README

### Phase 2 — Domain Foundation

Create:

- BaseEntity
- audit
- soft delete
- optimistic concurrency
- Property
- Building
- Unit
- Person
- UnitOccupancy

### Phase 3 — API Foundation

Create:

- DTOs
- validation
- ProblemDetails
- exception handling
- Property endpoints
- Building endpoints
- Unit endpoints

### Phase 4 — Security

Create:

- Keycloak realm/client
- JWT bearer authentication
- `ICurrentUserService`
- roles
- policies
- authorization tests

### Phase 5 — Messaging

Create:

- RabbitMQ publisher
- Worker
- EmailRequested
- SmsRequested
- retries
- dead-letter queues

### Phase 6 — Storage

Create:

- MinIO abstraction
- document upload/download
- metadata table

### Phase 7 — Observability

Create:

- logs
- metrics
- health checks
- Grafana dashboard
- optional tracing

### Phase 8 — Business Modules

Implement incrementally:

- dues
- debts
- payments
- expenses
- announcements
- meters
- documents
- notifications

---

## 21. Definition of Done — Core Skeleton

The initial project skeleton is complete only when:

- `docker compose up` starts the required local infrastructure
- frontend starts successfully
- backend starts successfully
- worker starts successfully
- PostgreSQL is reachable
- Redis is reachable
- RabbitMQ is reachable
- MinIO is reachable
- Keycloak is reachable
- Grafana is reachable
- API health endpoint reports dependencies
- build passes
- tests pass
- no secrets are committed
- README contains local startup steps
- logo is visible in frontend shell/login area
- architecture boundaries are preserved

---

## 22. Non-Goals for Initial Commit

Do NOT implement everything at once.

Initial commit must NOT include:

- complete fee module
- complete payment module
- Kafka
- Kubernetes
- CQRS/MediatR unless a real need appears
- generic repository framework
- excessive abstraction
- premature microservices
- complicated multi-tenancy
- AI features

Start modular monolith first.

---

## 23. Core Architectural Decision

BuildingFlow starts as a **modular monolith**.

```text
Next.js
   │
   ▼
ASP.NET Core Modular Monolith
   │
   ├── Domain
   ├── Application
   ├── Infrastructure
   └── Worker
```

Do NOT split into microservices in the first version.

Reason:

- simpler development
- easier transactions
- easier debugging
- easier deployment
- domain boundaries can still be kept clean
- services can be extracted later only when justified

---

## 24. First Coding Target

The first coding target is NOT the full business system.

Create only:

```text
Infrastructure running in Docker
+
Frontend shell
+
Backend solution
+
BaseEntity
+
Health checks
+
Keycloak authentication foundation
+
Property/Building/Unit domain skeleton
```

Then continue lesson-by-lesson.

## Approved model revision — 2026-10-08

This revision supersedes earlier Site/Block/Apartment naming and mandatory hierarchy.

- Shared entities: Property, optional Building, Unit, PropertyMembership; all inherit BaseEntity and retain all eight required NOT NULL audit/status columns.
- First release PropertyType: APARTMENT, SITE, RESIDENCE, MIXED. A standalone APARTMENT has direct Units with building_id NULL and no artificial Building row. Other types use Buildings and their Units.
- Unit always has property_id and optionally building_id. Composite FK guarantees that a selected Building belongs to the same Property. UnitType: APARTMENT, OFFICE, SHOP, VILLA, WAREHOUSE, WORKSHOP. APARTMENT properties contain APARTMENT units.
- Property type may change only if no Building or Unit relationship/history exists, including disabled and soft-deleted children. Creator membership does not block it. SUPER_ADMIN and expected version are required; concurrent child writes share the parent lock. Conversion of populated properties remains a future explicit workflow.
- floorCount is optional metadata on Property (standalone apartment) and Building. Unit.floor retains basement/ground/upper-floor numbers; no separate Floor or Section table yet. Section/phase and floor entities remain future features. Counts in UI reflect real undeleted records, including disabled records; entry does not silently generate empty units.
- Frontend label: Yönetim Alanları. APARTMENT detail shows Units directly; other types show Buildings, then Units. Canonical APIs/routes use properties/buildings/units; legacy frontend /sites bookmarks redirect.
- Data migration must rename existing tables, preserve IDs/audit/version/access/history, backfill existing properties to SITE and units to APARTMENT, and derive each unit.property_id from its old block. Applied migrations remain unchanged. Downgrade must refuse standalone units or new types that the old schema cannot represent.
- Write policies: Property create/edit/delete SUPER_ADMIN; Building/Unit SUPER_ADMIN/SITE_ADMIN/MANAGER with active PropertyMembership unless global. Soft-delete, disabled-parent guards, optimistic concurrency and parent version locking remain mandatory.

### Frontend locale, feedback and management navigation — implemented 2026-10-08

Next-intl TR/EN uses identical message keys and a validated preference cookie; URLs and Keycloak S256 PKCE callbacks keep their existing structure. Shared MessageBanner/FieldError and notifications distinguish success, error, warning and information with text, semantic colors, icons and appropriate live regions. Unknown ProblemDetails content uses a safe localized fallback. Existing business data is authorized client-side data, never anonymous SSR data.

Units and People (for authorized managers) remain visible on the overview and property list. When no Property is selected, `/units` and `/people` reuse the authorized Property list as a module picker; selecting a Property opens that module directly. The selected Property scopes independent General Information, Buildings/Blocks, Units and People menu pages. APARTMENT has direct Units and no building menu. Multi-building properties select a building on the Units page before managing its units. Person/unit relationships have their own `/properties/{id}/people/{personId}` page. Section/Floor entities are not added. Existing legacy links, backend access/version rules and user records are preserved. See frontend-rules.md and TESTING.md for implemented versus planned standards and evidence.

## 05 — İş yol haritası: aidat ve borçlandırma — 2026-10-08

Ekrandaki 00–… iş yol haritasının 05 adımı aidat/borçlandırmadır; yukarıdaki ilk iskelet “Phase 5 — Messaging” başlığı ayrı tarihsel teknik plandır. Güncel iş kapsamı [TESTING.md](TESTING.md) ve [aidat API sözleşmesi](api/dues.md) ile izlenir.

DuesAssessment başlık + DuesCharge satırları ortak BaseEntity’den türer. Borç Property/Unit’e bağlanır; apartmanda building_id NULL modeli korunur. Aylık dönem, TRY, pozitif en fazla iki ondalık tutar, vade/açıklama; seçilmiş 1–500 aktif bölüme tek transaction ile aynı tutar. İstek UUID/fingerprint replay ve daire/dönem partial unique index mükerreri önler. Aynı Property satırındaki kilit ve parent version, yarışlarda rollback davranışını korur.

Finans erişimi SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT + aktif üyelik; muhasebeciye Person erişimi açılmaz. Borç malik/kiracıya otomatik atanmaz. OPEN/OVERDUE Europe/Istanbul gününden, CANCELLED gerekçeli version kontrollü iptalden gelir. İptal bakiyeyi sıfırlar, tutar/audit/geçmiş kalır. Finans geçmişi olan Property silinmez; soft-delete edilmiş bölümün geçmiş borcu kapsam filtresi korunarak okunur. Ödeme/mahsup/makbuz sonraki 06 fazıdır.

Kalıcı ayrı `/dues` modül seçimi ve `/properties/{id}/dues`; TR/EN, shadcn Dialog + TanStack RecordList masaüstü tablo/mobil kart. Sunucu arama/sayfalama/sıralama, bölüm/dönem/vade/durum filtreleri ve filtreye uyan tüm kayıtların özetleri. Toplu bölüm seçimi, kuruş hesabı, kullanıcı onayı, alan hatası, ikonlu uyarı/bildirim, aynı içerikte belirsiz sonuç retry UUID ve güncel version yükleme ortak frontend standartlarına uyar. Hassas business verisi anonim SSR’ye eklenmez.

Yeni EF migration/snapshot `20261008185906_DuesAndCharges`; önceki migration’lar korunur. Ayrı gerçek PostgreSQL’de domain/API/şema/migration ve negatif rol kontrolleri: 196 backend testi; frontend sözleşme testleri/typecheck/build sonuçları ve kalan sınırlar TESTING.md’de. Yeni paket/harici görsel/lisanssız ürün eklenmez.

## UI Aşama 06 uygulama kaydı — 2026-10-09

Görsel plandaki Daireler/Kişiler yenilendi: gerçek kapsam özetleri, ortak kompakt RecordList/TanStack tablo ve mobil kart, sağ detay/düzenleme paneli; shadcn Dialog, mevcut EntityEditor ve merkezi yetkili API kullanılır. İş akışı feature katmanında, sunum WorkspacePresentation'da kalır. Kişi detayında gerçek UnitOccupancy rolleri/tarihleri gösterilir; kişiye tek rol veya doluluk durumu uydurulmaz. APARTMENT doğrudan Unit davranışı, rol/üyelik, version/409, soft-delete/audit, API ve DB şeması korunur. Görsel UI06 ile iş 06 ödeme/tahsilat farklıdır. 23 frontend test ve typecheck/build geçti; manuel kapsam/sınırlar [TESTING.md](TESTING.md), referans karşılaştırması [UI_STAGE06_REPORT.md](analysis/UI_STAGE06_REPORT.md) içindedir.

## İş Fazı 06 — Ödeme ve borç kapatma — 2026-10-09

Ekrandaki iş yol haritasının 06 adımıdır; görsel UI06 ve tarihsel teknik phase numaralarından ayrıdır. Payment + PaymentAllocation ortak BaseEntity/mapping kullanır. Alınmış CASH/BANK_TRANSFER tahsilatı aynı Property/Unit'in açıkça seçilen 1–100 borcuna dağıtılır; TRY decimal string, toplam eşliği, kısmi/tam bakiye, future date/version/referans ve üyelik kontrolleri. Property row lock, UUID/fingerprint, partial unique bank reference ve tek transaction mükerrer/fazla ödeme yarışlarını korur. Gerekçeli version kontrollü ödeme iptali bakiyeyi atomik geri açar, ödeme ve dağıtım geçmişi kalır. Ödemeli borç doğrudan iptal edilemez.

DuesCharge.paid_amount ile kalan bakiye ve OPEN/PARTIALLY_PAID/OVERDUE/PAID/CANCELLED durumları hesaplanır; açık/gecikmiş özet kalan bakiyeyi kullanır. Tarihsel pasif/silinmiş Unit borcu tahsil edilebilir; Property/üyelik kapsamı açıktır. Muhasebeciye kişisel veri erişimi eklenmez. APARTMENT doğrudan Unit davranışı ve ortak sekiz alan korunur. Yeni PaymentsAndAllocations migration/snapshot; önceki migration dosyaları değişmez, finans geçmişinde Down reddedilir.

Kalıcı ayrı Tahsilatlar menüsü, alan seçimi, filtre/gerçek özet/masaüstü tablo/mobil kart, ortak sağ form/detay; borç detayında ödeme ve tahsilat geçmişi. TR/EN, runtime DTO/scope doğrulaması, kesin kuruş toplamı, UUID belirsiz retry ve version/409 kurtarma. Detay uygulama içi tahsilat belgesidir; gerçek transfer, online kart sağlayıcısı, banka iadesi veya fiskal makbuz üretilmez. Bu bağlantılar ayrı, sağlayıcısı/lisansı doğrulanmış gelecekteki kapsamdır. [API](api/payments.md), [TESTING](TESTING.md) ve [migration akışı](MIGRATIONS.md) güncel kanıtı taşır.

## Onaylı ek Faz 06.1 — Tahakkuk kişi geçmişi ve taşınma onayı

Property → opsiyonel Building → Unit modeli korunur. DuesChargeParty her tahakkukta dönem ilk gününü kapsayan OWNER/TENANT/RESIDENT kişi ilişkilerinin kimlik/ad/rol/tarih snapshot'ını saklar. DuesAssessment.debtorType UNIT (varsayılan), TENANT veya OWNER; kişisel muhatap açık seçilir ve her bölüm için tek geçerli ilişki gerektirir. Eski borçlara güncel kişilerden tarihçe üretilmez. Kişi seçenekleri/rol filtresi borç snapshot'larından; ACCOUNTANT kişisel veri yetkisi genişletilmez.

DuesCancellationReason beş başlangıç sebebi içerir; OTHER açıklama ister. İptalde sebep kod/adı da değişmez geçmiş olarak borca yazılır. Her iki yeni tablo BaseEntity ve ortak mapping kullanır; EF migration/snapshot, composite FK, transaction/version, soft-delete/audit sözleşmeleri korunur.

Kullanıcı seçimi: **otomatik iptal yerine yönetici onayı**. Kiracı ilişkisi bitişi öncesinde yalnız o ilişkiye atanmış gelecekteki, hiç ödeme geçmişi olmayan borçlar önizlenir. Varsayılan onay kapalı; açık ID/version listesiyle onaylanan iptal ve bitiş atomik. Malik/eski dönem/aynı dönem/bölüm muhatabı/legacy/ödemeli ve terslenmiş ödeme geçmişi korunur. Yeni borçlar eski onaya dahil edilmez. Güncel sözleşmeler [dues](api/dues.md), [people](api/people.md), [analiz](analysis/PHASE_06_1_DUES_PARTIES.md); test kanıtları TESTING.md ve ekrandaki ayrı 06.1 kaydıdır. Faz 07 Giderler bu taleple başlamış sayılmaz.

## İŞ 07 — Gider ve bütçe uygulaması — 10.10.2026

Onaylı kapsam Expense + PropertyBudget + BudgetRevision; sekiz BaseEntity NOT NULL alanı. Gider pozitif TRY/iki ondalık, Europe/Istanbul bugünden ileri tarih yok, açıklama/referans; tutar değiştirilmez, version/gerekçeyle tek iptal ve yeni düzeltme kaydı. Sekiz sabit kategori; dosya yükleme sonraki 08 kapsamıdır. Bütçe alan/ay/kategori için tek; sıfır plan açık, her yazım immutable revizyon, UUID/normalize fingerprint/expected version. Replay eski isteği tekrar uygulamaz, güncel bütçe + appliedRevision verir.

Aylık gelir yalnız iptal edilmemiş Payment.paidDate tahsilatı; gider Expense.occurredDate gerçekleşmesi. Tahakkuk gelir değildir. Tek RepeatableRead anlık görüntüde sekiz kategori, plan/gerçekleşen/fark ve net; plansız kategori sıfır planla karıştırılmaz. Gider listesi tüm filtre kapsamı özetiyle sunucuda sayfalı; aylık özet filtrelerden bağımsızdır. Dört finans rolü/aktif üyelik, ACCOUNTANT kişi yetkisi sınırı, Property kilidi/audit/version, composite FK/check/index ve tarihçe Down koruması. Yeni migration ExpensesAndBudgets; önceki satırlar/migration değiştirilmez.

Kalıcı Gider ve Bütçe menüsü, TR/EN, shadcn/TanStack liste/mobil kart ve ortak sağ panel; kodlu merkezi bildirimler. [API](api/expenses.md), [analiz](analysis/PHASE_07_EXPENSES_BUDGET.md), [test ve teslim kanıtı](TESTING.md). Banka mutabakatı, tedarikçi, gider dağıtımı, belge yükleme veya ödeme emri eklenmez.


## İŞ 08 — Duyurular ve özel dosya yönetimi — 10.10.2026

Onaylı uygulama Announcement + StoredDocument ve IFileStorage'ın Minio7.0.0 sağlayıcısıdır. Modular monolith korunur; iki yeni uygulama tablosu ortak BaseEntity/audit/version/soft-delete sözleşmesini kullanır. Aktif Property üyeliği, SUPER_ADMIN/SITE_ADMIN/MANAGER yazımı; diğer üyeler yalnız yayımlanmış ALL_MEMBERS içeriğini okur. Person/UnitOccupancy giriş hesabı veya otomatik hedef kitle değildir.

DRAFT→PUBLISHED→ARCHIVED; düz metin içerik, taslak başına en çok10 ek ve yayın geçmişi korunur. Belgeler PDF/JPEG/PNG imza+MIME+uzantı kontrolü,10MiB; özel bucket/sunucu UUID anahtarı, metadata DB'de. API kimlik bilgisi/nesne anahtarı/public URL döndürmez; JWT kontrollü attachment/no-store/nosniff indirme ve SHA256/boyut doğrulaması. UUID/fingerprint replay, parent kilidi/version ve composite Property FK korunur. MinIO ile DB dağıtık transaction değildir; belirsiz commit nesnesini otomatik silme. Soft-delete metadata geçmişini ve özel nesneyi korur; retention/orphan temizliği ayrı bakım kapsamıdır.

Kalıcı Duyurular/Belgeler menüleri, TR/EN, ortak sağ panel ve shadcn/TanStack liste/mobil düzen kullanılır. [API](api/communication.md), [analiz](analysis/PHASE_08_ANNOUNCEMENTS_DOCUMENTS.md), [test kanıtı](TESTING.md). Mail/SMS İŞ09'dur; finans eki, virüs taraması ve üretim dosya saklama operasyonları tamamlanmış sayılmaz.

## Onaylı İŞ10 uygulama eki — 11.10.2026

İlk teknik planın8 fazı değişmez; ayrı iş yol haritası İŞ10(kayıt13/14) kullanıcı tarafından onaylandı. Ortak Property→opsiyonel Unit Meter; MeterReading→immutable MeterReadingRevision, sekiz BaseEntity alanı ve migration20261011064227_MetersAndReadings. Sayaç/tarih/endeks ve rapor anlamı [API sözleşmesinde](api/meters-reports.md); otomatik sayaç tahakkuku yok. Raporlar kişi verisi içermeyen alan/üyelik kontrollü özetlerdir.

Minimum telemetry şimdi çalışır: API+worker OpenTelemetry →Collector →Prometheus/Loki →Grafana; request/histogram/error/worker ve bağımlılık erişimi dashboard'u. [İşletme/yerel run](OBSERVABILITY.md). Grafana log deposu değildir. Trace deposu/Tempo ve broker traceparent propagation bu teslimin dışındadır. Lisanslar [licenses.md](licenses.md), kanıtlar [TESTING.md](TESTING.md).

## Onaylı kişi profili ve uygulama bildirimleri —12.10.2026

Kullanıcı kapsamıyla Person NATURAL/LEGAL ve DOMESTIC/FOREIGN kimlik, ülke, opsiyonel doğum/cinsiyet/adres ve zorunlu telefon/email alanlarıyla genişledi. Person hâlâ giriş hesabı değildir; aynıProperty aktif üyelik subject eşleştirmesiyle PersonMessage kalıcı alıcı kutusu sunulur. Yeni yetki/üyelik yaratılmaz. SMS sağlayıcı bağlanana kadar kapalı. Legacy kimlik bilinmez/null, tahakkuk snapshot korunur; migration eskileri değiştirmez. Yetki/şema/mesaj ayrıntısı [people.md](api/people.md).
