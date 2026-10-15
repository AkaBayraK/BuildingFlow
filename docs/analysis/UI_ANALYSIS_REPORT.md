# BuildingFlow — UI Aşama 01 analiz raporu

Tarih: **8 Ekim 2026**  
Proje: /Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow
Teslim: docs/analysis/UI_ANALYSIS_REPORT.md

## 1. Sonuç ve kapsam

**UI Aşama 01 analizi tamamlandı.** Mevcut shadcn/ui + TanStack Table, TR/EN, ortak mesajlar ve Property → Building → Unit modeli tasarım dönüşümünün temeli olabilir. Dokuz referans gerçek React bileşenleriyle uygulanabilir; örnek sayılar, kişiler, finansal işlemler ve servis durumları uygulama verisi değildir.

Bu analiz sırasında uygulama kodu, API sözleşmesi, migration, veritabanı şeması ve bağımlılıklar değiştirilmedi. Yeni teslim yalnızca bu rapordur. Servis başlatma/durdurma, veri oluşturma, migration uygulama ve hesap/parola sıfırlama yapılmadı. Tek agent kullanıldı; ek skill veya alt agent çalıştırılmadı.

**Mevcut engel:** Analiz talebinden önce başlatılan iş Faz 05 aidat taslağı tamamlanmamıştır. Güncel backend derlemesi DependencyInjection.cs:15 içindeki eksik **DuesStore** nedeniyle **CS0246** hatasıyla başarısızdır. Taslak çalışan özellik kabul edilmez. Önceden başlatılmış API sürecinin çalışması yeni kaynak kodun derlendiğini kanıtlamaz.

**Bu tur doğrulananlar:** Frontend 11 testi, TypeScript kontrolü ve üretim derlemesi başarılıdır. Backend derlemesi başarısızdır. Önceki 171 backend testi ve 17 Keycloak kontrolü bu tur yeniden çalıştırılmadı.

## 2. Verilen prompt'un değerlendirmesi

Analiz → rapor → onay sırası uygundur. Kod/veri/API değişikliklerini yasaklaması, iş kurallarını görsel örneklerden ayırması ve ortak bileşen planı istemesi doğru sınırları koyar.

| Konu | Tespit | Bu analizdeki karar |
| --- | --- | --- |
| Prompt konumu | MASTER_PROMPT.md, AGENTS.md ve PHASES.md frontend/buildingflow-ui-plan altında. | Üçü birlikte; kök/frontend AGENTS ve docs/frontend-rules.md ayrıca okundu. |
| Rapor yolu | MASTER_PROMPT docs/ui-modernization-audit.md; güncel kullanıcı docs/analysis/UI_ANALYSIS_REPORT.md istiyor. | Kullanıcının güncel yolu esas alındı; ikinci rapor oluşturulmadı. |
| Referans yolu | Prompt docs/ui-references diyor. Kullanıcı frontend/buildingflow-web/doc klasörünü verdi. | Gerçek ui-references alt klasöründeki **9 PNG** incelendi. .DS_Store görsel değildir. |
| Faz numarası | İş Faz 05 aidat; UI PHASES.md Aşama 05 yönetim alanları. | Bu belgede **iş Faz** ve **UI Aşama** ayrı kullanılır. Sonraki onay UI Aşama 02 içindir. |
| Paket/servis varsayımı | Plan Recharts ve “var olan Excel/PDF servisleri” diyor. | Recharts kurulu değil; Excel/PDF servisi bulunmadı. Hazır özellik sayılmadı. |
| AGENTS taşıma | Plan README'si dosyaları frontend köküne kopyalamayı öneriyor. | Mevcut AGENTS üzerine yazılmadı; ileride plana bağlantı eklemek mevcut domain/17 standardı korur. |
| Görsel/veri ilişkisi | Kimlik, tek kişi rolü, blok ve finans örnekleri kurallarla çelişebilir. | Çelişkiler aşağıda raporlandı; gerçek model görsele göre değiştirilmedi. |
| Kontroller | Build/test üretilen dosyalar oluşturabilir. | Kaynaklar korundu; .next, bin/obj ve TypeScript önbelleği kaynak değişikliği sayılmadı. |

Sonraki prompt'ta gerçek referans klasörü, tek rapor yolu ve “UI Aşama 02” yazılmalı. Eksik API için örnek KPI/grafik veya sıfır üretilmemeli; ayrı backend ihtiyacı onaylı kapsam olmalı.

Okunan temel kaynaklar:

- [Kök AGENTS.md](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/AGENTS.md)
- [Frontend AGENTS.md](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/AGENTS.md)
- [Frontend Convention — 17 standart](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/frontend-rules.md)
- [MASTER_PROMPT.md](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-ui-plan/MASTER_PROMPT.md)
- [UI planı AGENTS.md](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-ui-plan/AGENTS.md)
- [UI PHASES.md](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-ui-plan/PHASES.md)
- [Mimari kaynak](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/BUILDINGFLOW_PROJECT_CORE.md)
- [Test kaydı](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/TESTING.md)
- [Migration kuralları](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/MIGRATIONS.md)
- [Kişi/ilişki API sözleşmesi](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/api/people.md)
- [Lisans politikası](/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/docs/licenses.md)

## 3. Mevcut teknoloji ve mimari

### 3.1. Teknoloji envanteri

Sürümler package.json/proje dosyalarından okundu; aralıklı değerler manifest aralığıdır.

| Alan | Mevcut durum | Sonuç |
| --- | --- | --- |
| Next.js | 16.4.0, App Router, standalone | Server/Client sınırı korunmalı; kodlamada kurulu sürümün yerel rehberi okunmalı. |
| React / React DOM | 19.3.0 | Mevcut bileşen yapısı kullanılabilir. |
| TypeScript | ^5.9.3, strict açık | Başarılı JSON DTO'ları runtime şemayla doğrulanmıyor. |
| Tailwind | CSS/PostCSS 4.3.3 | CSS import ve @theme inline mevcut; klasik tailwind.config olmaması eksik kurulum değildir. |
| shadcn/ui | New York, slate, CSS variables; kaynaklar proje içinde | Button/Input/Table/Dialog vb. ve LICENSE/NOTICE korunmalı. |
| TanStack Table | 8.21.3 | Sunucu pagination/sorting mevcut; TanStack Query değildir. |
| i18n | next-intl 4.14.9; TR/EN, cookie, LanguageSwitcher | Yeni UI/form/ARIA metinleri iki sözlüğe eklenmeli; URL dil öneki yok. |
| Auth | keycloak-js 26.2.4 | Authorization Code + S256 PKCE; token bellekte. |
| İkon/yardımcılar | Lucide 1.52.0, Radix ^1.7.0, CVA 0.7.1, clsx 2.1.1, tailwind-merge 3.7.0 | Yeni ikon seti/ücretli kit gerekmiyor. |
| Backend | .NET/ASP.NET Core net10.0 | Api/Application/Domain/Infrastructure/Worker modular monolith. |
| Persistence | EF Core 10.0.12; Npgsql EF provider 10.0.3 | PostgreSQL, sürümlü EF migrations; EnsureCreated geçişi önerilmiyor. |
| Altyapı | Compose: PostgreSQL, Keycloak, Redis, RabbitMQ, MinIO, Grafana, FE/API/Worker | Servis tanımı bütün business entegrasyonlarının tamamlandığı anlamına gelmez. |
| Grafik | Recharts veya başka grafik paketi manifestte yok | UI 04 öncesi uyumluluk/lisans incelemesi gerekir; bu tur kurulmadı. |
| Form/state | RHF, Zod, TanStack Query, Zustand kurulu değil | Convention hedefleridir; belgeyi okumak toplu kurulum gerektirmez. |
| Tema | Açık tema/kısmi semantic token | Light/Dark/System sağlayıcısı henüz yok. |
| Scripts | dev, test, typecheck, build, start | lint/format-check yok; çalıştırılmış sayılmaz. |

