# PERSON FAZ05 — Yönetim görevlendirmeleri

Görev `ManagementAssignment` ile tenant+Property+Person'a bağlıdır; Unit/Ownership veya kişi rol boolean'ı değildir. Bir kişi farklı alanlarda ve farklı görevlerde çalışabilir. İzinli görevler SITE_ADMIN, MANAGER, ACCOUNTANT; yeni Keycloak rolü oluşturulmaz veya atanmaz.

## API

Kök `/api/v2/tenants/{tenantId}/properties/{propertyId}`. Hem `PersonRefactor:Enabled` hem `PersonRefactor:AssignmentsEnabled` gerekir; varsayılan kapalıdır. Feature bayrağı migration kapısı değildir.

| Yöntem/yol | Davranış |
|---|---|
| GET `/management-assignments` | Yönetici; page1+,pageSize1–100, tarih/id kararlı sıralama |
| GET `/management-assignments/{id}` | Yönetici; aynı tenant+Property kapsamı |
| GET `/management-assignments/{id}/history` | Yönetici; sayfalı değişmez audit, sürüm sırasıyla |
| POST `/management-assignments` | SUPER_ADMIN veya yetkili SITE_ADMIN; görevlendirme |
| POST `/management-assignments/{id}/end` | Aynı yazım yetkisi; tek bitiş tarihi ve gerekçe |
| POST `/management-assignments/{id}/cancel` | Aynı yazım yetkisi; derhal iptal, tarihler korunur |
| GET `/management-access?role=MANAGER` | Yalnız mevcut hesabın güvenli erişim kararı; başka hesap seçilemez |

Yazım DTO'ları ek alanları reddeder:

- Create: `operationId,personId,role,startDate,endDate?,reason`.
- End: `operationId,expectedVersion,endDate,reason`.
- Cancel: `operationId,expectedVersion,reason`.

UUID zorunlu; reason trim sonrası1–500, kontrol karakteri yok; sürüm≥1. Bilinmeyen rol, tarih/girdi/JSON alanı400; erişilemeyen nesne404, anonim401, rol reddi403. Stale version, değişmiş UUID içeriği, çakışma, kapatılmış görevi yeniden kapatma ve yetim yönetim riski409 `BF_RECORD_CHANGED`. Son yönetici devri için kimlik servisi doğrulaması yapılamazsa503 `BF_IDENTITY_UNAVAILABLE`; görev kapatılmaz.

Yanıt `ManagementAssignmentMutation={record,replayed,appliedVersion}`. Record: id,tenantId,propertyId,personId,role,startDate,endDate,status,version,cancelledAt. Status ACTIVE/UPCOMING/ENDED/CANCELLED veya ortak etkinlik bayrağı kapalıysa DISABLED. Kişinin kimlik/iletişim alanları dönmez. Audit yalnız yetkili history uçlarından action,reason,appliedVersion,createdAt,createdBy olarak döner; serbest gerekçe loglanmaz.

## Yetki sınırı

SUPER_ADMIN aktif scope içinde platform istisnasını korur. Diğer aktörlerde exact issuer+subject aktif hesabı, aktif tenant üyeliği, aktif Property üyeliği, aktif tenant/Property/PersonPropertyScope gerekir. MANAGER/SITE_ADMIN okuyabilir; ACCOUNTANT, OWNER, TENANT, RESIDENT görev veya kişi listesi okuyamaz. MANAGER yazamaz/kendine görev atayamaz. SITE_ADMIN yalnız MANAGER/ACCOUNTANT görevi oluşturup kapatabilir; SITE_ADMIN görevine yalnız SUPER_ADMIN müdahale eder.

Atanan kişi aynı tenantta aktif NATURAL olmalıdır. Görev oluşturma kişi–hesap bağı veya realm rolü üretmez. Süreli görev kendi başına kimlik doğrulama/yetki sağlamaz: kullanım anında gerçek JWT rol üst sınırı, aktif üyelikler ve doğrulanmış kişi–hesap bağı birlikte kontrol edilir. LEGAL, başka tenant, pasif/silinmiş kişi, sona ermiş/gelecek hesap bağı veya stale legacy shadow görevi erişime dönüştüremez.

## Tarih, iptal ve audit

DateOnly aralıklar Europe/Istanbul iş günüyle iki uç dahil yorumlanır. Aynı kişi+alan+görev çakışamaz; farklı görevler ve alanlar desteklenir. Bitiş bir kez yazılır. Önceden planlı bitiş yeniden yazılmaz; mevcut/gelecek görev gerekçeyle iptal edilebilir. Zaten geçmişte bitmiş göreve iptal yazılmaz. İptal, CancelledAt ile anında devreye girer; geçmiş başlangıç/bitiş ve daire rolleri silinmez.

Tenant→Property parent row lock, kilit sonrası yetki ve version kontrolü, transaction içinde görev+audit+makbuz kullanılır. Aynı issuer+subject+tenant+UUID ve aynı normalize içerik tekrarında tek kayıt/audit üretilir; güncel record ve ilk appliedVersion döner. Farklı içerik409. Güncel yetki her replay'de tekrar kontrol edilir; eski makbuz erişim vermez.

