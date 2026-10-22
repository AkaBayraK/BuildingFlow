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
| POST `` | `{username, email, firstName, lastName, enabled, roles[], propertyId?, temporaryPassword?, phone?}`. Kullanıcı adı küçük harfe çevrilir (`^[a-z0-9][a-z0-9._-]{2,49}$`). SITE_ADMIN için kendi yönetim alanlarından biri zorunlu; seçilen alanda üyelik açılır. Geçici parola (≥12, büyük/küçük harf, rakam) yoksa Keycloak "parola belirle" e-postası gönderilir. Rol/üyelik adımı başarısız olursa açılan Keycloak kullanıcısı silinir. Çakışma `BF_USER_EXISTS`. |
| PUT `/{id}` | `{email, firstName, lastName, enabled, roles[]}`; kullanıcı adı değişmez. |
| POST `/{id}/password-reset` | Keycloak `UPDATE_PASSWORD` e-postası. |
| POST `/{id}/people` | `{propertyId, personId, version}`: kişiye bağlar, aynı transaction'da o alanda üyelik açar/etkinleştirir. Kişi başka hesaba bağlıysa veya kullanıcı aynı alanda başka kişiye bağlıysa `BF_USER_LINK_CONFLICT`; kişi profili eksikse 400; version eskiyse 409. |
| DELETE `/{id}/people/{personId}?propertyId&version` | Bağı çözer; üyelik korunur (başka rol işleri için). |
| GET `/{id}/photo` | İŞ19: hesabın ortak profil fotoğrafı (`image/jpeg`); kapsam dışı veya fotoğrafsız 404. Liste ve tek kullanıcı yanıtında `photoId` (yoksa null). Ayrıntı [account.md](account.md). |

Hard delete yoktur; "silme" pasifleştirme ile yapılır (bağlar ve denetim izleri korunur). Telefon bilgisi Keycloak'ta tutulmaz (realm kullanıcı profili yönetilmeyen alanlara kapalı).


## BAKIM — 22.10.2026 — Yetkili fotoğraf yönetimi

Kullanıcı, yetkili yöneticinin diğer hesaplara da fotoğraf eklemesini açıkça onayladı. `canManagePhoto` liste ve detayda ayrı boolean: SUPER_ADMIN tüm hesaplar; SITE_ADMIN kendi hesabı veya mevcut düzenleme yetkisi olan alan üyeleri. SITE_ADMIN başka SUPER_ADMIN/SITE_ADMIN hesabının fotoğrafını değiştiremez. Diğer roller 403; kapsam dışı 404. Kendi fotoğrafını yönetmek hesap rolü/bağlama/parola düzenleme yetkisi vermez.

- GET `/{id}/photo/state`: `{userId, photoId, photoSha256, version, canReplace}`. `canReplace` hedef sahibinin **güncel PROFILE_PHOTO rızasını** gösterir; yazımda tekrar kontrol edilir. Telefon, özel profil ayrıntısı ve rıza geçmişi dönmez.
- POST `/{id}/photo/commands`: `{operationId, expectedVersion, photoAction: REPLACE|REMOVE, photoBase64?}` → `{operationId, photo, replayed}`. REPLACE standart base64 JPEG ≤256 KiB, boyut ≤1024 px; metadata aynı normalizer ile temizlenir. JSON gerekir (415), request ≤384 KiB (413); bozuk/uyumsuz resim/KEEP/negatif sürüm/boş UUID 400. REMOVE için fotoğraf/rıza gerekmez. Yeni yükleme rızasız veya eski rızayla 403 `BF_CONSENT_REQUIRED`.
- Kullanıcı kapsamı ve hedef rolü depolamadan önce ve profil transaction'ı içinde yeniden kontrol edilir. Aynı hedef/version/komut ve **aynı işlem yapan kullanıcı** UUID tekrarında özgün sonuç gelir; içerik veya işlemi yapan değişirse ya da yeni komut sürümü eskiyse 409. Başka kullanıcının işlem kimliği idempotent yetki sağlamaz.
- Mobil ve web aynı `MobileProfile`/fotoğraf pointer'ını, özel depoyu, hash'i, audit/version, staging/retired cleanup ve receipt mekanizmasını paylaşır. Yeni entity/kolon/migration yoktur. Creator/modifier gerçek yöneticidir, fotoğrafın sahibi hedef hesaptır; impersonation yoktur. Mevcut profil adı/soyadı/telefonu değiştirilmez. İlk fotoğrafta profil henüz yoksa mevcut Keycloak ad/soyadı (her biri 1–80 karakter) ile başlatılır; böylece mobil version>0 sözleşmesi korunur. Keycloak hesabı/rolleri değiştirilmez.
- Rıza sahibi geri çektiğinde fotoğraf ortak profilden kaldırılır; özgün başarılı komutun replay'i eski fotoğrafı diriltemez. Eski fotoğraf uçları 404 döner. REMOVE rızayı iptal etmez.
- Web’de mevcut kullanıcı satırındaki kamera düğmesi ortak sağ paneli açar. Önizleme/kaydet/kaldır, çift işlem kilidi, kapanış odağı, belirsiz yanıtta aynı UUID, 409'da açıkça yeniden yükle/seç, TR/EN ve mobil düzen bulunur. Kaynak JPEG/PNG/WebP ≤20 MB/40 MP; yerel merkez kırpma 512×512 JPEG ≤256 KiB, EXIF/konum gönderilmez. Yeni hesap önce kaydedilir, sonra fotoğraf eklenir. HEIC için yeni dönüştürücü/paket eklenmez; tarayıcıda JPEG/PNG/WebP seçilir.