### 3.2. Yapı ve sorumluluklar

~~~text
BuildingFlow/
├── AGENTS.md
├── backend/
│   ├── BuildingFlow.sln
│   ├── src/BuildingFlow.{Api,Application,Domain,Infrastructure,Worker}
│   └── tests/BuildingFlow.{UnitTests,IntegrationTests}
├── frontend/
│   ├── buildingflow-ui-plan/        # UI prompt ve 01–12 planı
│   └── buildingflow-web/
│       ├── AGENTS.md
│       ├── doc/ui-references/      # 9 masaüstü PNG
│       ├── messages/{tr,en}.json
│       ├── src/app/                # App Router, layout, globals.css
│       ├── src/auth/               # AuthGate, config, dönüş whitelist
│       ├── src/components/         # shell, UI, feedback, RecordList
│       ├── src/features/           # properties/hierarchy/people/navigation/roadmap
│       ├── src/services/           # API, ProblemDetails, server-only health
│       ├── src/i18n/
│       ├── src/lib/                # Intl/date-only format
│       └── tests/
├── deploy/
├── scripts/                        # dev/DB/auth/smoke/lisans
└── docs/                           # mimari/API/migration/test/lisans
~~~

Feature bileşenleri veri/iş akışını, ortak bileşenler sunumu yönetiyor. Domain HTTP/EF/UI bağımlılığı almıyor. Görsel dönüşüm için yeni microservice/framework mimarisi gerekmiyor.

### 3.3. Sidebar, header, layout ve CRUD

WorkspaceShell/PropertyScope seçili yönetim alanını ve aktif menüyü belirliyor. Masaüstünde yaklaşık 248 px beyaz sidebar ve büyük kullanıcı logosu mevcut. Header basit çalışma alanı başlığı, dil, kullanıcı adı, API kontrolü ve çıkış içeriyor. Çok seviyeli breadcrumb, global arama, bildirim merkezi, avatar menüsü ve sidebar daraltma yok.

globals.css açık temada background #f5f7fb, foreground #102342 ve primary #0866e8 kullanıyor. Arial/Helvetica/system font mevcut; dış font yok. Semantic token yanında slate/blue sınıfları ve global aside/nav/header/article kuralları bulunuyor. Ayarlar gibi iç sidebar'lar yanlış shell stilini alabilir; bu kurallar kendi sınıflarıyla sınırlandırılmalı.

650/900/1050 px özel eşiklerle Tailwind eşikleri birlikte kullanılıyor. Mobilde menüler satırlara dönüşüyor; referanstaki açılır navigasyon yok. Kişi/ilişki ve bina seçiminde RecordList masaüstü tablo/mobil kart; eski Property/Hierarchy tablolarında kendi container'ında yatay kayma var. Bu tur önceki iki menü screenshot'u incelendi; yeni canlı responsive kabulü yapılmadı.

EntityEditor/DeleteEntity ve oluşturma formları required/alan hatası, gönderim kilidi, iptal, form değerini koruma, version/409 güncel kaydı yükleme ve soft-delete davranışlarını paylaşıyor. RHF/Zod henüz kullanılmıyor; native select/number/date kontrolleri var. Sayısal virgül/nokta girişinin ortaklaştırılması planlıdır.

MessageBanner/FieldError/NotificationProvider başarı, hata, uyarı ve bilgiyi farklı ikon, tür başlığı ve semantic renkle ayırıyor. Alan hatası kalıcı; hata/uyarı kapatılana kadar, başarı/bilgi 6 saniye görünür. Dialog focus/escape, live region ve mevcut recovery eylemleri tasarımda korunmalı.

## 4. Menü, route, API, authentication ve iş kuralları

### 4.1. Gerçek frontend rotaları

| Menü/ekran | Rota | Mevcut davranış |
| --- | --- | --- |
| Genel Bakış | / | Foundation kartları, API sağlık durumu ve yol haritası; operasyon dashboard'u değil. |
| Yönetim Alanları | /properties | Create, search/sort/server pagination, kolon görünürlüğü ve detay. |
| Daireler / Bağımsız Bölümler | /units | Alan seçilmemişse erişilebilen Property seçimi; kalıcı menü. |
| Kişiler | /people | Yönetici rolünde Property seçimi; genel tüm kişiler listesi değil. |
| Alan genel bilgileri | /properties/[id] | Tür/adres/metadata/sayaç, düzenleme ve silme. |
| Bina/Bloklar | /properties/[id]/buildings | Ayrı liste/CRUD; APARTMENT'ta gösterilmez. |
| Alan bölümleri | /properties/[id]/units | Apartmanda doğrudan Unit; diğer tiplerde bina seçimi. |
| Bina detail/bölümleri | /properties/[id]/buildings/[buildingId] ve altındaki /units | Ayrı bina bilgisi ve bölüm listesi/CRUD. |
| Alan kişileri | /properties/[id]/people | Yöneticiye açık Person CRUD; RecordList. |
| Kişi ilişkileri | /properties/[id]/people/[personId] | Ayrı PersonPage; OWNER/TENANT/RESIDENT, tarihli bitiş/geçmiş. |
| Eski bağlantılar | /sites, /sites/[id], /sites/[id]/blocks/[blockId] | Canonical Property/Building redirect. |
| Giriş/sağlık/hata | /login, /health, not-found ve workspace/error | Keycloak yönlendirme, sağlık ve güvenli hata sınırları. |

Aidat, ödeme, muhasebe, talepler, duyurular, raporlar, kullanıcı yönetimi ve sistem ayarları için tamamlanmış frontend rota/ekran bulunmuyor. Referans sidebar'ındaki menüler çalışır özellik sayılmamalı.

### 4.2. API ve auth entegrasyonu

