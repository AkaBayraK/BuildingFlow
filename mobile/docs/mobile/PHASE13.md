# Faz 13 — Duyurular ve bildirimler

15.10.2026. Kullanıcı Faz13’ü onayladı; yerel `main` üzerinde çalışıldı. Önceki Faz12 ve eşzamanlı merkez değişiklikleri korundu. Commit, push veya mağaza yayını yapılmadı. Kanonik kapsam `mobile/buildingflow-mobile-prompts/PHASES.md`; Faz14 başlamadı.

## Sunulan akış

Alt sekmedeki **Bildirimler** ekranında Duyurular ve Mesaj kutum bulunur. Lacivert okunmamış özeti, kartlar, çizgi ikonlar, detay, boş/hata/yükleniyor durumları, 50 kayıtla sayfalama ve son güncelleme zamanı eklendi. Yayınlanmış ALL_MEMBERS duyuruları yetkili sakinlere; MANAGEMENT duyuruları yalnız gerçek yönetim bağlamlarına açılır. Taslak ve arşiv kayıtları yönetici için ayrı açılır panelde korunur. Kullanıcıya gönderilmiş mevcut IN_APP PersonMessage kayıtları kişisel mesaj kutusuna alınır; başka alıcı mesajı veya kişi kimliği DTO’ya eklenmez.

**Okundu işaretle** açık bir kullanıcı eylemidir. Merkezde ilk okuma zamanı korunur; başka cihaz aynı hesaba girdiğinde bu bilgiyi alır. Mesaj okuma mevcut merkez ReadAt kaydını kullanır. Duyuru okuması hesap/duyuru başına tek kayıttır. Tekrar ve eşzamanlı gönderim ikinci kayıt oluşturmaz. Listeyi açmak otomatik okundu işlemi yapmaz.

**Yeni içerik vurguları** tercihi hesap başına merkezde version ile saklanır. Kapatınca sayaç/kart vurgusu kapanır; mesajlar kaybolmaz veya okundu sayılmaz. Çevrimdışı değiştirilmez. Başka cihazdaki çakışmada409 ile güncel tercih alınır; kayıp yanıtın aynı değerle tekrarı güvenlidir.

## Push sınırı ve bağlantılar

Depoda APNs/FCM/Expo push sağlayıcısı ve token teslim sözleşmesi bulunmuyor. `pushAvailable=false`; OS bildirim izni istenmez, token kaydı veya sahte telefon bildirimi üretilmez. Ekrandaki Telefon bildirimleri kartı bu durumu ve ileride bağlamsal izin akışını açıklar. Duyuru/mesaj kutusu OS izninden bağımsızdır. Sağlayıcı, cihaz-token yaşam döngüsü, sunucu olayı, genel ve hassas içerik içermeyen OS metni, gerçek izin kabul-ret ve push deep link teslimi **açık bağımlılıktır**; tam Faz13 ürün kabulü değildir.

`/announcements?id=<uuid>&kind=ANNOUNCEMENT|MESSAGE&propertyId=<uuid>` bağlantısı giriş kapısından ve seçili doğrulanmış mülk bağlamından geçer. Dizi/bozuk kimlik, yabancı mülk veya doğrulanmamış rol reddedilir. Otomatik rol/mülk yetkisi verilmez. API itemId sorgusunda aynı yayın/alıcı/üyelik kontrolleri tekrar uygulanır. Bu, push sağlayıcısının gerçek link tesliminin test edildiği anlamına gelmez.

## Çevrimdışı veri

SQLite şeması6→7 eklemelidir; önceki taslak, dosya, outbox ve snapshot kayıtları korunur. `notice_jobs` yalnız hesap, bağlam, tür, kimlik, deneme ve hata metadata’sı tutar. Hesap başına mevcut SQLCipher/cache anahtarı kullanılır. Duyuru/mesaj kopyaları mevcut owner_snapshots içinde bağlam/tür başına tek slotla sınırlıdır; son liste sayfası veya detay tutulur. Bütün kutu indirilmiş sayılmaz. 24saat TTL, hesap/rol/mülk/sorgu doğrulaması ve eski yanıt reddi uygulanır. Çıkış mevcut anahtar öncelikli temizliği kullanır; doğrulanmış oturumda kaldırılan bağlamların kopya/kuyrukları temizlenir.

Mobil okundu kuyruğu aynı kimliği tekilleştirir; bağlantı varken ekran açıkken/yeniden açıldığında eşitler. Her tur en fazla10işlem,6deneme, üst sınırı60saniye bekleme vardır. Ağ/429/5xx yeniden denenebilir;401/403 veya bozuk200makbuz başarı sayılmaz. Geç çıkış/bağlam değişiminde yanıt uygulanmaz. Web yalnız çevrimiçi çalışır. İşletim sistemi arka plan görevi veya garantili kapalı uygulama eşitlemesi eklenmedi.

## Merkez ve migration

`GET /api/v1/mobile/notices`, `POST /read`, `POST /preference` uygulandı. Yetki her istekte gerçek MobileSession bağlamlarından türetilir. SUPER_ADMIN mevcut global aktif Property kapsamını kullanır; yönetim rolü yokken seçili MANAGER/SUPER_ADMIN metni yetki vermez. Kişisel mesajda mevcut alıcı ve aktif üyelik filtresi korunur. Yeni BaseEntity tablolarında sekiz zorunlu audit/sürüm/silme/etkinlik alanı ve compositeFK/unique indexler vardır.

