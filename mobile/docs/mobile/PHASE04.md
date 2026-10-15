# Faz 04 — Kimlik doğrulama, oturum ve rol/mülk bağlamı

13.10.2026. Kullanıcı Faz 04'e ve bunun gerektirdiği backend/Keycloak genişletmesine açıkça onay verdi. Faz 05 başlamadı.

## Uygulama

- `mobile/src/auth`: OIDC Authorization Code + S256 PKCE; tarayıcı üzerinde Türkçe Keycloak girişi, vazgeçme ve güvenli hata/yeniden deneme. Client secret ve password grant yok. Tokenı decode ederek rol verilmez; backend'in imza doğrulamasından sonra `/api/v1/mobile/session` yanıtı kullanılır.
- Access token yalnız bellekte; native refresh token SecureStore `WHEN_UNLOCKED_THIS_DEVICE_ONLY` deposunda. Web refresh/access token yalnız bellekte; sayfa yenilemesinde yeniden giriş gerekir. SQLite/AsyncStorage/localStorage/sessionStorage token deposu olarak kullanılmaz.
- Açılışta native refresh + API bağlam doğrulaması. Süre bitmeden yenileme; uygulama foreground'a döndüğünde bağlam tekrar doğrulanır. İstek yarışlarında generation kontrolü; hesap değişimi/çıkış eski yanıtı kabul etmez. Yenileme başarısızsa hassas içerik gizlenir ve tekrar giriş istenir. Offline veri temeli Faz 05 olduğundan şu anda oturum doğrulanamayan cihazda iş verisi gösterilmez.
- Çıkışta yerel anahtar ve bellek temizlenir; Keycloak logout endpoint'i refresh token ile POST edilir. Sunucu erişilemezse yerel çıkış ile sunucu oturumunun kapatılması ayrılır ve mesaj gösterilir. Sunucuda JWT access tokenlarının anlık global revocation garantisi verilmez; sonraki yetkili isteklerde mevcut backend kontrolleri uygulanır.
- Ana ekrandaki Faz 03 finans kısayolu kaldırıldı. Giriş öncesi login ekranı; giriş sonrası gerçek hesap bağlamına göre ana ekran. Profilde yalnız API'nin döndürdüğü rol/mülk seçenekleri; üstte seçili bağlam kısayolu. Borç/ödeme/talep verisi ilgili sonraki fazlarda eklenecek.

## Backend sözleşmesi

Yeni `GET /api/v1/mobile/session`: `{userId,username,contexts:[{propertyId,propertyName,unitId,unitNumber,role}],hasMore}`. Anonim veya sub'sız istek 401. Boş contexts geçerli hesabın aktif bağlantısı olmadığını belirtir. Sabit üst limit 500; `hasMore=true` mobilde açık hata olarak ele alınır, eksik listeyle seçim yapılmaz.

- SUPER_ADMIN mevcut backend kapsamıyla tüm aktif mülkleri; SITE_ADMIN/MANAGER/ACCOUNTANT yalnız aktif üyelik mülklerini alır. Muhasebe rolü yönetici rolüne dönüştürülmez.
- OWNER/TENANT/RESIDENT, hesabın `Person.LinkedUserId` bağlantısı + aktif açık PropertyMembership + aktif kişi/daire/bina + Europe/Istanbul bugün dahil geçerli UnitOccupancy üzerinden türetilir. Başka hesaba veya mülke ait veri yok; soft-delete filtreleri korunur. Bu roller mevcut yönetici endpoint'lerini kullanma izni vermez.
- Domain/şema/migration değişikliği yok. Yeni Application sözleşmesi + Infrastructure read store + API endpoint; DI kaydı. DevelopmentIssuers yalnız Development'ta açık allowlist, üretim issuer güvenliği korunur.

## Keycloak ve çalıştırma

`buildingflow-mobile`: public client, S256, standard flow; implicit/password/service account grant kapalı, `buildingflow-api` audience mapper. `basic` default scope zorunlu; sub claim bu scope'tan gelir. Mevcut istemciler, roller ve kullanıcı parolaları değiştirilmedi. Keycloak REST client update default scope ilişkisini güncellemediği için betik basic scope'u ayrıca default-client-scopes endpoint'inden bağlar.

