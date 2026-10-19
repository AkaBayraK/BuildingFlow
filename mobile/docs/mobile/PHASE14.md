# Mobil Faz 14 — Güvenlik, erişilebilirlik ve performans

15.10.2026. Kullanıcı Faz14’e devamı onayladı. Yerel `main`; önceki Faz12–13, kişi kodu ve merkez güvenlik/konsol değişiklikleri korunur. Kanonik kapsam `mobile/buildingflow-mobile-prompts/PHASES.md`. Faz15 başlamadı. Commit/push/mağaza yayını yok.

Bu tur dört geliştirme işi: mevcut sınırlar ve audit; dar kod düzeltmeleri; hedefli otomatik doğrulama; belge/GitHub kaydı. Kod teslimi tamamlandı; gerçek cihaz, upstream bağımlılık ve ürün kabulü açıktır. **In review**, yayın onayı değildir.

## Bulgular ve uygulanan düzeltmeler

| Kimlik | Öncelik / kanıt | Sonuç |
| --- | --- | --- |
| BF14-01 | P1, useScopedRead: ağ/yanıt hatasında cache güncel görünüyordu | Cache/server kaynağı ayrıldı. Eski kopya sunucu doğrulaması ve kayıt bitene kadar kopya olarak gösterilir; ağ veya bozuk200’de eski kopya etiketi kalır. 401/403 görünümü boşaltıp scoped kopyayı temizler. Her async sınırında aktif hesap/bağlam kontrolü; 5 senaryo testi. |
| BF14-02 | P2, summary ve refreshSyncView tüm geçmiş payloadlarını okuyordu | SQL SUM/COUNT özeti ve ayrı `visibleJobs LIMIT10` sorgusu. Ekran yalnız aktif işleri alır; sent/cancelled geçmişi ve fotoğraf metadata payloadı belleğe taşınmaz. Büyük fixture 10.000×2.000karakter, hesap/bağlam ayrımı, toplamların doğruluğu ve getAll kullanılmaması testli. Eski işlemler silinmez. |
| BF14-03 | P1, native Router’a ham bozuk/aşırı URL | `+native-intent` ön kontrolü4096karakter, kontrol/boşluk karakteri ve decodeURIComponent ile bozuk percent/UTF8 reddi uygular. Hata sabit invalid-link ekranına gider; URL, code/token veya ham hata loglanmaz. OIDC callback ve geçerli Türkçe percent örnekleri testli. Bu, aşağıdaki upstream DoS açığının tüm platformlarda kapatıldığı iddiası değildir. |
| BF14-04 | P2, ortak Pressable kontrollerinde pasif durum/ölçü farkları | Action bileşeni native Pressable davranışını korur, default button rolü, gerçek disabled ve mevcut selected/expanded ile birleşen accessibilityState ve48×48minimum hedef uygular. Giriş, profil ve ortak kart/panel düğmelerine bağlandı. Profil modal başlıkları ve fotoğraf önizleme etiketi tamamlandı. |
| BF14-05 | P2, klavye/metin büyümesi | Screen’de handled taps ve drag dismissal; marka satırı, istatistik ve ikincil eylemler sarılır. Var olan Text ölçekleme sınırlandırılmadı. Ortak text/muted/primary renkleri iki açık yüzeyde6çift için4.5kontrast testi geçti. Bu bütün ekranlar için WCAG sertifikası değildir. |
| BF14-06 | P2, arka planda periyodik JS eşitleme | Global kuyruk, profil ve bildirimlerin periyodik çağrıları yalnız AppState active iken çalışır; öne dönüşte yenileme tetiklenir. Çalışan isteğin arka plana geçişte iptali veya OS background job eklenmedi. Ev sahibi/kiracı okuma yaşam döngüsü ortak useScopedRead’e taşındı; mükerrer poll/cleanup ve önceki lint uyarısı kaldırıldı. |
| BF14-07 | P2, Android otomatik uygulama veri yedeği varsayılanı | app.json android.allowBackup=false. Expo config introspect üretilen Android manifestinde false doğrulandı. Ayar yeni Android native derlemesinde etkili olur; mevcut APK’da uygulanmış veya cihazlar arası aktarımın tüm OS/üreticilerde engellendiği iddia edilmez. iOS yedek politikası bu ayarla değişmez. |

## Güvenlik sınırı incelemesi

Backend rol/aktif üyelik/Property/Unit kapsamı ve alıcı/yayın/audience kontrolleri korunur. UI kontrolü yetki yerine kullanılmaz. OIDC Code+S256 PKCE, access token belleği, refresh SecureStore ve key-first cache temizliği mevcut haliyle incelendi; password grant veya auth bypass eklenmedi. Cache SQLCipher, fotoğraflar AES dosyası; hesap/rol/mülk ayrımı,24saat kopya süresi, makbuz ve geç yanıt guardları ilgili testlerle regresyondan geçti. Bu tur fiziksel cihazda SecureStore/SQLCipher/AES fixture yeniden çalıştırılmadı. Cihaz kilidi, jailbreak/root, açık telefon kaybı, ekran kaydı/uygulama değiştirici snapshotı ve uzaktan kapalı cihaz temizliği için garanti verilmez. Çıkış/hesap değişimi mevcut anahtar temizliğiyle çalışır; sunucu yetki iptalinin çevrimdışı cihaza anında ulaşması mümkün değildir.

