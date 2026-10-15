# GitHub Projects — BuildingFlow Mobil

15.10.2026. Kullanıcı isteğiyle hedef: https://github.com/users/AkaBayraK/projects/3 (`@BuildingFlow-Task`, private).

## Kaynak ve durum politikası

Tek faz kaynağı `buildingflow-mobile-prompts/PHASES.md` (15 faz). `mobile/scripts/github-project/deliveries.mjs` yeni iş planı değildir; bu kaynağın teslim/kanıt/durum eşlemesidir. Faz02–09 raporları, BRAND ve merkez SeaweedFS raporu kullanılır. Faz01 için bağımsız rapor dosyası bulunmadığı açıkça kaydedilir; kullanıcı onayı/veri kararı Faz02 belgesinde mevcuttur.

22yönetilen kart:15faz+3bakım+4kabul/güvenlik. Faz01–04 Done; Faz05–11 kod/otomatik kanıt mevcut ama cihaz/ürün kabulü açık olduğu için In review; Faz12–15 Backlog. Marka/SeaweedFS/adlandırma bakımları Done. Offline ve fotoğraf kabulü Ready; yetkili OWNER/TENANT cihaz kabulü ve audit/release takibi Backlog. Bu status kararı önceki “kod tamamlandı” teslimini tüm cihaz/release kabulü gibi göstermemek içindir. Pano kaydı yeni faza kodlama onayı veya mağaza yayını değildir.

Önceden panoda bulunan4TraceFlow Issue kaydı değiştirilmez/silinmez. Mobil git remote olmadığından taslak proje kayıtları oluşturulur; Issue/PR/uzakCI varmış gibi gösterilmez. Depo bağlama, commit/push, assignee/estimate/hedef tarih veya görünürlük değişikliği yapılmaz.

## Komutlar

`mobile` klasöründen:

```sh
npm run project:plan    # Yerel plan; ağda yazma/okuma yapmaz.
npm run project:sync    # Mevcut gh kimliğiyle hedef panoya kontrollü yazma.
npm run project:verify  # Uzak kart içerik/durumlarını salt okunur doğrula.
node --test scripts/github-project/core.test.mjs
```

Sırlar/gh token scriptte/depoda tutulmaz; gh mevcut güvenli kimliğini kullanır. İlk dry-run sadece `mobile/artifacts/github-project/plan.json` üretir; apply/verify sonucu `last-report.json`. Bu klasör gitignore’dadır. `--apply` olmadan uzak mutation yapılmaz. Kullanıcı bu turda belirtilen panoya mobil ilerleme bilgisi yazımını açıkça onaylamıştır; gelecekte sync komutunun çalıştırılması ilgili yazma kapsamına yönelik kullanıcı yetkisi gerektirir. Otomatik zamanlanmış görev/heartbeat kurulmadı.

## Güvenli eşitleme

- Hedef kullanıcı/number ve GraphQL project node kimliği doğrulanır. Status/Priority alanları canlı panodan alınır; seçenek kimliği tahmin edilmez.
- Kanonik15faz, tekil plan anahtarları, geçerli durum/öncelik ve hassas yerel kimlik/token örüntüleri ilk yazımdan önce doğrulanır. Oluşturulmuş public içerik yerine özel teknik ilerleme özeti gönderilir; ham env/log, telefon UDID, kişisel dosya yolu veya gerçek kullanıcı fotoğrafı paylaşılmaz.
- Kalıcı HTML anahtarıyla upsert: aynı kart yeniden oluşturulmaz. Yinelenen anahtar, aynı başlıklı ama yönetilmeyen kayıt, Issue/PR’ye dönüşmüş içerik ve bozuk yönetim bloğunda işlem durur; ilgisiz kayıt sahiplenilmez.
- Taslak içerikte yönetim bloğu dışındaki insan notları korunur. Yönetilen bölüm kaynak dosyadan yenilenir. Önceden atanmış insan önceliği korunur; boş öncelik ilk plandan doldurulur. Status bu kanıt eşlemesinin yönettiği alandır.
- Pano README’sine yönetilen mobil özet eklenir; mevcut insan metni korunur. Draft/alan mutation’ları GraphQL değişkenleri JSON stdin üzerinden gider; shell interpolation, token komut satırı veya gövde shell escaping kullanılmaz.
- Sonuç uzaktan yeniden okunur:22kartın title/body/status eşleşmesi ve önceki4TraceFlow kaydının değişmediği kontrol edilir. GraphQL işlemleri toplu atomik değildir; kısmi kesintiden sonra tekrar sync aynı kimlikle devam eder, doğrulama geçmeden tam başarı raporu verilmez.

## Doğrulama sınırı

7hedefli script testi: kanonikplan, insan notu/idempotency, foreign/duplicate/non-draft, aynıtitleunmanagedkoruması, durum/öncelik/PIIpreflight markerbozulması ve belirsiz oluşturma cevabında ikinci taslağın engellenmesi. `node --check` geçti. Tüm uygulama lint’i bu tur0error/7mevcutuyarı gösterdi (üç sekmede duplicateimport ve useOwner effectref uyarısı); bu kayıt görevi uygulama davranışını değiştirmedi ve bu uyarıları giderilmiş saymaz. Faz09’daki45mobil/63merkez test sayıları tarihsel kaynak kanıtıdır; bu pano turunda iş testleri yeniden çalıştırılmadı.

## Uzak sonuç — 15.10.2026

Hedef panoda22yönetilen taslak kayıt mevcut:7Done (4faz+3bakım),5Inreview,8Backlog,2Ready. Önceki4TraceFlowIssue kaydı korunur; pano toplam26kayıt. Kartların title/body/status ve proje README özeti uzaktan doğrulandı. İlk yazım22oluşturdu; tekrar apply0oluşturma/0gövde güncellemesi,22/22doğrulama,problems[]; salt okunur verify de22/22geçti. İlk yazımdan hemen sonraki liste API’sinde bazı yeni kartların görünmesi gecikti; doğrulama bu sırada başarı sayılmadı. Sonraki read ve kalıcı mutation günlüğüyle nodeId üzerinden takip eklendi: oluşturma niyeti ağ isteğinden önce kaydedilir; belirsiz sonuçta yeni taslak açılmaz, bilinen node’lar liste indeksine bağlı kalmadan okunabilir. Taslakların karşılık geldiği yeni GitHub repo/Issue oluşturulmadı.

Hedefli7test, scriptESLint ve node syntax geçti; mobileTypeScript geçti. Tam uygulama lint’inde0error/7mevcutuyarı bulundu; bu uyarılar ayrı mevcut UI dosyalarına aittir ve bu tur düzeltildiği iddia edilmez. Plan metadata değişince önce planı inceleyin, sonra explicit sync ve verify çalıştırın. Üretim kapsamına ilişkin yeni özellik eklenmedi.


Pano görünüm kontrolü: Backlog/Priority board/Team items/Roadmap filtreleri boş. My items görünümü `assignee:@me` filtrelidir; otomatik atama yapılmadığı için yeni taslaklar orada görünmeyebilir. Backlog görünümünden tüm mobil kayıtlar izlenebilir.
