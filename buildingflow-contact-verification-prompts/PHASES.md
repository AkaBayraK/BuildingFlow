# BF-VERIFY — Kanonik faz ve kabul planı

Toplam **8 faz (00–07)**. Faz kimliği ile listedeki sıra farklıdır. FAZ00/01 tamamlandı; güncel yetkili kapsam **FAZ02**'dir. FAZ03–07 planlıdır.
Görevlerin canlı checkbox/durum kaynağı GitHub issue'dur; aşağıdaki maddeler kabul tanımıdır, ikinci bir durum listesi değildir.
Pano: [@BuildingFlow / Project 3](https://github.com/users/AkaBayraK/projects/3).
BF-MULTI, İŞ, UI ve MOBİL fazları yeniden numaralandırılmaz; ürün yol haritasında bu belge turu için tik değiştirilmez.

| Kimlik | Sıra | Faz | Bağımlılık | GitHub görevi |
| --- | --- | --- | --- | --- |
| BF-VERIFY 00 | 1/08 | [Kaynak analizi, prompt paketi ve görev sözleşmesi](phases/PHASE_00.md) | Yok | [#43](https://github.com/AkaBayraK/BuildingFlow/issues/43) |
| BF-VERIFY 01 | 2/08 | [Ortak doğrulama modeli, API sözleşmesi ve kalıcılık](phases/PHASE_01.md) | 00 | [#44](https://github.com/AkaBayraK/BuildingFlow/issues/44) |
| BF-VERIFY 02 | 3/08 | [Keycloak e-posta doğrulaması ve hesap açılışı](phases/PHASE_02.md) | 01 | [#45](https://github.com/AkaBayraK/BuildingFlow/issues/45) |
| BF-VERIFY 03 | 4/08 | [Telefon OTP, güvenli gönderim ve sağlayıcı entegrasyonu](phases/PHASE_03.md) | 02 | [#46](https://github.com/AkaBayraK/BuildingFlow/issues/46) |
| BF-VERIFY 04 | 5/08 | [Merkez ve yönetim web ekranları](phases/PHASE_04.md) | 03 | [#47](https://github.com/AkaBayraK/BuildingFlow/issues/47) |
| BF-VERIFY 05 | 6/08 | [Mobil doğrulama ve ortak durum yenileme](phases/PHASE_05.md) | 04 | [#48](https://github.com/AkaBayraK/BuildingFlow/issues/48) |
| BF-VERIFY 06 | 7/08 | [Çapraz istemci güvenlik ve regresyon kabulü](phases/PHASE_06.md) | 05 | [#49](https://github.com/AkaBayraK/BuildingFlow/issues/49) |
| BF-VERIFY 07 | 8/08 | [Kontrollü geçiş, operasyon ve nihai teslim](phases/PHASE_07.md) | 06 | [#50](https://github.com/AkaBayraK/BuildingFlow/issues/50) |

Her faz: 3 resmi adım (incele → uygula → kanıtla ve teslim et). Faz 00 uygulaması yalnız prompt/belgedir.
Faz00/01 kapanmıştır; FAZ02 kendi rapor/görev kanıtıyla izlenir. FAZ03–07 uygulanmış değildir.

## FAZ 00 — Kaynak analizi, prompt paketi ve görev sözleşmesi

Bu tur yalnız belge ve görev yönetimi; uygulama kodu, şema, servis, gerçek kullanıcı ve canlı ayarlar değişmez.

- **P00.A1:** Mevcut yönetici/mobil hesap oluşturma, profil, e-posta, telefon ve kişi↔hesap bağı üretici/tüketici haritası dosya kanıtlarıyla çıkarıldı.
- **P00.A2:** Onaylı gereksinimler ile önerilen politikalar ve kullanıcı kararı bekleyen noktalar ayrıldı; gerçek ortam doğrulanmadıysa açıkça belirtildi.
- **P00.A3:** MASTER_PROMPT, PHASES, START_HERE, faz promptları ve ayrıntılı görev teslim şablonu oluşturuldu.
- **P00.A4:** Sekiz faz için tekil GitHub görevleri açıldı, birbirine bağımlılıkları ve @BuildingFlow pano bağlantıları kaydedildi.
- **P00.A5:** Fonksiyonel, güvenlik, eski istemci, migration ve mobil↔web kabul senaryoları fazlara bağlandı.
- **P00.A6:** Yerel bağlantı/kimlik/sayaç/plan-görev eşliği ve uygulama kaynağı/bağımlılık koruması kontrol edilip 00-analysis.md raporuna işlendi.

**Zorunlu doğrulama:** Belge bağlantıları, 8 faz / 8 issue / 8 pano öğesi eşliği, kaynak ve manifest/lock karşılaştırması. Uygulama testleri bu belge fazında çalıştırılmaz.

**Rapor:** `docs/refactor/contact-verification/00-analysis.md`.

## FAZ 01 — Ortak doğrulama modeli, API sözleşmesi ve kalıcılık

Hesap düzeyinde kanal/değer bazlı doğrulama modeli ve ortak backend sözleşmesi; e-posta otoritesi Keycloak kalır. Canlı zorunluluk ve gerçek gönderim açılmaz.

- **P01.A1:** Mevcut Phone/Profile/API tüketicileri incelenip yeni model için ADR, DTO/null/durum/geçiş ve geriye uyumluluk sözleşmesi kesinleştirildi.
- **P01.A2:** Account(sub)+kanal+normalize değer bağlamı, doğrulama kanıtı ve bekleyen değişiklik ayrıldı; kayıtlı kişi/Property ilişkisine otomatik yetki veya onay verilmedi.
- **P01.A3:** Mevcut doğrulanmış değer yeni aday doğrulanana kadar korundu; aynı normalize değerde profil kaydı doğrulama kanıtını sıfırlamıyor.
- **P01.A4:** Eski web/mobil profil yazım yolları yeni doğrulamayı atlayamıyor; eski istemci davranışı açık adaptör/uyumluluk testleriyle korundu.
- **P01.A5:** Kendi hesabı için ortak okuma/komut sözleşmesi, sunucudan version/409 ve UUID/fingerprint replay, ErrorCatalog/TR/EN parametreleri birlikte uygulandı.
- **P01.A6:** Yeni entity gerekiyorsa BaseEntity'nin sekiz alanı, mapping, EF migration+Designer+snapshot ve veri koruyan Down birlikte tamamlandı; mevcut telefonlar doğrulanmış diye backfill edilmedi.
- **P01.A7:** İzole PostgreSQL ilk kurulum/yükseltme/tekrar uygulama ve tüm uygulama tabloları model/gerçek şema testleri tamamlandı.
- **P01.A8:** Kanal durumundan ayrı gönderim/challenge durumu ve güncellik bilgisi tanımlandı; Keycloak ulaşılamaması yanlış VERIFIED sonucu üretmiyor.

**Zorunlu doğrulama:** İlgili domain + izole PostgreSQL entegrasyonları, BaseEntity şema/migration sözleşmeleri; v1 istemci uyumluluk ve negatif actor/version/replay testleri.

**Rapor:** `docs/refactor/contact-verification/01-report.md`.

## FAZ 02 — Keycloak e-posta doğrulaması ve hesap açılışı

Yönetici oluşturması ve mobil kayıt aynı açık e-posta sahipliği politikasına uyar. İlk doğrulama Keycloak giriş/required-action akışında; SMTP ve canlı politika ayrı devreye alma kapısındadır.

- **P02.A1:** E-posta zorunluluğu/telefon isteğe bağlılığı (D1), mevcut hesap geçişi (D2) ve güvenli kurtarma politikası açık kullanıcı kararıyla kesinleştirildi; onaysız realm-geneli kilitleme yapılmadı.
- **P02.A2:** Kurulu Keycloak sürümüyle VERIFY_EMAIL/UPDATE_EMAIL ve execute-actions-email davranışı izole gerçek IdP testinde doğrulandı; UPDATE_PASSWORD adına bakılarak doğrulama varsayılmadı.
- **P02.A3:** Yönetici normal davet, geçici parola ve mobil kayıt yolları kapsandı; hesap başarıyla oluşturulmadan davet yollanmadı ve davet hatası doğrulama başarısı gibi gösterilmedi.
- **P02.A4:** Parola belirleme/e-posta onayı tek başlangıç akışında gereksiz ikinci davet üretmeden tamamlandı; doğrulanmayan girişte Keycloak required-action ve güvenli dönüş çalışıyor.
- **P02.A5:** E-posta değişikliği yeni adres doğrulanana kadar eski doğrulanmış adresi koruyor; yeniden kimlik doğrulama ve eski adrese bildirim güvenli akışla tamamlandı.
- **P02.A6:** Yönetici başka hesaba yalnız mevcut yetkisi içinde davet başlatabiliyor; e-posta/telefon doğrulanmış işaretini elle veremiyor; Users.Update doğrudan yazımı yeni politikayı bypass etmiyor.
- **P02.A7:** Eski JWT, reset/link replay, süresi dolmuş link, IdP/SMTP kesintisi ve tekrar gönderim negatifleri doğrulandı; doğrulama adresi/token log veya localStorage'a girmiyor.
- **P02.A8:** Mailpit/SMTP kabulü ile gerçek posta kutusu teslimi ayrıldı; fazın zorunlu izole gerçek e-posta akışı kanıtlandı, üretim teslimi FAZ 07'ye açıkça bırakıldı.

**Zorunlu doğrulama:** İzole gerçek Keycloak+yerel test posta kutusu, backend negatif JWT/yetki, yönetici+geçici parola+mobil kayıt kombinasyonları; güvenli web/mobil redirect regresyonu.

**Rapor:** `docs/refactor/contact-verification/02-report.md`.

## FAZ 03 — Telefon OTP, güvenli gönderim ve sağlayıcı entegrasyonu

Kendi hesabının telefonunu doğrulayan ortak backend. Gerçek sağlayıcı yoksa MOCK yalnız testte kullanılır; üretim SMS kabulü eksikken görev kapanmaz.

- **P03.A1:** Gerçek SMS sağlayıcısı, servis kullanım koşulları/ücret, gönderici hesabı ve maliyet kapsamı (D3) kullanıcı tarafından seçilip kaydedildi; yeni SDK/paket eklenmeden BCL/HttpClient alternatifi değerlendirildi.
- **P03.A2:** Platform hesap doğrulaması ile Property bazlı SMS kredisi/yetkisi ayrıldı; rastgele bina seçerek başka alanın hesabını veya kredisini kullanma engellendi.
- **P03.A3:** Kod gönder/yeniden gönder/doğrula/iptal komutları aynı account+kanal+aday değer+amaç+challenge sürümüne bağlı; başka kullanıcının kodu işe yaramıyor.
- **P03.A4:** Kriptografik RNG ve ayrı amaçlı sunucu anahtarlı HMAC, kısa süre, tek kullanım, deneme sınırı ve hesap/hedef/IP bazlı gönderim limiti D4 kararıyla uygulandı; ham kod/veri loglanmıyor.
- **P03.A5:** Yeniden gönderimde önceki challenge atomik geçersiz; aynı operationId tekrarında yeni SMS/kredi düşümü yok; çift tıklama, eşzamanlı doğrulama ve stale version davranışı kanıtlandı.
- **P03.A6:** Sağlayıcı timeout/belirsiz kabul ve güvenli retry/dedupe politikası uygulandı; eski/süresi dolmuş OTP kuyruktan geç gönderilmiyor.
- **P03.A7:** OTP içeriği sıradan yönetici SMS listesi, şablonu, raporu veya destek ekranından okunamıyor; şifreli gönderim payload'ı ile doğrulama HMAC'i ayrı yaşam döngüsünde.
- **P03.A8:** Boş/aynı normalize telefon ve yeni telefon değişimi doğru yönetildi; isim/fotoğraf/alan değişimi doğrulamayı sıfırlamıyor.
- **P03.A9:** MOCK testleri gerçek teslim diye sayılmadı; yalnız onaylı test alıcısı üzerinde gerçek SMS gönderimi ve kod doğrulaması kontrollü olarak kanıtlandı.
- **P03.A10:** Üretim ayarı kapalı/gönderici yoksa açık UNAVAILABLE davranışı var; gönderilmediği halde kod gönderildi/doğrulandı başarısı üretilmiyor.

**Zorunlu doğrulama:** Domain, gerçek izole PostgreSQL yarış/TTL/replay/limit testleri, sağlayıcı sözleşme testleri ve kullanıcı onaylı kontrollü gerçek SMS kabulü. Gerçek kabul atlanırsa task açık kalır.

**Rapor:** `docs/refactor/contact-verification/03-report.md`.

## FAZ 04 — Merkez ve yönetim web ekranları

Mevcut /account ve merkez/yönetim kabukları üzerinden ortak doğrulama UX'i; sunucu yetkisinin yerini UI durumu almaz.

- **P04.A1:** Hesabım/Kişisel bilgilerde iki kanalın durumları, maskeli hedef, doğrula/yeniden gönder ve telefon kod girişi mevcut ortak UI bileşenleriyle eklendi.
- **P04.A2:** Merkez, malik/kiracı portalı ve yönetim ekranından ortak hesaba ulaşım var; Property seçmek veya yönetici rolü taşımak kendi hesabını doğrulamak için zorunlu değil.
- **P04.A3:** İlk girişte gerekli e-posta Keycloak akışına, giriş sonrası telefon ise hesap ekranına yönleniyor; döngü/anonim veri/unsafe redirect yok.
- **P04.A4:** Sunucu retryAfter/süre/attempt/version bilgisi kullanılıyor; çift gönderim, süre dolması, yanlış kod, 409, 429 ve servis kesintisi anlaşılır ve erişilebilir.
- **P04.A5:** Sekme odağına dönüş/yenilemede ortak sunucu durumu yeniden okunuyor; mobilde biten doğrulama ve eski JWT iddiasının güncelliği doğru işleniyor.
- **P04.A6:** İsim/fotoğraf veya aynı normalize iletişim değeri kaydı yeniden doğrulama istemiyor; değişen kanalın bekleyen değeri ile eski doğrulanmış değeri ayrı gösteriliyor.
- **P04.A7:** Yöneticinin izinli davet işlemi yetki kapsamında; başka kullanıcı için keyfi VERIFIED işareti veya OTP okuma yok.
- **P04.A8:** TR/EN, ikonlu durumlar, açık/koyu/sistem, klavye/odak ve 375/1440 px gerçek API etkileşimleri kanıtlandı.

**Zorunlu doğrulama:** Mevcut npm test/typecheck/build; gerçek bileşen ve gerçek test API'siyle pozitif/negatif etkileşim, erişilebilirlik/odak, dar ekran ve çapraz cihaz durum yenileme.

**Rapor:** `docs/refactor/contact-verification/04-report.md`.

## FAZ 05 — Mobil doğrulama ve ortak durum yenileme

Mevcut Expo uygulaması Hesabım ve ilk giriş akışı aynı backend'i kullanır; yerel SQLite doğrulama otoritesi değildir.

- **P05.A1:** Kurulu Expo/RN sürümü ve mevcut profil/auth/i18n bileşenleri incelendi; API kullanılacaksa sürüme uygun resmî doküman doğrulandı, yeni paket eklenmedi.
- **P05.A2:** Hesabım iletişim durumları, doğrula/yeniden gönder/kod girişi ve ilk giriş yönlendirmesi mevcut native görsel kalıpla uygulandı.
- **P05.A3:** E-posta onayı mevcut OIDC Authorization Code+PKCE ve güvenli uygulama dönüşüyle çalışıyor; mobil veya merkezden başlayan akışta aynı hesap kanıtı okunuyor.
- **P05.A4:** Uygulama ön plana dönünce/ekran açılınca sunucu durumu yenileniyor; webde tamamlanan doğrulama tekrar istenmiyor.
- **P05.A5:** Yanlış/eksik kod, süre dolumu, retryAfter, deneme kilidi, 409/429, servis kesintisi ve erişilebilir klavye/odak/geri davranışları tamamlandı.
- **P05.A6:** Offline durumda doğrulama tamamlanmış gösterilmiyor ve OTP işlemi offline kuyruğa alınmıyor; hassas kod/link SQLite/log/normal storage'da tutulmuyor.
- **P05.A7:** Logout/hesap değişiminde pending state ve hassas ekran verileri temizleniyor; diğer hesap veya eski aday değere doğrulama taşınmıyor.
- **P05.A8:** Fotoğraf/rıza/telefon profil uyumluluğu, çoklu alan/daire/rol değişimi ve boş telefon davranışı korunuyor.
- **P05.A9:** npm run check ve npm run build ile fiziksel/emülatör online/offline+web↔mobil kabulü gerçekten tamamlandı; cihaz kanıtı yoksa ilgili madde açık.

**Zorunlu doğrulama:** Mobil npm run check/build; mevcut auth/profile testleri; cihaz veya emülatörde e-posta dönüşü, SMS girişi, uygulama odağı, ağ kaybı ve hesap değişimi. Simülasyon gerçek cihaz teslimi diye sayılmaz.

**Rapor:** `docs/refactor/contact-verification/05-report.md`.

## FAZ 06 — Çapraz istemci güvenlik ve regresyon kabulü

Önceki fazların tek tek yeşil olması uçtan uca başarı değildir; ortak hesap akışı ve yetki sınırları birlikte kanıtlanır.

- **P06.A1:** ACCEPTANCE_MATRIX içindeki tüm zorunlu CV senaryoları gerçek test/kanıt kimliği, sonuç ve dosyayla eşlendi; atlanan zorunlu madde kalmadı.
- **P06.A2:** Mobilde doğrula→webde yenile ve webde doğrula→mobil ön plana dön; aynı hesap/bilgi yeniden doğrulama istemiyor.
- **P06.A3:** İlk kayıt normal/geçici parola/mobil, eski hesap, eksik iletişim ve yalnız bir kanalı değiştirme kombinasyonları doğrulandı.
- **P06.A4:** Başka hesap/challenge, başka tenant/Property/daire, yönetici keyfi onayı ve kişi ilişkisini otomatik yetkilendirme negatifleri kanıtlandı.
- **P06.A5:** Eski JWT, link/OTP replay, eşzamanlı iki cihaz/değişen aday, race/version/UUID ve çok API örneğinde dağıtık limitler doğrulandı.
- **P06.A6:** Gerçek izole PostgreSQL migration/Backfill/BaseEntity sözleşmeleri ve önceki profil/fotoğraf/rıza/hesap bağlama/rol finans erişimi regresyonu geçti.
- **P06.A7:** Log/telemetri/hata/SSR/cache/normal storage içinde ham OTP/link/token/telefon/e-posta sızıntısı olmaması kontrollü sentetik veriyle doğrulandı.
- **P06.A8:** Backend, web test/typecheck/build ve mobil check/build sonuçları güncel tur kanıtlarıyla raporlandı; build başarısı runtime doğrulaması yerine kullanılmadı.

**Zorunlu doğrulama:** İzole .NET unit/integration, gerçek IdP ve test gönderim altyapısı, web npm test/typecheck/build, mobil npm run check/build, çapraz cihaz ve negatif gizlilik/yetki kabulü.

**Rapor:** `docs/refactor/contact-verification/06-report.md`.

## FAZ 07 — Kontrollü geçiş, operasyon ve nihai teslim

Veriyi ve yönetici erişimini koruyan kademeli açılış; canlı işlem yalnız ilgili ortama açıkça verilmiş yetkiyle. Kodun yazılması ile ortamın açılması ayrıdır.

- **P07.A1:** D1–D4 kararları, rollout ortamı/kitlesi ve gerçek SMTP/SMS kullanım yetkisi kayıtlı; üretim için ayrıca ortam yetkisi verilmeden canlı ayar/migration uygulanmadı.
- **P07.A2:** Mevcut doğrulanmış e-postalar korunuyor; eski telefonlara sahte doğrulama backfill'i yok; eksik e-postalı/yetkili mevcut hesaplar ve yönetici erişim kurtarma planı test edildi.
- **P07.A3:** Kademeli feature/policy açma, auth-gate+backend enforcement, başarısızlık geri dönüşü ve salt güvenlik kodlu ölçümler belgelenip izole ortamda kanıtlandı.
- **P07.A4:** Aynı hesap çok Property/daire/rol ile ikinci kez doğrulanmıyor; kişi-hesap ilişkisi ve finans geçmişi yeniden yazılmıyor.
- **P07.A5:** Gerçek hedef ortamda yetkili kontrollü e-posta ve SMS teslimi ile web/mobil doğrulama kabulü tamamlandı; gönderici veya cihaz/ortam kanıtı eksikse task açık kaldı.
- **P07.A6:** Veri silmeyen rollback ve açık challenge/anahtar rotasyonu/kesinti davranışı, mevcut hesapları kilitlemeden geri dönüş ve destek prosedürü doğrulandı.
- **P07.A7:** Önceki fazların bütün issue checklist/kanıt/raporları eksiksiz ve kapalı; son teslimde dosya/API/migration/yetki/bağımlılık/test/operasyon maddeleri ayrıntılı.
- **P07.A8:** Yalnız uygulanmış ve kanıtlanmış geliştirme mevcut İŞ19/ilgili mobil/UI kaydına aynı kimlikle bağlandı; diğer planların tamamlanma işaretleri oynatılmadı.

**Zorunlu doğrulama:** Yetkili izole/kararlaştırılmış ortamda rollout ve rollback provası, gerçek SMTP/SMS + web/mobil uçtan uca kabul; eski hesap/kurtarma smoke. Yetki veya gerçek teslim yoksa doğrulanamadı ve açık task.

**Rapor:** `docs/refactor/contact-verification/07-report.md`.

## Her görevin ortak kapanış maddeleri

- **K1:** Yapılanlar bölümünde her kabul maddesi için değişiklik, gerekçe ve gerçek dosya yolları ayrıntılı maddelerle yazıldı.
- **K2:** Faza uygun zorunlu kontroller gerçekten çalıştırıldı; komut/sonuç/kanıt kaydedildi. Zorunlu bir test atlandıysa veya gerçek teslim doğrulanamadıysa bu madde açık kalır.
- **K3:** Faz raporu, docs/TESTING.md ve mevcut STATUS/SESSION/CHANGELOG kayıtları aynı BF-VERIFY kimliğiyle güncellendi; bağımlılık ve kaynak koruması incelendi.
- **K4:** Görevin güncel içeriği yeniden okunarak açık kapsam maddesi, zorunlu kanıt eksiği veya çözülmemiş engel kalmadığı kontrol edildi; kapanışa hazır.

Kapanıştan sonra **issue Closed + Project 3 Done** son okumayla doğrulanır. Eksik maddeyi başka göreve taşımak otomatik kapanış yetkisi değildir.



## Güncel kullanıcı yetkisi — 22.10.2026

Prompt/görev/analiz teslimi tamamlandı; #43 Closed(completed), Project3 Done son okumayla doğrulandı. Kullanıcının **ilk fazdan başlayabilirsin** mesajı BF-VERIFY FAZ01/#44 ilk uygulama fazını başlattı; FAZ02–07 henüz yetkili değil. Yukarıdaki önce-yalnız-FAZ00 ifadeleri başlangıç kapsamının tarihçesidir; güncel kapsam bu kayıttır.


## Son durum — 22.10.2026

FAZ00/#43 ve ilk uygulama FAZ01/#44 tamamlandı;iki issue Closed(completed),Project3 Done,0 açık maddeyle son okumada doğrulandı.Kalan #45–#50 açık/Backlog;FAZ02 ayrıca başlatılmadı.FAZ01 kanıtı:14/14 PostgreSQL/API+6/6 domain ve build GEÇTİ.Gerçek e-posta/SMS gönderim workflow'u ve istemci doğrulama ekranları sonraki fazlardadır.


## Güncel kapsam — BF-VERIFY 02 / 22.10.2026

Kullanıcı sıradaki fazı başlattı; #45 In progress. D1/D2 açık onayı: yeni hesaplarda e-posta doğrulaması zorunlu, telefon isteğe bağlı; mevcut doğrulanmış adresler korunur, eski hesaplar kontrollü geçirilir, e-postaya erişim kaybında otomatik VERIFIED verilmeden yetkili destek gerekir. Canlı zorunluluk bu fazda açılmaz; EmailEnabled opt-in olarak kapalı kalır. FAZ03–07 başlatılmadı.


## Son doğrulanan durum — BF-VERIFY02

FAZ00/01/02 tamamlandı; #45 Closed(completed), Project3 Done,12/12 madde son uzak okumada doğrulandı. Faz3/3=%100,00;plan3/8=%37,50. FAZ03–07 başlatılmadı. Kanıt: `docs/refactor/contact-verification/02-report.md`. Önceki kapsam paragrafları tarihsel kayıttır.
