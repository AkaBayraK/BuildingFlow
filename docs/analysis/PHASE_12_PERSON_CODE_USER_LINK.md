# İŞ12–15 — Kişi kodu, SMS, kullanıcı yönetimi ve mobil eşleştirme analizi

15.10.2026 kullanıcı talebiyle salt okunur analiz yapıldı; kapsam kullanıcı isteğiyle küçük tutulmak için dört faza (İŞ12–15) ve 23 numaralı task'a bölündü; hepsi milestones.ts'te `planned`. Uygulama kullanıcı onayı olmadan başlamaz. Bu turda build/test çalıştırılmadı, kod yazılmadı. İş yol haritası toplamı 83 → 106 görev olur; yüzde bu nedenle düşer (yeni kapsam, gerileme değil).

## Talep özeti

1. Kişi oluşturulurken rastgele, ardışık olmayan, tahmin edilemez bir **kişi kodu** üretilir.
2. Kayıttan sonra kişiye "`{{code}}` kod ile kişi kaydınız kayıt altına alınmıştır" SMS'i gider.
3. Merkezde **kullanıcı yönetimi** ekranı: Keycloak kullanıcılarını listeleme, CRUD, kullanıcıyı kişiye bağlama/bağı çözme.
4. Mobilde online iken ad, soyad, cep telefonu, e-posta, kullanıcı adı ve parola ile önce Keycloak kaydı açılır, sonra çevrimdışı kullanım için yerel DB'ye profil aktarılır.
5. Mobilde kod girilince kişi bulunur ve ev sahibi/kiracı/sakin eşleşmesi kurulur; eşleşme kontrol kurallarıyla yapılır.
6. SMS tanım ekranları TraceFlow'daki yapıya benzer olur.

## Mevcut durum (kaynakla doğrulandı)

- `Person` (Domain/Person.cs) Property'ye bağlı; `LinkedUserId` (Keycloak sub, max200) zaten var. Kod alanı yok. `PeopleStore.CheckLinked` bağlantı için hedef kullanıcının o Property'de **aktif PropertyMembership**'i olmasını şart koşar; yani eşleşme üyelik oluşturmadan çalışmaz.
- `MobileSessionStore` mobil bağlamı `person.LinkedUserId == userId` + aktif `UnitOccupancy` (OWNER/TENANT/RESIDENT) ile kurar. Eşleşme sonrası ek mobil değişiklik gerekmeden bağlam görünür.
- `UnitOccupancy` tipi OWNER/TENANT/RESIDENT; tarih aralığı ve `IsEnabled` ile aktiflik belirlenir.
- SMS: `SmsRequested` sözleşmesi (E.164 alıcı, max1000 gövde) ve outbox/RabbitMQ altyapısı Faz09'da hazır; **SMS sağlayıcısı ve tüketicisi kullanıcı kararıyla kapalı**. Gerçek SMS gönderilmiyor.
- Keycloak realm (deploy/keycloak/realm-buildingflow.json): `registrationAllowed=false`, yalnız `buildingflow-web` (public, PKCE, `directAccessGrantsEnabled=false`) ve `buildingflow-api` (bearer-only). Backend'de Keycloak Admin API kullanımı ve servis hesabı yok. Parola politikası: 12+ karakter, büyük/küçük harf, rakam.
- Mobil: `expo-auth-session` ile OIDC/PKCE girişi; refresh token `expo-secure-store`'da (`WHEN_UNLOCKED_THIS_DEVICE_ONLY`); `expo-sqlite` şifreli çevrimdışı önbellek, şema v7. Parola hiçbir yerde saklanmıyor. Mobil plan 15 faz; Faz13 review'da, Faz14 başlamadı.
- TraceFlow (Java, `masterdata/sms` + `web-next/src/pages/sms`): sekmeli ekran — **Hesaplar** (sağlayıcı, gönderici başlığı, şifreli secret, `hasSecret`, kontör bakiyesi, düşük kontör eşiği), **Kontör yükleme/hareketleri** (LOAD/CONSUME/REFUND/ADJUST), **Şablonlar** (kodda tanımlı SMS tipi + sabit parametreler, `{{param}}`, segment tahmini, sistem varsayılanı/şirket şablonu), **Mesajlar** (maskeli telefon, kod/şifre maskeli metin, durum, sağlayıcı mesaj kimliği), **Elle gönderim**. Amaç ayrımı OTP/TRANSACTIONAL/OPERATIONAL/SECURITY/MARKETING. Sağlayıcı olarak yalnız MOCK uygulanmış. Kod Java olduğu için doğrudan taşınmaz; model ve ekran akışı .NET/Next.js'e uyarlanır.