`mobile/scripts/keycloak-client.dev.json`, `configure-keycloak-mobile.sh`: tekrarlanabilir yalnız mobil development istemcisi kurulumu; admin bilgileri çalışan Keycloak konteynerinde kalır, dosyaya veya çıktıya kopyalanmaz. Development access token süresi 60 saniyedir; yenileme davranışını görünür kılar, üretim ayarı değildir.

`mobile/scripts/mobile-api.sh`: mevcut ana API'yi durdurmadan port5006, LAN bind, explicit Development issuer ve loopback web CORS. AutoMigrate kapalı; kullanıcı DB şeması değiştirilmez.

```sh
cd mobile
bash scripts/configure-keycloak-mobile.sh
BUILDINGFLOW_MOBILE_HOST=192.168.1.104 bash scripts/mobile-api.sh
# Ayrı terminal:
npm run start -- --lan --port 8081
```

Yerel `.env` gitignored; telefon API/issuer adresi bilgisayarın LAN IP'sidir. Web için ayrı localhost adresleri vardır. IP değişirse bu public adresler ve API DevelopmentIssuers ayarı birlikte güncellenir. Üretimde HTTPS gerekir; development HTTP ayarları production bundle'da reddedilir. 8762 Faz 02 prototipi, 8763 eski statik export olabilir; güncel geliştirme uygulaması `http://localhost:8081`.

## Telefonda OAuth

Expo Go özel OAuth scheme'i desteklemediğinden giriş düğmesi açıklamayla kapalıdır. Ekran incelemek için Expo Go kullanılabilir; güvenli giriş için development build gerekir. `expo-dev-client` eklendi, development paket kimlikleri `com.buildingflow.mobile.dev`, scheme `buildingflow-dev`.

```sh
cd mobile
npx expo run:ios       # Xcode ve uygun simulator gerekir
npx expo run:android   # Android SDK/emulator gerekir
# Telefona kurulu development build ile:
npm run start -- --dev-client --lan --port 8081
```

Fiziksel iOS cihazında imzalama/cihaz seçimi ayrıca gerekir; Apple hesabı veya mağaza yüklemesi bu tur yapılmadı. Dev paket kimliği üretim mağaza kimliği değildir. Native OAuth ve SecureStore gerçek cihaz kabulü henüz yapılmadı.

## Doğrulama

- `npm run check`: TypeScript/lint ve 8/8 test. Ortam URL güvenliği, yabancı bağlam seçimi, bilinmeyen rol, owner unit zorunluluğu ve truncated session reddi.
- `npm run build`: iOS/Android/Web export başarılı; imzalı native binary değildir.
- `npx expo-doctor`: 21/21 başarılı.
- Backend build: 0 hata/uyarı. İzole PostgreSQL MobileSessionTests + AuthenticationTests + ReleaseSecurityTests: 45/45, atlama0. Başka hesap, signed/spoofed/client rol, pasif üyelik/daire, bitmiş ilişki ve JWT/üretim güvenliği kapsamı. Tüm backend regresyonu yeniden çalıştırılmadı.
- `check-licenses.py`: mevcut backend/web 331 paket lisans kontrolü geçti. Mobil paket lisans metadata envanteri DEPENDENCIES.tsv içinde; yeni Expo paketleri MIT. Önceki 28 npm audit uyarısı (18 high/10 moderate) devam ediyor; bu faz onları giderilmiş saymaz.
- Chrome'da geçici fixture hesapla gerçek PKCE code exchange + API session + girişli boş mülk durumu doğrulandı. Profil → çıkış → tekrar tam kullanıcı adı/şifre formu doğrulandı. Gerçek kullanıcı parolası veya finans yazımı yok.

