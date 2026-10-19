# PERSON FAZ09 — Canonical mobil offline CRUD / sync

Mevcut [mobil sync](mobile-sync.md), [profil](mobile-profile.md) ve [minimum yetki kopyası](mobile-person-authorization.md) korunarak opt-in genişletilmiştir. Yeni tablo/migration/paket yoktur. Merkezi Person, görev ve kesin finans kayıtları bu outbox'ın yazım hedefi değildir.

## Etkinleştirme ve uyumluluk

Sunucuda `PersonRefactor:Enabled`, `SessionContextsEnabled`, `MobileAuthorizationEnabled`, `MobileSyncEnabled`; mobilde `EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED` ve `EXPO_PUBLIC_PERSON_SYNC_ENABLED` birlikte açılır. Örneklerde yeni bayraklar false'dur; gerçek ortam değiştirilmedi. Yalnız PERSON yetki bayrağı açık, sync bayrağı kapalı istemci FAZ08 readonly görünümünde kalır. Canonical istek, sunucuda eski modele geri düşmez.

`SessionContextsEnabled=true` olduğunda eski mobil session/profil/rıza kapısı da canonical hesap ve kaynaklardan okunur; diğer üç API bayrağı eksikse403 verir. Böylece eski realm rolü/PropertyMembership yeni mobil grant'i yerine geçmez. Bayrak kapalı legacy kurulum eski davranışını sürdürür. Bu kapsam merkez/web sözleşmesini değiştirmez; yeni mobilde eski finans/manager/notices ekranları açılmaz. Mevcut mali API'lerin tümünü yeniden modelleme veya canlı global geçiş tamamlandı iddiası yoktur.

Canonical istemci mevcut SQLCipher schema8 tablolarda `tenantId:kind:contextId` scope anahtarı kullanır. Eski `propertyId:unitId-or-dash:role` satırları ayrı kalır; otomatik yeniden anahtarlama veya yeniden gönderme yoktur. Session payload'ı güncellenirken eski outbox/draft/dosyalar silinmez. Legacy sürümdeki `request-v2` ve en eski makbuz fingerprint biçimleri korunur; yeni canonical komut `request-v3` fingerprint'i taşır. Profil kendi account'ına ait olduğundan mevcut account kuyruğu/idempotency sözleşmesi aynıdır.

## Talep uçları

`POST /api/v1/mobile/sync/commands` mevcut alanlara opsiyonel `contextKind` ve UUID `contextId` ekler. Canonical sunucu modunda ikisi zorunlu, eşleşen aktif context şarttır; legacy modda bu alanlar reddedilir. Diğer alanlar operationId/recordId/propertyId/unitId/role/action/expectedVersion/description/category/priority/photoAction/photoBase64'tür. Kinds PLATFORM/MANAGEMENT/OWNER/TENANT/RESIDENT; role+Property+Unit aynı context ile eşleşmelidir. ACCOUNTANT talep işlemi yapamaz. İstemcinin seçtiği rol/UUID hak üretmez.

CREATE version0; UPDATE/DELETE >=1; aynı UUID/fingerprint özgün makbuzu döndürür, farklı içerik/eski sürüm409. Yönetim geçmişi olan talep değiştirilemez. İşlem sahipliği subject ve kaynak scope ile kontrol edilir. Canonical yetki ilk kontrolün ardından transaction içinde tenant→Property parent kilitleri altında tekrar doğrulanır; replay de iptal edilmiş grant'i atlayamaz. Context store mevcut transaction'ı kullanır; bağımsız snapshot okumaları RepeatableRead kalır.

`GET /api/v1/mobile/sync/requests?propertyId=...&role=...&unitId=...&contextKind=...&contextId=...` aynı güncel kaynak kontrolünü yapar. Canonical `contextKey=tenantId:kind:contextId`; actor yalnız kendi talep kayıtları/tombstone'larını alır.1000 sınırı/hasMore, sürüm ve sahiplik denetimi korunur; kısmi liste tam snapshot diye uygulanmaz.

`GET /api/v1/mobile/sync/requests/{id}/photo?contextKind=...&contextId=...` kendi fotoğrafında context'i yeniden doğrular. Başkasının fotoğrafını merkezden okuyan yönetici için ayrıca güncel Property MANAGER capability gerekir; membership/realm rolü tek başına yetmez. Diğer sahip/tenant/Property erişimi reddedilir. Mevcut REQUEST_PHOTO açık rıza, JPEG imza/yapı/boyut/EXIF temizliği, private object/hash doğrulaması değişmez. Yeni veri tabanı fotoğraf BLOB'u veya dış URL yükleme eklenmedi.

