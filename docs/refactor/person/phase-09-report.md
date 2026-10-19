# PERSON FAZ09 — Mobil Offline CRUD ve Senkronizasyon

18.10.2026. Kullanıcı FAZ08'i kabul ederek FAZ09 başlangıcını onayladı. Önceki [#13](https://github.com/AkaBayraK/BuildingFlow/issues/13) Done; bu teslim mevcut [#14](https://github.com/AkaBayraK/BuildingFlow/issues/14) üzerindedir. Yeni görev açılmadı. FAZ10 başlatılmadı.

## İnceleme, önkoşul ve etki

Paket MASTER_PROMPT/STANDARDS/PHASES/AGENTS/PHASE09, önceki faz raporu, kök/mobil AGENTS, mobil master/faz/kalite protokolü, core mimari ve mevcut API/SQLite/outbox/profil/rıza/fotoğraf kodu incelendi. Mevcut Expo57/RN0.86/SQLCipher/SecureStore altyapısı yeterli; kurulu API ve resmî SQLite/ImagePicker kaynakları kontrol edildi. Yerel akadev phase/contract/security/verify yönergeleri tek agent ile uygulandı; alt agent kullanılmadı.

FAZ08 minimum kişi/context kopyası ve mevcut talep/profil idempotency önkoşulları hazırdır. Eksik bağlantı yeni canonical grant'lerin mevcut kuyruğa ve API'ye taşınmasıdır. Plan bu bağlantıyı mevcut servislerde küçük, opt-in değişikliklerle kurmaktır. Yeni backend domain/model/migration veya SQLite schema bump gerekmedi. Finans projeksiyonu, rol atama ve genel kişi düzenleme bu fazın kapsamına alınmadı.

## Uygulanan davranış

- Talep komutu ve pull/photo query'leri `contextKind/contextId` taşır. Canonical sunucu bunları güncel görev/ilişki, exact issuer+subject, kişi/hesap bağı, tenant/Property/bölüm ve role ile doğrular; eski realm/membership veya istemci rolü hak açmaz. ACCOUNTANT talep yazamaz. İlk kontrolün ardından tenant→Property kilitli transaction içinde kontrol tekrar edilir; replay de iptali atlamaz.
- Context store, mutasyon içinden çağrıldığında mevcut transaction'ı kullanır; tek başına oturum/snapshot okumaları RepeatableRead kalır. Profil/rıza kapısı canonical modda aktif exact issuer/subject account ve yeni session projeksiyonunu kullanır. Açık SUPER_ADMIN platform istisnası korunur.
- Canonical mobil turu önce `/api/v2/mobile/authorization` yeniler. Güncel context'i yoksa veya snapshot geçersiz/expired/downgraded ise push yapılmaz. Yeni anahtar `tenantId:kind:contextId` olur; eski scope kuyrukları sessizce dönüştürülmez veya gönderilmez.
- Mevcut transaction+outbox/fotoğraf dosyası sistemi tekrar kullanılır. CREATE talep/arıza açıklaması, kategori, öncelik ve isteğe bağlı fotoğraf; UPDATE açıklama; DELETE kaldırma isteği yerelde kaydedilir. Sunucu sürümü/UUID/makbuzu değişmez kurallarıyla kesinleşir. Yerel taslak merkezde kesinleşmiş kayıt gibi gösterilmez. Yönetim geçmişi olan talep değiştirilemez;409 kullanıcı incelemesi bekler.
- Retry/backoff,429 Retry-After,6deneme/5iş sınırı, manuel eşitleme, foreground/bağlantı yenileme ve açık uygulamada15sn otomatik eşitleme korunur. Arka planda OS işi veya uygulama kapalıyken garantili gönderim eklenmedi. Makbuz doğrulanmadan sent yapılmaz; belirsiz yanıt aynı UUID ile doğrulanır.
- İptal edilmiş canonical scope'ta okuma cache'i temizlenir, işlem blocked olur; şifreli bekleyen açıklama/fotoğraf/taslaklar korunur. Başka context'e taşıma/gönderme yapılmaz. Legacy satır/dosyalar ayrı kalır. Mevcut explicit logout anahtar/cache temizliği davranışı değişmez.
- REQUEST_PHOTO/PROFILE_PHOTO rıza, JPEG yapısı/boyut/EXIF temizliği, private object saklama ve hash kontrolü korunur. Fotoğraf opsiyoneldir; rıza verilmeden fotoğrafsız talep/profil kaydı yapılabilir. Dosya referansı transaction'ı başarısızsa yeni dosya temizlenir, eski taslak kalır.
- Uygun profil taslağı ayrı MobileProfile hesap kaydıdır. CentralPerson ve merkezi kişi kartı, görev veya Keycloak rolü değiştirilmez. Sakin kendi merkezi kişi kartını readonly görür; kişi/genel liste düzenleme açılmadı. Hesap profili mevcut version/receipt/queue/rıza akışına bağlandı.
- Ödeme/kesin tahakkuk/rol değişimi outbox action whitelist'inde yoktur. Hem yerel validator hem sunucu bu eylemleri reddeder; yeni mobil ekran bunları offline eylem olarak sunmaz. Yeni modda eski finance/manager/notices ekranları ve person-link/kayıt API akışları kapalı kalır.
- Mevcut lacivert tasarım ve bileşenlerle talep formu, kayıt/düzenleme/kaldırma, bekleyen iş/durum/manuel sync ve hesap profil ekranları yeni çalışma alanına eklendi. Kamera/galeri çağrısı sırasında form gizlilik örtüsü altında korunur; dönüşte online yeni token+API veya offline biyometri ve oturum/context nesli doğrulanır. Profil native modalında da örtü vardır. Doğrulanmamış dönüş kaydedilmez. Bu mantık gerçek cihaz testi sayılmaz.

