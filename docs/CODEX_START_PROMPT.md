# BuildingFlow — Codex Initial Coding Prompt

You are starting a new production-minded learning project named **BuildingFlow**.

The user is a Senior Java/Spring Boot developer learning modern .NET and wants to build the project incrementally, while preserving clean architecture and interview-quality engineering practices.

## PRODUCT

BuildingFlow is an apartment / residential complex / site management platform.

It will eventually manage:

- sites
- blocks/buildings
- apartments
- residents
- owners
- tenants
- dues
- debts
- payments
- expenses
- announcements
- documents
- meters
- notifications

The project must start SMALL and grow incrementally.

Do NOT implement the entire business domain now.

---

## REQUIRED STACK

Frontend:
- Next.js

Backend:
- modern .NET
- ASP.NET Core Web API
- C#

Database:
- PostgreSQL
- EF Core
- Npgsql

Authentication / Authorization:
- Keycloak
- OAuth2
- OpenID Connect
- JWT Bearer

Cache:
- Redis

Messaging:
- RabbitMQ

Object Storage:
- MinIO

Background Processing:
- BuildingFlow.Worker
- email and SMS work will later be handled asynchronously through RabbitMQ

Observability:
- structured logging
- health checks
- metrics
- Grafana

Containerization:
- Docker
- Docker Compose

---

## IMPORTANT ARCHITECTURE DECISION

Start as a MODULAR MONOLITH.

Do NOT create microservices.

Use:

BuildingFlow.Api
BuildingFlow.Application
BuildingFlow.Domain
BuildingFlow.Infrastructure
BuildingFlow.Worker
BuildingFlow.UnitTests
BuildingFlow.IntegrationTests

Clean Architecture dependency direction:

Api -> Application
Infrastructure -> Application + Domain
Application -> Domain
Domain -> no outer project

Domain must NOT reference:

- ASP.NET Core
- EF Core
- Keycloak
- Redis
- RabbitMQ
- MinIO
- Grafana

---

## REPOSITORY STRUCTURE

Create:

buildingflow/
  frontend/
    buildingflow-web/

  backend/
    BuildingFlow.sln
    src/
      BuildingFlow.Api/
      BuildingFlow.Application/
      BuildingFlow.Domain/
      BuildingFlow.Infrastructure/
      BuildingFlow.Worker/
    tests/
      BuildingFlow.UnitTests/
      BuildingFlow.IntegrationTests/

  deploy/
    docker-compose.yml
    postgres/
    keycloak/
    redis/
    rabbitmq/
    minio/
    grafana/

  docs/

  .env.example
  README.md

Copy the provided logo asset:

buildingflow-logo.png

into the Next.js public folder and use it in the basic application shell or login placeholder.

---

## BACKEND BASE ENTITY — MANDATORY

Create a common abstract BaseEntity.

It MUST contain:

- Guid Id
- long Version
- DateTimeOffset CreatedAt
- string CreatedBy
- DateTimeOffset ModifiedAt
- string ModifiedBy
- bool IsDeleted
- bool IsEnabled

Database column names MUST be:

id
version
created_at
created_by
modified_at
modified_by
is_deleted
is_enabled

Requirements:

- Guid/UUID identity
- Version = optimistic concurrency token
- CreatedAt / CreatedBy automatically populated
- ModifiedAt / ModifiedBy automatically populated
- soft delete with IsDeleted
- business enable/disable with IsEnabled
- global query filter for IsDeleted
- SaveChangesInterceptor for audit
- do NOT populate audit fields in controllers
- do NOT use EF attributes in Domain
- use Fluent API configuration

Use an application-generated Guid strategy if it fits the design and prevents Guid.Empty relationships.

---

## INITIAL DOMAIN ONLY

Create only the initial skeleton for:

Site
Block
Apartment

Relationships:

Site 1 -> N Block
Block 1 -> N Apartment

Site:
- Name
- City
- District
- Address

Block:
- SiteId
- Name

Apartment:
- BlockId
- Number
- Floor
- GrossSquareMeters

Rules:

- names/numbers cannot be empty
- duplicate block name inside the same site should be prevented at domain/application level
- duplicate apartment number inside the same block should be prevented
- GrossSquareMeters must be positive

Use encapsulated collections.

Prefer:

private List<T>
IReadOnlyCollection<T>

Do NOT expose unrestricted mutable public lists.

Do NOT implement Person, fees, payments or announcements yet.

---

## POSTGRESQL / EF CORE

Configure:

- BuildingFlowDbContext
- DbSet<Site>
- DbSet<Block>
- DbSet<Apartment>
- Fluent API configurations
- snake_case columns
- PostgreSQL via Npgsql
- global soft-delete query filtering
- optimistic concurrency

Use Restrict / NoAction delete behavior where appropriate.

Do NOT use database cascade deletes for important historical business data.

Add an initial migration only after the solution builds and tests pass.

---

## CURRENT USER ABSTRACTION

In Application create:

ICurrentUserService

with:

string? UserId
string? Username
bool IsAuthenticated

Do NOT make Application depend on Keycloak or HttpContext.

API/Infrastructure may implement the abstraction later.

For the first skeleton, a safe development/system implementation is acceptable if needed for audit.

---

## KEYCLOAK

Prepare the project for Keycloak authentication.

Create configuration placeholders for:

- Authority
- Audience

Do NOT hardcode URLs, secrets or admin credentials in source code.

Authentication target:

Keycloak
 -> Access Token
 -> ASP.NET Core JWT Bearer
 -> ClaimsPrincipal
 -> ICurrentUserService
 -> SaveChangesInterceptor

