# BF-VERIFY FAZ 04 — Merkez ve yönetim web ekranları

Önce [MASTER_PROMPT.md](../MASTER_PROMPT.md), [PHASES.md](../PHASES.md) ve ilgili GitHub görevini oku.
Kimlik **BF-VERIFY 04**, listedeki sıra **5/08**, resmi adım sayısı **3**.
Bağımlılık: BF-VERIFY 03 görevinin zorunlu kapsam ve kanıtları eksiksiz tamamlanmış olmalı.
Sonraki faza geçiş: kullanıcının açık başlatma kapsamını esas al.

## Adım 01/03 — Önkoşul ve mevcut sözleşme
- İlgili AGENTS/progress/API/test belgeleri ve git durumunu incele; görevin en son içeriğini oku.
- Mevcut /account ve merkez/yönetim kabukları üzerinden ortak doğrulama UX'i; sunucu yetkisinin yerini UI durumu almaz.
- PHASES.md içindeki **P04.A1–A8** kabul maddeleri bağlayıcıdır; bunları küçültme veya çoğaltıp yüzde değiştirme.
- İlgili mevcut kaynaklar (isim/yol değişmişse depoda bul; bulunmayanı varmış gibi kullanma):
  - `frontend/buildingflow-web/src/features/account/AccountPage.tsx`
  - `frontend/buildingflow-web/src/components/WorkspaceShell.tsx`
  - `frontend/buildingflow-web/src/features/users/UsersWorkspace.tsx`
  - `frontend/buildingflow-web/src/services/error-codes.ts`
  - `frontend/buildingflow-web/messages/tr.json`
  - `frontend/buildingflow-web/messages/en.json`

## Adım 02/03 — Yetkili faz çıktıları
- PHASES.md'deki bu fazın bütün kabul maddelerini uygula ve her anlamlı işlemde Türkçe sonuç ver.
- D1–D4 kararları gereken fazda kararsızsa bağımsız işi ilerlet; karara bağlı canlı politika/sağlayıcıyı uydurma.
- Yeni paket/SDK ekleme; mevcut kullanıcı/veri/.env ve başka fazın değişikliklerini koru.
- Backend ve bu fazın açıkça kapsadığı tüketicileri birlikte ele al; property/kişi sınırını iletişim kanıtıyla genişletme.

## Adım 03/03 — Doğrulama ve görev teslimi
- Mevcut npm test/typecheck/build; gerçek bileşen ve gerçek test API'siyle pozitif/negatif etkileşim, erişilebilirlik/odak, dar ekran ve çapraz cihaz durum yenileme.
- [Kabul matrisi](../../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md) ve [teslim şablonu](../TASK_REPORT_TEMPLATE.md) ile gerçek sonuçları kaydet.
- Rapor: `docs/refactor/contact-verification/04-report.md`; kabul/test kanıtı TESTING.md, güncel bağlam STATUS/SESSION, gerçek değişiklik CHANGELOG.
- Görevde Yapılanlar bölümünü her kabul için ayrıntılı dosya/işlem/test maddeleriyle doldur. Eksik zorunlu madde varsa açık bırak.
- Bütün maddeler ve zorunlu kanıtlar tamamlanınca issue'yu completed kapat; Project 3 Done yap; ikisini tekrar okuyarak doğrula.
- Uygulama testi, cihaz, SMTP/SMS veya canlı ortam doğrulanmadıysa açıkça yaz; zorunlu olan eksikse task kapanmaz.

