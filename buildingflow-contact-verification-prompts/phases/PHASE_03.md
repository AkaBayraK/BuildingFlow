# BF-VERIFY FAZ 03 — Telefon OTP, güvenli gönderim ve sağlayıcı entegrasyonu

Önce [MASTER_PROMPT.md](../MASTER_PROMPT.md), [PHASES.md](../PHASES.md) ve ilgili GitHub görevini oku.
Kimlik **BF-VERIFY 03**, listedeki sıra **4/08**, resmi adım sayısı **3**.
Bağımlılık: BF-VERIFY 02 görevinin zorunlu kapsam ve kanıtları eksiksiz tamamlanmış olmalı.
Sonraki faza geçiş: kullanıcının açık başlatma kapsamını esas al.

## Adım 01/03 — Önkoşul ve mevcut sözleşme
- İlgili AGENTS/progress/API/test belgeleri ve git durumunu incele; görevin en son içeriğini oku.
- Kendi hesabının telefonunu doğrulayan ortak backend. Gerçek sağlayıcı yoksa MOCK yalnız testte kullanılır; üretim SMS kabulü eksikken görev kapanmaz.
- PHASES.md içindeki **P03.A1–A10** kabul maddeleri bağlayıcıdır; bunları küçültme veya çoğaltıp yüzde değiştirme.
- İlgili mevcut kaynaklar (isim/yol değişmişse depoda bul; bulunmayanı varmış gibi kullanma):
  - `backend/src/BuildingFlow.Application/Sms.cs`
  - `backend/src/BuildingFlow.Infrastructure/SmsDelivery.cs`
  - `backend/src/BuildingFlow.Infrastructure/SmsPayloadProtector.cs`
  - `backend/src/BuildingFlow.Infrastructure/DependencyInjection.cs`
  - `backend/src/BuildingFlow.Domain/Sms.cs`
  - `docs/licenses.md`

## Adım 02/03 — Yetkili faz çıktıları
- PHASES.md'deki bu fazın bütün kabul maddelerini uygula ve her anlamlı işlemde Türkçe sonuç ver.
- D1–D4 kararları gereken fazda kararsızsa bağımsız işi ilerlet; karara bağlı canlı politika/sağlayıcıyı uydurma.
- Yeni paket/SDK ekleme; mevcut kullanıcı/veri/.env ve başka fazın değişikliklerini koru.
- Backend ve bu fazın açıkça kapsadığı tüketicileri birlikte ele al; property/kişi sınırını iletişim kanıtıyla genişletme.

## Adım 03/03 — Doğrulama ve görev teslimi
- Domain, gerçek izole PostgreSQL yarış/TTL/replay/limit testleri, sağlayıcı sözleşme testleri ve kullanıcı onaylı kontrollü gerçek SMS kabulü. Gerçek kabul atlanırsa task açık kalır.
- [Kabul matrisi](../../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md) ve [teslim şablonu](../TASK_REPORT_TEMPLATE.md) ile gerçek sonuçları kaydet.
- Rapor: `docs/refactor/contact-verification/03-report.md`; kabul/test kanıtı TESTING.md, güncel bağlam STATUS/SESSION, gerçek değişiklik CHANGELOG.
- Görevde Yapılanlar bölümünü her kabul için ayrıntılı dosya/işlem/test maddeleriyle doldur. Eksik zorunlu madde varsa açık bırak.
- Bütün maddeler ve zorunlu kanıtlar tamamlanınca issue'yu completed kapat; Project 3 Done yap; ikisini tekrar okuyarak doğrula.
- Uygulama testi, cihaz, SMTP/SMS veya canlı ortam doğrulanmadıysa açıkça yaz; zorunlu olan eksikse task kapanmaz.