## Eksik / hatalı / riskli noktalar ve kararlar

1. **Kod biçimi (karar, 15.10.2026):** kullanıcı elle gireceği için **6 haneli, yalnız rakam**; ilk hane 1–9 (900.000 olası kod). CSPRNG (`RandomNumberGenerator.GetInt32`) ile üretilir, kişiler arasında sıra/zaman/ID ilişkisi yoktur. Zayıf desenler reddedilip yeniden üretilir: artan/azalan dizi (123456, 654321), tek rakam tekrarı (111111), ikili/üçlü tekrar (121212, 123123), aynı rakamın 4+ kez geçmesi. Sistem genelinde benzersiz indeks, çakışmada yeniden üretim. Kod log, hata mesajı, outbox payload logu ve mesaj geçmişinde maskelenir (`****56`).
2. **Kodun saklanması:** 6 rakamlı kodun düz SHA karması saniyeler içinde kırılır; bu yüzden sunucu sırrıyla (ortam değişkeni) **HMAC-SHA256** saklanır, benzersiz indeks bu değer üzerindedir, ekranda yalnız son 2 hane görünür. Yeniden gönderimde yeni kod üretilir, eski geçersiz olur. Varsayılan budur; yönetici kodu ekranda tekrar görmek isterse şifreli saklamaya geçilir.
3. **Tek başına kod yetmez:** SMS ile gittiği için telefonu ele geçiren veya kodu gören herkes kişiyi sahiplenebilir. Karar (kullanıcı, 15.10.2026): eşleşmede mobilde girilen ad, soyad, cep telefonu ve e-posta merkezdeki Person kaydıyla karşılaştırılır; tutmazsa eşleşme yapılmaz ve hata uyarısı verilir. Karşılaştırma normalize edilir (TR locale büyük/küçük harf, boşluk, telefon E.164, e-posta küçük harf); merkezde boş olan alan karşılaştırmaya girmez, telefon zorunludur. Hata mesajı hangi alanın tutmadığını söylemez (kişi verisi sızıntısını ve deneme yoluyla bilgi toplamayı önler), tek bir "bilgiler kayıtla uyuşmuyor" kodu döner. İsteğe bağlı OTP. Kod tek kullanımlık, eşleşmeden sonra tüketilir; süre sınırı (öneri 30 gün) ve yeniden üretim yönetici eylemidir.
4. **Kaba kuvvet:** 6 hane küçük bir uzaydır; güvenliği kod tek başına değil, kodla birlikte ad/soyad/telefon/e-posta eşleşmesi sağlar. Ayrıca kod girişi kullanıcı başına 5 hatada 15 dk, IP başına saatlik sınırla kilitlenir; tüm denemeler kod değeri olmadan güvenlik olayı olarak loglanır.
5. **Eşleşme kuralları:** kişi aktif ve en az bir aktif `UnitOccupancy`'ye sahip olmalı; kişi zaten başka kullanıcıya bağlıysa ret; aynı kullanıcı farklı Property'lerdeki kişi kayıtlarına bağlanabilir, aynı Property'de tek kişiye. Eşleşme tek transaction'da: `LinkedUserId` atanır, gerekirse aktif `PropertyMembership` (RESIDENT) oluşturulur, kod tüketilir. `CheckLinked` üyelik şartı bu akışta üyeliği önce yaratacak şekilde ele alınmalı. Kişinin ilişkisi bitince bağ kalır; mobil bağlam zaten aktif ilişki filtresiyle kapanır.
6. **Mevcut kişiler:** kodsuz eski kişiler için backfill migration kod üretir; backfill toplu SMS **göndermez** (yönetici seçerek gönderir).
7. **Telefon formatı:** `Person.PhoneNumber` serbest 30 karakter, `SmsRequested` E.164 ister. Kişi kaydında normalize (TR varsayılan +90) ve geçersizse SMS atlanıp ekranda uyarı. Telefonsuz kişiye SMS gitmez; kod yöneticide kalır.
8. **SMS sağlayıcı (karar, 15.10.2026):** kullanıcının sağlayıcısı yok. Gerçek gönderim yapılmaz; **MOCK sağlayıcı** mesajı SMS mesaj tablosuna yazar (durum `SENT`, sağlayıcı `MOCK`, maskeli telefon/kod), kontör düşümü ve şablon render'ı gerçek akıştaki gibi çalışır. Testler bu tablo üzerinden yapılır. Ekranda MOCK hesabı açıkça "test, gerçek gönderim yok" olarak gösterilir. Gerçek sağlayıcı (Netgsm/İleti Merkezi/Verimor + BTK gönderici başlığı) sonraki iştir; sağlayıcı arayüzü buna hazır tasarlanır.
9. **KVKK / İYS:** kayıt SMS'i bilgilendirme (TRANSACTIONAL) amaçlıdır, pazarlama değildir; İYS izni gerektirmez ama aydınlatma metninde SMS ile bilgilendirme yer almalı. Mesaj geçmişinde telefon maskeli, kod maskeli; saklama süresi tanımlanmalı. Pazarlama amacı bu fazda yok.
10. **Keycloak Admin API yetkisi:** yeni confidential client (`buildingflow-admin-api`) + servis hesabı; yalnız `realm-management` `view-users`, `query-users`, `manage-users` (realm-admin değil). Secret ortam değişkeninde. Kullanıcılar realm genelindedir: SITE_ADMIN tüm realm kullanıcılarını görürse **başka sitelerin sakinleri sızar**. Karar: SUPER_ADMIN tüm kullanıcılar; SITE_ADMIN yalnız kendi Property üyeleri ve kendi oluşturduğu kullanıcılar. Rol atama allowlist'i: SITE_ADMIN SUPER_ADMIN veremez.
11. **Kullanıcı silme:** Keycloak'ta hard delete geri alınamaz ve `LinkedUserId`/audit izleri kopar. Öneri: "sil" = devre dışı bırak + bağları çöz; hard delete yalnız SUPER_ADMIN ve onaylı.
12. **Mobil kayıt (karar, 15.10.2026):** ilk kayıtta kullanıcı adı, parola + parola tekrarı, ad, soyad, cep telefonu, e-posta ve **isteğe bağlı** kişi kodu girilir. Backend `POST /api/v1/mobile/registrations` Admin API ile Keycloak kullanıcısını açar (ROPC açılmaz, realm `registrationAllowed=false` kalır), giriş mevcut PKCE akışıyla yapılır. Parola tekrarı istemcide, parola politikası (12+, büyük/küçük harf, rakam) sunucuda doğrulanır. Kod girildiyse kayıttan hemen sonra eşleşme denenir; tutmazsa hesap açık kalır, kullanıcı uyarılır. Kayıt ucu IP/cihaz rate limit'i ve idempotent `requestId` ile korunur; kullanıcı adı/e-posta çakışması tek tip hata döner (hesap var/yok bilgisi sızmaz).
13. **Kodsuz hesap ve kısmi hata (karar):** kodsuz ya da eşleşmemiş hesap yalnız **hesap bilgileri** ekranını görür ve kendi ad/soyad/telefon/e-postasını güncelleyebilir (yalnız Keycloak'ta); site/borç/talep/duyuru ekranları ve senkronizasyon kapalıdır. Bu backend'de zaten zorunludur: `MobileSessionStore` bağsız kullanıcıya bağlam döndürmez, mobil uçlar bağlam olmadan veri vermez; mobil yalnız arayüzde gizler, yetkiyi sunucu verir. Kod sonradan girilince ya da güncellenince eşleşme yapılır, bağlam oluşur ve ekranlar + senkron açılır. Yeni kod girmek mevcut bağı silmez; ek bir kişiye (başka daire/site) bağlanır. Bağ çözme yalnız merkezden. Eşleşmeden sonra kullanıcının mobilde değiştirdiği bilgiler merkezdeki Person kaydını **değiştirmez** (aksi halde eşleşme kontrolü anlamsızlaşır); merkez kaydını yönetici günceller. Bağsız hesaplar yönetim ekranında "bağlı kişi yok" filtresiyle görünür ve temizlenebilir.
14. **Çevrimdışı parola:** parola yerel DB'ye veya SecureStore'a **düz yazılmaz**. Çevrimdışı açılış mevcut refresh token + cihaz kilidi/biyometri ile; yerel parola kontrolü istenirse yalnız Argon2id/PBKDF2 karması SecureStore'da, sunucuda parola değişirse eski parola cihazda çalışmaya devam eder (kabul edilmesi gereken risk). Yerel DB'ye profil (ad, soyad, telefon, e-posta) ve eşleşme durumu şifreli önbellekte yazılır; şema v8 migration. Kodsuz hesapta çevrimdışı yalnız profil görünür.
15. **Faz numaralandırması:** bu iş İŞ12–15'tir; mobil tarafı mobil Faz14/15 sırasını değiştirmez, mobil uygulama adımları İŞ15 task'larına bağlanır. Mobil teslimde `npm run check`/`npm run build` kuralı geçerlidir.

## Faz ve task planı

15.10.2026 kullanıcı analizi onayladı ve fazlara bölünmesini istedi. Task numarası `T<faz>.<sıra>` biçimindedir; kodlama task numarasıyla istenebilir. Grup etiketi ([KOD], [SMS], [KULLANICI], [MOBİL]) ilişkili task'ları bir arada gösterir, Bağımlılık sütunu önce bitmesi gerekeni söyler. Önerilen sıra: İŞ12 → İŞ13 ve İŞ14 (birbirinden bağımsız) → İŞ15. Her faz ayrı onayla başlar.

### İŞ12 [KOD] — Kişi kodu

6 haneli rastgele kişi kodu: üretim, HMAC saklama, yenileme, eski kişilere kod.

| Task | İş | Bağımlılık |
|---|---|---|
| T12.1 | **Kod alanı ve migration** — Person'a HMAC kod karması, son 2 hane, üretim/son kullanma zamanı; benzersiz indeks ve EF migration. | — |
| T12.2 | **Kod üretici** — CSPRNG ile 6 hane, ilk hane 1–9; ardışık/tekrarlı desen reddi, çakışmada yeniden üretim; birim testleri. | — |
| T12.3 | **Kişi kaydında kod ve yenileme** — Kişi oluşturulurken kod üretimi, yenileme ucu, merkez kişi ekranında maskeli gösterim ve yenile düğmesi. | 12.1, 12.2 |
| T12.4 | **Eski kişilere kod** — Kodsuz mevcut kişilere tek seferlik kod üretimi; toplu SMS gönderilmez. | 12.1, 12.2 |
| T12.5 | **Testler ve kabul** — Tahmin edilemezlik, desen reddi, benzersizlik, maskeleme, yetki; TESTING ve API belgesi. | 12.1–12.4 |

### İŞ13 [SMS] — SMS modülü (MOCK)

TraceFlow SMS ekranlarının uyarlaması; sağlayıcı yokken MOCK mesajı tabloya yazar.

| Task | İş | Bağımlılık |
|---|---|---|
| T13.1 | **SMS veri modeli** — Hesap, kontör hareketi, şablon ve mesaj tabloları (BaseEntity, şifreli secret) ve migration. | — |
| T13.2 | **MOCK sağlayıcı ve SMS tüketicisi** — Faz09 SmsRequested tüketicisi; şablon render, kontör düşümü, maskeli kayıt; MOCK tabloya SENT yazar. | 13.1 |
| T13.3 | **SMS API** — Hesap, kontör yükleme/hareket, şablon, mesaj geçmişi ve elle gönderim uçları; sayfalama ve yetki. | 13.1, 13.2 |
| T13.4 | **Merkez SMS ekranları** — Hesaplar, kontör, şablonlar, mesajlar sekmeleri ve elle gönderim; segment tahmini, TR/EN. | 13.3 |
| T13.5 | **Kayıt SMS'i** — Kişi kaydıyla aynı işlemde PERSON_REGISTERED SMS'i outbox'a; telefonsuz/geçersiz/kontör uyarısı, yeniden gönderim. | 12.3, 13.2 |
| T13.6 | **Testler ve kabul** — Maskeleme, kontör tutarlılığı, idempotency, yetki, tarayıcı kabulü; gerçek SMS gönderilmez. | 13.1–13.5 |

### İŞ14 [KULLANICI] — Merkez kullanıcı yönetimi

Keycloak kullanıcılarını kapsamlı listeleme, CRUD ve kişiye bağlama/çözme.

| Task | İş | Bağımlılık |
|---|---|---|
| T14.1 | **Keycloak servis hesabı** — Confidential client, yalnız view/query/manage-users; realm dosyası ve ortam değişkenleri. | — |
| T14.2 | **Keycloak Admin istemcisi** — Infrastructure istemcisi, token önbelleği, hata eşleme; SUPER_ADMIN tümü, SITE_ADMIN kendi üyeleri. | 14.1 |
| T14.3 | **Kullanıcı API** — Sayfalı liste/arama, oluştur, güncelle, devre dışı bırak, parola sıfırlama e-postası, rol allowlist'i. | 14.2 |
| T14.4 | **Kişiye bağla/çöz** — Merkezden kullanıcıyı kişiye bağlama ve çözme; üyeliği aynı işlemde açma/kapatma. | 14.2 |
| T14.5 | **Kullanıcı yönetim ekranı** — Liste, filtre (bağlı kişi yok), form, bağla/çöz diyalogları; TR/EN ve mobil görünüm. | 14.3, 14.4 |
| T14.6 | **Testler ve kabul** — Kapsam sızıntısı, rol yükseltme reddi, test realm'i, tarayıcı kabulü. | 14.1–14.5 |

### İŞ15 [MOBİL] — Mobil kayıt ve kodla eşleşme

Mobilde kayıt, kodsuz hesap modu, kod ve kişisel bilgiyle eşleşme, çevrimdışı profil.

| Task | İş | Bağımlılık |
|---|---|---|
| T15.1 | **Eşleştirme servisi** — Kod + ad/soyad/telefon/e-posta karşılaştırma, tek tip hata, deneme sınırı/kilit; bağ+üyelik+kod tüketimi tek işlemde. | 12.3, 14.4 |
| T15.2 | **Mobil kayıt ucu** — Backend Keycloak kullanıcısı açar; parola politikası, idempotent requestId, rate limit, tek tip çakışma hatası. | 14.2 |
| T15.3 | **Mobil kayıt ekranı** — Kullanıcı adı, parola ×2, ad, soyad, telefon, e-posta, isteğe bağlı kod; sonra PKCE giriş. | 15.2 |
| T15.4 | **Kodsuz hesap ve kod girişi** — Bağsız hesapta yalnız hesap bilgileri ve güncelleme; kod gir/güncelle ekranı, eşleşince ekranlar açılır. | 15.1, 15.3 |
| T15.5 | **Çevrimdışı profil ve senkron** — SQLite v8 şifreli profil ve eşleşme durumu; parola saklanmaz; eşleşince senkron açılır. | 15.4 |
| T15.6 | **Testler ve kabul** — Merkez integration, mobil npm run check/build, uçak modu ve cihaz kabulü. | 15.1–15.5 |

## Kalan açık noktalar

- Gerçek SMS sağlayıcısı ve gönderici başlığı (MOCK ile ilerlenir).
- Yöneticinin kodu tekrar görmesi gerekirse HMAC yerine şifreli saklama.
