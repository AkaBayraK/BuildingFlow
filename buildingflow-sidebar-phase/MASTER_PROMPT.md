# BuildingFlow — Professional Sidebar & Navigation Refactor

## Görev
Mevcut BuildingFlow uygulamasının sidebar ve navigasyon deneyimini, mevcut işlevleri koruyarak profesyonel SaaS standardına yükselt. Bu görev yeni bir uygulama oluşturma görevi DEĞİLDİR. Var olan repository üzerinde kontrollü refactor yap.

## Zorunlu çalışma prensipleri
1. Önce repository yapısını, mevcut faz dosyalarını, AGENTS.md, README ve frontend teknoloji yığınını incele. Mevcut tasarım sistemi, ikonlar, bileşenler, route tanımları, authorization modeli, tenant/yönetim alanı kapsamı ve responsive davranışı tespit et.
2. Mevcut kodu ve git değişikliklerini koru. Kullanıcı değişikliklerini silme, reset/clean/force checkout yapma. Gereksiz paket yükleme ve toplu yeniden yazım yasak.
3. En az token ile yüksek verim: hedefli dosya okuma, kısa analiz, küçük değişiklikler; gereksiz tekrar ve uzun konsol dökümleri yok.
4. TypeScript strict, erişilebilirlik, SOLID, DRY, KISS, merkezi menü şeması, tip güvenliği, test edilebilirlik. Var olan component ve helper'ları yeniden kullan; mükerrer metot oluşturma.
5. Ticari/SaaS/dağıtım kullanımına uygun doğrudan ve transitif bağımlılık lisanslarını kontrol et. Uygunluğu doğrulanamayan yeni bağımlılık ekleme; mümkünse mevcut kütüphanelerle ilerle.
6. Güvenlik: UI menü gizleme yetkilendirme değildir. Backend API, route ve tenant scope kontrolleri mevcut haliyle korunmalı. Rol/izin eşlemesi tahmin edilmemeli; gerçek auth kaynaklarından türetilmeli. Fail-closed yaklaşımı benimse.
7. KVKK: kişi verilerini konsola veya client loglarına yazma. Arama ve yerel sidebar tercihi kişisel veriyi açığa çıkarmasın.
8. Offline/mobile mimariyle uyum: menü kabuğu bağlantı yokken çalışabilmeli; ancak yetki bilgisi olmayan/eskimiş durumlarda hassas ekranları erişilebilir kabul etme. Sunucu doğrulamasını atlama.
9. İlerlemeyi Türkçe ve kısa konsol satırlarıyla bildir: `[FAZ 07.1][Adım 02/08] ...`. Her adımda yapılanlar, değişen dosyalar, test sonucu ve riskleri belirt.
10. Mevcut backend, veritabanı, API sözleşmeleri ve iş kuralları kapsam dışıdır; zorunlu görünürse önce raporla ve onay bekle.

## Hedef UX
- Masaüstü geniş sidebar yaklaşık 260–280 px; daraltılmış ikon modu yaklaşık 68–76 px. Gerçek mevcut layout'a göre ölçüleri uyarlayabilirsin.
- Lacivert zemin, BuildingFlow mavi vurgu, açık metin, ölçülü hover/focus/active durumları; aktif satırda ince sol vurgu.
- Üstte logo/marka, daraltma kontrolü ve arama; altta profil ve bağlam kontrolleri. Profil ve yönetim alanı bilgilerini mevcut uygulama state'inden al.
- Menüler: Genel Bakış, Mülk Yönetimi, Finans, Operasyonlar, İletişim, Sistem Yönetimi. Bunlar öneri gruplardır; gerçek route/izinlere göre uyarlanır.
- Örnek yerleşim: Dashboard; Yönetim Alanları; Daireler/Bağımsız Bölümler; Kişiler; Ortak Alanlar; Araçlar/Otopark; Tedarikçiler; Aidat/Borçlar; Tahsilatlar; Gider/Bütçe; Sayaçlar; Raporlar; Duyurular; Belgeler; Web Sitesi Yönetimi; Talep/Arızalar; SMS; Açık Rıza; Tanımlar; Kullanıcılar; Sistem Ayarları.
- Aynı ada sahip Kullanıcılar öğelerini incele; farklı işlevlerse kapsamı belirten ayrı adlarla göster, aynı route/işlevse tekilleştir. Yetkiyi veya route'u silme.
- Aktif rota, parent açılması, klavye navigasyonu, aria-expanded/current, focus ring, Escape ile mobil drawer kapatma, tooltip yalnızca gerektiğinde.
- Mobilde overlay drawer, backdrop, scroll lock ve route değişiminde kapanma; tablet ve masaüstünde taşma olmamalı.
- Menü arama yalnızca kullanıcının görebildiği öğelerde yapılmalı; Türkçe karakterlerle uyumlu olmalı.
- `prefers-reduced-motion` desteği ve WCAG AA kontrast hedefi.
- Geçerli route'lar değişmeyecek; mevcut URL'ler ve deep link'ler çalışmaya devam edecek.

## Mimari beklenti
Menü şeması için typed `NavigationItem`/`NavigationGroup` modelini kullan; route, icon, label, children ve permission/feature gate alanlarını mevcut kod tabanına göre tasarla. Permission mapping tek kaynakta olsun. `Sidebar`, `SidebarItem`, `SidebarGroup`, `SidebarSearch`, `SidebarFooter`, `MobileNavigation` gibi bileşenlere yalnızca gerçekten gerekirse ayır. Mevcut component library ve design tokenlarını önceliklendir. Client/server boundary ve hydration davranışlarını doğrula.

## Teslim
- Çalışan refactor ve mevcut sayfalara geriye dönük uyumluluk
- `docs/navigation-refactor/ANALYSIS.md`, `IMPLEMENTATION.md`, `TEST_RESULTS.md`, `LICENSE_CHECK.md` (mevcut dokümantasyon düzenine uyarlanabilir)
- Değişen dosyalar, kalan riskler, test komutları, manuel doğrulama listesi
- Her adım tamamlandığında Türkçe kısa özet; durma noktalarına uy.

## Başlangıç komutu
`AGENTS.md`, varsa `MASTER_PROMPT.md` ve faz planlarını incele. Önce yalnızca PHASE_07_1.md içindeki Aşama 01'i uygula. Analiz ve önerilen değişiklik listesini yaz, kod değiştirmeden dur ve onay bekle.