Ürün kaynaklarında parola/token/ham kişisel yanıtı console’a yazan yeni yol eklenmedi. Mevcut tek native doğrulama logu __DEV__ ve açık opt-in ile yalnız boolean fixture sonucu yazar; gerçek kullanıcı veri yükü içermez. Talep/profil fotoğrafı isteğe bağlı, kamera/galeri bağlamsal; yeni mikrofon/konum/push izni yok. Genel no-store, rate limit ve JWT davranışı backend regresyonunda test edildi. İsteklerde mevcut15saniye abort korunur; response body’yi aldıktan sonra JSON okumanın her durumda ayrı boyut/zaman sınırı olduğu iddia edilmez.

## Güncel bağımlılık audit değerlendirmesi

`npm audit --json`: **28bulgu,18high/10moderate,0critical**. Bunlar dört kök advisory’nin transitif paketlere yayılmış sonuçlarıdır;28bağımsız açık gibi sayılmamalı. **Audit geçmedi ve güvenli yayın onayı verilmedi.** Paket sürümü/lockfile değişmedi, force fix veya bulgu bastırma yapılmadı.

| Paket / mevcut sürüm | Advisory ve yol | Erişilebilirlik / karar |
| --- | --- | --- |
| decode-uri-component0.2.2 | GHSA-vcc3-ghjq-m6fr; expo-router57.0.26→query-string7.1.3 | URL parse runtime’ında DoS riski. Upstream yama0.5.0 ESM; mevcut query-string CJS require ile çağrılabilir fonksiyon bekler. Kör override uyumlu varsayılmadı. Native girişte boyut/UTF8 kontrolü kısmi savunma; web ve diğer Router girişleri için tam çözüm değildir. P1 yayın engeli; Expo57 uyumlu upstream/backport veya ayrı doğrulanmış decoder uyarlaması gerekir. |
| braces3.0.3 | GHSA-vfj7-8cjw-p6xm; Expo→Metro→metro-file-map→micromatch | Geliştirme/build glob işleme, derin desen DoS. Registry’de son sürüm3.0.3, advisory tümünü etkiliyor. Yalnız güvenilir proje dosya/desenleri kullanılır; build/dev erişimini sınırlamak yama değildir. Upstream düzeltme ve yeniden audit açık. |
| node-forge1.4.0 | GHSA-86w9-cpqp-85rv; Expo CLI/code-signing-certificates | Build/dev imza doğrulama zinciri. Registry’de son sürüm1.4.0, yamasız. Mobil auth’ın kriptografisi bu paketle yapılmıyor; bu ayrım dağıtım/imza riskini kapatmaz. Upstream yama ve imzalı dağıtım doğrulaması açık. |
| uuid7.0.3 | GHSA-w5hq-g745-h8pq; Expo config-plugins→xcode | Advisory v3/v5/v6 ve dış buffer yolunda. İncelenen xcode pbxProject yalnız v4 çağırıyor. Mevcut çağrı yolunda belirtilen sink görülmedi; paketin bütünü güvenli diye işaretlenmedi.11.1.1 ana sürüm override yapılmadı; uyumlu upstream güncelleme açık. |

Registry npm audit fixAvailable önerilerinde Expo44 veya Router58 ana sürümü var; Expo57/ReactNative0.86 ile uyumlu güvenli yama gibi uygulanmadı.0.5.0 decoder sürümünün MIT/ESM metadata’sı ve ilgili advisory incelendi; kurulmadı. Bu değerlendirme yeni bağımlılık eklemedi. Lisans kontrolü329npm/NuGet sürüm için geçti; metadata, container/ticari dağıtım yükümlülüklerinin yerine geçmez.

