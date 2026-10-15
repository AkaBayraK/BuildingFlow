# BuildingFlow Mobile — Faz Planı ve Çalıştırma Promptları

## Faz kapısı

Her faz ayrı bir oturumda veya ayrı bir açık talimatla başlatılır. Şu şablonda `NN` ve faz adını değiştirin:

> BuildingFlow Mobile talimatlarını oku. Yalnızca **Faz NN — [faz adı]** kapsamını uygula. Bu fazın önceki fazlardan gelen önkoşullarını doğrula; eksikse değişiklik yapmadan raporla. İşe başlamadan Git çalışma ağacını kontrol et ve mevcut kullanıcı değişikliklerini koru. İlerlemeyi Türkçe konsola `[BuildingFlow Mobil][Faz NN][x/y]` biçiminde yaz. Faz dışına çıkma. Kabul ölçütlerini ve ilgili testleri çalıştır; sonuçları Türkçe raporla. Sonraki faza geçmeden dur.

## Faz 01 — Mevcut proje analizi ve mimari karar raporu (salt okunur)

**Kapsam:** Depo yapısı, çalışma ağacı, mevcut BuildingFlow web/backend, kullanılan diller/frameworkler, auth, API sözleşmeleri, tasarım/marka varlıkları, testler ve dokümantasyonu incele. Ev sahibi/kiracı/yönetici akışlarını ve fotoğraf/KVKK gereksinimini gereksinim tablosuna dönüştür. Expo/RN, SQLite ve native şifreleme seçeneklerinin repo sürümleriyle uyumunu doğrulama için raporla; henüz kurulum yapma.

**Karar raporu:** (1) Her cihazda kullanıcıya özel SQLite + gerektiğinde merkezi API senkronizasyonu, (2) merkezden tamamen bağımsız tek cihaz kullanımı, (3) birden çok cihazın merkezsiz ortak veri paylaşımı seçeneklerinin farkı, veri kaynağı, yedekleme, rol yönetimi ve çakışma sonuçları. Kullanıcı kararı olmadan senkronizasyon veya merkezsiz çoklu cihaz varsayma. Öneriyi gerekçelendir ve açık kararları listele.

**Kabul ölçütü:** Türkçe, somut dosya/kanıtlarla rapor; hiçbir dosya değişikliği/oluşumu yok; faz sonunda durup onay bekle.

**Başlatma promptu:** `MASTER_PROMPT.md` içindeki İlk Çalıştırma Promptu.

## Faz 02 — Ürün kapsamı ve mobil UX/UI tasarım sistemi

**Önkoşul:** Faz 01 raporu onaylandı; mobil ürün kapsamı ve marka yönü belirlendi.

**Kapsam:** Ev sahibi, kiracı, yönetici ana ekranları; rol/mülk değişimi; navigasyon ve en sık görev akışları; renk, tipografi, ikon, boşluk, buton, kart, form, liste, onay penceresi; çevrimdışı, boş, yükleniyor, hata, başarı durumları; erişilebilirlik ve küçük/büyük ekran düzenleri. Bina finans ve yönetim ürünü hissi; TraceFlow'dan ayrışmış BuildingFlow marka dili. Depoda verilmemiş görselleri uydurma veya TraceFlow görseliyle ikame etme.

**Kabul ölçütü:** Ekran akışları ve bileşen kararları belgeli; projede varsa görsel prototip; kullanılabilirlik ilkeleri rol akışlarında görünür.

## Faz 03 — Mobil uygulama temeli ve geliştirme deneyimi

**Önkoşul:** Teknoloji seçimi ve UX taslağı onaylandı.

**Kapsam:** Mevcut repoya uygun mobil uygulama iskeleti, TypeScript kuralları, navigasyon iskeleti, tema, hata sınırları, ortam yapılandırması, güvenli yapılandırma örnekleri, lint/typecheck/test/build komutları. Gereksiz monorepo veya paket taşıması yapma.

**Kabul ölçütü:** iOS/Android geliştirme çalıştırma adımları belgeli; CI/yerel doğrulamalar geçer; sırlar repoya eklenmez.

## Faz 04 — Kimlik doğrulama, oturum ve rol bağlamı

**Önkoşul:** Backend auth/API ve rol modeli doğrulandı.

