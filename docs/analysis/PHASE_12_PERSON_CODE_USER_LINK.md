# İŞ12 — Kişi kodu, kayıt SMS'i ve kullanıcı–kişi eşleştirme analizi

15.10.2026 kullanıcı talebiyle salt okunur analiz yapıldı; faz ve sekiz resmi görev milestones.ts'e `planned` olarak eklendi. Uygulama kullanıcı onayı olmadan başlamaz. Bu turda build/test çalıştırılmadı, kod yazılmadı. İş yol haritası toplamı 83 → 91 görev olur; yüzde bu nedenle düşer (yeni kapsam, gerileme değil).

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

1. **Kod güvenliği:** kod tek başına kişiye erişim veren bir sırdır. CSPRNG (`RandomNumberGenerator`), en az 10 karakter Crockford Base32 (I/L/O/U yok, okunabilir), sistem genelinde benzersiz indeks, çakışmada yeniden üretim. Ardışık/zaman tabanlı/ID türevi üretim yasak. Kod log, hata mesajı, outbox payload logu ve mesaj geçmişinde maskelenir.
2. **Kodun saklanması:** önerilen: düz metin yerine HMAC-SHA256 karması + son 3 karakter görünür. Yeniden gönderimde kod yenilenir (eski geçersiz). Yöneticinin kodu ekranda tekrar görmesi isteniyorsa şifreli saklama gerekir — **karar kullanıcıda**.
3. **Tek başına kod yetmez:** SMS ile gittiği için telefonu ele geçiren veya kodu gören herkes kişiyi sahiplenebilir. Öneri: kod + kayıt telefonunun `Person.PhoneNumber` ile normalize eşleşmesi; isteğe bağlı OTP. Kod tek kullanımlık, eşleşmeden sonra tüketilir; süre sınırı (öneri 30 gün) ve yeniden üretim yönetici eylemidir.
4. **Kaba kuvvet:** kod doğrulama ucu kullanıcı + IP bazlı rate limit, art arda 5 hatada geçici kilit, tüm denemeler güvenlik olayı olarak loglanır (kod değeri olmadan).
5. **Eşleşme kuralları:** kişi aktif ve en az bir aktif `UnitOccupancy`'ye sahip olmalı; kişi zaten başka kullanıcıya bağlıysa ret; aynı kullanıcı farklı Property'lerdeki kişi kayıtlarına bağlanabilir, aynı Property'de tek kişiye. Eşleşme tek transaction'da: `LinkedUserId` atanır, gerekirse aktif `PropertyMembership` (RESIDENT) oluşturulur, kod tüketilir. `CheckLinked` üyelik şartı bu akışta üyeliği önce yaratacak şekilde ele alınmalı. Kişinin ilişkisi bitince bağ kalır; mobil bağlam zaten aktif ilişki filtresiyle kapanır.
6. **Mevcut kişiler:** kodsuz eski kişiler için backfill migration kod üretir; backfill toplu SMS **göndermez** (yönetici seçerek gönderir).
7. **Telefon formatı:** `Person.PhoneNumber` serbest 30 karakter, `SmsRequested` E.164 ister. Kişi kaydında normalize (TR varsayılan +90) ve geçersizse SMS atlanıp ekranda uyarı. Telefonsuz kişiye SMS gitmez; kod yöneticide kalır.
8. **SMS sağlayıcı:** seçilmedi (Faz09 kararı). Netgsm / İleti Merkezi / Verimor seçeneklerinden biri ve BTK onaylı **gönderici başlığı** gerekir — **karar ve hesap kullanıcıda**. Seçilene kadar MOCK sağlayıcı + Mailpit benzeri yerel kayıt; gerçek gönderim yapılmış gibi işaretlenmez.
9. **KVKK / İYS:** kayıt SMS'i bilgilendirme (TRANSACTIONAL) amaçlıdır, pazarlama değildir; İYS izni gerektirmez ama aydınlatma metninde SMS ile bilgilendirme yer almalı. Mesaj geçmişinde telefon maskeli, kod maskeli; saklama süresi tanımlanmalı. Pazarlama amacı bu fazda yok.
10. **Keycloak Admin API yetkisi:** yeni confidential client (`buildingflow-admin-api`) + servis hesabı; yalnız `realm-management` `view-users`, `query-users`, `manage-users` (realm-admin değil). Secret ortam değişkeninde. Kullanıcılar realm genelindedir: SITE_ADMIN tüm realm kullanıcılarını görürse **başka sitelerin sakinleri sızar**. Karar: SUPER_ADMIN tüm kullanıcılar; SITE_ADMIN yalnız kendi Property üyeleri ve kendi oluşturduğu kullanıcılar. Rol atama allowlist'i: SITE_ADMIN SUPER_ADMIN veremez.
11. **Kullanıcı silme:** Keycloak'ta hard delete geri alınamaz ve `LinkedUserId`/audit izleri kopar. Öneri: "sil" = devre dışı bırak + bağları çöz; hard delete yalnız SUPER_ADMIN ve onaylı.
12. **Mobil kayıt yolu:** kullanıcı uygulama içinde kullanıcı adı/parola istiyor. Seçenekler: (a) backend `POST /api/v1/mobile/registrations` Admin API ile kullanıcı açar, ardından mevcut PKCE girişi; (b) `directAccessGrants` (ROPC) yalnız mobil client'ta açılır — OAuth 2.1'de kaldırılmış, MFA/brute-force korumasını zayıflatır. **Öneri (a)**; parola formu uygulamada, giriş yine Keycloak. Açık kayıt spam üretmesin diye kayıt ucu rate limit + (öneri) kişi kodu/telefon doğrulamasıyla birlikte çalışır. Realm `registrationAllowed=false` kalır.
13. **Kısmi hata:** Keycloak kullanıcısı açıldı ama eşleşme başarısız → yetim kullanıcı. Kayıt idempotent `requestId` ile yapılır; kod eşleşmesi kullanıcı açıldıktan sonra ayrı ve tekrar denenebilir adımdır; yetim kullanıcılar yönetim ekranında "bağlı kişi yok" filtresiyle görünür.
14. **Çevrimdışı parola:** parola yerel DB'ye veya SecureStore'a **düz yazılmaz**. Çevrimdışı açılış mevcut refresh token + cihaz kilidi/biyometri ile; yerel parola kontrolü istenirse yalnız Argon2id/PBKDF2 karması SecureStore'da, sunucuda parola değişirse eski parola cihazda çalışmaya devam eder (kabul edilmesi gereken risk). Yerel DB'ye yalnız profil (ad, soyad, telefon, e-posta) şifreli önbellekte yazılır; şema v8 migration.
15. **Faz numaralandırması:** bu iş İŞ12'dir; mobil tarafı mobil Faz14/15 sırasını değiştirmez, mobil uygulama adımları İŞ12 görevlerine bağlanır. Mobil teslimde `npm run check`/`npm run build` kuralı geçerlidir.