| API grubu | Kaynak | Kapsam |
| --- | --- | --- |
| /api/v1/properties ve /{id} | PropertyEndpoints, HierarchyEndpoints | Liste/detail/CRUD; Property yazma SUPER_ADMIN. |
| Property /buildings ve /{buildingId} | HierarchyEndpoints | Bina CRUD; yönetici yazma ve üyelik scope. |
| Property /units veya Building /units | DirectUnitEndpoints, HierarchyEndpoints | Doğrudan apartman/bina bölümü; parent/type/version kontrolleri. |
| /people, /occupancies, /unit-choices | PeopleEndpoints | Kişi/ilişki; SUPER_ADMIN/SITE_ADMIN/MANAGER + scope. ACCOUNTANT/RESIDENT kişisel veriyi okuyamaz. |
| /health, /health/live, /api/v1/status | Program.cs | PostgreSQL readiness, liveness ve yetkili API kontrolü; bütün servisleri kapsayan durum API'si yok. |
| Property /dues | DuesEndpoints — önceki taslak | Kaynakta mevcut; store/migration/test/UI eksik, build başarısız. Çalışan sözleşme kabul edilmedi. |

AuthGate standard flow/S256 PKCE kullanıyor. Token bellekte; sessionStorage yalnız whitelist'ten geçen yerel dönüş pathname'ini tutuyor. Locale login'e iletiliyor; refresh/logout/legacy dönüş korunmalı. AuthGate kullanıcı deneyimidir, backend yetkilendirmesi değildir. Ayrı frontend middleware/proxy yetki altyapısı bulunmadı.

Backend issuer/audience/lifetime ve imzayı doğruluyor. Rol kaynağı imzalı realm_access; rastgele rol claim veya client role kabul edilmiyor. SUPER_ADMIN global istisnası dışındaki veri erişimi aktif PropertyMembership ile sınırlı. Üretimde HTTPS metadata; CORS belirli origin/method/header.

useAuthorizedApi token yeniler, Authorization/no-store ekler ve ProblemDetails'i güvenli TR/EN mesaja dönüştürür. Hata gövdesi unknown olarak kontrol edilir; başarılı JSON ise TypeScript tipine cast edilir. Runtime DTO doğrulama, timeout, Query cache ve code/traceId sözleşmesi tamamlanmış değil. İş ekranına ikinci fetch/token client eklenmemeli.

Login/reset Keycloak'ın iki sütunlu çizimli temasında yürütülüyor. Önceki test kaydı yerel Mailpit teslimini içeriyor; üretim SMTP bu tur kurulmuş/test edilmiş değildir. Dokuz yönetim görseli login referansı içermiyor; mevcut güvenli giriş akışı ayrı korunmalı.

### 4.3. Korunacak domain kuralları

1. **Property:** APARTMENT/SITE/RESIDENCE/MIXED. Apartman doğrudan Unit, building_id NULL; diğer tiplerde gerçek Building. Sahte A Blok yok.
2. **Unit türü:** APARTMENT/OFFICE/SHOP/VILLA/WAREHOUSE/WORKSHOP. “3+1” oda düzeni mevcut Unit.type değildir.
3. **Kat:** Unit.floor sayısal, bodrum negatif olabilir; floorCount metadata. Section/Floor entity ve otomatik kat/daire üretimi yok.
4. **Tür değişimi:** SUPER_ADMIN, expected version ve canChangeType. Pasif/soft-delete alt kayıt geçmişi de engel; görünür sıfır sayaç yetmez. Oluşturan üyeliği engel değildir.
5. **Person:** Property'ye özel, login hesabı değil. Ad/soyad, isteğe bağlı iletişim; **TC kimlik numarası toplanmaz**.
6. **UnitOccupancy:** Rol kişi üzerindeki tek alan değil, ilişkidedir. OWNER/TENANT/RESIDENT; ortak malik/çoklu ilişki. “Aile ferdi” ayrı enum değil.
7. **Tarih/geçmiş:** DateOnly iki uç dahil, Europe/Istanbul. Aynı kişi/bölüm/rol ve iki birincil iletişim çakışamaz. Bitiş version ile bir kez; devam eden/gelecekte biten ilişki Person/Unit silme-pasife almayı engeller.
8. **Persistence:** Sekiz NOT NULL BaseEntity alanı, audit/version/soft-delete; migration/snapshot ve ayrı gerçek PostgreSQL doğrulaması.
9. **Finans:** Tahakkuk, tahsilat ve muhasebe geliri farklıdır. Faz 05 planında borç Unit'e aittir; kişiye otomatik finansal sorumluluk atanmaz.

## 5. Referansların ortak görsel analizi

Dokuz PNG yaklaşık 1672 × 941 masaüstü panelidir. Mobil/tablet referansı yok; responsive öneriler uygulama planıdır, test edilmiş hedef görünüm değildir.

| Alan | Gözlem | Uygulama planı |
| --- | --- | --- |
| Yerleşim | Yaklaşık 246 px koyu sidebar, ince beyaz header, açık içerik ve kart/table | Tek WorkspaceShell; mevcut ayrı modül route'ları korunur. |
| Palet | Lacivert, mavi aktif menü, beyaz/açık gri yüzey; yeşil/amber/kırmızı durum | Ortak semantic sidebar/content/status/chart token. Rasterdan kesin hex çıkarıldığı iddia edilmez. |
| Tipografi | Sans serif, belirgin başlık, küçük ikincil metin ve kompakt table | Sistem fontuyla ortak ölçek; font adı görselden varsayılmaz. |
| Sidebar/header | Menüler gruplu, ikon/aktif pill; arama, dil, bildirim, avatar/rol | Mevcut Lucide/logo; rol/type kontrolü. Business arama ve bildirim ayrı veri ihtiyacı. |
| KPI/grafik | Pastel ikon tile, büyük sayı, eğilim; donut/bar/line/progress | KpiCard/ChartCard, erişilebilir sayı özeti; aynı scope/dönem/payda. |
| Table/filtre | Search, filter, export, badge, action, pagination | Server state + mobil RecordList; çalışmayan filtre/export gösterilmez. |
| CRUD/detail | Yeni kayıt, kebab, sağ panel/sekme, hızlı eylem | EntityEditor/DeleteEntity; desktop panel, mobile tek dialog/detail. |
| Responsive | Yalnız desktop gösterilmiş | KPI 1/2/3 sütun, grafik alta, sidebar açılır, detay tek panel; 390/tablet/desktop test planı. |

Bina/kişi fotoğrafları, banka logoları ve farklı marka çizimleri otomatik asset olarak kopyalanmamalı. Kullanım hakları bilinmeyen görsel/font/paket eklenemez. Kullanıcının mevcut logosu korunmalı; referans fotoğrafı lisans kanıtı değildir.

## 6. Dokuz ekranın karşılaştırması

Her ekran aşağıdaki sekiz başlıkla değerlendirilmiştir.

### 6.1. Genel Bakış

Referans: [BuildingFlow Genel Bakış Paneli.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Genel Bakış Paneli.png>)

