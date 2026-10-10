# Foundation decisions

- Modular monolith: Domain is dependency-free, Application references Domain, Infrastructure references Application/Domain. API references Infrastructure only at the composition root.
- .NET 10 and Next.js App Router. No CQRS framework or generic repository.
- Property owns optional Buildings and Units. APARTMENT has direct Units; SITE/RESIDENCE/MIXED Units belong to a Building. Fully load relevant child collections before domain mutation. Database citext unique indexes also prevent races and case-insensitive duplicates. Query filters apply to each entity independently; parent soft deletion does not recursively delete children. Use cases check active parents.
- `Version` is an incrementing bigint optimistic concurrency token. SaveChangesInterceptor applies audit and soft deletion to both synchronous and asynchronous saves. Created audit fields are immutable on update.
- Authentication uses issuer/audience/lifetime validation; no password endpoints. `/api/v1/status` requires authentication. Business CRUD validates both signed realm roles and per-property membership.
- Keycloak uses its own persistent database. The local development account is provisioned with a generated ignored password, without resetting an existing account. Phase 01 implements frontend OIDC sessions; phases 02/03 implement data policies.
- API emits structured JSON logs and uses native ASP.NET request meters (`Microsoft.AspNetCore.Hosting`, `Microsoft.AspNetCore.Server.Kestrel`). There is no exporter or Grafana datasource yet. Grafana cannot store logs; Loki, Prometheus and OpenTelemetry exporters are later work.
- Worker runs an idle cancellable host. It does not claim to consume messages or send notifications. Messaging/storage interfaces are contracts, not registered implementations.
- `/health` checks PostgreSQL connectivity, `/health/live` checks API process availability. It does not assert Redis, RabbitMQ, Keycloak or MinIO readiness. Compose checks PostgreSQL, Redis and RabbitMQ; smoke checks verify all UIs and identity discovery separately.
- Secrets are generated into ignored `.env`. Compose is local development only, bound to loopback. Production HTTPS, non-admin DB credentials and hardened Keycloak settings are a later deployment task.

## Phase 01 identity decisions (2026-10-07)

- Custom credentials UI lives in a Keycloak login theme; Next.js redirects with the official keycloak-js adapter. Passwords never pass through the BuildingFlow API. Authorization Code + S256 PKCE; public client, no secret and no password grant.
- Tokens stay in adapter memory; refresh is periodic, logout ends the refresh session. Frontend AuthGate is a UX guard. Future sensitive SSR data needs server authentication before rendering; API remains the authority for access control.
- Keycloak owns reset action tokens, password policy and email delivery. Local SMTP uses Mailpit; production needs a real SMTP provider with TLS, HTTPS, exact redirect/origin settings and production-mode Keycloak.
- Phase 01 precedes phase 02 Site CRUD. Site roles and per-site data access policies remain in phase 02.
- Dependency licenses and test evidence are tracked in docs/licenses.md and docs/TESTING.md.

## Migration lifecycle (2026-10-07)

- EF Core Migrations is the schema and versioned data-change mechanism. Existing C# migrations replace the role of Liquibase XML changesets; no second framework was added.
- Source/Designer/snapshot travel together. A model/snapshot regression test rejects unrecorded changes. Published/applied migrations remain immutable; corrections use a new migration. Column renames, backfills and destructive changes require explicit review of generated Up/Down/SQL.
- scripts/db.sh standardizes source listing, model checking, adding a migration, idempotent SQL export and local update. Generated SQL goes to ignored artifacts/database; the helper never accepts a production connection. Production bundle/pipeline remains future work.
- The existing one-shot Compose migration service applies schema before API readiness; normal API startup does not migrate. Isolated real PostgreSQL tests cover fresh install, initial-to-latest upgrade preserving data, and repeated application. See docs/MIGRATIONS.md.

## Phase 02 site decisions (2026-10-07)

