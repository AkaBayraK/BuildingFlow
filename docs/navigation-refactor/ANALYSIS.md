# FAZ 07.1 — Sidebar ve navigasyon analizi

Tarih: 19.10.2026. Kapsam: yalnız Aşama 01, kaynak incelemesi. Durum: analiz teslim edildi; Aşama 02 kullanıcı onayı bekliyor. Sekiz aşamalı sidebar planında 1/8 = %12,50; bu tur Aşama 01 işi 1/1 = %100,00. Bu kimlik, mevcut İŞ 07.1 hata mesajları işi ve 12 aşamalı UI planından ayrıdır. Genel iş yol haritası yüzdesi yeniden hesaplanmadı: BİLGİ MEVCUT DEĞİL.

## Yetki, yöntem ve kaynaklar

- Talep: `buildingflow-sidebar-phase/MASTER_PROMPT.md` ve `PHASE_07_1.md`. İkisi okundu; bu klasörde PHASES.md ve alt AGENTS.md tespit edilmedi. Kökte MASTER_PROMPT.md/PHASES.md yok; ayrı UI kaynakları `frontend/buildingflow-ui-plan/MASTER_PROMPT.md` ve `PHASES.md` (12 aşama) incelendi. Tarihsel görevler yeniden başlatılmadı.
- Kök ve frontend AGENTS.md, `docs/progress/STATUS.md`, `SESSION.md`, README, mimari çekirdek, frontend-rules, ilgili TESTING bölümleri ve akadev-audit talimatları incelendi. Salt okunur denetimde yalnız ayrıca istenen bu rapor yazılır; ilerleme, changelog, test ve roadmap dosyaları güncellenmez.
- Başlangıç `git status --short`: yalnız `?? buildingflow-sidebar-phase/`. Bu önceden mevcut kullanıcı klasörüdür; sahiplenilmedi/değiştirilmedi.
- Canlı tarayıcı/oturum incelenmedi. Aşağıdaki envanter, ekranda üretilebilen öğelerin kaynak kod eşlemesidir; güncel kullanıcının rolü, feature flag değerleri ve görünen yönetim alanı TESPİT EDİLEMEDİ. Gizli ortam dosyaları ve kişisel veriler okunmadı.

## Mevcut mimari ve giriş noktaları

`frontend/buildingflow-web` Next.js 16.4.0 App Router, React 19.3.0, strict TypeScript, Tailwind 4.3.3, next-intl TR/EN kullanır. `src/app/layout.tsx` dil ve bildirim provider'larını; `src/app/(workspace)/layout.tsx:5` AuthGate + WorkspaceShell sınırını kurar. Sidebar ayrı bir paket değil, `src/components/WorkspaceShell.tsx:18–63` içinde oluşturulur.

Layout server tarafında `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED` ve `BUILDINGFLOW_PERSON_CENTER_ENABLED` değerlerini boolean olarak kabuğa geçirir. Session contexts açıkken `/` ve `/workspaces*` için aynı sidebar yerine ContextShell kullanılır (`WorkspaceShell.tsx:16,66`). Bu dallanma korunmalıdır. Profil `auth/AuthGate.tsx:82–89` içindeki SessionControls'tadır; hesap API'si/useAccount, AccountAvatar, mevcut dil, bildirim ve Keycloak çıkışı kullanılır. Sidebar footer'ında şu anda profil/seçici yoktur.

PropertyScope (`features/navigation/PropertyScope.tsx:8–18`) yönetim alanını URL'deki UUID'den çıkarır, yetkili `/api/v1/properties/{id}` isteğiyle yükler; rota değişiminde önceki property temizlenir, istek iptal edilir. Global bir seçili alan deposu değildir. `/units`, `/people` ve modül girişleri PropertyModuleSelection/PropertiesWorkspace üzerinden alan seçtirir; merkezi kişiler bayrağı `/people` davranışını ayrıca etkiler. Yeni footer seçicisi bu gerçek kaynakları kullanmalı, ikinci tenant state'i üretmemelidir.

