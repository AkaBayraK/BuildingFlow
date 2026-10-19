# İŞ20 — Dashboard ve menü düzeni

## Talep özeti (kullanıcı, 18.10.2026 00:5x, ekran görüntüleriyle)

"sol menüyü de revize edelim, genel bakış en alta geçsin, Dashboard yerine gelsin, alttaki building flow yazısı kalksın, ordaki yazı üstte arama ve language'in tam ortasında yazsın ve birden fazla da olabilir güzel metinler dönsün her refreshte, 5-10 adet olabilir. Dashboard'da ise background'ında her refreshte değişecek resim ekleyebilirler. Ayrıca Tahsilat kutucuğu, Geciken Borç, Açık Talep, Planlanan Bakım, Yaklaşan Ödeme, 3. resimdeki diğer alanları eklemek istiyorum; DASHBOARD olarak taskları açıp faz faz gidelim. Planlı Bakım için şu an BE'den beslenecek ne tablo ne ekran mevcut ama yine de boş olsun, geliştirme olarak task'a ekleniyor."

Örnek görseller: açık temalı "Merhaba Ahmet Bey" panosu (site seçici, 5 sayı kutusu, aidat durumu, talep halkası, son bildirimler) ve koyu "Yönetim Paneli" (bölüm/dolu/boş/tahsilat/geciken borç/açık talep kutuları, gelir-gider trendi, bağımsız bölüm dağılımı, yaklaşan işlemler).

## Kararlar (varsayılanlar, kullanıcıya bildirildi)

- Dashboard yeni rota `/dashboard`; `/` (Genel bakış: tanıtım, sistem durumu, yol haritası) korunur ve menünün en altına taşınır. Logo ve alt kısa yol Dashboard'a gider. Girişten sonra açılan sayfa değişmedi.
- Alttaki yazı sol menü altındaki "BuildingFlow / Daha düzenli yaşam alanları / v1.0.0" bloğu olarak anlaşıldı; kaldırıldı. Alt kısa yol çubuğunun sağındaki "BuildingFlow" yazısı da kaldırıldı.
- Sloganlar projeye ait 8 TR/EN metin (`ui.shellSlogan1..8`); sayfa her yüklendiğinde biri rastgele seçilir (hidrasyondan sonra, sunucu/istemci uyumsuzluğu yok). 1024 px altında üst çubukta yer olmadığından gizlenir.
- Arka plan "resim" lisans kuralı gereği (yeni görsel/paket yok) projenin çizdiği 6 SVG sahne: sabah, öğle, gün batımı, alacakaranlık, gece, bahar. Her yüklemede biri; koyu sahnede yazı beyaz.
- Grafikler paketsiz SVG. Kategorik renkler dataviz doğrulayıcısıyla açık zeminde kontrol edildi (tüm kontroller geçti): `--chart-1..6`.
- Açık talep sayısı talep yönetimi yetkisiyle aynı: yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER; ACCOUNTANT'a `requests: null` gelir ve kutucuk gösterilmez.
- Planlı bakım: tablo/API/ekran yok; kutucuk "Geliştirme aşamasında" ve yaklaşan işlemlerde boş yer olarak hazır. Modül T20.6 olarak açık görev.

## Faz ve task planı (yol haritası İŞ20, 7 görev)

| Görev | Durum |
|---|---|
| T20.1 Sol menü düzeni | TAMAMLANDI |
| T20.2 Üst çubukta dönen sloganlar | TAMAMLANDI |
| T20.3 Dashboard özet API'si | TAMAMLANDI |
| T20.4 Dashboard sayfası, arka plan ve kutucuklar | TAMAMLANDI |
| T20.5 Grafikler ve yaklaşan işlemler | TAMAMLANDI |
| T20.6 Planlı bakım modülü (tablo, migration, API, ekran, Dashboard beslemesi) | BEKLIYOR (geliştirme görevi) |
| T20.7 Testler ve kullanıcı kabulü | BEKLIYOR |

İŞ20 5/7 = %71.43; iş yol haritası 131/138 = %94.93.

## Yapılanlar — 18.10.2026

- **Menü** (`components/WorkspaceShell.tsx`): ana menü Dashboard ile başlar (`LayoutDashboard`), Genel bakış (`ListChecks`) en altta ayrı çizgiyle; menü araması ve alt kısa yollar Dashboard'u içerir; `sidebar-footer` kaldırıldı.
- **Slogan** (`features/dashboard/slogans.ts`, `.workspace-slogan`): arama kutusu masaüstünde 320 px'e indi, slogan ortada, mavi, taşarsa üç nokta.
- **API** `GET /api/v1/properties/{propertyId}/dashboard` (`Dashboard.cs`, `DashboardStore.cs`, `DashboardEndpoints.cs`): ayrıntı [dashboard.md](../api/dashboard.md).
- **Sayfa** `features/dashboard/DashboardPage.tsx`, `model.ts` (yanıt doğrulama), `scenes.tsx` (arka planlar); alan seçimi `?property=` adresinde tutulur (tarayıcı deposu yok).
- **Kutucuklar:** Bağımsız bölüm, Dolu (%), Boş (%), Tahsilat (bu ay, 6 aylık küçük çubuk), Geciken borç, Açık talep (yeni · işlemde), Yaklaşan ödeme (30 gün), Planlı bakım (boş). Bağlantılı olanlar ilgili modülü açar.
- **Paneller:** Gelir - Gider trendi (6 ay, ipucu, gösterge, ekran okuyucu tablosu, telefon genişliğinde gerçek boyutta çizim), Bağımsız bölüm dağılımı (halka, tür başına sabit renk, sayı ve yüzde), Yaklaşan işlemler (en yakın 4 aidat son ödeme tarihi + Planlı bakım yeri).

## Bilinen sınırlar

- Örnek görseldeki "Son bildirimler" ve "Talep durum halkası" bu turda eklenmedi; açık talep kutucuğu yeni/işlemde ayrımını taşır.
- Gelir tahsilat tarihine, gider gider tarihine göre; tahakkuk gelir sayılmaz (rapor sözleşmesiyle aynı).
- Dashboard tek yönetim alanını gösterir; tüm alanların toplamı yok.
