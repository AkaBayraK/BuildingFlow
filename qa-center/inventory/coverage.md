# QA FAZ 02 — Ekran/API/DB kapsama raporu

Üretici: `python3 scripts/qa_inventory.py`. Sinyaller metin sezgisidir; test kanıtı değildir.

| Kalem | Sayı |
|---|---|
| API endpoint (çalışma zamanı) | 458 |
| Yetkili / açık anonim / public prefix | 445 / 13 / 0 |
| Web çağrısı: tam eşleşme / yalnız taban yol | 139 / 265 |
| Mobil çağrısı: tam eşleşme / yalnız taban yol | 20 / 30 |
| Backend testlerinde route metni geçen endpoint | 269 |
| Web/mobilde tam eşleşmesi olmayan endpoint | 302 |
| Hiç tüketicisi bulunmayan endpoint | 19 |
| Web sayfası | 57 |
| Menü bağlantısı / planlı menü | 10 / 1 |
| Mobil ekran | 9 |
| Mobil sekme: her zaman / bağlı kişi gerekir / gizli | 2 / 4 / 1 |
| DB tablosu (EF snapshot) | 135 |

## Web sayfaları ve statik sinyaller

| Route | Feature | API çağrısı | Sinyal |
|---|---|---|---|
| `/` | session-workspaces | 1 | paging |
| `/account` | account | 7 | create, filter, form, paging, update |
| `/announcements` | navigation | 3 | filter, paging, search |
| `/common-areas` | - | 0 | - |
| `/consents` | consents | 4 | create, delete, filter, form, paging, search, update |
| `/consents/records/[recordId]` | consents | 4 | create, delete, filter, form, paging, search, update |
| `/dashboard` | dashboard | 5 | create, filter, form, paging, search |
| `/documents` | navigation | 3 | filter, paging, search |
| `/dues` | navigation | 3 | filter, paging, search |
| `/expenses` | navigation | 3 | filter, paging, search |
| `/login` | - | 0 | - |
| `/maintenance` | navigation | 3 | filter, paging, search |
| `/meters` | navigation | 3 | filter, paging, search |
| `/parking` | navigation | 3 | filter, paging, search |
| `/payments` | navigation | 3 | filter, paging, search |
| `/people` | navigation, person-center | 12 | create, filter, form, paging, search, update |
| `/people/[tenantId]` | person-center | 9 | create, filter, form, paging, search, update |
| `/people/[tenantId]/[personId]` | person-center | 9 | create, filter, form, paging, search, update |
| `/properties` | properties | 2 | create, filter, form, paging, search, table |
| `/properties/[id]` | properties | 2 | create, filter, form, paging, search, table |
| `/properties/[id]/announcements` | communication | 5 | create, delete, export, filter, form, paging, search, update |
| `/properties/[id]/buildings` | navigation | 3 | filter, paging, search |
| `/properties/[id]/buildings/[buildingId]` | hierarchy | 3 | create, delete, filter, form, paging, search, update |
| `/properties/[id]/buildings/[buildingId]/units` | navigation, people | 9 | create, filter, form, paging, search, update |
| `/properties/[id]/common-areas` | - | 0 | - |
| `/properties/[id]/documents` | communication | 5 | create, delete, export, filter, form, paging, search, update |
| `/properties/[id]/dues` | dues | 1 | create, filter, form, paging, search, update |
| `/properties/[id]/expenses` | expenses | 2 | create, filter, form, paging, search, update |
| `/properties/[id]/maintenance` | maintenance | 2 | create, delete, filter, form, paging, search, update |
| `/properties/[id]/meters` | meters | 3 | create, form, paging, search, update |
| `/properties/[id]/parking` | parking | 2 | create, delete, export, filter, form, paging, search, update |
| `/properties/[id]/payments` | payments | 2 | create, filter, form, paging, search, update |
| `/properties/[id]/people` | people | 7 | create, filter, form, paging, search, update |
| `/properties/[id]/people/[personId]` | people | 7 | create, filter, form, paging, search, update |
| `/properties/[id]/reports` | reports | 1 | paging |
| `/properties/[id]/requests` | requests | 4 | create, filter, form, paging, search |
| `/properties/[id]/sms` | sms | 1 | create, delete, filter, form, paging, search, update |
| `/properties/[id]/surveys` | surveys | 3 | create, delete, export, filter, form, paging, search, update |
| `/properties/[id]/units` | navigation, people | 9 | create, filter, form, paging, search, update |
| `/properties/[id]/website` | website | 6 | create, delete, export, filter, form, paging, search, update |
| `/reports` | navigation | 3 | filter, paging, search |
| `/requests` | navigation | 3 | filter, paging, search |
| `/sites` | - | 0 | - |
| `/sites/[id]` | - | 0 | - |
| `/sites/[id]/blocks/[blockId]` | - | 0 | - |
| `/sms` | navigation | 3 | filter, paging, search |
| `/suppliers` | suppliers | 6 | create, delete, export, filter, form, paging, search, update |
| `/surveys` | navigation | 3 | filter, paging, search |
| `/system-settings` | online-payments, qa-center, system-status | 4 | create, export, filter, form, paging, search, update |
| `/system-settings/qa-center` | qa-center | 1 | create, filter, paging, search, update |
| `/units` | navigation | 3 | filter, paging, search |
| `/users` | users | 8 | create, delete, filter, form, paging, search, update |
| `/website` | navigation | 3 | filter, paging, search |
| `/website-entry/[siteKey]/[audience]` | session-workspaces | 1 | paging |
| `/workspaces` | session-workspaces | 1 | paging |
| `/workspaces/[kind]/[id]` | person-center, session-workspaces | 10 | create, filter, form, paging, search, update |
| `/workspaces/portfolio` | my-portfolio | 7 | create, export, filter, form, paging, search |