Sözleşme, hata kodları, bayraklar ve rollback: [mobile-canonical-sync.md](../../api/mobile-canonical-sync.md).

## Bu tur doğrulama

| Kontrol | Sonuç |
| --- | --- |
| Geniş seçili gerçek PostgreSQL + fixture storage integration | **363/363**,0fail/skip |
| Dar session/mobile grubu | **50/50**,0fail/skip; son legacy-v2 testi daha sonra geniş363 içinde geçti |
| Yeni canonical HTTP senaryoları | **9** (363 içinde) |
| Backend unit | **187/187**,0fail/skip |
| Mobil `npm run check` | **126/126**,0fail/skip; TypeScript ve Expo lint başarılı |
| Yeni canonical SQLite/HTTP port senaryoları | **7** (126 içinde) |
| Expo `npm run build` | Kapalı ve açık kişi+sync bayraklarıyla iOS/Android/web export; son açık koşu teslim kodunu içerir |
| .NET solution build | **0hata/0uyarı** |
| Lisans metadata | **329** mevcut npm/NuGet sürümü PASS; yeni bağımlılık yok |
| Compose | config--quiet başarılı; servis başlatılmadı |
| Kaynak koruması | Başlangıçtaki70 manifest/proje/migration dosyası hash'i aynı; SQLite schema8 korunur |

9 HTTP: MANAGER ve RESIDENT CRUD/replay/version409/tombstone (2); iptal sonrası yeni iş/replay/pull/photo reddi; yabancı context/Property/role ve legacy kimliksiz komut reddi; ACCOUNTANT/featureoff reddi; hesap profili replay ve CentralPerson değişmezliği/pasif account reddi; rızalı fotoğraf replay/hash/IDOR/invalid içerik; concurrent duplicate tek kayıt; önceden yazılmış `request-v2` fingerprint makbuzunun replay uyumu. Profil/Consent ve eski MobileSync/Photo regresyonları geniş gruptadır.

7 mobil: immutable context binding ve finans/rol komutu reddi; snapshot öncesi push yok+yanıt kaybından sonra aynı UUID replay; revoke sonrası push yok+canonical body/legacy queue koruması; expiry/downgrade reddi;409 ve kısmi snapshot reddi;429 bounded retry; gerçek Node SQLite dosya referansı transaction rollback+fotoğraf taslağı korunması. Yerel fotoğraf fixture'ı kontrollü test verisidir; gerçek JPEG/rıza/özel storage kontrolü HTTP entegrasyonunda ayrıca doğrulanır.

Geniş filtre SessionContext, Mobile, PersonCenter/Assignment/Transition/Relation/CentralPeople/CentralPerson, People/Profile/Code/UserManagement, Authentication/ReleaseSecurity, migration/persistence/PropertyModel ve Dues/DuesParty/Payment/Report gruplarıdır; tüm backend suite iddiası yoktur. Test rolü ana projects DB'ye erişemez; geçici DB/role/schema ve fixture storage kaynakları temizlendi. Kalıcı veri/nesne değiştirilmedi. Geçici loglar `/tmp/bf-person09/` altındadır.