AuthGate (`auth/AuthGate.tsx:65–80`) Keycloak standard flow + S256 PKCE ve bellek içi kimlik sağlar; token yenileme/giriş başarısızlığında kabuğu açık kabul etmez. Güvenli dönüş allowlist'i `auth/return-path.ts`; eski `/sites` rotaları mevcut redirect dosyalarında kalır. `features/navigation/routes.ts:2–7` UUID scope ve bölüm eşlemesinin mevcut merkezidir.

## Menü / route / görünürlük matrisi

Kaynak: `WorkspaceShell.tsx:24–50`. Kısaltmalar yalnız mevcut frontend koşullarını anlatır: M = SUPER_ADMIN/SITE_ADMIN/MANAGER; F = M veya ACCOUNTANT; P = SUPER_ADMIN; S = sessionContexts açık; C = centralPeople açık; B = `/properties/{id}`. “Her oturum” AuthGate içindeki üretim koşuludur, backend veri erişimi izni anlamına gelmez.

| Öğe | Gerçek hedef | Kaynaktaki koşul / durum |
|---|---|---|
| BuildingFlow marka | `/dashboard` | Her sidebar oturumu |
| Dashboard | `/dashboard` | Her sidebar oturumu; veri yetkisi ayrıca doğrulanır |
| Yönetim Alanları | `/properties` | Her sidebar oturumu |
| Genel bilgiler | B | URL'de property UUID varsa |
| Bina / Blok | B + `/buildings` | Property yüklenmiş, tipi APARTMENT değilse |
| Daireler / Bağımsız Bölümler | B + `/units`, alan yoksa `/units` | Her sidebar oturumu; APARTMENT etiketinde daireler |
| Kişiler | C ise `/people`, değilse B + `/people` veya `/people` | M |
| Sayaçlar | B + `/meters` veya `/meters` | F |
| Raporlar | B + `/reports` veya `/reports` | F |
| Aidat / Borçlar | B + `/dues` veya `/dues` | F |
| Tahsilatlar | B + `/payments` veya `/payments` | F |
| Gider / Bütçe | B + `/expenses` veya `/expenses` | F |
| Duyurular | B + `/announcements` veya `/announcements` | Her sidebar oturumu; içerik görünürlüğü backend'de |
| Belgeler | B + `/documents` veya `/documents` | Her sidebar oturumu; içerik görünürlüğü backend'de |
| Web Sitesi Yönetimi | B + `/website` veya `/website` | M |
| Talep / Arızalar | B + `/requests` veya `/requests` | M |
| SMS | B + `/sms` veya `/sms` | M |
| Kullanıcılar (çalışan) | `/users` | P veya (!S ve SITE_ADMIN) |
| Ortak Alanlar | Hedef yok | Her sidebar oturumunda disabled, Planlandı |
| Araçlar / Otopark | Hedef yok | Her sidebar oturumunda disabled, Planlandı |
| Tedarikçiler | Hedef yok | Her sidebar oturumunda disabled, Planlandı |
| Açık Rıza | `/consents` | M ve (!S veya P) |
| Tanımlar | Hedef yok | M ve (!S veya P), disabled, Planlandı |
| Kullanıcılar (planlı) | Hedef yok | M ve (!S veya P), disabled, Planlandı |
| Sistem Ayarları | Hedef yok | M ve (!S veya P), disabled, Planlandı |
| Genel bakış / Çalışma alanları | S ise `/workspaces`, değilse `/` | Sidebar sonu; active yalnız path `/` kontrolü |

Ek yüzeyler: alt kısayollar Dashboard, Yönetim Alanları, Daireler ve yetkiliyse Kişiler; arama bunlara ek mevcut modül öğeleri, izinli Açık Rıza, `/account` ve home öğesini kullanır. Planlı disabled öğeler aramada yoktur. Profil açılır menüsünde `/account`, kimlikli API kontrolü ve Keycloak çıkışı bulunur. ContextShell'de `/workspaces` ve `/account` bağlantıları vardır. Aynı öğelerin bu yüzeylerde tekrarı ayrı işlev/route değildir.

