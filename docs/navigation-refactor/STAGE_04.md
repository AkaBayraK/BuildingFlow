# SIDEBAR FAZ07.1 — Aşama04 teslimi

20.10.2026. Yetkili kapsam yalnız buildingflow-sidebar-phase/PHASE_07_1.md Aşama04/08. Aşama05 başlamadı; önceki staged/unstaged değişiklikler korundu, Git indeksine işlem yapılmadı.

## Uygulanan davranış

**Mevcut görünür menüler altı gruba ayrıldı; boş gruplar gizlenir.** Şema mevcut rol/bayrak/property koşullarını korur; arama aynı görünür modelden beslenir. Backend yetkisi/aktif üyelik son otoritedir.

| Grup | Mevcut öğeler |
|---|---|
| Genel Bakış | Dashboard |
| Mülk Yönetimi | Yönetim alanları, alan genel bilgileri, uygun tipte bina/blok, daire/bölüm, yetkili kişiler, planlı ortak alan/otopark |
| Finans | Aidat/borçlar, tahsilatlar, gider/bütçe, raporlar |
| Operasyonlar | Yetkili talep/arıza, sayaçlar, planlı tedarikçiler |
| İletişim | Duyurular, belgeler, yetkili web sitesi ve SMS |
| Sistem Yönetimi | Yetkili Kullanıcılar/Açık Rıza, planlı Tanımlar/Sistem Ayarları |

İkinci, routesuz ve pasif Kullanıcılar yer tutucusu kaldırıldı. Çalışan `/users` route'u ve SUPER_ADMIN/SITE_ADMIN + sessionContexts koşulu aynıdır; MANAGER'a yeni kullanıcı yönetimi izni verilmedi. Diğer planlı düğmeler routesuz/pasif kalır. Alt kısayollar, home/account ve ContextShell değişmez. **320 rol/bayrak/alan kombinasyonunda çalışan ID+href listeleri tur başlangıç çıktılarıyla birebir eşleşti.** Kaynak snapshot geçici test kanıtıdır, yeni backlog veya ürün route'u değildir.

**Türkçe karakter uyumlu menü araması:** uygulama locale'iyle case dönüşümü, NFD işaret temizliği ve ı→i eşlemesi; I/İ/ı/i ve Ç/Ğ/Ö/Ş/Ü desteklenir. Boşlukla ayrılan kelimelerin tümü etikette herhangi bir sırada aranır. Başlangıç/son boşluk ve Unicode birleşik/ayrık yazımlar desteklenir. Boş sorgu erişilebilir bağlantıları gösterir; eşleşme yoksa yerelleştirilmiş role=status mesajı verir. Route, property UUID veya kişi/daire kayıtları aranmaz. Gizli/planlı öğeler arama kaynağında yoktur. Arama ağ isteği yapmaz, log/depo üretmez; kapanışta metin temizlenir ve autocomplete kapalıdır. Cmd/Ctrl+K ve mevcut Dialog altyapısı korunur. Üst arama metni artık gerçek menü araması olduğunu açıklar.

TR/EN altı grup anahtarı mevcut sözlüklere eklendi. Arama sonuçlarına44px bağlantı, ikon düzeni ve ortak focus/empty stilleri eklendi. Yeni paket/font/ikon/görsel yok; mevcut React/String Unicode API'leri ve projeye ait kod kullanılır.

## Dosyalar

- `frontend/buildingflow-web/src/features/navigation/navigation.ts`: gruplama ve mükerrer placeholder temizliği.
- `src/features/navigation/search.ts` (yeni): saf locale/aksan ve çoklu kelime araması.
- `src/components/WorkspaceShell.tsx`: aynı modelden grup/arama; query temizliği ve doğru erişilebilir grup adları.
- `src/app/globals.css`, `messages/tr.json`, `messages/en.json`: sınırlı sonuç stilleri ve çeviriler.
- `tests/navigation-search.test.mjs` (yeni) ve `tests/navigation.test.mjs`: grup, duplicate, TR/EN ve gizli öğe regresyonları.
- Bu rapor, TESTING/frontend-rules ve mevcut ilerleme/Skill kullanım kayıtları.

