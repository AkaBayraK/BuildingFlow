# Merkez giriş ve çalışma alanları — PERSON FAZ07

18.10.2026. Keycloak kimliği doğrular; context listesi ve her nesne isteğinin yetkisi .NET tarafından güncel verilerden hesaplanır. Seçilen context ID/rol bir yetki belgesi değildir, istemci veya URL yeni hak vermez.

## Aktivasyon ve uyumluluk

Varsayılan kapalı:

- API: `PersonRefactor:Enabled=true` ve `PersonRefactor:SessionContextsEnabled=true`.
- Web build **ve** runtime: `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED=true`; web yeniden derlenir. Bu akışta merkezi kişi rotaları da kullanılabilir; ayrı kişi merkezi bayrağının kapalı olması çalışma alanındaki kişi bağlantısını eski ekrana düşürmez.

Compose build args/environment bu değerleri taşır. Gerçek .env, canlı DB, tenant/hesap/rol eşlemesi ve servisler bu fazda değiştirilmedi. Yeni migration/model/bağımlılık yoktur. Önceki fazlardan kalan migrationların sonraki AutoMigrate açık API başlangıcında uygulanma ihtimali devam eder.

**Bu API bayrağı merkez için global ve katıdır.** Açıldığında ManagementAccess LEGACY/SHADOW yerine ENFORCED davranır; açık eski ayar erişim fallback'i sağlamaz. Aktif canonical hesap+kişi bağı, tenant/Property üyeliği, PersonPropertyScope ve güncel görevler önceden hazırlanmalıdır. Eksik kayıtlar eski membership'ten otomatik oluşturulmaz. Bilinmeyen tenant mode yapılandırması kapalı kalır. SUPER_ADMIN platform istisnası aşağıda açıkça temsil edilir.

Bayrak kapalıyken mevcut merkez ve mobil davranışları korunur. Bayraklar tenant/rol verisi yazmaz; web bayrağını tek başına açmak API'de hak açmaz. Veri modeli hazırlanıp yayın kabulü alınmadan canlı açılış yapılmamalıdır.

## GET sözleşmeleri

`/api/v2/session/contexts?page=1&pageSize=20` kimlikli kullanıcıya sayfalı `items/page/pageSize/totalCount` döndürür. Sayfa1–100000, boyut1–100. Liste RepeatableRead snapshot'ta sayılır ve sıralanır: tenantId/propertyId/kind/id. Tüm sonuçları ilk100 kayıttan ibaret saymaz; tek alan kararında totalCount kullanılır.

`/api/v2/session/contexts/{kind}/{id}` yalnız aktörün **o an geçerli** context'ini ve minimum kendi kişi bilgisini döndürür: `{context,self}`. Yabancı/iptal/sona ermiş/inaktif kaynak404, geçersiz kind/boş UUID veya pagination400; anonim401. Her GET veri ve bağları yeniden okur; oturumda uzun ömürlü grant/cache tutulmaz.

Context alanları:

| Alan | Açıklama |
| --- | --- |
| id, kind | Kaynak UUID + PLATFORM/MANAGEMENT/OWNER/TENANT/RESIDENT; birlikte kimlik |
| tenantId/name, propertyId/name | İzinli müşteri ve alan |
| unitId/number | Yalnız daire ilişkisi için; yönetimde null |
| role | Mevcut görev veya ilişki türü; yeni realm rolü üretmez |
| startDate/endDate | İki uç dahil tarihler; PLATFORM'da null |
| canManagePeople | SITE_ADMIN/MANAGER ve platform için yönetici yeteneği |
| canManageFinance | Yalnız yönetim/platform context'i |
| canManageAssignments | SITE_ADMIN/platform yeteneği; gerçek komut ayrıca önceki servis kurallarına tabidir |

`self`: yalnız kendi UUID/tenantId/displayName/kind/maskedIdentity/canEdit:false. Tam kimlik numarası, iletişim bilgisi, issuer/subject, parola veya başka kişinin kartı yoktur. PLATFORM için hesap bağı bulunmayabilir ve self null olabilir. Bu uçlarda yazım yoktur.

## Context üretimi

- MANAGEMENT: aktif NATURAL kişi, tam issuer+subject hesabı, aktif ve başlamış/bitmemiş hesap bağı, aktif tenant/Property üyeliği ve scope, güncel ve iptal edilmemiş ManagementAssignment. Kaynak legacy kişi değişmişse gölge kişi görev yetkisinde kullanılamaz.
- Görev aynı zamanda mevcut JWT realm yeteneğine uymalıdır. MANAGER claim'i SITE_ADMIN göreviyle yükselmez. SITE_ADMIN/MANAGER üst yetenekleri daha alt görevi yerine getirebilir; yalnız ACCOUNTANT görevi bulunan MANAGER claim'i kişi okuma yetkisi kazanmaz. Rol iptali JWT yenilenmesine; DB görev/üyelik/bağ iptali sonraki API isteğine yansır. JWT dışı gerçek Keycloak rol değişimi her okumada introspection ile sorgulanmaz.
- OWNER/TENANT/RESIDENT: kişinin tam issuer hesabı ve tenant üyeliği, kendi aktif canonical ilişkisi, aynı tenant/Property scope ve aktif bölüm/üst bina gerekir. Property üyeliği tek başına daire ilişkisi değildir. Realm'de OWNER checkbox/rolü aramak yerine ilişki kaydı esas alınır. OWNER'dan RESIDENT otomatik çıkarılmaz; üç tür ayrı seçimdir.
- Aktarılmış daire ilişkisinde legacy source version/enabled kontrolü de yapılır. Eski kaynağın değişmesi eski gölge snapshot ile girişe izin vermez. Geçmiş ve gelecek ilişkiler güncel context listesine girmez.
- PLATFORM: SUPER_ADMIN için aktif tenant+Property scope; id=PropertyId. Yapay görev, kişi veya malik ilişkisi oluşturulmaz. Müşteri/alan adı dışındaki platform verisi context listesine dökülmez.

