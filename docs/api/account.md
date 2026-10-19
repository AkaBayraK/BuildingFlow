# Hesap bilgilerim API'si (İŞ19)

Giriş yapmış hesabın kendi verisi; merkez (web) "Hesap bilgilerim" sayfası kullanır. Profil ve fotoğraf, mobil uygulamanın düzenlediği kayıtların aynısıdır ([mobile-profile.md](mobile-profile.md)): hesap başına tek profil ve tek fotoğraf. Burada verilen rıza kararları `channel = WEB` ile kaydedilir; kurallar [mobile-consents.md](mobile-consents.md) ile aynıdır. Bütün `/api` yanıtları `Cache-Control: no-store` taşır.

Yetki: geçerli JWT yeter (her rol, yalnız kendi verisi). Başka hesabın verisine bu uçlardan erişilemez; kimlik `sub` talebinden gelir, gövde veya adres ile değiştirilemez.

| Uç | Açıklama |
| --- | --- |
| GET `/api/v1/account` | `{userId, username, email, roles[], contexts[], hasMore, profile}`. `roles` yalnız uygulama rolleri (SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT, RESIDENT). `contexts` mobil oturumdaki bağlam listesiyle aynı (`propertyId, propertyName, unitId, unitNumber, role`), en fazla 500; fazlası `hasMore`. `profile` = `MobileProfileDto` (ilk açılışta version 0, ad/soyad Keycloak'tan). Tokensız 401. |
| GET `/api/v1/account/profile/photo/{id}` | Kendi güncel fotoğrafı, `image/jpeg`. Başka hesabın veya eski fotoğrafın kimliği 404. |
| POST `/api/v1/account/profile/commands` | Mobil profil komutunun aynısı: `{operationId, expectedVersion, firstName, lastName, phone, photoAction: KEEP\|REPLACE\|REMOVE, photoBase64?}`. Ad/soyad yazım kuralı (`PersonNames`) uygulanır. JPEG, ≤256 KB, ≤1024 px. Gövde ≤384 KB (413), JSON değilse 415. Eski version 409. REPLACE için güncel PROFILE_PHOTO rızası yoksa 403 `BF_CONSENT_REQUIRED`; KEEP ve REMOVE her zaman serbest. |
| GET `/api/v1/account/consents` | `MobileConsentList {userId, items:[{status, currentText}]}`; geçmiş yok (geçmiş yalnız merkez Açık Rıza menüsünde). |
| POST `/api/v1/account/consents/commands` | `{operationId, action: APPROVE\|WITHDRAW, typeCode, textId?, bodySha256?, appVersion?}`. Web istemcisi `appVersion = "web"` gönderir. Eski metin 409 `BF_CONSENT_TEXT_OUTDATED`; PROFILE_PHOTO geri çekilince profil fotoğrafı silinir. Gövde ≤16 KB. |

## Fotoğrafın başka ekranlarda görünmesi

| Uç | Kim görür |
| --- | --- |
| GET `/api/v1/users/{userId}/photo` | Kullanıcılar ekranını görebilen yönetici (SUPER_ADMIN her hesap, SITE_ADMIN yalnız kendi alanlarındaki hesaplar; `UserService` kapsamı). Kapsam dışı veya fotoğrafsız 404. Liste yanıtındaki `photoId` yalnız fotoğraf varsa dolu. |
| `linkedPhotoId` (kişi listesi ve detayı) | Bağlı hesabın güncel fotoğraf kimliği, yoksa null. İstemci yalnız doluysa fotoğrafı ister. |
| GET `/api/v1/properties/{propertyId}/people/{personId}/photo` | Kişiyi okuyabilen yönetici (`PeopleService.GetAsync` kapsamı: yönetim rolü + aktif üyelik). Kişiye bağlı giriş hesabının fotoğrafı; bağ yoksa veya fotoğraf yoksa 404. Muhasebeci ve başka alanın yöneticisi göremez. |

Yöneticiler fotoğrafı görüntüler ama ekleyemez veya değiştiremez; fotoğrafı yalnız hesap sahibi kendi rızasıyla ekler.

## Testler

`AccountApiTests` (PostgreSQL + SeaweedFS): kendi rol/alan/profil, web kanallı rıza kaydı, mobil ile aynı profil; rızasız fotoğraf 403, onay sonrası aynı fotoğraf mobil uçtan da okunur; bağlı kişi üzerinden kapsam içi yönetici görür, bağsız kişi 404, başka alan yöneticisi ve muhasebeci göremez, başka hesap kendi uçlarından 404 alır; geri çekme fotoğrafı her yerden kaldırır. `UserManagementTests.UserPhotoIsVisibleOnlyWithinManagerScope`: SITE_ADMIN kapsamı.