**Kapsam:** Mevcut auth sağlayıcısına uygun OIDC/OAuth akışı, güvenli token depolama, oturum yenileme/sona erme, çıkış, hesap değişimi, ev sahibi/kiracı/yönetici rol ve mülk seçimi. İlk giriş ve yeni cihaz çevrimiçi olmalıdır.

**Kabul ölçütü:** Tokenlar düz SQLite/AsyncStorage içine konmaz; yetkisiz rol verisi gösterilmez; hata/oturum sonu akışları test edilir.

## Faz 05 — SQLite ve offline-first veri temeli

**Önkoşul:** Faz 01 veri sahipliği kararı onaylandı; teknoloji uyumu doğrulandı.

**Kapsam:** Cihaz başına SQLite şeması ve migration yaklaşımı; kullanıcı/tenant ayrımı; ortak alanlar (UUID, created/updated, version gerekiyorsa, deleted_at, sync status); güvenli saklama ve anahtar yaşam döngüsü; indeksleme; veri temizleme; fixture/test verisi. Fotoğraf dosyaları BLOB olarak DB'ye gömülmez.

**Kabul ölçütü:** Uçak modu senaryosunda daha önce eşitlenen veriler okunur; migration ve hesap değiştirme testleri var; şifreleme yaklaşımı gerçek cihazlarda doğrulanır veya destek sınırlaması belgelenir.

## Faz 06 — Senkronizasyon ve çakışma yönetimi (yalnızca seçildiyse)

**Önkoşul:** Faz 01'de online API senkronizasyonu seçildi; API sözleşmeleri, yetki kapsamı ve sunucu idempotency davranışı mevcut.

**Kapsam:** Outbox/inbox veya eşdeğer kuyruk, bağlantı algılama, kontrollü yeniden deneme, idempotency key, tombstone, sürüm/çakışma tespiti, kullanıcıya bekliyor/gönderildi/hata/çatışma durumları, yeniden oturum açma ve iptal davranışı. Yetkisiz veya silinmiş hesap verisini senkronize etme.

**Kabul ölçütü:** Uçak modu, bağlantı kesintisi, çift gönderim, sunucu hatası ve çakışma testleri; tekrar gönderim yinelenen iş kaydı üretmiyor. API yoksa faz blokajını raporla; sahte sync başarısı gösterme.

## Faz 07 — Profil, fotoğraf ve gizlilik tercihleri

**Önkoşul:** Kimlik ve veri saklama kararı onaylı.

**Kapsam:** Profil görüntüleme/düzenleme; fotoğraf seçme, kamera/galeri izinlerini bağlamında isteme, kırpma/önizleme, boyut ve tür kontrolü, değiştirme/silme, başarısız yüklemede güvenli kuyruk. Aydınlatma, açık rıza ve isteğe bağlı izinleri ayrı göster; sürüm/tarih/amaç kaydı gerekiyorsa backend/veri sorumlusu kararıyla tasarla.

**Kabul ölçütü:** Fotoğraf isteğe bağlı; izin reddinde uygulama çalışıyor; özel veri loglanmıyor; kullanıcı fotoğrafı kaldırabiliyor; hukuki metin onaylanmadan üretim metni gibi yazılmıyor.

## Faz 08 — Ev sahibi deneyimi

**Önkoşul:** Temel navigasyon, profil ve veri katmanı hazır.

**Kapsam:** Ev sahibinin yetkisine göre daire/mülk özeti, kiracı/sözleşme özeti, tahakkuk/borç ve geçmişi, duyurular, bakım talepleri. Yetki sınırını API belirler; yerel önbellek erişimi rol bağlamında ayrılır.

**Kabul ölçütü:** Güncellik zamanı ve çevrimdışı durumu görünür; önemli eylemler kısa akışlı; veri yok/hata ekranları tasarlanmış.

## Faz 09 — Kiracı deneyimi

**Önkoşul:** Temel navigasyon, profil ve veri katmanı hazır.

**Kapsam:** Kiracı ana ekranı; kira/aidat durumu, ödeme geçmişi/online ödeme bağlantısı varsa ödeme, duyuru, sözleşme özeti, talep/arıza oluşturma. Ödeme ve güncel borç doğrulaması online gereksinim olarak değerlendirilir; ödeme sonucu yerelde kesinleşmiş gibi gösterilmez.

