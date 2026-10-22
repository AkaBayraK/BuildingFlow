# BuildingFlow — Ortak e-posta ve cep telefonu doğrulaması / MASTER PROMPT

Plan kimliği: **BF-VERIFY**. Toplam **8 faz: 00–07**. Bu plan İŞ/UI/MOBİL/BF-MULTI sayaçlarından ayrıdır.
İlk kapsam **önce prompt, görevler ve analiz fazı** idi; FAZ00 tamamlandı. FAZ01 tamamlandı; güncel kullanıcı mesajıyla FAZ02 yetkilidir, FAZ03–07 başlamaz.

## Amaç ve kesin sınırlar

Mobil uygulama, merkez web portalı ve yönetim web ekranı aynı giriş hesabının iletişim doğrulama durumunu kullanır. Yönetici kişi kaydını, kişi yanındaymış gibi SMS kodu bekletmeden oluşturabilir. Giriş hesabının sahibi e-postasını bağlantıyla ve telefonunu kodla doğrular. Değişmeyen normalize değerde yeniden doğrulama istenmez; değişen kanal yeniden doğrulanır. Bir cihazda tamamlanan işlem diğer istemcide sunucudan yenilendiğinde görülür; her girişte veya bina/daire/rol değişiminde yeni doğrulama yoktur.

**Person kaydı ≠ giriş hesabı ≠ malik/kiracı ilişkisinin yönetici onayı.** Hesap iletişimi doğrulamak Person birleştirme, yeni üyelik veya daire yetkisi üretmez. Aynı hesaba ait aynı iletişim kanıtı hesap düzeyindedir; başka hesaba kopyalanamaz.

D1/D2 kullanıcı tarafından 22.10.2026 onaylandı; aşağıdaki güncel kapsamı esas al. D3/D4 sağlayıcı/OTP kararları kendi fazlarında bekler. Telefonun mevcut isteğe bağlı girişini sessizce zorunlu hale getirme. Gerçek SMS sağlayıcısı kaynakta yoktur; yalnız MOCK bulunmaktadır.

## Çalışma yöntemi

1. Kök ve etkilenen modül AGENTS.md; STATUS/SESSION; ilgili mimari/API/test belgeleri ve bu PHASES.md okunur. Git değişiklikleri korunur. Tarihsel başka master prompt tamamlanmış fazı yeniden başlatmaz.
2. Yalnız açıkça başlatılan fazın promptunu uygula. Kullanıcı aynı anda birden fazla fazı ve sırasını açıkça onayladıysa tekrar onay istemeden o sırayla ilerle; bunun dışında sonraki faza kendiliğinden geçme. Tek agent; kullanıcı istemeden alt agent yok.
3. GitHub görevini önce oku; aynı marker/FAZ varsa yenisini açma. Mevcut @BuildingFlow (Project 3) panosunda Backlog → Ready → In progress → In review → Done akışını kullan. GitHub issue açık/kapalı durumu ile pano durumu ayrı olup ikisi doğrulanır.
4. Her faz 3 resmi adımdır: (1) önkoşul/sözleşme/kapsam incelemesi, (2) faz çıktılarının uygulanması, (3) zorunlu kanıt ve görev teslimi. Checklist alt maddeleri resmi adım sayısını şişirmez.
5. Türkçe konsol ve çalışma mesajlarında [BUILDINGFLOW], [FAZ BF-VERIFY NN | KAYIT sıra/08], [ADIM n/03], [DURUM], [İŞLEM]/[DOSYA]/[TEST]/[İLERLEME] kullan. En fazla 60 saniyede anlamlı durum ver. Parola/token/OTP/reset veya doğrulama bağlantısı/kişisel veri loglama.
6. İlerleme yalnız tamamlanan adım / 3; plan ilerlemesi kapanış kanıtı tamamlanan faz / 8. Uygulama ve belge fazı ayrılır. STATUS/SESSION özet; TESTING kanıt; CHANGELOG gerçek değişiklik geçmişidir. İŞ19 veya mobil yol haritası yalnız uygulanıp doğrulanmış gelişme varsa güncellenir.

## Ortak mimari ve güvenlik sözleşmesi