### İki Kullanıcılar öğesinin nedeni

`WorkspaceShell.tsx:31` çalışan `/users` bağlantısını üretir; `:49` eski tasarım yer tutucusunu routesuz disabled düğme olarak üretir. `messages/tr.json` içindeki navUsers ve shellUsers aynı kullanıcı kavramını etiketler. `src/app/(workspace)/users/page.tsx:1–2` gerçek UsersWorkspace'i açar; `features/users/UsersWorkspace.tsx:38–40` SUPER_ADMIN/SITE_ADMIN kontrolü yapar. `backend/src/BuildingFlow.Api/UserEndpoints.cs:7` CanManageUsers politikasını, `Program.cs:36` bu iki gerçek rolü kullanır. İkinci ayrı yönetici route'u tespit edilmedi. Öneri: gerçek bağlantıyı tek Sistem Yönetimi grubuna taşı; planlı mükerrer öğeyi sunumdan kaldır, route/yetkiyi koru. Bu tur uygulanmadı.

Pasif öğelerin nedeni erişilebilir bir modülün geçici hata durumu değil, `planned` ve literal disabled düğmelerdir. Otomatik etkinleştirilmemeli, varsayımsal hedef/izin atanmamalıdır.

## Tasarım farkları ve bulgular

Önem: yüksek/orta/düşük; güven kaynak incelemesine aittir, runtime testi değildir.

| Önem / güven | Kanıt | Etki ve öneri |
|---|---|---|
| Yüksek / yüksek | WorkspaceShell.tsx:24–35; backend/src/BuildingFlow.Api/CurrentUserService.cs:9–22 | Menü realm rollerinden türetiliyor; backend ayrıca WorkspacePropertyPermissions kapsamını kullanıyor. Merkezi şema mevcut rol/flag koşullarını korumalı; realm rolünü property üyeliği veya güncel assignment izni sanmamalı. Yeni permission uydurulmadan sunucunun mevcut bağlam kaynaklarıyla uyum kontrol edilmeli. |
| Orta / yüksek | WorkspaceShell.tsx:31,49 | Çalışan ve planlı Kullanıcılar karışıyor. Yukarıdaki tekilleştirme önerilir; erişim genişletilmez. |
| Orta / yüksek | WorkspaceShell.tsx:27–39 | Etiket, route, görünürlük, başlık, kısayol ve arama farklı ifadelerde tekrar ediyor. Typed NavigationItem/NavigationGroup ve saf görünürlük/aktiflik yardımcıları önerilir. |
| Orta / yüksek | WorkspaceShell.tsx:61 | Arama `toLocaleLowerCase()` çağrısına locale vermiyor. Türkçe I/İ eşlemesi ortamın varsayılanına bağlı; TR/EN locale ile normalize edilmeli, yalnız filtrelenmiş yetkili menüde çalışmalı. |
| Orta / yüksek | WorkspaceShell.tsx:21,40,62 | Kendi Link onNavigate'leri drawer'ı kapatıyor; path effect yalnız içerik scroll'unu sıfırlıyor. Back/forward/programatik rota değişiminde kapanış için path tabanlı kontrol önerilir. Canlı arıza yeniden üretilmedi. |
| Orta / yüksek | WorkspaceShell.tsx:57 | `aria-expanded={menuOpen||!collapsed}` mobilde kapalı drawer için de true olabilir. Mobil drawer ile masaüstü daraltma state'ine uygun ad/açıklama ve aria-controls önerilir. |
| Orta / yüksek | globals.css:366,414–426 | Mevcut 248px/76px, navy/mavi ve hover/active zaten var. Geniş hedef 260–280px değerlendirilmeli; 76px korunabilir. Aktif sol vurgu yok; focus görünürlüğü ve 38px minimum satırlar klavye/dokunma kabulünde gözden geçirilmeli. |
| Orta / yüksek | WorkspaceShell.tsx:19,57; globals.css:426 | Collapse yalnız component state; yenilemede sıfırlanır. Sürüm kontrollü boolean UI tercihi saklanabilir; kişi/tenant/token veya arama metni depolanmamalı. title mevcut, özel erişilebilir tooltip yok. |
| Orta / yüksek | WorkspaceShell.tsx:62; components/ui/dialog.tsx:32–106; globals.css:496 | Mobil panel mevcut Radix/shadcn Dialog, overlay ve odak geri dönüşü kullanır; yeniden drawer altyapısı yazılmamalı. Escape, focus trap, scroll lock ve route değişimi gerçek UI'da doğrulanmalı. Reduced motion kuralı slogana özel; drawer geçişleri ayrıca ele alınmalı. |
| Orta / yüksek | WorkspaceShell.tsx:66; app/(workspace)/layout.tsx:5 | ContextShell ayrı deneyimdir. Bayrak açık/kapalı, merkezi kişiler ve çalışma alanı yolları regresyon matrisine alınmalı. |
| Düşük / yüksek | WorkspaceShell.tsx:45–50,57; AuthGate.tsx:82–89 | Şu an Ana Menü/Operasyonlar/Yönetim grupları; profil üst bardadır. İstenen alt profil/alan bağlamı mevcut bileşen ve kaynakları kullanmalı, logout/dil/bildirim veya profil rızası akışı çatallanmamalı. |
| Orta / yüksek | tests/dashboard.test.mjs:32–37 | Test mevcut kaynak metninin JSX biçimini regex ile sabitliyor. Refactor sonrası aynı davranışı kanıtlayan testlere uyarlanmalı; yeşil test için eski kaynak metni yapay olarak korunmamalı. |

