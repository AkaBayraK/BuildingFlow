# Mobil kayıt ve kişi kodu eşleşmesi (İŞ15)

## Kayıt — POST `/api/v1/mobile/registrations` (anonim)

`{requestId, username, password, passwordConfirmation, firstName, lastName, phoneNumber, email, personCode?}`

- Kullanıcı adı küçük harfe çevrilir (`^[a-z0-9][a-z0-9._-]{2,49}$`); parola ≥12, büyük/küçük harf, rakam ve tekrarıyla aynı; telefon E.164'e normalize edilir (0 5xx…, 5xx…, 0090…); e-posta zorunlu.
- API Keycloak kullanıcısını servis hesabıyla açar ve parolayı kalıcı olarak ayarlar (ROPC açılmaz; realm `registrationAllowed=false` kalır). Giriş mobil uygulamada mevcut PKCE akışıyla yapılır. Parola veritabanına ve cihaza yazılmaz.
- Aynı `requestId` ve aynı içerik → önceki sonuç (200, `replayed`); farklı içerik veya kullanıcı adı/e-posta çakışması → `BF_MOBILE_REGISTRATION_CONFLICT` (409, hangi alanın çakıştığı söylenmez).
- İstemci başına (IP adresinin SHA-256 özeti) saatte 5 kayıt; aşılırsa `BF_MOBILE_RATE_LIMITED`. Production'daki genel anonim istek sınırı da geçerlidir.
- Ad, soyad ve telefon kullanıcının mobil profiline (`MobileProfile`) yazılır; uygulamadaki "Hesabım" ekranında görünür ve düzenlenebilir.
- Keycloak hesabı açıldıktan sonra parola veya profil adımı başarısız olursa hesap silinir.
- `personCode` verilmişse kayıt sonrası aynı kurallarla eşleşme denenir; sonuç `personLink.status`: `LINKED`, `REJECTED`, `LOCKED`, `NONE`. Eşleşme olmasa da hesap açılır.
- Keycloak ayarı yoksa veya erişilemezse `503 BF_IDENTITY_UNAVAILABLE`.

## Kişi kodu eşleşmesi — POST `/api/v1/mobile/person-links` (girişli)

`{personCode, firstName, lastName, phoneNumber, email?}`

Kod HMAC ile aranır. Eşleşmenin kabulü için şunların hepsi gerekir:
- Kişi ve yönetim alanı etkin, kişi profili tamamlanmış.
- Ad ve soyad (Türkçe büyük harf, boşluklar sadeleştirilmiş), telefon (E.164) kayıtla aynı. Kişide e-posta varsa e-posta da aynı.
- Kişinin bugün geçerli, etkin bir ev sahibi/kiracı/sakin ilişkisi ve etkin bir bağımsız bölümü var.
- Kişi başka bir hesaba bağlı değil ve kullanıcı aynı yönetim alanında başka bir kişiye bağlı değil.

Kabulde tek transaction'da (Property satırı kilitli): üyelik açılır/etkinleşir, `Person.LinkedUserId` atanır, kişi kodu silinir (tek kullanımlık) ve başarılı deneme kaydı yazılır. Uygulama oturumu yenileyince `/api/v1/mobile/session` OWNER/TENANT/RESIDENT bağlamını döner, sekmeler ve senkron açılır.

Her ret nedeni aynı yanıtı döner: `409 BF_MOBILE_LINK_REJECTED` (alan veya kişi varlığı sızmaz). Kullanıcı başına 15 dakikada 5 hatalı denemeden sonra doğru bilgiler de `BF_MOBILE_LINK_LOCKED` alır. Denemeler `person_link_attempts` tablosunda tutulur (kod değeri olmadan). Eşleşmeden sonra mobilde değiştirilen bilgiler merkezdeki kişi kaydını değiştirmez.

Kullanılmış kod eski kişi kod üretimi tarafından yeniden üretilmez; yönetici gerekirse "Kodu yenile" ile yeni kod verebilir. Merkez kişi ekranında bağlı kişide "Kullanıldı; kişi bir hesaba bağlı" görünür.

## Mobil uygulama

- Giriş ekranında "Hesap oluştur" → kullanıcı adı, parola ×2, ad, soyad, cep telefonu, e-posta, isteğe bağlı kişi kodu. Tekrar gönderimde aynı `requestId`.
- Bağlı kişi yoksa ana sayfa yalnız hesap kartını ve "Kişi kodunu gir" formunu gösterir; Ödemeler, Bildirimler ve Talepler sekmeleri gizlenir (sunucu zaten veri vermez). Hesabım ekranında profil düzenleme ve kod formu bulunur.
- Çevrimdışı: doğrulanmış oturum anlık görüntüsü (bağlamsız da olsa) mevcut şifreli SQLite önbelleğinde 24 saat açılır; kod girişi internet ister. Parola cihazda saklanmaz. Şema değişikliği gerekmedi (v7).

Migration: `20261015191218_MobileOnboarding` (`mobile_registrations`, `person_link_attempts`).
