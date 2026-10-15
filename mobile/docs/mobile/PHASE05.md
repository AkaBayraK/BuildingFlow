# Faz 05 — SQLite ve çevrimdışı veri temeli

14.10.2026. Kullanıcı Faz04'ü onayladı ve Faz05'i başlattı. Yalnız Faz05 uygulandı; Faz06 senkronizasyon/outbox ve sonraki iş modülleri başlatılmadı.

## Kullanıcıya görünen davranış

- iOS/Android development build, yalnız API'den doğrulanmış hesap adı, yetkili mülk/daire/rol listesi ve son seçimi şifreli SQLite'ta saklar. İş verilerinin API sözleşmeleri sonraki fazlarda eklenecektir; borç/ödeme/talep kayıtları varmış gibi oluşturulmaz.
- Daha önce doğrulanan hesap, cihaz bağlantısızken mevcut güvenli refresh kaydı ve en fazla24saatlik önbellekle salt okunur açılır. İlk giriş ve yeni cihaz çevrimiçi olmalıdır.24saat konservatif geliştirme sınırıdır; ürünün yayın saklama politikası ayrıca kesinleştirilmelidir.
- Ekranda “Çevrimdışı · Salt okunur”, son doğrulama zamanı ve bilgilerin değişmiş olabileceği gösterilir. Profilde veri güncelliği/saklama açıklaması vardır. Bağlantı dönünce oturum sunucudan yeniden doğrulanır; eski yerel rol listesi çevrimiçi yetki yerine kullanılmaz.
-401/403 veya diğer çevrimiçi oturum doğrulama hatalarında önbellek üzerinden yeniden giriş yapılmaz. Saat geri alınmışsa veya kopya eskimişse okunmaz. Çıkış/hesap değişimi yerel anahtarı ve kopyayı kaldırır.
- Tarayıcı önizlemesi hassas veriyi kalıcı saklamaz; web SQLite/SQLCipher desteği varsayılmaz. iPhone/Android için yeni native build gerekir; Expo Go desteklenmez.

## Dosyalar ve veri modeli

`mobile/src/offline/database.ts`: iki sürümlü migration; account, cache_records, preferences. Her kayıt UUID, hesap/context/kind, created/updated/verified tarihleri, version, deleted_at ve cached durumu içerir. Fotoğraf BLOB'u, token, parola veya işlem kuyruğu yok. Kullanıcı değerleri SQL parametreleriyle bağlanır; API JSON'u izin verilen alanlara indirgenir. Migration ve snapshot değişimi transaction içinde geri alınır. Gelecek şema sürümü silinmez/geriye çevrilmez.

`cache.native.ts`: her hesap/issuer/API/client kapsamı için rastgele isimli ayrı SQLCipher dosyası;32rastgele bayt anahtar SecureStore WHEN_UNLOCKED_THIS_DEVICE_ONLY. SQLCipher varlığı PRAGMA cipher_version ile doğrulanmadan hassas kayıt yazılmaz; düz SQLite'a geçiş yok. WAL ve foreign_keys etkin. `cache.ts` web kalıcılığını kapatır. `queue.ts` işlemleri sıraya alır; çıkış bekleyen eski yazmaları geçersiz kılar, yeni hesap silme bitmeden başlamaz.

`AuthProvider.tsx` yalnız doğrulanmış API oturumunu saklar. Offline snapshot bir access token veya sunucu yetkisi üretmez. `Screen.tsx` bağlantısız uyarıyı, profil ekranı son doğrulama bilgisini gösterir; onaylı lacivert/ikon/kart tasarımı korunur.

## Anahtar, çıkış ve yedekleme

Anahtar senkronize edilmez veya dışa aktarılmaz; tokenlar SQLite içinde bulunmaz. Çıkışta önce anahtar, sonra locator ve veritabanı silinir. Dosya silme başarısız olursa kalmış dosya silinmiş anahtarla şifrelidir. Anahtar kaybı/başka cihaza geri yükleme durumunda çevrimdışı erişim verilmez; çevrimiçi giriş gerekir. OS yedeği şifreli DB dosyasını içerebilir; uygulama dışa aktarma/yedekleme özelliği sunmaz. Android SecureStore backup dışlama ayarları Expo plugin tarafından yönetilir. Önbellek kaydedilemezse kullanıcı bilgilendirilir; çevrimiçi doğrulanmış erişim sürer, düz saklama yapılmaz.