## Mobil rol kapıları (statik: kaynakta geçen rol/ilişki adları)

| Modül/ekran | Roller |
|---|---|
| `auth` | ACCOUNTANT, MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `common-areas` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `components` | ACCOUNTANT, TENANT |
| `finance` | ACCOUNTANT, MANAGER, OWNER, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `manager` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `offline` | MANAGER |
| `owner` | OWNER, TENANT |
| `parking` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `person-authorization` | ACCOUNTANT, MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `screen:index` | OWNER, TENANT |
| `screen:profile` | SITE_ADMIN, SUPER_ADMIN |
| `screen:requests` | ACCOUNTANT |
| `sync` | ACCOUNTANT |

## Bulgular

- Planlı (henüz ekranı olmayan) menü: `definitions`
- Mobil sekme sekme çubuğunda gizli: `parking`
- Eşleşmeyen web çağrısı: `/api/v1/qa-center`

## Web/mobil tüketicisi bulunmayan endpointler (19)

Worker, admin, entegrasyon veya dinamik URL ile çağrılıyor olabilir; FAZ 04’te tek tek doğrulanır.

- `GET /api/public/v1/domains/{host}`
- `POST /api/public/v1/preview`
- `POST /api/public/v1/preview/media/{id:guid}/{sha}`
- `GET /api/public/v1/sites/{siteKey}`
- `POST /api/public/v1/sites/{siteKey}/inquiries/`
- `GET /api/public/v1/sites/{siteKey}/inquiries/challenge`
- `GET /api/public/v1/sites/{siteKey}/listings/`
- `GET /api/public/v1/sites/{siteKey}/listings/{id:guid}`
- `GET /api/public/v1/sites/{siteKey}/media/{id:guid}/{sha}.{ext}`
- `GET /api/public/v1/sites/{siteKey}/primary-domain`
- `GET /api/v1/parking/my-vehicles/`
- `POST /api/v1/parking/my-vehicles/`
- `POST /api/v1/parking/my-vehicles/{vehicleId:guid}/deactivate`
- `POST /api/v1/payment-webhooks/{provider}`
- `POST /api/v1/sms/opt-out-callbacks`
- `GET /api/v1/sms/platform-sender/`
- `PUT /api/v1/sms/platform-sender/`
- `GET /api/v1/status`
- `POST /api/v2/me/person-links`
