# Faz 02 — Mobil ürün ve tasarım sistemi

Tarih: 13.10.2026. Faz 01 kullanıcı tarafından onaylandı. Bu belge Faz 02 teslimidir; faz sırası için mevcut `buildingflow-mobile-prompts/PHASES.md` esas alınır.

## Ürün ve marka kararı

iOS/Android, Türkçe varsayılan, ev sahibi/kiracı/yönetici. Bir hesap birden fazla rol ve mülk bağlamı taşıyabilir. Yönetici ilk sürümde özet ve kritik işlemlerle sınırlandırılır. Mevcut BuildingFlow web `globals.css` renkleri (#102342, #0866e8, #f5f7fb) temel alınır. TraceFlow varlığı kullanılmaz. Prototipte metin marka kullanılır; mevcut BuildingFlow logosunun küçük ekran uyarlaması ve mağaza ikonları Faz 15'e bırakılır.

## Navigasyon ve rol akışları

Alt gezinme: Ana sayfa, Finans, Duyurular, Talepler, Profil. Üstte etkin rol + mülk seçicisi; değişimde eski içerik temizlenir, yetkili yeni bağlam yüklenir. Önbelleği olmayan mülk çevrimdışı açılamaz. Çoklu mülk özetleri finans detayına girmeden açıkça etiketlenir.

| Rol | Ana ekran | Sık görev ve hedef akış |
|---|---|---|
| Ev sahibi | Seçili mülkün borç özeti, bağlı daireler, duyurular | Mülk seç → daire → dönem borcu → hareketler; kiracı/sözleşme özeti yalnız yetkili API verisiyle |
| Kiracı | Kendi dairesinin dönem borcu, duyurular, açık talepler | Borcu incele → dönem detayı; Talep oluştur → kategori/açıklama → isteğe bağlı fotoğraf → taslak |
| Yönetici | Yetkili binanın tahakkuk/borç özeti, talep ve duyuru kısayolları | Finans → daire/dönem → hareket; Duyurular → taslak → hedef kitle → önizleme → yayın onayı |

Rol adları sunucu izni değildir. Mevcut API'deki Person/UnitOccupancy OWNER/TENANT ilişkisi giriş hesabı rolüne otomatik dönüştürülmez. Yönetici finans politikaları ev sahibi/kiracıya verilmez. İlgili read sözleşmeleri Faz 04 öncesinde doğrulanır.

## Ekran sözleşmeleri

- Ana sayfa: rol/mülk, güncellik zamanı, dönem borcu, tek öncelikli eylem, duyuru ve talep kısayolları.
- Finans: dönem ve tutar; borç detayı, ödeme/hareket geçmişi. Tutarlar TRY ve iki ondalık, tarih Europe/Istanbul. Online ödeme sağlayıcısı yokken ödeme düğmesi sunulmaz. Offline bakiye tarihli önbellektir.
- Duyurular: başlık/tarih/okundu durumu → detay → yetkili ek. Yönetici yayınında hedef kitle ve son onay gerekir.
- Talepler: durum ve tarih → detay/zaman çizelgesi. Yeni talepte kategori + açıklama zorunlu, fotoğraf isteğe bağlı; yerel taslak ve sunucuya gönderim ayrı durumdur.
- Profil: hesap bilgisi, rol/mülk listesi, fotoğraf ekle/değiştir/kaldır, bildirim tercihleri ve çıkış. Hesap değişiminde önceki hesap verisi gizlenir.
- Onay penceresi: işlem, mülk ve etki açıkça yazılır; Vazgeç varsayılan güvenli seçenek. Taslak silmede açık onay; gönderim sürerken çift dokunma engellenir. Sunucu sonucu belirsizse başarı gösterilmez.

## Tasarım tokenları ve bileşenler

| Alan | Karar |
|---|---|
| Renk | Zemin #f5f7fb; yüzey #ffffff; metin #102342; ikincil #52647a; birincil #0866e8; hata #a12622; uyarı #795100; başarı #146344 |
| Tipografi | Sistem yazı tipi, gövde 16/24, yardımcı 14/20, başlık 24/30, finans değeri 32/40; metin ölçekleme kapatılmaz |
| Boşluk | 4, 8, 12, 16, 24, 32; ekran kenarı 16; kart içi 20 |
| Yüzey | Kart radius 20; alan/buton radius 12; hafif sınır; gölge yalnız katman ayrımı için |
| Buton | En az 48 yüksekliği; bir grupta tek birincil; disabled gerekçesi görünür; ilerleyen işlemin metni değişir |
| Form | Görünür etiket, 16px giriş, zorunluluk metni, alan altında hata; ilk hataya odak; placeholder etiketi yerine geçmez |
| Liste | Başlık + bağlam + durum; satır en az 48; uzun başlık sarılır; yönetim listelerinde sayfalama |
| İkon | Uygulama fazında lisansı doğrulanmış tutarlı ikon seti; dekoratif ikon ekran okuyucudan gizli; eylem ikonunun erişilebilir adı var |
| Diyalog | Yerel platform/modal davranışı; odak içinde tutulur, kapatınca açan kontrole döner; büyük metinde kaydırılabilir |

320–430px telefonlarda tek sütun. 768px ve üzerinde içerik en fazla 560px; masaüstü tabloyu küçültme yok. Alt gezinme beş eşit hedef ve metin etiketi; safe-area hesaba katılır. Klavye form eylemini örtmez. Renk yanında her durumda metin kullanılır; odak halkası ve ekran okuyucu başlıkları korunur. Native Faz 03+ hedefi dokunma alanları 48dp, metin kontrastı en az 4.5:1.

## Durum matrisi

| Durum | Gösterim | Davranış |
|---|---|---|
| Yükleniyor | Alanla eşleşen iskelet ve Yükleniyor metni | Eski rol verisi gösterilmez; hareket azaltma ayarı korunur |
| Boş | Veri türünü açıklayan mesaj | Rol izin veriyorsa ilgili ilk eylem; boş borç hata değildir |
| Hata | Güvenli mesaj + Tekrar dene | Form girdisi korunur; GET tekrar yapılabilir, finans yazımı körlemesine tekrarlanmaz |
| Çevrimdışı | Çevrimdışısın + önbellek zamanı | Mevcut veri okunur, ödeme ve güncel bakiye doğrulaması kapalı |
| Bekleyen | Cihazda kayıtlı, gönderim bekliyor | Yalnız gerçek kuyruk varsa sayı; sync sözleşmesi sonrası gönderim |
| Başarı | Sunucu teyitli kayıt veya yerel taslak şeklinde açık metin | Yerel kayıt sunucu başarısı sayılmaz |
| Çakışma | Kayıt değişmiş; güncel kaydı incele | Otomatik finans birleştirme yok; mevcut taslak korunur |
| Oturum sonu | Devam etmek için giriş yap | Hassas içerik gizlenir; yetkili bağlam yeniden doğrulanır |

## Fotoğraf ve gizlilik

Fotoğraf izinleri yalnız kullanıcı ekleme eyleminde istenir. Kamera ve galeri ayrı seçeneklerdir. Önizle/değiştir/kaldır akışı; ret halinde fotoğrafsız devam. Talep eki ile profil fotoğrafı amaçları ayrı açıklanır. Hukuki metin, saklama süresi, yükleme sınırı ve silme politikası ürün sahibi/backend kararı bekler. Prototip gerçek dosya veya izin istemez.

## Prototip ve kapsam sınırı

`design/phase02/index.html`: bağımlılıksız, yapay örnek verili etkileşimli tasarım prototipi. Rol/mülk değişimi, beş gezinme hedefi, durum seçimi, finans detayı, talep taslağı ve silme onayı incelenebilir. Taslak yalnız sayfa belleğinde tutulur ve yenilemede silinir. Kalıcılık, gerçek auth/API, native fotoğraf/izin, ödeme ve sync uygulanmaz; bunlar ilgili sonraki fazlardadır.

Faz 03 önkoşulu: bu UX taslağının ve Expo/RN/TypeScript yönünün onayı. Faz 04 öncesi mobil OIDC istemci/redirect ve rol izin sözleşmeleri; Faz 05/06 öncesi saklama/şifreleme/yedek/idempotency kararları. Merkezi API ile offline-first yönü Faz 01 onayının kapsamı olarak kabul edilmiştir; endpoint desteği varsayılmaz.

## Doğrulama ve teslim

- `node --check /tmp/buildingflow-phase02.js`: prototip JavaScript sözdizimi başarılı.
- Codex tarayıcısı: talep kategori/açıklama girişi, yerel taslak sonucu, silme diyaloğu ve Vazgeç ile taslağın korunması doğrulandı.
- Kiracı/yönetici/ev sahibi değişimi, mülk seçimi, çevrimdışı uyarı ve Hata → Tekrar dene → Güncel geçişi doğrulandı. Mülk değişimi sırasında bulunan sabit daire özeti seçili mülkü kullanacak şekilde düzeltildi.
- 320, 390 ve 412px görünümde document scrollWidth = viewport width; yatay sayfa taşması yok. 390 ve 412px görsel kontrol yapıldı. Bu tarayıcı kontrolüdür; native iOS/Android veya ekran okuyucu kabulü değildir.
- Prototip ekran kanıtı: `design/phase02/preview.png`. Sayfa taslakları rol/mülk değişiminde sıfırlar; üretimde taslakları bağlama göre koruma kararı Faz 05/12'de uygulanmalı.
- Bu klasör Git deposu değil; `git status --short` sonucu repository bulunamadı. Commit/push yapılmadı. Yeni paket, backend değişikliği ve gerçek kullanıcı verisi yok. Uygulama build/typecheck/native testleri henüz mobil uygulama bulunmadığı için çalıştırılmadı.

Faz 02: kapsam/marka, belge/prototip ve prototip kontrolü tamamlandı (3/3). Faz 03 başlatılmadı.
