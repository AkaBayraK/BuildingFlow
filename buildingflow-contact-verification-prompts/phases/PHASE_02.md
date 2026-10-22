# BF-VERIFY FAZ 02 — Keycloak e-posta doğrulaması ve hesap açılışı

Önce [MASTER_PROMPT.md](../MASTER_PROMPT.md), [PHASES.md](../PHASES.md) ve ilgili GitHub görevini oku.
Kimlik **BF-VERIFY 02**, listedeki sıra **3/08**, resmi adım sayısı **3**.
Bağımlılık: BF-VERIFY 01 görevinin zorunlu kapsam ve kanıtları eksiksiz tamamlanmış olmalı.
Sonraki faza geçiş: kullanıcının açık başlatma kapsamını esas al.

## Adım 01/03 — Önkoşul ve mevcut sözleşme
- İlgili AGENTS/progress/API/test belgeleri ve git durumunu incele; görevin en son içeriğini oku.
- Yönetici oluşturması ve mobil kayıt aynı açık e-posta sahipliği politikasına uyar. İlk doğrulama Keycloak giriş/required-action akışında; SMTP ve canlı politika ayrı devreye alma kapısındadır.
- PHASES.md içindeki **P02.A1–A8** kabul maddeleri bağlayıcıdır; bunları küçültme veya çoğaltıp yüzde değiştirme.
- İlgili mevcut kaynaklar (isim/yol değişmişse depoda bul; bulunmayanı varmış gibi kullanma):
  - `backend/src/BuildingFlow.Application/Users.cs`
  - `backend/src/BuildingFlow.Application/MobileOnboarding.cs`
  - `backend/src/BuildingFlow.Infrastructure/KeycloakAdminClient.cs`
  - `backend/src/BuildingFlow.Api/Program.cs`
  - `frontend/buildingflow-web/src/auth`
  - `mobile/buildingflow-mobile/src/auth`
  - `deploy/keycloak/realm-buildingflow.json`
  - `scripts/configure-identity.sh`

## Adım 02/03 — Yetkili faz çıktıları
- PHASES.md'deki bu fazın bütün kabul maddelerini uygula ve her anlamlı işlemde Türkçe sonuç ver.
- D1–D4 kararları gereken fazda kararsızsa bağımsız işi ilerlet; karara bağlı canlı politika/sağlayıcıyı uydurma.
- Yeni paket/SDK ekleme; mevcut kullanıcı/veri/.env ve başka fazın değişikliklerini koru.
- Backend ve bu fazın açıkça kapsadığı tüketicileri birlikte ele al; property/kişi sınırını iletişim kanıtıyla genişletme.

## Adım 03/03 — Doğrulama ve görev teslimi
- İzole gerçek Keycloak+yerel test posta kutusu, backend negatif JWT/yetki, yönetici+geçici parola+mobil kayıt kombinasyonları; güvenli web/mobil redirect regresyonu.
- [Kabul matrisi](../../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md) ve [teslim şablonu](../TASK_REPORT_TEMPLATE.md) ile gerçek sonuçları kaydet.
- Rapor: `docs/refactor/contact-verification/02-report.md`; kabul/test kanıtı TESTING.md, güncel bağlam STATUS/SESSION, gerçek değişiklik CHANGELOG.
- Görevde Yapılanlar bölümünü her kabul için ayrıntılı dosya/işlem/test maddeleriyle doldur. Eksik zorunlu madde varsa açık bırak.
- Bütün maddeler ve zorunlu kanıtlar tamamlanınca issue'yu completed kapat; Project 3 Done yap; ikisini tekrar okuyarak doğrula.
- Uygulama testi, cihaz, SMTP/SMS veya canlı ortam doğrulanmadıysa açıkça yaz; zorunlu olan eksikse task kapanmaz.