Ara mobil kontrolde View/Modal JSX kapanışları hata verdi; doğru wrapper/kapanışlarla düzeltildi ve son check başarılı oldu. Hatalı ara export başarı sayılmadı; son export tekrar çalıştırıldı. Gerçek telefon/biyometri/picker/background ve kullanıcı UI etkileşimi bu tur **DOĞRULANAMADI**. Node SQLite native SQLCipher, port testleri OS doğrulaması değildir. Web/frontend bu fazda değiştirilmedi; web test/build yeniden çalıştırılmadı. SAST/SCA/CVE/secret scanner, gerçek Keycloak ve production kabulü çalıştırılmadı.

## Değişen dosyalar

- Backend: `Application/MobileSync.cs`, `Api/MobileSyncEndpoints.cs`, `Infrastructure/{MobileSyncStore,MobileSessionStore,SessionContextStore}.cs`.
- Test: yeni `IntegrationTests/MobileCanonicalSyncTests.cs`, `mobile/buildingflow-mobile/tests/canonical-sync.test.mjs`.
- Mobil: `auth/{contracts.ts,AuthProvider.tsx}`, `sync/{contracts.ts,engine.ts,outbox.ts}`, `requests/drafts.ts`, `app/_layout.tsx`, `person-authorization/PersonModelScreen.tsx`, `components/{RequestForm,RequestBoard,ProfileEditor}.tsx`.
- Aktivasyon: kök/mobil env örnekleri ve compose API bayrağı. Gerçek `.env` değiştirilmedi.
- Belgeler: bu rapor, yeni canonical-sync API sözleşmesi, önceki authorization/sync belgesine geçiş bağlantısı, TESTING, STATUS/SESSION/CHANGELOG, SKILLS_AND_AGENTS ve mevcut GitHub #13/#14 durumları.

Paylaşılan dirty main checkout ve başka sohbet değişiklikleri korundu. Yeni paket/font/görsel/extension/skill yok; no install/restore, commit/push/merge/reset/stash/branch değişimi yok. Domain/EF snapshot/migration değişmedi; canlı migration apply, tenant/Property/account-link/Keycloak/rol/servis işlemi yapılmadı.

## Risk ve teslim

- Offline yetki iptali anında bilinmez; FAZ08 lease boyunca yalnız cihazda izinli yerel okuma/taslak mümkündür. Sunucu her sync'te tekrar doğrular. Cihaz kaybı/remote wipe/root/jailbreak/donanımsal saat sınırları FAZ08 ile aynıdır.
- Gerçek iOS/Android SQLCipher, SecureStore, galeri/kamera dönüşü ve OS/modal/background davranışı cihazda açık kabul işidir. Yeni binary veya mağaza yayını bu tur yapılmadı. Gizlilik örtüsü native screenshot/app-switcher güvenlik garantisi değildir.
- Legacy kuyruklar korunur fakat canonical modele otomatik taşınmaz. Yetkisi kaldırılan draft'lar başka role veya yeni ilişki kimliğine sessizce atanmaz; açıkça incelenmiş veri geçişi gerekir. Hesap logout'u mevcut davranış gereği yerel veriyi temizler.
- Mevcut mali okumaların tümünü canonical finans projeksiyonuna geçirmek bu FAZ09 talep/profil kapsamı değildir; canlı global cutover tamamlandı iddiası yoktur. Gerçek tenant/account-link/görev/scope hazırlığı ve yayın güvenliği/cihaz/regresyon kabulü tamamlanmadan canlı bayraklar açılmaz.
- Rollback sync bayraklarını kapatarak FAZ08 readonly görünümüne döner; schema downgrade/veri silme yok. Legacy çalışma için tüm session/client bayrakları birlikte uyarlanıp çevrimiçi doğrulanır.

Resmî adımlar inceleme/önkoşul/etki planı/uygulama/test-limit raporu/teslim: **6/6=%100,00 teknik teslim**. Bu tur altı adımı kapsar; cihaz/yayın kabulü yapılmış sayılmaz. Kullanıcı tarafından kabul edilmiş önceki fazlar **8/11=%72,73**. FAZ09 #14 In review, kullanıcı kabulü bekler; FAZ10 başlamaz. Diğer İŞ/UI/MOBİL sayaçları değişmedi.

Token giriş/çıkış/toplam/kalan context: **BİLGİ MEVCUT DEĞİL**. Son18.10.2026 Codexusage300dk kullanılan%52/kalan%48 reset18.10.2026 14:28:36;10080dk kullanılan%24/kalan%76 reset25.10.2026 00:15:34 Europe/Istanbul. Hesap penceresi sohbet token tüketimi değildir.