Fotoğraflı karşılama/hava durumu, altı KPI, doluluk donut'u, blok doluluk, gelir-gider, duyuru, son aidat/talep ve gider dağılımı. Üç ana bant mobilde tek sütuna inmeli.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | / foundation kartları, sağlık ve roadmap; operasyon KPI yok. |
| 2. Hedef | Karşılama + KPI grid + grafikler + son kayıt table'ları. |
| 3. Eksik UI | KpiCard, ChartCard, ActivityList, dönem/scope filtresi, duyuru özeti. |
| 4. Tekrar kullanım | Shell, MessageBanner, Skeleton, formatter, Button/Table, TR/EN. |
| 5. Frontend değişikliği | Overview feature; roadmap erişilebilir ayrı alan; rol/modül bazlı kart ve state. |
| 6. API ihtiyacı | Global bina/bölüm/doluluk/kişi, dönemli finans/duyuru/talep özetleri. Property totalCount erişilebilen alan sayısını verir; tek sayfa sayaç toplamı global KPI değildir. Hava durumu ayrı entegrasyon. |
| 7. Risk/bağımlılık | Sahte trend, kişi/daire tekrar sayımı, muhasebe rolüne PII, hazır olmayan finansı aktif gösterme. Doluluk tanımı/tarih kesiti onaylanmalı. |
| 8. Sıra | UI 02 → 03 → 04 yalnız hazır veri; eksik kartlar ilgili iş fazı/aggregate API sonrası. |

### 6.2. Yönetim Alanları

Referans: [BuildingFlow Yönetim Alanları Panosu.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Yönetim Alanları Panosu.png>)

Dört KPI, fotoğraflı üç alan kartı, checkbox/aksiyon/pagination table; search, durum/il/ilçe, görünüm, refresh, Excel/PDF ve create. Kartlar mobilde tek akışa, table ortak mobil listeye dönüşmeli.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | PropertiesWorkspace: gerçek type/konum/sayaç, server search/sort/page, kolon görünürlüğü, create/detail. |
| 2. Hedef | Card/Table switch, KPI, filtre toolbar, fotoğraf ve toplu seçim. |
| 3. Eksik UI | PropertyCard, görünüm seçici, FilterBar, fotoğraf/çoklu seçim/export. |
| 4. Tekrar kullanım | Property DTO/TanStack, Dialog, EntityEditor, StatusBadge, Skeleton, formatter. |
| 5. Frontend değişikliği | Aynı API sayfası/state ile iki sunum; mobil RecordList; tek CRUD/type kilidi akışı. |
| 6. API ihtiyacı | Mevcut CRUD görünümü için hayır. Ayrı durum/il/ilçe filtreleri, fotoğraf, kişi/ortak alan/personel özetleri ve tüm filtre sonucunun export'u için evet. |
| 7. Risk/bağımlılık | Referans Altunbayrak Rezidans'ı site/3 blok gösteriyor; mevcut kayıt apartman/doğrudan daire. Görsel adına göre gerçek type/veri değişmez. Fotoğraf hakkı ve sayaç kaynağı gerekli. |
| 8. Sıra | UI 05 önce mevcut DTO card/table/CRUD; ileri filtre/fotoğraf/export ayrı onaylı API sonrası. |

### 6.3. Daireler / Bağımsız Bölümler

Referans: [Daire Yönetim Paneli.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/Daire Yönetim Paneli.png>)

Alan başlığı/KPI, büyük table, sağda kat dağılımı, oda donut'u ve aktivite. Kolonlar no/blok/kat/3+1/m²/kişi/doluluk/güncelleme; sağ sütun mobilde alta alınmalı.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | APARTMENT direct Unit; diğer tiplerde Building seçimi/HierarchyRows. number/type/floor/grossSquareMeters/status/version. |
| 2. Hedef | Alan özeti, doluluk KPI, zengin filtre/table ve kat/oda grafiği. |
| 3. Eksik UI | Unit KPI, kat/blok/doluluk FilterBar, mobil kart, ilişki özeti, chart/toplu/export. |
| 4. Tekrar kullanım | ManagementPage, BuildingDetail, HierarchyRows, RecordList, EntityEditor/DeleteEntity ve formatter. |
| 5. Frontend değişikliği | Kalıcı Daireler/alan seçimi; apartmanda blok kolon/filtresi kaldırılır, diğer tiplerde altı bölüm türü korunur. |
| 6. API ihtiyacı | Mevcut alanların görünümü için hayır. Kişi arama, çapraz bina listeleme, kat/doluluk filtre/özeti, oda düzeni ve otopark için ayrı sözleşme gerekir. |
| 7. Risk/bağımlılık | **3+1 Unit.type değildir.** isEnabled doluluk değildir; malik kaydı fiilen oturulduğunu kanıtlamaz. PII yetkisi, negatif kat ve tarih politikası korunmalı. |
| 8. Sıra | UI 06 CRUD/mobil liste → onaylı doluluk/filtre/grafik. Section/Floor entity eklenmez. |

### 6.4. Kişiler / Sakinler

Referans: [BuildingFlow Sakinler Yönetim Paneli.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Sakinler Yönetim Paneli.png>)

Dört KPI, avatar/rol/iletişim table, sağda Genel/Daire/Aile/Notlar sekmesi. Panelde TC/doğum/cinsiyet, hızlı aidat/talep/mesaj/arama var.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | PeopleWorkspace scoped CRUD; ayrı PersonPage/PersonOccupancies, tarihli OWNER/TENANT/RESIDENT geçmişi. |
| 2. Hedef | Zengin kişi listesi, tek seçili kişi özeti, rol filtreleri ve modül eylemleri. |
| 3. Eksik UI | PersonSummaryPanel, ilişki badge grubu, KPI/filtre; fotoğraf/aile/not mevcut değil. |
| 4. Tekrar kullanım | RecordList, PersonOccupancies, kişi formları, Dialog, StatusBadge, date-only/feedback. |
| 5. Frontend değişikliği | Kişiler menüsü korunur; Sakin bir ilişki rolü. Panel/ayrı person route'u ile tek akış, mobil tek odaklı detail. |
| 6. API ihtiyacı | Mevcut CRUD/ilişki görünümü için hayır. Contact/rol/blok/daire arama-filtre, distinct rol KPI, fotoğraf/not/aile ayrı API/kapsam. |
| 7. Risk/bağımlılık | **TC kimlik alanı kurala aykırı, eklenmemeli.** Person login değildir; tek rol çoklu ilişkiyi bozar. Aile ferdi enum değil; örnek malik/kiracı/sakin toplamları distinct kişi sayısı varsayılamaz. |
| 8. Sıra | UI 06 güvenli CRUD/geçmiş → onaylı özet/filtre; finans/talep/mesaj iş fazı sonrası. |

### 6.5. Aidat İşlemleri

Referans: [BuildingFlow Aidat Yönetim Paneli.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Aidat Yönetim Paneli.png>)

