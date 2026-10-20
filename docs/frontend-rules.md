# BuildingFlow Frontend Convention

Güncelleme: 2026-10-08. Kapsam: `frontend/buildingflow-web` ve bu arayüzü etkileyen API sözleşmeleri. Kullanıcının paylaştığı 17 başlık, mevcut BuildingFlow mimarisine uyarlanmıştır. Yeni ekran ve ilgili refactor öncesinde bu belge okunur; kullanıcı talebi ve kök [AGENTS.md](../AGENTS.md) önceliklidir.

Bu dosya geliştirme standardıdır. Bir paketin burada adının geçmesi onun kurulduğunu veya bir özelliğin tamamlandığını göstermez. Bugünkü uygulama ile hedef altyapı aşağıda ayrı kaydedilir. Toplu ekran dönüşümü, bütün paketlerin kurulması veya yeni servis açılması bu belge değişikliğinin kapsamında değildir.

## 1. Dil / i18n

- Hedef diller `tr` ve `en`, varsayılan `tr`; dil seçimi tek locale kaynağından yapılır. Bayrak tek başına dil adı yerine kullanılmaz; Türkçe / English erişilebilir adları bulunur.
- Hedef sağlayıcı `next-intl`; sözlükler frontend kökünde `messages/tr.json` ve `messages/en.json`. Çeviri anahtarları aynı olmalı; UI, form hatası, dialog, notification ve erişilebilir adlar bu sözlüklerden gelir. Yeni çeviri altyapısında ekran içine ayrı Türkçe/İngilizce koşulları yazılmaz.
- Anahtar örnekleri: `common.save`, `common.cancel`, `common.delete`, `property.title`, `property.create`, `property.type`, `validation.required`, `errors.propertyNotFound`. Çeviri interpolasyonu kullan; cümle parçalarını birleştirme.
- Kullanıcının girdiği isim/adres, API enum/alan isimleri ve teknik route'lar çevrilmez. Keycloak giriş/reset ekranı da seçilen locale ile uyumlu olmalı.
- i18n kurulumu mevcut OIDC redirect ve dönüş rotalarını korumalı. Locale route kararı, whitelist, eski bağlantılar ve TR/EN kabul testleri birlikte uygulanır; yalnızca `/tr` öneki ekleyerek oturum akışı değiştirilmez.
- Uygulandı: next-intl 4.14.9, eş anahtarlı TR/EN sözlükleri, root sağlayıcısı, doğrulanmış `buildingflow-locale` HttpOnly/SameSite cookie ve ortak LanguageSwitcher. Mevcut adreslere dil öneki eklenmez. Yönetim ekranları, yol haritası, form/alan hataları ve Keycloak özel metinleri çevrilir; kullanıcı verisi çevrilmez.

## 2. Ortak UI / Design System

- Kullanıcının seçimi **shadcn/ui + Tailwind CSS + TanStack Table** korunur. Önce `src/components/ui`, mevcut feature bileşenleri ve LICENSE/NOTICE incelenir; aynı amaç için yeni button/modal/table sistemi eklenmez.
- Button, Input, Select, Checkbox, DatePicker, Badge, Tooltip, Dialog/Modal, Drawer, Tabs, Card, DataTable, Pagination ve SearchInput ihtiyaç ortaya çıktığında ortaklaştırılır. Hepsi için kullanılmayan boş bileşen üretme.
- Ortak sunum bileşeni iş domain'i veya doğrudan API erişimi taşımaz. Feature modülü veri/iş akışını, UI bileşeni görünümü ve etkileşimi yönetir.
- İkonlar mevcut lisanslı Lucide kaynağından; ikon düğmeleri erişilebilir ad taşır. Dialog odak yönetimi, klavye ve kapatma davranışı korunur. Lisanssız template/font/görsel eklenmez.

## 3. CRUD ekranları

- Liste düzeni: başlık ve açıklama → yetkili Yeni Kayıt → arama / uygulanmış filtreler / Yenile → tablo veya mobil liste → sayfalama. Kullanılmayan, çalışmayan filtre düğmesi eklenmez.
- Kolonlar ilgili ad/numara, tür, durum ve Detay/Düzenle/Sil işlemlerini aynı dil ve yerleşimle sunar. İşlem görünürlüğü gerçek yetkiyle, kapalı işlem açıklaması gerçek durumla belirlenir.
- Pagination, arama ve sıralama sunucuda uygulanır; yalnızca alınan bir sayfayı tüm listeymiş gibi filtreleme. Arama/filtre değişince sayfa 1'e dön; izinli sort alanları, kararlı sıralama ve mevcut API sayfa limitleri korunur.
- Mutation sonrası liste, detay ve üst kaydın version/sayaç bilgisi güncellenir. Başarı bildirimi yalnızca başarılı API yanıtından sonra gösterilir.
- Genel isim **Yönetim Alanları / Property**. Apartman doğrudan Daireler; Site/Rezidans/Karma yapı Bina/Blok → Bağımsız bölümler. Sahte blok veya boş daire üretme.

## 4. Formlar

- Hedef form altyapısı **React Hook Form + Zod**; ortak form şemaları ve sunucu alan hatası eşlemesi kullanılır. Client validation kullanıcı deneyimidir; backend validasyonu ve domain kuralları zorunludur.
- Alan etiketi/`*`, sağ üst hata bildirimi ve ekran okuyucu alan açıklaması, `aria-invalid`/`aria-describedby`, ilk hatalı alana odak, server hata özeti ve Kaydet yüklenme durumu tutarlıdır. İptal/Kaydet sırası ve loading sırasında gönderim kilidi bütün formlarda aynıdır.
- İptal değişiklikleri kaydetmez. Başarısız istek form değerlerini korur. 409 otomatik üzerine yazılmaz; **Güncel kaydı yükle** güncel version ve alanları getirir, kullanıcı yeniden kaydeder.
- Sayılar/ondalıklar açık şekilde parse edilir. Boş isteğe bağlı sayı `null` olur; bodrum katı negatif olabilir, alan pozitif ve en fazla iki ondalıklıdır. Yerelleştirilmiş virgül/nokta girişinin kabul kuralı ortak şemada belirlenir ve test edilir.
- Audit/ID/ebeveyn alanları formdan yetkili veri gibi gönderilmez. Güncelleme/silmede son okunan version kullanılır. Tür alanı `canChangeType` ile açıklamalı kilitlenir; backend son karar verir.
- RHF/Zod henüz kurulu değil. Kurulum ve ortak form geçişi ayrı, lisans doğrulanmış değişikliktir; o zamana kadar mevcut `EntityEditor` / oluşturma formu davranışlarını koru, üçüncü bir form yaklaşımı açma.