SQL trigger sonlu aralık çakışmasını ve kimlik/rol/başlangıç/bitiş/iptal geçmişinin yeniden yazılmasını reddeder. Audit ve makbuz UPDATE/DELETE ile değiştirilemez. Composite FK tenant/Property/assignment eşliğini korur. Üç tablo ortak BaseEntity8NOTNULL alanını taşır.

## Tenant bazlı v2 geçiş modu

`PersonRefactor:ManagementModes:{tenantUuid}` sunucu yapılandırmasıdır; istemci DTO/query parametresi değildir. Varsayılan LEGACY. JSON yapılandırmasında gerçek tenant UUID anahtarına LEGACY, SHADOW veya ENFORCED yazılır; bilinmeyen değer fail-closed hata verir.

- LEGACY: mevcut rol+aktif üyelik sınırı; görev varlığı zorunlu değildir.
- SHADOW: aynı erişim korunur; `/management-access` ayrıca assignmentAllowed karşılaştırmasını verir.
- ENFORCED: yeni v2 görevlendirme uçları ve canonical kişi yönetimi geçerli görev de ister. `claim OR assignment` fallback yoktur. Görev iptali/üyelik veya hesap/kişi/alan iptali sonraki istekte etkili olur; frontend cache'ine güvenilmez.

Kişi yönetimi için SITE_ADMIN/MANAGER görevi ve gerçek rol üst sınırı gerekir; ACCOUNTANT görevi kişi yetkisi açmaz. En az bir yetkili Property'deki görev tenantın merkezi kişi kartlarını okumaya yeter; başka Property'de görev yazma yetkisi vermez. Kendi masked Person kartı salt okunur kalır; self-read görev gerektirmez.

**ENFORCED bu fazda v2 kapsamındadır.** Eski v1 merkez/mobil/finans oturumları otomatik yeni modele geçirilmedi; LEGACY davranışları korunur. Tenant genelinde üretim enforcement'ı tamamlandı denemez. Eski/yeni uçlarda kohort geçişi, mobil cache/outbox ve grant/revoke uyarlaması sonraki onaylı fazlardır. Bayrağı kapatmak, sunucuda açıkça ENFORCED seçilmiş bir tenantın canonical kontrolünü LEGACY'ye düşürmez.

Tanılama yalnız mevcut hesabın mode,legacyAllowed,assignmentAllowed,allowed değerlerini verir; erişilemeyen kapsam UNAVAILABLE ve false değerler alır. Kişi/hesap listesi veya başka tenantın konfigürasyonu dönmez.

## Son yönetici koruması

ENFORCED altında SITE_ADMIN görevi kapatılırken kalan tüm mevcut/gelecek yetki dönemi, diğer uygun SITE_ADMIN dönemlerinin birleşimiyle kesintisiz kapsanmalıdır. İptal bugünden, end ise son günün ertesi gününden itibaren kontrol edilir; gelecek yedek ile bugün arasında boşluk kabul edilmez. Yedek kişi aktif, doğal, güncel; hesabı, kişi bağı, tenant/Property üyeliği aktif ve issuer doğru olmalıdır. Gerekli yedek hesaplar için mevcut Keycloak adapter'ından salt okunur enabled ve gerçek SITE_ADMIN/SUPER_ADMIN rol kontrolü yapılır; aynı hesap bir işlemde tekrar sorgulanmaz. Rolü olmayan görev yedek sayılmaz. Keycloak'ta rol yazılmaz.

Koruma bu modülün görev kapatma transaction'ına aittir. Dışarıdan Keycloak rolünün kaldırılması veya başka bir yaşam döngüsünden hesap/üyelik/alan kapatma dağıtık atomik işlem değildir; bunlar erişimi kapatır, üretim geçişinde ayrıca uzlaştırılmalıdır. Platform SUPER_ADMIN recovery istisnası korunur. LEGACY/SHADOW görevleri henüz erişim otoritesi olmadığından bu koruma erişimi kesen ENFORCED görev kapatmasında uygulanır.

## İşletim

Yeni migration `20261017150715_ManagementAssignments`: üç eklemeli tablo, Designer/snapshot, kapsam kısıtları, history/audit trigger'ları ve dolu Down reddi. Legacy veriye backfill yapılmaz. Dolu tabloda rollback yerine özelliği kapatıp ileri düzeltme/izole restore planı uygulanır; eski migrationlar değiştirilmez.

Bu teslimde yalnız izole fixture DB ve FakeKeycloak ile test yapıldı. Gerçek tenant/görev/rol kurulmadı; ana DB migrate veya servis restart yapılmadı. Canlı enforcement öncesinde doğru tenant/Property UUID'leri, doğal kişi+hesap/issuer bağı, gerçek realm rolleri/üyelikler, süreklilik, SHADOW farkları, yedek/restore ve v1/mobil geçiş kapsamı doğrulanmalıdır. Kullanıcının mevcut kişiler için belirlediği Altunbayrak Rezidans müşteri sınırı korunur; otomatik müşteri oluşturulmaz.

[FAZ05 raporu](../refactor/person/phase-05-report.md), [testler](../TESTING.md), [canonical kişi](central-people.md).
