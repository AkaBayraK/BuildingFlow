# BF-VERIFY FAZ 07 — Kontrollü geçiş, operasyon ve nihai teslim

Önce [MASTER_PROMPT.md](../MASTER_PROMPT.md), [PHASES.md](../PHASES.md) ve ilgili GitHub görevini oku.
Kimlik **BF-VERIFY 07**, listedeki sıra **8/08**, resmi adım sayısı **3**.
Bağımlılık: BF-VERIFY 06 görevinin zorunlu kapsam ve kanıtları eksiksiz tamamlanmış olmalı.
Sonraki faza geçiş: kullanıcının açık başlatma kapsamını esas al.

## Adım 01/03 — Önkoşul ve mevcut sözleşme
- İlgili AGENTS/progress/API/test belgeleri ve git durumunu incele; görevin en son içeriğini oku.
- Veriyi ve yönetici erişimini koruyan kademeli açılış; canlı işlem yalnız ilgili ortama açıkça verilmiş yetkiyle. Kodun yazılması ile ortamın açılması ayrıdır.
- PHASES.md içindeki **P07.A1–A8** kabul maddeleri bağlayıcıdır; bunları küçültme veya çoğaltıp yüzde değiştirme.
- İlgili mevcut kaynaklar (isim/yol değişmişse depoda bul; bulunmayanı varmış gibi kullanma):
  - `docs/RELEASE.md`
  - `docs/TESTING.md`
  - `docs/progress/STATUS.md`
  - `docs/progress/SESSION.md`
  - `docs/progress/CHANGELOG.md`
  - `docs/api/account.md`
  - `mobile/docs/PHASE_STATUS.md`

## Adım 02/03 — Yetkili faz çıktıları
- PHASES.md'deki bu fazın bütün kabul maddelerini uygula ve her anlamlı işlemde Türkçe sonuç ver.
- D1–D4 kararları gereken fazda kararsızsa bağımsız işi ilerlet; karara bağlı canlı politika/sağlayıcıyı uydurma.
- Yeni paket/SDK ekleme; mevcut kullanıcı/veri/.env ve başka fazın değişikliklerini koru.
- Backend ve bu fazın açıkça kapsadığı tüketicileri birlikte ele al; property/kişi sınırını iletişim kanıtıyla genişletme.

## Adım 03/03 — Doğrulama ve görev teslimi
- Yetkili izole/kararlaştırılmış ortamda rollout ve rollback provası, gerçek SMTP/SMS + web/mobil uçtan uca kabul; eski hesap/kurtarma smoke. Yetki veya gerçek teslim yoksa doğrulanamadı ve açık task.
- [Kabul matrisi](../../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md) ve [teslim şablonu](../TASK_REPORT_TEMPLATE.md) ile gerçek sonuçları kaydet.
- Rapor: `docs/refactor/contact-verification/07-report.md`; kabul/test kanıtı TESTING.md, güncel bağlam STATUS/SESSION, gerçek değişiklik CHANGELOG.
- Görevde Yapılanlar bölümünü her kabul için ayrıntılı dosya/işlem/test maddeleriyle doldur. Eksik zorunlu madde varsa açık bırak.
- Bütün maddeler ve zorunlu kanıtlar tamamlanınca issue'yu completed kapat; Project 3 Done yap; ikisini tekrar okuyarak doğrula.
- Uygulama testi, cihaz, SMTP/SMS veya canlı ortam doğrulanmadıysa açıkça yaz; zorunlu olan eksikse task kapanmaz.

