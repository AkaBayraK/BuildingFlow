# BF-VERIFY FAZ 00 — Ortak iletişim doğrulaması analiz ve plan teslimi

Tarih: **22.10.2026**. Plan: **8 faz (00–07)**; bu faz sıra **01/08**, resmi adım **3**.
Görev: [#43](https://github.com/AkaBayraK/BuildingFlow/issues/43). Pano: [@BuildingFlow / Project 3](https://github.com/users/AkaBayraK/projects/3).
Bu tur kullanıcı kapsamı: **Önce prompt, görevler ve analiz fazı**. Kodlama fazları başlamadı.
Durum: **TAMAMLANDI — #43 Closed(completed), Project 3 Done, açık madde 0**. Aşağıdaki tespitler kaynak incelemesidir; canlı IdP/SMTP/SMS veya cihaz davranışı testi değildir.

## 1. Kullanıcı ihtiyacı ve kapsam

- Cep ve e-posta sahipliği doğrulamasının zamanlaması açık olmalı; yöneticinin kişi kaydetmesi sırasında karşı tarafın kod girmesi beklenmez.
- Hesap sahibi mobil veya merkez/yönetim ekranındaki kendi hesabından işlemi başlatıp tamamlayabilmeli.
- Aynı hesap+kanal+normalize değer için kanıt ortak sunucuda olmalı; cihaz/rol/bina/daire değişiminde tekrar istenmemeli.
- İletişim değişince yalnız değişen kanal için yeni sahiplik onayı; değişmeyen ad/fotoğraf veya profil kaydı onayı sıfırlamamalı.
- Yeni refactor promptlar ve GitHub görevleriyle fazlara bölünmeli; her görevde yapılanlar ayrıntılı maddelerle, eksik kalmadığında kapanış.
- Bu tur yalnız belge, analiz ve görev yönetimi. Gerçek .env, hesap/parola, Keycloak/SMTP/SMS, DB/migration, kaynak kod ve bağımlılık değiştirilmez. Commit/push/deploy yok.

## 2. Mevcut kaynak haritası ve bulgular

| Kaynak | Mevcut davranış / etki | Güven ve sınır |
| --- | --- | --- |
| `backend/src/BuildingFlow.Application/Users.cs:132–180` | Yönetici oluşturması scope/rol/telefonu denetler; Keycloak kaydı+roller/parola ve yerel profil/üyelik hazırlığı. Geçici parola yoksa yalnız UPDATE_PASSWORD aksiyon maili ister; ilk mail IdentityUnavailable hatası yakalanıp oluşturma başarısı dönebilir. Update e-posta değişimini doğrudan IdP'ye iletir. | Yüksek, kaynak. Gerçek mail teslimi veya e-posta onay sonucu test edilmedi. |
| `backend/src/BuildingFlow.Infrastructure/KeycloakAdminClient.cs:54–70,91–96` | Yeni kullanıcıda emailVerified=false; Update'de açık bir pending/onay iş akışı kurulmamış. execute-actions-email için lifespan=86400 kullanılıyor. | Yüksek, kaynak; bu süre yeni OTP politikası değildir. UPDATE_PASSWORD tokenının e-postayı hangi koşulda doğruladığı sürüm/akış testinde kanıtlanmalı. |
| `backend/src/BuildingFlow.Application/MobileOnboarding.cs:45–74` | Mobil kayıt kendi CreateAsync/SetPassword/SaveRegistration yoluna sahip; yalnız yönetici oluşturmayı değiştirmek yeterli değil. | Yüksek. Person eşleştirmesi iletişim sahiplik kanıtı değildir. |
| `backend/src/BuildingFlow.Domain/MobileProfile.cs`, `Infrastructure/MobileProfileStore.cs:40–51` | Telefon profil alanı ve biçim kontrolü var; PhoneVerified/VerifiedAt/challenge yaşam döngüsü yok. Profil kaydı telefonu doğrudan değiştirir. | Yüksek. Gelecekte eski yazım yolu onayı bypass etmemeli. |
| `backend/src/BuildingFlow.Api/AccountEndpoints.cs:19–38` | Kendi hesap GET/profil komutları JWT ile IMobileProfileStore kullanır; e-posta claim'den okunur. | Yüksek. Eski JWT tazeliği yeni doğrulama için tek otorite olamaz. |
| `backend/src/BuildingFlow.Api/MobileProfileEndpoints.cs:6–18` | Mobil de aynı IMobileProfileStore okuma/yazımı kullanır. | Yüksek. Ortak durum yeni servis/modelde paylaşılabilir. |
| `frontend/buildingflow-web/src/features/account/AccountPage.tsx` | Kişisel bilgilerde salt okunur e-posta ve profil telefonu; SMS doğrulama arayüzü yok. | Yüksek. Merkez/yönetim ortak /account yolu korunur. |
| `frontend/buildingflow-web/src/components/WorkspaceShell.tsx:81` | ContextShell merkez/portföy kabuğunda /account bağlantısı var. | Yüksek. Kendi hesabına erişim Property seçimine bağlanmamalı. |
| `mobile/buildingflow-mobile/src/profile/useProfile.ts`, `profile/engine.ts` | Mevcut mobil profil /api/v1/mobile/profile ve commands tüketicisi. | Yüksek. App foreground ve offline politikası yeni doğrulamaya uyarlanmalı. |
| `backend/src/BuildingFlow.Application/PersonAccountLinks.cs` | Yönetici onaylı kişi-hesap bağlama kodu ayrı güven sözleşmesidir. | Yüksek. SMS/e-posta onayı bu bağa veya maliklik/kiracılığa dönüşmez. |
| `backend/src/BuildingFlow.Domain/Sms.cs:3`, `Infrastructure/SmsDelivery.cs:39`, `DependencyInjection.cs:41` | SmsProvider yalnız MOCK; MockSmsProvider kayıtlı. Mevcut kuyruk Property/SmsAccount/kredi kapsamındadır. | Yüksek. Gerçek SMS sağlayıcısı/teslim altyapısı yok; üretim başarısı iddia edilemez. |
| `deploy/keycloak/realm-buildingflow.json`, `scripts/configure-identity.sh` | Kaynakta zorunlu verifyEmail/VERIFY_EMAIL kurulumu açık değil; geliştirme SMTP örneği Mailpit. | Yüksek kaynak, çalışan realm ayarı bilinmiyor. |
| `deploy/docker-compose.yml` | Varsayılan harici Keycloak host adı keycloak-26.7.5'e işaret ediyor. | Bu bir çalışma zamanı sürüm ölçümü değildir. FAZ 02 gerçek sürümü izole ortamda doğrular. |
| `docs/api/account.md`, `mobile-profile.md`, `users.md` | Mevcut kendi profil ve kullanıcı yetki sözleşmeleri. | Yeni uç/DTO adları FAZ 01'de kesinleşir; bu rapor uygulanmış API belgesi değildir. |

## 3. Önerilen ortak davranış ve otorite

1. Yönetici Person/ilişki kaydını kaydeder; hesap daveti varsa başarılı hesap oluşturmasından sonra gönderir. Kişi kaydı hesap aktif olmasını beklemez.
2. Hesap sahibi e-postayı Keycloak'ın güvenilir bağlantı/required-action akışında tamamlar. Yeni hesapta girişten önce zorunluluk D1'le karara bağlanır. Giriş engelleniyorsa doğrulama yalnız JWT isteyen uygulama ekranına hapsedilmez; IdP giriş sayfası tamamlatır.
3. Cep kodu yönetici kaydederken otomatik ve gereksiz yere üretilmez; kullanıcı kendi Hesabım ekranında hazır olduğunda başlatır. Kendi kendine kayıtta aynı sihirbazın son adımı olabilir.
4. Hesap+kanal+normalize değere bağlı kanıt sunucuda; e-posta için otorite Keycloak. Kayıt varlığı / gönderim / challenge / sahiplik / güncellik ayrı. UI veya gönderici “başarılı” cevabı VERIFIED üretmez.
5. Mobilde onaylanan değer merkez/yönetimde yenilendiğinde görünür; webde tamamlanan onay mobil ön plana geldiğinde okunur. Yeni hesap/başka hesap aynı telefon numarasına sahip olsa da diğer hesabın kanıtı kopyalanmaz.
6. Aynı normalize değeri tekrar kaydetmek, isim/fotoğraf/rıza değiştirmek, rol veya bina seçmek yeniden doğrulama istemez. Yeni aday doğrulanana kadar mevcut doğrulanmış iletişim korunur.
7. E-posta değişiminde yeniden kimlik doğrulama + yeni adres onayı + eski adrese bildirim; alternatif yönetici Update yolu da güven sözleşmesini korur. Native UPDATE_EMAIL tercih edilir.
8. Telefon challenge'ı amaç ve aday sürümüne bağlı; HMAC, süre, tek kullanım, resend/attempt/dağıtık rate limit, idempotency ve yarış kontrolleri ortak sunucuda. OTP içeriği yönetici SMS listesinden görülemez.
9. Kendi hesap doğrulaması için bina üyeliği gerekmez. Yönetici daveti yalnız mevcut yetki kapsamında başlatabilir; manuel “doğrulandı” yetkisi eklenmez.
10. Telefon doğrulama internete bağlıdır; offline kuyruk/sahte başarı yok. Doğrulama her girişte ikinci faktör istemek değildir.

Native e-posta akışının dayanağı: [Keycloak yönetim rehberi](https://www.keycloak.org/docs/26.7.0/server_admin/index.html).
Değişiklikte yeniden kimlik doğrulama/yeni adres onayı/eski adrese bildirim ve tek kullanımlık süreli token ilkeleri: [OWASP e-posta doğrulama rehberi](https://cheatsheetseries.owasp.org/cheatsheets/Email_Validation_and_Verification_Cheat_Sheet.html).
Bunlar tasarım dayanağıdır; kurulu sürüm/sağlayıcı kabul testi yerine geçmez.

## 4. Karar kapıları — öneri onay demek değildir

| Kod | Karar / önerilen başlangıç | Gereken faz | Şu anki durum |
| --- | --- | --- | --- |
| D1 | Yeni hesapta e-posta onayını zorunlu, telefonu mevcut gibi isteğe bağlı tutma; hangi işlemin aktivasyon bekleyeceği | 02,07 | Kullanıcının açık politika kararı beklenir. Bu tur kapsamı yalnız plan/analizdir. |
| D2 | Mevcut doğrulanmış e-postaları koruma; eski telefonlara sahte doğrulama vermeme; eksik e-postalı/yetkili hesaplar için kademeli geçiş ve kurtarma | 02,07 | Canlı kapsam/erişim değişikliği onaylanmadı. |
| D3 | Gerçek SMS sağlayıcısı, ücret/koşul, gönderici ve platform mu davet kapsamı mı maliyet hesabı; izinli test alıcısı | 03,07 | Kaynakta yalnız MOCK; yeni ücretli/harici hizmet seçilmedi. Bu kapı çözülmeden üretim SMS kabulü kapanmaz. |
| D4 | OTP süre/deneme/resend/ülke-normalizasyonu, saklama/temizleme ve gerçek sağlayıcının tekrar gönderim/timeout politikası | 01 tasarım,03 uygulama | Örnek 5 dakika / 5 deneme / 60 saniye yalnız öneridir, uygulanmış ayar değildir. |

Bu kapılar analiz fazının eksik yapılmış maddeleri değil, ilgili uygulama fazlarının açık önkoşullarıdır. FAZ 00 bütün kararları verilmiş saymaz; karar listesini ve etkisini teslim eder.

## 5. Üretici–tüketici / veri / uyumluluk planı

- **Backend:** Ortak doğrulama application servisi ve kendi hesap okuma/komut uçları; isim/path/DTO/null/hata kodları FAZ 01'de kesinleştirilir. E-posta kaynağı Keycloak, telefon kanıtı sunucunun kendi kaydı; Property/Person tablosuna “hesap doğrulandı” işareti yayılmaz.
- **Kalıcılık:** Kanıt/aday/challenge/makbuz ve geçici gönderim verisinin yaşam döngüsü ayrılır. Yeni entity gerekirse sekiz zorunlu alan, EF mapping+migration+snapshot, audit/version/idempotency. Backfill UNVERIFIED/kanıt yok; eski numaralara uydurma verifiedAt/actor yok.
- **Eski profil komutları:** Web /account/profile/commands ve mobil /mobile/profile/commands telefon yazıyor. Yeni akış yalnız yeni ekranda yapılırsa bu yollar kanıtı bypass eder. Phone alanının eski istemci semantiği, bekleyen aday ve mevcut doğrulanmış değer uyumu FAZ 01'de testli adaptör kararı ister.
- **Web:** Mevcut /account her iki kabukta çalışır; ContactInputs/semantic tema/ikon/TR–EN/ortak feedback kullanılır. E-posta için güvenli IdP dönüşü, telefon için kod formu; gerçek retryAfter ve sunucu güncelliği.
- **Mobil:** Mevcut profile/auth/Expo Router ve güvenli PKCE; foreground refetch, online komutlar, hesap değişiminde state temizliği. SQLite sahiplik otoritesi değildir.
- **SMS:** Mevcut provider arayüzü/payload koruması değerlendirilebilir; Property kredi kapsamı global doğrulamaya sessizce taşınamaz. .NET HttpClient ile paket eklemeden transport mümkün; hizmet sözleşmesi/ücret/veri aktarımı onayı ayrı.
- **E-posta teslimi:** Davet başarısızlığı güvenli ve görünür durum+retry olmalı; DB ve Keycloak/SMTP arasında dağıtık transaction varmış gibi davranılmaz. Belirsiz commit/delivery'de kör tekrar/hesap silme yapılmaz.
- **Rollout/rollback:** Önce model ve uyumluluk, sonra testli akış, sonra açık onaylı kademeli enforcement. AutoMigrate olan sunucuyu başlatmak bile canlı şema yazımı yapabilir; FAZ 00 servis/build/migration başlatmaz. Down veri kaybı ve aktif challenge/anahtar rotasyonu ayrıca kanıtlanır.
- **Lisans:** Yeni bağımlılık/asset/SDK/container yok. Yeni paket yerine mevcut BCL/Keycloak/kurulu UI; gerçek SMS hizmeti kaçınılmaz teslim gereksinimi olup onaysız seçilmez. Envanter üreten license/restore komutları bu belge turunda çalıştırılmadı.

## 6. Fazlar, görevler ve kapanış sözleşmesi

Kanonik [PHASES.md](../../../buildingflow-contact-verification-prompts/PHASES.md); her fazın ayrıntılı kabulü oradadır.

| Faz | Görev | Bu tur kapsamı |
| --- | --- | --- |
| BF-VERIFY 00 | [#43](https://github.com/AkaBayraK/BuildingFlow/issues/43) | Tamamlandı; Closed/Done, açık madde 0 |
| BF-VERIFY 01 | [#44](https://github.com/AkaBayraK/BuildingFlow/issues/44) | Backlog; uygulanmadı |
| BF-VERIFY 02 | [#45](https://github.com/AkaBayraK/BuildingFlow/issues/45) | Backlog; uygulanmadı |
| BF-VERIFY 03 | [#46](https://github.com/AkaBayraK/BuildingFlow/issues/46) | Backlog; uygulanmadı |
| BF-VERIFY 04 | [#47](https://github.com/AkaBayraK/BuildingFlow/issues/47) | Backlog; uygulanmadı |
| BF-VERIFY 05 | [#48](https://github.com/AkaBayraK/BuildingFlow/issues/48) | Backlog; uygulanmadı |
| BF-VERIFY 06 | [#49](https://github.com/AkaBayraK/BuildingFlow/issues/49) | Backlog; uygulanmadı |
| BF-VERIFY 07 | [#50](https://github.com/AkaBayraK/BuildingFlow/issues/50) | Backlog; uygulanmadı |

- 65 faza özgü kabul maddesi + her görevde 4 kapanış maddesi. [32 senaryolu kabul matrisi](ACCEPTANCE_MATRIX.md).
- Görevin **Yapılanlar** bölümü [teslim şablonuyla](../../../buildingflow-contact-verification-prompts/TASK_REPORT_TEMPLATE.md) her kabul için davranış/gerekçe/dosya/API/yetki/migration/test maddeleri içerir.
- Gereken gerçek test/teslim/cihaz kanıtı yoksa açık madde kalır; skip veya mock ile kapanış yapılmaz. Yalnız gerekli olmayan kontrole gerekçeli UYGULANMAZ verilebilir.
- Yeni kullanıcı kabul checkbox'ı eklenmez: bütün iş ve kanıt tamamlanınca kullanıcı talimatıyla issue completed kapatılır ve pano Done yapılır. İki durum yeniden okunur.
- Yerel rapor/promptlar **commit/push edilmedi**. GitHub görev gövdeleri gerçek kaynak yollarını metin olarak verir; uzak depoda bulunmayan dosyaya canlı blob bağlantısı varmış gibi davranılmaz.
- FAZ 00 kapanması gerçek e-posta/SMS özelliğinin bitmesi değildir; 7 uygulama/entegrasyon/rollout fazı ayrı açık kalır.

## 7. Bu tur yapılan işlemler ve doğrulama

1. Mevcut kirli Git ağacı, devam eden BF-MULTI/mobil/hesap işlerinin kaynakları ve progress/API/kurallar okundu; eski işler yeniden başlatılmadı.
2. Temel üretici/tüketici ve iki profil endpoint'inin aynı store'u kullandığı; email claim güncelliği; telefon kanıtının olmayışı; yalnız MOCK sağlayıcı kaynakla belirlendi.
3. Onaylı ihtiyaç ve D1–D4 politika/servis kararları ayrıldı. Önce kayıt/sonra sahiplik, değişmeyende tekrar yok ve mobil/merkez ortaklığı açıklandı.
4. MASTER_PROMPT/PHASES/START_HERE/TASK_REPORT_TEMPLATE ve sekiz faz promptu oluşturuldu; her faz 3 resmi adım.
5. Mevcut GitHub repo/pano bulundu; BF-VERIFY marker'ı ile mükerrer arandı (yok); sekiz issue açıldı. Pano alanları okundu; sekiz taskın her biri aynı panoda tam bir kez bulundu. #43 In progress, #44–#50 açık/Backlog son okumayla doğrulandı.
6. Belgelerin yerel hedef/kimlik/kabul sayacı ve plan-görev eşliği, kaynak/manifest/lock koruma karşılaştırması **GEÇTİ**: 14 Markdown belge, 49 yerel bağlantı, 8 faz promptu, 65 kabul ve 32 senaryo; başlangıçtaki 19897 kaynak dosyası hash aynı.
7. TESTING ve mevcut STATUS/SESSION/CHANGELOG bu belge teslimiyle güncellendi; ürün yol haritasında tik değiştirilmedi.

| Kontrol | Bu tur gerçek sonuç |
| --- | --- |
| Kaynak keşfi ve üretici/tüketici haritası | GEÇTİ (salt kaynak incelemesi) |
| MASTER/PHASES gerçek yolları | UI: frontend/buildingflow-ui-plan/; mobil: mobile/buildingflow-mobile-prompts/ |
| Manifest komutları | Web: npm test/typecheck/build. Mobil: npm run check/build; faz uygulamalarında kullanılacak. |
| Belge bağlantısı, faz/task/pano sayısı | GEÇTİ: 14 belge / 49 yerel hedef / 8 faz / 8 issue / 8 tekil pano öğesi |
| Kaynak koruma | GEÇTİ: başlangıçtaki 19897 kaynak/yapılandırma dosyası hash aynı; eksik/değişen 0. Manifest/lock/milestones buna dahil. |
| .NET/web/mobil test, build, migration, restore/kurulum | ÇALIŞTIRILMADI; bu belge/analiz fazında gerekli değil |
| Gerçek Keycloak/SMTP/SMS/cihaz kabulü | ÇALIŞTIRILMADI; kaynak bulgusunu runtime başarısı saymıyoruz |
| Canlı DB/.env/Keycloak/servis/kullanıcı/parola değişikliği | YAPILMADI |
| Paket/font/ikon/asset/lisans envanteri değişikliği | YAPILMADI |
| Commit / push / deploy | YAPILMADI |

Kalan ürün işleri: FAZ 01–07. Analiz teslimini engelleyen karar yok; sonraki uygulama fazlarının karar/sağlayıcı kapıları açıkça planlıdır.

## 8. Son teslim kaydı

Belge kontrolü Python stdlib ile terminalde inline çalıştırıldı: Markdown hedefleri, faz kimlikleri/üç resmi adım, kabul/senaryo sayıları ve SHA256 kaynak karşılaştırması GEÇTİ. GitHub GraphQL son okumada her görevin kabul sayısı, teslim bölümü ve tekil Project 3 öğesi doğrulandı. Son GitHub GraphQL okuması: #43 CLOSED/COMPLETED,10/10 madde tamam ve Project3 Done;diğer yedi görev açık. Sonrasında kullanıcı FAZ01'i ayrıca başlattı; #44 In progress, #45–#50 Backlog. Faz00 3/3=%100,00;bu refactor planı1/8=%12,50. Uygulama kaynağı karşılaştırması19897 dosya,değişen0/yeni0 ile FAZ00 anına aittir;sonraki geliştirme bununla karıştırılmaz. Uygulama testleri bu tur çalıştırılmadı. Yerel dosyalar commit/push edilmedi.
Token alanları sağlanmadığından giriş/çıkış/toplam/kalan context ayrı ayrı BİLGİ MEVCUT DEĞİL; hesap kotası bu sohbet tüketimi değildir.