Çevrimdışı açılabilir uygulama kabuğu garanti edilmedi. AuthGate ağ/kimlik doğrulama bekler; bu görev offline yetki verme veya auth mimarisi değiştirme yetkisi sağlamaz. Önceden yüklenmiş kabuk davranışı ile ilk offline açılış ayrı test edilmelidir. Kontrast/WCAG AA, ekran okuyucu, hydration ve beş viewport kabulü kaynak varlığıyla başarılı sayılmadı.

## Mevcut UI, ikon ve lisans envanteri

Manifest, lock temelli yerel lisans kayıtları, `docs/licenses.md:55–61`, `docs/licenses/package-inventory.json`, `components/ui/LICENSE.md` ve NOTICE.md incelendi. Mevcut Lucide ikonları yeterli; ayrıca ikon/font/görsel gerekmez.

| Mevcut kaynak | Kaydedilmiş lisans | Kullanım |
|---|---|---|
| lucide-react 1.52.0 | ISC | Menü, profil, finans ve operasyon ikonları |
| shadcn yerel bileşen kaynakları | MIT | Button, Dialog, dropdown, form/table vb.; LICENSE/NOTICE korunur |
| radix-ui manifest ^1.7.0 | MIT | Dialog ve dropdown davranışı; çözülmüş sürüm lock'tan korunmalı |
| @tanstack/react-table 8.21.3 | MIT | Mevcut yönetim tabloları; sidebar için ek tablo bağımlılığı gerekmez |
| class-variance-authority 0.7.1 | Apache-2.0 | Mevcut varyantlar |
| clsx 2.1.1, tailwind-merge 3.7.0 | MIT | cn/stil bileşimi |
| Tailwind/@tailwindcss-postcss 4.3.3, tw-animate-css 1.4.0 | MIT | Mevcut tema ve animasyon |
| Next/React/React DOM/next-intl | MIT | Route, state, sunum ve çeviri |

Bu, mevcut yerel kayıtların incelemesidir; bu tur upstream/ağ veya binary/container lisans taraması yapılmadı. Dağıtım/telif yükümlülükleri `docs/licenses.md` kapsamında sürer. Yeni lisans yükü yok kararı esas: komut paleti, fuzzy search, tooltip, drawer veya state için yeni paket önerilmiyor; alternatif mevcut Dialog, React state/context, String locale API'leri, mevcut Radix ve projeye ait tipli koddur. Restore/install olmadığı için lisans scripti çalıştırılmadı.