- Signed realm_access.roles is the only business role source. Creating sites requires SUPER_ADMIN and a stable JWT sub. SUPER_ADMIN reads all undeleted sites; other users need an enabled, undeleted SiteMembership. Client role claims or scope query arguments cannot expand access.
- Application validates requests without EF/HTTP coupling. Infrastructure inserts Site + creator membership in one SaveChanges transaction. Trusted current-user audit stays in the existing interceptor.
- SiteStore applies access before search/count/sort/pagination. Sort fields/directions are whitelisted; ILIKE wildcard characters are escaped; Id breaks equal sort values. Cross-scope, absent and deleted details share a 404 response.
- CascadeDeleteTiming.OnSaveChanges lets the audit interceptor turn parent deletion into soft-delete before required tracked children cause conceptual-null errors; database foreign keys retain Restrict. Parent soft-delete does not recursively delete child data.
- Sensitive site content is client-fetched with in-memory JWT, never anonymous SSR. The workspace layout keeps auth across navigation. Only a validated /sites or /sites/{guid} return path is stored in sessionStorage to resume after the exact root OIDC callback; no credentials/tokens are stored there.
- UI uses official shadcn/ui MIT source + TanStack Table v8 MIT with Tailwind. Server pagination/sorting is explicit; no fake local filtering over one page. Licensed source/NOTICE is preserved. No paid UI template was added.
- This phase implements create/list/detail. Update/delete/version conflict UI, blocks/apartments and membership assignment are later work. The 56 .NET tests include 34 new site API cases on a separate PostgreSQL test database/schema; browser evidence is separately recorded.

## Phase 03 shared management model (2026-10-08)

- This approved revision supersedes the historical Site/Block/Apartment terminology above. Four initial PropertyTypes are APARTMENT/SITE/RESIDENCE/MIXED. A standalone apartment never requires an artificial block; Unit.BuildingId is nullable and Unit.PropertyId is always required. Other property types require a Building for each Unit. Six UnitTypes share one table; no separate apartment/office/shop entities.
- Section/phase and separate Floor entities are deferred. Unit.floor stores basement/ground/upper-floor numbers; optional positive floorCount is metadata on an apartment Property or a Building. UI counts are actual undeleted rows, including disabled rows. Entering counts does not generate empty Units. Property type may change when no Building/Unit history exists; disabled and soft-deleted children also lock it. Creator membership does not lock it. SUPER_ADMIN, expected version and the existing parent concurrency lock protect changes; populated conversion remains deferred.
- A composite FK from Unit(PropertyId, BuildingId) to Building(PropertyId, Id) enforces consistent ownership. Distinct partial unique indexes handle direct property units and building units, including the NULL-building case. All four application tables inherit BaseEntity with eight required audit/status columns.
- UnifiedPropertyModel renames/backfills the existing tables; applied old migrations are immutable. IDs/audit/version/access/history survive. Existing properties backfill to SITE; existing units to APARTMENT, with PropertyId derived from their old building. Down refuses values/history the old schema cannot represent.
- Property create/edit/delete requires SUPER_ADMIN. Building/Unit write requires SUPER_ADMIN/SITE_ADMIN/MANAGER plus enabled membership unless global. Out-of-scope or wrongly nested records share 404. Disabled parents block child writes; undeleted children block parent deletion. Deletes preserve history.
- Expected version is required on updates/deletes. Child mutations touch the property and building version in the same transaction, so concurrent parent changes roll back the whole write. Database concurrency/duplicate races return 409. UI offers explicit latest-record reload rather than overwriting a stale edit.
- Canonical UI/API paths use properties/buildings/units. Legacy frontend /sites bookmarks redirect; removed legacy API routes are documented as historical. OIDC return routes accept only validated GUID paths. Sensitive records remain client-fetched with an in-memory JWT.
- Delivery evidence: 121 backend tests, 0 failed/skipped; 17 Keycloak checks; host frontend build/typecheck; real PostgreSQL migration/schema/race tests; browser apartment/mixed/dual-session/mobile acceptance. See TESTING.md. Section/Floor, membership assignment and container rebuild remain outside this delivery.
