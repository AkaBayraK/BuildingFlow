# Kullanıcı yönetimi (İŞ14)

Merkez ekranı `/users`, Keycloak `buildingflow` realm'indeki giriş hesaplarını yönetir. API Keycloak Admin REST API'ye en az yetkili servis hesabıyla bağlanır.

## Servis hesabı

- Client `buildingflow-admin-api`: confidential, yalnız client credentials (standard flow ve direct grant kapalı).
- Servis hesabı rolleri: `realm-management` → `view-users`, `query-users`, `manage-users`. realm-admin yoktur; realm ayarı değiştirme denemesi 403 döner (yerelde doğrulandı).
- Yapılandırma: `KeycloakAdmin__BaseUrl` (ör. `http://localhost:8080`), `KeycloakAdmin__ClientSecret`, isteğe bağlı `KeycloakAdmin__Realm` (varsayılan `buildingflow`), `KeycloakAdmin__ClientId`. Yerelde `scripts/configure-identity.sh` client'ı oluşturur, sırrı `.env` `BUILDINGFLOW_KEYCLOAK_ADMIN_CLIENT_SECRET` değerinden ayarlar; yeni kurulumda `deploy/keycloak/realm-buildingflow.json` aynı client'ı içerir. Production'da BaseUrl verilirse HTTPS ve ≥16 karakter sır zorunlu (açılış ve `release-preflight.py`).
- Ayar yoksa veya Keycloak'a erişilemezse uçlar `503 BF_IDENTITY_UNAVAILABLE` döner.

## Yetki ve kapsam

| Rol | Görebildiği kullanıcılar | Atayabildiği roller |
| --- | --- | --- |
| SUPER_ADMIN | Realm'deki tüm kullanıcılar (Keycloak araması/sayfalama) | SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT, RESIDENT |
| SITE_ADMIN | Yalnız kendi aktif üyeliği olan yönetim alanlarının üyeleri | MANAGER, ACCOUNTANT, RESIDENT |

SITE_ADMIN; SUPER_ADMIN/SITE_ADMIN rollü kullanıcıları ve kendini düzenleyemez, bağlayamaz, parolasını sıfırlatamaz (`canEdit:false`, 403). Yetki dışı roller güncellemede korunur. Kimse kendi hesabını pasifleştiremez; SUPER_ADMIN kendi rolünü kaldıramaz (`BF_USER_SELF_CHANGE`). Diğer roller (MANAGER, ACCOUNTANT, RESIDENT) 403.

## Uçlar (`/api/v1/users`)

| Uç | Açıklama |
| --- | --- |
| GET `?page&pageSize(≤50)&search` | Kullanıcı, realm rolleri (yalnız uygulama rolleri), kapsam içindeki üyelik sayısı ve bağlı kişiler. |
| GET `/{id}` | Tek kullanıcı; kapsam dışı 404. |
| POST `` | `{username, email, firstName, lastName, enabled, roles[], propertyId?, temporaryPassword?}`. Kullanıcı adı küçük harfe çevrilir (`^[a-z0-9][a-z0-9._-]{2,49}$`). SITE_ADMIN için kendi yönetim alanlarından biri zorunlu; seçilen alanda üyelik açılır. Geçici parola (≥12, büyük/küçük harf, rakam) yoksa Keycloak "parola belirle" e-postası gönderilir. Rol/üyelik adımı başarısız olursa açılan Keycloak kullanıcısı silinir. Çakışma `BF_USER_EXISTS`. |
| PUT `/{id}` | `{email, firstName, lastName, enabled, roles[]}`; kullanıcı adı değişmez. |
| POST `/{id}/password-reset` | Keycloak `UPDATE_PASSWORD` e-postası. |
| POST `/{id}/people` | `{propertyId, personId, version}`: kişiye bağlar, aynı transaction'da o alanda üyelik açar/etkinleştirir. Kişi başka hesaba bağlıysa veya kullanıcı aynı alanda başka kişiye bağlıysa `BF_USER_LINK_CONFLICT`; kişi profili eksikse 400; version eskiyse 409. |
| DELETE `/{id}/people/{personId}?propertyId&version` | Bağı çözer; üyelik korunur (başka rol işleri için). |
| GET `/{id}/photo` | İŞ19: hesabın kendi eklediği profil fotoğrafı (`image/jpeg`); kapsam dışı veya fotoğrafsız 404. Liste ve tek kullanıcı yanıtında `photoId` (yoksa null). Ayrıntı [account.md](account.md). |

Hard delete yoktur; "silme" pasifleştirme ile yapılır (bağlar ve denetim izleri korunur). Telefon bilgisi Keycloak'ta tutulmaz (realm kullanıcı profili yönetilmeyen alanlara kapalı).