**Kabul ölçütü:** Kiracı yalnızca kendi yetkili daire/verisini görür; ödeme tekrarı/sonucu güvenli; offline'da yalnızca önbellek tarihiyle gösterilir.

## Faz 10 — Yönetici deneyimi

**Önkoşul:** Yönetici yetkileri ve backend sınırları belgeli.

**Kapsam:** Yönetici ana ekranı, bina/blok/daire görünümü, sakinler, aidat/duyuru/talep özetleri ve rol izinlerine uygun hızlı işlemler. Yönetim kapsamı mobil ekrana sığmıyorsa kritik akışlarla sınırla.

**Kabul ölçütü:** Rol/yetki kontrolleri, büyük listelerde performans, boş/hata/offline durumları; mobil ekranda güvenli ve anlaşılır onaylar.

## Faz 11 — Aidat, borç, ödeme ve işlem geçmişi

**Önkoşul:** Backend ödeme/tahakkuk sözleşmesi ve sağlayıcı akışı doğrulanmış.

**Kapsam:** Bakiye ve son güncelleme zamanı, dönem detayları, geçmiş, varsa güvenli ödeme yönlendirmesi. Kart verisi uygulama DB'sine/loglarına yazılmaz. Offline iken ödeme başlatılmaz; bakiye eski olabilir uyarısı gösterilir.

**Kabul ölçütü:** Para formatı/yuvarlama ve başarısız/tekrar ödeme durumları testli; idempotency ve sunucu teyidi olmadan ödeme tamamlandı gösterilmez.

## Faz 12 — Talep/arıza, ekler ve offline outbox

**Önkoşul:** Offline veri temeli; sync seçildiyse Faz 06.

**Kapsam:** Talep formu, kategori/öncelik, açıklama, fotoğraf eki, taslak ve yerel kuyruk; çevrimdışı kaydedildi/bekliyor/gönderildi durumları; yeniden deneme/silme. Konum yalnızca ürün gereksinimi ve kullanıcı izni varsa.

**Kabul ölçütü:** Uçak modunda taslak ve fotoğraf kaybolmaz; bağlantı gelince yalnızca onaylı sync kapsamındaysa gönderilir; başarısızlık kullanıcıya anlaşılır.

## Faz 13 — Duyurular ve bildirimler

**Önkoşul:** Bildirim sağlayıcısı, izin ve backend olayları net.

**Kapsam:** Uygulama içi bildirim/duyuru listesi, okundu durumu, tercih ve push izni için bağlamsal istem. Bildirim metninde hassas finansal/kişisel veri gösterme; deep link rol ve oturum kontrolünden geçer.

**Kabul ölçütü:** Bildirim izni reddedilse de uygulama içi akış çalışır; çevrimdışı önbellek zamanı gösterilir.

## Faz 14 — Güvenlik, erişilebilirlik, performans ve kullanıcı testi

**Önkoşul:** Önceki özellik fazları tamamlandı.

**Kapsam:** Rol sınırları, hassas yerel veri, loglar, cihaz kaybı/çıkış, izinler, bağımlılık/lisans, ağ hataları, ekran okuyucu, kontrast, metin ölçekleme, düşük seviye cihaz ve büyük veri listeleri. Gerçek kişisel veri kullanmadan senaryolar.

**Kabul ölçütü:** Öncelikli bulgular kapatılmış veya belgeli; test raporu ve bilinen sınırlamalar Türkçe.

## Faz 15 — Android/iOS yayın hazırlığı

**Önkoşul:** Ürün sahibi sürüm, paket kimliği, mağaza hesapları ve gizlilik metinlerini sağlamış; Faz 14 kabul edilmiş.

**Kapsam:** Release konfigürasyonu, imzalama sırlarının güvenli yönetimi, ikon/splash (onaylı BuildingFlow varlıkları), mağaza ekran görüntüsü kontrol listesi, gizlilik izin beyanları, sürümleme, build ve dağıtım yönergesi. Kullanıcı açıkça istemedikçe mağazaya yayınlama veya dış servise yükleme yok.

**Kabul ölçütü:** Android/iOS release build talimatları tekrarlanabilir; sırlar repoda yok; mağaza/gizlilik metni eksikleri listelenmiş.