Merkez geliştirme sürecinin otomatik şema eşitlemesi `20261015141531_AutoSchema_20261015171529` migration’ını üretip uygulamış; elle isimlendirilen `20261015141657_MobileNoticeReadAndPreferences` aynı model için boş bir takip migration’ı olmuştur. İkisi de uygulanmış olduğu için silinmedi/yeniden yazılmadı; source/Designer/snapshot korunur. Yeni tablolar ilk migration’da kurulur. Otomatik Down bu tabloları siler ve okundu/tercih verisini kaybettirir; geri alma çalıştırılmadı, gerekirse önce yedek ve incelenmiş ileri düzeltme kullanılmalıdır. Son yerel update bekleyen migration bulmadı. Kontrol edilen Properties/People/Units ve MobileRequest eski alanlarının bu son kontrol öncesi/sonrası hash’leri aynı; otomatik ilk uygulamadan önce hash alınmış gibi sunulmaz.

## Bu tur kanıt

- **67/67 mobil test**, sıfır atlama: sözleşme/whitelist, yabancı link/bağlam, cache/TTL/sayfa, eski yanıt, kayıp cevap ve replay, bozuk makbuz/403, çıkış guard, yetki purge ve SQLite6→7 koruması; önceki faz regresyonları.
- **102/102 ayrı gerçek PostgreSQL/SeaweedFS entegrasyon testi**, sıfır atlama: sekiz yeni bildirim testi; yayın/audience/alıcı projeksiyonu, eşzamanlı okuma/başka cihaz, mevcut merkez mesaj ReadAt, anonim/rol/mülk/üyelik, tercih version/replay/hesap ayrımı, sayfalama, gerçek kiracı-daire ilişkisi ve kapatılmış tercih. Model, tüm tablo audit kolonları, ilk kurulum/yükseltme/tekrar/idempotentSQL ve fotoğraf regresyonları dahil. Geçici DB/rol/bucket yalnız fixture kapsamıyla temizlendi.
- TypeScript ve iOS/Android/Web export geçti. Lint sıfır hata, önceki useOwner cleanup uyarısı1. Yeni native modül veya paket sürümü eklenmedi; önceki geliştirme binary’si yeterli. Bu tur fiziksel cihaz kurulumu veya UI/OS izin kabulü yapılmadı.
- İlk test koşusunda test hesabı eksik SQLite fixture’ı ve kapalı özel S3 servisi nedeniyle başarısızlık görüldü. Fixture düzeltildi, mevcut seaweedfs-s3 servisi başlatıldı; son koşular tamamen geçti. İlave kiracı fixture’ındaki zorunlu iletişim alanları da düzeltildi.
- API5006 `/health`:200; anonim notices:401; Metro8081 running. Mobil servisler main modülünden yeniden başlatıldı. Gerçek kullanıcı mesajına/okundu durumuna test mutation’ı yapılmadı.

Kullanıcının önceki talimatı gereği görsel inceleme yapılmadı. Gerçek iPhone/Android okuma/tercih, uçak modu/soğuk açılış, ekran okuyucu/metin ölçeği ve push izin/link kabulü açıktır. GitHub Faz13 **In review** olarak izlenir. Test sayısı üretime hazırlık yüzdesi değildir.

GitHub takip:22yönetilen kayıt,7Done/9Inreview/4Backlog/2Ready;7/7 takip script testi geçti. Faz13 In review, Faz14–15 Backlog. Kullanıcının önceki proje takip yetkisiyle yalnız yönetilen bloklar güncellendi,4TraceFlow kaydı korundu.

Son hesap kota okuması Codex usage aracı:5saatlik pencere%86kullanılan/%14kalan, reset15.10.2026 21:17:36 Europe/Istanbul; haftalık%29/%71, reset22.10.2026 10:57:02. Bu, fazın token/süre/context tüketimi değildir. Giriş/çıkış/toplam token,kalancontext ve tur tüketimi:BİLGİ MEVCUT DEĞİL.


### Faz13 teslim ve main aktarımı — 15.10.2026

Çalışma sırasında başka bir işlem ortak checkout’ı feature/dev-readable-console-logs dalına geçirip yalnız konsol değişikliğini commit etmişti. Kullanıcı “Main’e aktar; mevcut değişiklikleri koru” onayı verdi. main tabanına göre binary diff ve geçici stash yedeği alındı; main’e geçilip tüm mevcut tracked değişiklikler ve konsol değişikliği uygulandı.66dosyanın önce/sonra SHA256 değeri birebir aynı; untracked Faz12–13 dosyaları korundu. Feature commit ve aktarım stash yedeği silinmedi. Bu işlem yeni commit/merge/push yapmadı; aktif dalmain, kaynakAPI/Metro sağlıklı,gitdiffchecktemiz. Stash bir yedektir; main’de aynı değişiklikler zaten bulunduğundan tekrar uygulanmamalıdır.

Son teslim:67/67mobil,102/102PG+S3,7/7GitHubscript; TypeScript/üçplatformexport geçti;22GitHubkaydı problems=[] ile doğrulandı. Faz13Inreview,push/cihazkabulü açık;Faz14başlamadı. Paket lisans metadata kontrolü başarılı; yeni bağımlılık yok. Kanıtlar /tmp/bf13/ altında; kalıcı ürün/teknik özet mobile/docs/mobile/PHASE13.md.