- Mevcut modular monolith ve katman sınırları korunur. Yeni paket, SDK, font, ikon, container veya servis ekleme; mevcut Keycloak, .NET BCL/ASP.NET Core ve kurulu React/Expo altyapısı önceliklidir. Harici SMS hizmetinin şartı/ücreti ve veri aktarımı yeni paket olmasa da ayrıca karara bağlanır.
- E-posta için otorite Keycloak'tır. Doğrulanmış iddia doğrudan kullanıcıdan veya yalnız UI'dan alınmaz. Kurulu sürümde VERIFY_EMAIL/UPDATE_EMAIL/execute-actions-email davranışı izole gerçek IdP ile kanıtlanır; UPDATE_PASSWORD gönderildi diye sonuç varsayılmaz.
- Token alınmadan önce gereken e-posta doğrulaması Keycloak giriş/required-action akışındadır. Sırf doğrulama için anonim userId/e-posta ile keyfi hesaba işlem yapan API açma. Telefon işlemleri giriş sonrası kendi hesap kimliğiyle çalışır.
- Web ve mobil aynı uygulama servisini kullanır; endpointler ayrı uyumluluk adaptörü olabilir. Hesap kimliği JWT sub'dan gelir; Person/Property/Unit kimliği kanıt değildir. Kendi hesabı için bina üyeliği zorunlu tutulmaz.
- Durum modelinde iletişimin varlığı ve sahiplik kanıtı, bekleyen aday, OTP challenge ve gönderim sonucu birbirinden ayrılır. QUEUED/ACCEPTED veya HTTP 200, VERIFIED demek değildir. Sağlayıcı kesintisinde başarısız/unknown bilgi başarı gibi sunulmaz.
- Kanıt hesap+kanal+normalize değer+amaç/sürümle bağlanır. Mevcut doğrulanmış değer yeni aday doğrulanana kadar korunur. Eski v1 profil yazımları bu modeli atlayamaz; Phone semantiği ve adaptör uyumluluğu FAZ 01'de açıkça tanımlanır. İsim/fotoğraf/rıza/rol/daire değişimi kanıtı sıfırlamaz.
- E-posta değişikliği yeniden kimlik doğrulama, yeni adres onayı ve eski adrese bildirimle yapılır; Keycloak native akışı tercih edilir. Users.Update gibi alternatif doğrudan yazım yolları kapsam dışı bırakılmaz. Yönetici davet başlatabilir, sahiplik kanıtını elle VERIFIED yapamaz.
- OTP kriptografik RNG ile üretilir; düşük entropili kod yalnız düz SHA ile korunmaz, ayrı amaçlı sunucu anahtarlı HMAC ve kısa ömür kullanılır. Ham kod/link log, telemetri, yönetici SMS listesi, API hata metni, SQLite veya normal depoya yazılmaz. Gönderim için gereken geçici payload korunur ve süre sonunda temizlenir.
- Sunucu süre/deneme/yeniden gönderim ve dağıtık limitleri uygular; UUID/fingerprint replay SMS'i tekrar üretmez. Yeni challenge önceki aktif challenge'ı atomik geçersiz kılar. İki cihaz yarışı, eski aday, version/409 ve belirsiz sağlayıcı cevabı test edilir.
- OTP gönderimi mevcut Property SMS kredisine keyfi bağlanamaz. Platform göndereni veya davet kapsamındaki açık maliyet hesabı D3 ile belirlenir; başka alanın kredisi/yetkisi kullanılmaz. MOCK üretimde doğrulama teslim kanıtı değildir.
- Mobil doğrulama online'dır; OTP eylemi offline kuyruğa alınmaz. Ön plana dönüşte güncel sunucu durumu okunur. Logout/hesap değişiminde hassas pending state temizlenir. Her girişte ikinci faktör isteme ayrı üründür; bu refactora kendiliğinden katılmaz.
- Yeni tablo BaseEntity ve sekiz NOT NULL alan, ortak mapping/audit/UUID/version/soft-delete kuralına uyar. EF migration/Designer/snapshot birlikte; mevcut telefonlar sahte VERIFIED backfill edilmez. Uygulanmış migration değiştirilmez; gerçek izole PostgreSQL ilk kurulum/yükseltme/tekrar ve bütün tablo sözleşmeleri test edilir.
- Gerçek .env, kullanıcı, parola, canlı Keycloak/SMTP/SMS, feature policy ve DB'yi plan/analiz sırasında değiştirme. Test yalnız fixture kaynaklarını kullanır. Canlı açılış ortam yetkisi olmadan yapılmaz.
- Beklenen hatalar ErrorCatalog + güvenli parametre + FE/mobile sözleşmesiyle; TR/EN, ortak UI, ikonlu durum/çip, erişilebilirlik ve semantik tema korunur. Yeni hata ve endpoint negatif kabul testleriyle gelir.

