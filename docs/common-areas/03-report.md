# Ortak Alanlar FAZ 03 — teknik teslim, kabul bekliyor

26.10.2026 · OA-03 · plan sırası03/08 · [Issue #53](https://github.com/AkaBayraK/BuildingFlow/issues/53). [FAZ03](../../BuildingFlow-OrtakAlanlar/FAZ-03.md), [API](../api/common-areas.md), [FAZ02 raporu](02-report.md).

**Web yönetim ekranları, salt okunur daire görünümü ve yetkili galeri uygulandı.** Rezervasyon/saat motoru FAZ04, mobil FAZ06 kapsamındadır; bu faz takvimde yalnız "henüz yok, aktif durum boş saat garantisi değildir" bilgisini gösterir.

## Değişiklikler

- Web: `/properties/{id}/common-areas` (yönetici) ve `/common-areas` (daire bağlamı) route'ları; `features/common-areas/` altında kart/liste, ikonlu tür/durum/görünüm filtreleri, arama, sayfalama, detay paneli (Hakkında/Kurallar/Fotoğraflar/Saat takvimi), oluşturma/düzenleme/kaldırma, UUID replay, 409 güncel kaydı yükle akışı. Menü, portföy, mülk modül seçimi ve güvenli OIDC dönüş yolu güncellendi. TR/EN sözlük ve hata kodu eşleşmesi eklendi.
- Yetki: yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER yazma düğmelerini görür; backend `CanManageHierarchy` + üyelik zaten zorunlu. Diğer roller daire bağlamından yalnız aktif ve görünür alanları okur; kişisel bilgi yoktur.
- Galeri: `common_area_photos` (Property+alan+StoredDocument composite FK, sekiz NOT NULL alan). Mevcut belge yükleme/MinIO kullanılır; JPEG/PNG, 10 MiB, hak beyanı, alan version kontrolü, JWT'li no-store indirme, yönetici bağla/kaldır, sakin yalnız okur. Migration `20261026131804_CommonAreaPhotos`; dolu geçmişte Down reddeder.
- Bu turdaki düzeltmeler: test fixture'ında locale tipi `'tr'|'en'`; panel kapanınca odak "Yeni Ortak Alan"/"Düzenle" düğmesine dönmüyordu (alan adı `autoFocus`u Radix'in önceki odağı yakalamasından önce çalışıyordu) — `onOpenAutoFocus` + yükleme sonrası odak efektiyle düzeltildi; Kurallar sekmesine ayrı `ScrollText` ikonu verildi.
- Yeni paket/manifest/lock değişikliği yok.

## Bu tur çalıştırılan doğrulamalar

| Kontrol | Sonuç |
|---|---|
| `dotnet build backend/BuildingFlow.sln --no-restore -m:1 --disable-build-servers` | **GEÇTİ, 0 hata/0 uyarı** |
| İzole PostgreSQL test DB'de `dotnet test` (CommonArea/Persistence/MigrationTests.IdempotentScript/Authentication/Maintenance/Hierarchy/PropertyTypeChange filtresi) | **102/102 entegrasyon + 8/8 unit, 0 atlanan** |
| Web `npm test` | **271/271, 0 atlanan** |
| Web `npm run typecheck` | **GEÇTİ** |
| Web `next build --webpack` ve varsayılan `npm run build` (Turbopack) | **İkisi de GEÇTİ** (FAZ02'deki Turbopack kırığı artık yok) |
| `python3 scripts/check-licenses.py` | **GEÇTİ, 329 paket** |
| `git diff --check` / `--cached --check` | **GEÇTİ** |

## İzole UI kabulü (fixture, gerçek API/auth değil)

`scripts/common-area-fixture.mjs` ile üretim CSS'i + webpack paketi; 13006 portu önceki bir fixture süreci tarafından tutulduğundan aynı betik geçici kopyayla 13016'da çalıştırıldı. Uygulama içi tarayıcıda:

- **Masaüstü yönetici:** 7 alan kart görünümü; klavyeyle yeni alan → Enter ile kayıt → detaya geçiş ve başarı bildirimi; düzenlemede fixture çakışması → 409 uyarısı, Kaydet kilitli, "Güncel kaydı yükle".
- **Odak:** açılışta alan adı alanı; Esc ile kapanınca odak tetikleyen düğmeye döner (Yeni, Düzenle, Detay, Detay→Düzenle). Görünür tüm kontroller tab ile erişilir, adı var, odak halkası 2px.
- **Dar ekran 375px, EN, koyu tema:** yatay taşma yok (scrollWidth 375), liste görünümü dar ekranda kart olarak akar; görünür metinlerde 4.5:1 altı kontrast bulunmadı.
- **Salt okunur (RESIDENT):** yazma düğmesi yok (Yeni/Düzenle/Kaldır/Kaydet yok), fixture'daki pasif alan listelenmez, detayda yalnız sekmeler + Kapat; galeri boş durum ve saat takvimi bilgilendirmesi.
- Ekran görüntüleri: [yönetici masaüstü](screenshots/03-manager-desktop.png), [liste koyu](screenshots/03-manager-list-dark.png), [mobil EN koyu](screenshots/03-manager-mobile-en-dark.png), [salt okunur](screenshots/03-readonly-desktop.png).

## Çalıştırılmayan / açık kalanlar

- Gerçek Keycloak + API + MinIO ile uçtan uca tarayıcı kabulü ve gerçek fotoğraf yükleme/indirme UI akışı çalıştırılmadı (fixture fotoğraf listesini boş döner; galeri backend'i entegrasyon testleriyle kanıtlı).
- Ekran okuyucu ile manuel test, mobil (FAZ06), rezervasyon (FAZ04) yok.
- Migration canlı DB'ye uygulanmadı; commit/push/deploy yok. Önceki turdan kalan 13006 fixture süreci ve `/tmp/bf-common03-*` test DB ortamı bu turda silinmedi.

## Rollback

Commit edilmemiş değişiklikler; geri almak için ilgili dosyalar staged alandan çıkarılır. Migration uygulanmışsa boş tabloda `dotnet ef database update 20261026125717_CommonAreaDefinitions`; fotoğraf geçmişi varsa Down reddeder, ileri düzeltme migration'ı gerekir.