Ay seçimi, tahakkuk/tahsilat/bakiye KPI, karma grafik, ödendi/kısmi/ödenmedi donut, liste ve hızlı borçlandırma/makbuz/hatırlatma. Daire/Kişi/Blok/Site sekmelerinin kapsamı ayrıca tanımlanmalı.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | FE yok; iş 05 domain/application/mapping/endpoint taslağı. DuesStore/migration/test/UI eksik; build başarısız. |
| 2. Hedef | Gerçek dönemli borç/özet; ödeme tamamlanınca tahsilat/kısmi ödeme/makbuz. |
| 3. Eksik UI | DuesWorkspace, filtreler, AmountField, toplu seçme/önizleme/onay, borç detail/history; sonra chart/receipt/reminder. |
| 4. Tekrar kullanım | Property seçimi, RecordList/TanStack, Dialog/Button/feedback, formatter, API/session, version/409. |
| 5. Frontend değişikliği | Ayrı kalıcı Aidat/Borçlar; açık seçim/hesap özeti; belirsiz ağ sonucunda aynı idempotency anahtarıyla kurtarma. |
| 6. API ihtiyacı | İş 05 store/migration/schema/API testleri ve borç/özet gerekir. Ödeme/allocation iş 06; reminder iş 09. Taslak endpoint çalışan garanti değil. |
| 7. Risk/bağımlılık | İş 05'te ödendi/kısmi/makbuz simüle edilmemeli. Backend decimal/dönem/TRY, Unit borcu, atomic/idempotent işlem ve finans geçmişi esas. |
| 8. Sıra | Ayrı onayla iş 05 tamamla/test → UI 07 borç → iş 06 tahsilat/makbuz → iş 09 hatırlatma. |

### 6.6. Muhasebe

Referans: [BuildingFlow Muhasebe Paneli.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Muhasebe Paneli.png>)

Tarih aralığı, gelir/gider/net/bakiye KPI, iki chart, transaction table, banka hesapları/hızlı işlemler. Gelir/Gider/Transfer/Fatura/Bütçe/Cari Hesap sekmeleri mevcut iş kapsamından daha geniş.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | Ekran/API yok. İş 06 ödeme ve 07 gider/bütçe planlı. |
| 2. Hedef | Dönemli gerçek gelir-gider ve yetkili işlem table/eylem. |
| 3. Eksik UI | DateRangeFilter, finans KPI/chart, FinancialRecordList, hesap/detail. |
| 4. Tekrar kullanım | RecordList/Table, amount/date format, feedback, auth/scope/version/Dialog. |
| 5. Frontend değişikliği | Ayrı feature; dönem/para birimi/tür açık, finans hesapları client'ta yeniden yapılmaz. |
| 6. API ihtiyacı | Ödeme/gider/kategori/bakiye özeti gerekir. Banka/transfer/fatura/cari hesap backend'i yok. |
| 7. Risk/bağımlılık | Tahakkuk tahsilat, transfer gelir değildir. Başlangıç bakiyesi/düzeltme/dönem kapanışı tanımsızken bakiye üretilemez. ERP genişlemesi görsele dayanarak yapılmaz. |
| 8. Sıra | İş 06–07 kuralları/API → UI 07; banka/transfer/fatura ayrı kapsam. |

### 6.7. Talep ve Arızalar

Referans: [BuildingFlow Talep ve Arıza Yönetimi.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Talep ve Arıza Yönetimi.png>)

Durum KPI/sekme, kategori/konum/öncelik filtreleri, table ve sağda timeline/sorumlu/ek/not. Örnek toplam 48, alt durumlar 12 + 18 + 34 + 3 = 67; aynı kapsamın dağılımı olarak tutarsızdır.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | Ticket model/workflow/route/API yok; MinIO sözleşmesi tamamlanmış attachment servisi değil. |
| 2. Hedef | Filter/table ve tek seçili talep detail/timeline. |
| 3. Eksik UI | TicketList, PriorityBadge, Timeline, AssigneeSelector, AttachmentList ve durum geçişi. |
| 4. Tekrar kullanım | RecordList/Dialog/Table, formatter/feedback/Property scope. |
| 5. Frontend değişikliği | Desktop sağ panel, mobile tek dialog/detail; filter/seçim/back korunur. |
| 6. API ihtiyacı | CRUD/workflow/category/priority/assignee/timeline/attachment ve yetkili özet gerekir. |
| 7. Risk/bağımlılık | Örnek sayılar kopyalanmaz; paydalar eşleşmeli. Sakin/yönetici görünürlüğü, atama/geçiş ve dosya erişim/boyut/tip kuralı önce onaylanmalı. |
| 8. Sıra | Domain/yetki/dosya kapsamı onayı → backend/test → UI 08 → overview/rapor. |

### 6.8. Raporlar

Referans: [BuildingFlow Raporlar Dashboard’u.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Raporlar Dashboard’u.png>)

Dönem seçimi, altı kategori KPI, dört chart, kategori menüsü/katalog; son çalışma, görüntüle, Excel/PDF, yeni rapor/zamanla. Mobilde kategori seçici ve tek sütun grafik.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | Route/katalog/export/scheduler yok; iş 10 planlı. |
| 2. Hedef | Gerçek katalog/dönem/scope ve yetkili export. |
| 3. Eksik UI | ReportCatalog, CategoryFilter, ExportAction/progress, PeriodSelector/job geçmişi. |
| 4. Tekrar kullanım | RecordList/Table/Search, formatter/feedback/merkezi API/auth. |
| 5. Frontend değişikliği | Yalnız çalışan raporlar; loading/error/iptal/job durumu. Hazır olmayan format/zamanlama sunulmaz. |
| 6. API ihtiyacı | Scoped rapor, gerçek Excel/PDF, gerekiyorsa job/status/download/schedule. Planın “var olan servis” varsayımı doğrulanmadı. |
| 7. Risk/bağımlılık | Tek sayfa tüm filtre sonucu diye export edilmez; PII/finans scope, yetkili download, CSV/formül girdileri ve kütüphane lisansı. |
| 8. Sıra | Kaynak iş modülleri/iş 10 sözleşmesi → UI 09; scheduler ayrıca worker gerektirir. |

### 6.9. Sistem Ayarları

Referans: [BuildingFlow Sistem Ayarları Dashboard’u.png](</Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/frontend/buildingflow-web/doc/ui-references/BuildingFlow Sistem Ayarları Dashboard’u.png>)

Yedi üst kategori, iç sidebar, merkezi form, sağda kurum/servis durumu ve sistem işlemleri. Tema/dil/SMTP/SMS/güvenlik/audit/bakım/backup/cache/restart farklı yetki ve veri sorumluluğu taşır.