## 5. Merkezi API katmanı

- İş component'i içinde bağımsız `fetch`, token/refresh veya hata ayrıştırma yazılmaz. Bugünkü `src/services/api.ts` / `useAuthorizedApi` genişletilir; aynı sorumlulukta ikinci API client kurulmaz.
- Feature API modülleri ortak client üzerinde `getAll/getById/create/update/delete` gibi typed işlemler sunar. Hedef konum `src/services/api/` altında client ve property/building/unit modülleridir; klasör değişimi atomik yapılır, iki paralel altyapı bırakılmaz.
- Ortak client: API base URL, Authorization, session refresh, iptal/timeout, HTTP hata ayrıştırma ve mevcutsa request/trace ID. Token session sağlayıcısından alınır; interceptor başka token deposu açmaz.
- Unmount/arama değişiminde eski istek iptal edilir veya eski yanıtın yeni ekranı ezmesi önlenir. Abort, kullanıcıya sunucu hatası olarak gösterilmez. Timeout ile iptal ayrı tanınır.
- POST/PUT/DELETE ağ hatası veya timeout sonrası körlemesine otomatik tekrar edilmez; double-submit kilidi idempotency garantisi değildir. GET retry sınırı ve gerekçesi belirlenir.
- Sağlık kontrolü server-only `src/services/health.ts` üzerinden kalabilir; kimlik sağlayıcısının protokol çağrıları resmi adapter'da kalır. Gizli değerler `NEXT_PUBLIC_*` içine konmaz.

## 6. Authentication / Authorization

- Keycloak Authorization Code + S256 PKCE; password grant yok. Token/parola/reset linki log, Git, localStorage veya sessionStorage'a yazılmaz. SessionStorage'da sadece whitelist yerel dönüş rotası bulunabilir.
- Bugünkü roller: `SUPER_ADMIN`, `SITE_ADMIN`, `MANAGER`, `ACCOUNTANT`, `RESIDENT`. Paylaşılan örnekteki `ADMIN`/`STAFF` mevcut rol değildir; doğrulanmadan role alias veya izin eklenmez.
- Permission hedefi: `PROPERTY_VIEW/CREATE/UPDATE/DELETE`, `BUILDING_VIEW/CREATE/UPDATE/DELETE`, `UNIT_VIEW/CREATE/UPDATE/DELETE` ve ilgili modül izinleri. `Can` / permission helper bütün UI kontrollerinin ortak noktası olur; token'da var olmayan permission uydurulmaz.
- Mevcut rol + aktif PropertyMembership ve global SUPER_ADMIN istisnası korunur. Bir global rol, yönetim alanı erişimini kendiliğinden genişletmez. Backend hem işlem iznini hem veri kapsamını doğrular.
- Ayrıntılı permission grant/claim/API altyapısı henüz yok. UI permission katmanı eklenirse mevcut backend policy ile aynı sonuç üretmeli; yalnızca düğmeyi gizlemek yetkilendirme sayılmaz.
- Hassas kayıtlar anonim SSR içine konmaz. Server-side business fetch eklenirse önce güvenli server session, veri kapsamı ve kullanıcılar arası cache izolasyonu sağlanır.

## 7. Tema

- Hedef **Light / Dark / System**. Renkler ortak semantic token'larla tanımlanır: primary, secondary, success, warning, danger/destructive, background, surface, border, text-primary, text-secondary.
- Component başına ayrı hex palette oluşturma. Yeni/yenilenen ortak bileşenlerde mevcut semantic token'ları kullan; ham renk gerektiğinde ortak tema dosyasında gerekçeli tanımla.
- System tercihi ve SSR/hydration uyuşması test edilir; aynı sayfada ilk render ile client tema değeri tutarlı olmalı. Kontrast, odak görünürlüğü, disabled/error durumları iki temada incelenir.
- Mevcut açık tema ve kısmi CSS token'ları üç mod desteği değildir; tema sağlayıcısı ve dark dönüşümü planlıdır.

## 8. Notification / mesajlar

- Hedef tek `notification.success/error/warning/info` mekanizması; çeviri anahtarları ve erişilebilir live region üzerinden çalışır. Alan hatası metni sr-only açıklamada kalır; görsel hata sağ üstte gruplandırılır. aria-invalid/aria-describedby ve ilk hata odağı korunur.
- Liste/detail başarı mesajları aynı terminolojiyi kullanır; yinelenen toast üretme. Uzun süren işlem sonucu, kurtarma eylemi ve server error kullanıcıya anlamlı şekilde sunulur.
- Uygulandı: `components/feedback/MessageBanner`, `FieldError` ve `NotificationProvider/useNotification`. Başarı CircleCheck, hata CircleX, uyarı TriangleAlert, bilgi Info; semantic renk ve yerel tür başlığı zorunlu. Hata assertive/alert, diğerleri polite/status. Tüm toastlar 8 saniye görünür, hover/odakta süre durur. Alanın ekran okuyucu açıklaması düzeltmeye kadar korunur; 409 güncel kayıt/yenileme eylemi korunur. Toast ile aynı inline başarı ikinci kez duyurulmaz.

## 9. Loading / Empty / Error

- Her veri ekranı loading, dolu başarı, boş başarı ve hata durumlarını açık modeller. Sıfır kayıt ile sunucuya ulaşamama aynı görünüm değildir.
- Ortak Skeleton/EmptyState/ErrorState/Retry kullan; boyutları layout kaymasını azaltır. Boş durumda ilgili ve yetkili ilk kayıt eylemi görünür, yetkisiz kullanıcıya çalışmayan oluşturma düğmesi sunulmaz.
- Yenilemede mevcut verinin stale olduğuna ilişkin davranış belirlenir; hatalı yeni response eski kaydı başarılı diye sunmaz. Request iptali ve yeni parent ID'ye geçiş test edilir.
- Mevcut loading/empty/error senaryoları vardır; bütün ekranları kapsayan ortak state bileşenleri henüz tamamlanmış sayılmaz.

## 10. Silme / kritik işlemler