24saatlik bağlantısız pencere sırasında sunucudaki rol iptalinin anlık bilinmesi mümkün değildir. Bu kopya sadece eski bilgilerin okunması içindir. İşlem gönderme/ödeme gibi sonraki modüller offline durumda çevrimiçi yetki yerine bu kopyayı kullanmamalıdır. Gelecek fazlarda ayrıca idempotency ve API yetkileri doğrulanmalıdır.

## Doğrulama

- TypeScript/lint: başarılı.
-16/16 Node test: gerçek SQLite migration v1→v2, tekrar migration, gelecekteki sürümü reddetme, hesaplar arası okuma/yazma reddi, bağlam seçimi,24saat sınırı, geri saat, tombstone, bozuk payload, SQL injection metni, token alanlarının saklanmaması, snapshot atomic rollback, çıkışta bekleyen yazma iptali ve cleanup sonrası yeni hesap sırası.
- iOS/Android/Web export: başarılı. iOS fiziksel build: hata0/uyarı1; yeni build bağlı iPhone'a kuruldu.
- Gerçek iPhone native izole fixture kontrolü: SQLCipher, SecureStore anahtar yaz/oku/sil, migrations, accountIsolation, expires, wrongKeyRejected, cleanup tümü true. Kişisel veri/gerçek kullanıcı anahtarı yazılmadı; test dosyası/anahtarı temizlendi. Development self-check `src/offline/verify.native.ts`, isteğe bağlı EXPO_PUBLIC_VERIFY_OFFLINE=1 ile tekrar çalıştırılabilir; production/dev varsayılanında kapalı.
- Chrome gerçek PKCE giriş/API oturumu ve profil veri güncelliği görünümü doğrulandı;390px yatay taşma0. Kanıt: phase05-profile.png. Bu web testi mobil offline kabulü değildir.
- Expo Doctor21/21 geçti. npm audit önceki28uyarı (18high/10moderate) devam ediyor; giderilmiş sayılmaz.
-694 üst seviye paket metadata envanteri; belirsiz0. Yeni Expo paketleri MIT; SQLCipher'ın açık kaynak BSD lisansı ayrıca geçerlidir.

## Açık cihaz kabulü

Gerçek hesabın iPhone'da giriş → uçak modu → uygulamayı kapatıp aç → salt okunur mülk listesi → yeniden bağlantı → çıkış → uçak modunda eski hesabın açılmaması akışı henüz kullanıcı arayüzünden tamamlanmadı. Android'de fiziksel/emulator native runtime kabulü yapılmadı. Native şifreleme fixture testi bu uçtan uca kabulün yerine geçmez. Web önizleme testleri de mobil uçak modu kabulü değildir.

## Çalıştırma

```sh
cd mobile
npx expo prebuild --platform ios
npx expo run:ios --device --no-bundler
npm run start -- --dev-client --lan --port 8081
npm run check
npm run build
# Yalnız izole fixture kontrolü istenirse Metro'yu bu flag ile başlat:
EXPO_PUBLIC_VERIFY_OFFLINE=1 npm run start -- --dev-client --lan --port 8081
```

Kaynaklar: [Expo SQLite SDK57](https://docs.expo.dev/versions/v57.0.0/sdk/sqlite/), [SecureStore](https://docs.expo.dev/versions/v57.0.0/sdk/securestore/), [Network](https://docs.expo.dev/versions/v57.0.0/sdk/network/). Commit/push/mağaza yüklemesi yok. Faz06 için kullanıcı onayı ve API idempotency/veri sözleşmeleri gerekir.

## Ağ adresi değişimi düzeltmesi

Mac LAN adresi192.168.1.6→192.168.2.95 değiştiğinden mobil giriş eski adreste kalmıştı. npm run dev:phone launcher native API/issuer env, Metro advertised host, mobil API AllowedHosts ve DevelopmentIssuers ayarlarını birlikte günceller. Mevcut eski mobil süreçleri durdurulup launcher başlatıldı. Yeni LAN üzerinde API health200, Metro running ve discovery issuer/auth/token yeni adresle doğrulandı; iPhone development-client yeni URL ile açıldı. Masaüstülocalhost adresleri ve ana API5005 değiştirilmedi. Launcher yalnız kendi child process gruplarını yönetir, başka süreçlerin portunu zorla boşaltmaz.
