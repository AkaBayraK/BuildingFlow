# BF-VERIFY FAZ02 — E-posta doğrulaması ve hesap açılışı

22.10.2026 · Liste3/08 · [Görev#45](https://github.com/AkaBayraK/BuildingFlow/issues/45) · **Resmi adımlar3/3=%100,00. #45 Closed(completed), Project3 Done;12/12 madde son okumada doğrulandı. BF-VERIFY3/8=%37,50.**

## Politika ve sınır

Kullanıcı D1/D2 önerisini açıkça onayladı: yeni hesaplarda e-posta doğrulaması zorunlu, telefon doğrulaması isteğe bağlı; mevcut doğrulanmış adres korunur, eski hesaplar kontrollü geçirilir. E-postasına erişemeyen hesap otomatik VERIFIED yapılmaz; yetkili destek/kurtarma gerekir. Yeni bir Person/Property yetkisi doğmaz.

`ContactVerification:EmailEnabled` **varsayılan false** (`backend/src/BuildingFlow.Api/appsettings.json`). Canlı ayar, .env, realm, kullanıcı, parola, servis ve veritabanı değiştirilmedi. Bu faz opt-in uygulamayı ve izole kabulünü teslim eder. Realm genelinde verifyEmail/defaultAction açılmaz; etkinleştirildiğinde yalnız yeni hesabın requiredActions listesine VERIFY_EMAIL yazılır. Eski hesaplara otomatik toplu işlem yok. Mobilde mevcut telefon giriş/doğrulama UX'i bu fazda değiştirilmedi; ortak kanal ekranları FAZ04/05 kapsamındadır.

## Yapılanlar / kabul eşliği

- **P02.A1:** D1/D2 kullanıcı yanıtı prompt paketinin güncel kapsamına işlendi. Eksik e-postalı veya erişimini kaybetmiş hesaplar için yeni otomatik doğrulama/kilit kaldırma API'si eklenmedi; kurtarma yetkili destek ve FAZ07 kontrollü geçişidir. Native testte eski doğrulanmamış hesabın erişimi ve mevcut proof'un korunması denendi.
- **P02.A2:** Çalışan sunucudan salt okunur `kc.sh --version` sonucu **26.7.5**. Aynı mevcut resmi imajla ayrı Keycloak kuruldu. VERIFY_EMAIL, UPDATE_EMAIL ve execute-actions-email gerçek test edildi. Bu sürümde **UPDATE_PASSWORD bağlantısını tamamlamak bağlı e-postayı da doğruluyor**; adına göre varsayılmadı. Birleşik VERIFY_EMAIL+UPDATE_PASSWORD daveti normal/geçici parola için tek posta üretti.
- **P02.A3:** `KeycloakAdminClient.CreateAsync` ortak adaptöründe opt-in required-action, dolayısıyla yönetici ve mobil oluşturma aynı politikada. `Users.cs` ancak rol/parola/yerel hesap kurulumu başarılı olduktan sonra davet ister; kurulum hatasında davet yok ve mevcut telafi korunur. Başarılı hesapta davet kesintisi `invitationStatus=UNAVAILABLE`; hesap silinmez ve kullanıcı yeniden oluşturulmaz. Mobil kayıt aynı adaptörü kullanır; ayrı ikinci davet üretmez. ACCEPTED yalnız e-posta servisi kabulüdür, sahiplik değildir.
- **P02.A4:** Doğrulanmamış yeni hesabın ilk Authorization Code/S256 girişi IdP required-action ile bekler; bağlantı tamamlanır, sonraki giriş ikinci e-posta üretmez. Birleşik davet normal/geçici parola yollarında doğrulandı. Mobil kayıt mevcut parolayı belirler; ilk giriş native VERIFY_EMAIL akışına gider.
- **P02.A5:** `contact-verification-policy.json` UPDATE_EMAIL için `verifyEmail=true`, `max_auth_age=0` içerir. Gerçek testte mevcut oturumla dahi yeniden parola istendi; yeni adres onaylanana kadar eski VERIFIED adres kaldı. Onaydan sonra `ContactNoticeFactory` yalnız commit edilmiş UPDATE_EMAIL olayında eski adrese TR/EN bildirim yollar; mesaj kod/token/yeni adres içermez. Tekrar link kullanımında ikinci bildirim yok.
- **P02.A6:** `UserService.UpdateAsync` farklı e-posta yazımını politika açıkken reddeder. Adaptör ayrıca güncel adresi okur, farklı adresi reddeder ve aynı adresi PUT gövdesinden çıkarır; aradaki değişiklik eski değere döndürülmez. Yalnız ad/rol/etkinlik düzenlemesi mevcut kanıtı bozmadan sürer. Yönetici davet yenilemesi mevcut Scope/Editable denetimindedir; doğrulanmış işareti veren API yok. UserResponse.canEditEmail, web formunda salt okunur alan/açıklama sağlar.
- **P02.A7:** JWT issuer/audience/signature/expiry/anonim negatifleri; native eski JWT adresi ile güncel IdP ayrımı; tüketilmiş/süresi dolmuş bağlantı, yeniden gönderim, SMTP kesintisi, güvenli web/mobil redirect ve eksik PKCE negatifleri geçti. Test günlüklerinde fixture e-postaları, parola, secret, JWT ve action link işaretleri bulunmadı. Test hataları yalnız aşama/satır/tür yazar; ham yanıt veya link yazmaz.
- **P02.A8:** Gerçek Keycloak→izole Mailpit SMTP teslimi kanıtlandı; bu gerçek kullanıcı posta kutusu/üretim teslimi değildir. Test yalnız cached Keycloak26.7.5 ve Mailpit1.27.10, kendi geçici ağ/konteyner/env dosyalarını kullandı ve temizledi. Harici alıcıya e-posta/SMS gönderilmedi.

## Kaynaklar

- Backend: `backend/src/BuildingFlow.Application/Users.cs`, `backend/src/BuildingFlow.Infrastructure/KeycloakAdminClient.cs`, `DependencyInjection.cs`, API `appsettings.json`.
- Web: `frontend/buildingflow-web/src/features/users/{model.ts,UsersWorkspace.tsx}`, TR/EN sözlükleri ve `tests/users.test.mjs`. Davet hatası ortak warning; başarılı gönderim sahiplik sonucu diye gösterilmez.
- Projeye ait Keycloak kaynakları: `deploy/keycloak/contact-notice/`, `themes/buildingflow/email/`, `contact-verification-policy.json`.
- Tekrarlanabilir araçlar: `scripts/build-contact-notice.sh`, `scripts/test-contact-identity.py`.
- Testler: `EmailVerificationPolicyTests.cs` (UnitTests), `EmailIdentityClientTests.cs` (IntegrationTests).

## Bu tur testleri

| Kontrol | Gerçek sonuç |
| --- | --- |
| `python3 scripts/test-contact-identity.py` | **12/12 gerçek IdP/SMTP kabul grubu**, otomatik geçici kaynak temizliği |
| `dotnet test backend/tests/BuildingFlow.UnitTests/BuildingFlow.UnitTests.csproj --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~EmailVerificationPolicyTests` | **8/8**,0 atlanan |
| `dotnet test backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj --no-restore --no-build --filter 'FullyQualifiedName~EmailIdentityClientTests\|FullyQualifiedName~AuthenticationTests\|FullyQualifiedName~KeycloakClientCachesToken\|FullyQualifiedName~KeycloakRoleChanges'` | **15/15**,0 atlanan; adaptör mockları ve gerçek HTTP JWT testleri ayrı niteliktedir |
| Web `node --test tests/{users,navigation,security,standards}.test.mjs` | **20/20**,0 atlanan |
| Web `npm run typecheck`, `npm run build` | **GEÇTİ** |
| Mobil `node --test tests/{environment,registration,release}.test.mjs` | **19/19**,0 atlanan; mobil kaynak değişmedi |
| `dotnet build backend/BuildingFlow.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --verbosity minimal` | **0 hata/0 uyarı** |
| Java `javac --release 21` mevcut API jar'ları; shell/Python/JSON ve ilgili diff/bağlantı kontrolleri | **GEÇTİ** |

İlk .NET testinde sandbox soketi engellendi; izinli tekrar geçti. Native test tarayıcısında localhost Secure-cookie ve OAuth hata redirect davranışları düzeltildi. Bildirim oturumunda realm, hesap okunmadan önce kuruldu. Geçmeyen ara denemeler başarılı sayılmadı; yukarıdaki sonuç son çalıştırmalardır.

## Operasyon ve kalan fazlar

Canlı devreye alma **FAZ07**: mevcut realm/client ayarlarını koruyarak provider jar/tema kurulumu; UPDATE_EMAIL force verify/max-age yapılandırması; SMTP ve güvenli redirect/PKCE kontrolü; ardından API EmailEnabled opt-in. JSON bir politika referansıdır, mevcut realm üstüne körlemesine import edilmez; eventsListeners mevcut listeyle birleştirilir. Eski hesaplar/administratörler için kurtarma provası ve aşamalı onay gerekir. Canlı enforcement/üretim posta teslimi bu fazın sonucu değildir.

Eski adrese bildirim SMTP ile DB arasında atomik değildir. Başarısız bildirim güvenli `BF_CONTACT_NOTICE_UNAVAILABLE` olayı üretir; kalıcı bildirim retry/outbox eklenmedi. Onaylanmış adres değişikliği bildirim hatası yüzünden geri alınmaz. Üretimde bu olayın izlenmesi ve kurtarma prosedürü FAZ07 kapısıdır.

Yeni paket/SDK/font/ikon/görsel/imaj/sürüm yok; mevcut JDK/Keycloak API'leri kullanıldı. Jar yalnız projeye ait sınıfları içerir; üçüncü taraf jar paketlenmez. LICENSE/NOTICE korunur; restore/install ve envanter yeniden üretimi yok. Uygulama entity/şema/migration değişmedi; PostgreSQL şema testleri bu fazda tekrar çalıştırılmadı. Tam depo testi, gerçek mobil cihaz/native UI, harici SMTP posta kutusu, SMS ve üretim kabulü yapılmadı. Eşzamanlı değişiklikler/index korundu; bu çalışma commit/push/deploy yapmadı.

[Keycloak resmi yönetim kılavuzu](https://www.keycloak.org/docs/26.7.0/server_admin/index.html) ve [26.7.5 native handler](https://github.com/keycloak/keycloak/blob/26.7.5/services/src/main/java/org/keycloak/authentication/actiontoken/execactions/ExecuteActionsActionTokenHandler.java) incelendi; kabul sonucu kurulu sürümde gerçek testtir.


Son kota okuması: Codex usage /22.10.2026. 300dk kullanılan%98/kalan%2, reset22.10.2026 22:19:52;10080dk kullanılan%93/kalan%7, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.
Giriş token: BİLGİ MEVCUT DEĞİL.
Çıkış token: BİLGİ MEVCUT DEĞİL.
Toplam token: BİLGİ MEVCUT DEĞİL.
Kalan context: BİLGİ MEVCUT DEĞİL.