- Kısa development token süresiyle otomatik refresh doğrulandı: test hesabının lastAccess1791898986000 →1791899076000 ilerledi, uygulama girişli kaldı. Ardından yalnız fixture'ın sunucu oturumu sonlandırıldı; sonraki yenilemede uygulama login ekranına döndü ve eski bağlamı gizledi.
- Chrome girişli ana ekran390×844 ve412×915: yatay taşma0. `phase04-home.png`; iptal/login `phase04-login.png`. Bu ölçü kontrolü native cihaz kabulü değildir. Giriş sırasında Vazgeç popup'ı kapattı ve iptal mesajı geldi. Çıkış sonrası tekrar girişte tam kullanıcı adı/şifre formu görüldü.
- Geçici Keycloak fixture hesabı ve yerel kimlik bilgisi dosyaları temizlendi. Kullanıcı hesapları/parolaları değiştirilmedi; native cihazdaki refresh deposu, restore ve hesap geçişi henüz gerçek cihazda test edilmedi.
- Mevcut web/API5005 korunur; mobil API5006 ve Metro8081 geliştirme süreçleri açık. Commit/push yapılmadı. Native binary üretilmedi/mağazaya yüklenmedi. Girişli tarayıcı testi boş mülk fixture'ıyla yapıldı; çoklu bağlam selector UI kabulü ve native OAuth/SecureStore kabulü gerçek development build ile devam etmeli.

Faz 04 kod ve tarayıcı/API doğrulamaları tamamlandı. Native cihaz kabulü açık sınırlamadır; Faz 05 başlatılmadı.

## Kullanıcı görsel referansı — 13.10.2026

Kullanıcı BuildingFlow Mobil Uygulama Tanıtımı.png görselini verdi ve tüm sonraki fazlarda dikkate alınmasını istedi. DESIGN_REFERENCE.md ve mobil/proje AGENTS talimatlarına işlendi. Mevcut login lacivert karşılama alanı, rol etiketleri, daha güçlü tipografi ve özellik ikonlarıyla yenilendi; alt menü Ionicons çizgi ikonlarına geçti. Gövde/ikon düzeni320/390px genişlikte taşmasız; dekoratif ikonlar AX ağacından gizlendi. Ekran kanıtı phase04-reference.png.

Son UI değişimi sonrası TypeScript/lint8/8 ve iOS/Android/Web export geçti. Mobil690 üst seviye paket lisans metadata envanteri belirsiz0; @expo/vector-icons ve Expo font MIT. Native simulator build ve ekran kabulü bu devam turunda ayrıca yürütülüyor; sonuç doğrulanmadan başarı sayılmaz.

### iOS native build sonucu

`npx expo run:ios --device F8793F16-3DDA-441C-8478-EB361FDC43C1 --no-bundler`: **Build Succeeded**, hata0/uyarı1. CocoaPods kuruldu, iPhone17Pro simülatörüne `com.buildingflow.mobile.dev` yüklendi ve development-client açıldı. Uyarı Expo Dev Launcher script dependency analysis ile ilgili; native kaynaklar elle değiştirilmedi. `ios/` Expo tarafından üretildi ve gitignored.

Simülatör arayüzü bu oturumun CUA araçlarında erişilebilir uygulama olarak bulunamadı. Bu yüzden native login/SecureStore/restore görsel etkileşim kabulü doğrulanmış sayılmaz; fiziksel cihaz ve Android native testleri de açık. Build/install başarıları bu UI kabulünün yerine geçmez.

Devam sırasında LAN adresi192.168.1.104→192.168.1.6 değişti. Gitignored mobile/.env ve çalışan mobil API issuer allowlist güncellendi; Metro8081 development-client modunda yeni IP ile yeniden açıldı. Tekrarlanabilir betikte `BUILDINGFLOW_MOBILE_HOST` güncel IP olarak verilmelidir. API5006 ve mevcut ana API5005 ayrı kalır.

### Fiziksel iPhone açılış düzeltmesi — 13.10.2026

Apple Development imzalama zincirindeki eksik WWDR G3 ara sertifikası resmi Apple kaynağından tamamlandı; özel güven istisnası veya parola değişikliği yapılmadı. İmzalı development build fiziksel iPhone'a kuruldu. Cihazdaki geliştirici güveni tamamlandıktan sonra açılışta SIGTRAP oluştu. LLDB, UIKit `UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption` içinde EXC_BREAKPOINT gösterdi.