## Resmi görevler (milestones.ts — İŞ12)

1. **Analiz ve kararlar** — bu belge; açık kararlar (sağlayıcı, kod saklama, mobil kayıt yolu) onaylanır.
2. **Kişi kodu** — Person'a kod alanı (BaseEntity sekiz alan korunur), CSPRNG üretim, benzersiz indeks, maskeli gösterim/yenileme ve eski kişiler için kod üreten EF migration'ı.
3. **SMS yönetimi** — TraceFlow modelinin uyarlaması: hesap (şifreli secret, gönderici başlığı, kontör/eşik), kontör hareketleri, tip+şablon (`PERSON_REGISTERED` tipi, `{{code}}`, `{{fullName}}`), mesaj geçmişi, MOCK + seçilen sağlayıcı; Faz09 outbox/consumer üzerinden gönderim.
4. **Kayıt SMS'i** — kişi oluşturma ile aynı transaction'da outbox'a `SmsRequested`; telefon yok/geçersiz/kontör yetersiz durumları ekranda; yeniden gönderim yönetici eylemi.
5. **Merkez kullanıcı yönetimi** — Keycloak Admin API istemcisi, kapsamlı liste (sayfalı), oluştur/güncelle/devre dışı/parola sıfırlama e-postası, rol allowlist'i, kişiye bağla/çöz.
6. **Mobil kayıt ve çevrimdışı profil** — kayıt formu, backend kayıt ucu, PKCE giriş, şifreli yerel profil, parola saklamama.
7. **Kod ile eşleştirme** — mobil kod girişi, telefon eşleşmesi, rate limit/kilit, tek transaction'da bağ + üyelik + kod tüketimi, OWNER/TENANT/RESIDENT bağlamının mobilde görünmesi.
8. **Testler ve kabul** — kod tahmin edilemezlik/benzersizlik, yetki/kapsam sızıntısı, rate limit, transaction/idempotency, SMS maskeleme, Keycloak test realm'i, mobil çevrimdışı; TR/EN ve mobil kabul. Gerçek kişiye SMS kabul testine dahil değildir.

## Kullanıcıdan beklenen kararlar

- SMS sağlayıcısı ve gönderici başlığı (öneri: Netgsm veya İleti Merkezi; seçilene kadar MOCK).
- Kod saklama: karma + yenileme (öneri) veya şifreli + tekrar görüntüleme.
- Eşleşmede telefon doğrulaması zorunlu mu (öneri: evet).
- Mobil kayıt: backend kayıt ucu + PKCE (öneri) veya ROPC.