Anonim401; yetki/kapsam/yabancı hesap403; malformed400; version/fingerprint409; JSON/MIME415, büyük gövde413. POST üst sınırı400000byte; JPEG en fazla256KiB. Yanıtlar mevcut güvenli ProblemDetails ve no-store sözleşmesini kullanır. UUID, içerik, token ve fotoğraf loglanmaz.

## Cihaz işlemleri ve durum

Talep/arıza açıklaması+kategori+öncelik+isteğe bağlı fotoğraf yerel şifreli taslak olarak saklanır. Kuyruğa alma SQLite transaction'ı fotoğraf referansı ile atomiktir; işlem başarısızsa yeni dosya temizlenir, eski taslak korunur. Gönderim UUID'si retry sırasında değişmez. Sunucudan makbuz gelmeden yalnız bekleyen taslak gösterilir; merkez listesindeki kaydı yerel değişiklik kesinleşmiş gibi değiştirmez. Güncel talep açıklaması UPDATE, kaldırma DELETE olarak version ile kuyruğa alınabilir; çakışmada otomatik üzerine yazma yoktur.

Manuel eşitleme, foreground/bağlantı değişimi ve açık uygulamadaki15sn otomatik eşitleme mevcut motoru kullanır. Arka planda OS işi veya kapalı uygulamada garantili gönderim eklenmedi. Her tur önce yeni `/api/v2/mobile/authorization` alır; aktör/context yeniden doğrulanır. Sıra en çok5 iş; bounded exponential backoff,429 Retry-After ve6deneme sınırı korunur. Durumlar bekliyor/gönderiliyor/retry/conflict/blocked/failed/sent; gönderim belirsizse aynı UUID ile doğrulanır.401 oturumu kapatır;403/404 yetkiyi bloklar;409 kullanıcı incelemesi bekler. Makbuz doğrulanmadan sent yapılmaz.

Yetkisi kaldırılan canonical scope'ta okuma cache'i temizlenir, pending işlemler blocked olur. Şifreli bekleyen açıklama/fotoğraf/taslaklar korunur; başka context'e otomatik taşınmaz veya gönderilmez. Yetkisiz scope ekranı bu içeriği açmaz. Kullanıcının açık logout işlemi mevcut hesap cache/anahtar temizliğini uygular. Legacy scope'lar bu canonical temizliğin dışında kalır. Yerel lease süresi dolunca taslak yazımı da kapanır; süre sunucu yazım yetkisi değildir.

Hesap profili kendi ad/soyad/telefon/isteğe bağlı fotoğrafının ayrı MobileProfile kaydıdır; CentralPerson/PersonUserAccount/ManagementAssignment veya Keycloak rolü değiştirilmez. Mevcut profil kuyruğu/version/makbuz, rıza ve conflict akışı kullanılır. Canonical profil/rıza yetkisinde exact issuer+subject, aktif canonical account (açık SUPER_ADMIN istisnası) ve güncel session projeksiyonu gerekir. Merkezi kişi kartı tüm sakinlerde readonly kalır. Ödeme/kesin tahakkuk/rol değişikliği outbox action whitelist'inde yoktur; böyle bir komut400 ve yerelde validator reddidir.

Fotoğraf seçici sırasında form gizlilik örtüsü altında mounted kalır. Seçici dönüşünde online token+sunucu yetkisi veya offline cihaz biyometrisi yeniden kontrol edilir; hesap/context/oturum değişmişse dönüş reddedilir. Diğer background geçişleri normal kilit politikasını izler. Gerçek iOS/Android picker/biyometri/background kabulü yapılmadı; bu uygulama mantığı OS ekran görüntüsü garantisi sayılmaz.

## Geri dönüş ve sınırlar

FAZ09 rollback: istemci sync bayrağını kapatıp ilgili binary/bundle'ı yeniden hazırlayın, API MobileSyncEnabled kapalı tutulsun; FAZ08 readonly kopyası kalır, schema downgrade yoktur. Eski mobil çalışma için bütün canonical session bayrağı ve istemci kişi bayrağı uyumlu kapatılır; çevrimiçi giriş gerekir. Legacy kuyruğu canonical kimliğe otomatik geçirme ayrı, gözden geçirilmiş operasyon gerektirir. Bu faz böyle bir canlı veri işlemi yapmadı.

FAZ08 lease/remote revoke/cihaz kaybı riskleri devam eder. Native SQLCipher/OS SecureStore, uçak modu gerçek cihaz, FaceID/Android picker dönüşleri ve üretim Keycloak kabulü DOĞRULANAMADI. Kontrol edilen Expo57 kaynakları: [SQLite](https://docs.expo.dev/versions/v57.0.0/sdk/sqlite/) ve [ImagePicker](https://docs.expo.dev/versions/v57.0.0/sdk/imagepicker/). Test kanıtı [phase-09-report.md](../refactor/person/phase-09-report.md).
