# BuildingFlow mobil derleme ve yayın hazırlığı

15.10.2026. Bu yönerge **hazırlıktır**; cloud build, TestFlight/Play yüklemesi veya mağaza yayını yapılmadı. Ürün sahibi üretim bilgilerini henüz sağlamadığını ve eksiklerin engel olarak bırakılmasını açıkça seçti. Güvenlik ve cihaz kabulü kapanmadan imzalı üretim derlemesine başlanmamalıdır.

## Yapılandırma

app.json geliştirme kimliklerini korur: com.buildingflow.mobile.dev / buildingflow-dev. app.config.cjs üretim profilinde onaylanan ayrı kimlik/sürüm/HTTPS değerlerini zorunlu tutar. BF_RELEASE_IOS_BUNDLE_ID, BF_RELEASE_ANDROID_PACKAGE, BF_RELEASE_VERSION, BF_RELEASE_IOS_BUILD_NUMBER, BF_RELEASE_ANDROID_VERSION_CODE, BF_RELEASE_EAS_PROJECT_ID, EXPO_PUBLIC_API_URL, EXPO_PUBLIC_AUTH_ISSUER, EXPO_PUBLIC_AUTH_CLIENT_ID, EXPO_PUBLIC_AUTH_REDIRECT_SCHEME boş bırakılamaz. AppleTeam isteğe bağlıdır; geliştirme TeamID üretime kopyalanmaz. İmzalama/hesap kontrolü ayrıca gereklidir. Üretim değerleri gelişigüzel tahmin edilmez.

Üretim URL’leri HTTPS olmalı; userinfo/query/hash, IP literal, localhost/local/test/example hedefi reddedilir. Keycloak issuer yolu /realms/buildingflow ile biter. İsteğe bağlı web adresleri verilirse onlar da kontrol edilir. Üretim paket adı .dev olamaz. iOSbuildnumber ilk grup1–9999,isteğe bağlı iki0–99grup; AndroidversionCode1–2100000000. Görünen sürüm üç parçalı sayıdır. Bunlar syntax sınırlarıdır; gerçek DNS/TLS, mağaza kaydı/sahipliği veya önceki sürüme göre artış otomatik doğrulanmaz.

Üretimde yerel dotenv yükleme kapalıdır. EAS production ortamındaki public değerler plain/sensitive görünürlükle config değerlendirmesinde erişilebilir olmalıdır; EXPO_PUBLIC değerleri pakete girer, **sır değildir**. Public adında secret/password/token/private/accesskey olan dolu alan ve native fixture opt-in reddedilir. Sertifika, .p8,keystore,service-account JSON veya clientsecret buraya konmaz. İmzalama EAS remote credential yönetimiyle daha sonra yetkili hesap sahibi tarafından yapılır. OAuth public client için clientsecret eklenmez.

## Yerel doğrulama

Komutları mobile/buildingflow-mobile içinden çalıştırın. Aşağıdaki env dosyası üretim değerleri doldurulduğunda kullanılır; şimdi örnek boştur ve ön kontrolün başarısız olması beklenir.

```sh
npm run check
npm run build
npm run release:licenses
cp -n release/production.env.example release/production.env
# Dosyayı onaylı PUBLIC değerlerle doldurun; .env içindeki LAN adreslerini kopyalamayın.
set -a
source release/production.env
set +a
npm run release:check
```

release:check yalnız okur; npm audit ağ sorgusu yapar. Eksik/bozuk config, bilinen olmayan lisans, alınamayan veya sıfır olmayan audit ve pending kabulde exit1 verir. Log yalnız alan/kod/sayı taşır. release/readiness.json altı kabul alanında passed ve boş olmayan evidence ister:deviceAcceptance,privacyAndStoreDeclarations,storeAccountsAndSigning,productionOidc,screenshotsAndBrand,qualityAndLicenses. evidence gerçek test/inceleme raporunun izidir; otomatik kanıt veya hukuki onay üretmez, içeriği/kriptografik kaynak doğrulaması yapılmaz. Bu dosyayı doldurmak mağaza onayı sağlamaz. Audit için bypass/force veya sahte sıfır rapor parametresi yoktur.

## Derleme — yalnız engeller kapatılıp kaynak yükleme yetkisi verildikten sonra

