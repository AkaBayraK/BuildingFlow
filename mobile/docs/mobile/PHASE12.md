# Faz 12 — Talep/arıza, ekler ve offline outbox

15.10.2026. Kullanıcı "devam" talebiyle Faz12'yi başlattı; karar kartındaki önerilen "Tam kapsam" (kategori/öncelik + fotoğraf + taslak + kuyruk) uygulandı. Faz13 başlamadı.

## Uygulanan deneyim

Taleplerim ekranı artık OWNER ve TENANT bağlamında form gösterir (önceden yalnız TENANT). Form: yedi kategori (Su/tesisat, Elektrik, Isıtma/kombi, Asansör, Temizlik, Güvenlik, Diğer), dört öncelik (Düşük, Normal, Yüksek, Acil), 1–2000 karakter açıklama, kamera veya galeriden en fazla 3 fotoğraf. Form her değişiklikte (600 ms) taslak olarak şifreli SQLite'a, fotoğraflar AES-GCM ile özel dosyaya yazılır; uygulama kapanınca veya uçak modunda kaybolmaz. "Taslağı sil" onaylı. Gönderilen talepler listesinde kategori, öncelik ve fotoğraf sayısı görünür. Web önizlemesi talep göndermez.

Eşitleme paneli metin işlemlerinin yanında her fotoğraf için Bekliyor/Gönderiliyor/Yeniden denenecek/Çakışma/Yetki gerekiyor/Gönderilemedi durumunu ve nedenini (talep metni bekleniyor, biçim/3 sınırı, cihaz dosyası okunamadı) gösterir; tekrar dene ve onaylı sil düğmeleri vardır. Konum izni istenmez, EXIF istenmez; sunucu JPEG metadata'sını ayrıca temizler. Acil yardım uyarısı (112) formdadır.

## Kuyruk sözleşmesi

- Gönderimde talep CREATE komutu ile fotoğraf işleri tek SQLite transaction'ında kuyruğa girer, taslak silinir; hata olursa taslak ve fotoğraflar kalır.
- Fotoğraf işi yalnız üst komut `sent` olduktan sonra claim edilir; böylece talep metni her zaman önce gider. Üst komut iptal edilirse fotoğraflar `cancelled`, yetkisi kalkan bağlamda `blocked` olur ve yerel şifreli kopya silinir.
- Her fotoğraf kendi UUID operationId'si ile gönderilir; yanıt kaybında aynı kimlik yeniden denenir, sunucu replay ile ikinci kayıt oluşturmaz. 429/5xx/ağ hatası sınırlı backoff, 400/409 otomatik denenmez, 401 oturumu kapatır.
- Fotoğraf seçici en boy oranını korur; 1280→720 px ve JPEG kalite adımlarıyla 256 KB sunucu sınırına sığdırır.
- Yerel şema 5→6: `request_drafts`, `request_photo_jobs`. Eski kuyruktaki kategori/öncelik taşımayan komutlar geçerli kalır.

## Merkez API

Ayrıntı [BuildingFlow docs/api/mobile-sync.md](../../../docs/api/mobile-sync.md) Faz12 bölümü. Komutlara opsiyonel `category`/`priority`, yeni `POST /requests/{id}/photos` ve `GET /requests/{id}/photos/{photoId}`; migration `20261015133409_MobileRequestCategoriesAndPhotos`.

## Bu tur doğrulama ve sınırlar

- 62/62 mobil test (yeni `requests.test.mjs` 8 senaryo: sözleşme/DELETE/legacy, kayıt/makbuz parse, owner metadata, taslak yeniden açma ve dosya adı koruması, atomik gönderim, uçak modu→çevrimiçi sıra ve kayıp yanıt replay'i, iptal/yetki iptali temizliği, 400/409/429/401 ve dosya kaybı). TypeScript, lint 0 hata (1 önceki useOwner uyarısı), üç platform `expo export` geçti.
- Merkez: 109/109 unit, 401/423 integration (22 atlama: Keycloak/RabbitMQ gibi bu ortamda olmayan fixture'lar). Yeni MobileRequestPhotoTests 4 + MobileRequestMigrationTests 1 gerçek ayrı PostgreSQL ve SeaweedFS S3 ile koştu.
- Gerçek telefon (iPhone/Android) uçak modu, kamera izni ve görsel kabul yapılmadı; otomatik testler bunun yerine sunulmaz. Yönetici tarafında talep fotoğrafı görüntüleme ve talep durumu (atandı/çözüldü) bu fazda yok.
- Yeni npm/NuGet bağımlılığı veya native modül yok (expo-image-picker/manipulator mevcuttu).
