# Sidebar navigasyon refactor teslimi

20.10.2026 — SIDEBAR FAZ07.1, **8/8 resmi aşama, %100,00 teknik teslim**. Plan [MASTER_PROMPT](../../buildingflow-sidebar-phase/MASTER_PROMPT.md) ve [PHASE_07_1](../../buildingflow-sidebar-phase/PHASE_07_1.md). Bu kimlik mevcut İŞ07.1 hata/bildirim altyapısından ve 12 aşamalı tarihsel UI planından ayrıdır. Kök MASTER_PROMPT.md/PHASES.md yoktur; tarihsel UI belgeleri frontend/buildingflow-ui-plan altındadır. Ürün roadmap işaretleri değiştirilmedi. Açık canlı kabul sınırları [test raporunda](TEST_RESULTS.md).

## Uygulanan davranış ve kararlar

- Tek tipli navigasyon modeli altı grubu, mevcut route/rol/feature flag ve doğrulanmış UUID alan bağlamını üretir. Enabled ve routeless disabled öğeler ayrı tiptedir. Nested aktiflik farklı alan/prefix çakışmasını reddeder. APARTMENT bina bağlantısı üretmez. Backend üyelik/yetki otoritesi korunur; menü görünürlüğü erişim izni değildir.
- 272px geniş,76px dar sidebar; navy/mavi vurgu,44px satır, aktif çizgi, focus/hover/disabled. Kullanıcının düzeltmesiyle üst arama/sidebar/drawer menü metinleri ortak12px token kullanır. Grup başlıkları11px'tir.
- Genel Bakış/Mülk Yönetimi/Finans/Operasyonlar/İletişim/Sistem Yönetimi. Çalışan /users korunur; aynı adlı routesuz placeholder kaldırıldı. Sistem Ayarları/Tanımlar/Ortak Alanlar/Otopark/Tedarikçiler mevcut planlı ve pasiftir.
- Arama yalnız modelin görünür, enabled etiketlerini tarar; TR/EN case/aksan/Türkçe ı uyumu, çoklu kelime ve boş sonuç. Cmd/Ctrl+K; kapanışta sorgu temizlenir. Kişi/alan verisi, API araması veya sorgu kaydı yoktur.
- `buildingflow-sidebar-v1` yalnız collapsed/expanded UI tercihini saklar; storage erişim hatasında fallback. Rol,token,alan veya kişi verisi saklamaz.
- Mobil ortak menü Radix Dialog içinde; backdrop/Escape/focus trap/scroll lock, rota ve masaüstüne geçişte kapanma. Rota kapanışında içerik odağı, normal kapanışta açan düğmeye dönüş. Reduced-motion CSS, aria-current/expanded ve içeriğe geç bağlantısı vardır.
- Footer mevcut ortak account/Keycloak oturumundan profil/rolü gösterir; alt alan seçim bağlantısı 21.10.2026 kullanıcı talebiyle kaldırıldı. Subject uyuşmazlığında eski profil kullanılmaz; accountChanged ortak provider'ı günceller. Dashboard alan seçimi üst headerdadır; yeni hesap/rol/üyelik işlemi yok. Header dil/bildirimleri korunur; profil footer ile birlikte sağ üst header üzerinden de sunulur (20.10.2026 kullanıcı düzeltmesi).
- OIDC Authorization Code/S256, refresh/logout, API ve DB sözleşmeleri korunur. Yeni route, migration, servis, paket/font/ikon seti veya harici görsel eklenmedi.

## Değişen uygulama ve test dosyaları

Aşağıdaki liste sidebar kapsamıdır; Git'teki diğer mevcut kullanıcı farklarını bu işe mal etmez.

| Dosya (frontend/buildingflow-web altında) | Amaç |
|---|---|
| src/features/navigation/navigation.ts | Tipli gruplar/izin/aktiflik/alan seçim hedefi |
| src/features/navigation/search.ts | Etiket araması |
| src/features/navigation/sidebar-preference.ts | Sınırlı UI tercihi |
| src/features/navigation/SidebarHint.tsx | Mevcut Radix dar mod tooltip |
| src/components/WorkspaceShell.tsx | Ortak sidebar/drawer/arama/footer ve odak |
| src/auth/AuthGate.tsx | Mevcut oturum profilinin ortak provider/menu sunumu |
| src/features/account/model.ts | Profil subject eşliği |
| src/components/ui/dialog.tsx | Overlay reduced-motion sınıfı |
| src/app/globals.css | Sidebar/header/drawer/footer stilleri |
| messages/tr.json, messages/en.json | Menü/grup/arama/footer metinleri |
| tests/navigation.test.mjs, navigation-search.test.mjs, navigation-footer.test.mjs, sidebar-preference.test.mjs | Model/arama/profil/tercih davranışları |
| tests/dashboard.test.mjs | Ortak modelle Dashboard kabulü |
| scripts/sidebar-fixture.mjs; tests/fixtures/sidebar-{entry.tsx,mocks.tsx,keycloak.ts,api.ts,loader.cjs} | Loopback test adaptörleri; üretim route/auth bypass içermez |

