# İŞ19 [HESAP] — Hesap bilgilerim ve kullanıcı fotoğrafı

Tarih: 17.10.2026. Kullanıcı talebi: "merkez ekranında kullanıcı fotosu görünmüyor, burayda eklenmesi gerekiyor, kişi ile bağlantısı var ise kişi tarafındanda gözküsün, merkez tarafında da hesap bilgisi menüsü olsun trace flow da olduğu gibi ordaki işlemlerin aynısı burada da olsun. Hesap bilgilerimi kodlar mısın".

## Talep özeti

- Merkezde (web) giriş yapan kullanıcının fotoğrafı görünmüyor; merkezden de eklenebilmeli.
- Hesap bir kişi kaydına bağlıysa fotoğraf kişi ekranında da görünmeli.
- TraceFlow'daki profil sayfası (`web-next/src/pages/profile/index.tsx`, `components/avatar-picker.tsx`) karşılığı olarak "Hesap bilgilerim" menüsü ve aynı işlemler.

## Kararlar (varsayılanlar; kullanıcıya planla birlikte bildirildi)

1. **Tek fotoğraf:** merkez ve mobil aynı profil kaydını (`MobileProfile`, `MobileProfilePhoto`) kullanır. Yeni tablo veya migration yok.
2. **Fotoğrafı yalnız hesap sahibi ekler**, kendi PROFILE_PHOTO açık rızasıyla (İŞ17 kuralı). Merkezde verilen karar `ConsentChannel.WEB` ile kaydedilir. Yöneticiler fotoğrafı görür, ekleyemez/değiştiremez.
3. **Kapsam sunucuda:** kullanıcı fotoğrafı `UserService` kapsamı (SUPER_ADMIN tümü, SITE_ADMIN kendi alanları), kişi fotoğrafı `PeopleService.GetAsync` kapsamı (yönetim rolü + aktif üyelik). Muhasebeci kişi fotoğrafını göremez.
4. **Parola değişimi** uygulamada tutulmaz: Keycloak `UPDATE_PASSWORD` gerekli eylemiyle güvenli giriş sayfasında yapılır, sonra sayfaya dönülür. (TraceFlow'daki uygulama içi parola formu, BuildingFlow'da kimlik Keycloak'ta olduğu için bu şekilde karşılandı.)
5. **Yeni paket yok** ("hiçbir şekilde lisans kullanımı olmayacak"): görsel kırpma/küçültme tarayıcının canvas'ıyla, karma Web Crypto ile yapılır.
6. Ad/soyad yazım kuralı (17.10.2026 bakım) bu sayfada da uygulanır; hiç kaydedilmemiş profilde form Keycloak adlarıyla, kurala göre doldurulur.

## Faz ve task planı

| Task | İş | Dosyalar | Kabul kriteri | Bağımlılık |
|---|---|---|---|---|
| T19.1 | **Hesap API'si** — `GET /api/v1/account` (kendi rol, alanlar, profil), kendi profil komutu ve fotoğrafı, rıza listesi/kararı (WEB kanalı). | Api/AccountEndpoints.cs, Program.cs, Application/MobileConsents.cs (kanal parametresi), Infrastructure/MobileConsentStore.cs | Tokensız 401; başka hesabın fotoğrafı 404; rızasız fotoğraf 403; karar WEB kanalıyla; mobil aynı veriyi görür | İŞ17 |
| T19.2 | **Kullanıcı ve kişi fotoğraf uçları** — `UserResponse.PhotoId`, `GET /users/{id}/photo`, `GET /properties/{p}/people/{id}/photo`. | Application/Users.cs, MobileProfile.cs, Infrastructure/MobileProfileStore.cs, Api/UserEndpoints.cs, PeopleEndpoints.cs | Kapsam dışı yönetici, muhasebeci, bağsız kişi 404/403; geri çekmeden sonra her yerden kalkar | 19.1 |
| T19.3 | **Hesap bilgilerim sayfası** — fotoğraf (önce rıza; 512 px JPEG, ≤256 KB), ad/soyad/telefon (409 kurtarma), kullanıcı adı, e-posta, roller, bağlı alanlar, parola değiştir, Rızalarım (oku, onayla, geri çek). | app/(workspace)/account/page.tsx, features/account/*, components/AccountAvatar.tsx, messages/*.json | Rızasız seçici açılmaz; vazgeçince fotoğrafsız devam; metin karması uyuşmazsa onay yok; 1280/390 px taşma yok | 19.1 |
| T19.4 | **Üst menü** — sağ üstte fotoğraf ve "Ad SOYAD", menüde Hesap bilgilerim; başlık, arama girdisi, giriş dönüş listesi. | auth/AuthGate.tsx, components/WorkspaceShell.tsx, auth/return-path.ts | Fotoğraf yoksa baş harfler; kaydedince menü de yenilenir | 19.3 |
| T19.5 | **Kullanıcılar ve kişi ekranında fotoğraf** | features/users/*, features/people/PersonPage.tsx | Yalnız `photoId` varsa istek atılır; 404 sessiz | 19.2 |
| T19.6 | **Testler ve kabul** — backend/web testleri, gerçek Keycloak girişiyle parola değişimi ve fotoğraf kabulü. | AccountApiTests.cs, UserManagementTests.cs, tests/account.test.mjs, return-path.test.mjs | Kabul kullanıcıyla yapılmadıysa açık kalır | 19.1–19.5 |

## Bilinen sınırlar

- Profil uçları mobil oturum kuralını paylaşır: 500'den fazla bağlamı olan hesap (ör. çok sayıda alanı olan SUPER_ADMIN) profil okuyamaz (mobilde de böyle). Gerekirse ayrı faz.
- Kullanıcılar listesindeki ad/soyad Keycloak'tandır; hesap sahibinin profilde kaydettiği ad ile farklı olabilir (fotoğraf ortaktır).

## Yapılanlar

### İŞ19 — 17.10.2026 (kullanıcı talebi yukarıda; plan aynı gün bildirildi)

Ortam: cihaz klasörünün 17.10 kopyası (dev4) kapsayıcıda derlendi ve test edildi (.NET 10, ayrı PostgreSQL `buildingflow_test`, SeaweedFS S3), sonra cihaz klasörüne yama ile aktarıldı. Cihazdaki Kişi Refaktörü değişikliklerine dokunulmadı. **Yeni paket yok:** csproj, package.json ve lock dosyaları değişmedi.

**T19.1 Hesap API'si — TAMAMLANDI.**
- `Api/AccountEndpoints.cs` (yeni): `AccountDto(UserId, Username, Email, Roles, Contexts, HasMore, Profile)`; `GET /api/v1/account`, `GET /account/profile/photo/{id}`, `POST /account/profile/commands` (≤384 KB, 413/415), `GET /account/consents`, `POST /account/consents/commands` (≤16 KB). `Program.cs` `MapAccountEndpoints()`.
- `IMobileConsentStore.CommandAsync(..., ConsentChannel channel = MOBILE)`; merkez `WEB` gönderir, `UserConsent` kanal sütununa yazılır.
- Test: `AccountApiTests.AccountShowsOwnRolesPropertiesAndProfileAndRecordsWebConsent` (401, kendi rol/alan, ad kuralı "Ayşe Nur YILMAZ", WEB kanalı, mobil aynı profil ve rızayı görür).

**T19.2 Kullanıcı ve kişi fotoğraf uçları — TAMAMLANDI.**
- `IMobileProfileStore.CurrentPhotosAsync(users)` (liste için tek sorgu) ve `AccountPhotoAsync(user)`; `UserService` isteğe bağlı `IMobileProfileStore` alır, `PhotoAsync` `Visible` kapsamıyla, `UserResponse.PhotoId`.
- `GET /api/v1/users/{userId}/photo`, `GET /api/v1/properties/{p}/people/{personId}/photo` (`LinkedUserId` üzerinden). Hepsi `image/jpeg`; `/api` yanıtları zaten `no-store`.
- Test: `AccountPhotoIsSharedWithMobileAndVisibleOnlyThroughALinkedPersonInScope` (rızasız 403, aynı bayt mobil uçta, bağlı kişi 200, bağsız 404, başka alan yöneticisi ve muhasebeci reddedilir, başka hesap kendi uçlarından 404, geri çekme fotoğrafı her yerden kaldırır); `UserManagementTests.UserPhotoIsVisibleOnlyWithinManagerScope`.

**T19.3 Hesap bilgilerim sayfası — TAMAMLANDI.**
- `features/account/model.ts` (sıkı DTO ayrıştırıcıları: hesap başka kullanıcıya aitse, bilinmeyen rol, >500 bağlam, fotoğraf kimliği/karma tutarsızlığı, metin–durum uyuşmazlığı reddedilir; onay/geri çekme/profil komutları), `photo.ts` (JPEG/PNG/WebP, ≤20 MB kaynak; ortadan kare kırpma, 512 px JPEG, kalite düşürerek ≤256 KB; SHA-256), `useAccount.ts` (sayfa ve üst menü ortak yenileme), `ConsentDialog.tsx` (tam metin, karma kontrolü, kutu işaretlenmeden onay yok), `AccountPage.tsx`, `app/(workspace)/account/page.tsx`, `components/AccountAvatar.tsx` (fotoğraf yetkili istekle blob olarak alınır; yoksa baş harfler).
- Fotoğraf ekle: rıza onaylı değilse önce rıza penceresi açılır; onaylayınca dosya seçici açılır, "Onaylamadan devam et" fotoğrafsız bırakır. Sunucu 403 `BF_CONSENT_REQUIRED` dönerse aynı pencere açılır. 409'da güncel hâl yüklenip kullanıcıya tekrar kaydetmesi söylenir.
- Rızalarım: tür başına durum rozeti, sürüm/tarih, Metni oku / Oku ve onayla, Rızayı geri çek (etkisini anlatan onayla). Geçmiş gösterilmez (yalnız merkez Açık Rıza menüsü).
- `services/authorized-request.ts` ve `useAuthorizedApi` beklenen durumlar (`[403,404]` gibi) için genel hata bildirimini susturan isteğe bağlı parametre aldı.
- TR/EN `account` sözlüğü ve `ui.navAccount`.

**T19.4 Üst menü — TAMAMLANDI.** `SessionControls` profil adını ("Ad SOYAD", yoksa Keycloak adı) ve fotoğrafı gösterir; menüde "Hesap bilgilerim". `WorkspaceShell` başlık ve arama girdisi. `return-path.ts` listesine `/account`, ayrıca daha önce eksik olan `/users` ve `/sms` eklendi (girişten sonra bu sayfalara dönülemiyordu); testi güncellendi.

**T19.5 Kullanıcı ve kişi ekranında fotoğraf — TAMAMLANDI.** Kullanıcılar listesinde ad sütununda fotoğraf (yalnız `photoId` varsa istek), kişi sayfasında "Kişi bilgileri" başlığı yanında bağlı hesabın fotoğrafı.

**T19.6 Testler ve kabul — AÇIK.**
- Backend: `dotnet test` birim 184/184; entegrasyon 520/520 geçti, 22 test RabbitMQ/SMTP aracı kapsayıcıda olmadığı için atlandı (hesap ve fotoğraf testleri dahil depolama testleri koştu).
- Web: `tests/account.test.mjs` 4/4, `npm test` 123/123, `npx tsc --noEmit` temiz, `npm run build` başarılı.
- Tarayıcı (Playwright, taklit oturum ve API, 1280 ve 390 px, yatay taşma 0): Hesap bilgilerim, Rızalarım, üst menü, fotoğraf rıza penceresi, Kullanıcılar, kişi sayfası. Gerçek dosya akışı: 3000×2000 PNG seçildi, tarayıcıda 512 px JPEG'e (11,5 KB) çevrilip `REPLACE` komutuyla gönderildi, menü ve sayfa yenilendi. Görüntüler `/mnt/project-files/analysis/is19-screens/`.
- Açık: gerçek Keycloak girişiyle parola değişimi dönüşü, gerçek fotoğraf yükleme ve mobil ile aynı fotoğrafın görünmesi kullanıcıyla kabul edilecek.

### İŞ19 ek — 17.10.2026 (kullanıcı bildirimi: "kişilerde resim görünmüyor, roller renkli değil, hesap bilgilerimdeki butonlar standart değil renkli değil")

- **Kişiler listesinde fotoğraf:** `PersonResponse.LinkedPhotoId` (bağlı hesabın güncel fotoğrafı, `PeopleStore.Project` alt sorgusu; yoksa null). Liste ve kişi sayfası yalnız bu alan doluysa fotoğraf ister; boş kişiler için 404 isteği yapılmaz. Web `parsePerson` alanı yalnız GUID ve bağlı hesapla kabul eder. Mobil kartta da fotoğraf. Test: `AccountApiTests` liste/detay `linkedPhotoId` ve geri çekmede null; `management.test.mjs` sözleşme.
- **Renkli rol rozeti (ortak):** `WorkspacePresentation` içinde `ToneBadge` (ortak `--message-*` tonları ve `globals.css`'e eklenen `--tone-violet-*`, `--tone-teal-*`) ve `RoleBadge` (her rol için tek renk ve ikon: Süper yönetici mor, Site yöneticisi mavi, Yönetici turkuaz, Muhasebeci sarı, Malik yeşil, Kiracı mavi, Sakin gri). Kullanıldığı yerler: Kullanıcılar listesi ve mobil kartı, Hesap bilgilerim rolleri ve bağlı alanlar, Kişiler listesi ilişki rolü ve özet kartı, kişi ilişkileri, daire kayıt ayrıntısı. Renk tek bilgi kaynağı değildir (ikon ve metin var).
- **Standart düğmeler:** Hesap bilgilerimde ana eylemler birincil (mavi) düğme: Fotoğraf ekle/değiştir, Kaydet, Parolayı değiştir, Oku ve onayla; silen eylemler kırmızı `destructive`: Fotoğrafı kaldır, Rızayı geri çek; Metni oku ikincil (outline). Rıza durumları renkli rozet: Onaylı/Okundu yeşil, Yeni sürüm sarı, Onaylanmadı mavi, Geri çekildi kırmızı. Kullanıcılar listesindeki Aktif/Pasif ortak `StatusBadge`.
- Doğrulama: backend birim 184/184, entegrasyon 520/520 (22 RabbitMQ/SMTP atlandı); web 123/123, typecheck, build; Playwright 1280/390 px taşma 0; görüntüler `is19-screens/` (09-kisiler eklendi, diğerleri güncellendi).

### İŞ19 ek 2 — 17.10.2026 (kullanıcı bildirimi: "telefon inputu böyle olmaz, bir standardı vardır nasıl gireceğini yönlendirir +9 eklenecek ise gibi, email de input farklı olur")

- **Ortak kural** `frontend/buildingflow-web/src/lib/contact.ts` (paket yok): `phoneChars` yazarken yalnız telefon karakterleri; `formatPhone` alan bırakılınca Türkiye numarasını `+90 5xx xxx xx xx` (3-3-2-2) yapar (0532…, 532…, 90532…, 0090… girişleri), yurt dışı `+ülke kodu` korunur; `validPhone` Türkiye için +90 ve 2–5 ile başlayan 10 hane, yurt dışı 8–15 hane; `normalizeEmail` (boşluksuz, küçük harf), `validEmail` (tek @, noktalı alan adı, ≤254).
- **Ortak alanlar** `components/forms/ContactInputs.tsx`: `PhoneInput` (telefon ikonu, telefon klavyesi, `+90 5xx xxx xx xx` örneği, yönlendiren yardım metni, hatalıysa alan altında örnekli kırmızı uyarı) ve `EmailInput` (e-posta ikonu ve klavyesi, büyük harf/yazım düzeltme kapalı, `ornek@eposta.com` örneği, hatalıysa uyarı). TR/EN `ui.phoneField*`, `ui.emailField*`.
- **Bağlanan formlar:** Hesap bilgilerim telefonu (kaydederken biçimlenir, hatalıysa gönderilmez; ad kuralı notu ad/soyad altına taşındı), kişi ekle/düzenle telefon ve e-posta (`EntityEditor` kaydederken aynı kural, hata alan altında), kullanıcı ekle/düzenle e-postası, SMS gönder telefonu. Mobil profil düzenleme telefonu aynı kurala bağlandı (`mobile/.../src/profile/phone.ts`, örnek, yardım metni, kaydetmeden önce kontrol).
- Doğrulama: web `tests/contact.test.mjs` 3/3, `npm test` 126/126, typecheck, build; mobil `tests/phone.test.mjs` 2/2, `npm run check` 108/108; Playwright: "05321234567" → "+90 532 123 45 67", "0532 12" uyarı, kişi formunda "5551234567" → "+90 555 123 45 67", "Ayse.Kaya@Ornek" → küçük harf ve uyarı. Görüntüler `is19-screens/10-telefon-alani.png`, `11-kisi-formu-iletisim.png`.
- Not: kayıt ekranı ve kişi kodu kartındaki telefon alanları (mobil) kişi eşleştirmesinde kullanıldığı için bu turda değiştirilmedi.

### İŞ19 ek 3 — 17.10.2026 (kullanıcı bildirimi: "butonlar konusunda yenileme ve görünüm buttonları beyaz, bir button formatı belirleyelim"; seçenek görseli `is19-screens/12-dugme-secenekleri.png`, seçim "A · Yumuşak renkli")

- **Ortak bileşen** `components/ui/button.tsx`: yeni varyantlar `view` (açık mavi #edf4ff / yazı #0866e8 / kenar #cfe0fb), `edit` (turkuaz #f0fdfa / #0f766e / #99f6e4), `neutral` (gri #f4f6f9 / #385374 / #e2e8f0), `warn` (amber #fffbeb / #92400e / #fde68a), `danger` (açık kırmızı #fef2f2 / #b4233d / #fecaca), `field` (form benzeri seçim, beyaz). `outline` artık `neutral` ile aynı gri tondur. Renkler `globals.css` `--action-*` token'larında (bg/hover/border/text). Paket yok.
- **Ekranlara uygulama** (src altında 275 düğme tarandı, ikon ve metin anahtarına göre): RefreshCw/Eye/History/ListFilter/FileText/Download/ArrowUpRight/ReceiptText/Copy/Phone ve "Tekrar dene/Detay/Okumalar/Güncel kaydı yükle" → `view`; Pencil/UserPen ve "Düzenle/Düzelt" → `edit`; KeyRound ve "İlişkiyi bitir" → `warn`; Trash2/Unlink/CircleX (iptal başlatan) ve ödeme "Kaldır" → `danger`; ghost sayfalama/vazgeç/temizle/sıfırla/bağla → `outline`; `secondary` düğmeler `view`; yönetim alanı türü kartları, ödeme bölüm listesi, kişi hesap seçicisi → `field`, seçim listelerinde seçili öğe `view`. Onay pencerelerindeki son silme/iptal `destructive` olarak kaldı. Menü, zil, kapatma X ve sıralama başlıkları `ghost`.
- **Hesap bilgilerim:** Fotoğrafı kaldır ve Rızayı geri çek `danger`, Parolayı değiştir `warn`, Metni oku `view`; kaldırma onayı `destructive`.
- Doğrulama: `tests/buttons.test.mjs` 3/3 (varyant ve token tanımı, outline beyaz değil, ikon/anlam eşlemesi, `secondary` düğme yok, Hesap sayfası eşlemesi), `npm test` 129/129, typecheck, build; Playwright 1280/390 px Hesap bilgilerim, Kullanıcılar, Kişiler, kişi sayfası, yatay taşma 0. Görüntüler `is19-screens/13-dugme-hesap.png`, `14-dugme-kullanicilar.png`, `15-dugme-kisiler.png`.