EAS CLI24.14.0 ve Node22.23.3 profillerde sabittir. appVersionSource local; autoIncrementfalse. Her gerçek yükleme öncesinde onaylı version/buildnumber/versionCode artırılır; mağazadaki son sürümle karşılaştırılır. Yeni imzalı binary’de kullanılan Xcode/AndroidSDK/EAS image, kaynak commit’i ve lockfile checksum’u teslim kaydına alınır. Şu an repo kirli ve native image/SDK ile üretim archive doğrulanmadı; bit düzeyinde tekrarlanabilir build iddiası yoktur.

```sh
# Bunlar gelecekte çalıştırılacak komutlardır; bu fazda çalıştırılmadı.
# Cloud build kaynak kodunu dış servise yükler; ayrıca yetkilendirilmelidir.
npx eas-cli@24.14.0 build --platform android --profile production
npx eas-cli@24.14.0 build --platform ios --profile production
```

Android store profili AAB; iOS store profili simulatorfalse ve cihaz IPA hedefler. İmzalama anahtarı oluşturma/hesaba bağlanma otomatik bu tur yapılmadı. EAS’ın eas-build-post-install hook’u npm install/prebuild ve iOS’ta pod install SONRASI ön kontrolü tekrar çalıştırır; bu, kaynak upload’ından önce bir güvenlik sınırı değildir. Yerel release:check cloud komutundan önce çalıştırılmalıdır. Hook’ta development açıkça üretim kabulü sayılmadan atlanır. Submit profili/auto-submit, OTA/EASUpdate veya mağaza yükleme script’i eklenmedi; üretim updatesenabledfalse.

## Kimlik doğrulama ve binary kabulü

Üretim public Keycloak client’ta tam EXPO_PUBLIC_AUTH_REDIRECT_SCHEME://auth adresini, yalnız gerekli HTTPS web originlerini, Code+S256PKCE ve API audience/rol eşlemesini kaydedin. Dev wildcard/LAN callback’i üretim client’ına taşımayın. Geliştirme callback’i buildingflow-dev://auth olarak kalır. İstemci dönüş adresi public env ile eşleşir. Gerçek yeni binary’de giriş,iptal,yenileme,çıkış,link ve soğuk açılış testi henüz yapılmadı.

Kopya/restore için AndroidallowBackupfalse ve usesCleartextTrafficfalse üretimde üretilir; iOSATS açık HTTP’ye izin vermez. AndroidSYSTEM_ALERT_WINDOW veRECORD_AUDIO manifestmerger’da remove olarak engellenir. Gerçek AAB/IPA merged permissions,entitlements,privacy manifest ve TLS ağ trafiği ayrıca incelenir. Config introspect gerçek imzalı artefakt incelemesinin yerine geçmez.

## Marka ve splash

Onaylı app-icon.png1024×1024RGB; alpha kanalı yoktur. Android adaptive foreground ve ortak mark.json geometrisi korunur. Resmi MIT expo-splash-screen57.0.10 SDK57 ile eklendi; aynı ikon #F6F9FCzemin,180genişlik/contain ile kullanılır. Görsel değiştirilmedi. Native değişiklik için yeni binary gerekir; mevcut Expo Go/devclient görünümü release splash kabulü değildir. Gerçek release coldstart,karanlıktema ve farklı ekranlarda kabul açık. Geliştirmede yeni splash’i görmek istenirse mevcut developmentenv ile expo run:ios/android kullanılır; üretim env’ini mevcut development ios/ klasöründe çalıştırmayın. CloudCNG yok sayılan ios/android klasörlerini yeni configten üretir.

## Yayın ve geri dönüş

Mağaza ekran görüntüsü/veri beyanı kontrolü: STORE_CHECKLIST.md. Gerçek kişisel veri veya yönetici sırları ekran görüntüsüne koyulmaz. İmzalı dosyanın checksum’u,paketkimliği,sürümü,permissions ve cihaz kabul kaydı saklanır. Sorunda otomatik eski binary yükleme yapılmaz; daha yüksek buildnumarasıyla test edilmiş düzeltme, mağaza kanal durdurma ve sunucu geriye uyumu değerlendirilir. Mobil DB/merkez şeması veya kullanıcı verisi silinmez. Signingkeyrotate/keystoresilme bu yönergenin otomatik parçası değildir.

Resmi kaynaklar: [EAS config](https://docs.expo.dev/eas/json/), [app config](https://docs.expo.dev/workflow/configuration/), [versioning](https://docs.expo.dev/build-reference/app-versions/), [credentials](https://docs.expo.dev/app-signing/managed-credentials/), [hooks](https://docs.expo.dev/build-reference/npm-hooks/), [SDK57 splash](https://docs.expo.dev/versions/v57.0.0/sdk/splash-screen/).
