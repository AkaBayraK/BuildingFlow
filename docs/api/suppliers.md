# Tedarikçi sözleşmesi

Tenant merkezli katalog; kişi giriş hesabı değildir, tedarikçi de hesap oluşturmaz. Yalnız aktif tenant ve issuer/subject üyeliği ile SUPER_ADMIN/SITE_ADMIN/MANAGER; ENFORCED yönetim ataması korunur. ACCOUNTANT iletişim/kişi alanlarına erişmez. Alan ilişkileri tenant+Property kapsamını korur.

`GET /api/v2/tenants/{tenantId}/suppliers`: page1..1000000, pageSize1..100, search en fazla200, status DRAFT/ACTIVE/INACTIVE/BLOCKED, sortBy name/createdAt, sortDirection asc/desc. items/page/pageSize/totalCount; pasif geçmiş listelenir. `GET /{supplierId}` sadece aynı tenant.

POST/PUT body: operationId UUID, name1..200, kind COMPANY/INDIVIDUAL, description nullable max2000, version (POST0,PUT>=1), status (varsayılan DRAFT), statusReason nullable max1000; pasif/engelli gerekçesi zorunlu. Bilinmeyen JSON alanı reddedilir. Sonuç record/replayed/appliedVersion. Aynı UUID+normalize içerik güncel record döndürür; eski sürüme geri yazmaz. Farklı içerik veya stale version409. Aynı tenant normalize ad tek; gizli başka kayıt bilgisi hata mesajına eklenmez.

401 anonim,403 rol,404 kapsam/kayıt,400 BF_SUPPLIER_INVALID,409 BF_SUPPLIER_DUPLICATE veya BF_RECORD_CHANGED. Ham hata render edilmez. TCKN/vergi/IBAN temel DTO'da yok. Belge SupplierManagementArea üzerinden Property+StoredDocument uyumlu metadata; yükleme/indirme FAZ04 tamamlanmadan var sayılmaz.

Migration SupplierFoundation eklemeli7tablo; tümü BaseEntity. İşlem kaydı insert-only; dolu Down veri kaybı reddedilir. [Kanıt](../suppliers/01-report.md).

## FAZ03 alan ve kategori

Tenant kökünde GET/POST `supplier-categories` (operationId+name;aynıUUID aynıtrimmedad tekrar),GET `supplier-property-choices`;supplier altında GET/PUT `/{id}/areas`. PUT operationId/version/propertyId/categoryId/serviceName. AktifSupplier ve her alanda MANAGER kararı;tenant→Property kilidi;alanListesiPropertyScopes ile sınırlandırılır. SupplierAreaService iki compositeFK ile alan/hizmet kapsamını korur. EskiUUID güncelSupplier veappliedVersion döndürür. [Kanıt](../suppliers/03-report.md).

## FAZ04 — Özel belgeler ve sözleşmeler

Tedarikçi kökü `/api/v2/tenants/{tenantId}/suppliers/{supplierId}` altında:

| İstek | Sözleşme |
|---|---|
| GET `/documents` | Yetkili alanlarla sınırlı sayfalı belge/revizyon listesi |
| POST `/documents` | Multipart: file, operationId, supplierVersion, propertyId, opsiyonel expiresOn ve previousDocumentId; PDF/JPEG/PNG, max10MiB |
| GET `/documents/{id}/download` | İd SupplierDocument kimliğidir; JWT/kapsam ve SHA256/boyut doğrulaması, attachment/no-store/nosniff |
| PUT `/documents/{id}/remove` | operationId/supplierVersion/version; bağlı geçmiş varsa409; diğerlerinde soft-delete ve UUID replay |
| GET `/contracts` | page/pageSize, opsiyonel renewalDue; alan/hizmet adları ve hesaplanan durum |
| GET `/contracts/{id}` | Yetkili güncel detay |
| POST `/contracts` / PUT `/contracts/{id}` | operationId/supplierVersion/version/propertyId/areaServiceId/title/startDate/endDate/amount/reminderDays/documentId/previousContractId; yalnız taslak değişir |
| PUT `/contracts/{id}/activate` veya `/cancel` | operationId/supplierVersion/version; iptalde reason zorunlu |
| GET `/contracts/{id}/history` | Sayfalı, immutable işlem/revizyon snapshot'ları |

