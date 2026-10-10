# Faz 06.1 — Tahakkuk muhatabı, kişi geçmişi ve onaylı taşınma iptali

2026-10-09 kullanıcı analizi/onayı: otomatik iptal yapılmaz, **yönetici açık onayı gerekir**.

## Mevcut durum ve karar

Aidat borcu Unit'e aittir; kişi kayıtları tarihli OWNER/TENANT/RESIDENT ilişkileriyle tutulur. Bugünkü kişiyi geçmiş borca join etmek isim/ilişki değişiminde geçmişi yanlış gösterir. Yeni tahakkuk, dönem ilk günündeki ilişkilerin kimlik/rol/tarihlerini ve kayıt anındaki kişi adını değişmez snapshot olarak saklar. Telefon/e-posta/TC kimlik toplanmaz. Malik ve sakinler ayrı; borç muhatabı UNIT/TENANT/OWNER açık seçilir. TENANT/OWNER için tam bir geçerli kişi gerekir; belirsiz çoklu ilişkide tahmin yapılmaz, daire muhatabı seçilebilir. Çoklu malik/sakin bilgileri snapshot'ta korunur.

Kişi listesi/arama filtresi yalnız borç snapshot'larından, property kapsamlı ve sunucuda sayfalı gelir; güncel Person listesiyle karıştırılmaz. Yönetici rollerinin mevcut kişi yetkisi korunur; ACCOUNTANT kişi adı/kimliği snapshot'ı alamaz veya kişiyle filtreleyemez, daire finansı erişimi devam eder. Kişi geçmişi tahakkukta kayıtlı değilse boş durum açıkça gösterilir.

## Eski veriler

Önceki borçların kişi snapshot'ı yoktur. Yeni migration bunları `parties_recorded=false`, muhatap UNIT ile korur; güncel sakinden sahte geçmiş üretmez. Tutar, audit, version ve ödeme geçmişi değişmez. Eski serbest iptal metinleri korunur; yeni iptaller DB'deki sebep kataloğuna bağlanır. OTHER açıklaması zorunlu; bilinen sebeplerde ek açıklama isteğe bağlıdır. Sebep kodu/adı iptal kaydında da saklanır.

## Taşınma onayı

İlişki bitiş günü dahil geçerlidir. Kiracı için bitiş tarihinden **sonraki dönem başlangıçlı**, o ilişkiye muhatap atanmış, iptal edilmemiş ve **hiç ödeme dağıtımı geçmişi olmayan** borçlar önizlenir. Aynı ay, geçmiş dönem, malik/başka kiracı, daire muhatabı ve legacy belirsiz kayıtlar etkilenmez. Ödeme almış/terslenmiş kayıtlar listeden çıkarılır ve uyarı sayısında görünür; otomatik tersleme/iade yoktur.

Yönetici isterse önizlemedeki açık chargeId/version listesini onaylar. İlişki bitişi ve yalnız bu açık listedeki iptaller tek transaction/property kilidi altında yapılır. Yeni oluşan borçlar onay listesine sessiz eklenmez; eski version/ödeme yarışı tüm işlemi 409 ile reddeder. Onaysız bitiş borca dokunmaz ve kalan inceleme sayısını bildirir. Sebep TENANT_MOVED_OUT + bitiş günü + ilişki kimliği geçmişte tutulur.

## Doğrulama planı

- Domain: snapshot kapsamı/rol/tek muhatap, değişmez ad; katalog/OTHER açıklaması, finans geçmişi iptal engeli.
- Gerçek ayrı PG API: dönem sınırları, çoklu malik/sakin, farklı/boş ilişki; replay ve değişen muhatap, ad/soft-delete sonrası eski isim; borçtan seçim/kişi filtresi/sayfa dışı KPI; accountant redaction/403, 401/sahte rol/üyelik/cross-property.
- Taşınma: önizleme/onaysız bitiş/onaylı sadece seçili geleceği iptal; aynı ay/malik/başka kiracı/ödemeli/tersleme/legacy koruma; stale charge/link ve eşzamanlı ödeme/assessment/end rollback.
- Migration: iki yeni BaseEntity tablosu ve FK/check/index/seed; sekiz zorunlu kolon tüm application model/PG şeması; eski borç ve iptal metni koruma, ilk kurulum/yükseltme/tekrar, veri varsa Down reddi.
- FE: TR/EN tablo/mobil snapshot ve kişi combo/search/pagination, rol gizliliği; tahakkuk muhatabı ve reason select/OTHER; sağ panel bitiş önizleme/açık onay/date değişiminde onay reset/409/form/odak. DTO/negative/locale tests, typecheck/build, lisans ve mevcut veri/hash koruma.

Durum/test kanıtı [TESTING.md](../TESTING.md) ve ayrı 06.1 yol haritasında yalnız doğrulandıktan sonra işaretlenir.