| Başlık | Analiz |
| --- | --- |
| 1. Mevcut durum | Dil/cookie, kullanıcı/çıkış, API health. Settings/theme/user-role/audit/system operation API yok. |
| 2. Hedef | Ayar kategorileri/form; gerçek servis durumu ve yetkili hazır eylemler. |
| 3. Eksik UI | SettingsSections, Tabs, ThemeSwitcher, kurum formu, IntegrationStatus/AuditList. |
| 4. Tekrar kullanım | LanguageSwitcher, Input/Label/Textarea/Button, Dialog/feedback/auth/health. |
| 5. Frontend değişikliği | Görüntü tercihi ile kurum/system ayarını ayır; mobil kategori seçici; iç aside shell stilini almamalı. |
| 6. API ihtiyacı | Dil/yerel tema için iş API'si gerekmez. Kurum/settings/service/role/audit/kritik işlem için backend gerekir. |
| 7. Risk/bağımlılık | “Tüm servisler aktif”, son yedek/sürüm gerçek sayılmaz. SMTP/SMS/Keycloak admin sırları client'a gitmez. Backup/restart/cache sıradan CRUD değil; role client claim ekleyerek değişmez. |
| 8. Sıra | UI 02 tema temeli → 03 profil/dil → 10 hazır ayarlar; sistem operasyonları ayrı altyapı onayı. |

## 7. Referans sidebar'ındaki diğer menüler

Bu menülerin ayrı PNG'si yok; tam ekran hedefi biliniyor gibi kabul edilmemeli. Site ve Bloklar için mevcut “Bina / Bloklar” ismi ve apartmanda gizleme korunmalı.

| Menü | Mevcut ekran | Hedef kaynak | Eksik UI | Tekrar kullanım | FE değişikliği | API ihtiyacı | Risk/bağımlılık | Sıra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Site ve Bloklar | Alan içi liste/detail/CRUD | Sidebar + Property/Unit referansı | Mobil card/PageHeader/toolbar | ManagementPage/HierarchyRows/BuildingDetail/EntityEditor | Scoped route modernizasyonu; genel menü istenirse alan seçimi | Görünüm hayır; çapraz alan arama evet | APARTMENT blok gerektirmez; floorCount metadata | UI 06 mevcut route; UI 11 diğer menü tamamlaması |
| Ortak Alanlar | Yok | Sidebar | Tür/durum liste/form/detail | RecordList/Dialog/feedback | Onaylı yeni feature | Evet, domain yok | Rezervasyon/bakım/erişim tanımsız | Ayrı iş kapsamı → UI 11 |
| Araçlar/Otopark | Yok | Sidebar + Unit KPI | Plaka/yer/tahsis/geçmiş | RecordList/form/date | Alan içi yeni yönetim | Evet | PII/kişi-daire/tahsis tarihi | Ayrı iş kapsamı → UI 11 |
| Tedarikçiler | Yok | Sidebar | CRUD/hizmet/iletişim | RecordList/EntityEditor kalıbı | Yeni feature | Evet | Person/login/kurum ayrımı ve finans ilişkisi | Ayrı iş kapsamı → UI 11 |
| Duyurular | Yok | Sidebar + overview card | Yayın/list/detail/attachment | Dialog/feedback/RecordList | Yetkili yayın/hedef kitle | Evet, iş 08 | Yayın ve dosya scope; gönderim iş 09 | İş 08 → UI 11 → overview |
| Tanımlar | Yok | Sidebar | Kategori/tür yetkili yönetim | RecordList/form | Önce yönetilebilir tanımlar belirlenir | Evet, kullanıcı tanımlı kayıtlar için | Domain enum silmek geçmişi bozar | İlgili domain → UI 11 |
| Kullanıcılar | Keycloak auth var, yönetim UI yok | Ayarlar kullanıcı/rol sekmesi + sidebar | Davet/üyelik/rol table/form | Auth/table/dialog/feedback | Person'dan ayrı hesap ekranı | Evet, kontrollü admin/backend | Realm rolü/Property üyeliği ayrımı; admin sırrı client'a gitmez | Ayrı iş kapsamı → UI 10 |

Giriş/reset/health/not-found/error referans menüsü değildir; mevcut davranışları UI 02/03 regresyonuna dahil edilmeli. **Daireler ve yetkili Kişiler genel ekranlarda kaybolmamalı; iç içe yönetim ekranları yeniden üretilmemeli.**

## 8. Ortak bileşen planı

### 8.1. Yeniden kullanılacak kaynaklar

| Kaynak | Kullanım | Korunacak davranış |
| --- | --- | --- |
| components/ui | Button/Input/Label/Textarea/Badge/Dialog/DropdownMenu/Skeleton/Table | Variant, focus/escape/ARIA, LICENSE/NOTICE |
| management/RecordList | Desktop table/mobile card | Aynı veri/scope/page/sort |
| PropertiesWorkspace | Property create/list/detail bağlantısı | Server search/sort/page, kolonlar, type/version |
| EntityEditor/HierarchyRows | Property/Building/Unit CRUD; DeleteEntity mevcut ortak kaynakta | Required, form değerleri, submit lock, 409, soft-delete/parent |
| PeopleWorkspace/PersonPage/PersonOccupancies | Kişi ve ayrı ilişki ekranı | Çoklu malik/sakin, tarihler/geçmiş |
| PropertyScope/ManagementPage/PropertyModuleSelection/routes | Nav/alan seçimi | Kalıcı modüller, APARTMENT, canonical route |
| MessageBanner/FieldError/Notifications | Ortak mesajlar | Dört ikon/tür/renk, live region, alan hatası, recovery |
| auth/services/lib | OIDC/API/ProblemDetails/Intl | Token güvenliği, scope, TR/EN/date-only/decimal |

### 8.2. İhtiyaç oldukça eklenecek sunum bileşenleri

| Bileşen | Amaç | Sınır |
| --- | --- | --- |
| PageHeader/SectionCard | Başlık/açıklama/yetkili eylem | UI 03–05; API içermez |
| FilterBar/ListToolbar/Pagination | Tutarlı liste kontrolü | UI 05–06; yalnız gerçek server filtreleri |
| KpiCard/ChartCard | Loading/empty/error ve erişilebilir özet | UI 04; component içinde business hesabı yok |
| EmptyState/ErrorState | Mevcut state sunumunu birleştirme | İlgili ekran aşaması; yeni notification sistemi değil |
| DetailPanel | Desktop sağ, mobile tek dialog/detail | UI 06/08; EntityEditor'ı yeniden yazmaz |
| StatusBadge/PriorityBadge | İkon/metin/renk | StatusBadge mevcut; Priority domain sonrası |
| ThemePreferenceProvider/ThemeSwitcher | Light/Dark/System | UI 02 temel, 03 header; yeni paket zorunlu değil |
| ExportAction | Gerçek export/progress/iptal | UI 09; servis olmadan eklenmez |

Önerilen yapı mevcut mimariyi genişletir; boş feature/bileşen oluşturulmaz:

~~~text
src/components/
├── ui/                         # mevcut shadcn
├── feedback/                   # mevcut feedback
├── management/                 # RecordList, ihtiyaç oldukça toolbar
├── theme/                      # UI 02
├── layout/                     # UI 03 shell parçaları
└── dashboard/                  # UI 04 KPI/chart
src/features/
├── properties/hierarchy/people/navigation/roadmap/
└── overview/dues/...            # yalnız onaylı ve veri sağlayan modüller
src/services/                   # tek API client
src/auth/                       # mevcut OIDC
src/i18n/
src/lib/
~~~

