# BF-VERIFY 01 — Ortak hesap iletişim sözleşmesi / ADR

Tarih:22.10.2026. Kullanıcı ilk uygulama fazını başlattı; görev [#44](https://github.com/AkaBayraK/BuildingFlow/issues/44).
Bu karar yalnız model/API/kalıcılık temelidir; SMS veya e-posta zorunluluğu uygulanmış değildir.

## Otorite ve kapsam

- Hesap JWT `sub` ile belirlenir. Kendi hesap verisinde Property/Unit/Person veya yönetici rolü gerekmiyor; adres/gövde ile başka hesap seçilemez.
- E-posta kaynağı her okumada `IKeycloakAdmin.GetAsync(sub)`; eski JWT `email_verified` değeri kullanılmaz. IdP kaydı/erişimi yoksa `UNKNOWN`, `available=false`, `value=null`; sahte VERIFIED veya eski claim'e fallback yok. Yerel e-posta tablosu/onay boolean'ı eklenmedi.
- Telefon kanıtı `AccountPhoneVerification : BaseEntity`, hesap başına tek satırdır. `verified_phone/key/at` ayrı; `pending_phone/key` ayrı. Kişi-hesap bağı veya maliklik/üyelik bu kayıttan türetilmez.
- Bu fazda `Confirm` yalnız domain metodu ve test fixture tarafından kullanılır; HTTP ile kanıt üretme/VERIFIED yazma yolu yok. Gerçek OTP proof handler FAZ03'tür.
- D1 aktivasyon/e-posta zorunluluğu,D2 mevcut hesap geçişi,D3 sağlayıcı/maliyet bu fazda etkinleştirilmez. D4 için yalnız mevcut veriyle uyumlu değer anahtarı tanımlanır; kod süresi/ülke gönderim desteği/deneme limiti sonraki fazdadır.

## Değerler ve eski istemci uyumluluğu

- Eski `MobileProfileDto` ve `MobileProfileCommand` şekli değişmedi. `Phone` hâlâ eski istemcinin girdiği profil değeridir; sahiplik kanıtı veya güvenilir SMS hedefi sayılmaz.
- Yeni `profileValue` bunu açıkça gösterir; `verifiedValue` son doğrulanmış hedef, `pendingValue` yeni adaydır. Yeni `/phone` komutu adayı hazırlar, eski profil alanını veya ismi/fotoğrafı değiştirmez. Onaydan sonra profil alanını eşitleme FAZ03 işleminde atomik ele alınır.
- Eski web/mobil profil komutunda gerçek telefon değeri değişirse, profil ile aynı transaction ve aynı hesap kilidi içinde aday/proof modeli güncellenir. İsim veya aynı normalize numara biçimi değişirse contact version/kanıt değişmez. Yeni aday varken isim kaydı adayı iptal etmez.
- Açık telefon kaldırma (eski profil komutunda null/boş) kanıtı ve adayı kaldırır. Yeni adaya geçiş eski doğrulanmış hedefi korur; eski doğrulanmış değere dönülürse bekleyen değişiklik iptal olur ve yeni onay gerekmez.
- Mevcut biçim sözleşmesi korunur:32 karaktere kadar,7–15 ASCII rakam ve `+ -()`; yeni aday komutu aynı temel denetimi yapar. Uluslararası açık `+` biçimi ve mevcut TR 10 hane/başında0 olan11 hane eşdeğer anahtara gider. Diğer belirsiz eski yerel biçimler `LOCAL:`/`RAW:` iç anahtarıyla korunur; bunlar kanıt olarak onaylanamaz. Bu iç anahtarlar DTO'ya çıkmaz.
- Eski kayıtların GET'i veri yazmaz. Satırı olmayan telefon `UNVERIFIED`; boş telefon `MISSING`; doğrulandı backfill'i yok. Geçici aday/staging, SMS gönderildi demek değildir.

## Yeni uçlar

| Uç | Yetki ve davranış |
| --- | --- |
| GET `/api/v1/account/contact-verification` | Yalnız kendi hesap durumu; JWT; bina seçimi gerekmez |
| GET `/api/v1/mobile/contact-verification` | Aynı store ve DTO; web ile aynı otorite |
| POST `/api/v1/account/contact-verification/phone` | Yalnız aday hazırlama; JSON `{operationId,expectedVersion,phone}` |
| POST `/api/v1/mobile/contact-verification/phone` | Aynı komut/transaction/replay sözleşmesi |

DTO: `{version,email:{status,value,available,checkedAt},phone:{status,profileValue,verifiedValue,pendingValue,verifiedAt,canSendCode,deliveryStatus,challengeId}}`.
Bu fazda `canSendCode=false`, `deliveryStatus=NOT_REQUESTED`, `challengeId=null`.

| Durum | Anlam |
| --- | --- |
| Email `MISSING/UNVERIFIED/VERIFIED` | Keycloak'tan o okumada elde edilen sonuç |
| Email `UNKNOWN`,available=false | Kaynak okunamadı/kayıt bulunamadı; onay varsayılmaz |
| Phone `MISSING` | Profil/kanıt/adaya ait numara yok veya açık kaldırma var |
| Phone `UNVERIFIED` | Eski profil numarası var, henüz yerel kanıt/aday kaydı yok |
| Phone `PENDING` | Aday var; eski verifiedValue varsa korunur; SMS gönderilmiş anlamına gelmez |
| Phone `VERIFIED` | Sunucunun kanıt kaydı var, yeni aday yok |

Komut sonucu `{operationId,replayed,contact}`. Contact version profil version'dan ayrıdır; isim/fotoğraf yazımı telefon adayını geçersiz kılmaz.

## Yarış, replay ve hata sözleşmesi

- Profil komutuyla aynı `profile:sub` advisory transaction kilidi, ardından `sub:operationId` kilidi. Bir hesaba iki eşzamanlı version0 isteğinden biri yazar; diğeri409.
- Ortak `MobileOperationReceipt` namespace'i kullanılır; fingerprint `contact.stage-phone.v1` amacı+sub+beklenen version+normalize telefon içerir. Profil komutu ile aynı operationId'nin başka amaç/içerikle kullanımı409'dur.
- Aynı UUID/içerik tek etki; retry yeni satır/sürüm/mesaj üretmez. Replay eski adayı geri yazmaz, **güncel** durumu döndürür. Makbuzda ham iletişim değeri/OTP yoktur; yalnız komut türü saklanır.
- Boş UUID,negatif version veya bozuk telefon:400 `BF_USER_INVALID`,alan `phone`; eski version/farklı içerik:409 `BF_MOBILE_SYNC_CONFLICT`. Mevcut TR/EN/FE registry kodları yeniden kullanıldı; yeni kod yok.
- Token yoksa401. Pasif/silinmiş kendi profil/kanıt kaydı403; sorgu filtreleri kaldırılırken açık owner kontrolü korunur. Başka hesaba yol yok. Bilinmeyen `userId`, `verified` gibi JSON alanları400;istemci sahiplik boolean'ı veremez.
- JSON olmayan415,gövde>4KiB413;sunucu body limit özelliği de ayarlanır. API mevcut no-store güvenlik hattındadır; yeni log/telemetri payload'ı yoktur.

## Şema ve geçiş

- EF migration `20261022142952_AccountPhoneVerificationFoundation`,Designer ve snapshot. Yalnız ekleyici tek tablo;eski tablolar/kolonlar değiştirilmedi,veri backfill'i yok.
- Sekiz ortak NOT NULL BaseEntity alanı,owner unique index,nullable proof/aday çiftleri için DB check constraint. UserId dış IdP kimliğidir;Person/Property FK'si oluşturulmaz.
- Down tablo doluysa (soft-delete dahil) reddeder;boş tablo geri alınabilir. Kod geri dönüşünde genişletilmiş tablo bırakılabilir;veri silerek rollback yapılmaz. Mevcut kimlik/finans/ilişki migration'ları değiştirilmez.
- Development AutoMigrate veya canlı sunucu başlatılmadı;şema yalnız test fixture'larında uygulanır. Gerçek rollout FAZ07'dir.

## Kabul sınırı

Bu fazın kanıtı domain/API/gerçek izole PostgreSQL ve eski profil uyumluluğudur. E-posta gönderimi/required-action FAZ02,gerçek telefon OTP FAZ03,web UX FAZ04,mobil UX FAZ05,uçtan uca kabul FAZ06 ve ortam açılışı FAZ07'dir. Aynı sonuca ulaşmak için yeni paket/SDK/container imajı gerekmiyor;test konteyneri mevcut PostgreSQL imajını indirimsiz yeniden kullanır.
