# BAKIM — Yetkili kullanıcı fotoğrafı — 22.10.2026

**TAMAMLANDI — ADIM 01/01; bakım akışı ve bu tur 1/1 = %100,00.** Mevcut İŞ19 fotoğraf görevlerine kanıt bağlandı; yeni ürün fazı veya kabul tiki eklenmedi. İŞ yol haritası okuması: 24 kayıt,141/151 tamamlanmış görev=%93,38; bu bakım toplamı değiştirmez.

Kullanıcının açık yetkisi: “Yetkili yönetici diğer kullanıcılara da eklesin.” SUPER_ADMIN tüm hesaplarda; SITE_ADMIN kendi hesabında ve mevcut kapsamındaki yönetilebilir hesaplarda ekleme/değiştirme/kaldırma. Diğer ayrıcalıklı hesapları değiştirme veya başka kişi adına rıza onaylama yetkisi verilmedi.

## Uygulama

- `src/features/users/UserPhotoDialog.tsx`, `UsersWorkspace.tsx`, `model.ts`: kamera eylemi, ortak sağ panel, önizleme/kaydet/kaldır, TR/EN, yükleme kilidi, kapanış odağı, aynı UUID ile belirsiz yanıt retry,409'da yeniden yükle/seç. DTO hedef kimliği/hash/version/capability doğrulaması. Yeni kullanıcı önce kaydedilir; sonra satırdan fotoğraf eklenir.
- `src/features/account/photo.ts`: mevcut yerel kare512 JPEG≤256KiB/EXIF temizleme altyapısına mobildeki40MP sınırı; kaynak≤20MB,tarayıcı JPEG/PNG/WebP. Kaynak veya base64 tarayıcı depolamasına yazılmaz. Yeni paket veya dış dönüştürücü yok.
- `Application/ManagedUserPhotos.cs`, `Users.cs`, `Api/UserEndpoints.cs`, `Infrastructure/MobileProfileStore.cs`, `DependencyInjection.cs`, `Domain/MobileProfile.cs`, `MobileConsentStore.cs`: kapsamlı state/komut uçları; mevcut profil transaction/pointer/özel nesne deposu/receipt/audit/version/cleanup paylaşılır. UUID fingerprint gerçek işlem yapan+hedef+komuta bağlıdır. Rıza hedef sahibinde güncel olmalı; withdrawal kilidiyle tekrar kontrol edilir. REMOVE için rıza gerekmez. Telefon/özel profil ayrıntısı fotoğraf API yanıtında yoktur. Mevcut profil ve Keycloak alanları değişmez; ilk profil dizindeki geçerli ad/soyad ile başlatılır, mobilversion>0 sözleşmesi korunur. Yeni tablo/entity/kolon/migration yoktur.

## Bu turun kanıtı

- **24/24 entegrasyon geçti,0 atlanan/başarısız.** `dotnet test backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~UserManagementTests|FullyQualifiedName~MobileProfileTests|FullyQualifiedName~AccountApiTests'`; son sonuç `/tmp/bf-user-photo-results/user-photo.trx`. İzole PostgreSQL rol/veritabanı ve yalnız fixture'ın özel S3 test bucket'ı; oluşturulan kaynaklar cleanup ile kaldırıldı. Gerçek kullanıcı/veritabanı ayarları değiştirilmedi.
- Yeni `ManagedUserPhotoTests.cs`: her iki ayrıcalıklı rolü yönetme, SITE_ADMIN kapsamı ve kendi fotoğrafı istisnası, rızasız403/outdated rıza, withdraw ile kaldırma, mobile ortak fotoğraf ve ad/telefon koruması, gerçek actor audit'i, aynı UUID yarışında replay, farklı aktör/payload409, eski version409, eski fotoğraf404, malformed/oversize/type400/413/415, negatif roller/anonim, üyelik geri alma. Eski replay kaldırılmış fotoğrafı diriltemez. `UserManagementTests.cs` yalnız partial yapılmıştır; mevcut senaryolar korunur.
- **221/221 web testi geçti,0 atlanan/başarısız; npm run typecheck ve npm run build başarılı.** Yeni `tests/user-photo.test.mjs`: hedef/digest/version/replay doğrulama, capability tipi, dosya format/boyut ve40MP sınırı/bitmap kapatma. Test fixture'ı gerçek bileşenleri çalıştırır; canlı API/OIDC hesabı içermez.
- **CUA masaüstü1440×1000 ve dar ekran390×844:** proje test JPEG'iyle seç/önizle/kaydet, listede avatar, tekrar açma, rızasız yükleme engeli, kaldır/kaydet, kapanışta ilgili butona odak, yatay taşma yok (dialog clientWidth=scrollWidth=388px). Browser error/warn kaydı boş. Görseldeki mavi daire proje JPEG test girdisidir; gerçek kullanıcı fotoğrafı değildir.
- [Masaüstü](USER_PHOTO_DESKTOP.png),[dar ekran](USER_PHOTO_MOBILE.png),[API sözleşmesi](../api/users.md).

Canlı Keycloak hesabıyla kullanıcı kabulü ve gerçek telefonda cihaz kabulü bu turun testi değildir. Mobil kaynak kodu/dependency değişmedi; mobil check/build çalıştırılmadı. Mobil uyumluluğu ortak API entegrasyonuyla doğrulandı.

## Koruma ve teslim

Yeni bağımlılık/lisans/dış görsel yok; paket manifest/lock aynı. Git yazma/stage/commit/push işlemi bu akışta yapılmadı. Paylaşılan depodaki indeks çalışma sırasında başlangıçtan farklılaştı (188 yol); mevcut `5afd7f2` commit'i ve diğer çalışmalar korundu, üstüne indeks geri yazılmadı. Teslim öncesi alınan yeni indeks ile son kontrol karşılaştırılır. Next.js'nin yalnız bilinen next-env.d.ts build yol değişimi başlangıç haline alındı. Kalan teknik engel yok; sonraki kapsam kullanıcı talebidir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
300 dk: kullanılan %8, kalan %92; reset 22.10.2026 15:39:36 Europe/Istanbul. 10080 dk: kullanılan %64, kalan %36; reset 27.10.2026 13:32:07 Europe/Istanbul. Kaynak: Codex usage rateLimitsByLimitId.codex, 22.10.2026 teslim okuması.
