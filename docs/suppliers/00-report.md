# TEDARİKÇİ FAZ 00 — Keşif ve uygulama sözleşmesi

28.10.2026. Dal: `buildingflow_tedarikci`. Kullanıcının güncel isteği fazların sırayla kodlanmasını ve GitHub görev takibini yetkilendirir; paket içindeki varsayılan yalnız analiz başlangıcı bunun yerine geçmez. Tek agent.

## Mevcut altyapı ve yeniden kullanım

| Alan | Gerçek kaynak | Kullanım |
|---|---|---|
| Tenant / hesap / üyelik | backend/src/BuildingFlow.Domain/CentralPerson.cs | PersonTenant, issuer+subject hesabı ve aktif tenant üyeliği |
| Alan kapsamı | backend/src/BuildingFlow.Domain/PersonUnitRelations.cs | PersonPropertyScope; tenant ile Property bağlantısı |
| Atama yetkisi | backend/src/BuildingFlow.Infrastructure/ManagementAccess.cs | ENFORCED atama kararını koru |
| Talep / arıza | backend/src/BuildingFlow.Application/RequestManagement.cs | İş emri kaynak ilişkisi; mevcut talebi kopyalama |
| Bakım | backend/src/BuildingFlow.Application/Maintenance.cs | Plan ve durumları; sonraki fazda tedarikçi ilişkisi |
| Gider | backend/src/BuildingFlow.Application/Expenses.cs | Değişmez gider/iptal; tedarikçi faturası tahsilat değildir |
| Dosya | backend/src/BuildingFlow.Application/Communication.cs | StoredDocument metadata, özel nesne ve kontrollü indirme |
| Bildirim | backend/src/BuildingFlow.Infrastructure/ParkingStore.Notifications.cs | Mevcut olay/duyuru yaklaşımı; yeni sağlayıcı yok |
| Rapor | backend/src/BuildingFlow.Infrastructure/ReportStore.cs | Yetkili filtre ve finans kapsamı |
| Web | frontend/buildingflow-web/src/features/maintenance/ | RecordList, ManagementPanel, ortak API ve TR/EN |
| Menü | frontend/buildingflow-web/src/features/navigation/navigation.ts | Tedarikçiler halen planlı; gerçek API/UI hazırken etkinleştir |
| Mobil | mobile/buildingflow-mobile/src/offline/ ve src/sync/ | Hesap/kapsam temizliği, mevcut SQLite ve conflict handling |
| Test | backend/tests/BuildingFlow.IntegrationTests/CentralPeopleTests.cs ve MaintenanceApiTests.cs | Gerçek izole PostgreSQL, rol/üyelik/version/UUID testleri |

## Tasarım ve sözleşme

Depodaki `buildingflow_tedarikci/TASARIM_REFERANSI.png` incelendi; dış Downloads yolu bulunamadı. Görsel konsepttir; Supabase kodu kullanılmaz, firma logoları/fotoğrafları uygulamaya kopyalanmaz.

Liste: başlık + yeni kayıt → arama/kategori/durum → tablo/mobil kart → sayfalama. Detay sağ ManagementPanel: genel bilgiler, alan ilişkileri, hizmetler; sözleşme/teklif/iş emri/fatura/performans sekmeleri yalnız kendi fazında çalışır hale gelir. Oluşturma/düzenleme ortak form, hatada değer koruma, 409 güncel kaydı yükleme. Tür/kategori/durumlar ikonlu. Masaüstü ve dar ekran, TR/EN ve klavye/odak kabulü gerekir.

Önerilen API: `/api/v2/tenants/{tenantId}/suppliers`; GET sayfalı liste, GET detay, POST oluştur, PUT version ile güncelle. Alan bağlama aynı tenant ve aktif Property kapsamını kontrol eder; ilişki geçmişi korunur. UUID normalize fingerprint tekrarında güncel kayıt ve uygulanan version döner. Yanlış tenant/kayıt 404, rol 403, anonim 401; doğrulama 400, yarış/tekrar içerik farkı 409. Tenant görünümü sadece aktif üyelik ve yönetici rolü; alan görünümü ayrıca aktif üyelik/atama gerektirir. ACCOUNTANT kişisel iletişim verisini okuyamaz.

## Migration, lisans ve riskler

Eklemeli Supplier, SupplierCategory, SupplierService, SupplierManagementArea, SupplierContact ve SupplierDocument tabloları; her biri BaseEntity sekiz NOT NULL alanı. Composite FK ile tenant uyumu; StoredDocument için Property uyumu korunur. Supplier operation kaydı UUID replay için ayrıdır. Tenant kilidi → parent/version düzeni. Dolu rollback reddedilir; mevcut migration değiştirilmez. İlk/yükseltme/tekrar ve model/gerçek PostgreSQL şema testleri gerekir. Yerel uygulama DB'sine migration bu analizde uygulanmadı.

Yeni üçüncü taraf bağımlılık, font, ikon veya görsel eklenmez. Mevcut React/Next/.NET/EF/lucide, nesne deposu ve mobil modülleri yeterlidir. PDF/Excel için önce mevcut export/BCL; yeni paket gerektiren çözüm onaysız eklenmez. KVKK minimizasyonu: ilk temel kayıtta TCKN/vergi/IBAN toplanmaz; gerekli sonraki finans sözleşmesi ayrı, maskeli ve yetkilidir. Tenant modeli merkezi kişi altyapısında vardır; yeni tenant sistemi kurulmaz. Eski Property-only akış ile merkezi kapsam karıştırılmaz. AI opsiyoneldir; dış veri aktarımı/servis otomatik açılmaz.

## Görev ve doğrulama

Kanonik sıra/kabul maddeleri: `buildingflow_tedarikci/PHASES.md`, 00–12 toplam13 faz,38 resmi madde. Her faz GitHub issue, her kabul maddesi checkbox; kanıt olmadan işaretleme/kapatma yok. GitHub mükerrer araması `repo:AkaBayraK/BuildingFlow tedarik` sonuç0. FAZ00 uygulama kodu içermez; build/unit/API/E2E bu analizde uygulanamaz ve çalıştırılmadı. Doküman bağlantı/kaynak/sayaç kontrolü teslimde yapılır. Sonraki: FAZ01 veri modeli, migration ve negatif yetki testleri.

GitHub: FAZ00 [#90](https://github.com/AkaBayraK/BuildingFlow/issues/90), FAZ01 [#91](https://github.com/AkaBayraK/BuildingFlow/issues/91), FAZ02 [#92](https://github.com/AkaBayraK/BuildingFlow/issues/92), FAZ03 [#93](https://github.com/AkaBayraK/BuildingFlow/issues/93), FAZ04 [#94](https://github.com/AkaBayraK/BuildingFlow/issues/94), FAZ05 [#95](https://github.com/AkaBayraK/BuildingFlow/issues/95), FAZ06 [#96](https://github.com/AkaBayraK/BuildingFlow/issues/96), FAZ07 [#97](https://github.com/AkaBayraK/BuildingFlow/issues/97), FAZ08 [#98](https://github.com/AkaBayraK/BuildingFlow/issues/98), FAZ09 [#99](https://github.com/AkaBayraK/BuildingFlow/issues/99), FAZ10 [#100](https://github.com/AkaBayraK/BuildingFlow/issues/100), FAZ11 [#101](https://github.com/AkaBayraK/BuildingFlow/issues/101), FAZ12 [#102](https://github.com/AkaBayraK/BuildingFlow/issues/102).
