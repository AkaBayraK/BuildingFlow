# Faz 06 — Güvenli senkronizasyon ve işlem kuyruğu

14.10.2026. Kullanıcı sonraki fazı ve eksik mobil API'lerin backend'e eklenmesini onayladı. Yalnız Faz06 uygulandı; profil fotoğrafı Faz07 ve talep formu/fotoğraf/kategoriler Faz12 kapsamında kalır.

## Kapsam

Talep metninin CREATE/UPDATE/DELETE işlemleri için merkezi API sözleşmesi ve SQLCipher outbox altyapısı. Bu faz yeni talep formu sunmaz; sonraki form, AuthProvider.queueCommand ile aynı altyapıyı kullanabilir. Ödeme işlemleri, belge/fotoğraf yükleme ve duyuru yazımı bu kuyruğa alınmadı.

Profilde Veri eşitleme paneli: gerçek kuyruktan bekleyen/çakışma/ilgi gerektiren sayıları, son başarılı tam okuma tarihi, Şimdi eşitle, geçici hatalarda tekrar deneme ve henüz gönderimi başlamamış işlemde onaylı iptal. Web preview kalıcı kuyruğun yalnız mobil uygulamada bulunduğunu açıkça gösterir. Faz04 görsel yönü korunur.

## Üretici ve tüketici

Backend:
- POST /api/v1/mobile/sync/commands: operationId, recordId, propertyId, unitId, role, action, expectedVersion, description.
- GET /api/v1/mobile/sync/requests?propertyId=...&unitId=...&role=...: userId/contextKey/records/hasMore. Tam bağlam listesi;1000 üzeri hasMore=true, istemci parçalı yanıtı uygulamaz.
- mobile_requests: gerçek talep metni, kullanıcı, Property/Unit/rol, version/audit ve soft-delete.
- mobile_operation_receipts: kullanıcı+operationId tek, SHA256 fingerprint ve aynı transaction'daki özgün sonuç. Aynı UUID/aynı içerik özgün sonucu replay olarak döndürür. Farklı içerik veya eski sürüm409 BF_MOBILE_SYNC_CONFLICT.
- Aktif MobileSession bağlamı her gönderim, replay ve okumada yeniden doğrulanır. OWNER/TENANT/RESIDENT daire ilişkisi ve aktif üyelik; yönetim rollerinde aktif yetkili Property. ACCOUNTANT-only bağlam talep yazamaz/okuyamaz. Kullanıcı yalnız kendi oluşturduğu taleplere erişir; yönetici diğer kişilerin taleplerini bu uçtan görmez.
- İstek gövdesi32KiB, açıklama1–2000 karakter. Id/operation UUID, createVersion0, update/deleteVersion>=1. Silmede açıklamaNULL; cevap tombstone ile descriptionNULL. Version audit interceptor ile ilerler.
- Yeni EF migration20261014062447_MobileRequestSync yalnız iki yeni tablo/index/FK ekler. Dolu işlem/talep geçmişi Down ile silinemez. Kaynak/Designer/snapshot birlikte üretildi, model check geçti ve yerel geliştirme DB'ye uygulandı.