Alan seçimi `supplier-property-choices?supplierId={id}` ile mevcut tedarikçi bağlantılarına daralır. Belgeler SupplierOnly+MANAGEMENT; genel belge endpoint'leri bu kayıtları görmez veya silemez. Eski revizyon ve sözleşmeye bağlı belge korunur; yeni revizyon mevcut sözleşmenin DocumentId'sini yeniden yazmaz. Her mutation tenant→Property kilidi ve parent version ile sıralanır. Sözleşme TRY tutarı iki ondalık string; ödeme veya gider oluşturmaz. Yenileme eski bitişten sonra ve aynı alan/hizmette yeni taslaktır. ReminderDays0–365 ve Europe/Istanbul günü ekran içi uyarıyı belirler; dış bildirim FAZ10'dur.

Yeni kodlar: BF_SUPPLIER_CONTRACT_STATE, BF_SUPPLIER_DOCUMENT_PROTECTED, BF_SUPPLIER_PROPERTY_HISTORY (409). Mevcut dosya400/413/415/503 semantiği korunur. [Kabul kanıtı](../suppliers/04-report.md).

## FAZ05 — Teklif ve karşılaştırma

Kök `/api/v2/tenants/{tenantId}/suppliers/{supplierId}`. Bütün uçlar `CanManagePeople`, aktif tenant/alan üyeliği ve supplier alan bağlantısı kontrolündedir; ACCOUNTANT/sakin erişimi yoktur.

| Uç | Sözleşme |
| --- | --- |
| GET `/quote-candidates?propertyId=&categoryId=&page=&pageSize=` | Aynı alan/kategoride aktif supplier+hizmet+alan seçenekleri. |
| GET `/inquiries`, GET `/inquiries/{id}` | Yalnız davetli supplier ve yetkili alan bağlamı; karşılaştırma talebin tüm davetlerini içerir. |
| POST `/inquiries` | operationId, supplierVersion, propertyId, categoryId, title, description, deadline, areaServiceIds. 1–20 farklı supplier, supplier başına tek hizmet; bağlam supplier'ı da davet edilir. OPEN oluşur, kapsam ve davetler değişmez. |
| PUT `/inquiries/{id}/quotes` | operationId, talep version, invitationId, netAmount, vatRate, currency, validUntil, notes. Bir davette tek teklif; PENDING teklifi yalnız ilk giren aynı issuer+subject düzenler. |
| PUT `/inquiries/{id}/quotes/{quoteId}/approve`, `/reject` | operationId, talep version, zorunlu reason. SITE_ADMIN/SUPER_ADMIN + gerçek alan SITE_ADMIN yetkisi. İlk giren kendi teklifini SUPER_ADMIN olsa da onaylayamaz. Onay/red nihai. |
| PUT `/inquiries/{id}/cancel` | Aynı karar yetkisi/version ve gerekçe; yalnız OPEN→CANCELLED. |
| PUT `/inquiries/{id}/convert` | Aynı karar yetkisi, operationId/version. AWARDED→bir gerçek SupplierWorkOrder DRAFT kaydı; alan/supplier/hizmet/başlık/brüt/para birimi snapshot. |
| GET `/inquiries/{id}/history` | Sayfalı action/snapshot/appliedVersion/tarih; ham issuer/subject UI'ya çıkmaz, audit'te saklanır. |

Liste sınırı100; davet sınırı20. Talep son tarihi ve teklif geçerliliği Europe/Istanbul bugün dahil. Son tarihten sonra yeni/düzenlenmiş teklif ve süresi geçmiş teklifi onaylama reddedilir. Net pozitif ve iki ondalık, kullanıcı girişli KDV oranı0–100/iki ondalık, KDV half-up iki ondalık, brüt≤99.999.999,99. Para alanları invariant string. Hukuki oran kataloğu yoktur. TRY/USD/EUR ayrı gruplarda brüte göre sıralanır; kur dönüşümü veya otomatik seçim yapılmaz. FE önizlemesi BigInt kuruş; TR virgüllü giriş ortak normalizasyondan geçer.

Tek onay talebi AWARDED yapar; diğer PENDING teklifler NOT_SELECTED görünür, yapay red gerekçesi oluşturulmaz. Seçilen aktif supplier/hizmet dönüşümde tekrar doğrulanır. DRAFT dönüşüm ödeme/bildirim başlatmaz; tam iş emri yaşam döngüsü ve arıza bağlantısı FAZ06'dır.

