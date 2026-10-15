# BuildingFlow Mobile

Faz05: Expo Router/TypeScript, gerçek OIDC/API oturumu ve native SQLCipher hesap/mülk önbelleği. Tasarım kaynağı: `../docs/mobile/DESIGN_REFERENCE.md`. Ödeme/talep/duyuru iş modülleri sonraki fazlardadır. Ayrıntılar: `../docs/mobile/PHASE05.md`.

## Kurulum ve çalıştırma

Node.js LTS (en az 22.18; testler Node'un yerleşik TypeScript desteğini kullanır) ve npm gerekir. Bu tur Node 26.8.2 ile doğrulandı; Expo resmi olarak LTS önerir.

```sh
cd mobile
npm ci
npm run check
npm run start
```

iOS: macOS + Xcode/simülatör kuruluysa `npm run ios`. Android: Android Studio + SDK ve çalışan emulator varsa `npm run android`. Fiziksel cihaz: aynı ağda `npm run start`, SDK 57 uyumlu Expo Go/development build ile QR açılır. Expo Go sürümü uyumsuzsa uygun development build gerekir; yerel native derleme `npx expo run:ios` veya `npx expo run:android` (native araçlar gerekir). Mağaza paket kimliği ve imzalama Faz 15 kararıdır; şu anda üretim build'i yoktur.

Web önizleme: `npm run web -- --port 8763`. Paketleme: `npm run build` iOS/Android/Web JS ve varlıklarını dist altında dışa aktarır; imzalı IPA/APK üretmez. Bu çıktıları paylaşmadan önce gerçek ortam ayarlarını doğrula.

## Yapı ve kontroller

- `src/app`: Expo Router kök hata sınırı, beş sekme ve bulunamadı ekranı.
- `src/components/Screen.tsx`, `src/theme.ts`: güvenli alan, kaydırma, tema ve kartlar.
- `src/config`: isteğe bağlı API URL doğrulaması; üretimde HTTPS; kullanıcı/parola/sorgu/fragment reddi. Faz 03 ağ çağrısı yapmaz.
- `.env.example`: yalnız public API adresi. EXPO_PUBLIC değerleri uygulama paketinde görünür; token, parola ve client secret konulmaz. Cihazdaki localhost bilgisayarın API'sini göstermez; LAN adresi kullanılır.
- `npm run typecheck`, `npm run lint`, `npm test`: sırasıyla TypeScript, Expo ESLint ve güvenli ortam adresi testleri.
- `.github/workflows/check.yml`: npm ci/check/build. Cloud build, mağaza yüklemesi ve hesap bağlantısı yok.

Faz 04: OIDC giriş/çıkış, native SecureStore, yenileme ve API rol/mülk bağlamı eklendi. Güncel kurulum ve sınırlar `../docs/mobile/PHASE04.md` içindedir. Expo Go giriş için yeterli değildir; `npx expo run:ios` / `npx expo run:android` ile development build gerekir. Güncel geliştirme önizlemesi `http://localhost:8081`; 8762 tasarım prototipidir. Faz05 yerel veri ve Faz06 senkronizasyon altyapısı uygulanmıştır.

## Faz05 native veri

SQLCipher için yeni development build gerekir; Expo Go yeterli değildir. İlk giriş çevrimiçi yapılır. Native son doğrulanmış hesap/mülk kopyası24saat salt okunur; çıkışta şifreleme anahtarı/kopya silinir. Tarayıcı hassas veriyi kalıcı saklamaz. Faz06 hesap/bağlam kapsamlı işlem kuyruğu ve çakışma yönetimi bu şifreli depoyu kullanır. İsteğe bağlı EXPO_PUBLIC_VERIFY_OFFLINE=1 development kontrolü yalnız geçici fixture dosyası/anahtarı oluşturup siler.

## Telefon için günlük geliştirme çalıştırması

npm run dev:phone Mac'in aktif LAN adresini bulur, gitignored .env içindeki native API/issuer adreslerini günceller ve mobil API5006 + Metro8081 development-client süreçlerini birlikte başlatır. Çalışırken ağ adresi değişirse kendi başlattığı süreçleri yeni adresle yeniden başlatır. Ctrl+C yalnız kendi süreçlerini kapatır; başka servisleri sonlandırmaz. Portlar başka süreçte açıksa onları silmez, açıklamayla durur. Keycloak8080 açık olmalıdır.

Telefonda **BuildingFlow** uygulamasını kullanın; Expo Go özel OIDC dönüşünü ve SQLCipher'ı desteklemez. Mac ve telefon aynı yerel ağda olmalıdır. Yeniden bağlantı gerektiğinde terminaldeki güncel development-client adresini/QR'ı kullanın; localhost telefonda Mac anlamına gelmez. Bilgisayar veya geliştirme süreçleri kapalıyken development build sunucuya erişemez. Bu betik üretim dağıtımı veya arka plan sistem servisi değildir.

## Faz06 ve marka

Senkronizasyon sözleşmesi,26 mobil test ve52 seçili backend testinin kanıtları [PHASE06.md](../docs/mobile/PHASE06.md) içindedir. Profilde Veri eşitleme paneli bulunur; talep formu Faz12 kapsamındadır. Faz07 kullanıcı tarafından onaylandı; öncelikli SeaweedFS depolama refactoru tamamlandı. Onaylı fotoğraf akışı [PHASE07.md](../docs/mobile/PHASE07.md) içinde.

Paylaşılan bina simgesi kodla vektör olarak yeniden çizildi; mobil başlık/giriş ve uygulama ikonu ile merkez marka varlıkları aynı geometriyi kullanır. Üretim ve kontrol bilgisi [BRAND.md](../docs/mobile/BRAND.md).

Mobil API artık merkezdeki SeaweedFS4.48/S3 deposunu ObjectStorage ayarlarıyla kullanır. Dosya erişimi mobil→API→özel S3 gateway şeklindedir; S3 kimlik bilgileri uygulamaya girmez.


## GitHub mobil faz panosu

Kullanıcı onaylı pano: https://github.com/users/AkaBayraK/projects/3

Kanonik15faz `../buildingflow-mobile-prompts/PHASES.md` kaynağından okunur. Teslim/kanıt eşlemesi `scripts/github-project/deliveries.mjs`; ayrıntı `../docs/mobile/GITHUB_PROJECT.md`. Mobil remote henüz yok; taslak proje kayıtları kullanılır.

```sh
npm run project:plan    # Sadece yerel önizleme
npm run project:test    # Yedi güvenli upsert/preflight testi
npm run project:sync    # Açık yazma yetkisiyle GitHub'a uygula
npm run project:verify  # Uzak içerik/durum ve kapsamdışı kayıt koruması
```

Sır eklemeyin; mevcut gh kimliği kullanılır. Plan/doğrulama raporu gitignored `artifacts/github-project/` altındadır. Yönetim bloğu dışındaki insan notları korunur; aynı başlıklı ama yönetilmeyen veya yinelenen kayıt varsa işlem durur. Bu komutlar mağaza yayını, commit/push veya zamanlanmış otomasyon oluşturmaz.
