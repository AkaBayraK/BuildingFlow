# WebsiteManagement — test senaryoları (WEBSITE FAZ 01)

Durum: plan · 18.10.2026. Uygulandıkça `docs/TESTING.md` İŞ21 bölümüne gerçek sonuçlarla taşınır. Entegrasyon testleri ayrı PostgreSQL ve yalnız fixture'ın `buildingflow-test-*` bucket'ı; çalıştırılamayan test "atlandı" olarak raporlanır.

## Birim (Domain/Application) — FAZ 02, 05, 06
- U1 siteKey regex, rezerve kelimeler, uzunluk sınırları.
- U2 Renk biçimi ve kontrast ≥ 4.5:1; geçersiz renk reddi.
- U3 Bölüm `data` şemaları: her tip için geçerli örnek, bilinmeyen alan, HTML etiketi, `javascript:` bağlantı, başka sitenin medya kimliği, 16 KiB aşımı → red.
- U4 Markdown alt kümesi ayrıştırıcı: izinli token'lar, kaçış, iç içe sınır.
- U5 Durum makineleri (site, duyuru, ilan) tüm geçerli/geçersiz geçişler; onaylı ilan düzenlemesinin PENDING_APPROVAL'a dönmesi.
- U6 İlan: fiyat > 0, RENT⇔MONTHLY, net ≤ brüt, oda düzeni regex, en fazla 20 fotoğraf / tek kapak, AUTHORIZED_AGENT için yetki no zorunlu.
- U7 JPEG/PNG temizleyici: EXIF/GPS, XMP, tEXt/iTXt/zTXt/eXIf çıkarılır; bozuk CRC, kesik dosya, boyut/piksel aşımı, polyglot (PNG+HTML), uzantı-imza uyuşmazlığı reddi; çıktı tekrar ayrıştırılabilir.
- U8 Public DTO eşlemesi beyaz liste: snapshot'ta `propertyId`, `unitId`, `version`, audit alanı, kişi alanı yok (alan adı taraması).
- U9 Domain normalize: büyük harf, port, IDN, sonda nokta, `..`, 254+ karakter.

## Entegrasyon (gerçek PostgreSQL) — FAZ 02, 05, 06, 07
- I1 Migration: ilk kurulum, mevcut veriyle yükseltme (mevcut tablolar hash eşit), tekrar uygulama, idempotent SQL, Down guard dolu tabloda durur; model/şema sözleşmesi 8 BaseEntity alanını kapsar.
- I2 Yetki: anonim 401; ACCOUNTANT/RESIDENT 403; başka Property üyesi 404; MANAGER yayın/onay 403; SITE_ADMIN yayın 200; SUPER_ADMIN askıya alma.
- I3 Composite FK: başka Property'nin sayfasına bölüm, medyasına referans, Unit'ine ilan bağlama veritabanı ve API'de red.
- I4 Concurrency: aynı version ile iki yazım → biri 409; yayın sırasında içerik değişimi `expectedSiteVersion` ile 409.
- I5 Replay: aynı requestId+içerik aynı sonuç; farklı içerik 409.
- I6 Yayın: snapshot değişmez; tek aktif yayın (partial unique); geri alma yeni numara üretir; boş site yayın reddi; PRIVACY/COOKIES doldurulmadan ilk yayın reddi.
- I7 Public API: DRAFT/SUSPENDED/pasif Property/silinmiş site aynı 404; yalnız aktif snapshot; ETag/304.
- I8 Public sızıntı: iç Announcement (ALL_MEMBERS dahil), kişi, borç, belge, talep fotoğrafı hiçbir public uçta dönmez; referanssız medya, taslak ilan fotoğrafı, malik onay belgesi public 404.
- I9 Mimari test: public endpoint/store kaynak kodu yalnız izinli entity setlerini kullanır.
- I10 İlan akışı: belge/onay notu olmadan approve 409; red nedeni zorunlu; 90 gün sonra Worker EXPIRED; olay günlüğü değişmez; Unit pasif/silinince ilan public'ten düşer.
- I11 Başvuru: honeypot dolu / 3 sn altı → 202 ama SPAM; rate-limit 429; ad/telefon loglarda yok; 180 gün sonra anonimleştirme; ACCOUNTANT göremez.
- I12 Medya depolama: nesne+metadata; metadata hatasında yetim nesne temizliği yalnız 24 saat sonra; kota aşımı.
- I13 Domain (FAZ 07): PENDING host 404; yanlış TXT doğrulanmaz; aynı host iki siteye bağlanamaz; DISABLED anında 404.

## Website uygulaması (FAZ 03, 04, 07, 08)
- W1 Bilinmeyen/geçersiz siteKey → genel 404, API ayrıntısı sızmaz; hata sayfası stack göstermez.
- W2 SEO: başlık/açıklama/canonical/OG, `lang="tr"`, sitemap ve robots yalnız yayınlı sayfalar; önizleme `noindex`.
- W3 Erişilebilirlik: klavye ile menü/karusel, odak görünürlüğü, alt metin zorunlu, kontrast, `prefers-reduced-motion`.
- W4 Duyarlı tasarım 390/768/1280 px, yatay taşma yok; mockup bölümlerinin sırası.
- W5 Demo bandı `isDemo=true` iken her sayfada.
- W6 Giriş kartı: parola alanı yok; düğmeler web `/login`'e gider; `returnTo` yalnız allowlist.
- W7 Cross-tenant önbellek: iki site ardışık isteklerde birbirinin içeriğini/ETag'ini almaz; host injection (FAZ 07) canonical/sitemap'i değiştirmez.
- W8 CSP/HSTS/nosniff başlıkları; üçüncü taraf istek yok (ağ kaydı).
- W9 Core Web Vitals hedefi (LCP < 2.5 sn, CLS < 0.1) Lighthouse ile ölçülür; ölçülemezse raporlanır.

## Yönetim paneli (FAZ 05, 06)
- A1 Menü görünürlüğü role göre; ACCOUNTANT görmez.
- A2 Sayfa/bölüm düzenleme, sıralama, önizleme, yayın, geri alma; hata kodlarının TR/EN çevirisi.
- A3 Medya yükleme hak beyanı zorunluluğu; kullanılan medya silme 409.
- A4 İlan onay ekranında mevzuat onay kutusu ve not zorunlu.

## Kalite kapıları (her faz)
`dotnet build` 0 hata/0 uyarı, birim + entegrasyon, `npm test`/typecheck/build (frontend ve website), `check-licenses.py`, compose config, `scripts/ci.sh` mümkünse. Yeni paket eklenmediği lock farkıyla gösterilir.
