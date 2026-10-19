# Faz 12 — Talep, fotoğraf ve çevrimdışı gönderim

15.10.2026. Kullanıcı Faz 12’yi ve **yerel main** dalını onayladı. Başlangıç commit’i `ea13e59`; bu tur dal değiştirilmedi, merge, commit veya push yapılmadı. Kanonik kapsam: `mobile/buildingflow-mobile-prompts/PHASES.md`. Faz 13 başlamadı.

## Kullanıcıya sunulan akış

- Talep formu: Arıza, Temizlik, Güvenlik, Ortak alan, Öneri veya Diğer kategorisi; Düşük, Normal veya Yüksek öncelik; 1–2000 karakter açıklama. Yetkili ev sahibi, kiracı, sakin ve yönetici bağlamları desteklenir. Muhasebe rolüne talep veya kişi yetkisi verilmez.
- İsteğe bağlı **bir fotoğraf**: kamera veya galeri, önizleme, değiştirme ve kaldırma. Talep fotoğrafında oran korunur; uzun kenar 512 piksele indirilir, JPEG çıktısı en fazla 256 KB olur. Kaynak türü, 20 MB ve 40 MP sınırları kontrol edilir. Profilin kare kırpması korunur. Konum izni veya koordinat toplanmaz; bu form acil yardım hizmeti değildir.
- Form değişiklikleri 600 ms beklemeden sonra şifreli taslağa kaydedilir. Fotoğraf seçimi ayrıca disk kaydını bekler. “Kaydedildi” yalnız dosya ve metadata yazımı tamamlandığında gösterilir. Bu mesajdan önce uygulamanın zorla kapatılması son değişikliği kaybettirebilir. Taslağı silme onayı, gönderilmiş kayıtları etkilemez.
- Gönderimde sabit `operationId` ve `recordId`, mevcut outbox ve yeniden deneme kuralları kullanılır. Kuyruğa kaydetmek merkez başarısı değildir. Bekleyen fotoğraf uygulama yeniden açıldıktan sonra kuyruk kartından çevrimdışı açılabilir. Henüz denenmemiş işlem iptal edilir; belirsiz gönderim yeni kimlikle tekrar oluşturulmaz.
- Kendi gönderilmiş talepleri cihazda 50 satırlık sayfalarla gösterilir. Kaldırma, sürümlü komut ve merkez tombstone onayıyla işler. Yönetici, yetkili binadaki taleplerin kategori, öncelik ve fotoğrafını görebilir. Atama, işleme alma ve çözüldü durumları eklenmedi.

## Merkez sözleşmesi ve özel depolama

Mevcut `POST /api/v1/mobile/sync/commands` sözleşmesine isteğe bağlı `Category`, `Priority`, `PhotoAction` ve `PhotoBase64` eklendi. Fotoğraf işlemleri KEEP, REPLACE ve REMOVE. Eski sekiz alanlı komutların fingerprint serileştirmesi ve eski makbuzları korunur. Yeni komutta kategori, öncelik ve normalize fotoğraf hash’i fingerprint’e katılır. Kayıt/operasyon kilitleri, version/409 ve transaction içindeki makbuz korunur. İstek gövdesi 400.000 baytla sınırlıdır.

`MobileRequestPhoto` bir BaseEntity metadata tablosudur; fotoğraf baytları veritabanına yazılmaz. Owner/property/photo bileşik FK, owner/operation tekilliği ve sekiz zorunlu audit alanı korunur. STAGED rezervasyonu yüklemeden önce kalıcıdır. Mevcut JPEG doğrulaması EXIF/APP/yorum alanlarını temizler. Nesne özel SeaweedFS alanına yazılır; S3 kimlik bilgileri mobil uygulamaya verilmez. CURRENT, RETIRED ve PURGED durumları; iki dakikada bir çalışan bakım, retired veya 30 dakikadan eski staged nesneleri kayıt kilidi altında temizler. DB ile S3 tek dağıtık transaction değildir; belirsiz yüklemenin nesnesi hemen silinmez.

`GET /api/v1/mobile/sync/requests/{recordId}/photo`, kullanıcının kendi yetkili bağlamını veya gerçek yönetici rolüyle aktif Property kapsamını doğrular. SUPER_ADMIN için mevcut global kapsam istisnası korunur. Anonim istek 401, yetkisiz/iptal edilmiş erişim 403, olmayan veya kaldırılmış fotoğraf 404 döner. İndirmede boyut ve SHA256 doğrulanır; DTO yalnız fotoğraf kimliği ve hash taşır. Nesne anahtarı veya depolama sırrı dönmez.

## Cihazda saklama ve temizlik