## Görev teslimi ve otomatik kapanış kuralı

Kullanıcı, **iş tamamlanınca yapılanları ayrıntılı maddelerle yazmayı ve hiçbir madde eksik değilse taskı kapatmayı** açıkça istedi. Tamamlanmış göreve ayrıca yapay bir kullanıcı-kabul checkbox'ı ekleyerek kapanışı bekletme. Sonraki fazın başlatılması ayrı kapsam kararıdır.

- Kapsam ve kabulün kanonik tanımı PHASES.md; canlı checkbox/işlem/kanıt durumu ilgili tek GitHub issue'dur. İkinci bağımsız backlog oluşturma. Faz raporu kanıtı ayrıntılandırır.
- TASK_REPORT_TEMPLATE.md'yi kullanarak **Yapılanlar** bölümünü ayrıntılı maddelerle güncelle: her maddede davranış, neden, dosya, API/DTO, yetki/scope, migration/uyumluluk, test komutu ve gerçek sonuç.
- Sadece build geçmesi; mock mesaj; SMTP kabulü; skip; eski tur test sonucu veya kanıtsız “çalışıyor” zorunlu maddeyi tamamlamaz.
- Faza uygulanmayan test açık gerekçeyle UYGULANMAZ olabilir; bu, zorunlu runtime testini atlamak için kullanılamaz. Zorunlu kanıt DOĞRULANAMADI/BAŞARISIZ ise görev açık ve In progress/In review kalır.
- Eksik maddeyi başka göreve taşımak asıl işi bitirmez. Kapsam azaltılacaksa kullanıcı kararı ve eski/yeni kabul kapsamı görünür olmalı.
- Önce güncel issue'yu yeniden oku; bütün kapsam ve kapanış maddelerinin kanıtı tamamlandıysa checkbox'ları ve ayrıntılı raporu güncelle. Ardından issue'yu completed gerekçesiyle kapat ve pano durumunu Done yap.
- Son okumada hem issue Closed hem Project 3 Done olduğunu doğrula. Kısmi ağ hatasında körlemesine yeniden task açma; son durumu okuyup tutarsızlığı düzelt veya açıkça bildir. Eksik doğrulama varsa tamamlandı deme.
- Yerel dosyalar commit/push edilmediyse raporda açıkça yaz; GitHub'da mevcut olmayan blob bağlantılarını varmış gibi sunma.



## Güncel kullanıcı yetkisi — 22.10.2026

Prompt/görev/analiz teslimi tamamlandı; #43 Closed(completed), Project3 Done son okumayla doğrulandı. Kullanıcının **ilk fazdan başlayabilirsin** mesajı BF-VERIFY FAZ01/#44 ilk uygulama fazını başlattı; FAZ02–07 henüz yetkili değil. Yukarıdaki önce-yalnız-FAZ00 ifadeleri başlangıç kapsamının tarihçesidir; güncel kapsam bu kayıttır.


## Son durum — 22.10.2026

FAZ00/#43 ve ilk uygulama FAZ01/#44 tamamlandı;iki issue Closed(completed),Project3 Done,0 açık maddeyle son okumada doğrulandı.Kalan #45–#50 açık/Backlog;FAZ02 ayrıca başlatılmadı.FAZ01 kanıtı:14/14 PostgreSQL/API+6/6 domain ve build GEÇTİ.Gerçek e-posta/SMS gönderim workflow'u ve istemci doğrulama ekranları sonraki fazlardadır.


## Güncel kapsam — BF-VERIFY 02 / 22.10.2026

Kullanıcı sıradaki fazı başlattı; #45 In progress. D1/D2 açık onayı: yeni hesaplarda e-posta doğrulaması zorunlu, telefon isteğe bağlı; mevcut doğrulanmış adresler korunur, eski hesaplar kontrollü geçirilir, e-postaya erişim kaybında otomatik VERIFIED verilmeden yetkili destek gerekir. Canlı zorunluluk bu fazda açılmaz; EmailEnabled opt-in olarak kapalı kalır. FAZ03–07 başlatılmadı.


## Son doğrulanan durum — BF-VERIFY02

FAZ00/01/02 tamamlandı; #45 Closed(completed), Project3 Done,12/12 madde son uzak okumada doğrulandı. Faz3/3=%100,00;plan3/8=%37,50. FAZ03–07 başlatılmadı. Kanıt: `docs/refactor/contact-verification/02-report.md`. Önceki kapsam paragrafları tarihsel kayıttır.