Mobil:
- SQLite schema3: sync_outbox, sync_requests, sync_state. Hesap/context kapsamlı, mevcut SQLCipher anahtarıyla korunur.
- Kuyruğa alma UUID/body'yi değişmez tutar, ekstra alan/token yazmaz. pending→sending→sent; geçici ağ/429/5xx retry;409 conflict;403/404 blocked; diğer kalıcı hatalar failed. Sent yalnız eşleşen doğrulanmış sunucu makbuzuyla olur.
- Yanıt kaybolursa aynı operationId/body yeniden gönderilir; yeni UUID oluşturulmaz. Süreç kapanmasında sending→retry, makbuz replay ile belirsiz commit çözülür.
- En fazla5 işlem/run, otomatik6deneme, üstel gecikme en fazla60s; Retry-After en fazla120s. Kullanıcının manuel geçici-hata denemesinde UUID ve toplam deneme sayısı korunur. Çakışma, yetki veya bozuk makbuz otomatik/manual kör tekrar gönderilmez; düzeltme akışı sonraki form fazında ayrı yeni işlem gerektirir.
- İptal yalnız pending/attempts0; gönderimi başlamış işlemi iptal edilmiş gibi göstermez. İptal soft terminal durumudur.
- Tam yetkili pull snapshot transaction ile uygulanır; yeni version eski replay ile geriye alınmaz. Tombstone içerikNULL olur; listeden çıkmış kayıt temizlenir. Yetkisi kaldırılmış context'in önbelleği silinir, kuyruk payload'ı temizlenip blocked yapılır.
- HTTP401 yeniden giriş gerektirir. Mevcut Faz05 gizlilik politikası nedeniyle oturum temizliğinde yerel kuyruk/anahtar da silinir; taslağın logout sonrası korunacağı vaat edilmez.
- Bağlam değişimi/çıkış HTTP isteğini iptal eder; geç gelen cevap eski oturumda başarıya veya önbelleğe dönüşmez.
- Bağlantı/ön plan değişimi ve15s aralıkta kontrol; token gerektiğinde yenilenir. Token yenileme mevcut ekranı gereksiz yere giriş ekranına döndürmez; hata halinde oturum temizliği korunur.

## Doğrulama

- Backend build0hata/0uyarı;52/52 seçili MobileSync, MobileSession, Authentication, ReleaseSecurity test, atlama0. Gerçek ayrı PostgreSQL database/role ve fixture schema; temizlendi. Tüm backend regresyonu yeniden çalıştırılmadı.
- Mobil26/26 test: Node gerçek SQLite migration, hesap/scope ayrımı, cevabı kaybolmuş commit/replay,409,429/gecikme/deneme sınırı,401, yetki kaldırma, iptal, süreç recovery, tombstone, yanlış/truncated snapshot, token alanları, bozuk başarı makbuzu ve çıkış sırasındaki geç cevap.
- TypeScript/lint geçti; iOS/Android/Web export başarılı.
- iPhone SQLCipher fixture: nativeQueueReplay, migrations, secureStore, wrongKeyRejected, cleanup ve önceki güvenlik kontrolleri true. Bu test gerçek şifreli SQLite üzerinde kontrollü HTTP yanıt simülasyonu kullanır; canlı iş verisi yazmaz.
- Yerel API health200; anonim sync GET401; yeni API5006 + Metro8081 güncel LAN192.168.2.95 ile çalışır. Geçici native test modu kapatıldı.
- Chrome gerçek PKCE/API oturumu ve profil Veri eşitleme görünümü390px boyutta kontrol edildi; yatay taşma0. Kanıt phase06-profile.png. Bu web görünümü native kuyruk sayaçlarının test edildiği anlamına gelmez.
- Yeni npm/.NET bağımlılığı yok; önceki npm28audit uyarısı devam eder.

## Açık kabul ve sonraki adım

Gerçek iPhone uçak modu, uygulamanın tamamen kapatılıp açılması ve gerçek talep formuyla uçtan uca gönderim henüz arayüzden tamamlanmadı; form Faz12'de. Android native runtime kabulü yok. Development build'in soğuk açılışta Metro'ya ihtiyaç duyabilmesi offline release kabulünden ayrıdır. Geçerli sunucu makbuzu ve kuyruk davranışı, fiziksel iPhone SQLCipher fixture ile API integration testleri üzerinden doğrulandı; bu sınırlamalar tam uçak modu kabulü olarak sunulmaz.

Profil/photo Faz07 için kullanıcı onayı gerekir. Commit/push/mağaza yüklemesi yok. İş/UI roadmap sayacı değiştirilmedi; bu ayrı mobil15faz planıdır.
