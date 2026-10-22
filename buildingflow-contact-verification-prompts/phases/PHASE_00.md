# BF-VERIFY FAZ 00 — Kaynak analizi, prompt paketi ve görev sözleşmesi

Önce [MASTER_PROMPT.md](../MASTER_PROMPT.md), [PHASES.md](../PHASES.md) ve ilgili GitHub görevini oku.
Kimlik **BF-VERIFY 00**, listedeki sıra **1/08**, resmi adım sayısı **3**.
Bağımlılık: Kullanıcının bu tur için prompt+görev+analiz kapsamı.
Sonraki faza geçiş: kullanıcının açık başlatma kapsamını esas al.

## Adım 01/03 — Önkoşul ve mevcut sözleşme
- İlgili AGENTS/progress/API/test belgeleri ve git durumunu incele; görevin en son içeriğini oku.
- Bu tur yalnız belge ve görev yönetimi; uygulama kodu, şema, servis, gerçek kullanıcı ve canlı ayarlar değişmez.
- PHASES.md içindeki **P00.A1–A6** kabul maddeleri bağlayıcıdır; bunları küçültme veya çoğaltıp yüzde değiştirme.
- İlgili mevcut kaynaklar (isim/yol değişmişse depoda bul; bulunmayanı varmış gibi kullanma):
  - `backend/src/BuildingFlow.Application/Users.cs`
  - `backend/src/BuildingFlow.Application/MobileOnboarding.cs`
  - `backend/src/BuildingFlow.Infrastructure/KeycloakAdminClient.cs`
  - `backend/src/BuildingFlow.Api/AccountEndpoints.cs`
  - `backend/src/BuildingFlow.Api/MobileProfileEndpoints.cs`
  - `backend/src/BuildingFlow.Infrastructure/SmsDelivery.cs`

## Adım 02/03 — Yetkili faz çıktıları
- PHASES.md'deki bu fazın bütün kabul maddelerini uygula ve her anlamlı işlemde Türkçe sonuç ver.
- D1–D4 kararları gereken fazda kararsızsa bağımsız işi ilerlet; karara bağlı canlı politika/sağlayıcıyı uydurma.
- Yeni paket/SDK ekleme; mevcut kullanıcı/veri/.env ve başka fazın değişikliklerini koru.
- Uygulama kodu, migration, servis veya ayar değiştirme; yalnız prompt/analiz/görev kayıtları.

## Adım 03/03 — Doğrulama ve görev teslimi
- Belge bağlantıları, 8 faz / 8 issue / 8 pano öğesi eşliği, kaynak ve manifest/lock karşılaştırması. Uygulama testleri bu belge fazında çalıştırılmaz.
- [Kabul matrisi](../../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md) ve [teslim şablonu](../TASK_REPORT_TEMPLATE.md) ile gerçek sonuçları kaydet.
- Rapor: `docs/refactor/contact-verification/00-analysis.md`; kabul/test kanıtı TESTING.md, güncel bağlam STATUS/SESSION, gerçek değişiklik CHANGELOG.
- Görevde Yapılanlar bölümünü her kabul için ayrıntılı dosya/işlem/test maddeleriyle doldur. Eksik zorunlu madde varsa açık bırak.
- Bütün maddeler ve zorunlu kanıtlar tamamlanınca issue'yu completed kapat; Project 3 Done yap; ikisini tekrar okuyarak doğrula.
- Uygulama testi, cihaz, SMTP/SMS veya canlı ortam doğrulanmadıysa açıkça yaz; zorunlu olan eksikse task kapanmaz.

