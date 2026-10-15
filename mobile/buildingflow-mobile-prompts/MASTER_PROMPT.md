# BuildingFlow Mobile — Master Prompt

## Görev

Mevcut BuildingFlow projesine, kullanıcı memnuniyeti ve görsel kaliteyi önceliklendiren iOS/Android mobil uygulamasını fazlar halinde tasarla ve geliştir. Her oturumda yalnızca kullanıcının açıkça başlattığı tek fazı uygula. Kullanıcı bir sonraki faza geç demeden ilerleme.

## Ürün bağlamı

- Ürün adı **BuildingFlow**. TraceFlow ayrı bir üründür; TraceFlow'a ait logo, renk paleti, görsel, metin, ekran düzeni veya alan modeli BuildingFlow'a taşınmayacaktır.
- Temel mobil kullanıcıları: **ev sahibi, kiracı ve yönetici**. Bir hesap birden fazla rolde/mülkte bulunabilir; uygun olduğu yerde kolay rol/mülk değişimi sağlanmalıdır.
- Mobil deneyim masaüstü ekranının küçültülmüş hali olmayacaktır. Sık işlemler kısa akışlarla, büyük dokunma alanlarıyla ve anlaşılır durum mesajlarıyla tamamlanmalıdır.
- Görsel kalite ürün gereksinimidir: tutarlı tipografi, boşluk, renk, ikon, erişilebilir kontrast, yükleme/boş/hata/çevrimdışı durumları ve gerçek cihaz boyutlarında kullanım düşünülmelidir.
- Uygulama kullanıcının profil fotoğrafını ve talep/arıza fotoğraflarını telefondan seçip çekebilmelidir. İzinler yalnızca ilgili özellik kullanılırken istenir. Aydınlatma, açık rıza ve diğer hukuki dayanakları birbirine karıştırma; hukuki yorum uydurma. Gereken metin ve saklama/işleme kararları raporlanıp ürün sahibinin onayına sunulur. Rıza verilmemesi, fotoğraf zorunlu değilse temel uygulama kullanımını engellemez.

## Başlangıç teknoloji varsayımları — depoda doğrula

- Mobil: React Native + Expo + TypeScript (mevcut repo ve native gereksinimler uyumluysa).
- Yerel veri: cihazda kullanıcı/kurulum bağlamına ait SQLite. Hassas alanlar korunmalı; şifreleme ve anahtar saklama yöntemi kullanılan Expo/React Native sürümüyle uyumluluk kontrolünden geçmeli.
- Backend: mevcut BuildingFlow backend'i ve API sözleşmeleri. Depoda doğrulanmadan .NET, Keycloak, PostgreSQL veya başka teknoloji varmış gibi davranma.
- Mobil uygulama merkezi DB'ye doğrudan bağlanmaz. API kullanımı ve çevrimiçi senkronizasyon ayrı karardır.

## Offline ve veri sahipliği kararı

Aşağıdaki iki kapsamı Faz 01'de açıkça karşılaştır ve ürün sahibi kararını bekle:

**A — Offline-first, gerektiğinde senkronize olan mobil istemci (önerilen varsayılan):** Her uygulama kurulumu/kullanıcı bağlamı kendi SQLite verisini tutar; önceden indirilmiş veriler çevrimdışı okunabilir, desteklenen işlemler kuyruklanır; bağlantı geldiğinde yetkilendirilmiş API ile senkronize edilir. Merkezi backend varsa merkezi iş kayıtlarının yetkili kaynağıdır. Ödeme, ilk oturum açma ve güncel bakiye gibi işlemler online gerektirebilir.

**B — Merkezden tamamen bağımsız çalışma:** Merkezi API/DB olmadan bir cihaz veya cihazlar arası veri paylaşımı, yedekleme, kullanıcı/rol yönetimi ve eşzamanlı düzenleme nasıl yapılacak ayrıca tanımlanmalıdır. Tek cihazın yerel uygulaması ile birden fazla kullanıcının ortak bina verisini paylaşması aynı mimari değildir. Bu gereksinimler karara bağlanmadan tam bağımsız sistem varsayma veya sync kodlama.

Aynı cihazda hesap değişiminde verinin ayrıştırılması, oturum kapatmada hassas verinin korunması/silinmesi, cihaz kaybı, dışa aktarma/yedek ve çakışma stratejisi analiz edilmelidir. Sunucuya gönderilen yazma işlemleri idempotent olmalı; UUID, sürüm ve silme/iptal semantiği tutarlı tasarlanmalıdır.

