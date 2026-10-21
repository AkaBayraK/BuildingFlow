# Foundation endpoints

- `GET /health`: PostgreSQL readiness (200 or 503), sanitized JSON.
- `GET /health/live`: API liveness (200).
- `GET /openapi/v1.json`: development OpenAPI document.
- `GET /api/v1/status`: JWT authentication required (401 without a token).

Property/Building/Unit CRUD and role/membership authorization are implemented; see [hierarchy.md](hierarchy.md). Unexpected failures return sanitized ProblemDetails; no exception messages or tokens are logged.

Property-scoped people and dated OWNER/TENANT/RESIDENT relationships: [people.md](people.md). Reading personal records requires a management role and active membership.

Property-scoped finance assessments, debt filters, idempotency and history-preserving cancellation: [dues.md](dues.md). Management/accountant roles and active membership are enforced by the API.

Manual cash/bank receipts, partial settlement, explicit charge allocation and reversal: [payments.md](payments.md). Internal receipt viewing does not initiate a transfer or produce a fiscal document.

- [Merkezi hata/uyarı kodları ve FE bildirim sözleşmesi](errors.md) — İŞ 07.1.

- [Gider, aylık kategori bütçeleri, revizyon geçmişi ve gelir–gider özeti](expenses.md) — İŞ 07; dört finans rolü ve aktif yönetim alanı üyeliği.

- [Duyurular ve belgeler (İŞ08)](communication.md)
- [Planlama & Bakımlar](maintenance.md)
- [Anketler](surveys.md)

- [Hesap bilgilerim: kendi profil, fotoğraf ve rızalar; kullanıcı ve kişi fotoğrafı (İŞ19)](account.md)
- [Dashboard özeti (İŞ20)](dashboard.md)

- [Kişiler merkezi — PERSON FAZ06](person-center.md): kapsamlı listeler, detay sekmeleri ve sınırlı native ilişki yazımı.
- [Web sitesi CMS — WEBSITE FAZ 02](website.md): site, sayfa, blok ve tema yönetimi, değişmez yayın ve anonim public site API'si.

- [Merkez giriş ve çalışma alanları — PERSON FAZ07](session-workspaces.md): güncel görev/daire context seçimi ve merkez istek kapsamı.