Expo SDK57.0.28 için resmi `expo-build-properties`57.0.22 (MIT) eklendi ve `ios.enableSceneSupport:true` etkinleştirildi. Expo prebuild AppDelegate ve scene manifestini üretti; native kaynaklar elle düzenlenmedi. Fiziksel build yeniden başarılı: hata0/uyarı1. TypeScript ve lint geçti. Yeniden kurulan uygulama development-client bağlantısıyla açıldı, Metro'dan iOS bundle'ını aldı ve ReactInstance JavaScript değerlendirmesine ulaştı; önceki açılış çökmesi tekrarlanmadı. Kullanıcının ekrandan giriş/OAuth/SecureStore/restore kabulü hâlâ ayrıca doğrulanmalıdır. Faz05 başlatılmadı.

Kaynak: https://docs.expo.dev/versions/v57.0.0/sdk/build-properties/#pluginconfigtypeios

### Fiziksel Safari giriş adresi

Keycloak discovery native LAN isteğinde authorization endpointini localhost olarak döndürüyordu; giriş formu/tema kaynakları da localhost'a bağlıydı. Ortak yerel geliştirme Compose dosyasında sabit KC_HOSTNAME kaldırıldı, KC_HOSTNAME_STRICT=false ve KC_HOSTNAME_BACKCHANNEL_DYNAMIC=false ayarlandı; servis yeniden başlatıldı. Bu yalnız güvenilen yerel geliştirme ağı içindir; üretimde sabit HTTPS hostname ve host doğrulaması korunmalıdır. Önceki Compose kopyası /tmp/buildingflow-keycloak-compose-before-mobile.yaml konumunda saklandı.

Doğrulama: LAN discovery issuer/auth/token192.168.1.6:8080; localhost discovery issuer/auth localhost:8080; LAN login formu ve kaynak adreslerinde localhost yok. iPhone uygulaması yeni discovery alması için yeniden açıldı. Gerçek cihazdaki kullanıcı adı/parola ve OAuth dönüşünün kullanıcı tarafından tamamlanması bekleniyor. Kaynak: https://www.keycloak.org/server/hostname

### Ekran tasarımı revizyonu — 14.10.2026

Kullanıcının mevcut ekranları beğenmediği geri bildirimi üzerine Faz04 içindeki ekran kabuğu yeniden düzenlendi. Ana sayfada lacivert karşılama başlığı, kullanıcı baş harfi, dört renkli kısayol, gerçek yetkili mülk adı/rolünü taşıyan kart ve günlük yaşam bağlantıları var. Bina görseli dekoratif çizgi ikonudur; gerçek bina fotoğrafı olarak sunulmaz. Profilde hesap kartı, seçili mülk/rol durumu ve çıkış satırı yenilendi. Alt menü, tipografi ve boşluklar ortak tasarıma alındı; girişli native durum çubuğu açık renk kullanır.

Ödemeler, talepler ve duyurular tamamlanmamış modüller olarak açıkça belirtilir; eski yanlış “bağlı mülk yok” mesajı ve kullanıcı akışındaki faz numaraları kaldırıldı. İş verisi, ödeme/talep gönderimi veya yeni faz özelliği eklenmedi.

Doğrulama: gerçek mevcut geliştirme hesabıyla Chrome PKCE giriş/API oturumu, tab geçişleri ve mevcut rol seçimi çalıştı.320px ve430px ekranlarda yatay taşma yok;390px ana sayfa ayrıca görsel kontrol edildi. TypeScript/lint ve8/8 mevcut test geçti; iOS/Android/Web export başarılı. Son durum çubuğu düzenlemesi sonrası TypeScript/lint tekrar geçti. Bunlar native cihaz görsel kabulü yerine geçmez. Kanıt: phase04-redesign-home.png, phase04-redesign-profile.png. Yeni bağımlılık eklenmedi, sonraki faz başlatılmadı.

## Kullanıcı faz onayı — 14.10.2026

Kullanıcı Faz04'ü onaylayarak Faz05'in başlamasını istedi. Faz05 raporu PHASE05.md içindedir. Önceki native UI test sınırlamaları, kullanıcının faz onayından ayrı teknik kayıt olarak korunur.
