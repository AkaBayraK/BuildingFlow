# Faz 07 — Profil, fotoğraf ve gizlilik tercihleri

14.10.2026. Kullanıcının onayladığı merkez hesabı akışı uygulandı. Faz 08 başlatılmadı. Görsel inceleme kullanıcının “bakmana gerek yok” talimatıyla yapılmadı; daha önce paylaşılan BuildingFlow referansı ve DESIGN_REFERENCE.md tasarım yönü korundu.

## Uygulanan davranış

- Hesabım ekranında profil kartı, ad/soyad/isteğe bağlı iletişim telefonu düzenleme, fotoğraf önizleme, galeri/kamera seçimi ve kaldırma bulunur. Bu bilgiler giriş hesabına aittir; Keycloak kullanıcı adı/parolası veya bina yönetimindeki resmi Person kayıtları değişmez.
- Fotoğraf isteğe bağlıdır. Kamera izni yalnız eylem sırasında istenir; ret halinde galeri veya fotoğrafsız kullanım mümkündür. Sistem galeri seçicisi kullanılır; geniş kütüphane izni istenmez. Mobilde kırpma ve kare önizleme, ortadan kırpma/512px JPEG dönüşümü uygulanır. Girdi JPEG/PNG/HEIC/HEIF/WebP, en fazla20MiB/40MP; merkez çıktısı en fazla256KiB JPEG/1024px. JPEG segment yapısı doğrulanır ve EXIF/APP/yorum metadata’sı kaldırılır; bu bir tam JPEG entropy decoder değildir.
- Mobil yükleme → yetkili merkez API → özel SeaweedFS4.48 bucket. S3 kimlik bilgisi veya public dosya adresi mobile verilmez. Yeniden kurulum/giriş ve15s profil eşitlemesi merkezi metadata ve fotoğrafı getirir. İndirme SHA256/boyutla doğrulanır.
- Değiştirme yeni sürüm/fotoğraf kimliği oluşturur. Kaldırma veya değiştirme eski fotoğrafa erişimi hemen kapatır. Merkezde RETIRED nesneler ve30dakikadan eski başarısız STAGED yüklemeler2dakikalık bakımda en fazla5nesne/pass temizlenir; metadata/audit tutulur. Bakım başarısızsa sonraki döngü yeniden dener, sabit bir fiziksel silinme süresi garanti edilmez.
- Native SQLite schema4: profil metadata, cihaz görüntüleme tercihi ve tek bekleyen işlem metadata’sı. Fotoğraf ve işlem gövdesi BLOB değil, SQLCipher anahtarına bağlı AES-GCM özel dosyalarıdır; dosya adı ve hesap deposu AAD ile bağlanır. Çıkışta önce anahtar, ardından DB ve özel dosya klasörü silinir. Önceki24saatlik oturum önbelleği sınırı korunur.
- Gönderimde sabit UUID, immutable içerik ve expectedVersion kullanılır. Cevap kaybında aynı UUID ile retry; en fazla6otomatik deneme, bounded backoff/Retry-After. Geçersiz makbuz otomatik retry olmaz; manuel retry aynı UUID’yi korur.409 başka cihaz değişikliğini ezmez; kullanıcı merkezdeki bilgileri alıp yeniden düzenler. Gönderimi belirsiz taslak iptal edilmez.403 profil ve taslağı temizler. Başarılı makbuz sonrası fotoğraf indirme hatası yüklemeyi yeniden oluşturmaz.
- Web önizlemesi hassas profil/fotoğrafı kalıcı depolamaz. Gönderim belirsizliği için aynı UUID’li taslak yalnız sekme belleğinde tutulur; sekme kapatılırsa kaybolur. Kalıcı çevrimdışı kuyruk development build içindir.
- “Bu cihazda fotoğrafımı göster” teknik görüntüleme tercihidir; merkez fotoğrafını silmez ve hukuki rıza sayılmaz. Onaylı aydınlatma/açık rıza metni sağlanmadığı için metin veya rıza kaydı uydurulmadı. Yayın öncesi hukuki metin sahibi tarafından sağlanmalıdır.

## Merkez sözleşmesi

GET /api/v1/mobile/profile; POST /api/v1/mobile/profile/commands; GET /api/v1/mobile/profile/photo/{id}. Sahiplik JWT sub’dan alınır. Aktif backend hesap/mülk bağlamı kontrol edilir; kullanıcı kendi profilini yönetir. Fotoğraf pointer’ı OwnerUserId+PhotoId composite FK ile korunur. Profil ve fotoğraf BaseEntity/audit/version alanlarını taşır. Ortak işlem makbuzu, transaction/advisory lock, işlem kimliği/fingerprint ve optimistic version yarış koruması kullanılır. Migration20261014162556_MobileAccountProfiles yalnız iki yeni tablo/indeks ekler; dolu tablolar için Down koruması vardır. Yerel merkeze uygulandı.

## Doğrulama

- Mobil TypeScript/lint: başarılı, hata/uyarı0. Node/gerçek SQLite testleri36/36: migration/account ayrımı, encoding/yanıt doğrulama, güvenli dosya portu, UUID replay,409/403, logout/geç cevap, recovery, HTTP/network retry bütçesi, indirme bütünlüğü ve makbuz sonrası indirme hatası.
- Merkez27/27 seçili MobileProfile/ProfileImage/MobileSync/MobileSession/S3Policy integration testleri; atlama0. Gerçek geçici PostgreSQL database/role ve yalnız fixture’a ait SeaweedFS bucket. Eşzamanlı duplicate, sürüm çatışması, anonim/yabancı/üyeliği kaldırılmış hesap, ACCOUNTANT own-profile, boyut/tür, EXIF temizliği, eski fotoğraf erişimi ve fiziksel silme doğrulandı; test kaynakları temizlendi.
- iOS/Android/Web export geçti. iPhone native build0hata/1toolchainuyarı; yeni uygulama yüklenip açıldı. Opt-in fixture SQLCipher/SecureStore/migration/expiry/account isolation/native queue replay, AES dosya round-trip, dosya adı AAD, yanlış AES anahtarı ve cleanup kontrolleri true. Gerçek kullanıcı fotoğrafı veya canlı profil değişikliği test amacıyla yapılmadı.
- Merkez API/worker/web Docker derlemesi ve dağıtımı başarılı; API5005/5006, Metro8081 ve merkez3003/login HTTP200. Önceki merkez oturum kapatma sayfası iyileştirmesi de dağıtıldı;104/104 FE testi önceki UI turunda geçmişti.
- Yeni Expo modülleriSDK57 uyumlu MIT image-picker57.0.20/image-manipulator57.0.21/file-system57.0.7. Merkez329paket lisans metadata kontrolü geçti. Önceki npm audit bulguları ayrı bakım konusudur.

## Kabul sınırları

Gerçek kullanıcının kamera/galeri izin akışı, uçak modu/soğuk başlangıç ve Android native arayüz kabulü yapılmadı. Cihaz şifrelemesi fiziksel iPhone fixture’ında; merkez dosya/API davranışı izole integration testlerinde doğrulandı. Kullanıcının talimatıyla ekran açıp görsel inceleme yapılmadı. Commit/push/mağaza yayını yok.
