# PERSON FAZ08 — Mobil Yerel Kişi ve Rol Modeli

18.10.2026. Kullanıcı FAZ07'den sonraki fazı ve devamını onayladı. #12 kabul/Done; mevcut [#13](https://github.com/AkaBayraK/BuildingFlow/issues/13) teknik teslim/In review. Yeni görev açılmadı. FAZ09 başlatılmadı.

## İnceleme ve uygulama

Paket MASTER_PROMPT/STANDARDS/PHASES/AGENTS/PHASE08, önceki kişi raporları, kök/mobil AGENTS ve mobil promptları, mevcut OIDC/session/SQLite/SQLCipher/SecureStore/outbox ve canonical context kodu incelendi. Önkoşul FAZ07 kabulü sağlandı. Expo57 SQLite/SecureStore resmî belgeleri ve kurulu tipler kontrol edildi. Yerel akadev phase/contract/security/verify yönergeleri tek agent ile uygulandı.

1. Minimum sunucu sözleşmesi: salt okunur `/api/v2/mobile/authorization`, policy/snapshot UUID/subject/zamanlar, yalnız kendi kişi UUID/tenant/adı ve canonical context/capability'leri. Güncel kaynak ve exact issuer/subject kontrolleri FAZ07 store'unu kullanır. Yöneticiye offline genel kişiler listesi indirilmez; sakin sadece kendi kaydını görür ve değiştiremez.
2. Süreli kopya: yönetim/platform/boş context1saat; yalnız malik/kiracı/sakin24saat; biten ilişkinin İstanbul gece yarısıyla daha erken kapanış. Yeni API flag ve mobil flag defaultfalse; diğer kişi/session bayrakları da gerekir. İlk giriş çevrimiçi Keycloak+PKCE.
3. Mevcut SQLCipher schema8 session payload'ı genişletildi. Whitelist parser parola/token/kimlik numarasını dışarıda tutar, hesap/tenant/rol/capability/UUID/tarih/tekrar/lease tutarlılığını denetler. Model/migration değiştirilmedi; eski taslak/kuyruklar mod geçişinde korunur.
4. Native offline giriş: çevrimiçi açık etkinleştirme, SecureStore korumalı cihaz bağı ve biyometri; iptal/anahtar kaybı/hesap-scope farkı/saat geri alma/lease sonu erişimi kapatır. Web veya biyometri olmayan cihaz online kalır. Yazılım PIN fallback yoktur. Arka planda sunum kilitlenir, açılışta yeniden doğrulama; uygulama açıkken lease ayrıca kapanır.
5. Yeni mod yalnız salt okunur çalışma alanı/kendi kişi görünümü açar; eski profil/notices/finance/manager/resident önbelleği ve kayıt/CRUD/outbox/sync API'lerine geçiş engellenir. Eski session'a sessiz fallback yoktur. Çıkışta refresh/cache ve cihaz anahtarı temizliği mevcut kuyruk nesil korumasıyla sıralanır.

Ayrıntılı sözleşme, aktivasyon/geri dönüş ve riskler: [mobile-person-authorization.md](../../api/mobile-person-authorization.md).

## Doğrulama

| Kontrol | Bu tur sonucu |
| --- | --- |
| Mobil `npm run check` | **119/119**,0fail/skip; TypeScript ve Expo lint başarılı |
| Yeni model/cihaz/SQLite davranışları | **11 yeni test**, yukarıdaki119 içinde |
| Expo `npm run build` | iOS/Android/web export başarılı; kapalı ve açık bayrakla ayrı koşular; son açık koşu son kodu içerir |
| Dar yeni API/session testi | **25/25** gerçek izole PostgreSQL,0fail/skip |
| Geniş seçili backend integration | **354/354**,0fail/skip, gerçek ayrı PostgreSQL + yalnız fixture storage |
| Backend unit | **187/187**,0fail/skip |
| .NET solution build | **0hata/0uyarı** |
| Lisans metadata | **329** npm/NuGet sürümü PASS; yeni paket yok |
| Compose | config--quiet başarılı; servis başlatma yok |
| Koruma | Başlangıçtaki70 proje/manifest/lock/migration dosyası hash'i aynı; yerel SQLite schema8 korunur |

11 mobil senaryo: sanitize/projection/account eşlemesi; tenant/rol/permission/tarih/duplicate reddi;1h/24h sınırı; expiry/gelecek/rollback; gerçek Node SQLite roundtrip+eski pending outbox'ın korunması; online anchor+explicit biometric enrollment; iptal/kayıp anahtar/yabancı hesap-scope/nohardware; biyometri sırasında süre dolması ve highwater; account switch+anahtar silme sırası; logout ile geç kuyruğa alınan enrollment iptali; biyometri değişiminden sonra çevrimiçi anahtar yenileme. Node SQLite testi native SQLCipher testinin yerine geçmez. Enjekte edilmiş cihaz portları işletim sistemi biyometri kabulünün yerine geçmez.

