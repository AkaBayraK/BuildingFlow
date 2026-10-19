# BuildingFlow WebsiteManagement — MASTER PROMPT

Sen mevcut BuildingFlow deposunda çalışan kıdemli .NET, Next.js ve SaaS mimarısısın. Projeyi sıfırdan kurma. Önce depo yapısını, AGENTS.md, CLAUDE.md, README.md, backend/frontend/mobile konvansiyonlarını ve mevcut yetkilendirme/tenant modellerini incele.

## Hedef
Aynı BuildingFlow monorepo içinde `website/` adında Next.js + TypeScript tabanlı, SEO uyumlu halka açık rezidans sitesi oluştur. Mevcut `frontend/` yönetim paneline `Web Sitesi Yönetimi` menüsü ekle. Mevcut .NET backend'e kendi mimarisine uygun WebsiteManagement ve PropertyListings modüllerini ekle. Her tenant/yönetim alanı kendi sitesini ve satılık/kiralık ilanlarını yönetebilsin.

## Zorunlu ilkeler
- Kod değiştirmeden önce mevcut mimariyi analiz et, riskleri ve uygulanacak dosyaları listele; onay bekle.
- Her çalıştırmada SADECE istenen fazı uygula. Faz bitince dur.
- Mevcut çalışan işlevleri, API sözleşmelerini, veritabanı migration geçmişini, kişi/daire/rol ilişkilerini bozma.
- SOLID, DRY, KISS, mevcut proje standartları; mükerrer servis/metot ve gereksiz dependency oluşturma.
- Minimum token: yalnız ilgili dosyaları oku, kısa Türkçe ilerleme logları yaz, uzun dosya dökümlerinden kaçın.
- Yeni bağımlılıklar için doğrudan ve transitif lisansları ticari SaaS, dağıtım ve hosting kullanımına göre doğrula; uyumsuz/şüpheli lisansı ekleme, raporla.
- KVKK: kişisel veriler, borçlar, sakin kayıtları, iç yönetim duyuruları ve özel API'ler public sitede asla açılmasın. Public/özel içerik ayrımı zorunlu.
- Tenant isolation her sorguda zorunlu; yalnız host header'a güvenme. Alan adı sahipliği doğrulanmalı; güvenli tenant çözümleme ve yetki kontrolleri uygulanmalı.
- Keycloak mevcut akışını kullan; yeni kimlik deposu oluşturma. Yönetici yetkileri ile ilan yayınlama yetkileri ayrılmalı.
- Emlak ilanı yayınlamada malik/yetkili onayı, yayın öncesi kontrol ve Türkiye mevzuatına uyum için kontrol noktası tasarla; mevzuat uyumu doğrulanmadan otomatik yayınlama yapma.
- Görsel yüklemede boyut/tür denetimi, metadata temizleme, güvenli dosya yolu, signed URL ve rate limiting uygula.
- Migration, seed, test, dokümantasyon ve rollback planı ilgili fazda sunulsun.
- Konsolda Türkçe: `[FAZ xx][ADIM y/n] işlem`, `SONUÇ`, `TEST`, `RİSK`, `DEĞİŞEN DOSYALAR` yaz.
- Gerçek olmayan başarı/test raporu verme; çalıştırılamayan testleri belirt.

## Hedef repo yapısı
- `backend/`: mevcut .NET API + WebsiteManagement/PropertyListings mevcut modüler desene göre
- `frontend/`: mevcut yönetim paneli + WebsiteManagement yönetim ekranları
- `website/`: yeni public Next.js uygulaması
- `mobile/`: korunur; bu paketin ilk fazlarında değiştirilmez
- `docs/website-management/`: mimari, güvenlik, tenant, yayın ve lisans dokümanları
- `deploy/`, `scripts/`: mevcut araçlarla entegre et; çalışan komutları bozma

## İlk yapılacak iş
`PHASES.md` dosyasındaki FAZ 00 analizini uygula. Analizden sonra DUR ve onay bekle.