## Onay sonrası önerilen dosya kapsamı

Henüz hiçbir uygulama dosyası değiştirilmedi. Aşağıdaki liste yeni backlog değildir, mevcut Aşama 02–08'in olası uygulama yüzeyidir:

- `frontend/buildingflow-web/src/features/navigation/navigation.ts` (yeni öneri): typed şema, kararlı ID, grup, route, ikon ve gerçek görünürlük koşulları; saf arama/active yardımcıları gerektiğinde aynı feature içinde ayrılır.
- `src/components/WorkspaceShell.tsx`: aynı şemadan sidebar/arama/kısayollar; gerçek link, context ve route davranışlarını koruma. Sidebar alt bileşenleri yalnız okunabilirliğe yararsa ayrılır.
- `src/features/navigation/routes.ts`: mevcut parser/section'ı tekrar kullanma, nested active testleri; `PropertyScope.tsx` ve `PropertyModuleSelection.tsx` yalnız ihtiyaç kanıtlanırsa değişir.
- `src/app/globals.css`: sınırlı shell tokenları, geniş/dar görünüm, focus/active ve reduced-motion; sayfa bazlı toplu CSS rewrite yok.
- `src/auth/AuthGate.tsx`: SessionControls/profil sunumu tekrar kullanılacaksa sınırlı çıkarım; OIDC/rol/yenileme davranışı değişmez.
- `messages/tr.json`, `messages/en.json`: eş anahtarlı grup, arama, aria, collapse ve bağlam metinleri.
- `tests/navigation.test.mjs` (öneri) ve mevcut `tests/dashboard.test.mjs`, `standards.test.mjs`: görünürlük, duplicate ID/route, locale arama, nested route ve mevcut sıra sözleşmesini yeni kabul ile doğrulama.
- `docs/navigation-refactor/IMPLEMENTATION.md`, `TEST_RESULTS.md`, `LICENSE_CHECK.md`: yalnız ilgili sonraki aşamada; TESTING ve ilerleme kaynakları geliştirme yetkisi geldiğinde mevcut protokole göre güncellenir.

Backend, DB, API sözleşmesi, migration, mobil istemci, package manifest/lock, .env ve canlı servisler bu refactor kapsamı dışında kalır. Mevcut API ile bağlam seçimi karşılanamazsa ihtiyaç ayrıca raporlanır; onaysız backend genişletmesi yapılmaz.

## Doğrulama ve durma noktası

Bu tur: dosya/route/izin/placeholder eşlemesi ve yerel lisans kayıtları statik incelendi. Test, lint, typecheck, build, scanner, ağ isteği, servis başlatma ve runtime UI kabulü ÇALIŞTIRILMADI. Önceki TESTING sonuçları bu turun test sonucu değildir. Web package.json'da `npm test`, `npm run typecheck`, `npm run build` var; `lint` scripti yok, varmış gibi raporlanmamalı.

İleride mevcut komutlara ek davranış senaryoları: M/F/P/RESIDENT ve çoklu rol; S/C bayrak kombinasyonları; property yok/yükleniyor/hata/APARTMENT/diğer tip; yabancı tenant/doğrudan URL reddi; hesap değişimi; TR/EN I/İ araması; nested aktif/parent; 320/375/768/1024/geniş ekran; back/forward, Escape/focus dönüşü, scroll lock, reduced-motion ve offline/stale yetki. Backend negatif yetki doğrulaması yalnız mevcut ayrı test altyapısıyla, refactor kapsamına uygun seçilmelidir.

Tek yetkili çıktı bu rapordur. Aşama 01'in teknik engeli yok; sonraki aşamalar kullanıcı onayı bekliyor. Token giriş/çıkış/toplam/kalan context ayrı ayrı: BİLGİ MEVCUT DEĞİL. Hesap kotası sohbet token tüketimi değildir; güncel kota teslim mesajında gösterilir.