Initial roles that will be used later:

SUPER_ADMIN
SITE_ADMIN
MANAGER
ACCOUNTANT
RESIDENT

For this FIRST SKELETON:

- configure the authentication foundation if practical
- do NOT build the full role/policy model yet
- do NOT create custom login/password endpoints
- do NOT manually validate username/password in the API

---

## REDIS

Add Redis infrastructure and configuration.

For the first commit:

- service must run in Docker Compose
- backend configuration placeholder must exist
- health check may be added

Do NOT add arbitrary caching logic yet.

Redis is NOT source of truth.

---

## RABBITMQ

Add RabbitMQ to Docker Compose.

Create a minimal messaging abstraction such as:

IMessagePublisher

and an Infrastructure implementation only if this can be done cleanly without overbuilding.

Create BuildingFlow.Worker.

For the first commit:

- RabbitMQ should run
- worker should start
- health/configuration should exist
- no full email/SMS implementation yet

Future message examples:

EmailRequested
SmsRequested
NotificationRequested

Do NOT block API requests while sending mail or SMS.

---

## MINIO

Add MinIO to Docker Compose.

Prepare an abstraction such as:

IFileStorage

Do not yet implement complete document management.

MinIO should be available locally and configuration should be externalized.

Database should eventually store metadata, not file bytes.

---

## GRAFANA / OBSERVABILITY

Add Grafana service.

At minimum implement:

- structured application logs
- health endpoints
- basic metrics-ready structure

If needed, add Prometheus for metrics collection.

Keep the first observability stack simple.

Do NOT overbuild distributed tracing in the first commit.

---

## DOCKER COMPOSE

docker-compose.yml must contain at least:

- buildingflow-web
- buildingflow-api
- buildingflow-worker
- postgres
- keycloak
- redis
- rabbitmq
- minio
- grafana

Recommended local ports:

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

Use one internal network:

buildingflow-network

Use environment variables.

Create:

.env.example

Never commit real secrets.

---

## FRONTEND

Create a modern Next.js application.

Use a feature-oriented folder structure.

Initial frontend only needs:

- application shell
- BuildingFlow logo
- basic header/sidebar placeholder
- health/status page or dashboard placeholder
- API configuration
- authentication configuration placeholder

Do NOT implement the full UI now.

Use the provided `buildingflow-logo.png`.

---

## API FOUNDATION

Create:

- `/health`
- OpenAPI
- ProblemDetails
- centralized exception handling
- API version prefix `/api/v1`
- cancellation token propagation
- async APIs

Do not expose EF entities directly if DTOs are already appropriate.

---

## TESTS

Add meaningful tests.

Unit tests:
- Site valid creation
- empty Site name rejected
- Block validation
- Apartment validation
- duplicate rules where practical

Integration tests:
- DbContext mapping
- soft-delete filter
- optimistic concurrency
- PostgreSQL connectivity behavior where practical

Do NOT write meaningless tests only to increase coverage.

Do NOT rely only on EF Core InMemory provider for relational behavior.

---

## SECURITY RULES

Mandatory:

- never log access tokens
- never log refresh tokens
- never commit credentials
- do not trust decoded JWT without validation
- backend authorization will be mandatory
- use HTTPS expectations for production
- use least privilege
- restrict CORS later
- validate uploaded files later
- do not expose secrets through frontend environment variables

---

## FIRST ITERATION GOAL

The first iteration is complete when:

1. Repository structure exists.
2. Next.js frontend builds.
3. .NET solution builds.
4. Worker builds.
5. docker compose configuration exists.
6. PostgreSQL starts.
7. Keycloak starts.
8. Redis starts.
9. RabbitMQ starts.
10. MinIO starts.
11. Grafana starts.
12. API health endpoint works.
13. BaseEntity exists with fixed fields.
14. EF Core soft delete and concurrency foundations exist.
15. Site/Block/Apartment domain skeleton exists.
16. Unit tests pass.
17. Integration test project exists.
18. README explains local startup.
19. .env.example exists.
20. No secrets are committed.

---

## VERY IMPORTANT — DO NOT OVERBUILD

Do NOT implement yet:

- full Person/Resident module
- dues
- debts
- payments
- expenses
- announcements
- meters
- full document management
- complete mail/SMS provider integration
- CQRS/MediatR unless already justified
- Kafka
- Kubernetes
- microservices
- AI features
- generic repository framework
- speculative abstractions

Build the project incrementally.

---

## CODING STYLE

Use:

- modern C#
- nullable reference types
- async/await correctly
- CancellationToken for I/O
- constructor injection
- records for DTOs where appropriate
- sealed classes where inheritance is not intended
- clear naming
- small focused classes
- no magic strings for future roles/policies
- explicit architecture boundaries

---

## AFTER IMPLEMENTATION

Do all of the following:

1. Run frontend install/build.
2. Run `dotnet restore`.
3. Run `dotnet build`.
4. Run `dotnet test`.
5. Validate Docker Compose syntax.
6. Show the final repository tree.
7. Show all created projects.
8. Show all Docker Compose services.
9. Show all exposed local ports.
10. Show all environment variables in `.env.example` without real secrets.
11. Explain architecture decisions.
12. Explain anything intentionally deferred.
13. Report warnings/errors.
14. Do NOT hide failures.
15. Do NOT modify unrelated files.

Finally produce a short section:

"Next lesson / next implementation step"

The next step should be small and logical, not a large feature batch.