## Kesin çalışma kuralları

1. İlk çalıştırmada yalnızca **Faz 01: mevcut projeyi analiz et ve mimari/ürün raporu hazırla**. Kod, bağımlılık, yapılandırma, migration, görsel veya klasör oluşturma/değiştirme. İstenen raporu uygun mevcut bir belgeye yazma izni de yoktur; analiz raporunu konsola ve yanıtına ver. Faz 01 sonrası dur ve onay bekle.
2. İlk olarak repo kökünü, mevcut `AGENTS.md` ve proje belgelerini, Git durumunu, mobil/web/backend klasörlerini, framework sürümlerini, mevcut tasarım sistemini, API/auth yaklaşımını ve test komutlarını incele. Çalışma ağacındaki kullanıcı değişikliklerini koru.
3. Bir faz başlatıldığında sadece o fazın kapsamındaki dosyalara dokun. Gerekli bir önkoşul eksikse kodu varsayımla yazma; somut bulguyu ve blokajı raporla.
4. Faz kapsamı için kabul ölçütlerini uygula; mevcut testleri ve faza uygun kontrolleri çalıştır. Başarısız kontrolleri açıkça yaz, başarılı gibi gösterme.
5. Her faz sonunda değişen dosyaları, kullanıcıya görünen sonucu, test/komut ve sonuçlarını, açık kararları ve sonraki fazın önkoşullarını Türkçe özetle. Ardından dur.
6. Kodlama oturumlarının başında ve her anlamlı alt adımda konsola Türkçe durum satırı bas: `[BuildingFlow Mobil][Faz NN][x/y] ...`. Faz tamamlanma ve test sonuçlarını da konsola yaz. Token bütçesini veya kalan kullanım süresini görmüyorsan değer uydurma; bunun yerine mevcut ortamda görünmediğini belirt.
7. Lisans ve bakım durumu belirsiz bağımlılıkları ekleme. Mevcut sürümler ve resmi dokümantasyonla uyumluluğu doğrulanmadan paket yükseltme.
8. Gizli anahtar, kişisel veri veya gerçek kullanıcı fotoğrafı loglama. Testlerde sahte veri kullan.
9. Kullanıcı arayüzünde Türkçe varsayılanı ve erişilebilir metinleri kullan; uluslararasılaştırma mevcutsa mevcut yaklaşımı izle.
10. Görsel oluşturma/görsel kullanımı: yalnızca BuildingFlow marka varlıkları ve kullanıcı tarafından verilen BuildingFlow referansları. TraceFlow görsellerini kullanma. Depoda uygun varlık yoksa yer tutucu oluşturup finalmiş gibi sunma; hangi görsel varlıkların gerektiğini raporla.

## İlk Çalıştırma Promptu

> BuildingFlow Mobile için `MASTER_PROMPT.md`, `AGENTS.md` ve `PHASES.md` dosyalarını oku. Önce mevcut BuildingFlow deposunu analiz et. Yalnızca Faz 01'i gerçekleştir: mevcut mimariyi, repo yapısını, kullanıcı rollerini, backend/API/auth durumunu, mobil başlangıç durumunu, offline gereksinimlerini, KVKK/fotoğraf akışını, görsel marka varlıklarını, test yaklaşımını ve riskleri raporla. SQLite'ın cihaz başına veritabanı olması, tek cihaz/çok cihaz farkı ve merkezi sistemle senkronizasyon seçeneklerini karşılaştır; eksik kararları varsayma. Konsola Türkçe ilerleme yaz. Hiçbir dosyayı oluşturma, değiştirme veya silme; bağımlılık yükleme ve kod çalıştırarak dosya üretme. Analiz raporunu Türkçe olarak konsola ver ve ardından durup onayımı bekle.

## v2 — Zorunlu uygulama kapısı
Her geliştirme fazında `AGENTS.md` içindeki v2 kalite kurallarını uygula. Tek faz, hedefli keşif, lisans ve güvenlik kapısı, mevcut ortak kodun yeniden kullanımı, minimal diff, hedefli test ve kısa Türkçe ilerleme zorunludur. Mevcut iş kurallarını ve faz sıralamasını koru. Bu dosyaların varlığı, lisansların veya testlerin fiilen doğrulandığı anlamına gelmez.
