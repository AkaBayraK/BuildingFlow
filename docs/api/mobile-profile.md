# Mobil profil — Faz 07

14.10.2026. Kullanıcı mobil yükleme → merkezde SeaweedFS4.48 saklama → yeniden kurulum/diğer cihaz eşitlemesini onayladı.

## API
- GET `/api/v1/mobile/profile`: JWT sub sahibi profil; ilk kullanımda boş ad/soyad/phone/photo ve version0/modifiedAt null. Mevcut profilde ad/soyad, phone, photoId, photoSha256, version, modifiedAt.
- POST `/api/v1/mobile/profile/commands`: operationId UUID, expectedVersion, firstName/lastName (trim,1–80), phone (opsiyonel,ASCII7–15rakam/max32), photoAction KEEP/REPLACE/REMOVE, photoBase64 (yalnız REPLACE). JSON üst sınır384KiB; JPEG256KiB/1024px; segment yapısı kontrolü, APP/EXIF/yorum temizliği. Yanıt operationId/profile/replayed. İŞ17: REPLACE için ACTIVE `PROFILE_PHOTO` açık rızası gerekir, aksi 403 `BF_CONSENT_REQUIRED`; KEEP/REMOVE rıza istemez. Rıza uçları ve geri çekmede fotoğrafın kaldırılması [mobile-consents.md](mobile-consents.md).
- GET `/api/v1/mobile/profile/photo/{id}`: yalnız sahibi güncel fotoğrafını okuyabilir; eski/yabancı kimlik404. Özel S3 indirme, SHA256/boyut kontrolü, JPEG bytes; ortak auth/no-store/nosniff middleware korunur. Public URL, doğrudan S3 erişimi veya mobile S3 sırrı yok.

Aktif MobileSession bağlamı doğrulanır; hasMore veya boş context reddedilir. ACCOUNTANT dahil kendi profil yönetimi mümkündür; Person resmi kimlik kayıtları, kullanıcı adı/parola ve diğer hesap profilleri bu API’den değiştirilemez. Anonim401; aktif bağlamı olmayan/iptal edilmiş hesap403; doğrulama400; büyükbody413; JSON dışı415; UUID/sürüm çatışması BF_MOBILE_SYNC_CONFLICT409.

## Tutarlılık ve saklama
MobileProfile/MobileProfilePhoto BaseEntity; unique sahibi, sahibi+işlem UUID ve sahibi+fotoğraf composite FK. PostgreSQL advisory lock/transaction/version ve ortak MobileOperationReceipt UUID/fingerprint replay kullanılır. Fotoğraf STAGED rezervasyonu upload’dan önce kalıcıdır; belirsiz nesne yazımı aynı UUID/içerikle güvenli tekrar edilir. Makbuz eski işlemin orijinal profil sürümünü döndürür, yeni bilgiyi sessizce ezmez.

Değiştirme/kaldırma eski pointer erişimini hemen kapatır. API hosted maintenance2dakikada en fazla5 RETIRED veya30dakikadan eski STAGED nesneyi, hesap kilidi/pointer kontrolüyle silip PURGED audit durumuna taşır. Storage başarısızlığında sonraki döngü tekrar dener; sabit silinme süresi garanti edilmez. JPEG kontrolü segment yapısaldır, tam entropy decode değildir.

Migration `20261014162556_MobileAccountProfiles` yalnız iki yeni tablo/indeks ekler; dolu tabloların Down işlemi reddedilir. Yerel merkeze uygulandı.

Mobil: SQLCipher schema4 metadata/tek iş; AES-GCM dosya, hesap-depo/dosya adı AAD; fotoğraf DB BLOB değildir. Çıkışta anahtar ve dosyalar silinir. SabitUUID/expectedVersion, bounded backoff/Retry-After,6deneme bütçesi, bozuk makbuz manuel aynıUUIDretry,409 çakışma. Web yalnız sekme belleği; native güvenli kalıcı kuyruk.24saatlik mevcut offline oturum sınırı korunur.

Rıza metinleri merkezden yayınlanır (İŞ16); mobil rıza ekranları İŞ18 kapsamındadır, o zamana kadar mobil uygulama mevcut metni gösterir. Fotoğraf görünürlük tercihi teknik cihaz ayarıdır, hukuki rıza değildir. Kamera/galeri fotoğraf seçimi opsiyoneldir.

Kanıt:27/27 hedefli MobileProfile/ProfileImage/MobileSession/MobileSync/S3Policy test; gerçek izole PostgreSQL ve fixture SeaweedFS bucket temizlendi. Mobil36/36, lint/typecheck/export; fiziksel iPhone AES dosya/AAD/yanlış anahtar/SQLCipher fixture geçti. Gerçek kullanıcı fotoğrafı yazılmadı. UI görsel inceleme kullanıcı talimatıyla yapılmadı. Ayrıntı kardeş mobil docs/mobile/PHASE07.md.


## BF-VERIFY 01 — Ortak iletişim doğrulama temeli

GET `/api/v1/account/contact-verification` ve mobilde GET `/api/v1/mobile/contact-verification` aynı `IContactVerificationStore` sözleşmesini kullanır. Yalnız JWT hesabının kendi verisi;Property/Person seçimi yok. E-posta Keycloak'tan güncel okunur,ulaşılamazsa UNKNOWN/available=false;eski JWT onayı otorite sayılmaz. Telefonun eski profil değeri,mevcut doğrulanmış değeri ve yeni adayı ayrıdır. POST aynı kökün `/phone` ucu `{operationId,expectedVersion,phone}` ile **aday hazırlar**;SMS göndermez,doğrulandı yapmaz. DTO'da canSendCode=false,deliveryStatus=NOT_REQUESTED,challengeId=null. Mevcut profil DTO/komutu değişmez;eski telefon yazımı aynı transaction içinde kanıt/aday modelini günceller;isim/fotoğraf/aynı normalize numara kanıtı sıfırlamaz. Hatalar mevcut BF_USER_INVALID(phone)/BF_MOBILE_SYNC_CONFLICT kodları;bilinmeyen JSON alanları reddedilir.

[Ayrıntılı DTO/null/versiyon/replay/şema sözleşmesi](../refactor/contact-verification/01-contract.md). Bu bir sahiplik doğrulama gönderim akışı değildir;gönderim/e-posta zorunluluğu sonraki fazlardır.

BF-VERIFY02: mobil hesap açılışı ortak Keycloak.CreateAsync adaptörünü kullanır; EmailEnabled opt-in ile yeni hesaba VERIFY_EMAIL atanır. Mobil kayıt için ikinci admin daveti gönderilmez; ilk PKCE girişi native doğrulamayı tamamlatır. Kayıt/profil DTO şekli ve mevcut mobil telefon giriş validasyonu bu fazda değişmedi; kanal ekranları FAZ05 kapsamındadır. [Gerçek IdP ve mobil callback kabulü](../refactor/contact-verification/02-report.md).