## Bu turun kanıtları

**npm test:173/173,0fail/skip; npm run typecheck ve npm run build GEÇTİ.** Dört yeni test: altı grup ve tüm enabled hedeflerin tekilliği; Türkçe case/aksan/Unicode; trim/çoklu kelime/boş sorgu/sonuç/EN; rol ve flag kombinasyonlarında gizli öğelerin aramaya çıkmaması. Önceki169 test bu turun toplamında yeniden çalıştı. Ayrı320kombinasyon karşılaştırması çalışan hedefleri başlangıçla doğruladı; mevcut64kombinasyon duplicate testleri de suite içinde geçti. `git diff --check`, yeni belge bağlantıları, manifest/lock ve iki başlangıç next-env SHA256 koruması kontrol edildi. Başlangıç staged dosyaları indeksinde kaldı; commit/push yapılmadı. Build'in next-env üretimi yalnız başlangıç byte'larına geri döndürüldü. Manifestte lint scripti yoktur.

**Gerçek tarayıcı kabulü DOĞRULANAMADI:** Chrome'daki önceki localhost sekmeleri erişilemiyordu. Yerel frontend3003 ve API5005 kapalı, Keycloak8080 erişilebilirdi; HTTP kontrolü kimlikli sağlık kabulü sayılmadı. Yalnız frontend geçici başlatıldı; yeni test sekmesi /properties'ten mevcut Keycloak Code+S256 giriş ekranına yönlendi. Açık oturum olmadığı için menü/grup/arama gerçek UI'da sınanamadı. Kimlik bilgisi okunmadı/girilmedi; backend/DB/Keycloak servisi veya hesabı değiştirilmedi. Bu tur açılan sekme ve geçici frontend süreci kapatıldı; önceki sekmeler korundu. Auth bypass, test rolü veya sahte runtime kabulü eklenmedi. CUA başlangıç binding'i yeniden seçilerek düzeltildi. Process doğrulamasında sandbox ps engeli izinli tekrar ile aşıldı; kendi geçici süreç kimliği doğrulanıp kapatıldı.

Gerçek rol/tenant kullanıcılarıyla tarayıcı, mobil kırılımlar, offline, backend doğrudan URL ve kapsamlı smoke kabulü bu aşamada ÇALIŞTIRILMADI. Bu doğrulamalar sonraki yetkili aşamalarda kalır. Önceki scopedSMS OIDC allowlist kısıtı değiştirilmedi.

## Koruma ve devam

API/DB/migration, auth/rol/tenant sözleşmesi, mobil kod, .env ve kullanıcı iş verileri değişmez. Yeni bağımlılık veya restore/npmci yok; mevcut LICENSE/NOTICE korunur, lisans envanteri yeniden üretilmedi. Genel ürün roadmapindeki İŞ07.1 hata mesajları farklı görevdir; sidebar adına tiklenmedi. Yeni bağımsız backlog oluşturulmadı; kabul bu rapor/TESTING ve kaynak aşama kimliğine bağlanır.

**Aşama04 uygulama ve otomatik doğrulama tamamlandı: sidebar4/8=%50,00; bu tur1/1=%100,00.** Gerçek UI kabulünün açık oturum eksikliğiyle doğrulanamadığı yukarıda açıkça ayrıdır. Genel iş yol haritası güncel toplamı: BİLGİ MEVCUT DEĞİL. Sonraki: Aşama05 için kullanıcı onayı. Tek agent; mevcut phase/verify/docs uygulandı, kaynak/dependency koruması kontrol edildi; yeni agent/Skill/MCP yok.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codexusage20.10.2026:300dk kullanılan%20/kalan%80 reset20.10.2026 18:32:07;10080dk kullanılan%3/kalan%97 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.