Yazımlar tenant→Property kilidi ve beklenen talep version'ı ile atomiktir. Actor issuer+subject+tenant UUID/fingerprint kayıtları insert-only'dir; tutar ölçeği ve davet sırası normalize edilir. Eski UUID replay güncel talep/iş emrini ve ilk appliedVersion'ı döndürür; farklı içerik409, replay yetkisi tekrar doğrulanır. Alan/kategori composite FK/trigger'ları, tek onay/davet/dönüşüm indeksleri, immutable geçmiş/davet/nihai teklif/iş emri korumaları vardır. Beş tablo BaseEntity sekiz NOT NULL alanını taşır. Migration `20261029180412_SupplierQuotes` canlıya uygulanmadı; dolu Down veri kaybını reddeder.

FE ikonlu Teklifler sekmesi, alan/kategori ve davet seçimleri, ortak RecordList, sağ panel, gerekçeli merkez onayı ve 409 güncel kayıt yükleme kullanır. Hata409 `BF_SUPPLIER_QUOTE_STATE`, doğrulama400, erişim403 ve bulunamayan kapsam404 korunur. Yeni paket eklenmedi.

## FAZ06 — İş emirleri ve Talep/Arıza bağlantısı

Kök `/api/v2/tenants/{tenantId}/suppliers/{supplierId}`; `CanManagePeople` politikası, aktif tenant üyeliği ve Property üyeliği zorunludur. ACCOUNTANT 403, anonim 401, kapsam dışı alan/kaldırılmış üyelik 404 alır.

| Uç | Davranış |
| --- | --- |
| `GET /work-order-sources?propertyId&source=REQUEST\|MAINTENANCE` | Aynı tenant/Property içindeki, açık iş emri olmayan mevcut MobileRequest veya MaintenancePlan kayıtlarını version ile sayfalı listeler. |
| `GET /work-orders?status&page&pageSize` | Yetkili alanlardaki iş emirleri; geçersiz durum 400. |
| `POST /work-orders` | `operationId`, `supplierVersion`, `propertyId`, `areaServiceId`, `source`, `sourceId`, `sourceVersion`, `title`, `description`, `dueAt?`. Kaynak metni anlık görüntü olarak saklanır; bir kaynakta tek açık iş emri, kaynak version uyuşmazlığı 409. |
| `GET/PUT /work-orders/{orderId}` | Yalnız DRAFT kapsam/SLA düzenlenir; UUID replay güncel kaydı, farklı içerik 409 döner. |
| `PUT /work-orders/{orderId}/transition` | `operationId`, `version`, `status`, `reason`. DRAFT→ASSIGNED→ACCEPTED→IN_PROGRESS→COMPLETED→VERIFIED→CLOSED; ASSIGNED'dan REJECTED, kapanış öncesi her adımdan CANCELLED. Gerekçe zorunlu, COMPLETED gerekçesi tamamlanma notudur. VERIFIED/CLOSED yalnız alan yetkili SITE_ADMIN/SUPER_ADMIN (MANAGER 403). ASSIGNED için SLA zorunlu; kaynağı kapanmış iş emri yürütülemez, gerekçeli iptal edilebilir. |
| `GET /work-orders/{orderId}/history` | Immutable revizyonlar (en yeni önce), karar gerekçesi snapshot'ta. |
| `GET/POST /work-orders/{orderId}/photos` | Multipart `file`, `operationId`, `version`, `caption`; yalnız JPEG/PNG imza+MIME+uzantı, en fazla 10 fotoğraf, COMPLETED sonrası yükleme 409. İndirme mevcut supplier belge ucu üzerinden JWT kontrollü `no-store`; genel Property belge ucu 404 verir, fotoğraf kanıtı kaldırılamaz. |

Kabul/başlatma tedarikçi portalı değil, iç yönetici kaydıdır. Talep veya bakım planı durumu otomatik değiştirilmez. SLA UTC saklanır, ekranda Europe/Istanbul gösterilir; `isOverdue` yalnız açık iş emrinde hesaplanır. Yazımlar tenant→Property kilidi, beklenen version ve normalize UUID fingerprint ile atomiktir; eşzamanlı iki geçişte biri 200, diğeri 409 alır. FAZ05 teklif DRAFT iş emirleri `20261030052649_SupplierWorkOrders` ile korunarak taşınır; kaynak/zaman çizgisi/fotoğraf tabloları immutable trigger'larla korunur ve dolu Down veri kaybını reddeder. Migration canlıya uygulanmadı.

## FAZ07 — Periyodik bakım ve planlama

