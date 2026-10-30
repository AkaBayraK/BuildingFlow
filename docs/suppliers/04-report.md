# TEDARİKÇİ FAZ04 — Belgeler ve sözleşmeler

29.10.2026 · Dal: `buildingflow_tedarikci` · GitHub: [#94](https://github.com/AkaBayraK/BuildingFlow/issues/94).

## Uygulanan davranış

Tedarikçi belgeleri aynı tenant, tedarikçi ve yönetim alanı kapsamına bağlıdır. Yükleme PDF/JPEG/PNG imza, MIME, uzantı ve 10 MiB sınırını mevcut ortak okuyucu/validasyonla denetler. Dosyalar mevcut özel nesne deposuna sunucunun UUID anahtarıyla yazılır. Download attachment/no-store/nosniff ile döner; SHA256 ve boyut hem backend hem web indirmesinde doğrulanır.

`StoredDocument.is_supplier_only` mahremiyet sınırıdır: tedarikçi belgeleri MANAGEMENT olur, duyuru eki olamaz ve genel belge liste/indirme/silme yollarından açılamaz. Yeni belge revizyonu eskisini değiştirmez; revizyon zinciri ve son geçerlilik tarihi saklanır. Sözleşme veya revizyon geçmişi olan belge kaldırılamaz. Bağımsız belge kaldırma metadata soft-delete işlemidir; nesne otomatik silinmez. Belirsiz DB commit sonucunda nesne silinmez; bakım gereksinimi yalnız güvenli metinle loglanır.

Sözleşme belirli alan/hizmet bağlantısına aittir. Taslak düzenlenir; etkinleştirilince içerik değişmez. Gerekçeli iptal, değişmez SupplierContractRevision ve UUID/version kontrolüyle kaydedilir. Yenileme eski kaydı değiştirmeyen, aynı alan/hizmette ve eski bitişten sonra başlayan yeni taslaktır. TRY tutarı pozitif ve iki ondalıklıdır; sözleşme tutarı gider, ödeme veya gerçek imza değildir. Europe/Istanbul gününe göre UPCOMING/ACTIVE/EXPIRED durumu ve hatırlatma günüyle yenileme filtresi hesaplanır. Bu fazdaki hatırlatma ekran içi uyarıdır; dış SMS/push teslimatı FAZ10 kapsamındadır.

Alan silme, tedarikçi hizmet/sözleşme geçmişi varsa engellenir. İlişkili veri okuma ve yazmada aktif tenant/hesap üyeliği, yönetim rolü, alan üyeliği ve ENFORCED atama kararları korunur. ACCOUNTANT'a özel belge/kişi yetkisi açılmaz.

## Web

İkonlu Genel Bilgiler, Alanlar/Hizmetler, Belgeler ve Sözleşmeler sekmeleri; ok tuşları/Home/End, mevcut ManagementPanel, merkezde onay pencereleri ve işlem sürerken kapanış kilidi kullanılır. Belge ve sözleşme listeleri gerçek alan/hizmet adlarını gösterir. Dosya revizyonu/indirme/korumalı kaldırma; sözleşme oluşturma, düzenleme, etkinleştirme, iptal, yenileme ve geçmiş akışları TR/EN kaynaklarına bağlıdır. Hatalı formda ilk alana odak, version409 kurtarma ve aynı içerikte UUID koruması vardır. İş verisi localStorage'a yazılmaz; issuer/subject değişiminde çalışma alanı sıfırlanır.

## Migration ve doğrulama

`SupplierDocumentsContracts` eklemelidir. Eski özel SupplierDocument kayıtları revision1 ve is_supplier_only=true olarak yükseltilir. Eski bağlantı kamuya açık veya duyuruya bağlıysa sessiz mahremiyet değişimi yerine migration reddedilir. İki yeni BaseEntity tablosu, compositeFK, özel belge check'leri ve değişmezlik tetikleyicileri eklendi. Dolu Down sözleşme, revizyon veya mahremiyet kaybını reddeder. Mevcut uygulanmış migration değiştirilmedi.

- Son `bash scripts/test.sh --filter 'Supplier|CommunicationApiTests|CommunicationDomainTests|PersistenceTests|ErrorContractTests|QaEndpointInventoryTests|HierarchyApiTests'`: **Integration90/90 + Unit16/16; atlanan0**. Yalnız geçici PostgreSQL/rol ve fixture özel nesne deposu kullanıldı; guard ve temizleme geçti. Kanıt: `/tmp/bf-supplier04-tests5.log`.
- İlk genel hata testlerinde yerel `QaCenter__Enabled` bayrağı, DB kullanmayan test sunucusunu kapattı. Test scriptinde runtime bayrağı temizliği düzeltildi; `.env` değiştirilmedi. Tek alan-silme senaryosunda yanlış MANAGER test rolü 403 verdi; test doğru SUPER_ADMIN rolüne alındı, uygulama yetkisi genişletilmedi. Son koşumların tamamı tekrarlandı.
- Kapsam: özel upload/download/revision, genel endpoint bypass reddi, rol/anonim/foreign scope, uzantı/MIME/imza/boyut, UUID replay/version, soft-delete, sözleşme taslak/edit/aktif/yenileme/iptal, eski belge snapshot'ı, immutable SQL geçmişi, property silme engeli, legacy backfill ve Down guard; tüm tablo audit/model şema sözleşmesi.
- Web **316/316**, typecheck ve production build geçti. Kanıt: `/tmp/bf-supplier04-final-webtests.log`, `/tmp/bf-supplier04-final-typecheck.log`, `/tmp/bf-supplier04-final-webbuild.log`.
- EF `has-pending-model-changes --no-build`: model ve snapshot aynı. Restore sonrası lisans metadata **329 PASS**; yeni paket, font, ikon kütüphanesi veya dış asset yok. `git diff --check` geçti.
- Gerçek React bileşenleri/sentetik API ile tarayıcı: sözleşme taslağı, 12.000,50 TRY, etkinleştirme, yenileme başlangıcının eski bitiş+1 olması, gerekçeli iptal, geçmiş; sentetik PDF upload/download, iki revizyon ve onaylı bağımsız belge kaldırma doğrulandı. Son sürümde alan/hizmet adları, ok tuşlarıyla sekme geçişi ve ilk hatalı alana odak yeniden kontrol edildi. [Sözleşme kanıtı](evidence/04-active-contract-history.png), [belge kanıtı](evidence/04-document-revisions.png). Canlı Keycloak/API tarayıcı E2E değildir.

## Sınırlar ve devam

Yerel uygulama DB'sine migration uygulanmadı; API yeniden başlatılmadı. Mobil, dış bildirim, canlı OIDC E2E ve yayın bu fazda yapılmadı. Diğer çalışmanın aynı dalda oluşturduğu QA commitleri değiştirilmedi; bu tur commit/push yapılmadı. Resmi faz2/2=%100,00; tedarikçi akışı5/13=%38,46. Sonraki FAZ05, mevcut kullanıcı devam yetkisiyle teklif yönetimidir.
