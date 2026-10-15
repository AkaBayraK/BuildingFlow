# Faz 03 — Mobil uygulama temeli

13.10.2026. Kullanıcı Faz 02 tasarımını ve Faz 03 başlangıcını onayladı. Plan kaynağı `buildingflow-mobile-prompts/PHASES.md` (15 faz); bu tur yalnız Faz 03.

## Uygulanan kapsam

- `mobile/`: resmi create-expo-app blank-typescript temeli, Expo 57.0.28, React 19.2.3, React Native 0.86.3, TypeScript 6.0.3. Expo Router 57.0.26 entry, `src/app` route yapısı, beş alt sekme ve bulunamadı ekranı.
- Ortak Screen/Card, safe-area, kaydırılabilir içerik ve Faz 02 renkleri. Şimdilik sistem karakterleriyle navigasyon işaretleri, metin etiketleri; özel ikon/mağaza varlığı eklenmedi. Temanın görünümü açık olarak sabit.
- Kök route ErrorBoundary, yeniden deneme, kullanıcıya güvenli metin; exception içeriği veya kişisel veri yazdırılmaz.
- Strict TypeScript, Expo ESLint, URL doğrulama testleri, üç platform JS/Hermes export komutu ve GitHub Actions örneği. CI dosyası yerelde oluşturuldu; uzak CI çalıştırılmadı.
- Public env örneği, secret/env ignore kuralları. API adresi isteğe bağlı; üretimde HTTPS, kimlik bilgisi/query/fragment reddi. Şu an ağ isteği yok.
- `mobile/README.md`: npm ci, iOS/Android/emulator/fiziksel cihaz, web ve development build adımları. Native binary imzalama/yayın Faz 15 kapsamıdır.

## Kaynaklar ve lisanslar

Expo SDK 57 sürümlü Router ve safe-area belgeleri ile resmi kurulum yönergesi incelendi:
https://docs.expo.dev/versions/v57.0.0/sdk/router/
https://docs.expo.dev/versions/v57.0.0/sdk/safe-area-context/
https://docs.expo.dev/router/installation/

Doğrudan paketler MIT; TypeScript Apache-2.0. Paket lisans dosyaları korunur. `DEPENDENCIES.tsv` kurulu üst seviye 678 paket için metadata/lisans dosyası envanteridir; belirsiz kayıt yok. Envanter nested paketlerin veya dağıtım yükümlülüklerinin tam hukuki denetimi değildir; yayın öncesi Faz 14/15'te lockfile üzerinden tam tarama gerekir. Expo Router lisansı resmi Expo MIT lisansını izler: https://github.com/expo/expo/blob/main/LICENSE .

## Doğrulamalar

- `npm run check`: TypeScript ve lint başarılı; URL yapılandırması 4/4 test. Cleartext production, unsafe protocol, URL içinde credential/query/fragment negatif testleri var.
- `npm run build`: web, iOS ve Android bundle/export başarılı. Bu kontrol native derleme/IPA/APK veya gerçek cihaz kabulü değildir.
- Expo paket eşliği yerel SDK haritası ile geçti. Router'ın otomatik getirdiği Worklets 0.13.0 peer uyuşmazlığı, SDK destekli Reanimated 4.5.1 + Worklets 0.10.1 ile düzeltildi; `npm ls` temiz.
- Çevrimiçi `npx expo-doctor`: 21/21 kontrol başarılı. Son tema/gezinme değişikliği sonrası `npm run check` ve üç platform export tekrar başarılı.
- Tarayıcıda ana sayfa → finans bağlantısı ve beş sekme geçişi doğrulandı; 390px ve 412px görünüm kontrolü yapıldı. 412px document scrollWidth 412px; konsol error kaydı yok. Kanıt: `phase03-preview.png`. Native simülatör/gerçek iOS/Android cihazında çalışma test edilmedi. Xcode yolu mevcut, `adb` PATH'te bulunmadı.
- `npm audit`: 28 uyarı (18 high, 10 moderate). SDK 44/RN 0.72'ye geri döndürme veya Router 58'e taşıma önerileri uyumluluğu bozduğu için force fix uygulanmadı. Node-forge, braces, decode-uri-component, uuid ve Reanimated/Worklets transitif zincirleri dahil; bu açıklar giderilmiş sayılmaz. Native runtime ve geliştirme araçları etkisini ayıran güvenlik çalışması gerekir; üretim yayın kapısı açık değildir.

## Sınırlar ve devam

Mobil uygulama gerçek auth, rol/mülk verisi, SQLite, sync, talep/ödeme işlemi içermez. Sekmeler sonraki fazların dürüst boş durumlarıdır. Faz 02 HTML prototipi ayrı tasarım referansıdır. API/web/backend kullanıcı dosyaları değiştirilmedi. Create-expo-app `mobile/.git` başlangıç deposunu otomatik oluşturdu; faz kodu commit/push edilmedi, üst klasör hâlâ ayrı Git deposu değildir.

Sonraki faz: 04 — OIDC, oturum ve rol/mülk bağlamı. Mobil public client, redirect scheme ve kullanıcı-üyelik izin sözleşmesi doğrulanmadan giriş kodlanmamalı. Kullanıcı onayı olmadan Faz 04 başlatılmaz.

Faz 03 uygulama temeli ve yerel kontrolleri tamamlandı; güvenlik denetimindeki açıklar bilinen sınırlama olarak taşınır. Remote CI ve native runtime başarı iddiası yoktur. Token tüketimi/kalan context: BİLGİ MEVCUT DEĞİL.
