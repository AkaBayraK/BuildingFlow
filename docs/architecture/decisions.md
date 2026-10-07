# Foundation decisions

- Modular monolith: Domain is dependency-free, Application references Domain, Infrastructure references Application/Domain. API references Infrastructure only at the composition root.
- .NET 10 and Next.js App Router. No CQRS framework or generic repository.
- Site owns blocks, Block owns apartments. Fully load active child collections before domain mutation. Database citext unique indexes also prevent races and case-insensitive duplicates. Query filters apply to each entity independently; parent soft deletion does not recursively delete children. Future use cases must check active parents.
- `Version` is an incrementing bigint optimistic concurrency token. SaveChangesInterceptor applies audit and soft deletion to both synchronous and asynchronous saves. Created audit fields are immutable on update.
- Authentication uses issuer/audience/lifetime validation; no password endpoints. `/api/v1/status` requires authentication. No business CRUD until authorization scope is implemented.
- Keycloak uses its own persistent database. No seeded users or shared passwords. Realm roles exist; policies and frontend sessions are deferred.
- API emits structured JSON logs and uses native ASP.NET request meters (`Microsoft.AspNetCore.Hosting`, `Microsoft.AspNetCore.Server.Kestrel`). There is no exporter or Grafana datasource yet. Grafana cannot store logs; Loki, Prometheus and OpenTelemetry exporters are later work.
- Worker runs an idle cancellable host. It does not claim to consume messages or send notifications. Messaging/storage interfaces are contracts, not registered implementations.
- `/health` checks PostgreSQL connectivity, `/health/live` checks API process availability. It does not assert Redis, RabbitMQ, Keycloak or MinIO readiness. Compose checks PostgreSQL, Redis and RabbitMQ; smoke checks verify all UIs and identity discovery separately.
- Secrets are generated into ignored `.env`. Compose is local development only, bound to loopback. Production HTTPS, non-admin DB credentials and hardened Keycloak settings are a later deployment task.