People altındaki genel useRows/useSearch bina seçiminde de kullanılıyor; ortak liste refactor'unda feature bağımlılığı sadeleştirilebilir. API modüllerini services/api altında ayırmak Convention hedefidir; Aşama 01'de dosya taşınmadı. PropertyScope ve detail ekranlarının aynı kaydı ayrı yüklemesi ileride tek kapsamlı veri kaynağıyla azaltılabilir; Query kurulu varsayılmamalı.

## 9. UI Aşama 02 — kesin dosya bazlı uygulama planı

**Planlıdır, uygulanmamıştır.** Onay verilirse yalnız token/typography/icon ve Light/Dark/System temeli ele alınır. Shell düzeni, yeni dashboard, finans store/migration ve ekran dönüşümü kapsam dışıdır.

| Dosya | İşlem | Kabul kriteri |
| --- | --- | --- |
| frontend/buildingflow-web/src/app/globals.css | background/surface/text/border/primary/status/sidebar/chart token; dark palette; type/radius/shadow/spacing/focus; global shell selector sınırı | Mevcut açık tema korunur; dark form/table/feedback okunur; iç aside shell layout almaz |
| frontend/buildingflow-web/src/components/theme/ThemePreferenceProvider.tsx — yeni | Küçük proje içi üç mod sağlayıcı; system değişimi/ilk render; auth/business state'ten ayrı | Tercih/ilk render uyumlu, hydration hatası yok; localStorage kullanılırsa yalnız tema tercihi |
| frontend/buildingflow-web/src/components/theme/ThemeSwitcher.tsx — yeni | Mevcut Button/DropdownMenu/Lucide ve TR/EN | Klavye/focus/selected; header yerleşimi UI 03 |
| frontend/buildingflow-web/src/app/layout.tsx | Tema/ilk render entegrasyonu; Intl/Notification korunur | Locale cookie/html lang/OIDC ve server-client sınırı değişmez |
| frontend/buildingflow-web/src/components/ui/button.tsx, badge.tsx, input.tsx, textarea.tsx, table.tsx, dialog.tsx, dropdown-menu.tsx, skeleton.tsx | Yalnız gerekli semantic/focus/disabled/error uyumu | Davranış/API imzası ve Dialog/Lisans korunur |
| frontend/buildingflow-web/src/components/feedback/MessageBanner.tsx, Notifications.tsx | Gerektiğinde dark kontrast | Dört ikon/tür, assertive/polite, kalıcılık ve 409 korunur |
| frontend/buildingflow-web/messages/tr.json, en.json | Tema seçenek/ARIA anahtarları | Eş anahtar/ICU; kullanıcı verisi çevrilmez |
| frontend/buildingflow-web/tests/standards.test.mjs | Anlamlı tema tercih/fallback ve i18n sözleşmesi kontrolleri | Mevcut 11 test korunur; CSS satırını aynalayan test yok |
| docs/frontend-rules.md, docs/TESTING.md, docs/SKILLS_AND_AGENTS.md | Önce senaryolar, sonra uygulanan standart ve gerçek sonuç | Kurulmamış paket hazır gösterilmez; agent/skill ve test sayısı doğru |

Kodlama öncesi docs/TESTING.md'ye yazılacak senaryolar:

- Light/Dark/System ilk açılış, yenileme, sistem değişimi, tercih devamlılığı ve hydration.
- TR/EN seçim/ARIA; OIDC login/dönüş/logout ve mevcut dil.
- Input/placeholder/disabled/focus; dört mesaj ve form hata kontrastı.
- Property/People listesi, create/edit/delete Dialog, 409 güncel kaydı yükle.
- 390 px mobil, tablet/desktop, EN uzun metin ve klavye; yeni canlı kabul kanıtları.
- npm test, npm run typecheck, npm run build.
- Manifest/lockfile değişikliği; yeni paket gerçekten gerekiyorsa resmi sürüm lisansı/NOTICE/envanter ve install sonrası lisans kontrolü.

Sistem fontu/Lucide ile başlanmalı. Yeni font, Recharts, RHF/Zod/Query veya bütün shadcn bileşenlerini toplu kurmak Aşama 02'nin gereği değildir.

## 10. Aşamalı geliştirme sırası

PHASES.md korunabilir; API'si olmayan parça ayrı backend onayı gerektirir veya ertelenir. UI aşaması aynı numaralı iş fazı değildir.

| UI aşama | Teslim | Bağımlılık |
| --- | --- | --- |
| 01 | Bu analiz | **Tamamlandı; uygulama değişmedi** |
| 02 | Token/type/icon/tema temeli | Sonraki onay noktası; iş API'si gerekmez |
| 03 | Sidebar/header/breadcrumb/dil/profil/responsive | Kalıcı Daireler/Kişiler/scope korunur. Menü araması ile business global search farklı; global search/bildirim API gerekir |
| 04 | Overview | Gerçek hazır veri. Eksik özet açık empty state; bilinmeyen değer sıfır değildir |
| 05 | Property card/table | Mevcut CRUD ile görünüm; filtre/fotoğraf/export ayrı API |
| 06 | Unit/People/Building | Çalışan iş 03–04, mobil liste; role/type/history kuralları |
| 07 | Aidat/Muhasebe | İş 05 taslak engeli, 06 tahsilat, 07 gider; bundan önce tam finans paneli bitmiş sayılamaz |
| 08 | Talep/Arıza | Domain/yetki/attachment kapsamı ve backend önce |
| 09 | Raporlar | İş 10 ve kaynak modüller; export/job servisi yok |
| 10 | Settings/Users | Tema/dil hazır oldukça; kurum/role/system backend ayrı |
| 11 | Diğerleri | Building mevcut; ortak alan/otopark/tedarikçi/tanım yeni iş kapsamı, duyuru iş 08 |
| 12 | Son kalite kontrol | Her aşamada regresyon + responsive/a11y; lint gerçekten kurulduğunda |

Öncelikli risk/kararlar:

1. **Backend build engeli:** İş 05 taslağı ayrı devam onayıyla store/migration/schema/domain/API testleri ve UI'ye kadar tamamlanmalı. Bu analizde onarım yapılmadı.
2. **Stil kapsamı:** Global selector/dağınık renk/eşikler UI 02'de sınırlanmalı.
3. **Navigation:** Shell dönüşümünde apartman ve kalıcı modül menüleri kaybolmamalı.
4. **Aggregate tanımı:** Doluluk/distinct kişi/dönem/bakiye/trend backend scope sözleşmesine dayanmalı; sayfa toplamı tüm kayıt değildir.
5. **PII/model çatışması:** TC eklenmemeli; rol/login/finans sorumluluğu ayrı.
6. **Genişleme:** Banka/fatura/cari hesap/scheduler/admin/kritik sistem işlemi sırf görselde var diye kapsam içine alınmamalı.

