# PERSON FAZ07 — Merkez giriş ve çalışma alanları

18.10.2026. FAZ06 kullanıcı tarafından açıkça kabul edildi; önceki [#11](https://github.com/AkaBayraK/BuildingFlow/issues/11) Done. Kullanıcı FAZ07 kodlamasını onayladı. Bu teslim mevcut [#12](https://github.com/AkaBayraK/BuildingFlow/issues/12) üzerindedir; yeni görev oluşturulmadı. FAZ08 başlatılmadı.

## Önkoşul, kapsam ve etki

Paket MASTER_PROMPT/STANDARDS/PHASES/AGENTS/PHASE07, kök/frontend AGENTS, önceki faz raporları ve merkezi kişi/ilişki/görev sözleşmeleri; Keycloak/AuthGate/OIDC dönüşü, PropertyScope/WorkspaceShell, eski merkez ve mobil session servisleri, nesne/finans kişi görünürlüğü kodu incelendi. Yerel phase/contract/security/verify yönergeleri tek agent ile uygulandı.

Mevcut kullanıcılar için Altunbayrak Rezidans tek müşteri kararı korunur. Gerçek tenant/Property/kişi-hesap veya görev eşlemesi oluşturulmadı. Faz planı: yeni oturum DTO/store/API, giriş sonrası seçim/tek alan rotaları, güncel merkez Property kapısı ve i18n/scope/regresyon testleri. Mevcut şemayı okumak yeterli olduğundan **domain/model/snapshot/migration değişikliği yoktur**. Yeni bağımlılık, font/ikon/görsel ve extension yoktur.

## Uygulanan davranış

- `GET /api/v2/session/contexts`: yalnız aktörün geçerli MANAGEMENT ve bağımsız OWNER/TENANT/RESIDENT ilişkileri; SUPER_ADMIN için açık PLATFORM istisnası. Kararlı sıralama ve gerçek toplamlı sayfalama; birden fazla rol/daire ayrı seçenek kalır. İlk sayfada tek kayıt görülmesi otomatik tek alan sayılmaz.
- `GET /api/v2/session/contexts/{kind}/{id}`: aynı yetkiyi yeniden doğrulayan tek context ve minimum maskeli kendi kişi bilgisi. UUID/kind seçimi grant değildir. İstemci başlığı veya role alanı hak açmaz. Yabancı/iptal/inaktif kaynak404; hatalı girdi400; anonim401.
- Exact issuer+subject hesabı, aktif canonical NATURAL kişi/hesap bağı/tenant üyeliği; yönetimde ayrıca Property üyeliği ve güncel, iptal edilmemiş görev aranır. Tenant/scope/Property/bölüm/üst bina kapanışları ve tarih uçları denetlenir. Kaynağı değişmiş imported kişi veya ilişki gölgesi context yetkisi sayılmaz. Yönetim görevi realm yeteneğini yükseltmez; daire ilişkisi kendisi kaynak olduğu için ayrıca OWNER checkbox/claim'i gerektirmez.
- Keycloak girişinden sonra varsayılan `/` girişinde tek context doğrudan açılır; çoklu/sıfır sonuç `/workspaces` seçimine gider. `/workspaces/{kind}/{id}` detayında daire ilişkisi salt okunur kendi bilgileridir; yönetim bağlantıları server capability'lerine göredir. Mevcut geçerli OIDC derin bağlantıları korunur ve API tarafından tekrar doğrulanır.
- Müşteri/alan/görev/daire etiketli TR/EN seçimi, loading/error/empty/retry, sayfalama ve alan değiştirme bağlantıları mevcut tasarım bileşenleriyle yapıldı. Sekmeye geri dönüşte context yenilenir. Aktör/rota değişiminde istek ve önceki veri temizlenir; PII/context grant'i localStorage/sessionStorage'a veya anonim SSR'a yazılmaz. Hesap bilgileri minimumdur ve `canEdit:false` taşır.
- Yeni akış açıkken merkez Property listesi güncel yönetim görevleriyle DB'de filtrelenir. Property route istekleri için sunucuda finans/yönetim capability kapısı vardır; eski endpoint policy ve store nesne/scope/version denetimleri korunur. Görev iptali sonraki merkez isteğinde alan/kişi erişimini kapatır.
- Finans altındaki kişi seçenekleri/filtreleri de kontrol edildi. Realm MANAGER fakat yalnız ACCOUNTANT görevi bulunan aktör, finans okuyabilir; o Property isteğinde kişi okuma/yazma yeteneği taşımaz. `CurrentUserService` sunucunun hesapladığı istek kapsamını kullanır; JWT/Keycloak rolü yazılmaz. Mevcut finans DTO'sunun kişi görünürlüğü de bu daralmayı izler.
- Global eski users/consents yönetim API'leri canonical tenant scope'u temsil etmediği için opt-in akışta tenant yöneticilerine kapalı tutulur; SUPER_ADMIN platform yönetimi istisnası korunur. Menü bağlantıları da buna uyar. Kendi account/rıza işlemleri önceki kapsamlarıyla kalır.

Uçlar ve activation/rollback ayrıntıları: [session-workspaces.md](../../api/session-workspaces.md).

## Bayrak ve geçiş sınırı

`PersonRefactor:SessionContextsEnabled=false` ve private `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED=false` varsayılandır. API ayrıca PersonRefactor.Enabled ister. Web flag build+runtime'da taşınır, değişince yeniden derlenir. Compose/config örnekleri hazırlandı, gerçek .env ve çalışan servisler değiştirilmedi.

API bayrağı açıkken merkez ManagementAccess global ENFORCED olur; LEGACY/SHADOW fallback yoktur. Hesap bağları/görevler/tenant/Property mapping hazır değilse kullanıcı sıfır context görür; eski membership'ten otomatik hak üretilmez. SUPER_ADMIN açık platform context'iyle ele alınır. Bu global bayrak henüz tenant bazlı rollout kapısı değildir. Yeni Property de doğru scope tanımlanmadan context listesine girmez.

**Mobil yerel model, eski mobile/session/manager/owner/finance/sync protokolleri ve finansın yeni ilişki okuma projeksiyonu bu fazda geçirilmedi.** Bunlar FAZ08–09 ve sonraki yayın kabulü içindir. Merkezdeki görev iptali testleri tüm eski mobil kanalların yeni yetki modeline geçtiği anlamına gelmez. Bu nedenle tüm uygulamada güvenli global cutover tamamlandı veya yeni bayrak canlı açıldı iddiası yoktur. Aktarılmış veride FAZ06 kaynak otoritesi ve native ilişki yazım kısıtı devam eder.

## Değişen dosyalar

- Backend yeni: `Application/SessionContexts.cs`, `Infrastructure/SessionContextStore.cs`, `Api/SessionContextEndpoints.cs`.
- Backend bağlantı/kapsam: `Program.cs`, `CurrentUserService.cs`, `DependencyInjection.cs`, `ManagementAccess.cs`, `PropertyStore.cs`.
- Frontend yeni: `features/session-workspaces/model.ts`, `SessionWorkspaces.tsx`, `app/(workspace)/workspaces/page.tsx`, `workspaces/[kind]/[id]/page.tsx`.
- Frontend giriş/nav: workspace page/layout, `WorkspaceShell.tsx`, `auth/return-path.ts`, mevcut merkezi people rotalarının ortak flag koşulu, TR/EN sözlükleri.
- Aktivasyon: kök/frontend env örnekleri, frontend Dockerfile, compose build/runtime ortamı.
- Kanıt: `SessionContextTests.cs`, frontend `session-workspaces.test.mjs`, bu rapor, API/TESTING/progress/skill kayıtları.

Paylaşılan checkout'ta paralel başka sohbetlerden gelen UI/account/consent/mobile değişiklikleri korundu; bu fazın işi olarak sahiplenilmedi. Commit/push/merge/reset/stash/branch değiştirme yapılmadı.

## Bu tur doğrulama

| Kontrol | Sonuç |
| --- | --- |
| Seçili gerçek PostgreSQL + fixture storage integration | **348/348**, başarısız0, atlanan0 |
| Yeni SessionContext senaryoları | **19** senaryo geniş grubun içinde; önceki18 dar finans denetimiyle ayrıca geçti |
| Backend unit | **187/187**, başarısız0, atlanan0 |
| Frontend Node | **143/143**, başarısız0, atlanan0;8 yeni çalışma alanı sözleşme senaryosu |
| TypeScript | `npm run typecheck` başarılı |
| Next production build | Bayrak kapalı ve açık ayrı derlemeler başarılı; yeni ve eski rotalar birlikte |
| .NET solution build | 0 hata,0 uyarı |
| Compose | `docker compose ... config --quiet` başarılı; servis başlatılmadı |
| Lisans metadata | **329** mevcut npm/NuGet sürümü; yeni paket/manifest/lock değişikliği yok |
| Şema/koruma | Mevcut migration/Designer/snapshot ve proje manifest hashleri aynı; `git diff --check` başarılı |

API senaryoları: tek yönetim context'i, yabancı tenant/gerçek kaynak UUID'si, aynı kişide yönetici+malik+sakin ayrı görünümü; salt daire ilişkisinde yönetim/genel kişi reddi; realm görev yükseltme reddi, accountant göreviyle kişi ve hiyerarşi yazımı reddi; finans içinden kişi seçenekleri/filtreleri403; görev iptali, pasif hesap/bağ/tenant/Property üyeliği/alan/scope/kişi; yanlış issuer; legacy source version sapması; bugünün dahil olması ve geleceğin/geçmişin dışlanması; pasif unit; açık platform istisnası, sayfalama/boş sonuç/flagoff ve sahte context başlıkları.

Geniş filtre SessionContext, PersonCenter, ManagementAssignment, CentralPeople/CentralPerson, PersonTransition/PersonRelation, People/PersonProfiles/PersonCode/UserManagement, Mobile, Migration/Persistence/PropertyModel, Authentication/ReleaseSecurity, Dues/DuesParty/Payment/Report gruplarıdır; bütün backend suite'i çalıştı iddiası yoktur. Integration ve unit csproj'lerinde `.NET test --no-restore --disable-build-servers -m:1` kullanıldı. Web `npm test/typecheck/build`; lisans `python3 scripts/check-licenses.py`.

Bütün DB testleri mevcut script ile ayrılmış geçici DB/rol ve kendi fixture şema/bucket'larında çalıştı. Test rolünün ana projects DB erişimi kapalıydı. Kaynaklar temizlendi. Ana DB, gerçek kullanıcı/rol, hesap parolası, tenant mapping ve servisler değiştirilmedi.

Ara doğrulamada eski users policy'sinin403 yanıtına karşı testteki404 beklentisi düzeltildi. i18n kaynak tarayıcısı için aynı dosyada ayrı namespace alias'ı kullanıldı. Compose girinti hatası giderildi, son config doğrulaması geçti. Son yetki incelemesinde finansın kişi görünürlüğü yeni göreve daraltıldı ve yeniden geniş regresyon çalıştırıldı. Nihai hata yoktur.

## Çalıştırılmayanlar ve riskler

- Kullanıcının önceki tercihi doğrultusunda gerçek Keycloak tarayıcı girişi, canlı UI/klavye/ekran okuyucu ve telefon kabulü yapılmadı: **DOĞRULANAMADI**. Source/DTO/i18n kontrolleri bu kabulün yerine geçmez.
- SAST/Fortify/SCA/CVE/secret scanner veya üretim güvenlik kabulü çalıştırılmadı; lisans metadata kontrolü bunların yerine sayılmaz.
- JWT realm rol değişimleri mevcut token/refresh politikasına tabidir; her istekte Keycloak introspection çağrısı eklenmedi. DB görev/bağ değişikliği yeni merkez isteğinde etkilidir, başlamış v1 transaction'ı geriye dönük iptal mekanizması değildir.
- Global eski users/consents tenant yöneticisi işlevleri için canonical scope uyarlaması olmadan bu yeni bayrakla genel canlı açılış tamamlanmış sayılamaz. Merkez dışı mobil kanalların yeni modele geçişi de bekler.
- Önceki fazların bekleyen migrationları sonraki AutoMigrate açık API başlangıcında uygulanabilir; bu fazda servis restart veya migration apply yapılmadı.

## Geri dönüş ve ilerleme

Yeni veri/migration yoktur. Web bayrağını kapatıp yeniden derlemek önceki giriş ekranını getirir. API session bayrağını kapatmak v1 merkez görev kapısını kaldırıp eski rol+üyelik erişimini yeniden açabilir; bu bir yetki geri alma işlemi gibi otomatik uygulanmaz. V2'de explicit ENFORCED tenant ayarları korunur. Canlıda hiçbir geri dönüş işlemi çalıştırılmadı.

Teknik altı resmî adım **6/6=%100,00**. FAZ07 kullanıcı kabulü ayrıca bekler, #12 In review. Kabul edilmiş fazlar **6/11=%54,55**; FAZ08 başlamadı. Mevcut ürün/İŞ/UI/mobil roadmap sayaçları değiştirilmedi. Sonraki adım kullanıcı FAZ07 kabulü ve FAZ08 başlangıç onayıdır.

Codex usage (18.10.2026):300dk kullanılan%75/kalan%25, reset18.10.2026 05:15:34;10080dk kullanılan%12/kalan%88, reset25.10.2026 00:15:34 Europe/Istanbul. Hesap pencereleri sohbet token tüketimi değildir. Giriş/çıkış/toplam token ve kalan context: BİLGİ MEVCUT DEĞİL.