Kaynaklar: [decoder advisory](https://github.com/advisories/GHSA-vcc3-ghjq-m6fr), [braces advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm), [forge advisory](https://github.com/advisories/GHSA-86w9-cpqp-85rv), [uuid advisory](https://github.com/advisories/GHSA-w5hq-g745-h8pq). Uygulama API’leri: [Expo57 Router](https://docs.expo.dev/versions/v57.0.0/sdk/router/), [native intent](https://docs.expo.dev/router/advanced/native-intent/), [Expo57 config](https://docs.expo.dev/versions/v57.0.0/config/app/), [RN0.86 erişilebilirlik](https://reactnative.dev/docs/0.86/accessibility).

## Bu tur doğrulama

- **75/75 mobil test**,0atlama. Sekiz yeni hardening senaryosu: cache/network/bozuk yanıt, doğrulama-kayıt sırası,401/403purge,hesap-bağlam yarışları,offline/corruptcache,ham nativeURL,10.000kuyruk ve6renkçifti. Önceki oturum/expiry/temizleme, rol/mülk, para, request/fotoğraf, notice/okundu/preference regresyonları dahil.
- **142/142 gerçek ayrı PostgreSQL/SeaweedFS integration**,0atlama. Mobile/Migration/Persistence/PersonProfiles yanında AuthenticationTests ve ReleaseSecurityTests: yanlış issuer/audience/süre/imza, auth/üyelik/rol, rate limit/header/yayın yapılandırması ve önceki mobil fotoğraf/işlem regresyonları. Test auth anahtarları fixture’dır; gerçek Keycloak üretim kabulü değildir. Yalnız fixture DB/rol/bucket temizlendi. Bu faz backend mutation/migration eklemedi; concurrent merkez dosyaları değiştirilmedi.
- TypeScript, **lint0hata/0uyarı**, iOS/Android/Web export geçti.75test içinde büyük kuyruk kurulumu/sorgu kontrolü host Node/SQLite üzerinde çalıştı; yaklaşık50ms tek koşu gözlemi cihazFPS veya düşük cihaz benchmark’ı değildir. Toplam fixture2.000karakter×10.000satır; uygulama iş metni/PII kullanılmadı.
- Android config introspect manifest allowBackup=false. Android native build veya iOS install bu tur yapılmadı. JS geliştirme değişiklikleri mevcut Metro’dan alınır. API5006health200,Metro8081running. No-new-dependency lisans metadata329geçti.
- Kullanıcının önceki “bakmana gerek yok” talimatı gereği tarayıcı/görsel inceleme yapılmadı; VoiceOver/TalkBack, gerçek native izin veya uçak modu etkileşimi yapılmış sayılmaz.

## Açık cihaz/kullanıcı kabul matrisi

| Senaryo | Güvenli test | Durum |
| --- | --- | --- |
| iOS VoiceOver / Android TalkBack | Giriş,sekme,profilmodal,talepkategori,disabled,seçili,okundu; etiket/odak sırası | DOĞRULANAMADI — fiziksel etkileşim yok |
| 320px ve büyük ekran / metin%200 | Marka,sayaç,düğme,profilformu,klavyeveodak; yataytaşma ve erişilebilir kaydet | DOĞRULANAMADI — gerçek görsel/etkileşim yok |
| Düşük cihaz /50satır+fotoğraf | KaydırmaFPS,peakmemory,soğukaçılış ve tekrarlı gezinme | DOĞRULANAMADI — hostSQLitefixture cihazbenchmarkı değil |
| Uçak modu/yeniden açılış | Yetkili fixturekopya/TTL,offlinefoto,okundukuyruğu,bağlanınca tekmerkezkaydı | DOĞRULANAMADI — otomatik port testleri geçti |
| Hesap değişimi/çıkış/kayıp cihaz | Önceki foto/cache erişilmez; kilit/backup/reinstall ve online revocation | DOĞRULANAMADI — native key fixture bu tur çalışmadı |
| Kamera/galeri kabul-ret | Fotoğrafsız devam,iptal,metadata/boyut,yanlışizin durumundan geri dönüş | DOĞRULANAMADI — Faz07/12 kartları açık |
| Auth/native bağlantı | Gerçek OIDC callback,bozuk/uzunURL ve soğuk/sıcak açılış | Parser testi geçti; native teslim/yeniden giriş kabulü açık |
| Web dış URL | Router öncesi webserver guard/uyumlu decoder | AÇIK P1 — +native-intent webi korumaz |
| Android backup | Yeni APK’da manifest/restore/cihaz aktarım davranışı | Config doğrulandı; native derleme ve gerçek restore kabulü açık |

Push sağlayıcısı ve onaylı hukuki metin yok; önceki Faz13/07 açık maddeleri kapanmaz. Audit ve cihaz kabulü kapanmadan güvenli yayın veya Faz15mağaza hazırlığı tamamlandı denmez. Bir sonraki faz için yeni kullanıcı talebi beklenir.

## Teslim durumu ve kullanım verisi

Bu turun4geliştirme işpaketi tamamlandı (4/4,%100,00):analiz,düzeltme,otomatikkanıt,belge/takip. Bu oran ürünün tüm kabulünün veya güvenli yayının tamamlandığı anlamına gelmez. Mobil yol haritası15faz; Faz01–04belgeliDone,05–14Inreview,15Backlog. Güvenlik açıkları ve cihaz kabulü yukarıda açıkça ayrılır.

Son Codex usage okuması:300dakikalık pencere%22kullanılan/%78kalan,reset16.10.2026 02:24:58 Europe/Istanbul;10080dakikalıkpencere%34/%66,reset22.10.2026 10:57:02. Bu hesap pencereleridir;bu fazın token/süre/context tüketimi olarak hesaplanmaz. Giriştoken,çıkıştoken,toplamtoken,kalancontext,turtüketimi:BİLGİ MEVCUT DEĞİL.
