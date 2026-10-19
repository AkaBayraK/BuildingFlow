# Mobil merkezi kişi yetki kopyası — PERSON FAZ08

`GET /api/v2/mobile/authorization`, Bearer OIDC ile salt okunur, `no-store` yanıtıdır. API'de `PersonRefactor:Enabled`, `SessionContextsEnabled`, `MobileAuthorizationEnabled` birlikte açık olmalıdır; varsayılanlar kapalıdır. Anonim401, kapalı özellik404, 500 üzeri context400 (eksik kopyayla hak açılmaz). Veri yazmaz.

Yanıt: `policyVersion:1`, UUID `snapshotId`, OIDC `userId` (subject), UTC `issuedAt/expiresAt`, `offlineReadOnly:true`, `people:[{id,tenantId,displayName}]`, FAZ07 `contexts` alanları (id/kind/tenant/property/unit/role/tarih/capability). Kişiler yalnız aktörün kendi güncel NATURAL kişi bağlarıdır; yöneticinin genel kişi listesi indirilmez. Kimlik numarası, telefon, parola, token, fotoğraf veya başka kişilerin kaydı bu sözleşmede yoktur. `SessionContextStore` aynı RepeatableRead içinde güncel issuer/subject, kişi, hesap bağı, üyelik, scope, görev ve ilişki kontrollerini kullanır; istemci rol seçimi yetki üretmez. Ayrıntı: [merkez sözleşmesi](session-workspaces.md).

Herhangi bir yönetim/platform context'i veya sıfır context varsa en fazla1saat; yalnız malik/kiracı/sakin context'leri varsa en fazla24saat. Tarihli ilişkinin bitişinden sonraki Europe/Istanbul gece yarısı daha erkense bütün kopyanın sonu kısaltılır. İlk giriş ve yeni kopya yalnız çevrimiçi Keycloak Authorization Code + S256 PKCE sonrasında alınır. Snapshot refresh token değildir; API yazma yetkisi sağlamaz.

## Yerel erişim ve geçiş

Mobil `EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED=false` varsayılanıdır. Yeni mod API başarısızsa eski mobile/session veya eski yetki kopyasına dönmez. Şifreli mevcut SQLCipher veritabanında schema8 `cache_records` payload'ına açıkça izin verilen alanlar eklenir; yeni SQLite/backend migration yoktur. Aynı hesap/issuer/API/client scope korunur. Eski outbox/draft/dosya tabloları değiştirilmez; yeni modda bunları okuyan/yazan veya sunucuya gönderen eski akışlar kapalıdır. CRUD/sync FAZ09 kapsamıdır. Bayrak geri kapatıldığında canonical kopya eski oturum yerine kabul edilmez; çevrimiçi giriş gerekir.

Cihazda kayıtlı biyometri ve mevcut SecureStore `requireAuthentication:true` ile korunmuş rastgele cihaz bağı gerekir. Kullanıcı önce çevrimiçi ekrandan biyometrik çevrimdışı erişimi etkinleştirir. Kopya SQLCipher'da, kısa hesap/scope/nonce bağı SecureStore'da `WHEN_UNLOCKED_THIS_DEVICE_ONLY` ile tutulur. Parola SQLite'a yazılmaz; mevcut refresh token saklama sözleşmesi SecureStore'da kalır. Biyometri yoksa veya iptal/anahtar geçersizleşmesi varsa yalnız çevrimiçi erişim mümkündür; düz metin veya yazılım PIN fallback yoktur. Web çevrimdışı erişim desteklemez.

Kilit açmadan önce ve sonra sunucu süresi ve SecureStore yüksek zaman işareti kontrol edilir. Saatin geri alınması erişimi reddeder. Açık ekranda süre/saat kontrolü, foreground dönüşü ve background kilidi vardır; arka planda kişi ekranı gizlenir. Bu JavaScript gizleme OS uygulama değiştirici ekran görüntüsüne karşı native garanti sayılmaz. Oturum nesli iptal edilmiş async sonucun eski hesabı yeniden göstermesini engeller. Çıkış/çevrimiçi reddedilmiş oturum mevcut refresh/cache temizliğine ek olarak biyometrik anahtar ve zaman bağını siler; temizleme başarısızlığı kullanıcıya bildirilir. Logout eski taslakları da mevcut davranış gereği temizler; mod değiştirmek taslak temizliği yapmaz.

## Sınırlar ve işletim

Çevrimdışı cihaz uzaktaki iptali anında öğrenemez: kalan lease boyunca minimum salt okunur bilgi açılabilir. Bir sonraki başarılı çevrimiçi kopya geçerli görev/ilişkileri yeniden hesaplar; kapatılmış erişim geri verilmez. Cihaz kaybında Keycloak oturumu/hesap bağı/görev kapatılmalı; uzaktan cihaz silme/MDM mevcut değildir. Root/jailbreak, OS güvenlik ihlali veya saatin sürekli dondurulmasına karşı donanımsal monoton sayaç garantisi verilmez. Token/API yetkisi sunucuda yeniden doğrulanır; yerel snapshot hiçbir yönetim değişikliği için otorite değildir.

Gerçek tenant/Property/account-link/görev hazırlığı ve FAZ09 protokol geçişi tamamlanmadan canlı bayrak açılmaz. Yeni FaceID açıklaması native binary yapılandırmasıdır; yalnız OTA/export yeterli değildir. Expo Go bu güvenlik kabulünün yerine geçmez. Bu fazda development binary üretilmedi/kurulmadı; gerçek iOS/Android SQLCipher, SecureStore, biyometri iptali/değişimi ve background davranışı cihazda DOĞRULANAMADI. Export derleme kanıtıdır, cihaz güvenliği kanıtı değildir.

Mevcut Expo57 kaynakları: [SQLCipher yapılandırması](https://docs.expo.dev/versions/v57.0.0/sdk/sqlite/), [SecureStore biyometri ve anahtar yaşam döngüsü](https://docs.expo.dev/versions/v57.0.0/sdk/securestore/). Yeni üçüncü taraf paket eklenmedi.

## FAZ09 opt-in genişletmesi

Mobil sync bayrağı ayrıca açıldığında desteklenen yerel taslak/CRUD/sync işlemleri [canonical sync sözleşmesini](mobile-canonical-sync.md) izler. `offlineReadOnly:true` yetki kopyasının salt okunur kaynak olduğunu belirtir; sunucu yazımı bu kopyaya dayanmaz, her gönderimde tekrar doğrulanır. Kişi/görev kayıtları readonly kalır. FAZ08 yalnız readonly modu sync bayrağı kapalıyken korunur.