**Kanıt:** [TESTING](../TESTING.md), [masaüstü](../navigation-refactor/USER_PHOTO_DESKTOP.png), [mobil](../navigation-refactor/USER_PHOTO_MOBILE.png). API entegrasyonu gerçek izole PostgreSQL ve test bucket'ında; tarayıcı kanıtı gerçek bileşen + izole fixture, canlı hesap/üretim yazımı değildir.


## BAKIM — 22.10.2026 — Yeni kullanıcı telefonu

`POST /api/v1/users` isteğinde `phone?: string|null` isteğe bağlıdır; mevcut istemciler alanı göndermeden çalışır. Numara boşsa profil oluşturulmaz; mevcut version0 davranışı korunur. Numara varsa mevcut ortak MobileProfile, giriş formunun normalize edilmiş ad/soyadı ve telefonuyla ilk kez oluşturulur (version1,photoId=null). Bu durumda ad/soyad her biri≤80; telefon mevcut mobil profil kuralıyla≤32 karakter,7–15 ASCII rakam ve yalnız `+ -()` ayırıcıları. UI ortak PhoneInput/formatPhone/validPhone kullanır; TR için+90 biçimi, uluslararası ülke kodu. Yeni SMS/OTP doğrulaması yoktur.

Rol/alan/telefon doğrulaması Keycloak hesabı açılmadan yapılır; hata400 mevcut BF_USER_INVALID ile TR/EN kodlu mesajdır. `phone` ortak FE alan hatası allowlist'ine eklendi; ham hata/iletişim verisi gösterilmez. SUPER_ADMIN/SITE_ADMIN ve mevcut rol/alan atama sınırları aynıdır. Yeni hesaba üyelik ve isteğe bağlı profil tek yerel PostgreSQL transaction'ında yazılır; parent/profil kilidi kullanılır, mevcut profil üzerine yazılmaz. Sonraki adım hatasında mevcut Keycloak oluşturma telafisi yeni hesabı kaldırmayı dener; Keycloak+PostgreSQL dağıtık tek transaction değildir. Karşılama/parola e-postası yerel kurulumdan sonra istenir.

Telefon Keycloak attribute'ına veya Property'ye özel Person kaydına yazılmaz. Dizin liste/detayına telefon döndürülmez; hesap sahibi kendi GET account/mobile profile uçlarından okur ve mevcut version'lı profil komutuyla günceller. PHOTO rızası verilmez, roller/kişiler bu telefon nedeniyle bağlanmaz. Şema/entity/migration eklenmedi. [Kanıt](../navigation-refactor/ACCOUNT_CONTACT.md).

## BF-VERIFY02 — Opt-in e-posta politikası

`ContactVerification:EmailEnabled=false` varsayılandır. Açıldığında ortak Keycloak oluşturma adaptörü yeni hesaba VERIFY_EMAIL required-action ekler; eski hesaplar/realm genelinde toplu zorunluluk açılmaz. Yönetici normal/geçici parola oluşturması başarılı yerel kurulumdan sonra tek VERIFY_EMAIL+UPDATE_PASSWORD daveti ister. Create UserResponse ek alanı `invitationStatus=ACCEPTED|UNAVAILABLE|NOT_REQUESTED` (liste/detayda null); ACCEPTED SMTP kabulüdür. UNAVAILABLE halinde hesap oluşmuştur, tekrar oluşturulmaz; mevcut kapsamlı password-reset işlemiyle gönderim yenilenebilir. Doğrulanmış mevcut hesap resetinde yalnız UPDATE_PASSWORD, doğrulanmamışta birleşik akış kullanılır.

`canEditEmail=false` politika açıkken yönetici formunu salt okunur yapar. PUT farklı adresi BF_USER_INVALID(email)/400 ile reddeder; aynı adreste adaptör email/emailVerified alanlarını yazmaz. Hesap sahibi native UPDATE_EMAIL AIA/hesap konsolu üzerinden yeniden kimlik doğrulama ve yeni adres onayıyla değiştirir. Backend yöneticinin kapsam/rolünü korur; VERIFIED yazdıran API yok. [Faz sözleşmesi, gerçek test ve devreye alma sınırı](../refactor/contact-verification/02-report.md).
