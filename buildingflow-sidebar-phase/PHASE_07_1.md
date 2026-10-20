# FAZ 07.1 — Professional Sidebar & Navigation Refactor

## Aşama 01 — Mevcut yapıyı analiz et (ONAY KAPISI)
- Git status, frontend giriş noktası, sidebar, layout, routing, auth/permission, tenant/yönetim alanı seçimi, tema ve responsive bileşenlerini incele.
- Ekrandaki bütün menü öğelerini gerçek route, izin ve görünürlük durumuyla eşleştir.
- Aynı adlı Kullanıcılar menülerinin nedenini tespit et. Pasif/gri menülerin neden pasif olduğunu bul; otomatik etkinleştirme.
- Kullanılabilir mevcut ikon ve UI kütüphanelerini ve lisanslarını kontrol et.
- Tasarım ve mimari fark analizi, riskler ve uygulanacak dosyaların listesini ANALYSIS.md'ye yaz.
- **Kod değiştirme. Kullanıcı onayı bekle.**

## Aşama 02 — Navigasyon sözleşmesi ve izin eşlemesi
- Mevcut route'ları değiştirmeden tipli menü şemasını oluştur.
- Yetki/feature flag/tenant bağlamını gerçek kaynaklarla eşleştir.
- Belirsiz izin eşlemelerinde erişimi genişletme; onay iste.
- Unit test: menü görünürlüğü, nested route active state, duplicate detection.

## Aşama 03 — Sidebar tasarım sistemi
- Var olan tema tokenlarıyla navy/blue sidebar stilini uygula.
- Açık/dar mod; ikon, etiket, aktif, hover, focus, disabled durumları.
- Dar mod tooltip ve geniş mod alt menü davranışını geliştir.
- Sidebar tercihini güvenli biçimde yalnızca UI tercihi olarak sakla (mevcut yöntem varsa kullan).

## Aşama 04 — Gruplama ve menü arama
- Genel Bakış / Mülk Yönetimi / Finans / Operasyonlar / İletişim / Sistem Yönetimi gruplarını gerçek bilgi mimarisine göre uygula.
- Mevcut bütün route'ları erişilebilir tut.
- Yetkili öğelerde Türkçe karakter destekli arama ve boş sonuç durumu ekle.

## Aşama 05 — Mobil ve erişilebilirlik
- Mobil drawer, backdrop, Escape, focus yönetimi, scroll lock ve route değişiminde kapanma.
- Klavye navigasyonu, aria-current, aria-expanded, focus görünürlüğü, reduced motion.
- 320px, 375px, 768px, 1024px ve geniş masaüstü kırılımlarını doğrula.

## Aşama 06 — Profil ve yönetim alanı bağlamı
- Footer'a mevcut oturum bilgisinden profil/rol ve uygun yönetim alanı seçicisini bağla.
- Yeni yetki veya hesap değiştirme mantığı icat etme.
- Çoklu rol ve birden fazla yönetim alanı senaryolarında mevcut oturum/izin modelini koru.

## Aşama 07 — Test ve regresyon
- Mevcut lint, typecheck, unit/integration ve build komutlarını tespit ederek çalıştır.
- Dashboard, daireler, kişiler, finans, web sitesi yönetimi ve ayarlar için navigasyon smoke testleri.
- Yetkisiz kullanıcı, farklı tenant, doğrudan URL, browser back/forward, mobile drawer, offline shell senaryoları.
- Test çalıştırılamayan durumları açıkça raporla; başarı iddiasında bulunma.

## Aşama 08 — Dokümantasyon ve teslim
- IMPLEMENTATION.md, TEST_RESULTS.md, LICENSE_CHECK.md oluştur/güncelle.
- Değişen dosyaları ve önemli tasarım kararlarını özetle.
- Yeni bağımlılık varsa doğrudan ve transitif lisans değerlendirmesini yaz.
- Geri alma planı ve manuel QA kontrol listesi ver.
- Son aşamada kullanıcıya Türkçe tamamlanma özeti sun.

## Yürütme kuralı
Her aşamaya geçmeden önce kullanıcı onayı al; kullanıcı birden fazla aşamaya açıkça izin verirse yalnızca izin verilen aşamaları uygula. Her adımda `[FAZ 07.1][Adım XX/08]` konsol bildirimi yap. Hiçbir zaman git reset/clean, destructive migration veya sessiz yetki genişletmesi yapma.
