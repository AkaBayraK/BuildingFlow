# Dashboard fotoğraflı üst alan —20.10.2026

BAKIM; kullanıcı iki ekran referansıyla değişikliği istedi. Önceki iptal edilen seçici işi, bu yeni istekle yalnız Dashboard için yeniden yetkilendirildi. Fotoğrafın alanın tüm kullanıcılarında ortak olması açıkça onaylandı. Yeni ürün fazı yok; SIDEBAR8/8 teknik teslimi korunur.

## Son davranış

Alan combosu Dashboard header'ında dil seçiminin hemen önündedir. Mevcut aktif/erişilebilir Property listesi100'lü sayfalarla okunur; seçim yalnız Dashboard veri endpointini etkiler. Sidebar/diğer modüllerin PropertyScope'u değiştirilmez. URL `/dashboard?property=UUID`; güvenli OIDC dönüş allowlist'i yalnız tek,boş olmayan UUID filtresine genişletildi. Ek query/token/fragment/duplicate parametre ve dış URL reddedilir. Back/forward ve doğrudan seçili URL desteklenir; erişilemeyen ID listeden seçilemez.

Eski rastgele SVG hero yerine fotoğraf/karartma/alan adı/BuildingFlow başlığı/slogan/özet kapsüller ve cam görünümlü saat paneli vardır. Varsayılan fotoğraf mevcut proje `website/public/images/residence.webp` dosyasının byte eş kopyasıdır; yeni stok/harici görsel/font/paket yok. Saat/tarih Europe/Istanbul gerçek saatidir. Blok/bölüm/dolu bölüm sayısı mevcut Property ve Dashboard DTO'larından gelir; dolu bölüm sakin kişi sayısı diye sunulmaz. ACCOUNTANT yönetici diye etiketlenmez. **Hava servisi olmadığı için sıcaklık boş ve Hava durumu kullanılamıyor gösterilir.** Görseldeki örnek18°C/218sakin/24x7 bilgileri gerçek veri olarak uydurulmadı.

Alan değişiminde veri alt bileşeni yeniden kurulur; önceki istekler abort edilir,eski kart/fotoğraf/yükleme durumu kalmaz. Daraltılmış KPI bakımı korunur. Mobil header iki satırda;picker/dil aynı üst sıradadır.

## Ortak fotoğraf sözleşmesi

Yeni endpoint/entity/migration/servis yok. Mevcut CommunicationService/StoredDocument/özel nesne deposu kullanılır:

- Yönetici(SUPER_ADMIN/SITE_ADMIN/MANAGER),dialogdaki açıklamayı görüp dosya seçer ve Kaydet'e basar. ACCOUNTANT yükleyemez;yetki backend CanManageHierarchy + aktif üyelikte tekrar doğrulanır.
- JPEG/PNG/WebP,10MiB,400x160 minimum;istemci decode+çözünürlük sınırı8192/32Mp,canvas ile en fazla2560x1440 JPEG'e dönüşüm. EXIF/orijinal dosya adı aktarılmaz. API mevcut JPEG imzası/MIME/uzantı/boyut kontrolünü sürdürür.
- `POST /api/v1/properties/{id}/documents`: file=`buildingflow-dashboard-banner.jpg`,audience=ALL_MEMBERS,announcementId yok,requestId rastgeleUUID. Belirsiz retry aynı hazırlanmış File/UUID'yi kullanır; yeni seçim yeniUUID'dir. Mevcut normalizefingerprint/replay/parentlock korunur.
- Liste `documents?page=1&pageSize=100&search=buildingflow-dashboard-banner.jpg` yeni→eski sırasındadır. Tam dosya adı,standalone,ALL_MEMBERS,JPEG eşliği olan en yeni kayıt seçilir. Aynı zamanda iki başarılı upload olursa createdAt/Id sırası kazanır; eski fotoğraf immutable belge olarak kalır. Yeni bir Property version alanı veya atomik compare-and-swap ayarı eklenmedi.
- İndirme yalnız tokenlı mevcut `/documents/{id}/download`;metadata propertyId,boyut,MIME ve SHA256 doğrulanır,objectURL temizlenir. Genel/public URL,token/localStorage veya anonimSSR yok. Eksik/erişilemez/bozuk görselde varsayılan fotoğraf kalır.
- Fotoğraf alan üyeleri için ortak belgedir;belgeler ekranında görülebilir. Yetkili kullanıcı en yeni fotoğraf belgesini silerse önceki uygun fotoğraf yeniden seçilir. Silinmiş/eski nesneler otomatik yok edilmez. Yeni fotoğraf diğer oturumlarda sayfa açılınca veya banner Yenile ile okunur;realtime push yoktur.

## Değişen dosyalar

