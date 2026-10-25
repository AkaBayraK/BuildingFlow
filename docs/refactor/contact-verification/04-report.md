# BF-VERIFY04 — Merkez ve yönetim web ekranları

23.10.2026 · görev [#47](https://github.com/AkaBayraK/BuildingFlow/issues/47) · sıra **5/08**, **3 resmi adım**. Kullanıcı FAZ03 gerçek SMS teslimi açıkken bu fazı açıkça başlattı; bu karar FAZ03’ü kapatmaz. FAZ05 bu tur kapsamında değildir.

## Yapılanlar ve kabul kanıtı

- **P04.A1:** `AccountPage.tsx` ortak `ContactVerification.tsx` panelini içerir. Button/Input/Label/ToneBadge/MessageBanner tekrar kullanıldı; `contact-style.ts` tek ikon eşlemesi ve `contact-verification.ts` strict unknown DTO sınırı. E-posta ve telefon durumları, maskeli hedef, gönder/yeniden gönder/kod/iptal; yalnız sunucunun VERIFIED sonucu kanıttır.
- **P04.A2:** Mevcut `/account` rotası ve `SessionProfileMenu` merkez/yönetim/portal kabuklarında ortak kalır. Yeni Property/rol kapısı eklenmedi. Gerçek API’de Property üyeliği olmayan RESIDENT; sentetik UI’da rolü ve alanı olmayan hesap paneli kullandı. Navigasyon testleri mevcut erişim sözleşmesini korur.
- **P04.A3:** İlk zorunlu e-posta mevcut Keycloak required action akışındadır. Sonraki e-posta işlemi yalnız tıklamada adapter `login({action:VERIFY_EMAIL|UPDATE_EMAIL})`; callback sabit `origin+/account`, query/hash taşınmaz. Telefon authenticated own-account komutudur. Tarayıcıda action/callback/locale kaydı kontrol edildi; güvenli PKCE/ilk giriş/native adres değişimi izole IdP testiyle ayrıca doğrulanır. Keycloak VERIFY_EMAIL AIA desteği [resmî sürüm kaynağında](https://github.com/keycloak/keycloak/blob/26.7.5/services/src/main/java/org/keycloak/authentication/requiredactions/VerifyEmail.java) da teyit edildi.
- **P04.A4:** Ref kilidi çift gönderimi engeller; belirsiz ağ hatasında aynı istek UUID/body bellekte korunur, otomatik mutation retry yoktur. CANCEL challengeId taşır. Sunucu version/challenge/attempt/retry/expiry alanları kullanılır; yerel sayaç UX içindir, karar sunucudadır. 409 yeniden okur; 429 ve workflow hataları ortak ikonlu bildirimdir. Tek bildirime indirmek için işlenen HTTP durumlarında ortak transport sessizdir. Kod bellekte, terminal/odak değişiminde temizlenir; storage veya log yoktur.
- **P04.A5:** Focus/visibility ve Durumu yenile sunucudan GET yapar; eski JWT email_verified kullanılmaz. Kişisel bilgiler e-postası da aynı güncel contact sonucundan beslenir; kaynak kesintisinde eski e-posta/doğrulama gösterilmez. Gerçek mobil contact API ile doğrulanan aynı hesap, web yenilemesinde VERIFIED oldu. Odak olayı ayrıca fixture düğmesiyle tetiklenerek GET sonucu görüldü; fiziksel mobil cihaz kullanıldığı iddia edilmez.
- **P04.A6:** İsim/fotoğraf profil yazarı değiştirilmedi. Gerçek API’de aynı normalize telefonla tekrar kayıt kanıtı korudu; değişen telefon eski doğrulanmış ve yeni bekleyen hedef olarak ayrı göründü. E-posta değişimi native IdP ekranında bekler: eski adres onaya kadar geçerlidir; auth-session içindeki bekleyen e-posta uygulama tablosuna/JWT’ye kopyalanmaz. Panel bunu açıkça anlatır ve native ekrana götürür.
- **P04.A7:** Users/davet yetkisi genişletilmedi; VERIFIED yazma ve başkasının OTP’sini okuma UI/API’si eklenmedi. Mevcut scoped yönetici davet sözleşmesi korunur. Bu tur yeniden çalışan negatif JWT/OTP/foreign owner/forged payload testleri 32 entegrasyon grubuna dahildir.
- **P04.A8:** TR/EN eş anahtarlar, ikonlu durumlar, semantic tema, label/aria-busy/one-time-code ve klavye/Enter/odak test edildi. 1440px: client/scroll1425px;375px:360px/360px, yatay taşma0. Açık/koyu/sistem (sistem tercihi açık çözümlendi) görüldü; OS tema tercihi değiştirilmedi.

## Bu tur çalıştırılan kontroller

| Kontrol | Sonuç |
| --- | --- |
| Başlangıç account/contact testleri | **7/7**,0 skip |
| `npm test` — son tam web kontrolü | **250/250**,0 skip |
| `npm run typecheck` | **GEÇTİ** |
| `npm run build` — son kaynak | **GEÇTİ**, mevcut kurulu Next16.4.3 |
| `python3 scripts/test-contact-web.py` + gerçek AccountPage/ortak transport/loopback HTTP köprüsü | **1/1**, gerçek izole PostgreSQL/API; SEND/yanlış kod/409/profile kaydı/mobil API→web yenileme |
| `python3 scripts/test-phone-otp.py` | **32/32** gerçek izole PostgreSQL/API/yerel taşıyıcı/JWT/migration + **16/16** unit,0 skip |
| `python3 scripts/test-contact-identity.py` | **12/12** gerçek Keycloak26.7.5/Mailpit kabul grubu |
| CUA sentetik HTTP negatifleri | **GEÇTİ**: çift tıklama1istek,doğru/yanlış kod,CANCEL,expiry,409,429,UNKNOWN,RATE_LIMITED3595sn,outage,odak,TR/EN/tema/klavye |

Gerçek API köprüsü yalnız test projesindedir: `PhoneOtpBrowserHost.cs`; JWT test sunucusunda kalır, uygulama endpoint’i eklenmez. SMS taşıyıcısı ve IdP dizin adaptörü bu tarayıcı testinde test çiftidir; **gerçek SMS teslimi değildir**. Native e-posta için ayrı gerçek Keycloak/Mailpit testi kullanılır. Test scripti kendi rastgele container/env/DB’sini temizler. Tarayıcı viewport override sıfırlandı; canlı `.env`,DB,realm,hesap,servis ve kullanıcı parolası değiştirilmedi.

Başlangıçta fixture’ın Next server-action import’u tarayıcıda process hatası verdi; test-only locale action alias’ı ve UTF-8 başlığı düzeltildi. Son yüklemelerde yeni runtime hatası görülmedi. İlk build mevcut Next16.4.0 güvenlik bildirimiyle durdu; tekrar derleme geçti. Eşzamanlı başka çalışmanın Next16.4.3 manifest/lock güncellemesi korunmuştur; bu faz bağımlılık kurmadı/güncellemedi, yeni lisans/paket/imaj eklemedi. Genel dirty tree veya index’in değişmediği iddia edilmez; BF-LATE/quality/Dockerfile/.nvmrc değişikliklerine müdahale edilmedi.

## Yeniden çalıştırma ve görseller

1. Web klasöründe `node tests/fixtures/verification-bundle.cjs`.
2. Kökten `python3 scripts/test-contact-web.py` (yalnız kendi geçici PostgreSQL’ini açar).
3. Web klasöründe `node tests/fixtures/verification-server.cjs /tmp/bf-verify04-ui --real`; tarayıcı `http://127.0.0.1:13047/account`.
4. Teste özel `fixture-external` ortak mobil API ile tamamlar; `fixture-stop` köprüyü kapatır. İkisinde de yalnız test `Bearer fixture-only` başlığı kullanılır. Bu değer gerçek kimlik bilgisi değildir.
5. HTTP hata durumları için `--real` olmadan ayrı sentetik fixture kullanılır. Gerçek backend kanıtı ve hata enjekte edilen fixture kanıtı ayrı tutulur.

[1440/TR/açık](evidence/04-web-light.png) · [375/EN/koyu, gerçek API](evidence/04-web-mobile-dark.png).

## Sınırlar ve teslim

Gerçek operatör SMS teslimi #46/FAZ03’te açıktır; PhoneEnabled kapalı varsayılanı korunur. FAZ05 mobil UI ve FAZ06 cihaz/regresyon kabulü burada tamamlanmış sayılmaz. Fiziksel telefon/gerçek kullanıcı veya canlı SMTP/SMS denenmedi. Commit/push/deploy yapılmadı.

Resmi adımlar01/02 tamam,03 kabul kontrolleri tamamlandı;uzak kapanış son okuması yapılır. Faz3/3=%100,00;tamamlanan BF-VERIFY fazları00/01/02/04=4/8=%50,00;bu tur3/3=%100,00. FAZ03 SMS engeli değişmedi.

Son kota kaynağı23.10.2026 Codex usage/rateLimitsByLimitId.codex:300dk%52kullanılan/%48kalan,reset23.10.2026 21:42:33;10080dk%24/%76,reset30.10.2026 11:03:51 Europe/Istanbul. Hesap kotası bu sohbetin tüketimi değildir. Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL.

Son uzak kontrol: **#47 CLOSED/COMPLETED;Project3 Done;12/12 checked,0unchecked**. FAZ04 tamamlandı.