Kök `/api/v2/tenants/{tenantId}/suppliers/{supplierId}`; FAZ06 ile aynı `CanManagePeople`, tenant ve alan üyeliği kuralları. Tatil takvimi tenant düzeyindedir: `/api/v2/tenants/{tenantId}/supplier-holidays`.

| Uç | Davranış |
| --- | --- |
| `GET/POST /supplier-holidays`, `DELETE /supplier-holidays/{id}?version=` | Yıl bazlı sayfalı liste; tarih doğal anahtardır, aynı tarih+ad tekrarı mevcut kaydı döndürür, farklı ad 409. Silme soft-delete, version uyuşmazlığı 409. Tatil değişikliği yalnız yeni planlara uygulanır. |
| `POST /maintenance-schedules/preview` | Yazmadan tarih listesi: asıl tarih, planlanan tarih, kaydırıldı ve çakışma bayrağı. |
| `GET/POST /maintenance-schedules`, `GET /maintenance-schedules/{id}` | `operationId`, `supplierVersion`, `plan{propertyId, areaServiceId, contractId?, title, description?, type, frequency, startDate, endDate, reminderDays, nonWorkingDayRule}`. Sıklık WEEKLY/MONTHLY/QUARTERLY/SEMIANNUAL/YEARLY; dönem en fazla 731 gün ve 120 tarih; ay adımları başlangıçtan hesaplanır (31→28→31 kayma yok). |
| `PUT /maintenance-schedules/{id}/end` | Gerekçeyle sonlandırır; kalan PLANNED tarihler SKIPPED, bağlı Planlama ve Bakımlar kayıtları CANCELLED olur. Açık iş emri varsa 409. |
| `GET /maintenance-schedules/{id}/history` | CREATE/END/COMPLETE/SKIP immutable revizyonları. |
| `GET /maintenance-occurrences?scheduleId&from&to&status&reminderDue` | Takvim ve hatırlatma listesi; aralık en fazla 366 gün. `isOverdue` = PLANNED ve tarih bugünden önce, `isReminderDue` = PLANNED ve tarih−hatırlatma günü ≤ bugün (Europe/Istanbul). |
| `PUT /maintenance-occurrences/{id}/complete` / `skip` | Tamamlama: tarih ≤ bugün, kanıt notu zorunlu, isteğe bağlı `workOrderId` yalnız bu tarihten açılmış VERIFIED/CLOSED iş emri. Açık iş emri varken tamamlama veya atlama 409. Gecikme günü = max(0, tamamlanma − planlanan). |

Hafta sonu her zaman iş günü dışıdır; kural NEXT_WORKING_DAY/PREVIOUS_WORKING_DAY/KEEP. Kaydırma plan dönemi dışına taşarsa veya aynı Property+hizmette aktif planın PLANNED tarihiyle çakışırsa oluşturma `BF_SUPPLIER_MAINTENANCE_CONFLICT` 409 verir. Sözleşmeye bağlı plan için sözleşme ACTIVE ve dönem sözleşme tarihleri içinde olmalı, aksi `BF_SUPPLIER_MAINTENANCE_STATE` 409. Her tarih mevcut MaintenancePlan modülünde PLANNED kayıt oluşturur (sorumlu: tedarikçi adı), böylece FAZ06 iş emri kaynağı olarak seçilebilir. Hatırlatmalar ekrandaki listedir; e-posta/push gönderimi FAZ10 kapsamındadır. Dört yeni BaseEntity tablosu, composite FK, kapsam/kanıt değişmezliği trigger'ları ve dolu Down guard `20261030061301_SupplierMaintenanceSchedules` içindedir; canlıya uygulanmadı.

## FAZ08 — Muhasebe, fatura ve gider entegrasyonu

Kök `/api/v2/tenants/{tenantId}/suppliers/{supplierId}/invoices`, politika `CanManageDues` (SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT). Erişim: aktif tenant üyeliği, tenant PropertyScope, kullanıcının aktif Property üyeliği (SUPER_ADMIN hariç) ve tedarikçinin o alana bağlı olması. Kişi yetkisi gerekmez; yanıt kişisel iletişim verisi içermez.