- Sil ikonundan doğrudan mutation yapılmaz; kayıt adı/numarası ve somut sonucu gösteren ortak confirm dialog açılır. Vazgeç hiçbir değişiklik yapmaz; gönderim sırasında ikinci silme engellenir.
- BuildingFlow silmesi **soft-delete**; mesajda listeden kaldırılma ve geçmişin saklanması belirtilir. Uygulamanın geri yükleme ekranı olmadan kullanıcıya geri alma düğmesi vaat edilmez; örnekteki genel “geri alınamaz” ifadesi her silmeye kopyalanmaz.
- Silinmemiş alt kaydı bulunan üst kayıt silinemez, pasif üstte alt yazma engellenir. Kısıt backend'de doğrulanır; yalnızca eldeki bir sayfadan çocuk sayısı tahmin edilmez.
- İlişkili tür dönüşümü ayrı iş akışıdır. Sıfır görünen sayaç tür değişimi için yeterli değildir: pasif/silinmiş bina veya Unit geçmişi de kilittir. API `canChangeType` ve expected version korunur.

## 11. Tarih / saat / sayı / para

- Ortak `formatDate`, `formatDateTime`, `formatNumber`, `formatCurrency`, `formatPercent` locale ve açık timeZone alır. Aynı format politikası component'lere dağıtılmaz.
- API tarihleri ISO 8601 UTC/offset ile taşınır; görüntüleme varsayılanı mevcut Europe/Istanbul politikasıdır. Date-only değerler UTC dönüşümüyle bir önceki/sonraki güne kaydırılmaz.
- TR/EN formatı Intl temelli, para birimi açık `TRY` (gerekirse API'deki currency); percent için `0.18` → `%18` / `18%` sözleşmesi tanımlıdır. Locale ile domain değeri değişmez.
- Finansal hesap backend'in decimal/yuvarlama politikasından gelir; frontend floating-point formatı finansal doğruluk sağlamaz. Büyük decimal DTO/parsing sözleşmesi finans modülünde tanımlanır.
- `lib/format.ts`: formatDateOnly/date/dateTime/number/currency/percent ve Europe/Istanbul businessToday uygulanmıştır. Mevcut tarih ve alan gösterimleri seçili locale kullanır. Native date input'un takvim düğmesi tarayıcı/işletim sistemi dilini izleyebilir; uygulama etiketleri ve kaydedilen ISO değer locale'den bağımsızdır.

## 12. Responsive / erişilebilirlik

- Breakpoint'ler kullanılan Tailwind sürümü/tema kaynağından alınır; component başına ayrı eşikler oluşturulmaz. Mobile/tablet/desktop/large desktop için sınırları ortak tema değişikliğinde kaydet.
- Hedef desktop Table, mobil Card/List; aynı API sayfa/kapsam/sıralama ve işlem izinleri korunur. İnce ekran için tabloyu sadece küçültmek varsayılan çözüm değildir.
- UI 06 sonrası kişi/ilişki ve HierarchyRows bina/daire listeleri ortak RecordList ile masaüstü Table / mobil Card sunar. Ana Property listesi kendi TanStack tablosunu korur; onun ortak liste geçişi ayrı kapsamdır.
- En az 390 px mobil, tablet ve desktop görünümü; klavye, label, dialog odak/escape, hata duyurusu ve ikon düğmelerinin erişilebilir adları doğrulanır. Renk tek bilgi kaynağı olmaz.

## 13. URL / Route

- Teknik kavramlar `properties/buildings/units`. Bugünkü UI: `/properties`, `/properties/{propertyId}`, `/properties/{propertyId}/buildings/{buildingId}`, `/properties/{propertyId}/people`. Genel bilgiler, bina/blok, daire/bölüm ve kişiler ayrı menü/sayfalardır; seçili yönetim alanının adı menüde gösterilir. Apartmanda Bina/Blok menüsü gizlenir; bölüm menüsü doğrudan dairelere gider.
- API: `/api/v1/properties`, property altında `/buildings` ve doğrudan `/units`; bina altında `/units`. UI route ile API route birbirinin yerine kullanılmaz; [güncel API sözleşmesi](api/hierarchy.md) izlenir.
- Daireler ve yetkili kullanıcı için Kişiler menüleri alan seçilmediğinde de görünür; `/units` ve `/people` ortak property listesini sunucu arama/sayfalama/sıralamasıyla alan seçimi için kullanır. Seçim doğrudan ilgili modüle gider, tüm alanların kişisel verisini tek genel listeye getirmez. Aktif menü ana modül rotasında da korunur.
- Uygulanan ayrı sayfalar: `/properties/{id}/buildings`, `/properties/{id}/units`, `/properties/{id}/buildings/{buildingId}/units`, `/properties/{id}/people/{personId}`. Çok binalı alanda bölüm menüsü sunucuda arama/sayfalama yapan bina seçimi sunar. Kişi listesi ilişkileri kendi sayfasına bağlar; liste içine ikinci ekran açmaz. `/dues`, `/payments` ve `/expenses` finans alan seçimi ve property-scope sayfaları uygulanmıştır. `/residents` ve `/reports` planlıdır.
- Eski `/sites` bağlantıları canonical yollara redirect eder. ID GUID ve parent uyumu doğrulanır. Next.js kurulu sürümünün async params/searchParams kurallarını oku.
- URL state'e token, auth code, parola veya kişisel hassas arama koyma; query state whitelist ve uzunluk/sayfalama limitleriyle parse edilir. Yeni canonical/locale route OIDC dönüş whitelist'i ve testleriyle birlikte güncellenir.

## 14. State yönetimi

- Server state hedefi **TanStack Query**, form state **React Hook Form**, paylaşılabilir URL state **Next.js searchParams**, authentication mevcut Auth Provider/session. Global UI için **Zustand yalnızca ihtiyaç varsa**; Redux/global store'a her veri konmaz.
- Query key yönetim alanı/bina/sayfa/filtre/sort ile oturum/veri kapsamını içerir. Mutation ilgili list/detail/parent sayaç ve version cache'lerini invalidation ile yeniler.
- Logout veya kullanıcı değişiminde business cache temizlenir; kullanıcılar arasında stale hassas veri gösterilmez. Başka tenant/property verisi aynı key altında paylaşılmaz. SSR ortak cache'e kullanıcıya ait sonuç konmaz.
- Query devtools/loglarına token/hassas veri bırakılmaz. Şu an TanStack **Table** vardır, TanStack **Query** yok; RHF/Zustand da kurulu değil. Mevcut useEffect/AbortController/state davranışı planlı geçiş yapılana kadar korunur.

## 15. TypeScript / modül yapısı

- `strict: true` korunur. `any`, `@ts-ignore` veya güvenli olmayan cast ile sözleşme hatası saklanmaz; belirsiz dış veri `unknown` olarak doğrulanır/narrow edilir.
- API Response, CreateRequest, UpdateRequest, FormData ve UI model gerektiğinde ayrı tiptir. Tek geniş DTO ile bütün mutation alanları açılmaz; whitelist body oluşturulur.
- Feature-based `src/features/{properties,hierarchy,...}`; tekrar eden sunum `components`, business API `services`, ortak saf utility `lib`, kimlik `auth`. Kullanılmayan klasör/abstraction ekleme.
- Next.js Server/Client sınırı açık; client boundary ihtiyaç kadar dar tutulur. Server-only env/modüller client bundle'a taşınmaz; props serileştirilebilir ve güvenli olmalıdır. Lisanslı ve mevcut font/ikon/asset kaynağı korunur.

## 16. Hata yönetimi

- Mevcut sözleşme ASP.NET **ProblemDetails / ValidationProblemDetails** (`status`, `title`, `errors`); ortadan kaldırılıp bağımsız bir hata JSON'u açılmaz.
- Machine-readable `code` uygulandı; backend/frontend sözleşmesi birlikte sürümlenir. `traceId` henüz ortak endpoint garantisi değildir. Uygulanan kodlar ErrorCatalog ve [hata sözleşmesi](api/errors.md) içindeki BF_* envanteridir; code/message/parameters/severity ve coded fieldIssues ProblemDetails extension olarak korunur.
- Client bilinen code'u çeviri anahtarına eşler; bilinmeyen/olmayan code için güvenli locale fallback kullanır. Stack trace, ham exception, token veya DB bağlantısı kullanıcıya/loga taşınmaz. Kullanıcı düzeltme adımı görebilir.

| HTTP | Ortak davranış |
| --- | --- |
| 400 | Alan hatalarını form modeline bağla; formu koru |
| 401 | Ortak session katmanı üzerinden oturumu yenile/yeniden giriş; yönlendirme döngüsü yok |
| 403 | Yetkisiz işlem; otomatik logout veya yetki genişletme yok |
| 404 | Bulunamadı/erişim kapsamı mesajı; başka kullanıcı verisini ifşa etme |
| 409 | Çakışma/domain kısıtı; uygun güncel kayıt/yenileme eylemi; kör overwrite yok |
| 500 / ağ / timeout | Güvenli sistem/bağlantı hatası; uygun retry/iptal eylemi |

- Beklenen API/domain hatası ile beklenmeyen render exception ayrılır. Gerektiğinde App Router `error.tsx`, `not-found.tsx`, `loading.tsx` sınırları ilgili segmentte kullanılır; form ValidationProblemDetails render crash'e dönüştürülmez.

## 17. Log / Audit / Trace

- Audit backend'de trusted JWT sub, değişmez created alanları, modified alanları ve version üzerinden yazılır; frontend createdBy/modifiedBy belirlemez.
- Request/trace/correlation ID mevcutsa merkezi client üzerinden taşınır ve güvenli hata desteğinde sunulur. Aynı amaç için birbirinden kopuk ID sistemi ekleme; header/CORS sözleşmesi ve OpenTelemetry entegrasyonu birlikte ele alınır.
- Token, Authorization, parola, sıfırlama linki, auth callback query/hash ve hassas kişi/finans verisi loglanmaz. Tam request/response body veya JWT console'a dökülmez.
- Grafana görüntüleme katmanıdır; Loki/Prometheus/OpenTelemetry entegrasyonu henüz tamamlanmış gibi raporlanmaz. UI işlem geçmişi backend audit'iyle tutarlı olmalı.

## Bugünkü uygulama ve planlı altyapı

| Alan | Mevcut | Sonraki standart çalışması |
| --- | --- | --- |
| Next.js / TypeScript | App Router, strict açık | Yeni sürümde ilgili yerel rehberi oku |
| UI / tablo | shadcn/ui, Tailwind, TanStack Table; People/HierarchyRows/finance RecordList masaüstü tablo/mobil kart | Ana Property listesinin ortak liste/state geçişi |
| Kimlik / yetki | Keycloak PKCE, rol + aktif üyelik, backend scope | Merkezi permission UI ve gerekirse doğrulanmış grant sözleşmesi |
| CRUD / form | Ortak EntityEditor/HierarchyRows, version/409, field error | Lisansı doğrulanmış RHF + Zod geçişi |
| API | useAuthorizedApi, typed DTO; unknown ProblemDetails doğrulaması, bilinen mesaj allowlist ve güvenli TR/EN fallback | Feature API modülleri, timeout/request ID ve yeni hata kodu/parametrelerin ortak registry’ye eklenmesi |
| Dil / format | next-intl, TR/EN sözlük/cookie/seçici; ortak tarih/sayı/para/yüzde formatter | Yeni ekran aynı sözlük ve formatter kullanır |
| Tema / bildirim | Açık tema; semantic token, dört ikonlu MessageBanner/FieldError ve tek notification sağlayıcısı | Light/Dark/System tema geçişi |
| Server state | useEffect/local state/AbortController | TanStack Query; Zustand ihtiyaç oluşursa |
| Kalite araçları | typecheck/build komutları | ESLint/Prettier; Husky kurulursa CI doğrulaması da gerekli |

Bu hedefler için yeni paket/sürüm eklenmeden resmi lisans incelenir; [licenses.md](licenses.md), envanter, LICENSE/NOTICE güncellenir. `npm ci` / `dotnet restore` sonrası `python3 scripts/check-licenses.py`; bilinmeyen lisans kontrolü geçmez. Kullanılmayan framework veya ücretli template ekleme.

## Her değişiklikte uygulama ve doğrulama

1. İlgili AGENTS ve bu belgeyi, mevcut ortak bileşen/API'yi ve kurulu Next.js'in `node_modules/next/dist/docs/` rehberini oku. Gerekli ortak altyapı eksikse mevcut akışı koruyarak kapsamlı olmayan bir geçiş tasarla; yapılmamış entegrasyonu hazır varsayma.
2. [TESTING.md](TESTING.md) içine etkilenen senaryoları önce yaz. Yeni özelliklerde locale/theme/state/permission/API/version/responsive etkilerini görevle orantılı ele al.
3. Ortak davranışı tekrar kullan, DTO/domain/backend kurallarını koru; hassas veri SSR/cache/log izolasyonunu sağla.
4. Frontend kod/config değiştiyse frontend klasöründe `npm run typecheck` ve `npm run build`; ilgili otomatik/kabul testlerini çalıştır. `npm run lint` henüz yok; varmış gibi komut/sonuç raporlama. Araç kurulumu sonrası lint/format-check komutları README/CI ile birlikte eklenir.
5. Backend yetki/güvenlik değişiminde negatif JWT/rol/scope, persistence değişiminde gerçek ayrı PostgreSQL testleri; yalnızca üretilen geçici veriyi temizle.
6. Test kanıtı, paket/lisans farkı, gerçek skill/agent kullanımı, kalan işler ve roadmap güncellenir. Sadece uygulanmış ve doğrulanmış iş tiklenir. Doküman turunda bağlantı/kapsam/whitespace doğrula; uygulama testi çalıştırılmadıysa geçti diye yazma.

## AGENTS, skill ve agent kararı

Bu repo standardını [kök AGENTS.md](../AGENTS.md) ve [frontend AGENTS.md](../frontend/buildingflow-web/AGENTS.md) zorunlu okuma olarak bağlar. Kök dosya, kökten başlanan frontend işlerinde de alt yönergenin okunmasını ister. Ayrı geliştirme agent'ı çalıştırmak gerekmez; skill ancak daha sonra belirli bir tekrar eden kalıp üretme/doğrulama akışı için oluşturulur. Genel kuralların ikinci bir kopyası skill içine konmaz.

Dosya kapsamı için kaynak: [OpenAI Docs — AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md). Projeye özel 17 başlık kullanıcının verdiği metinden uyarlanmıştır.

## Mesaj ve menü standardının teslimi — 2026-10-08

Uygulanan ortak altyapının testleri `npm test` içindeki standards.test.mjs ile korunur: sözlük/ICU parametre eşliği, kaynak ve roadmap anahtarları, locale whitelist, güvenli ProblemDetails/alan mesajları, dil değişiminde feedback, formatter ve yeni OIDC dönüş rotaları. Yeni metni sözlüğe ekle; bilinmeyen API title/error metnini ekrana doğrudan basma. 07.1 ile backend code/parameters/severity ve coded alan hataları garanti edildi. Frontend code registry/TR–EN eşlemesi ve güvenli fallback kullanır; code olmayan legacy mesaj allowlist’i korunur. TraceId henüz garanti değildir. İş mesajlarında exception/SQL/token/parola yok.

Bu teslim RHF/Zod/Query, permission grant veya dark theme çalışmalarını tamamlamaz. Yeni yönetim form/listelerinde aynı shadcn/TanStack ve EntityEditor/RecordList kaynakları kullanılır. Gerçek sonuçlar [TESTING.md](TESTING.md) içinde.

## 05 finans ekranı uygulama kaydı

2026-10-08: kalıcı Aidat / Borçlar modülü, TR/EN liste/form/durum/ARIA, shadcn Dialog/TanStack RecordList, finans alanlarını güvenli çeviren merkezi ProblemDetails ve açık dış DTO/scope doğrulaması uygulandı. Tutar string parse ve kuruş BigInt önizlemesi, 1–500 açık bölüm seçimi, UUID ile aynı içerik retry ve version kontrollü gerekçeli iptal kullanılır. Borç Unit’e aittir; ödeme 06 kapsamıdır. Yeni RHF/Zod/Query paketi kurulmadı. Mevcut useRows isteğe bağlı response decoder alabilir; yeni finance seçenekleri bunu kullanır. Dues menüsü dört finans rolüne, Person menüsü yönetici rollerine göre görünür; backend aktif üyelik son otoritedir. Gerçek test/manuel sınırlar TESTING.md’dedir.

## UI 06 ortak sunum uygulaması — 2026-10-09

WorkspacePresentation yalnızca sunum taşır: WorkspaceHeading, SummaryCard, IdentityMark, ListToolbar, DetailField, ikonlu StatusBadge ve (İŞ19) ortak tonlu ToneBadge ile rol başına tek renk ve ikonlu RoleBadge. Düğme standardı (İŞ19 ek 3, kullanıcı seçimi "A · Yumuşak renkli"): `components/ui/button.tsx` varyantları renkleri `globals.css` `--action-*` token'larından alır; beyaz eylem düğmesi yoktur. Ana eylem (kaydet, ekle, gönder, onayla) dolu mavi birincil (variant yok); görüntüle/detay/yenile/tekrar dene/geçmiş/dışa aktar/bağlantıya git `view` (açık mavi); düzenle/düzelt `edit` (turkuaz); bağla, vazgeç, kapat, temizle, sayfalama, sekme ve geri `outline` = `neutral` (açık gri); parola/anahtar/kod yenileme ve ilişki bitirme `warn` (amber); sil/bağı kaldır/rızayı geri çek/iptal başlatan düğme `danger` (açık kırmızı); dolu kırmızı `destructive` yalnız onay penceresindeki son silme/iptal düğmesidir. Form alanı gibi davranan seçimler (combobox tetikleyicisi, seçim kartı/listesi) `field` (beyaz kenarlı); seçili öğe `view`. `ghost` yalnız menü, bildirim zili, kapatma X ve sıralama başlığı için. Renk tek bilgi kaynağı değildir: düğmede ikon ve/veya metin bulunur, ikon düğmesi erişilebilir ad taşır. `tests/buttons.test.mjs` varyant/token tanımını ve ikon-anlam eşlemesini denetler. (İŞ20) Sol menü Dashboard ile başlar, Genel bakış en alttadır; sidebar altında marka/slogan/sürüm yazısı yoktur. Üst çubuk sloganı `ui.shellSloganN` anahtarlarından ve `features/dashboard/slogans.ts` ile sayfa başına bir kez seçilir. Dashboard grafikleri paketsiz SVG ve `--chart-1..6` sırasıyla (renk varlığa bağlı, sıraya değil); her grafikte gösterge ve ekran okuyucu tablosu bulunur. Arka plan görselleri projenin çizdiği SVG sahnelerdir; dış görsel eklenmez. Telefon ve e-posta alanları yalnız `components/forms/ContactInputs` (`PhoneInput`, `EmailInput`) ve `lib/contact.ts` kurallarıyla; düz `Input type="tel"` kullanılmaz. RecordList isteğe bağlı compact, server sorting ve footerControls alır; mobil kartlar yatay tabloyu kaldırır. DialogContent `placement="right"` destekler. Yönetim alanı, bina/blok, daire, kişi, ilişki ve aidat kayıt form/detayları ortak ManagementPanel ile sağdan açılır: masaüstünde 512 px/tam yükseklik, mobilde tam genişlik, kaydırılabilir içerik ve ortak başlık/alt eylem alanı. Silme/onay DialogContent merkezde kalır. Kapanış odağı her açılışta yakalanır; detaydan editöre geçişin ilk düğmesi korunur. API kullanan RecordDetails `features/hierarchy` katmanındadır; ortak UI veri erişimi yapmaz. Unit/Person ve kişi ilişkisi önizlemesi dış shape/ebeveyn scope kontrolünden geçer; yetki otoritesi API'dir. EntityEditor, version/409, mutation kilidi ve DeleteEntity yeniden kullanılır. Sayılar gerçek DTO'lardan gelir; aktif/pasif doluluk değildir, kişi birden fazla role sahip olabilir. Yeni paket yok; dark tema ve Query/RHF/Zod kuruldu varsayılmaz. Güncel kanıt/sınırlar TESTING.md'de.

## 06 tahsilat ekranı uygulama kaydı — 2026-10-09

Kalıcı `/payments` seçim ve `/properties/{id}/payments`; ayrı menü/kapsam/route dönüş whitelist'i. PaymentEditor ve güncel GET'li PaymentDetail aynı sağ ManagementPanel; RecordList/TanStack desktop tablo ve mobil kart, WorkspacePresentation/KPI/filter, merkezi yetkili API/feedback/formatter. Borç detayından ödeme paneline geçişte ilk düğmenin odağı korunur; geçmiş ödeme başka iç içe dialog açmadan gösterilir.

1–100 açık dağıtım, decimal string/BigInt kuruş toplamı/bakiye, borç version'ları ve normalize payload UUID. Belirsiz sonuç manuel retry; cancelled replay uyarı verir. DTO'lar Unit/Property kimliği, yöntem/durum/kalender/para/dağıtım toplamı/geçmiş ile runtime doğrulanır. TR/EN, aria-invalid/ikon/ilk odak/busy kilidi, 409 güncel borç yükleme, tarih değeri korunması. Kişi verisi ve anonim SSR'a finans DTO eklenmez. Yeni paket/asset veya Query/RHF/Zod geçişi yok. Tahsilat uygulama içi kayıt; online sağlayıcı, transfer ve fiskal belge sınırı kullanıcı metninde açıktır. Testler/manuel sınırlar [TESTING.md](TESTING.md).

## Toplu seçim standardı — 2026-10-09

Tahakkukta tüm sonuçları seç görünür sayfayla karıştırılmaz; mevcut arama/scope bütün sayfalardan yetkili API ile alınır. Sayfayı seç/çıkar, tüm seçimleri temizle ve tek satırı çıkar birlikte sunulur. Filtre/sayfa değişimi mevcut seçimi silmez; UUID ile dedup ve 500 üst sınır atomik uygulanır. Kontrol ve sonuç tutarı açık; bulk read loading/cancel/error/unmount akışı seçimi yarım bırakmaz. Create butonu seçili bölüm sayısını gösterir. Backend toplu transaction, scope/dönem tekilliği ve belirsiz UUID retry son otoritedir. Sözlük, native pagination/limit/abort testleri ve gerçek TR/EN/mobil kabul tekrar kullanılır.

## 06.1 kişi geçmişi / taşınma onayı

ChargePeople masaüstü TanStack tablosu, mobil kart ve sağ detayda snapshot ad/rol/muhatap gösterir. Legacy “kaydedilmemiş”, kayıtlı fakat ilişkisiz ve yetkisiz durumlar farklıdır. DuesPersonFilter mevcut useRows/AbortController/search/pagination üzerinden yalnız borç geçmişindeki seçenekleri alır; kişi araması hassas URL state'e konmaz. Katalog select ve OTHER açıklaması ortak FieldError/MessageBanner kullanır. EndOccupancy sağ panelinde önizleme/tutar/ödeme hariç sayısı ve açık, varsayılan kapalı onay vardır. Tarih/version/refresh onayı temizler; frontend helper sadece aynı önizlemedeki ID/version listesini üretir. TR/EN runtime DTO/ICU/feedback sözleşmeleri test edilir; yeni paket veya UI framework eklenmemiştir.

## İŞ 07.1 hata / uyarı bildirimi standardı

Genel API ve form error/warning mesajı sağ üst ortak NotificationProvider üzerinden gösterilir; aynı metin sayfada ikinci banner olarak basılmaz. Alan açıklaması sr-only olarak aria-describedby ile korunur, hatalı input işaretlenir ve ilk alana odaklanılır. Retry/güncel kayıt yükleme eylemi sayfada kalır; 409 otomatik overwrite edilmez. Client FieldError mesajları gruplandırılır. Tüm toastlar 8 saniye, hover/odak sırasında durma, elle kapatma ve dört bildirim/dedup uygular. Açık Dialog/ManagementPanel bildirim kapatıldığında kapanmaz; mevcut Radix focus/dismissable branch kullanılır. Mobile konum sağ üsttür. Yeni bağımlılık eklenmedi.

Katalog BE ErrorCatalog; FE services/error-codes.ts + messages/apiErrors/apiFields. Parametreler güvenli şemayla doğrulanır, ham backend mesajı render edilmez; bilinmeyen kod/yanıt güvenli fallback. HTTP 404 her hata için kullanılmaz. Yeni kod/parametre/sözlük ve ilgili API/FE negatif testi birlikte eklenir. Ayrıntılar [errors.md](api/errors.md); test kanıtı [TESTING.md](TESTING.md).

## İŞ 07 gider ve bütçe standardı

Kalıcı Gider ve Bütçe menüsü ve Property scope; giderler ve kategori bütçesi aynı modülde ayrı görünüm. RecordList/TanStack desktop + mobil kart, sağ ManagementPanel, TR/EN/aria/ilk alan odağı/8 saniye kodlu bildirim kullanılır. Tutar string, BigInt kuruş ve ortak formatDecimalCurrency büyük tutar/negatif sıfıra yakın farkı kaybetmez. Dış DTO scope/tarih/para/iptal/revizyon sırası/özet eşliğiyle doğrulanır.

Gider immutable; iptal gerekçeli/version, yeni kayıt UUID aynı içerik retry. Bütçe ilk version 0/sonraki güncel version, her değişiklik revizyon; 409 açık güncel plan yükleme ve kullanıcı açıklamasını koruma. Form locale render'ında girilmiş bütçe tutarını eski GET ile üzerine yazma. Plansız kategori farkı —, sıfır plan ayrı; aylık KPI liste filtrelerinden bağımsız, gelir tahsilattır. Ana kullanıcı DB'sinde başarılı finans mutation tarayıcı kabulü yapılmış sayılmaz; bu senaryolar ayrı PostgreSQL testlerindedir. Paket/ücretli template/asset eklenmedi; ayrıntı [TESTING.md](TESTING.md).

## İŞ08 dosya/duyuru ekranları

Duyurular ve Belgeler bağımsız menü, ortak RecordList/TanStack ve sağ ManagementPanel kullanır. Düz metin duyuru React text olarak gösterilir; HTML işleme yok. Upload FormData ve download Blob ortak authorizedRequest üzerinden geçer; multipart Content-Type browser tarafından boundary ile belirlenir. Blob boyut/MIME/SHA256 doğrulanmadan download URL üretilmez; geçici URL iptal edilir. Dosya kabulü PDF/JPEG/PNG≤10MiB; backend aynı sınırı ve imzayı bağımsız doğrular. ApiError ham server/SDK exception mesajı göstermeden kodla TR/EN sağ üst ortak bildirimde sunulur. Duyuru eki görünürlüğü server’da üst kayıttan gelir; FE rolü veri yetkisi değildir.

## Apartman/daire detay alanları — 11.10.2026

`features/property-details/model.ts` mevcut backend enum ve nullable alanlarının ortak FE kayıt defteridir. Oluşturma, düzenleme, 409 yeniden yükleme, detay, tablo ve mobil kartlar aynı alan tanımını kullanır. `DetailInputs`/`DetailValues` mevcut Input/Label/FieldError/DetailField ve TR/EN sözlükleriyle çalışır. Boş seçim null'dır; false/0 kaybolmaz. Net alan ≤brüt alan, iki ondalık; yapım yılı Europe/Istanbul güncel yılına kadar, kat konumu floor ile uyumlu olmalıdır. Yıl binlik ayırıcı olmadan gösterilir. Apartman özellikleri yalnız APARTMENT Property veya Building üzerinde tutulur. Yeni Unit sütunlarında backend desteklemediği için sort açılmaz; TanStack columnVisibility Sütunlar menüsünden yönetilir. API DTO'ları `management-contracts` ile dış veri/ebeveyn kapsamı kontrolünden geçer. PUT mevcut opsiyonel alanları taşımalı; yalnız temel alanları göndermek detayları null'a çevirir. Test kanıtı [TESTING.md](TESTING.md), sözleşme [hierarchy.md](api/hierarchy.md).

### Daire–kişi gezinme kalıbı (11.10.2026)

`features/people/unit-links.ts` canonical doğrudan/binalı UUID filtreli hedef, strict query ve batch DTO doğrulamasını paylaşır. PersonPage/PersonOccupancies/RecordDetails kişi/bölüm kimliğini doğrular; ham kişi/telefon/e-posta query'ye yazılmaz. `useUnitPeople` yalnız yönetici rolünde, görünen daire sayfası için tek bounded batch; loading/error/ilişki-yok farklı, iptalde/eski sayfada sonuç yayınlanmaz. ACCOUNTANT için görünür kişi sütunu veya kişi API isteği açılmaz.

`RecordList` varsayılan md davranışını korur; daireler `cardBreakpoint="xl"` kullanır: tablet/mobil kart, sm'den itibaren iki sütun (tek sonuç tam genişlik),1280px'den itibaren tablo. Kişi bağlantıları en az44px ve satır kırılmalı; yeni kişi alanları önce gösterilir. Yeni route/query OIDC whitelist + negatif testle birlikte gelir. Kanıt TESTING.md, gerçek iPad Safari kabulü Chrome ölçü önizlemesinden ayrıdır.

## Sabit çalışma çerçevesi — 11.10.2026

WorkspaceShell `100dvh` çerçevesidir: masaüstü (1024px ve üstü) sol menü ile üst/alt menü kaydırmada yerinde kalır. `workspace-content` tek ana içerik scroll alanıdır; min-height:0 ve flex ile footer son kaydı örtmez. Uzun sol menünün yalnız kendi listesi kayabilir; profil ve yönetim alanı footer'ı çerçevede kalır (SIDEBAR FAZ07.1). 1024px altında üst düğme aynı rol/kapsam menüsünü 320px sol Dialog içinde açar; alt kısa yollar Genel Bakış/Yönetim Alanları/Daireler ve yetkili Kişiler'dir. Mobil safe-area korunur. İçeriğe atla, isimli/odaklanabilir scroll region, klavye scroll, ESC ve dialog kapanış odağı korunur; rota değişiminde içerik başa döner. Yeni sayfalarda window/body scroll veya üst/alt menüyü içerik içine tekrar koyma. Dialog formları kendi scroll alanını korur. Sayaç Okumaları bilgi tablosu için ortak sağ panelin genişlik istisnası896px; onaylı kişi kayıt editörü768px genişlikte iki sütun/gruplanmış alanlar kullanır, mobilde tek sütuna iner. Diğer CRUD formları512px kalır.

Tüketim toplamları fiziksel miktardır: her sayaç türü ayrı, üç ondalık `formatConsumption` ile BigInt hassasiyeti korunur. Tarife olmadan TRY maliyeti uydurulmaz; faturalar mevcut Expense gerçekleşme ayının giderine girer. UI'de faturaya erişim ve bu ayrım açıklanır. Test kanıtı TESTING.md BAKIM sabit menüler bölümündedir.

## Kişi profili ve iletişim —12.10.2026

Person form/detay/listesi ortak EntityEditor/ManagementPanel/RecordList; genişleyen liste tablet/mobilde xl kart eşiği. Gerçek/tüzel–yerli/yabancı alanları backend sözleşmesiyle doğrulanır, telefon/email zorunlu. PersonAccountPicker aktifProperty üyeliğini sunucuda arar/sayfalar, keycloak user directory değildir. Mesaj paneli typed parse/abort/UUID retry, tel ve snapshot aidat geçmişini kullanır; NotificationInbox yalnız kendi alıcı kutusunu okur. SMS kapalı ve görünür açıklamalı. TR/EN, field-error/notification/odak/busy/ESC ortak davranışları korunur. Yeni paket yok.

## WEBSITE FAZ05 yönetim ekranları —18.10.2026

features/website mevcut ManagementPanel/RecordList/Button/ContactInputs/notification ve TR–EN kullanır. API DTO/scope doğrulanır.409 yenileme type=button, ayrı key ve preventDefault kullanır; girdiler korunur, kayıt ayrıca gönderilir. Önizleme oturumda React text/doğrulanmış blob sunar; public taslak URL/token deposu yoktur. Create/publish/restore/upload aynı payload UUID retry korur. Medya sözleşmesi docs/api/website.md içinde.

### SIDEBAR FAZ07.1 Aşama03 —19.10.2026

Mevcut WorkspaceShell272/76px geniş/dar mod, ortak marka tokenları, aktif sol vurgu/focus ve mevcut Radix Tooltip kullanır. Dar mod tooltip yalnız masaüstünde; geniş mod mevcut grup kontrolü aktif rotayı gizlemez. `buildingflow-sidebar-v1` yalnız collapsed/expanded UI tercihini saklar; kişi/tenant/route/arama/token saklamaz. SSR ilk render geniştir, storage reddinde kontrol bellekten çalışır. Yeni grup/arama/footer ve tüm mobil kabulü sonraki aşamalardır. [Kanıt](navigation-refactor/STAGE_03.md).

### SIDEBAR FAZ07.1 Aşama04 —20.10.2026

Mevcut navigasyon altı semantik gruptan üretilir; görünürlük merkezi navigationPermissions koşullarını korur. Route olmayan mükerrer Kullanıcılar yer tutucusu yoktur. Arama yalnız model.searchable içindeki yerelleştirilmiş görünür etiketlere uygulanır; locale case, NFD aksan temizliği ve ı/i eşleme, çoklu kelime AND ve boş sonuç ortak search.ts kaynağındadır. Arama metni kaydedilmez ve dialog kapanınca temizlenir. Grup adı route veya izin üretmez; backend son otoritedir. [Kanıt ve sınırlar](navigation-refactor/STAGE_04.md).

### SIDEBAR FAZ07.1 Aşama05 —20.10.2026

Mobil drawer mevcut Radix Dialog ile; pathname veya1024px masaüstü geçişi paneli kapatır. Açılışta aktif/ilk bağlantı, dismiss sonrası trigger, navigasyon sonrası main odağı kullanılır. Genişlik min(320px,100vw−16px); scroll-lock/focus-trap ortak primitive üzerinden, reduced-motion conditional CSS ile sağlanır. Üretim auth kapısı test fixture koduna bağlanmaz. [Fixture kabulü ve sınırları](navigation-refactor/STAGE_05.md).

BAKIM20.10.2026: üst arama, sidebar/drawer bağlantıları ve planlı öğeler ortak `--shell-menu-font-size:12px` kullanır; grup başlıkları11px kalır.

### SIDEBAR FAZ07.1 Aşama06 —20.10.2026

SessionProfileProvider mevcut hesabı paylaşır; subject eşleşmeyen profil kullanılmaz. Footer mevcut AccountAvatar/SessionProfileMenu ve PropertyScope'u kullanır. Alan değişimi mevcut modül giriş route'una gider; yeni tenant/hesap state'i veya yetki üretmez. accountChanged bütün profil yüzeylerini yeniler. [Kanıt](navigation-refactor/STAGE_06.md).


## Sidebar navigasyon sözleşmesi —20.10.2026

SIDEBAR FAZ07.1: menü/rol/flag/alan görünürlüğü features/navigation/navigation.ts kaynağından üretilir; UI görünürlüğü backend yetkisi değildir. Routesuz planlı öğeler enabled veya aranabilir yapılmaz. Arama yalnız yetkili menü etiketleridir. 272/76px sidebar, ortak12px üst/sidebar/drawer menü tokenı; dar tooltip mevcut Radix'tir. buildingflow-sidebar-v1 yalnız collapsed/expanded saklar. Footer profil mevcut account/subject eşliğiyle, alan mevcut PropertyScope ile bağlanır; yeni hesap/rol state'i oluşturulmaz. Drawer rota/desktop geçişinde kapanır, odak ve scroll lock temizlenir. [Uygulama/geri alma](navigation-refactor/IMPLEMENTATION.md), [test ve açık manuel kabul](navigation-refactor/TEST_RESULTS.md), [lisans farkı](navigation-refactor/LICENSE_CHECK.md).

20.10.2026 kullanıcı düzeltmesi: sağ üst header profil adı/rolü ve menüsü masaüstünde de görünür; sidebar footer ile aynı SessionProfileProvider kullanılır. Header profilini desktopta gizleyen koşul kaldırıldı.


20.10.2026 Dashboard banner güncellemesi: yönetimalanı seçimi headerda dil önündedir,yalnızDashboard'u etkiler;PropertyScope/diğer modüllere globalfiltre değildir. Dashboard veri altbileşeni seçiliUUIDile keylenir,öncekifotoğraf/kart/istekler temizlenir. Ortakfotoğraf mevcut standaloneALL_MEMBERS belge sözleşmesiyle;requestUUIDretry,metadataScope/SHA256/boyut ve objectURLtemizliği korunur. Alan başlığı/sayıları gerçekDTO,saatEurope/Istanbul;hava yokluğu açıkça gösterilir. [Sözleşme/kanıt](navigation-refactor/DASHBOARD_HERO.md).

20.10.2026 Dashboardtrend:mevcut6ayAPI verisi yeni SVG ikilisütun(mavigelir/pembegider) ile gösterilir;Son6/3ayyereldilimleme,klavye/touch/hovertooltip ve erişilebilir tam tutartablosu korunur. Paket eklenmez.
