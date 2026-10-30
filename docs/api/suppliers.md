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
