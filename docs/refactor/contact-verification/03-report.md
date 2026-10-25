# BF-VERIFY03 — Telefon OTP / yerel kabul

Son Codex usage okuması23.10.2026;hesap kotası sohbet token tüketimi değildir.
300 dk: %80 kullanıldı/%20 kaldı;reset 23.10.2026 16:03:51 Europe/Istanbul.
10080 dk: %12 kullanıldı/%88 kaldı;reset 30.10.2026 11:03:51 Europe/Istanbul.
Giriş token: BİLGİ MEVCUT DEĞİL.
Çıkış token: BİLGİ MEVCUT DEĞİL.
Toplam token: BİLGİ MEVCUT DEĞİL.
Kalan context: BİLGİ MEVCUT DEĞİL.

23.10.2026 · Liste4/08 · [Görev#46](https://github.com/AkaBayraK/BuildingFlow/issues/46) **açık/In progress**.
Kodlama ve yerel kabul tamam; resmi adımlar2/3=%66,67. Son zorunlu gerçek operatör teslimi eksiktir. Tamamlanmış BF-VERIFY planı3/8=%37,50;FAZ04 başlatılmadı.

## Kullanıcı kararları

- D3: Kullanıcı seçenekleri araştırmamı istedi, ardından **Netgsm / platform hesabı** seçti. Bina kredisi kullanılmaz. Gerçek sağlayıcı hesabı henüz yok; kullanıcı sağlayıcısız yerel testleri açıkça istedi. Abonelik, ödeme, gönderici başlığı veya gerçek test alıcısı oluşturulmadı.
- D4: **6 hane /5dk/5 yanlış deneme/60sn yeniden gönderim**; hesap ve hedef başına5/saat,IP20/saat;yalnız+90 Türkiye cep. Domain `PhoneOtpRules` tek politika kaynağıdır. Yeni SDK/paket yerine .NET BCL/HttpClient kullanıldı.
- [Netgsm resmî fiyatı](https://www.netgsm.com.tr/fiyatlar/otp-sms):standart1.000 OTP400TL; yeni abonelikte316TL, vergiler dahil,1yıl. [iletiMerkezi karşılaştırması](https://www.iletimerkezi.com/toplu-sms-fiyatlari):1.000 SMS359TL;[OTP ek abonelik istemiyor](https://www.iletimerkezi.com/sikca-sorulan-sorular). Fiyatlar23.10.2026'da kontrol edildi;hizmet bedeli yazılım paketi lisansı değildir.

## Yapılanlar / kabul kanıtı

- **P03.A1 — KISMİ:** Sağlayıcı/platform maliyeti seçildi,HTTP/SDK alternatifleri incelendi. Netgsm API alt kullanıcı/OTP paketi/onaylı3–11 karakter başlık ve izinli test alıcısı henüz hazır değil;bu madde tamamen tiklenmez.
- **P03.A2:** `NetgsmPhoneOtpSender` ayrı `IPhoneOtpSender` platform taşıyıcısıdır;Property/SmsAccount/şablon/kredi parametresi almaz. Testte alan üyeliği üretmediği ve bina SMS kredi hareketi yazmadığı doğrulandı.
- **P03.A3:** Web ve mobil kendi JWT sub'ıyla ortak SEND/VERIFY/CANCEL servisine bağlı. Aktif profil/proof kontrolü,expectedVersion409,challenge GUID+hesap+hedef+purpose HMAC bağı var. Başka hesabın gerçek challenge/kodu SUPER_ADMIN ile dahi reddedilir. JSON'a userId/verified ekleme400,anonim401.
- **P03.A4:** `PhoneOtpSecrets` ayrı32+byte hex OtpKey ister;development key'e fallback yok. HKDF ile ayrı proof/delivery anahtarları,RNG.GetInt32,amaç bağlı HMACSHA256 ve fixed-time karşılaştırma. HMAC/şifreleme ve yanlış anahtar negatifleri geçti. Hesap/hedef/IP kilitleri ve kalıcı1saat kayıtları tüm API instance'larında aynıDB'de uygulanır. Yanlış kodun5.denemesi kilitler;expiry sınırında kabul yok.
- **P03.A5:** `MobileOperationReceipt` yeniden kullanıldı;aynı UUID yalnız mevcut sonucu okur,yeniden SMS üretmez. VERIFY fingerprint'i kodu düz hash yerine HMAC ile bağlar;makbuz JSON'unda kod/telefon yok. Yeni gönderimde eski challenge iptal edilir. Çift SEND ve eşzamanlı VERIFY testi tek etki;aynı UUID farklı içerik409;yanlış kod replay'i denemeyi artırmaz.
- **P03.A6:** Önce reservation+makbuz commit edilir;ilk çağrı ancak sonra tek kez HTTP gönderir. Replay RESERVED durumunda bile dispatch etmez ve UNKNOWN döndürür. Taşıyıcıda otomatik retry yok;timeout/5xx/bozuk-belirsiz yanıt UNKNOWN,belgelenmiş ret FAILED. Açık yeniden gönderim yeni UUID ile60sn sonradır ve eskisini geçersiz kılar. Gönderim anında aday/expiry hesap kilidi altında kontrol edilir;geç gönderim kuyruğu yok. HTTP başlıkları ve gövde okuması birlikte10sn sınırında;response8192byte/karakter sınırı/DTD yasak,redirect kapalı.
- **P03.A7:** Payload mevcut AES-GCM `SmsPayloadProtector` algoritmasıyla ayrı OTP purpose/key kullanır;kripto kodu çoğaltılmadı. Kalıcı teslim gövdesi yalnız şifreli reservation sırasında var,sonuçta veya expiry/iptal/kilit/doğrulamada temizlenir. Mevcut SMS worker döngüsüne sınırlı expiry temizliği eklendi. OTP sıradan SmsMessage/şablon listesine yazılmaz;own-account DTO'da kod/MAC/payload yok. Eski SMS şifreleme/teslim regresyonu geçti.
- **P03.A8:** Eski doğrulanmış değer adayın onayına kadar korunur;başarılı VERIFY profile+proof'u tek transaction'da günceller. Aynı adayın v1 mobil kaydı ve isim düzenlemesi açık kodu iptal etmez. Farklı telefon/boşaltma/eski değere dönüş mevcut Stage/Observe adaptöründe challenge'ı iptal eder. Mobil/merkez okuması aynı kanıtı döndürür;profil isim/fotoğrafını değiştirmek için doğrulama şartı eklenmedi.
- **P03.A9 — AÇIK:** Gerçek Netgsm/operatör/telefon teslimi yapılmadı. Yerel sunucu ve test sender'ı gerçek SMS gibi raporlanmadı. Hesap/OTP paketi/gönderici ve yetkilendirilmiş test alıcısı olmadan bu madde tamamlanmaz.
- **P03.A10:** PhoneEnabled defaultfalse;gönderici veya ayrı anahtar eksikse SEND UNAVAILABLE,noHTTP. Önceden teslim edilmiş kod anahtar mevcutsa sağlayıcı kapalıyken de doğrulanabilir;CANCEL gönderici kapalıyken çalışır. ACCEPTED yalnız sağlayıcı kabulüdür,VERIFIED değildir.

## Bu tur gerçekten çalıştırılan testler

`python3 scripts/test-phone-otp.py`:cached PostgreSQL18.6 ile yalnız geçici `buildingflow-test-otp-*` konteyner/DB/env oluşturur,sonunda temizler. Gerçek kullanıcı/env/DB/servis bilgilerini okumaz veya değiştirmez. Gerçek Netgsm ağına bağlanmaz. Seçili dotnet test filtresi scriptte tam olarak yer alır.

- **32/32 entegrasyon,0 skip:** gerçek loopback HTTP Netgsm XML sözleşmesi;başarı/hata/bozukXML/XXE;simüle timeout;HMAC/AES;7 OTP API senaryosu;gerçek ayrı PostgreSQL migrations/ilk-yükseltme-tekrar/idempotent SQL/BaseEntity tüm tablo şeması/audit;Down'ınsoft-delete geçmişte de reddi;önceki contact/adaptör ve JWT negatifleri;mevcut SMS teslim/şifreleme regresyonu.
- **16/16 birim,0 skip:**10 yeni politika/domain ve6 mevcut account proof testi.5.deneme kilidi,expiry sınırı,tek kullanım,iptal,TR cep vekod formatı.
- **Worker build0hata0uyarı:**`dotnet build backend/src/BuildingFlow.Worker/BuildingFlow.Worker.csproj --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --verbosity minimal`. API/Infrastructure/test projeleri test koşusunda da derlendi.
- **9/9 web standart/sözlük testi:**yol haritasındaki BF-VERIFY03 kanıt metni için `node --test tests/standards.test.mjs`. UI kodu değişmedi;web typecheck/build/mobil check/build/cihaz bu tur çalıştırılmadı.
- İlgili diff/JSON/Python/bağlantı/manifest koruma kontrolleri ve geçici konteyner temizliği ayrıca kontrol edildi. Restore/install/yeni paket yok;metadata envanteri yeniden üretilmedi.

Ara hatalar:ilk VSTest sandbox soketi izinli tekrar gerektirdi;örnek sender uzunluğu ve istisna tipi testte düzeltildi;loopback sunucusununHost allowlist'i127.0.0.1'e açıldı. Bu ara denemeler başarılı sayılmadı. Son aynı aday/isim regresyonunu içeren koşu32+16 tamamen geçti.

## API ve dosyalar

- Yeni `Application/PhoneOtp.cs`, `Domain/PhoneOtpChallenge.cs`, `Infrastructure/{PhoneOtpStore,PhoneOtpSecrets,PhoneOtpConfiguration,NetgsmPhoneOtpSender}.cs`.
- Mevcut ContactVerification DTO/store/endpoints,DI,SmsPayloadProtector,Worker/MessagingWorkers ve appsettings güncellendi.
- Yeni migration **20261023082825_AccountPhoneOtp** +Designer+snapshot:yalnız `phone_otp_challenges` tablosu,8zorunlu BaseEntity alanı;telefon/IP yerine HMAC hedef/IP,şifreli geçici payload. Geçmiş doluyken Down reddedilir;mevcut migration değiştirilmedi,backfill yok.
- `PhoneOtp{Tests,TransportTests,ApiTests,MigrationTests}.cs` ve tekrar çalıştırılabilir test scripti eklendi.
- Komut/web-mobil DTO sözleşmesi [account.md](../../api/account.md),[mobile-profile.md](../../api/mobile-profile.md);test kanıtı [TESTING.md](../../TESTING.md). Görev checkbox'ları#46'da;bu belge ikinci backlog değildir.

## Yerel kabul ile gerçek kabul ayrımı

Yerel olarak gönderim,doğrulama,replay,limit/TTL/race ve migration test edilebilir ve test edildi. Kod test sunucusunda bellekte tutulup doğrulamaya verilir;üretimde kod döndüren endpoint,MOCK başarı yolu veya bypass eklenmedi. Gerçek cihaz/operatör teslimi için Netgsm hesabı gerekir. Kaynakta `PhoneEnabled=false` kalır;live migration,provider ayarı,gönderim,abonelik/satın alma veya deployment yapılmadı.

İleri devreye almada migration API/Worker'dan önce uygulanır. `ContactVerification:PhoneEnabled`, `Netgsm:Usercode|Password|Sender` (ContactVerification altında) ve ayrı `ContactVerification:OtpKey` güvenli sunucu yapılandırmasıdır;anahtar/parola sohbet,Git veyaNEXT_PUBLIC içine konmaz. Netgsm onaylı başlık/OTP paketini hesapta hazır etmek ve tek test alıcısını yetkilendirmek gerekir. SDK gerekmiyor. Anahtar değişimi aktif challenge'ları etkiler;kontrollü geçiş gerekir.

Netgsm belgeleri yalnız OTP'nin3dk içinde taşıyıcıya teslimini sınırlar;uygulamanın5dk kod süresi ayrı kavramdır. Provider mesaj kimliği kabul kaydıdır,operatör teslim raporu değildir. [Resmî API](https://www.netgsm.com.tr/dokuman/) için XML POST `https://api.netgsm.com.tr/sms/send/otp`,yerel10haneli hedef,ASCII tek-parça içerik kullanıldı. Belgelenmemiş dış idempotency garantisi varsayılmadı.

**Kalan:**A1'in gerçek gönderici hesabı hazırlığı,A9 gerçek SMS kabulü ve buna bağlı K2/K4 kapanış kanıtı. #46 kapanmaz;FAZ04 başlamaz. Başka sohbetlerin quality/Dockerfile/.nvmrc/progress/lisans değişiklikleri korundu;bu çalışma commit/push yapmadı.


Son dar doğrulama:başlıktan sonra gövdesi takılan gerçek loopback HTTP yanıtı eklendi. Taşıyıcı başlık+gövde okumalarını aynı cancellation bütçesinde sınırlar;32+16koşusundan sonra yalnız etkilenen **PhoneOtpTransportTests3/3** yeniden çalıştırıldı ve geçti. Böylece sonraki dar tekrar önceki PostgreSQL testleri yeniden çalışmış gibi gösterilmedi.
