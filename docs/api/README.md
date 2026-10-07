# Foundation endpoints

- `GET /health`: PostgreSQL readiness (200 or 503), sanitized JSON.
- `GET /health/live`: API liveness (200).
- `GET /openapi/v1.json`: development OpenAPI document.
- `GET /api/v1/status`: JWT authentication required (401 without a token).

Business CRUD and role/scope authorization are intentionally deferred. Unexpected failures return sanitized ProblemDetails; no exception messages or tokens are logged.