SQLCipher şeması **6** oldu: `request_drafts`, `request_files`, `request_photo_cache`. SQLite yalnız metadata ve dosya yollarını tutar. Taslak ve yükleme fotoğrafları mevcut SecureStore/AES-GCM dosya altyapısını, hesap ve dosya adına bağlı additional-data ile kullanır. Hesap/mülk/daire/rol ayrımı ve geç yanıt koruması korunur.

İndirilen fotoğraflar ayrıca kayıt/fotoğraf kimliği, hash, 24 saat ve geriye giden saat kontrolünden geçer. Yetki iptalinde ilgili taslak, yükleme ve önbellek dosyaları temizlenir. Kendi kaydı silinince veya fotoğrafı değişince eski cache kaldırılır. Çıkışta mevcut anahtar-önce temizliği tüm özel dosya dizinini kapsar.

Bağlam başına bir taslak; hesapta en fazla 40 yerel yükleme ve 40 indirilen fotoğraf kopyası tutulur. Merkez onaylı fakat sunucu kopyası henüz indirilmemiş fotoğraf, aynı kayıt/sürümle eşleşen yerel yükleme dosyasından çevrimdışı açılır; ekran bunun **yerel gönderim kopyası** olduğunu belirtir. Online görüntüleme merkezden yeniden doğrulanır. Doğrulanmış sunucu kopyası kaydedilince eski yükleme dosyası kaldırılır. Web önizlemesinde kalıcı fotoğraflı gönderim açılmaz.

## Bu tur doğrulama

- **60/60 mobil test:** kalıcı taslak ve ayrı dosya, sabit UUID ve farklı fotoğraf reddi, cevap kaybı/yeniden açılış/replay, iptal ve yetki temizliği, fotoğraf kapsamı/TTL, geçersiz kategori/öncelik/boyut/makbuz. Native AES fixture’ı bu tur yeniden çalıştırılmadı; SQLite port testleri fiziksel cihaz şifreleme kabulü olarak sunulmaz.
- **83/83 PostgreSQL ve SeaweedFS entegrasyon testi + 3/3 PersistenceTests; sıfır atlama.** Yeni altı talep testi; eski mobil modül regresyonları; model, kurulum, yükseltme, tekrar ve idempotent SQL kontrolleri. Eşzamanlı fotoğraf replay, hash, foreign/accountant/anonim/revoked erişim, değiştirme/kaldırma/silme/nesne temizliği ve eski makbuz uyumu geçti. Tüm uygulama tablolarının audit kolonları tarandı. Geçici test veritabanı, rol ve özel bucket temizlendi.
- TypeScript ve üç platform export başarılı. Lint: sıfır hata, önceki `useOwner` kodunda bir uyarı. Merkez derlemesi: sıfır hata/uyarı. Lisans metadata kontrolü: **329 paket**. Yeni bağımlılık veya paket sürümü değişikliği yok.
- `20261015133914_MobileRequestDetailsAndPhotos` migration’ı kaynak, Designer ve snapshot ile kaydedildi. Down, dolu fotoğraf veya varsayılandan farklı talep ayrıntılarını silmeyi reddeder. Yerelde yalnız bu migration uygulandı. Properties, People, Units ve MobileRequest eski alanlarının önce/sonra veri hash’leri aynı kaldı. Canlı kullanıcı talebi, fotoğrafı veya rolü test amacıyla yazılmadı.
- **iPhone geliştirme derlemesi başarılı ve bağlı telefona kuruldu.** Derlemede bir Expo geliştirme script uyarısı var. Kamera/galeri amaç açıklamaları profil ve talep fotoğrafını kapsayacak şekilde güncellendi. API5006 sağlık kontrolü 200, anonim fotoğraf isteği 401, Metro8081 çalışıyor. Kaynak `BuildingFlow/mobile/buildingflow-mobile` modülüdür.
- Kullanıcının önceki talimatıyla görsel inceleme yapılmadı. Gerçek kamera/galeri izin kabul-ret, uçak modunda soğuk açılış ve küçük/büyük ekran kullanıcı kabulü açık; native kurulum bu testlerin yerine geçmez. GitHub Faz 12 bu nedenle In review durumundadır.

## İlgili dosyalar

Mobil: `src/requests/drafts.ts`, RequestForm/Board/Photo/QueuedRequestPreview, SyncPanel, AuthProvider, sync sözleşmesi/engine, SQLite migration, mevcut private-files ve picker. Merkez: MobileSync DTO/store/configuration/endpoint, MobileRequest/Photo, yönetici DTO/store, bakım servisi, EF migration ve entegrasyon testleri. API sözleşmesi: `docs/api/mobile-requests.md`. Faz 13 için yeni onay beklenir.
