# OTOPARK FAZ06 — React Native mobil ve offline taslak

27.10.2026 • Plan06/08 • [Issue78](https://github.com/AkaBayraK/BuildingFlow/issues/78) • Teknik teslim incelemesi.

## Yetki ve kapsam

Kullanıcı “faz 06 geçebilirsin” diyerek Faz05'i kabul etti; #77 completed kapatıldı. Ardından “kaldığın yerden devam” talimatı aynı Faz06'yı sürdürür. Çalışma `.claude/worktrees/practical-napier-12a6c6`, branch `claude/project-thread-9pmvkm`; Faz05'in kaydedilmemiş değişiklikleri ve main'deki diğer çalışmalar korunur. Commit/push/main birleştirmesi/PR/deploy yapılmadı. Yeni faz numarası uydurulmadı: otopark8faz, mobil15faz ve UI12aşama ayrı kalır.

Gerçek prompt kaynağı ana checkout'un `BuildingFlow-Araclar-Otopark/MASTER_PROMPT.md`, `FAZ-06.md` ve sekiz FAZ dosyasıdır; bu pakette PHASES.md yoktur. Mobil AGENTS/MASTER/PHASES, token/yürütme kuralları, DESIGN_REFERENCE ve mobil tanıtım görseli incelendi. Expo57, React Native0.86.3 ve mevcut kurulu API'ler versioned resmi belgelerden kontrol edildi. Yeni paket/SDK/font/görsel eklenmedi.

## Son davranış

- Seçili hesap/yönetim alanı/daire/rol bağlamında Araçlarım, PENDING araç ekleme, açık/geçerli daire park tahsisleri. Tahsis park hakkı veya fiziksel doluluk olarak otomatik üretilmez.
- Yönetici için misafir/geçici park kaydı, ziyaretçi yeri ve daire seçimi, mevcut manuel giriş/çıkış ve iptal. Sakinlere rezervasyon/misafir yetkisi açılmadı. SECURITY realm/mobile-session değişikliği yoktur; mevcut mobile contexts SECURITY üretmediği için mobil guard ekranı iddia edilmez.
- `/api/v1/mobile/parking` facade: legacy mobile-session ve canonical exact context + güncel legacy alias/scope. Alias yoksa veya source version değişmişse403. Tarihli ilişki/görev/üyelik iptalinde okuma ve UUID replay reddedilir. ACCOUNTANT403, anonim401. Canonical-only legacy eşlemesi olmayan kullanıcılar kapalı kalır; otomatik eşleme/backfill yapılmaz.
- Tek ortak plaka/tarih/DTO sözleşmesi `shared/parking.ts`; web eski feature girişinden re-export eder. Web ve mobile paralel farklı algoritmalar taşımaz.
- Mevcut SQLCipher/SecureStore hesabında minimum cache; yönetici1h/sakin24h üst sınır, context/page/account/clock/lease kontrolü. Misafir adı/notu online snapshot ve cache'de yoktur.403/404 cache+taslak temizler; temizlenemiyorsa oturum kapanır ve anahtar temizlenir. Geç/başka bağlam cevabı gösterilmez.
- Offline taslak UUID ile kalır; outbox veya otomatik gönderim yok. Native online gönderimden önce taslak kalıcılaştırılır. Cevap kaybında aynı kimlik; içerik değişiminde yeni kimlik.409'da kullanıcı yenileme veya bilinçli form düzenlemesi yapmadan tekrar gönderemez. Nihai müsaitlik, kota, yetki ve kayıt sonucu yalnız sunucudan gelir.
- Legacy native akışta Expo Router route ve ikonlu kısayol; canonical çalışma alanında mevcut güvenli panel korunarak otopark açılır. Beş ana tab değişmedi. Sık işlemler48px asgari dokunma hedefli mevcut Action bileşenini kullanır.

## Değişen dosyalar

Backend: `Application/Parking.cs`, `Api/ParkingEndpoints.cs`, `Infrastructure/ParkingStore.cs`, `ParkingStore.Operations.cs`, yeni `ParkingStore.Mobile.cs`; yeni `MobileParkingTests.cs`. Schema/entity/migration değişikliği yoktur.

Mobil: yeni `src/parking/{Parking,Editor,useParking,contracts,cache,type-style}`; AuthProvider'ın scope/epoch/deny-cache uçları; ortak scopedRead fail-closed sunum temizliği; Screen kısayolu, canonical PersonModelScreen paneli; gizli parking route. SQLite şeması değişmedi. `tests/parking.test.mjs`, test-only fixture/typings ve `scripts/parking-fixture.mjs`.

Web/shared: `shared/parking.ts`, feature model re-export, aynı OTOPARK-06 kimlikli roadmap ve TR/EN kayıtları. docs/api/parking, TESTING, progress, mobilPHASE_STATUS ve teslim/lisans yönergeleri kanıtla eşlendi.

## Bu turun doğrulaması

| Kontrol | Gerçek sonuç |
|---|---|
| .NET solution build |0hata/0uyarı|
| Seçili gerçek geçici PostgreSQL/API |**46/46**, atlanan0: Parking/MobileParking/ErrorContract/Persistence/AutoMigration|
| Otopark unit |**18/18**, atlanan0|
| Mobil `npm run check` |**154/154 + typecheck/lint**, son kaynak koşusu|
| Mobil `npm run build -- --clear` |**iOS/Android/web son kaynak export GEÇTİ**|
| Web `npm test`, typecheck/build |**275/275**, son roadmap sonrası typecheck/build GEÇTİ|
| Lisans |merkez329 npm/NuGet, mobil871 lock kaydı metadata GEÇTİ|
| Koruma |tur başındaki109 tracked manifest/lock/csproj/migration kaynağı hash aynı; phase06 yeni migration yok|
| İzole CUA/native-web UI |375px iframe ve560px içerik; araç ekle→PENDING, yönetici manuel giriş,409→düğmeler kapalı, offline kesinleştirme kapalı|

API testleri kendi tenant/person/property/unit/membership kayıtlarını ayrı geçici DB'de üretir. Canonical eksik/eski alias, yanlış context, kapalı bayrak, bitmiş ilişki; legacy yabancı alan/unit/rol ve anonim; UUID marka farkı, kayıt replay, misafir çakışması/giriş/çıkış,20+3 sayfalama doğrulandı. SQLite testleri gerçek NodeSQLite üzerinde migration ve scope izolasyonu, TTL/clock rollback, draft UUID kalıcılığı, outbox'a sıfır kayıt, hesap/context bazında purge, offline istek göndermeme, gecikmiş cevabı atma ve purge hatasında veriyi gizleme davranışını test eder. Bu testler SQLCipher'ın gerçek cihaz şifreleme kanıtı değildir.

İlk mobil lint3hata verdi (renderDate.now ve metin kaçışı); düzeltildi. İlk API fixture'ında dört test zorunlu phone/email alanları eksik olduğundan başarısız oldu; fixture düzeltildi, son koşu geçti. İlk web Node testinde re-export `.ts` uzantısı eksikti; düzeltildi. İlk geniş koşu ayrıca S3 gerektiren mevcut canonical fotoğraf testini atladı; atlanmış test başarılı sayılmadı, son hedefli koşu storage gerektirmez ve0skip'tir.

Worktree mobil node_modules başlangıçta ana checkout'a symlink edildi. İlk Expo export başarıyla bitse de yeni ekran yerine main kaynaklarını içerdiği fark edildi; bu ilk çıktı başarı kanıtı sayılmadı. Symlink yalnız bu turun kendi bağlantısıydı; kaldırıldı. `npm ci --offline --ignore-scripts` ile aynı lockfile'dan835paket yerel cache'den bağımsız kuruldu, lock/manifest değişmedi; gerekli lisans kontrolleri çalıştırıldı. Yeniden export web bundle'ında `/api/v1/mobile/parking` bulundu, main'in common-areas modülü yoktur. Üç platform export native imzalı binary değildir.

[375px](screenshots/06-resident-375.png), [geniş](screenshots/06-resident-wide.png), [manuel giriş](screenshots/06-manager-arrived.png), [409](screenshots/06-conflict.png). Fixture gerçek feature bileşenlerini React Native Web ile render eder; shell/icons/auth/API portları yapaydır. Bu ekran kanıtı gerçek cihaz, canlı Keycloak veya gerçek API browserE2E değildir.375px ölçüsü iframe genişliğidir; ayrı sayısal taşma metriği alınmadı, görsel inceleme yapıldı.

## Güvenlik, lisans ve açık sınırlar

Yeni credential, IdProl, tenant kurulumu veya gerçek .env/veri değişikliği yok. Plaka/token/ad/not log/telemetriye eklenmedi; ham server exception/message render edilmez. Parametreli SQLite sorguları ve mevcut şifreli native depo tekrar kullanılır. Offline bir iptalin anında öğrenilemeyeceği açık sınırdır; TTL ve canonical lease dolunca erişim kapalıdır. Native SQLCipher/SecureStore/biyometri/background/presentation lock ve canlı OIDC/API uçtan uca kabulü bu tur DOĞRULANAMADI. Tam backend suite/SAST/pentest/performance/remoteCI/yayın kabulü yapılmadı.

Yeni dependency veya lisans yükü yok; mevcut React/ExpoSQLCipher/Crypto/Intl ve projeye ait kod kullanıldı. Npmci'nin offline “0vulnerability” çıktısı güncel advisory taraması değildir; güvenli yayın kabulü olarak kullanılmadı. Mevcut container/dağıtım/NOTICE yükümlülükleri korunur. Yanlış başarı iddiası, sahte ödeme, sensör doluluğu, kamera/ANPR/QR/bariyer eklenmedi. Bildirim/anonimleştirme FAZ07 kapsamındadır.

Rollback: yeni DB migration yok. Önceki mobil artifact'e dönmeden önce cache/draft uyumu ve eski paket sürümleri gözden geçirilir; yeni slotlar eski istemcide okunmaz, mevcut outbox bozulmaz. Bu tur kaynak değişiklikleri main'e birleştirilmedi. Faz05 migration canlıya uygulanmadı; AutoMigrate açık sonraki API başlangıcı hâlâ pending migration uygulayabilir. Test yalnız kendi geçici DB/rolünü ve fixture sunucusunu temizler.

## İlerleme

Tur: kapsam/uygulama/doğrulama **3/3=%100,00 teknik teslim**. FAZ06 resmi8kriterin teknik7'si raporlandı; **7/8=%87,50, kullanıcı kabulü açık**. Yeni #78 açılmadı; mevcut görev kullanılır ve açık tutulur. FAZ07 başlatılmadı. Önceki İŞ/UI/MOBİL faz sayaçları değişmez. Toplam8otopark fazı gerçek prompt paketinden sayıldı; önceki dört fazın bu tur yeniden test edildiği veya tam üretim kabulü yapıldığı iddia edilmez.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex (27.10.2026 son okuma): 300 dk kullanılan %60/kalan %40, reset28.10.2026 04:27:24;10080dk kullanılan%72/kalan%28,reset02.11.2026 15:28:41 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

Teslim: [#78 yorumunda](https://github.com/AkaBayraK/BuildingFlow/issues/78#issuecomment-6453055601) sonuçlar ve sınırlamalar kaydedildi; teknik7madde tamam, kullanıcı onayı açık. Ürün roadmap26kayıt144/155=%92,90. Yalnız bu turun geçici DB/rolü ve13016sunucusu temizlendi.
