# BF-VERIFY — Zorunlu kabul senaryoları

Bu **32 senaryo test planıdır; henüz uygulama testleri çalıştırılmış değildir**.
Kanonik fazlar [PHASES.md](../../../buildingflow-contact-verification-prompts/PHASES.md); gerçek checkbox ve kanıt ilgili GitHub görevi/faz raporundadır.
Her zorunlu senaryo GEÇTİ olmalı; DOĞRULANAMADI/BAŞARISIZ/skip ilgili görevi kapatmaz. Tasarım kararıyla UYGULANMAZ ise kullanıcı kapsam kararı ve gerekçe kaydedilir; test eksikliğini gizlemek için kullanılmaz.

| Kimlik | Senaryo | BF-VERIFY fazları | Gereken kanıt |
| --- | --- | --- | --- |
| CV01 | Yönetici kayıt açar; kişi yanından SMS beklenmez, davet ancak başarılı hesap kaydından sonra | 02 | İzole API + IdP |
| CV02 | Normal davet, geçici parola ve mobil kayıt yollarında açık e-posta politikası | 02 | Her yol için gerçek IdP akışı |
| CV03 | Mevcut e-posta ve eksik e-postalı eski/yetkili hesap geçişi, kilitlenmeden kurtarma | 02,07 | Karara uygun rollout provası |
| CV04 | İlk giriş e-posta onayı ve parola işlemi; gereksiz ikinci davet yok | 02 | Test posta kutusu + güvenli dönüş |
| CV05 | Mobilde telefon doğrula → merkez yenilemede aynı VERIFIED; tekrar kod yok | 04,05,06 | İki istemci / aynı hesap |
| CV06 | Merkezde doğrula → mobil ön plana dönüşünde güncel durum | 04,05,06 | Web + cihaz/emülatör |
| CV07 | Başka bina/daire/rol ve yeni üyelikte hesap kanıtı korunur | 04,05,06 | Çok bağlamlı hesap |
| CV08 | İsim/fotoğraf/rıza kaydı veya aynı normalize telefon kanıtı sıfırlamaz | 01,04,05 | V1/v2 uyumluluk + profil regresyonu |
| CV09 | Yalnız telefon değişince yeni değer bekler, eski doğrulanmış değer korunur; e-posta etkilenmez | 01,03,04,05 | İki aday ve version |
| CV10 | E-posta değişince yeniden kimlik doğrulama, yeni onay ve eski adrese bildirim; telefon etkilenmez | 02,04,05 | Gerçek IdP/mail fixture |
| CV11 | Süresi dolan/yanlış/eksik OTP, deneme sınırı ve yeniden gönderim | 03,04,05 | TimeProvider + API + UI |
| CV12 | Yeni kod eski kodu geçersiz kılar; doğrulanan kod ikinci kez kullanılamaz | 03,06 | Replay/TTL entegrasyonu |
| CV13 | Aynı operationId ve aynı içerik tek etki; değişen içerik 409 | 01,03,06 | Gerçek PostgreSQL makbuz/race |
| CV14 | İki cihaz aynı anda doğrular veya telefon değiştirir; eski challenge yeni adayı doğrulamaz | 03,06 | Eşzamanlı istekler |
| CV15 | Başka kullanıcı/actor/challenge/hedef değer ile onay reddi | 01,03,06 | Negatif JWT/IDOR |
| CV16 | Doğrulanan iletişim kişi-hesap bağı, malik/kiracı veya Property yetkisi üretmez | 01,06 | Scope/rol negatifleri |
| CV17 | Yönetici izinli daveti yeniler; yetkisiz hesaba gidemez ve VERIFIED işaretini elle veremez | 02,04,06 | Mevcut Users kapsam matrisi |
| CV18 | Eski JWT, e-posta değişimi ve IdP kesintisinde güncel kanıt doğru değerlendirilir | 02,06 | Güvenilir kaynak + tazelik |
| CV19 | Gönderim/deneme limiti çok API örneğinde ve hesap/IP/hedef kapsamında çalışır | 03,06 | Dağıtık limit testi |
| CV20 | Sağlayıcı kabulü/teslimi/timeout/belirsiz sonuç/yeniden deneme ayrılır; OTP süresi geçmiş kuyruk gönderilmez | 03,06 | Sağlayıcı kontratı + saat |
| CV21 | MOCK ile gerçek SMS başarısı karışmaz; onaylı alıcıda gerçek teslim ve kod onayı | 03,07 | Yetkili kontrollü gerçek SMS |
| CV22 | SMTP/Mailpit kabulü gerçek kullanıcı posta kutusu teslimi diye gösterilmez | 02,07 | Ayrı test ve hedef ortam kanıtı |
| CV23 | Offline OTP kuyruğu yok; sahte başarı yok; ağ dönüşünde durum yenilenir | 05,06 | Cihaz ağ kesme/dönüş |
| CV24 | Logout/hesap değişiminde state temizlenir; diğer hesap kanıtı gösterilmez | 04,05,06 | İki hesap + ön plan |
| CV25 | Ham OTP/link/token ve tam iletişim verisi log/telemetri/hata/SSR/cache/SQLite'a sızmaz | 03,05,06 | Sentetik işaretleyiciyle gizlilik kabulü |
| CV26 | OTP şablonu/içeriği yönetici SMS listesi veya destek ekranında görünmez | 03,06 | Yetkili yönetici ile negatif okuma |
| CV27 | V1 profil yazımı sahiplik kanıtını bypass etmez; eski istemci alan/null sözleşmesi korunur | 01,06 | Eski istemci fixture/DTO |
| CV28 | Yeni tabloların BaseEntity alanları, migrations ilk/yükseltme/tekrar ve veri koruyan Down | 01,06 | İzole gerçek PostgreSQL |
| CV29 | Eski telefonlar uydurma aktör/zamanla VERIFIED yapılmaz; geçiş eski hesapları bozmaz | 01,07 | Backfill/rollout testi |
| CV30 | TR/EN, ikon, açık/koyu/sistem, mobil dar ekran ve klavye/odak | 04,05 | Gerçek UI etkileşimleri |
| CV31 | Boş telefon isteğe bağlılık kararına uyar; gönderici yoksa UNAVAILABLE, sahte gönderildi yok | 03,04,05 | Karar + servis kesintisi |
| CV32 | SMS gideri global doğrulama ile Property kredisi arasında yetkisiz paylaştırılamaz | 03,06 | Maliyet/tenant negatifleri |

Gönderim sonucunun başarılı olması, alıcının iletişim sahipliğini doğruladığı anlamına gelmez. E-posta sahiplik otoritesi Keycloak; telefon kanıtı sunucu doğrulama akışıdır. Tüm test verileri sentetik/izinli fixture; gerçek alıcı testleri ayrıca yetkilidir.

