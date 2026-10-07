# BuildingFlow — Project Core Architecture

## 1. Project Identity

**Project Name:** BuildingFlow  
**Purpose:** Apartment / residential complex / site management platform  
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
Site
 └── Block
      └── Apartment

Person
 └── ApartmentOccupancy
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

#### Site

- Name
- City
- District
- Address

#### Block

- SiteId
- Name

#### Apartment

- BlockId
- Number
- Floor
- GrossSquareMeters

#### Person

- FirstName
- LastName
- NationalIdentityNumber (optional)
- PhoneNumber
- Email

#### ApartmentOccupancy

- ApartmentId
- PersonId
- OccupancyType
- StartDate
- EndDate
- IsPrimaryContact

Do NOT use only `owner_id` and `tenant_id` fields on Apartment because relationship history must be preserved.

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
CanManageSites
CanManageApartments
CanManageResidents
CanManageFinance
CanReadOwnApartment
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
- Site
- Block
- Apartment
- Person
- ApartmentOccupancy

### Phase 3 — API Foundation

Create:

- DTOs
- validation
- ProblemDetails
- exception handling
- Site endpoints
- Block endpoints
- Apartment endpoints

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
Site/Block/Apartment domain skeleton
```

Then continue lesson-by-lesson.