Belgeler: ANALYSIS ve STAGE_02–07 raporları/JSON/JPG kanıtları, bu IMPLEMENTATION, TEST_RESULTS, LICENSE_CHECK; docs/TESTING.md, docs/frontend-rules.md, docs/SKILLS_AND_AGENTS.md ve docs/progress kayıtları. Menü font bakımı MENU_FONT_FIX.jpg ile kayıtlıdır. İki mevcut next-env farkı sidebar değişikliği sayılmaz; build sonrası başlangıç byte'ları korunmuştur.

## Geri alma planı

1. Mevcut staged/unstaged/untracked durumu ve kullanıcı farklarını güvenli yerel yedekte koru. Bu teslim commit oluşturmaz; HEAD'i otomatik başlangıç varsayma.
2. Önceki onaylı frontend dağıtım artefaktı varsa yalnız frontend'i o sürüme döndür. Backend/DB migration geri alma veya Keycloak/hesap değişimi gerekmez.
3. Kaynaktan geri almada yukarıdaki sidebar kapsamını dosya/hunk düzeyinde karşılaştırarak önceki onaylı uygulamayı geri getir. WorkspaceShell/AuthGate/globals/messages/account/dialog/dashboard ortak değişikliklerini birlikte incele; tüm dosyaya reset/checkout/clean uygulama. Yeni navigation modüllerini ancak bütün üretim importları geri alındıktan sonra kaldır; kullanıcıya ait dosyaları silme.
4. Fixture/test importlarını da aynı kapsamla eşleştir. LICENSE/NOTICE ve teslim kanıtlarını koru. İsteğe bağlı UI tercihi anahtarı zararsızdır; temizleme gerekirse yalnız buildingflow-sidebar-v1, toplu localStorage temizliği yok.
5. Önceki sürümde npm test/typecheck/build ve rol/alan/desktop-mobile smoke çalıştır; sonucu TESTING'e tarihli kaydet. Bu plandaki geri alma işlemleri uygulanmadı.

## Sonraki kabul

[Manuel QA listesi](TEST_RESULTS.md) gerçek oturum/ağ/cihaz kabulünü içerir. Sonraki ürün fazı kendiliğinden başlatılmaz. Teknik teslim %100,00, gerçek yayın/cihaz/offline kabulü tamamlandı anlamına gelmez.


## 21.10.2026 — Son sidebar sunumu

İzin/rota kaynağı `navigation.ts` korunur;renderdüzeni `sidebar-layout.ts` ile referanstaki üst/alt menü biçimine çevrildi. CSSlacivert/cyanyuvarlatılmış çerçeve ve sidebararama eklendi. Profilkartı kalır;altalanseçimi geri eklenmez. [Güncel tasarım ve kanıt](SIDEBAR_REFERENCE.md).


## 21.10.2026 — BAKIM / Aidat tahsilat kartı

**1/1 doğrulandı.** [API sözleşmesi](../api/dashboard.md), [kabul kanıtı](DASHBOARD_COLLECTION_EVIDENCE.json) ve [görsel](DASHBOARD_COLLECTION.jpg). Mevcut dashboard işinin sunum/özet genişletmesidir; yeni ürün fazı açılmaz, SIDEBAR 8/8 ve ürün yol haritasındaki tamamlanmış İŞ20 durumu korunur.

21.10.2026 BAKIM — [Kişisel hava şehri ve servis seçenekleri](DASHBOARD_WEATHER.md): **1/1**, frontend193/193/typecheck/build ve UI fixture başarılı. Canlı sağlayıcı henüz bağlanmadı; faz/roadmap işareti değiştirilmedi.

21.10.2026 BAKIM — Kullanıcı onaylı MET Norway entegrasyonu **1/1** tamamlandı; yeni ürün fazı açılmadı. [Sözleşme/kanıt](DASHBOARD_WEATHER.md), [görsel](DASHBOARD_MET_WEATHER.jpg). Önceki “sağlayıcı henüz yok” notu önceki turun tarihsel durumudur.

21.10.2026 BAKIM — Yeni tahakkuk paneli masaüstünde sol seçim/sağ tahakkuk bilgileri olarak iki sütun, mobilde tek sütun yapıldı. **1/1**,207/207FE/typecheck/build ve gerçek form fixture doğrulandı. [Görsel](ASSESSMENT_TWO_COLUMNS.jpg), [kabul](../TESTING.md). Yeni ürün fazı/roadmap işareti yok.

21.10.2026 BAKIM — Sidebar masaüstü/mobil tek açık grup; rota değişiminde aktif grup açılır, manuel başka grup seçimi aktif grubu kapatabilir. Önceki zorunlu aktif açık/grup listesi davranışı güncellendi. **1/1**,207/207FE/typecheck/build ve gerçek shell etkileşimleri PASS. [Görsel](SIDEBAR_ACCORDION.jpg), [kabul](../TESTING.md). Ürün fazı/roadmap değişmedi.

21.10.2026 BAKIM — Tema seçimi **1/1** tamamlandı: Açık/Koyu/Sistem, kalıcılık, OS/sekme eşliği, nonce korumalı ilk boya ve koyu semantik palet.210/210FE/typecheck/build, production nonce ve React/mobile/form kabulü PASS. Yeni ürün fazı/roadmap işareti yok. [Görsel](THEME_DARK.jpg), [kanıt](THEME_EVIDENCE.json), [kabul](../TESTING.md).