## 11. Çalıştırılan kontroller ve çalışma ağacı

### 11.1. Gerçek kontrol sonuçları

| İşlem | Yer/komut | Sonuç |
| --- | --- | --- |
| Keşif/kaynak okuma | rg, rg --files; kök/FE/BE/plan/docs | Mimari/sürüm/route/auth/CRUD/iş kuralı envanteri |
| Görsel inceleme | 9 ui-references PNG + önceki 2 menü screenshot | **9/9 incelendi**; yeni browser testi değil |
| FE test | frontend/buildingflow-web: npm test | **11 geçti; 0 başarısız/atlanan** |
| FE build | Aynı klasör: npm run build | **Başarılı, exit 0** |
| FE TypeScript | Aynı klasör: npm run typecheck | **Başarılı, exit 0** |
| BE build | backend: /tmp/buildingflow-dotnet/dotnet build BuildingFlow.sln --no-restore --verbosity minimal; mevcut DOTNET_ROOT/CLI_HOME/NUGET_PACKAGES ile | **Başarısız, exit 1; CS0246 DuesStore, DependencyInjection.cs:15; 0 uyarı/1 hata** |
| Backend/Keycloak/DB kabul | Bu tur çalıştırılmadı | Önceki testler yeni başarı sayılmadı; DB/schema/migration işlemi yok |
| Bağımlılık/lisans | package.json/lockfile başlangıç karşılaştırması | Yeni paket/asset yok. npm ci/dotnet restore yok; lisans envanteri yeniden yazılmadı. Önceki 312 kontrolü tarihi kayıt |
| Kaynak koruma | 182 dosyanın başlangıç SHA-256 karşılaştırması; build/cache hariç | Başlangıç kaynak/config/prompt/görseller/korunan belgeler aynı; .env içeriği yazdırılmadan dahil |
| Rapor doğrulaması | Yerel bağlantılar, görsel/başlık envanteri ve git diff --check | **20 bağlantı mevcut; 9 görselin her birinde 8 karşılaştırma başlığı; boşluk kontrolü başarılı.** Git başlangıcına göre tek yeni girdi docs/analysis/; mevcut status girdileri aynı. |

11 FE testi sözlük eş anahtar/ICU, kullanılan çeviri parametreleri, locale fallback, bozuk/bilinmeyen ProblemDetails güvenliği, bilinen alan/409 çevirileri, tarih/sayı/para/yüzde formatter, dil değişiminde feedback ve canonical/legacy/modül OIDC whitelist'ini kapsar. Tam CRUD browser/E2E veya tüm responsive ekran testi değildir.

### 11.2. Git başlangıcı ve önceki taslak

Başlangıç temiz değildi: **36 takip edilen değişiklik/silme + 78 untracked dosya/klasör girdisi = 114 status satırı**. Untracked klasör girdisi dosya sayısı değildir. Bu değişiklikler analiz üretimi sayılmaz; kullanıcı çalışması geri alınmadı/stage edilmedi, commit/push yapılmadı.

Takip edilen değişiklikler .env.example/.gitignore/README, Api auth/current-user/error/Program/appsettings, Application abstractions, eski Apartment/Block/Site silmeleri, Infrastructure DbContext/DI/mapping/snapshot, persistence/domain tests, Compose/realm, core/API/architecture docs, FE Docker/config/package/lock/CSS/layout/auth/tsconfig/eski page silmesi ve smoke/start/origin scripts içeriyordu.

Untracked gruplar .vscode/AGENTS, Property/Building/Unit/Person/Occupancy domain-service-endpoint-store-tests, yeni migrations, Keycloak theme, FE UI/features/routes/i18n/tests, UI planı/referanslar, test/API/migration/license dokümanları ve scripts idi. Analiz yalnız Git'teki ilk skeleton üzerinden yapılmadı.

Önceki iş 05 taslağının ayırt edici dosyaları:

- backend/src/BuildingFlow.Domain/DuesAssessment.cs
- backend/src/BuildingFlow.Application/Dues.cs
- backend/src/BuildingFlow.Infrastructure/DuesConfigurations.cs
- backend/src/BuildingFlow.Api/DuesEndpoints.cs
- DbContext, DependencyInjection ve Program içindeki aidat kayıtları.

DuesStore, aidat migration/snapshot teslimi, finans testleri ve FE aidat ekranı tamamlanmadı. docs/TESTING.md/roadmap iş 05 kutuları planlı/boş; bu analizde tiklenmedi.

Önceki docs/TESTING.md kayıtlarında 171 backend, 17 auth ve 262/312 lisans kontrolleri tarihi teslim kanıtıdır. Eski “i18n planlı” satırları kendi faz tarihine aittir; güncel manifest/Frontend Convention next-intl'in uygulandığını gösterir. Grafana görüntüleme katmanıdır; Loki/Prometheus/OpenTelemetry hazır entegrasyon sayılmadı.

### 11.3. Türkçe konsol/işlem kaydı

Anlamlı adımlarda şu düzen kullanıldı:

~~~text
[BuildingFlow][PHASE 01]
İncelenen: dosya veya klasör
Analiz: gerçek davranış ve veri kaynağı
Eksiklik: uygulama/görsel farkı veya doğrulanmamış özellik
Öneri: mevcut kurallara uygun aşamalı çözüm
Sonuç: analiz / başarılı kontrol / başarısız kontrol / planlı
~~~

Adımlar: prompt/kural keşfi; FE sürüm/bileşen/route; BE domain/API/auth; gerçek klasör ve 9/9 referans; mevcut menü kanıtları; FE test/build/typecheck; BE CS0246; kaynak koruma ve rapor. Paketlerin özgün çıktısı ayrıca alındı; başarısızlık gizlenmedi.

## 12. Teslim ve onay noktası

- [x] **Mimari, sürüm, route/API/auth, ortak bileşen ve iş kuralları incelendi.**
- [x] **9/9 referans ve diğer sidebar menüleri karşılaştırıldı.**
- [x] **Ortak bileşen, UI 02 dosya planı ve sonraki aşama bağımlılıkları hazırlandı.**
- [x] **FE 11 test + typecheck + build geçti; backend engeli raporlandı.**
- [ ] UI Aşama 02 uygulaması — **onay bekliyor**.
- [ ] İş Faz 05 taslağını tamamlama — ayrı geliştirme kapsamı; analizde sürdürülmedi.

Analiz sonrası durulmuştur. **UI Aşama 02 onayı olmadan tasarım veya uygulama kodu değişikliğine geçilmez.**

Sonraki çalışma kaydı — 2026-10-09: Kullanıcı **UI Faz 06 ile devam edilmesini** istedi. Bu Aşama 01 raporu tarihsel keşif/karşılaştırma kaydıdır; sonraki uygulama ve güncel sınırlar [UI_STAGE06_REPORT.md](UI_STAGE06_REPORT.md) ile [TESTING.md](../TESTING.md) içinde izlenir. UI 02–05 topluca tamamlanmış sayılmaz.
