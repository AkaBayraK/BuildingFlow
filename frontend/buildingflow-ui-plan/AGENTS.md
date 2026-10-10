# BuildingFlow agent kuralları

- Mevcut uygulama Next.js + TypeScript frontend ve .NET backend kullanır. Önce gerçek repo ve sürümleri tespit et.
- Kullanıcının mevcut işlevlerini, rotalarını, API sözleşmelerini, kimlik doğrulama/oturum akışını ve yetkilerini koru.
- Gerçek veriyi değiştirme; seed, migration, backend değişikliği ve yeni paket kurulumundan önce gerekçeyi açıkla.
- Görsel referans: koyu lacivert sidebar, beyaz/açık gri içerik, mavi vurgu, yuvarlak kartlar, sade grafikler, Türkçe metinler.
- Ortak bileşenleri tekrar kullan; shadcn/ui, Tailwind CSS, TanStack Table ve Recharts mevcut sürümlerle uyumluysa tercih et.
- Mevcut bağımlılık ve stilleri analiz etmeden yeni tasarım kütüphanesi ekleme.
- Erişilebilirlik, klavye, loading/empty/error durumları, mobil/tablet uyumluluğu ve light/dark temayı gözet.
- Her aşamada yalnızca o aşamanın kapsamını uygula; sonraki aşamaya otomatik geçme.
- Her faz/adım öncesinde ve anlamlı işlem sonrasında konsola [kök AGENTS.md çalışma protokolündeki](../../AGENTS.md) `[BUILDINGFLOW]` biçimiyle amaç, UI fazı/toplam, adım/durum, değişen dosyalar, testler, sonuç ve kalan riskleri Türkçe yaz; UI fazını iş fazıyla karıştırma.
- Kod değişikliği öncesi git status kontrol et; kullanıcı değişikliklerini ezme. Git commit/push yapma, gizli anahtarları yazdırma.
- Testleri mevcut package scripts'e göre çalıştır; başarısızlıkları gizleme.
- UI'da görülen örnek KPI, finans, sakin ve durum verilerini gerçek veri gibi hardcode etme. API yoksa kontrollü placeholder/empty state göster.
- Yeni backend endpoint veya DB şeması uydurma; ihtiyaçları raporla ve onay iste.
