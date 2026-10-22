# Aşamalı uygulama planı

01. Repo keşfi: frontend/backend, rotalar, auth, i18n, CSS, API, mevcut CRUD, test ve risk raporu. Kod değiştirme.
02. Design tokens: renk, tipografi, radius, shadow, light/dark, icon standardı; mevcut CSS'e uyumlu.
03. Shell: sidebar, header, breadcrumbs, global arama, dil, profil, responsive, aktif menü ve yetki kontrolleri.
04. Genel Bakış: KPI, doluluk, gelir-gider, duyuru ve son işlemler; mevcut API verileri.
05. Yönetim Alanları: site/apartman kartları, arama/filtre ve tablo.
06. Daireler/Bağımsız Bölümler ve Sakinler: ortak tablo, CRUD, drawer, filtreler.
07. Aidat ve Muhasebe: özetler, grafikler, tablolar, finansal tutarlılık.
08. Talep ve Arızalar: durum, öncelik, sorumlu, zaman çizelgesi.
09. Raporlar: katalog, filtre, var olan Excel/PDF servisleri, hata yönetimi.
10. Sistem Ayarları: tema, dil, kullanıcı/rol, parametreler; kritik işlemler için doğrulama.
11. Diğerleri: Site/Bloklar, Ortak Alanlar, Otopark, Tedarikçiler, Duyurular, Tanımlar.
12. Son kontrol: lint, typecheck, build, test, responsive, accessibility, regresyon ve dokümantasyon.

Her aşama için kabul kriterleri: mevcut fonksiyonlar korunur; yeni UI tutarlı ve responsive olur; loading/error/empty durumları vardır; değişen dosyalar listelenir; test sonuçları raporlanır; kullanıcı onayı olmadan sıradaki aşamaya geçilmez.

## UI 06 teslim kaydı — 2026-10-09

- [x] **Mevcut API kapsamında Daireler ve Kişiler listeleri, mobil kartlar, CRUD/detay sağ paneli ve desteklenen arama/sıralama/sayfalama yenilendi.**
- [x] **Gerçek kişi–daire ilişkisi, üç rol, tarih/iptal/odak, TR/EN ve 390 px mobil kabulü doğrulandı.**
- [x] **23 frontend testi, typecheck/build ve 312 sürüm lisans kontrolü geçti; kullanıcı kayıtları/backend/DB sözleşmesi değiştirilmedi.**

Kanıt ve sınırlamalar: [TESTING.md](../../docs/TESTING.md), [UI06 uygulama raporu](../../docs/analysis/UI_STAGE06_REPORT.md). Bu görsel aşama iş yol haritasındaki 06 ödeme/tahsilat değildir. Önceki UI 02–05 başlıkları bu teslimle tamamlanmış sayılmaz; UI 07 için yeni kullanıcı onayı gerekir.

22.10.2026 BAKIM: ortak üst/alt menü oval köşeleri ve responsive boşlukları doğrulandı; UI03/Shell sunum kanıtı, yeni faz onayı/tiki değildir. [Kanıt](../../docs/navigation-refactor/ROUNDED_MENUS.md).
