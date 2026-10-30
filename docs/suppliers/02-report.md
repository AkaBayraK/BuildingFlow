# TEDARİKÇİ FAZ02 — Liste, form ve detay

28.10.2026. Dal buildingflow_tedarikci; GitHub #92.

`/suppliers`: sadece yönetici menüsü; backend-scoped `/api/v2/supplier-workspaces` seçimi, sunucu arama/sıralama/sayfalama ve ikonlu durum filtreleri. Ortak RecordList/TanStack mobil kart+masaüstü tablo; ManagementPanel oluştur/düzenle/detay. TR/EN, semantic token ve ortak bildirim. UUID aynı normalize içerikte korunur;409 otomatik üzerine yazmaz, güncel kaydı yükler; gerekçeli pasif/engelli geçmişi korur. OIDC dönüş whitelist'i eklendi. Tedarikçi kategori filtresi/hizmet ve alan işlemleri FAZ03.

## Kanıtlar

- İzole PostgreSQL/API:19/19+Unit2/2, skipped0; workspace üyelik/rol/sayfalama dahil. `/tmp/bf-supplier-phase02-api2.log`.
- Web305/305, typecheck ve production build: `/tmp/bf-supplier-ui-test4.log`, `/tmp/bf-supplier-ui-typecheck3.log`, `/tmp/bf-supplier-ui-build2.log`.
- Gerçek React bileşenleri mevcut Next webpack/TypeScript ile geçici `/tmp/bf-supplier-ui` fixture'ında paketlendi; yeni paket yok. Sentetik API verisi; canlı Keycloak/API E2E değildir.
- Tarayıcı: boş form ret ve ad odağı; pasif gerekçe zorunlu ve gerekçe odağı; oluşturma/sayının12→13 artması; arama; detay; ikinci sayfa ve filtrede1'e dönüş;409 form değerini koruma/Kaydet kilidi/güncel sürüm yükle/kaydet.409 çift bildirim düzeltildi, tekrarında yalnız bir uyarı görüldü. Kapanış odağı bağlantılı tetikleyiciye, liste yenilendiğinde Yeni tedarikçi düğmesine döner; Escape kapanışı doğrulandı. ACCOUNTANT ekranı veri/form göstermedi.
-375px EN koyu iframe viewport'ta kart ve panel,768px TR ve1200px TR iframe viewport'ta tablo okundu. İç iframe ölçümü DOM aracı tarafından sunulmadığı için otomatik scrollWidth/taşma metriği iddia edilmez;375px panel ekran görüntüsü `docs/suppliers/evidence/02-375-en-dark-panel.png`. Üst tarayıcı623px olduğundan geniş iframe ekran görüntüsü kırpılır; masaüstü tablo yapısı AX ile doğrulandı.

Yeni bağımlılık/migration yok (FAZ01 migration korunur); yerel DB/runtime/deploy/gerçek OIDC E2E yapılmadı. İlk typecheck ortak bileşen prop adlarıyla başarısızdı, düzeltildi; önceki placeholder beklentileri yeni gerçek menü davranışına güncellendi ve tüm305 test tekrar geçti. Yalnız uygulanan kapsam işaretlenir.