6 yeni HTTP senaryosu geniş354 grubundadır: yönetim minimum1h/no-store/yeni snapshot UUID, sakin24h ve bugünkü ilişki bitiş sınırı, görev iptalinden sonra boş kopya, yabancı hesapta boş kopya, anonim401/flagoff404. Geniş filtre SessionContext/Mobile, önceki person/transition/relation/assignment/center, people/profiles/codes/users, authentication/security, migration/persistence/property ve dues/payment/report gruplarıdır; tüm backend suite iddiası yoktur. Test rolü ana projects DB'ye erişemez; fixture kaynakları temizlendi.

İlk mobil TypeScript kontrolünde `accessibilityState.selected` boolean|null hatası boolean'a çevrildi; sonraki check geçti. İlk birim testi sandbox iletişim soketinde durdu; izinli ortamda187/187 tekrar başarılı oldu. İlk diff kontrolündeki mevcut TESTING son boş satırı, kanıt eki yazılırken düzeltildi. Başarısız ara kontroller başarı sayılmadı. Geçici loglar `/tmp/bf-person08/` altındadır; kalıcı kanıt bu rapordur.

## Dosyalar ve kapsam koruması

- Yeni backend: `Application/MobileAuthorization.cs`, `IntegrationTests/MobileAuthorizationTests.cs`.
- Backend bağlantı: `SessionContextStore.cs`, `DependencyInjection.cs`, `SessionContextEndpoints.cs`.
- Mobil yeni: `src/person-authorization/{model.ts,device-policy.ts,device.native.ts,device.ts,PersonModelScreen.tsx}`, `tests/person-authorization.test.mjs`.
- Mobil bağlantı: `auth/{contracts.ts,AuthProvider.tsx,LoginScreen.tsx}`, `offline/database.ts`, `app/_layout.tsx`, `app.json` SecureStore FaceID açıklaması.
- Bayrak örnekleri: kök ve mobil `.env.example`, `deploy/docker-compose.yml`.
- Belge: bu rapor, API sözleşmesi, TESTING, STATUS/SESSION/CHANGELOG, SKILLS_AND_AGENTS ve mevcut GitHub #13.

Paylaşılan checkout'ta diğer sohbetlerin frontend/Program/website ve önceki faz değişiklikleri korunur; başlangıçtan değişmiş bütün dosyalar bu faza ait değildir. Bu fazda frontend ürün kodu yazılmadı; web test/build tekrar edilmedi. Yeni paket/font/görsel/extension yok. Gerçek `.env`, ana DB, tenant/kişi/görev/Keycloak rol/hesap ve çalışan servis değiştirilmedi; migration uygulanmadı. Commit/push/merge/reset/stash/branch değişimi yok.

## Risk, eksik kabul ve sonraki adım

- Çevrimdışı uzaktaki iptal anında bilinmez; kalan lease boyunca yalnız minimum readonly bilgi açılabilir. Sonraki online snapshot yetkiyi yeniden hesaplar. Cihaz kaybında hesap/oturum/bağ/görev iptali gerekir; remote wipe/MDM yoktur.
- Gerçek cihazda SQLCipher/OS SecureStore/FaceID/Android biyometri, biyometri değişimi ve background ekran davranışı **DOĞRULANAMADI**. Yeni FaceID açıklaması için native development binary gerekir; bu tur oluşturulmadı/kurulmadı. JavaScript gizleme native screenshot/app-switcher koruması garantisi vermez. Root/jailbreak veya saat dondurma donanım garantisi yoktur.
- Gerçek Keycloak/production/görsel/cihaz kabulü ve SAST/SCA/CVE/secret scanner bu tur çalıştırılmadı. Mevcut lisans metadata kontrolü güvenlik taraması değildir.
- FAZ09 canonical offline CRUD/sync ve finans projeksiyonu bu teslimde yoktur. Canlı global cutover tamamlandı iddiası yoktur. Bayraklar canlı açılmadı; gerçek tenant/Property/account-link/görev hazırlığı yapılmadı.

Resmî faz adımları: inceleme, önkoşul, etki/dosya planı, uygulama, test/limit raporu, teslim **6/6=%100,00**. Bu tur bu altı adımı kapsar; teknik faz teslimidir, cihaz/yayın kabulü değildir. Kullanıcının kabul ettiği önceki fazlar **7/11=%63,64**; FAZ08 henüz kullanıcı kabulü bekler. Diğer İŞ/UI/MOBİL sayaçları değiştirilmez. Sonraki adım kullanıcı FAZ08 değerlendirmesi; FAZ09 onaysız başlamaz.

Token giriş/çıkış/toplam ve kalan context: **BİLGİ MEVCUT DEĞİL**. 18.10.2026 Codexusage son okuma:300dk kullanılan%16/kalan%84, reset18.10.2026 14:28:36;10080dk kullanılan%18/kalan%82, reset25.10.2026 00:15:34 Europe/Istanbul. Hesap penceresi bu sohbetin token tüketimi değildir.
