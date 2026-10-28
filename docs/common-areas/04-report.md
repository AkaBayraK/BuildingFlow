# Ortak Alanlar FAZ 04 — teknik teslim, kabul bekliyor

26.10.2026 · OA-04 · plan sırası04/08 · [Issue #54](https://github.com/AkaBayraK/BuildingFlow/issues/54). [FAZ04](../../BuildingFlow-OrtakAlanlar/FAZ-04.md), [API](../api/common-areas.md#rezervasyon-motoru-oa-faz04), [FAZ03 raporu](03-report.md).

**Rezervasyon motoru uygulandı:** yönetici 15 dakikalık slotlarla rezervasyon oluşturur, düzenler, gerekçeyle iptal eder. Kapasite, çakışma, kilit, idempotency, version, rol ve kapsam kontrolleri sunucudadır. Diğer kullanıcılar yalnız kişisel bilgi içermeyen saat takvimini (boş, kısmen dolu, dolu) görür.

## Kararlar

- Kullanıcı kararı (26.10.2026) uygulandı: yazma yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER; ACCOUNTANT/OWNER/TENANT/RESIDENT 403. Sakin talep gönderemez.
- Kapasite = örtüşen onaylı rezervasyonların kişi toplamı. Halı saha (14 kişi) için tam rezervasyon alanı kapatır; havuz gibi alanlarda kısmi doluluk gösterilir.
- Saat Europe/Istanbul yerel gün + `HH:mm` (15 dk). Geçmiş başlangıç, 365 günden ileri, çalışma saati dışı, pasif/OPEN_USE alan 409 `..._CLOSED`.
- Eşzamanlılık Property `FOR UPDATE` kilidiyle; rezervasyon alan version'ını da artırır (alan saat/kapasite düzenlemesi ile yarış 409 olur).
- İptal edilen satır silinmez. Gelecek rezervasyonları bozan alan düzenlemesi, rezervasyon geçmişi olan alan/daire silme 409.
- `APPROVAL` alanında yönetici kaydı doğrudan onaylıdır; onay akışı, ücret/ödeme FAZ07, bakım kapatması FAZ05, mobil FAZ06.

## Değişen dosyalar

- Backend: `Domain/CommonAreaReservation.cs`, `Application/CommonAreaReservations.cs`, `Infrastructure/CommonAreaReservationConfiguration.cs`, `Infrastructure/CommonAreaReservationStore.cs`, `CommonAreaStore.cs` (gelecek rezervasyon koruması, daire bağlamı ortak sorgu), `HierarchyStore.cs` (daire silme koruması), `BusinessDate.cs`, `ErrorCatalog.cs` (+3 kod), `CommonAreaEndpoints.cs`, `Program.cs`, `DependencyInjection.cs`; migration `20261026165346_CommonAreaReservations` + Designer + snapshot.
- Web: `features/common-areas/CommonAreaSchedule.tsx` (tarih seçimi, son güncelleme, ikonlu boş/kısmi/dolu aralıklar; yöneticide liste, oluştur, gerekçeli iptal), `model.ts` (katı DTO doğrulayıcıları, kişisel alan reddi), `type-style.ts`, `CommonAreaPanel.tsx`, `CommonAreasWorkspace.tsx`, TR/EN sözlük, `error-codes.ts`, test fixture.
- Testler: `IntegrationTests/CommonAreaReservationTests.cs` (6 test, 8 koşu), `CommonAreaMigrationTests` (+1), `UnitTests/CommonAreaReservationTests.cs` (2), `tests/common-areas.test.mjs` (+2).

## Bu tur çalıştırılan doğrulamalar

| Kontrol | Sonuç |
|---|---|
| `dotnet build backend/BuildingFlow.sln` | **GEÇTİ, 0 hata/0 uyarı** |
| İzole PostgreSQL'de `dotnet test` (CommonArea, Persistence, MigrationTests, Authentication, Maintenance, Hierarchy, PropertyTypeChange, SchemaContract, BaseEntity, ErrorContract, DirectUnit filtresi) | **157/157 entegrasyon + 10/10 unit, 0 atlanan** |
| Tüm unit `dotnet test backend/tests/BuildingFlow.UnitTests` | **404/404** |
| Web `npm test` | **273/273, 0 atlanan** |
| Web `npm run typecheck` | **GEÇTİ** |
| Web `next build --webpack` | **GEÇTİ** |
| Web varsayılan Turbopack build | **BAŞARISIZ — ortam:** çalışma kopyasında `node_modules` sembolik bağ olduğu için Turbopack `next/package.json` bulamadı. Kod hatası değil; ana çalışma kopyasında tekrar edilmeli. |
| `python3 scripts/check-licenses.py` | **GEÇTİ, 329 paket**; yeni paket yok |
| `git diff --check` | **GEÇTİ** |

Kanıtlanan senaryolar: oluştur/replay/farklı içerik 409/düzenle/eski version 409/gerekçesiz iptal 400/iptal/ikinci iptal 409/iptal edileni düzenleme 409; audit alanları. Kapasite: 10+5 örtüşme FULL, 10+4 kabul, değen aralık kabul; takvim aralıkları ve JSON'da not/daire kimliği yok. Geçersiz slot/tarih/kişi/yabancı daire/uzun not 400; saat dışı/geçmiş/365+ gün/OPEN_USE/pasif alan 409; kısmi yazım yok. Roller 403, anonim 401, yabancı yönetici/alan/rezervasyon 404. **Yarış:** aynı anda iki 10 kişilik rezervasyon → tek 201 + tek 409; aynı UUID → tek kayıt. Alan kapasite/saat/kullanım düzenlemesi ve alan/daire silme koruması; DB check constraint. Migration: yükseltme/tekrar/boş Down/yeniden Up, composite FK ve check reddi, dolu geçmişte Down reddi, model–snapshot eşliği.

## İzole UI kabulü (fixture, gerçek API/auth değil)

- Yönetici: Saat takvimi sekmesinde rezervasyon oluşturma (başarı bildirimi, aralık 4/14 kısmi), kapasite aşımında 409 hata, gerekçe boşken odak alanda kalır ve uyarı, iptal sonrası "İptal edildi" + gerekçe.
- Salt okunur sakin: form ve iptal düğmesi yok, yönetim notu görünmez; 08:00–18:00 boş, 18:00–19:30 dolu, 19:30–22:00 boş.
- 375px: yatay taşma yok. Ekranlar: [salt okunur takvim](screenshots/04-readonly-schedule.png), [yönetici mobil](screenshots/04-manager-bookings-mobile.png).

## Açık kalanlar ve riskler

- Gerçek Keycloak + API ile uçtan uca tarayıcı testi yapılmadı. Rezervasyon düzenleme (PUT) API'de var, web arayüzünde yalnız oluştur/iptal var; tam yönetici takvimi FAZ07.
- Yönetici rezervasyonda daire seçimi API'de destekleniyor, web formunda daire seçici yok (yalnız not).
- Her rezervasyon alan version'ını artırır; alan düzenleme paneli açıkken rezervasyon yapılırsa düzenleme 409 + "güncel kaydı yükle" ile çözülür.
- Migration canlı DB'ye uygulanmadı. Commit/push yok.

## Rollback

Migration uygulanmışsa ve tablo boşsa `dotnet ef database update 20261026131804_CommonAreaPhotos`; rezervasyon geçmişi varsa Down reddeder, ileri düzeltme migration'ı gerekir. Kod değişiklikleri commit edilmediği için dosya düzeyinde geri alınabilir.