Frontend yeni: DashboardArea.tsx,area-selection.ts,DashboardHero.tsx,banner.ts,dashboard-banner.test.mjs,public/images/dashboard-residence.webp ve provenance README. Güncellenen: DashboardPage,WorkspaceShell,AuthGate SessionControls sunum slotu,return-path,globals.css,TR/EN. Test-only sidebar fixture script/API/entry gerçek Dashboard ve fotoğraf Blob/SHA yolunu çalıştıracak şekilde genişletildi;önceki fixture kontrolleri korunur. Backend yalnız CommunicationApiTests'e ortak banner üyelik/yetki/replay testi eklendi;üretim backend kaynakları değişmedi.

Belgeler: API dashboard/communication,frontend-rules,licenses,SKILLS_AND_AGENTS,TESTING ve progress kayıtları. Başlangıç staged/unstaged kullanıcı farkları ve Git indeksi korundu;commit/push/deploy/reset yok.

## Bu turun kanıtı

**182/182 frontend,0fail/skip;typecheck ve üretim build PASS.** Yeni5senaryo:aktif alan/bozukID,pagination/abort/duplicates,banner metadatascope/en yeni seçim,dosya sınırı,negatif dönüşURL.

**23/23 PostgreSQL+storage integration,0fail/skip**:CommunicationApiTests17 ve DashboardApiTests6. Yeni DashboardBannerIsSharedAcrossMembersWithHistoryAndScopedDownloads testi ikinci bağımsız ACCOUNTANT üyeye liste/byte indirme,eski görsel geçmişi,replay,ACCOUNTANT upload403,dış alan/üyelik404,no-store doğruladı. GeçiciDB/rol ve yalnız fixture buildingflow-test-* bucket kullanıldı/temizlendi;gerçek uygulama verisi/ayarları değiştirilmedi.

Komutlar: frontend `npm test`, `npm run typecheck`, `npm run build`;backend `dotnet test backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj --no-restore --verbosity quiet --filter "FullyQualifiedName~CommunicationApiTests|FullyQualifiedName~DashboardApiTests" --logger "trx;LogFileName=hero.trx" --results-directory /tmp/bf-hero-results`. Genel scripts/test.sh/ci.sh servis/kurulum etkileri nedeniyle çağrılmadı. Tam backend suite bu tur koşulmadı.

**Gerçek React bileşenleri,test-only API/Keycloak/router ile tarayıcı:**1440px picker dil önünde,fotoğraf yüklü;ikinci alanda48 ve scoped KPI bağlantısı,geri dönüş96;alan değişiminde0eski kart/varsayılan fotoğraf;375pxheader97px;320pxdocumentwidth320/hero288,375pxtaşma0. Fotoğraf seçimi→JPEG hazırlama→Kaydet→Blob/SHA→arka plan değiştirme PASS. Bu UI save testinde API adapter kullanılır;gerçek ortak persistence kanıtı yukarıdaki integration testidir. Consoleerror0. [Olaylar](DASHBOARD_HERO_EVIDENCE.json),[masaüstü](DASHBOARD_HERO_DESKTOP.jpg),[mobil](DASHBOARD_HERO_MOBILE.jpg).

Canlı Keycloak/gerçek kullanıcı hesabıyla uçtanuca kabul,gerçekcihaz ve gerçek hava sağlayıcısı **DOĞRULANAMADI**. Uyarlama,referanstaki düzen ve katmanları uygular;gerçek ölçü/veri oranına göre responsive davranır. Servis fotoğraf okuma hatasında varsayılana döner;canlıstorage işletim sağlığı kabulü değildir.

## Koruma ve geri alma

Başlangıç indeks,manifest/lock/csproj/migration/LICENSE/NOTICE/iki next-env aynı;build ürettiği iki import yalnız exactbyte karşılaştırmasıyla başlangıca döndü. Görsel mevcut özgünprojeassetidir;restore/npmci/kurulum yapılmadı,lisans envanteri yeniden yazılmadı. Yeni lisans yükü yok. Fixture/sekme kapatıldı,viewport reset.

Geri alma gerektiğinde yalnız bu bakımın hunklarını önceki kaynaklarla karşılaştır:DashboardPage,SessionControls slotu/WorkspaceShell provider'ı,return-path izinliDashboard filtresi,CSS ve sözlükler birlikte ele alınır;diğer sidebar/profil/KPI kullanıcı farkları korunur. Yeniimportlar kalkmadan yeni modüller silinmez;backend üretimrollback/migration gerekmez. Ortak görsel belge geçmişi otomatik silinmez;önceki frontend artefaktına dönüş yeterlidir. Geri alma uygulanmadı.

**Bu tur1/1=%100,00;SIDEBAR8/8=%100,00 korunur.** İŞ20T20.6bakım ve gerçek kullanıcı kabul görevleri bu belgeyle tiklenmedi. Sonraki kapsam kullanıcı incelemesidir.