## Her istekte merkez alan yetkisi

Oturum bayrağı açıkken `/api/v1/properties/{propertyId}/...` merkez uçlarına ek sunucu kapısı uygulanır. ID ve eylem kapsamı URL'den, hak güncel canonical görevlerden hesaplanır. Finans modülleri ACCOUNTANT yeteneği; kişi/occupancy/talep/SMS ve hiyerarşi yazımı MANAGER yeteneği ister. Salt okunur hiyerarşi/alan/duyuru-belge okuması yönetim/finans yeteneğiyle kullanılabilir. Mevcut endpoint policy ve store nesne/scope/version kontrolleri ayrıca devam eder; middleware onların yerine geçmez. İlgili Property isteğinde uygulama yetenekleri güncel göreve daraltılır: örneğin realm MANAGER olup yalnız ACCOUNTANT görevi olan aktörün finans erişimi korunur, ancak Dues içindeki kişi seçenekleri/kişi filtresi ve kişi snapshot görünürlüğü yönetici sayılmaz. Bu sunucu kapsamı JWT/Keycloak rolü yazmaz ve istemci başlığından alınmaz. Aktörün diğer geçerli görevleri de yetki kaynağı olabilir, seçtiği navigasyon context'i diğer meşru görevlerini silmez.

Property listesi aynı güncel görev filtresini DB'de uygular. Kişi merkezi tenant okuması ve görev uçları ENFORCED denetimi kullanır. Salt OWNER/TENANT/RESIDENT üyelikten yönetim alanı/listesi veya genel kişi kartı okuyamaz; yeni kendi context ekranı üzerinden maskeli bilgi görür.

Global eski `/api/v1/users` ve `/api/v1/consents` tenant kapsamını temsil etmediğinden yeni akışta tenant yöneticilerine kapalıdır; mevcut policy'ye göre403 veya404 olur. SUPER_ADMIN platform yönetimi istisnası korunur. Menü bağlantıları da bu sınırı izler. Kendi `/account` ve kendi rıza işlemleri bu global yönetim uçları değildir ve önceki kapsamlarını korur.

**Geçiş sınırı:** mobil `/api/v1/mobile/*`, şifreli yerel model/outbox ve finansın canonical ilişki projeksiyonu FAZ08–09 kapsamındadır. Buradaki merkez denetimi mobil protokolün görev modeline geçtiği veya tüm uygulamada kesintisiz global cutover tamamlandığı iddiası değildir. Yeni bayrak canlıda açılmadı. Eski mobil yetki yolları da geçirilmeden tüm sistemde görev iptali kabulü tamamlanmış sayılamaz.

## Web davranışı

- Kimlik doğrulanınca varsayılan `/` girişinde: totalCount=1 ise doğru kind/id alanına replace; birden fazla veya sıfır ise `/workspaces` seçimi/boş durum.
- `/workspaces` manuel alan değiştirme ekranıdır; bütün sayfalar dolaşılabilir. `/workspaces/{kind}/{id}` her açılışta güncel API okur ve sekmeye geri dönünce yeniler. Geçerli OIDC derin bağlantıları korunur; sorgu token'ı/dış URL/geçersiz kind/UUID kabul edilmez.
- Yönetim ve daire context'i aynı kişi için ayrı ayrı görünür. Daire ekranında kişi düzenleme ya da genel kişi listesi bağlantısı yoktur. Yönetim ekranı sunucu yeteneklerine göre alan/kişi/finans bağlantıları verir. Kişi/görev yazımı önceki servislerdedir.
- AuthGate korunur; kişi verisi sunucu HTML'ine anonim yerleştirilmez. Liste/detay no-store yetkili API üzerinden istemcide yüklenir. Rota/aktör değişiminde eski veri ve istek iptal edilir; context grant'i veya kişi bilgisi localStorage/sessionStorage'a yazılmaz. Sadece mevcut doğrulanmış OIDC dönüş yolu mekanizması korunur.
- Hata/boş/loading/retry, TR/EN, etiketli navigasyon ve mevcut Button/MessageBanner/WorkspaceHeading bileşenleri kullanılır. Kalıcı otomatik “son rol” seçimi yapılmaz.

## Doğrulama ve geri dönüş

Kanıt [FAZ07 raporunda](../refactor/person/phase-07-report.md). Gerçek PG'de yabancı nesne, çoklu bağımsız ilişki, tarih sınırı, görev iptali, pasif tenant/alan/hesap/bağ/üyelik, yanlış issuer, kaynak sapması ve accountant görevinde manager yazımı reddi test edilir. Frontend testleri tam sayıya göre tek seçim, yetenek tutarlılığı, scope/PII projeksiyonu, güvenli dönüş yolu ve i18n kontrolüdür; canlı klavye/cihaz kabulünün yerine geçmez.

Veri yazmayan bayraklı değişimdir. Web kapatılıp yeniden derlendiğinde önceki giriş ekranı döner. API oturum bayrağını kapatmak global katı merkez kapısını kaldırır ve önceden yapılandırılmış tenant ManagementModes davranışına döner; bu yetki davranışı değişimi yayın kararıdır. Explicit ENFORCED tenant ayarını v2 uçlarında otomatik LEGACY'ye indirmez; ancak eski v1 Property için bu fazda eklenen kapı API bayrağıyla birlikte kapanır. Dolayısıyla API bayrağını kapatmak önceki rol+üyelik erişimini yeniden açabilir; bu işlem canlıda otomatik yetki rollback'i sayılmaz. Veri silme/Down migration gerekmez.