| Uç | Davranış ve rol |
| --- | --- |
| `GET ?status&page` | Sayfalı liste + durum bazlı brüt toplam özeti ve vadesi geçmiş sayısı (görünür tüm faturalar). |
| `POST` | Herhangi bir finans rolü. `operationId, propertyId, number, issueDate, dueDate, netAmount, vatRate, category, description, workOrderId?, contractId?, correctsInvoiceId?`. TRY, net ve KDV string, KDV yarım-yukarı kuruş. Fatura tarihi ≤ bugün, vade ≥ fatura tarihi. Numara boşluk/nokta/tire/eğik çizgi yok sayılarak aynı tedarikçide iptal edilmemiş faturalar arasında tekil (`BF_SUPPLIER_INVOICE_DUPLICATE`). İş emri COMPLETED/VERIFIED/CLOSED, sözleşme ACTIVE, düzeltilen fatura CANCELLED ve tek düzeltme. |
| `GET /{id}`, `GET /{id}/history` | Fatura ve immutable CREATE/APPROVE/PAY/RECONCILE/CANCEL revizyonları. |
| `PUT /{id}/approve` | Yalnız alan yetkili SITE_ADMIN/SUPER_ADMIN ve faturayı kaydeden kişiden farklı biri. Mevcut Gider modülünde `RequestId = faturaId`, tutar brüt, tarih fatura tarihi, belge referansı fatura numarası olan tek gider oluşturur. |
| `PUT /{id}/pay` | ACCOUNTANT veya SITE_ADMIN/SUPER_ADMIN; `date` (fatura tarihi ≤ tarih ≤ bugün) ve `text` ödeme referansı. Yalnız sistem dışı ödemenin kaydıdır; para hareketi veya ödeme sağlayıcısı yoktur. |
| `PUT /{id}/reconcile` | ACCOUNTANT veya SITE_ADMIN/SUPER_ADMIN; ödeme tarihinden sonra, not zorunlu. Nihai durum. |
| `PUT /{id}/cancel` | SITE_ADMIN/SUPER_ADMIN, gerekçe zorunlu; RECORDED/APPROVED. Onaylıysa bağlı gider aynı gerekçeyle iptal edilir. Ödenmiş fatura iptal edilemez (409); düzeltme yeni fatura ile yapılır. |

Faturaya bağlı gider Gider ekranından ayrıca iptal edilemez (`BF_SUPPLIER_INVOICE_LINKED`). Durum makinesi ve değer değişmezliği DB trigger'ı ile korunur; dolu Down veri kaybını reddeder (`20261030064630_SupplierInvoices`, canlıya uygulanmadı). Muhasebeci için ayrı fatura ekranı yoktur; web arayüzü tedarikçi detayındaki Faturalar sekmesidir (yönetici rolleri), muhasebeci aynı API'ye ve oluşan giderlere Gider modülünden erişir.

## FAZ09 — Mobil ve offline-first

Backend eklemeleri: `GET /api/v2/tenants/{tenantId}/supplier-work-orders?propertyId&status&page&pageSize` (`CanManagePeople`, tenant üyeliği + PropertyScope + alan MANAGER değerlendirmesi; tüm bağlı tedarikçilerin iş emirleri, `{record, supplierName}`) ve `POST /suppliers/{supplierId}/work-orders/{orderId}/notes` (`operationId, version, note≤2000`; kapalı/red/iptal iş emrinde 409; UUID replay; immutable `NOTE` revizyonu, migration `SupplierWorkOrderNotes` yalnız check constraint genişletir, NOTE varken Down reddeder).

Mobil (`mobile/buildingflow-mobile/src/supplier-work`): yalnız yeni yetki modelindeki MANAGEMENT bağlamı ve SUPER_ADMIN/SITE_ADMIN/MANAGER; istek yolları seçili tenant/Property ve yöntemle beyaz listededir. Liste kopyası hesap başına SQLCipher şifreli SQLite'ta (`owner_snapshots`) 1 saat; durum, not ve fotoğraf işlemleri `supplier_work_jobs` kuyruğunda (şema 10), fotoğraf AES ile şifreli özel dosyada. İş emri başına tek aktif işlem; gönderim sırayla, aynı `operationId` ile (sunucu replay'i tekrarı engeller). 200 gönderildi; 409 çakışma, otomatik yazılmaz, yönetici vazgeçer ve güncel listeden yeniden dener; 401 durur ve işlem bekler; 403/404 engellendi; ağ/429/5xx üstel bekleme, 6 denemeden sonra elle yeniden deneme. Hesap değişince şifreli depo anahtarıyla birlikte silinir (mevcut altyapı); yetkisi kalkan bağlamın işlemleri, fotoğrafları ve kopyası temizlenir. VERIFIED/CLOSED geçişleri yalnız SITE_ADMIN/SUPER_ADMIN'e gösterilir; son karar yine sunucudadır.
