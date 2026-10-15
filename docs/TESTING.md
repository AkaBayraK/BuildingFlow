## BAKIM — Daire/kişi bağlantıları ve iPad Air 11: TAMAMLANDI

11.10.2026 kullanıcı Daireler Kiracı/Ev sahibi (Malik) kolonları, isimden kişi bilgilerine ve kişi ilişkisindeki bağımsız bölümden kimliğiyle filtrelenmiş daire listesine geçiş; iPad Air 11 önizlemesi istedi. **Bu tur3/3=%100.00**: mevcut sözleşme analizi, iki yönlü gezinme/tablet düzeni, otomatik+tarayıcı kabulü ve teslim. İlişkili **İŞ04 kayıt05/14**; resmi **83/83** paydası değişmedi, mevcut görevde kanıt güncellendi.

- [x] **Güncel kiracı/malik:** Daireler tablosunda iki varsayılan kolon; tablet/mobil kartlarda iki etiket ve ad bağlantıları. Europe/Istanbul bugünü, start≤bugün≤end (iki uç dahil); OWNER/TENANT çoklu kişiler ayrı bağlantıdır. Geçmiş/gelecek/RESIDENT bu kolonlara girmez. İlişki yok ile yükleme hatası farklı gösterilir; yenileme vardır. Finans kişi snapshot'ı değiştirilmez.
- [x] **Güvenli sayfa batch'i:** `GET /api/v1/properties/{id}/unit-parties?unitIds={uuid}&unitIds={uuid}` yalnız yönetici kişisel veri policy'sine açıktır. İstek1–100 farklı dolu UUID; seçim bütünü aynı yetkili Property'ye ait olmalıdır. Tek tutarlı read transaction; yanıt `{propertyId,asOfDate,items:[{unitId,buildingId,owners:[{personId,personName}],tenants:[...]}]}`. Telefon/e-posta bu özette yok. ACCOUNTANT/RESIDENT/sahte rol/anonim erişimi açılmaz. FE yalnız görünen sayfa için tek batch yapar; yetkisiz rolde çağrı/kolon yok. Abort ve eski sayfa sonucunu reddeden anahtar korunur.
- [x] **İki yönlü gezinme:** Kiracı/malik adı → kişi bilgileri (ad, telefon, e-posta, durum) + ilişkiler. Kişi sağ detay özeti ve ayrı kişi sayfasındaki bölüm hücresi/kartı → canonical Property + varsa Building + tam `unitId` filtreli liste. Tam UUID backend'de sayfalama/count öncesi uygulanır; başka Property/bina bölümüne eşleşmez. “Tüm bağımsız bölümleri göster” query'yi kaldırır. Geçmişte silinmiş bölüm bağlantısı boş liste gösterir, ilgisiz bölümlere genişlemez. Bilinmeyen/silinmiş üst kayıt standart404 olur. Hatalı/tekrarlı FE query kapalı reddedilir. OIDC yalnız kanonik bölüm listesinde tek dolu UUID query'sini saklar; token/ek query/fragment/dış URL reddedilir. Hatalı hard-navigation OIDC'de güvenli genel bakışa döndü; query guard unit testlerinde, temizleme eylemi geçerli filtreyle tarayıcıda doğrulandı.
- [x] **13 yeni gerçek PostgreSQL senaryosu:** `UnitPeopleNavigationTests` (2 apartman/binalı güncel kişiler,3 yönetici rolü,1 bounded/membership/yabancı batch,5 soft-delete hedefi,2 tam kimlik/kapsam filtresi). Ortak malik ve aynı gün başlangıç/bitiş; eski/gelecek/sakin ayrımı, boş ilişki; malformed/empty/duplicate/>100 UUID; yabancı seçim ve silinmiş üyelik; person/occupancy/unit/property/building soft-delete; bir daire–benzer numara ayrımı, yabancı Property/bina,400/401/404. `PeopleApiTests` mevcut anonim ve5 sahte/yetersiz rol senaryosu yeni endpoint'i de kapsar. Hedefli **31/31** geçti.
- [x] **Son .NET regresyonu:** `bash scripts/test.sh --no-build --no-restore --verbosity minimal` (yerel /tmp .NET10 SDK ortamıyla): **464/464 (101 unit +363 integration), hata/atlama0**. Fixture'ların ayrı PostgreSQL şemaları/veritabanları, özel test MinIO/RabbitMQ/Mailpit kaynakları kullanıldı. Yeni read/API kodu önce `dotnet build ... --no-restore -m:1 --disable-build-servers` ile derlendi; uyarı/hata0. Schema/model/migration sözleşmeleri mevcut suite'te geçti, yeni schema yok.
- [x] **8 yeni FE davranış testi:** `unit-people-links.test.mjs` direkt/binalı canonical hedef; malformed/duplicate filtre; OIDC query güvenliği; çoklu/boş kişiler; eksik/yabancı/tekrar batch DTO; geçersiz kişi/tarih; occupancy rota UUID'si; aynı Property'de yanlış kişi DTO'si. **Son npm test99/99**, hata/atlama0. Son `npm run typecheck` / `npm run build` çıkış0. TR/EN ve merkezi bildirim/transport regresyonları dahil. Yeni bağımlılık yok; `check-licenses.py` **331**, manifest/lock/.env + tüm migration/snapshot **28/28 hash eşliği**, yeni migration0.
- [x] **Girişli Chrome kabulü:** mevcut ilişki üzerinden kişi→tek doğru daire→kiracı ve malik kişilerine→daire; filtreyi temizleyince48 kayıt listesi; TR→EN→TR. **820×1180** dikey: belge805/scroll805; iki kart sütunu, tek filtreli sonuç tam genişlik. **1180×820** yatay:1165/1165, iki kart sütunu; yeni daire sağ panel512px, viewport içine sığıyor, kaydetmeden kapandı. **390×844**:375/375, bağlantılar44px; **1440×900**:1425/1425, tabloda Kiracı/Ev sahibi kolonları. Kişi iletişim/ilişki sayfası ayrıca820×1180'de805/805. Yatay belge taşması0. Kişi adları kartta özelliklerden önce, çoklu bağlantılar satıra taşabilir.
- [x] **Yerel çalışma/koruma:** yalnız BuildingFlow API5005 yeni read kodunu yüklemek için yeniden başlatıldı, `/health` Healthy. Açılış `Entity model and migrations are in sync`; schema/migration dosyaları değişmedi. FE3003 çalışıyor. Kullanıcı iş kaydı oluşturulmadı/düzenlenmedi/silinmedi; tarayıcıdan başarılı mutation yapılmış sayılmaz. Önceki FE detay eşliği, .env, lisans/NOTICE kopyaları ve kullanıcı verisi korunur. Tek agent, ek skill yok; commit/merge/push yok.

**Ekran kanıtları:** [iPad dikey](screenshots/unit-people-ipad-portrait.png), [iPad yatay](screenshots/unit-people-ipad-landscape.png), [masaüstü kolonları](screenshots/unit-people-desktop.png). Chrome'daki tablet önizleme sekmesi açık bırakıldı; fiziksel iPad/iPadOS Safari, dokunmatik klavye veya tüm modüllerin cihaz kabulü yapılmış sayılmaz. Ölçü seçimi: [Apple11-inç ekran2360×1640](https://support.apple.com/en-gb/119894); @2 ölçekten820×1180 CSS önizlemesi **çıkarımdır**, gerçek Safari araç çubuklarını emüle etmez.

**Etkilenen kaynaklar:** BE People/Hierarchy Application+Store+endpoints ve integration tests; FE UnitPeople/unit-links, HierarchyRows, RecordList, PersonPage/PersonOccupancies/RecordDetails, ManagementPage+iki unit page route, auth return-path/AuthGate, management-contracts ve TR/EN+İŞ04 kanıtı. Sözleşme [people.md](api/people.md), [hierarchy.md](api/hierarchy.md); kalıp [frontend-rules.md](frontend-rules.md).

## BAKIM — Apartman/daire yeni alanlarının frontend eşliği: TAMAMLANDI

11.10.2026 kullanıcı Genel Bilgiler ve Daireler ekranlarındaki eksik yeni backend kolonlarını istedi. Mevcut BuildingDetails/APIdetails sözleşmesi kullanıldı. **Bu tur 3/3 = %100.00** (sözleşme analizi, form/detay/tablo eşliği, doğrulama ve teslim); ilişkili İŞ03 kayıt04/14, resmi görev/payda değişmedi: **genel 83/83**.

- [x] **Genel Bilgiler / oluşturma / düzenleme:** mahalle/semt bütün türlerde; yapım yılı, ısıtma, asansör, otopark yalnız APARTMENT Property üzerinde. Diğer türlerde bu dört alan Building formu/detayı/tablo/mobil kartındadır. Değiştirilmeden yeniden kaydedilen veya 409 sonrasında yüklenen nullable değerler korunur. Yıl 2023 biçiminde, binlik ayırıcı olmadan gösterilir.
- [x] **Daireler:** net alan, oda düzeni, kat konumu, ara kat, banyo sayısı, mutfak, balkon, ısıtma ortak sağ panelde oluştur/düzenle ve güncel detayda. Masaüstü tabloda net alan/oda düzeni varsayılan görünür; diğer özellikler Sütunlar menüsünden açılır. Mobil kartlar tüm özellikleri içerir. Yeni alanlarda desteklenmeyen backend sort parametresi gönderilmez.
- [x] **Yeni altı davranış testi — tests/property-details.test.mjs:** backend enum isimlerinin UI seçenekleriyle birebir eşliği; null/false/0 ve virgüllü net alan payload'ı; SITE'a gizli apartman özelliklerinin taşınmaması; net≤brüt/pozitif/iki ondalık, yıl/kat-konumu/banyo/metin sınırları; Property/Building/Unit alanlarının korunması ve yabancı ebeveyn reddi; bozuk boolean/enum/net/kat ve tekrar DTO satırı reddi.
- [x] **Son otomatik FE kabulü:** frontend klasöründe `npm test` **91/91**, başarısız/atlanan **0**; `npm run typecheck` ve `npm run build` çıkış **0**. TR/EN sözlük, ICU, roadmap, merkezi 8 saniyelik hata/uyarı, yetkili transport, OIDC/SSR güvenlik ve mevcut finans regresyonları dahil. `python3 scripts/check-licenses.py` **331**; `git diff --check` geçti. Paket/lock değişmedi.
- [x] **Girişli Chrome yerel kabulü:** gerçek Property GET değerleri Genel Bilgiler ve edit formunda görüldü; Daireler tablo sütunları ve Balkon açma, yeni/düzenle/detay panelleri, boş alanlarda Belirtilmedi, TR→EN→TR seçenek/metin eşliği doğrulandı. Numarası boş bırakılmış formda net120/brüt100 gönderimi frontend tarafından reddedildi, Türkçe net alan hatası ortak bildirime gitti. **390×844** tam genişlik panel ve mobil kartlar; belge genişliği **390**, yatay taşma **0**. Canlı iş kaydı oluşturulmadı/düzenlenmedi/silinmedi; başarılı POST/PUT tarayıcıdan yapılmış sayılmaz. Viewport ve dil geri alındı, test sekmeleri kapatıldı.
- [x] **Kaynak koruması:** bu kapsamın başlangıcındaki **149** BE kaynak/migration/test, `.env` ve FE manifest/lock hash'i aynı. Önceki oturumun **451 .NET** kanıtı yeniden çalıştırılmış sayılmaz; BE/DB değişikliği olmadığından bu tur frontend testleri çalıştı. Tek agent; ek skill veya dış asset yok. İŞ11 incelemesinde alınmış resmi MIT action lisans metinleri korunup licenses.md'ye bağlandı.

**Etkilenen kaynaklar:** `features/property-details/{model.ts,DetailFields.tsx}`; `services/{api.ts,management-contracts.ts}`; `components/management/RecordList.tsx`; `features/properties/{PropertiesWorkspace.tsx,PropertyDetail.tsx}`; `features/hierarchy/{EntityEditor.tsx,HierarchyRows.tsx,RecordDetails.tsx,BuildingDetail.tsx}`; TR/EN sözlükler ve mevcut İŞ03 roadmap kanıtı. Mimari/kural/ilerleme belgeleri güncellendi. Commit/merge/push yapılmadı.

**Ekran kanıtları:** [Genel Bilgiler](screenshots/property-details-general-desktop.png), [Daire tablosu](screenshots/property-details-unit-table-desktop.png), [Sağ form](screenshots/property-details-unit-form-desktop.png), [Mobil form](screenshots/property-details-unit-form-mobile.png). Sözleşme [hierarchy.md](api/hierarchy.md); kalıp [frontend-rules.md](frontend-rules.md).

## BAKIM — Açılışta otomatik migration ve apartman/daire kolonları — 11.10.2026

Kullanıcı talebi: entity'ye eklenen kolonlar tabloya gelmiyor, bir kolon elle eklenmiş; Liquibase benzeri, çalışınca kolonları oluşturan altyapı. Dal `feature/apartment-unit-details`; yeni ürün fazı değildir.

- [x] **Hatanın nedeni (kanıt):** dalın yeni kodu `units.bathroom_count`, `properties.has_elevator` gibi 17 kolonu sorguluyor; `20261011082637_ApartmentAndUnitDetails` ana DB'ye hiç uygulanmamıştı. Salt-okunur sorgu `42703 column u.bathroom_count does not exist` verdi. Elle eklenmiş iki kolon bulundu: `properties.construction_year bigint` ve `properties.neighborhood varchar(200)`, ikisi de boş. Migration olduğu gibi uygulansaydı "column already exists" ile duracaktı.
- [x] **Migration düzeltmesi (uygulanmamış yeni migration):** 17 `AddColumn` → `ADD COLUMN IF NOT EXISTS` + tip/NULL/varsayılan hizalama. Down veri koruması değişmedi. Eski migration'lara dokunulmadı.
- [x] **Otomatik migrate:** `DatabaseMigration.ApplyPendingAsync` (PostgreSQL advisory lock, entity-migration uyumsuzluğunda açılmama, adlarıyla log); API `Database:AutoMigrate` (Development varsayılan açık) ve `--migrate` aynı yolu kullanır. `db.sh sync` + `dev.sh api` entegrasyonu. Rehber [MIGRATIONS.md](MIGRATIONS.md).
- [x] **Yeni testler (her biri kendi geçici veritabanında):** boş DB'de açılışta tüm migration'lar; bekleyen migration açılışta uygulanır ve veri korunur; elle eklenmiş `bigint`/`varchar(200) NOT NULL DEFAULT` kolonlar `integer`/`varchar(100)` NULL'a hizalanır ve değer korunur; 150 karakterlik veri sığmayınca API açılmaz, kısmi şema kalmaz; ayar kapalıyken DB'ye dokunulmaz; advisory lock tutulurken ikinci migration bekler, kilit bırakılınca tamamlanır, tekrar çağrı boş döner.
- [x] **Dal hataları:** yeni kolonlar yüzünden 8 eski yükseltme testi (`Dues/Expense/Communication/Messaging/Meter`) başarısızdı: eski şema noktasında güncel modelle kayıt ekliyorlardı. `LegacySchema.BeforeApartmentDetails()` o noktada yeni alanları yok sayıyor; satır karşılaştırması yeni boş kolonları dışlıyor; model-migration eşliği güncel context ile ayrıca doğrulanıyor.
- [x] **`db.sh sync` provası (repo kopyasında):** `varchar(100)→50` değişikliği reddedildi, dosya üretilmedi, snapshot geri yüklendi; yeni alan için yalnız `AddColumn` içeren `AutoSchema_*` üretildi; ikinci çalıştırma "in sync". Temiz kopyada eksik restore hatası bulundu, `sync`'e restore eklendi.
- [x] **Ana yerel DB:** önce `pg_dump` yedeği `~/buildingflow-backups/buildingflow-before-ApartmentAndUnitDetails-20261011-134542.dump` (dizin 700, dosya 600, 23 tablo verisi). API `dev.sh api` ile yeniden başlatıldı; log `applying 1 migration(s): 20261011082637_ApartmentAndUnitDetails` → `applied`. 17 kolon mevcut, elle eklenenler `integer` / `varchar(100)`, kayıt sayıları aynı (7 Property / 4 Building / 53 Unit), API log'unda Error/Critical yok, `/health` 200. Başka veri değiştirilmedi.
- [x] **Frontend hata sözleşmesi:** dalın eklediği 10 yeni `BF_*` kodu frontend kayıt defterinde ve TR/EN çevirilerinde yoktu (`api-feedback` testi kırıktı); kodlar ve `neighborhood` alan etiketi eklendi.
- [x] **Regresyon:** `bash scripts/ci.sh` çıkış 0 — **451/451 .NET (101 unit + 350 integration), 85/85 FE, hata/atlama 0**, typecheck/build, npm audit 0, NuGet audit temiz, idempotent SQL'in geçici DB'ye iki kez uygulanması (**12 migration**).

**Sınırlar:** Girişli tarayıcı kabulü (apartman/daire liste, oluştur/düzenle) yapılmadı: geliştirme hesabının parolasını oturum kaydına taşımamak için giriş formu doldurulmadı. Ekranların kullandığı API yolları gerçek PostgreSQL entegrasyon testleriyle doğrulandı. Frontend kodu bu dalda değişmediği için FE3003 yeniden başlatılmadı. Push/merge yok.

## main birleştirme ve yerel servis güncellemesi — 11.10.2026

Kullanıcı onayıyla `faz-10-11-release` dalı `main`'e `--no-ff` ile birleştirildi (çakışma yok; main bu dalın atasıydı). Push yapılmadı, remote tanımlı değil.

- [x] **main üzerinde CI:** `bash scripts/ci.sh` çıkış 0 — **426/426 .NET (88 unit + 338 integration), 85/85 FE**, build, npm audit 0, NuGet audit temiz, preflight ve CI kapısı testleri, idempotent SQL provası (11 migration).
- [x] **Ana DB migration:** salt-okunur kontrol: `__EFMigrationsHistory` 11 migration'ın tamamını, `20261011043002_MessagingOutboxAndInbox` ve `20261011064227_MetersAndReadings` dahil, zaten içeriyor. Uygulanacak migration olmadığından DB'ye yazılmadı ve migration öncesi dump gerekmedi.
- [x] **Servisler:** API (5005) birleştirmeden sonra kullanıcının IntelliJ terminalinden yeni kodla yeniden başlatılmıştı; ona dokunulmadı. Yanıt başlıkları `no-store`, `nosniff`, `DENY`, `no-referrer`, `/health` 200. Worker eski koddu; durdurulup `bash scripts/dev.sh worker` ile yeniden başlatıldı (log `artifacts/logs/worker.log`, Git dışı). SMTP bu modda kapalı, mail gönderilmez. Outbox sorguları hatasız.
- [x] **Smoke:** `scripts/smoke.sh` **15/15**, `scripts/test-observability.py` **11/11** (yeniden başlatılan worker'ın Loki logu ve outbox sayacı dahil).

**Düzeltme:** İŞ11 adım04 kaydında gözlemlenebilirlik sonucu yanlışlıkla 12/12 yazılmıştı; betikte 11 kontrol var, kayıtlar 11/11 olarak düzeltildi.

## İŞ11 adım04 ve faz teslimi — Kabul ve yayın rehberi: TAMAMLANDI — 11.10.2026

**TAMAMLANDI: 4/4 resmi görev = %100.00; genel iş 83/83 = %100.00.** Rehber [RELEASE.md](RELEASE.md), yedek [BACKUP_RESTORE.md](BACKUP_RESTORE.md). Bu oran yol haritası adımlarını sayar; üretime dağıtım yapıldığı anlamına gelmez.

- [x] **Son regresyon:** `bash scripts/ci.sh` çıkış 0 — **426/426 .NET (88 unit + 338 integration), 85/85 FE, hata/atlama 0**; typecheck, production build, `npm audit` 0, NuGet audit temiz, **331 lisans**, CI kapısı 6/6, preflight testi, EF model eşliği, idempotent SQL'in geçici DB'ye iki kez uygulanması (11 migration). Yedek provası `test-backup-restore.py` 8/8 (adım03, kod değişmedi). Domain akışları ve negatif API vakaları (rol/üyelik/JWT/version/replay) bu integration suite'inde.
- [x] **Yayın preflight:** `scripts/release-preflight.py` üretim env dosyasını 15 kurala göre doğrular, değer yazdırmaz. `scripts/test_release_preflight.py`: doldurulmuş `deploy/production.env.example` kabul, SMTP kapalı varyant kabul, **21 güvensiz varyant** (HTTP issuer/metadata/API, wildcard/path CORS, wildcard/localhost host, CIDR/0.0.0.0 proxy, otomatik forwarded, limit kapalı, HTTP web modu, issuer uyuşmazlığı, zayıf/boş sır, yerel/düz metin/güvensiz SMTP, `.local` gönderen, eksik SMTP parolası) reddedildi; çıktıda değer yok.
- [x] **Bulgu ve düzeltme — HSTS:** web HSTS'i `next.config.ts` içinde build-zamanı `BUILDINGFLOW_PUBLIC_HTTPS` değerine bağlıydı ve web Dockerfile bu değeri build'e geçirmediği için imajda hiç açılmazdı. HSTS `proxy.ts`'e taşındı; çalışma zamanında okunur. HTTPS URL'leriyle build edilen production çıktısında `false` → HSTS/`upgrade-insecure-requests` yok, `true` → ikisi de var. Varsayılan (HTTP) build + `true` → `/login` 500, `/health` 200 (kapalı başarısızlık; preflight bu durumu yakalar).
- [x] **Gerçek pipeline smoke (salt-okunur, yerel stack):** `scripts/smoke.sh` **15/15** (web/API/DB sağlık, OpenAPI, anonim ve bozuk JWT 401, Keycloak discovery, Mailpit, RabbitMQ, MinIO, Grafana, Redis, worker). `scripts/test-observability.py` **11/11**. Worker log kontrolü ilk çalıştırmada başarısız oldu: worker 48 dk önce başlamıştı, boşta yalnız açılışta log yazıyor ve test son 10 dk'ya bakıyordu. Loki'de son 60 dk'da worker kaydı bulunduğu doğrulandı; pencere 12 saate genişletildi, sonra geçti.
- [x] **Anonim SSR:** oturumsuz 30 yönetim rotası (liste ve 3 gerçek Property altında 8 alt sayfa) FE3003'te 200 döndü. Ana DB'den salt-okunur `BEGIN READ ONLY` ile okunan 14 Property/Building/Person/Unit değerinin hiçbiri HTML'de yok. Çeviri kataloğundaki örnek metinlerle aynı olan 2 bina adı karşılaştırma dışında tutuldu; değerler yazdırılmadı, geçici dosya silindi.
- [x] **TR/EN/mobil:** uygulama içi tarayıcıda `/login` EN'e geçti; yeniden yüklemede `lang=en` korundu, locale cookie JS'den okunamıyor (httpOnly). 375×812'de yatay taşma yok. Dil TR'ye, viewport masaüstüne geri alındı.

**Sınırlar:** Dış domain/TLS, gerçek SMTP, CI sunucusu ve uzak yedek hedefi yok; workflow GitHub'da çalıştırılmadı. Girişli rol bazlı tarayıcı kabulü (form geri bildirimi, sağ panel) bu fazda tekrarlanmadı; önceki fazların kanıtları ve FE testleri geçerli. Kullanıcı verisinde mutation, canlı yedek veya restore yapılmadı. Çalışan yerel API (5005) ve worker süreçleri yeniden başlatılmadı; Faz11 kodu testlerde ve geçici production build'de doğrulandı. Production go-live kullanıcının hedef ortamıyla ayrıca yapılır.

**Olay notu:** kayıt yazımı sırasında tırnaksız heredoc yüzünden `bash scripts/ci.sh` bir kez istem dışı çalıştı. Yalnız izole test kaynaklarını kullandı; süreç/kalıntı kalmadı, dosya değişmedi, korunan 26 dosya aynı. Yukarıdaki sonuçlar bu çalıştırmaya dayanmıyor.

## İŞ11 adım03 — Yedekleme ve geri dönüş: TAMAMLANDI — 11.10.2026

**3/4 resmi görev = %75.00; genel iş 82/83 = %98.80.** Rehber [BACKUP_RESTORE.md](BACKUP_RESTORE.md).

- [x] **Fixture provası:** `python3 scripts/test-backup-restore.py` **8/8, iki ardışık çalıştırma**. Tamamen kendi `buildingflow-test-backup-*` kaynakları: idempotent SQL ile 11 migration + 2 satırlı uygulama DB'si, gerçek Keycloak 26.7.0 ile realm + kullanıcı (ayrı PG), MinIO'da 3 nesne (1 B–70 KB).
- [x] **Tutarlılık kapıları:** Keycloak/MinIO yazıcıları çalışırken yedek reddedildi ve klasör oluşmadı; durdurulunca yedek alındı (dump öncesi/sonrası DB parmak izi eşit). Klasör `0700`, dosyalar `0600`; SHA256 manifest + nesne bazında hash. Var olan yedek klasörünün üzerine yazılmaz.
- [x] **Negatifler:** tek bit bozulmuş dump, `INCOMPLETE` işareti, desteklenmeyen imaj sürümü, eksik dump hem doğrulamada hem `restore-check`'te kaynak oluşturulmadan reddedildi; `../` içeren arşiv üyesi reddedildi.
- [x] **Yeni izole hedefe restore:** internal ağda yeni PG + volume; iki DB parmak izi, satır/version/soft-delete ve migration geçmişi aynı; restore edilen Keycloak DB'siyle yeni Keycloak açıldı, fixture admin ile giriş ve realm kullanıcısı/e-postası doğrulandı; restore edilen MinIO'dan 3 nesnenin SHA256'sı birebir.
- [x] **Temizlik ve canlı koruma:** fixture/restore konteyner, volume, ağ ve geçici dosya kalmadı. BuildingFlow compose konteynerlerinin ad/durum/başlama zamanı öncesi-sonrası aynı. Canlı stack'e karşı `backup` yazıcılar çalıştığı için hiçbir dump/klasör oluşturmadan reddetti (yalnız `docker compose ps`/`inspect`).
- [x] **Bulgular ve düzeltmeler:** `wait_pg` PostgreSQL imajının init-zamanı (yalnız socket) sunucusunu hazır sayıp `createdb` yarışına düşüyordu → TCP `pg_isready -h 127.0.0.1`. Keycloak fixture'ı 8 GB Docker VM'de (diğer projelerin ~5.8 GB kullanımı) OOM ile öldü → `--memory 1g`, `-Xmx512m`. Restore ağı `--internal`, MinIO port yayınlamaz.

**Sınırlar:** Canlı/yerel kullanıcı verisinin yedeği alınmadı ve hiçbir canlı hedefe restore yapılmadı (kullanıcı onayı ve bakım penceresi gerekir). Online/PITR yedek, şifreli uzak saklama ve retention hedef ortamda belirlenir. Workflow adımı GitHub'da çalıştırılmadı.

## İŞ11 adım02 — CI ve otomatik kontroller: TAMAMLANDI — 11.10.2026

**2/4 resmi görev = %50.00; genel iş 81/83 = %97.59.**

- [x] **Yerel CI eşdeğeri:** `bash scripts/ci.sh` (çıkış 0): lisans **331**, CI sonuç kapısı negatif testleri **6/6**, FE **85/85** + typecheck + production build + `npm audit --audit-level=high` **0 açık**, backend **426/426 = 88 unit + 338 integration, hata/atlama 0** (TRX kapısı), EF bekleyen model değişikliği yok, idempotent SQL üretimi ve geçici izole DB'ye **iki kez uygulama (11 migration)**, NuGet `--vulnerable --include-transitive` temiz, `git diff --check`. Önceki çalışmadan kalan sonuçlar kapıyı geçemez (`artifacts/ci` her çalıştırmada sıfırlanır; `artifacts/` Git dışı).
- [x] **Bulgu ve düzeltme:** idempotent betik önceki fazlarda yalnız üretilmiş, hiç uygulanmamıştı. Gerçek PostgreSQL'de `20261007210343_UnifiedPropertyModel` içindeki `;` olmayan ham `Sql()` DO-blokta `42601 syntax error` verdi. Uygulanmış migration değiştirilmeden `TerminatedSqlMigrationsSqlGenerator` DI ve design-time context'e eklendi. Yeni integration testi: yamasız betik 42601, yamalı betik ayrı şemaya iki kez uygulanır, geçmiş = tüm migration'lar, bekleyen yok.
- [x] **Kapı negatifleri:** `scripts/test_ci_gates.py` eksik/başarısız/atlanan/boş/karışık TRX'i reddeder, tamamen başarılıyı kabul eder.
- [x] **İzolasyon ve sızıntı:** testler `buildingflow_tests`, `buildingflow-test-*` RabbitMQ/MinIO ve geçici Mailpit; SQL provası `buildingflow_ci_sql_<pid>` veritabanını oluşturup siler. Çalışma sonrası geçici DB/Mailpit/test kuyruğu kalmadı; CI logunda `.env` sır değerlerinden hiçbiri yok. Korunan 26 dosya hash'i aynı.
- [x] **GitHub workflow:** `contents: read`, SHA ile sabitlenmiş resmi action'lar, Node 24/.NET 10, `init-local.sh` ile tek kullanımlık sırlar, cache yalnız npm lockfile; sır/dump/artifact yüklemesi yok, deploy/merge/push adımı yok.

**Sınırlar:** Workflow GitHub üzerinde çalıştırılmadı (remote/CI hesabı yok); yerel eşdeğer çalıştı. `--install` (npm ci) çalışan FE3003 dev sürecini etkilememek için yerelde çalıştırılmadı. npm/NuGet audit ağ erişimine bağlıdır.

## İŞ11 adım01 — Güvenlik ve erişim: TAMAMLANDI — 11.10.2026

**1/4 resmi görev = %25.00; genel iş 80/83 = %96.39.** Dal `faz-10-11-release` (Faz10 commit'i 8ca000d ayrı). Push/merge yok.

- [x] **Production yapılandırma reddi (15 vaka):** HTTP veya kullanıcı bilgili Authority, HTTP metadata, path/HTTP/`*` CORS origin, boş/`*` AllowedHosts, otomatik forwarded-header güveni; kapatılmış veya 1–10000 dışı rate-limit; `0.0.0.0`, CIDR veya hostname olarak verilen proxy açılışta `InvalidOperationException` verir. Geçerli HTTPS yapılandırması kabul edilir.
- [x] **Forwarded header:** proxy tanımsızken `ForwardedHeaders.None`, boş KnownProxies/KnownIPNetworks. Doğrudan HTTP + sahte `X-Forwarded-Proto: https` ve listede olmayan eşten gelen başlık `400 BF_INVALID_REQUEST`. Yalnız tanımlı proxy (10.0.0.5) şemayı ve istemci IP'sini taşır; aynı IP üçüncü istekte 429, farklı IP ayrı bütçe. Sahte `X-Forwarded-For` anonim limiti aşamaz (401, 401, 429).
- [x] **HTTPS başlıkları ve 429:** HSTS, `nosniff`, `DENY`, `no-referrer`, API `no-store`; anonim limit sonrası `429 BF_RATE_LIMITED` + `Retry-After`; `/health/live` limit dışında. Kimlikli okuma/yazma bütçesi `sub` bazında ayrı: alice 3. okumada 429, bob 200; yazma bütçesi okumadan bağımsız. Production'da OpenAPI 404.
- [x] **Negatif JWT (production):** imzasız (`alg:none`), yabancı anahtar, HTTP issuer ve süresi dolmuş token 401. Geliştirme ortamındaki issuer/audience/expired/signature/malformed ve rol/üyelik negatifleri (sahte `ClaimTypes.Role`, client-role, üyelik dışı 403/404) mevcut integration suite'te bu tur yeniden çalıştı.
- [x] **FE nonce CSP ve cookie:** `src/security/policy.ts` yalnız açık API/OIDC origin'lerini kabul eder, credential/query/`javascript:`/production HTTP ve nonce enjeksiyonunu reddeder; `unsafe-eval` yok. Locale cookie `httpOnly`, `SameSite=Lax`, production'da `Secure` (3 FE güvenlik testi).
- [x] **Production build smoke (geçici 127.0.0.1:3013, sonra durduruldu):** tüm rotalar dinamik; `/login` yanıtında CSP + `private, no-store`, 24/24 script aynı istek nonce'unu taşır, her istekte yeni nonce; istemcinin gönderdiği `x-nonce`/CSP başlığı yok sayılır; `/health` CSP dışında. Tarayıcıda sayfa hydrate oldu, CSP ihlali konsolda yok.
- [x] **Regresyon:** `bash scripts/test.sh` **425/425 = 88 unit + 337 integration, hata/atlama 0** (yeni 28 ReleaseSecurity vakası dahil; ayrı test DB, `buildingflow-test-*` kuyruk/bucket, geçici Mailpit). `npm test` **85/85**, typecheck ve `npm run build` başarılı. Korunan 26 dosya hash'i değişmedi.

**Sınırlar:** Dış domain/TLS sertifikası yok; HTTPS davranışı test sunucusu ve yerel production build ile doğrulandı. `next.config.ts` HSTS başlığı build anındaki `BUILDINGFLOW_PUBLIC_HTTPS` değerini kullanır. Rate limiter süreç içi (tek API örneği) çalışır; çok örnekli dağıtımda proxy/gateway limiti gerekir.

## İŞ11 geliştirme öncesi kabul planı — 11.10.2026

[Analiz](analysis/PHASE_11_RELEASE_QUALITY.md). Production güvenlik/forwarded header/HTTPS/CORS/rate-limit ve negatifJWT; FE nonce CSP/cookie/TR-EN; izole gerçek altyapıyla backend regressyon; lokal CI eşdeğeri, npm/NuGet audit/lisans; yedek fixture tam geri dönüş/hatalı checksum/aktif writers ret/yalnız yeni target koruması; migration ve son kullanıcı rehberi. Henüz başarılı kabul sonucu değildir.

## İŞ10 — adım04 ve nihai teslim — 11.10.2026

**TAMAMLANDI: 4/4 resmi görev = %100.00; genel iş 79/83 = %95.18.** [Analiz](analysis/PHASE_10_REPORTS_METERS_OBSERVABILITY.md), [API](api/meters-reports.md), [gözlemlenebilirlik](OBSERVABILITY.md). Önceki Faz09 hızlı kontrol bu tur41/41; önceki tam374 kanıtı yeniden çalıştırılmış sayılmaz.

- [x] **Son backend regresyonu: 397/397 =88 unit +309 integration, hata/atlama0.** Sayaç API/migration, report scope/finans/tüketim ve telemetri tests dahil. Gerçek ayrı PostgreSQL, rastgele buildingflow-test-* RabbitMQ/MinIO ve throwaway Mailpit; yeni migration ilk kurulum/yükseltme/tekrar/immutable revizyon/Down veri koruması ve bütün22 uygulama tablosunda sekiz NOT NULL ortak alan; model-snapshot eşliği geçti. `dotnet ef migrations script --idempotent --no-build` yeni sayaç migration'ını içeren SQL üretti; SQL artifact'i ayrıca ana DB'ye uygulanmadı.
- [x] **Son FE regresyonu: 82/82; hata/atlama0**, typecheck ve production build başarılı. DTO/scope/decimal/gerçek takvim/TR-EN/OIDC rota ve menü, merkezi error registry eşliği; ortak RecordList/ManagementPanel. 409 düzeltme çatışması kaydetmeyi kilitler ve güncel okumayı yeniden yükler.
- [x] **Canlı altyapı: 11/11.** API401 request counter, request histogram, worker outbox counter, PostgreSQL/Redis/RabbitMQ başarılı TCP probe, bounded/hassas veri içermeyen labels, Grafana iki datasource health ve8 panel, API/worker Loki logları, API trace_id ve hassas sorgu işaretinin dışlanması. `python3 scripts/test-observability.py` çalışan yerel servisleri salt-okunur kontrol eder; token/parola/log body yazdırmaz, finans mutation veya gerçek alıcıya mesaj göndermez. TCP testi kimlik bilgisiyle DB sorgusu değildir; diğer gerçek entegrasyonlar ayrı suite'tedir.
- [x] **Tarayıcı:** girişli Chrome'da Sayaçlar ve Raporlar, TR/EN, boş sayaç listesi, gerçek bölüm seçenekleri, masaüstü sağ512px form,390×844 mobil tam genişlik/taşmasız panel ve rapor kabulü. Kayıt kaydedilmedi; dil/viewport geri yüklendi ve yalnız geçici test sekmesi kapatıldı. [Sayaç paneli kanıtı](screenshots/phase10-meter-panel-tr.png).
- [x] **Koruma/lisans:**331 npm/NuGet sürümü PASS; resmi OpenTelemetry Apache-2.0 LICENSE/NOTICE ve Collector/Prometheus/blackbox Apache-2.0, Loki AGPL-3.0 dağıtım yükümlülükleri kaydedildi. .env,FE manifest/lock ve önceki migration/Designer kaynakları23 SHA256 aynı;149 yerel doküman bağlantısı,79/83 yol haritası sayacı ve git diff --check geçti. Son roadmap/TR-EN kanıt metninden sonra82 FE,typecheck/build tekrar başarılı. Ana DB Faz09+10 upgrade öncesi/sonrası17 eski tablo satır/audit hash eşit; yeni sayaç/rapor/finans iş kaydı oluşturulmadı.

Komutlar: backend `dotnet test BuildingFlow.sln --no-restore --verbosity minimal -m:1 --disable-build-servers` (SDK /tmp/buildingflow-dotnet ve özel CLI_HOME/NUGET_PACKAGES); FE `npm test`, `npm run typecheck`, `npm run build`; kök `python3 scripts/check-licenses.py`, yeni telemetry compose servisleri ve `scripts/test-observability.py`; EF idempotent SQL. Gereken restore/VSTest/SWC/Docker ağ-soket izinleriyle çalıştı; engellenen ilk denemeler başarılı test sayılmadı. Yerel API5005/FE3003 ve SMTP kapalı test workerı; Grafana3002/Prometheus19090/Loki13100/OTLP14318. Diğer projenin3100 portu korunur. Tek agent; skill/alt agent yok.

**Sınırlar:** İlk endeks tüketim bilinmez; sayaç sıfırlama/rollover, katsayı/tarife/otomatik borç yok. Doluluk ilişki kaydının ay sonu yorumudur; fiziksel boşluk kanıtı değildir. Tahakkuk bakiye ve bölüm etkinliği sorgu anına aittir; geçmiş bakiye snapshot yok. Tempo trace deposu/tam broker trace propagation yok; isteğe bağlı trace debug export altyapısı mevcut. API/Worker/Web Docker imajları bu tur derlenmedi; dış SMTP ve İŞ11 üretim hazırlığı yapılmadı. Yerel iş verisinde başarılı mutation kabulü yapıldı sayılmaz; iş akışları gerçek ayrı test ortamlarında doğrulandı.

## İŞ10 adım03 kanıtı — 11.10.2026

**397/397 .NET (88 unit+309 integration), hata/atlama0.** Gerçek yeni Collector0.162.0, Prometheus3.15.0, blackbox0.28.0, Loki3.7.8 ve mevcut Grafana12.4.11 başlatıldı. `scripts/test-observability.py`:API401 sayacı, süre histogramı, worker outbox ölçümü, TCP probe serileri, bounded/hassas veri içermeyen etiketler, Grafana iki datasource health ve8 panelli dashboard geçti. Restore/lisans331 başarılı; NuGet sandbox DNS nedeniyle yerel yetkili tekrar kullanıldı. TraceFlow3100 çakışması farklı varsayılan13100/19090/14318 ile çözüldü; .env değişmedi. Worker dev komutuna eksik DB bağlantısı eklendi. Yerel ana DB'ye Faz09+10 yeni migrations uygulandı; önce/sonra17 eski tablo hash'i eşit. FE3003 korunur, API5005 yeniden başlatıldı. Kullanıcı DB'sinde yeni sayaç/rapor/finans mutation yok.

## İŞ10 adım02 kanıtı — 11.10.2026

**8/8 rapor filtreli entegrasyon,82/82 FE; hata/atlama0.** Typecheck/build başarılı. Aynı PostgreSQL snapshot'ından alan/üyelik kontrollü rapor; ortak malik doluluk sayılmaz, kiracı+sakin aynı bölüm bir kez; pasifler doluluk dışında. Dönem tahakkuku/güncel bakiye ile farklı ay tahsilatı ayrılır; iptal gider hariç, önceki ay baseline tüketimde kullanılır. ACCOUNTANT yanıtında kişi adı/id yok; anonim/rol/üyelik/silinmiş alan ve geçersiz dönem reddedilir. Ham toplamlar doğrulanır.

## İŞ10 adım01 kanıtı — 11.10.2026

**36/36 PostgreSQL/persistence/migration (sayaç+ilgili migration filtresi),80/80 FE; hata/atlama0.** Typecheck ve Next.js production build geçti. Yeni migration20261011064227_MetersAndReadings; tüm uygulama tablolarının sekiz NOT NULL alanı, compositeFK, endeks iki komşu, baseline, düzeltme/replay/current tarihçe, rol/üyelik/JWT, immutable revizyon ve Down veri guard gerçek ayrı PostgreSQL'de kontrol edildi. Bir eski MessagingMigration testi son migration yerine korunan Faz09 sınırını bekleyecek şekilde düzeltildi; migration kaynakları değişmedi. Yerel ana DB henüz yükseltilmedi, tarayıcı kabulü teslim kontrolünde yapılacak.

## İŞ10 planlı kabul — 11.10.2026

Yetki/scope/anonim/malformed JWT; sayaç iki komşu/endeks/tarih/version/UUID tekrar ve düzeltme revizyonu; üç yeni BaseEntity tablosu ve gerçek PostgreSQL migration upgrade/tekrar/Down guard; rapor kapsamı/tarih/para/doluluk/tüketim ve ACCOUNTANT kişisel veri yasağı; FE DTO/TR-EN/menü/rota/panel; Prometheus/Grafana/Loki/OTLP gerçek pipeline + hassas telemetri yokluğu. [Kapsam](analysis/PHASE_10_REPORTS_METERS_OBSERVABILITY.md). Henüz başarılı test kanıtı değildir.

## Faz09 bu tur hızlı tekrar kontrolü

11.10.2026: `FullyQualifiedName~Messaging` filtresiyle **41/41 (20 unit +21 integration), hata/atlama0**. Ayrı PostgreSQL, yalnız buildingflow-test-* RabbitMQ ve geçici Mailpit; kullanıcı verisine mail/SMS gönderilmedi. Önceki374 tüm-suite kanıtı önceki turun sonucudur.

# Test planı ve tamamlanma kaydı

## 09 — Mesajlaşma: adım04 sonucu ve faz teslimi — 11.10.2026

**İŞ 09 TAMAMLANDI: 4/4 resmi görev = %100.00; iş yol haritası 75/83 = %90.36.** SMS sağlayıcı kullanıcı kararıyla kapalıdır; tamamlanan görev SMTP e-posta kapsamıdır.

- [x] **SMTP handler (System.Net.Mail, yeni paket yok):** UTF-8 düz metin, `Message-ID: <messageId@buildingflow>`, `X-BuildingFlow-Message-Id/Correlation-Id`, Content-Language. Geçici, ayrı Mailpit 1.27.10 (rastgele loopback port, yalnız `@example.test` kabul): Türkçe karakterli konu/gövde, alıcı/gönderen ve izleme başlıkları API'den doğrulandı. 5xx alıcı reddi `PermanentMessageFailureException` (Mailpit'te mesaj yok), erişilemeyen sunucu geçici hata; hata metni alıcı/iç ayrıntı taşımaz.
- [x] **Dış sırlar ve güvenli yapılandırma:** `Smtp:*` yalnız ortam/secret'tan; şifresiz SMTP yalnız localhost/mailpit + açık `AllowInsecureLocal` ve kimlik bilgisi olmadan, aksi STARTTLS zorunlu. Eksik/yarım kimlik bilgisi, geçersiz/görünen adlı From, header injection'lı FromName, port/host hataları reddedilir; hata mesajı sır içermez. Compose worker yerelde Mailpit'e yönlendirildi; üretim değeri depoya yazılmadı.
- [x] **Uçtan uca (PG + RabbitMQ + Mailpit):** outbox → relay → consumer → SMTP tek mesaj; tekrar yayında ikinci mail yok (Mailpit sayaçları: 2 kabul, 2 red), reddedilen alıcı 1 denemede `permanent-failure` nedeniyle DLQ'da, inbox kaydı yok.
- [x] **Regresyon:** `dotnet test` **374/374 = 88 unit + 286 integration, 0 atlanan/başarısız**; FE **76/76**, typecheck; lisans 318 PASS; 21 korunan dosya aynı; `docker compose config` geçerli. Worker SMTP etkin + test prefix ile başlatıldı, email consumer'ı açıldı; smoke/test kuyrukları temizlendi, geçici Mailpit durduruldu, geliştirici Mailpit'ine dokunulmadı. `scripts/test.sh` aynı geçici Mailpit'i otomatik açıp kapatır.

**Faz sınırları:** gerçek alıcıya veya dış sağlayıcıya mail/SMS gönderilmedi; üretim SMTP sağlayıcı/kimlik bilgisi ve SPF/DKIM/DMARC kullanıcıya bağlıdır. System.Net.Mail implicit TLS (465) desteklemez; 587 STARTTLS gerekir. SMS ve uygulama içi bildirim teslim kanalı yoktur (kuyrukları kalıcı, tüketicisiz). Henüz hiçbir iş/API akışı e-posta üretmez; hangi olayların (ör. duyuru yayını, borç hatırlatma) mail tetikleyeceği yeni kapsam kararıdır. DLQ yeniden oynatma aracı yok. Migration yerel ana DB'ye uygulanmadı; Docker imajları derlenmedi (compose ile yeniden derlemede migration servisi ana DB'ye uygular). Sonraki faz için kullanıcı onayı beklenir.

## 09 — Mesajlaşma: adım03 sonucu — 11.10.2026

**Adım03 TAMAMLANDI: 3/4 = %75.00; iş yol haritası 74/83 = %89.16.**

- [x] **Gecikmeli retry ve limit (gerçek RabbitMQ + PG, yalnız `buildingflow-test-*`):** geçici handler hatasında mesaj gecikmeye özel TTL'li quorum retry kuyruğuna confirm ile kopyalanır, ardından kaynak ACK edilir; TTL sonunda orijinal routing key ile ana kuyruğa döner. Test: 2 hata → 300+600 ms sonra 3. denemede tek işlem/tek inbox, tüm kuyruklar boş. Deneme limiti (test 3, varsayılan 5; gecikmeler varsayılan 5s/30s/2dk/10dk) aşılınca `.failed` kuyruğuna `x-buildingflow-failure=retry-exhausted`, deneme sayısı, aynı MessageId/body/persistent ile gider; inbox kaydı oluşmaz.
- [x] **Kalıcı hata ve idempotency:** aynı ID farklı içerik retry'a girmeden `rejected` nedeniyle DLQ'ya gider, handler tekrar çalışmaz; başarılı mesajın iki ek tekrar teslimi ACK edilir, handler 1 kez/inbox 1 satır. Sahte `-50` deneme başlığı retry süresini uzatamaz. Retry/DLQ yayını confirm alamazsa kaynak `requeue` ile geri bırakılır, ACK edilmez; ana kuyruklarda `x-delivery-limit=10` ve at-least-once dead-letter güvenlik ağıdır.
- [x] **Politika birimleri (9):** başlık ayrıştırma (int/long/ASCII, negatif/bozuk → 0, üst sınır 100), artan gecikme/limit, güvensiz retry yapılandırmasının reddi.
- [x] **Regresyon:** `dotnet test` **369/369 = 88 unit + 281 integration, 0 atlanan/başarısız**; mesajlaşma paketi ardışık 3 çalıştırmada 30/30; broker'da test kuyruğu/exchange kalmadı; lisans 318 PASS; 21 korunan dosya aynı.

**Sınırlar:** SMTP gibi dış sağlayıcıda tam exactly-once yoktur: handler gönderip inbox commit'inden önce çökme/bağlantı kaybı olursa mesaj tekrar gönderilebilir; inbox yalnız commit edilmiş teslimleri tekilleştirir. DLQ mesajlarını yeniden oynatma aracı/ekranı yoktur. Kuyruk argümanları değiştiğinden önceden farklı argümanla oluşturulmuş üretim kuyruğu varsa yeniden bildirim PRECONDITION_FAILED verir; yerel broker'da üretim kuyruğu yoktu.

## 09 — Mesajlaşma: adım02 sonucu — 11.10.2026

**Adım02 TAMAMLANDI: 2/4 = %50.00; iş yol haritası 73/83 = %87.95.**

- [x] **BaseEntity Outbox/Inbox + migration `20261011043002_MessagingOutboxAndInbox`:** `outbox_messages` (tekil message_id, tür allowlist/payload ≤64KiB/durum check, Property FK, bekleyen kısmi index) ve `processed_messages` (tekil consumer+message_id, SHA-256 parmak izi, PROCESSED/SKIPPED). Ayrı şemada AnnouncementsAndDocuments'tan yükseltme, eski satır JSON eşliği, tekrar uygulama, sekiz NOT NULL alan, check ihlalleri; mesaj geçmişi varken Down PostgresException ile reddedilir. Model sözleşme testleri yeni tabloları kapsar; `db.sh check` farksız, idempotent SQL üretildi.
- [x] **Atomik enqueue + relay (gerçek PG + RabbitMQ, yalnız `buildingflow-test-*`):** iş kaydıyla aynı transaction rollback'inde ne Property ne outbox kalır; commit'te ikisi birlikte. Geçersiz komut eklenmez. Relay yayınlar/PUBLISHED işaretler, ikinci tur 0; 3 eşzamanlı relay `FOR UPDATE SKIP LOCKED` ile 45 mesajı tam bir kez yayınlar. Broker yokken mesaj PENDING, `PUBLISH_FAILED`, artan backoff; ilk hatada batch durur, broker dönünce yayın.
- [x] **Tüketici/inbox:** ilk teslim Processed, aynı içerik Duplicate (handler 1 kez), aynı ID farklı içerik/bozuk JSON/yanlış kuyruk/bilinmeyen Property Rejected; handler hatası inbox bırakmaz, tekrar başarılı; 5 eşzamanlı aynı teslimde handler 1 kez; SKIPPED kaydı. Gerçek consumer ack sonrası kuyruk boş, bozuk mesaj `.failed` dead-letter kuyruğuna gider.
- [x] **Regresyon:** `dotnet test` **355/355 = 88 unit + 267 integration, 0 atlanan/başarısız**; lisans 318 PASS; 21 korunan dosya aynı. Worker test DB'sine karşı 10 sn başlatıldı: relay döngüsü çalıştı, etkin kanal olmadığı için consumer başlatılmadı.

**Bulgu ve düzeltmeler:** İki RabbitMQ test sınıfı paralel çalışınca yerel broker'da quorum Ra küme oluşturma zaman aşımı görüldü (6 başarısız deneme, başarılı sayılmadı); sınıflar paralel olmayan tek koleksiyona alındı, ardışık 3 çalıştırma 16/16. Başarısız fixture'ların kalan 5 test kuyruğu/4 exchange'i yalnız `buildingflow-test-*` adıyla silindi; temizlik artık yeni bağlantıyla yapılır. Communication migration testi, boş messaging tablolarının önce geri alındığı yeni sırayı kesin olarak doğrular. **Sınırlar:** henüz hiçbir iş/API akışı enqueue etmez; üretim worker'ında etkin kanal yok (e-posta adım04, SMS kapalı, uygulama içi bildirim deposu kapsam dışı). Teslim en-az-bir-kez; retry/DLQ politikası adım03. Migration yerel ana DB'ye uygulanmadı; Docker imajları derlenmedi.

## 09 — Mesajlaşma: adım01 sonucu — 11.10.2026

**Adım01 TAMAMLANDI: 1/4 = %25.00; iş yol haritası 72/83 = %86.75.** Adım02–04 bu kanıtla tiklenmez.

- [x] **Sürümlü sözleşmeler (unit 20/20):** EmailRequested/SmsRequested/NotificationRequested roundtrip, sabit tür allowlist'i, schemaVersion1, aynı nesne retry'da aynı MessageId/byte. Boş UUID, UTC dışı/boş zaman, desteklenmeyen locale, uzunluk sınırları, e-posta header injection/display-name/boşluk, E.164 dışı SMS, zarf/payload kimlik uyuşmazlığı, bilinmeyen şema/tür, `$type`/fazla alan, 64KiB üstü, bozuk/null JSON reddedilir; hata mesajı alıcı/içerik taşımaz.
- [x] **Gerçek RabbitMQ 4.1.4 (integration 6/6):** yalnız rastgele `buildingflow-test-<uuid>` exchange/kuyrukları; durable quorum kuyruk + persistent mesaj, türe göre yönlendirme, MessageId/CorrelationId/ContentType, publisher confirms, mandatory-unroutable `MessagePublicationException` ve ardından yeni kanalla toparlanma, 20 eşzamanlı yayında kayıp/karışma yok, geçersiz/iptal edilen çağrı yayın yapmaz, yeniden oluşturulan yayıncıda aynı kimlikli tekrar (tekilleştirme adım02/03 inbox'ındadır), erişilemeyen broker kimlik bilgisi sızdırmayan güvenli hata. Test sonrası `rabbitmqctl list_queues` boş; mevcut kuyruk silinmedi.
- [x] **Regresyon:** `dotnet test backend/BuildingFlow.sln --no-build -m:1 --disable-build-servers` ayrı `buildingflow_tests` PostgreSQL ve test MinIO/RabbitMQ ile **345/345 = 88 unit + 257 integration, 0 atlanan/başarısız**. FE `npm test` **76/76**, `npm run typecheck` geçti. `python3 scripts/check-licenses.py` **318 paket** PASS (RabbitMQ.Client 7.2.1 Apache-2.0 seçimi, System.Threading.RateLimiting 8.0.0 MIT). 21 korunan dosya hash'i aynı.

**Ortam notu:** Sandbox içinden ilk RabbitMQ denemesi AMQP el sıkışmasında zaman aşımına uğradı (6 başarısız); bu başarılı sayılmadı, aynı komut yetkili yerel çalıştırmada geçti. **Gerçek sınırlar:** iş/API akışına yayın bağlanmadı (Outbox adım02); tüketici, retry/DLQ ve SMTP henüz yok; gerçek alıcıya mesaj gönderilmedi. Docker imajları yeniden derlenmedi.

## 09 — Mesajlaşma: geliştirme öncesi kabul planı — 10.10.2026

Kullanıcı fazı başlatmayı onayladı; SMTP e-posta, SMS daha sonra. Dört resmi görev ve sınırlar [analizde](analysis/PHASE_09_MESSAGING.md). Henüz test sonucu değildir.

- Adım01: üç sürümlü mesaj/UUID/property/correlation/UTC/type registry; strict bounded JSON ve güvenli alıcı/locale kontrolü. Gerçek RabbitMQ'da yalnız geçici namespace: routing, durable/persistent, confirms, mandatory-return, concurrent publish, iptal ve boş/bozuk sözleşme.
- Adım02/03: DB+Outbox atomikliği, gerçek PG sekiz alan/migration, consumer inbox/idempotency, retry ve DLQ arıza/tekrar teslim; yalnız uygulanınca doğrulanır.
- Adım04: Mailpit yerel SMTP, sırlar dış ayarda; gerçek alıcıya mail/SMS gönderilmez. SMS sağlayıcı kapalı ve ayrı bekleyen kapsamdır.
- Restore sonrası lisans, .env/manifest/eski migration hashleri, önceki test regresyonu; yalnız doğrulanmış görev roadmap'te tiklenir.


## 08 — Duyurular ve dosya yönetimi — 10.10.2026

**TAMAMLANDI: 4/4 resmi görev = %100.00; iş yol haritası 71/83 = %85.54.** [Analiz](analysis/PHASE_08_ANNOUNCEMENTS_DOCUMENTS.md), [API](api/communication.md). Önceki İŞ 07/07.1 teslimleri korunur; İŞ 09 ve ayrı UI aşamaları başlatılmadı. Test alt senaryoları dört resmi görevin paydasını değiştirmez.

- [x] **Duyuru modeli ve ekranları:** Announcement başlık/düz metin/hedef, DRAFT→PUBLISHED→ARCHIVED; aktif üyelik ve yönetici yazımı. Diğer üyeler yalnız PUBLISHED+ALL_MEMBERS görür; taslak/yönetim/arşiv kapsamı açılmaz. Yayın içerik ve eklerini değişmez kılar. Duyurular ve Belgeler ayrı menü/alan seçimi, ortak sağ ManagementPanel ve shadcn/TanStack RecordList ile TR/EN çalışır.
- [x] **Özel MinIO sağlayıcısı:** Minio 7.0.0 SDK, sunucu UUID nesne anahtarı; DB yalnız StoredDocument metadata/audit/version tutar. Yetkili JWT indirmesi attachment/no-store/nosniff ve boyut/SHA256 doğrulamalıdır. Bucket/anahtar/kimlik bilgisi/public URL DTO'ya taşınmaz. PDF/JPEG/PNG imza+uzantı+MIME, 10 MiB sınırı; anonim MinIO erişimi403 ve beklenmeyen bucket politikası503 gerçek özel geçici bucket ile doğrulandı.
- [x] **Güvenli dosya ve ek akışı:** aynı alan composite FK, taslak başına en çok10 ek, üst duyurunun görünürlüğünü devralma; Property kilidi/version ile yayın/yükleme yarışında kural korunur. UUID aynı içerikte replay200, farklı veya silinmiş istekte409; metadata soft-delete, özel nesne geçmişi korunur. Silinmiş/kapsam dışı indirme404; bozulmuş nesne güvenli503, ham SDK ayrıntısı yok. Belirsiz DB commit sonucunda yeni nesne otomatik silinmez; bu özel hata yolunun canlı arıza enjeksiyonu yapılmadı.
- [x] **Backend: 319/319 geçti; 68 unit +251 integration, başarısız/atlanan0.** Yeni fazın 9 unit +18 integration testi dahil. PostgreSQL isteyen testler ayrı buildingflow_tests DB/izole şema; depolama testleri yalnız fixture'ın buildingflow-test-* özel bucket'ında çalışır ve kendi verisini temizler. Anonim, sahte/yetersiz rol/JWT, ACCOUNTANT/RESIDENT görünürlüğü, üyelik, foreign property, MIME/boyut, replay, ek sınırı ve yayın yarışları kapsamlı regresyonda geçti. Bütün integration testleri DB testi olarak gösterilmez.
- [x] **Migration:** yeni 20261010043250_AnnouncementsAndDocuments/Designer/snapshot; iki tabloda sekiz BaseEntity NOT NULL alan. Ortak şema testleri bütün17 uygulama tablosunu taradı. Ayrı PostgreSQL ilk kurulum, önceki veri/audit korunarak yükseltme, tekrar uygulama, boş Down/re-Up ve duyuru/belge geçmişinde Down reddi geçti; model/migration eşliği korundu. İdempotent SQL üretildi. Yerel migration uygulandı; tekrar update "already up to date" sonucu ve15 eski tablonun satır/audit hash eşliğiyle doğrulandı. İlk canlı yükseltme öncesi snapshot Docker izniyle alınamadığından ilk uygulama için canlı before/after hash kanıtı sunulmaz; veri koruma yükseltme kanıtı ayrı test DB'dedir. ExpenseMigrationTests geçmiş faz hedefini açık seçmek üzere düzeltildi; uygulanmış migration kaynakları değiştirilmedi.
- [x] **Frontend: 76/76 geçti, başarısız/atlanan0; typecheck ve son yol haritasını içeren production build başarılı.** Yeni9 test duyuru/dosya DTO scope/state/şekil, güvenli dosya metadata'sı, gerçek ortak multipart/Blob transport, Authorization ve boundary başlığı, Abort, kodlu404/413/503 ve OIDC dönüş yollarını kapsar. Tüm113 BE/FE kodu, HTTP/severity ve TR/EN/ICU eşliği korunur; ham hata yerine ortak sağ üst bildirim kullanılır.
- [x] **Gerçek tarayıcı kabulü:** kullanıcı verisini kaydetmeden menüler, seçili alanın boş duyuru/belge listesi, TR masaüstü sağ512px panel ve boş form hata/ilk odak; EN390×844 belge paneli width390/scroll388 ve sayfa yatay taşma yok. Türkçe geri seçildi, viewport sıfırlandı ve yalnız geçici kabul sekmesi kapatıldı. Başarılı duyuru yayınlama/yükleme/silme kullanıcı DB'sinde yapılmadı; dolu liste/ek/yayın/indirme iş kuralları ayrı PG+MinIO ve FE sözleşme testlerinde doğrulandı. Native dosya seçici metni işletim sistemi/tarayıcı diline bağlıdır.
- [x] **Kalite ve koruma:** 316 npm/NuGet paket sürümü lisans metadata PASS; yeni Minio SDK Apache-2.0 lisansı resmi kaynaktan kaydedildi ve API/Worker imajlarına dahil edildi. MinIO server'ın mevcut AGPL yükümlülükleri [licenses.md](licenses.md) içinde korunur. .env, FE manifest/lockfile ve önceki migration/Designer kaynaklarını içeren19 dosyanın SHA256 özeti aynı. Yerel API5005 Healthy ve FE3003 API healthy. Tek agent; yeni skill/alt agent yok.

Komutlar: `bash scripts/test.sh` ayrı PostgreSQL ve MinIO test ortamını hazırlar; bu tur aynı test bağlantılarını mevcut .env'den belleğe yükleyen yardımcıyla `dotnet test backend/BuildingFlow.sln --no-restore --verbosity minimal -m:1 --disable-build-servers` çalıştı. SDK /tmp/buildingflow-dotnet; sır/ham istek çıktılarına yazılmadı. FE klasöründe `npm test`, `npm run typecheck`, `npm run build`; kökte `python3 scripts/check-licenses.py`, `bash scripts/db.sh update`; backend altında EF `migrations script --idempotent --no-build`. Sandbox NuGet ağ/VSTest soketi/SWC cache kısıtları gerekli yerel izinle tekrarlandı; engellenen denemeler başarılı test sayılmaz. İlk IPv6 localhost MinIO bağlantısı farklı servise gidiyordu; yerel endpoint127.0.0.1:9000 yapıldı, diğer uygulama değiştirilmedi.

Ekranlar: [TR duyurular](screenshots/phase08-announcements-tr.png), [TR duyuru formu](screenshots/phase08-announcement-form-tr.png), [TR belge formu](screenshots/phase08-document-form-tr.png), [EN mobil belge paneli](screenshots/phase08-document-mobile-en.png). Son teslim kontrolünde **182 yerel belge bağlantısı eksiksiz**, roadmap14 faz/71 tamamlanan/83 görev sayacı tutarlı,19 korunan dosya değişmez ve `git diff --check` başarılı.

**Gerçek sınırlar:** yalnız uygulama içi duyuru yayını vardır; mail/SMS/bildirim gönderimi İŞ09'dur. Finans kaydına belge iliştirme, virüs taraması, saklama/orphan temizliği, public/presigned paylaşım bu fazda yoktur. İmza kontrolü dosyanın zararsız olduğunu kanıtlamaz. Dosya silme metadata soft-delete'dir, özel nesne tutulur. Docker imajları ve dış Keycloak smoke bu tur yeniden çalıştırılmadı; build/yerel çalıştırma ve JWT negatif suite kanıtları ayrı tutulur. Default health DB'yi kontrol eder; MinIO erişimi dosya sağlayıcısının503 sözleşmesiyle denetlenir. Sonraki faz için kullanıcı onayı beklenir.

## 07 — Gider ve bütçe takibi — 10.10.2026

**TAMAMLANDI: 4/4 resmi görev = %100.00.** Onaylı kapsam [analiz](analysis/PHASE_07_EXPENSES_BUDGET.md), [API](api/expenses.md). İŞ 07.1 korunur; ayrı UI 07 ve İŞ 08 başlatılmadı. Test alt grupları resmi dört görevin paydasını değiştirmez.

- [x] **Model/kategoriler:** Expense, PropertyBudget, BudgetRevision; sekiz sabit kategori, TRY/iki ondalık/tarih/açıklama/referans; değişmez gider ve gerekçeli tek iptal, sıfır dahil bütçe ve immutable revizyon. Her tablo ortak BaseEntity mapping ile sekiz NOT NULL alan taşır.
- [x] **Backend regresyonu: 292/292 başarılı, hata/atlama 0; 59 unit + 233 integration.** Yeni 12 domain ve 28 integration senaryosu dahil. PostgreSQL gerektiren testler gerçek ayrı buildingflow_tests DB/izole şemalarda; mevcut auth/metadata izolasyon testleri de suite içindedir, 233 testin tümü DB testi olarak gösterilmez. Dört finans rolü/trusted actor audit/parent version, ACCOUNTANT Person403; anonim/bozuk JWT/sahte generic rol/client-only rol/RESIDENT, üyelik ve property/record scope; pasif/silinmiş alan/history read ve finans geçmişinde silme reddi doğrulandı.
- [x] **Atomiklik ve hesaplar:** normalize UUID/fingerprint replay, aynı UUID farklı içerik409, eşzamanlı gider kayıt/tek iptal, ilk bütçe/revizyon version yarışları; eski bütçe replay güncel planı geri almaz, appliedRevision eski sırayı gösterir. Gerçek Payment.paidDate aylık tahsilatı, terslenen tahsilat ve iptal gider hariç; önceki ay sınırı, kuruş tutarları, net/fark/kategori eşliği, plansız ile sıfır plan ayrımı. Liste literal %/_ arama ve filtre özeti sayfa dışı kayıtları kapsar. Bütçe/Gider yazımları Property kilidiyle atomiktir.
- [x] **Migration ve ana veri koruması:** yeni ExpensesAndBudgets/Designer/snapshot; ilk kurulum/yükseltme/tekrar, önceki satır/audit JSON eşliği, boş Down/re-Up; iptal gider veya sıfır bütçe/revizyon varken ilk drop öncesi Down reddi. Tüm uygulama tablolarının sekiz alanı/model eşliği, composite FK/check/index kontrolleri geçti. `has-pending-model-changes --no-build` başarılı, idempotent SQL artifact üretildi. Yerel migration uygulandı; öncesi/sonrası **12 eski tablonun bütün satır/audit hash'leri aynı**. Eski migration kaynakları değişmedi. Tarihsel DuesPartyMigrationTests kendi hedefini açık seçmek üzere düzeltildi; tüm zincirin geçmiş guard testi yeni ExpenseMigrationTests içindedir.
- [x] **Frontend 67/67 başarılı, hata/atlama 0:** 8 yeni test exact cents/zero plan/negatif fark/büyük tutar formatter, gider DTO gerçek tarih/scope/iptal, liste tüm filtre özeti, bütçe current/revision/replay, aylık kategori/toplam/scope, menü/OIDC dönüş, yeni kodlu alan/uyarı çevirisini doğrular. Bütün **105 BE/FE kodu** ve TR/EN sözlük/ICU eşliği mevcut testlerle korunur. Typecheck ve production build başarılı; son roadmap güncellemesini içeren build ayrıca teslimde başarıyla doğrulandı. Gider ve bütçe ortak sağ ManagementPanel, RecordList/TanStack desktop/mobil kart ve yetkili API katmanını kullanır.
- [x] **Gerçek tarayıcı, kaydetmeden:** Gider ve Bütçe menüsü → alan seçimi → seçili alanın doğru rotası; gerçek boş gider/sıfır tahsilat/sekiz plansız kategori. TR 1280×900 panel left768/right1280/width512; boş gider formunda category odağı, 3 aria-invalid alan ve tek sağ üst hata. Tarih 2026-10-01/tutar123,45 diğer alan değişiminde korundu; Vazgeç açan Yeni gider düğmesine odağı döndürdü. Bütçe boş formda tutar odağı; 0,00 yalnız eksik açıklama hatasıyla kabul edilir, plan kaydedilmedi. EN 390×844 mobil 8 kategori kartı, sayfa clientWidth=scrollWidth (scrollbar varken375), panel width390/client388/scroll388; sağ üst bildirim top16/right374/width358. Bildirimi kapatma bütçe panelini kapatmadı. TR geri seçildi, viewport override kaldırıldı. Dolmuş OIDC isteği uygulamadan taze girişle yenilendi; mevcut parola sıfırlanmadı/loglanmadı.
- [x] **Kalite/lisans/koruma:** 312 npm/NuGet sürümü lisans metadata PASS; yeni dependency/asset yok. .env/manifest/lockfile ve tüm önceki migration kaynak/Designer'ları içeren **18 korunan dosyanın SHA-256 özeti aynı**; snapshot yalnız yeni migration için değişti. Yerel Markdown bağlantıları ve git diff --check geçti. API sağlığı gerçek yerel PostgreSQL ile Healthy (200). Tek agent; yeni skill/alt agent yok.

Komutlar: frontend klasöründe `npm run typecheck`, `npm test`, `npm run build`; kökte `python3 scripts/check-licenses.py`; .env'den ayrı test DB bağlantısıyla `dotnet test backend/BuildingFlow.sln --no-restore --verbosity minimal -m:1`. Yerel SDK: /tmp/buildingflow-dotnet; testler .env'yi değiştirmeden ayrı DB bağlantısı alır. EF: `bash scripts/db.sh update`; `dotnet ef migrations has-pending-model-changes --no-build --project src/BuildingFlow.Infrastructure` ve `migrations script --idempotent --no-build`. İlk sandbox VSTest soketi/curl/SWC cache kısıtı gerekli yerel izinle giderildi. İlk suite'deki eski migration hedef varsayımı düzeltildikten sonra tam suite geçti; başarısız ilk deneme teslim sonucu olarak gösterilmez.

Ekranlar: [masaüstü bütçe tablosu](screenshots/phase07-budget-desktop-tr.png), [TR gider formu](screenshots/phase07-expense-form-tr.png), [EN mobil gider](screenshots/phase07-expense-mobile-en.png), [EN mobil bütçe/hata](screenshots/phase07-budget-mobile-en.png), [tamamlanan dört görev](screenshots/phase07-roadmap-tr.png). Fotoğraf/mock veri üretilmedi; gerçek React ekranları ve kullanıcı alanının salt-okunur mevcut verisi gösterilir.

**Gerçek sınırlar:** ana kullanıcı DB'sinde başarılı gider/bütçe oluşturma, revizyon veya iptal tarayıcıdan yapılmadı; dolu liste/detay/iptal/revizyon ve409/retry iş davranışı izole PostgreSQL API/domain ve FE DTO testleriyle doğrulandı. Tarayıcıda mevcut plan olmadığından dolu bütçe formunun dil değişiminde GET tekrarını önleyen davranışı canlı kabul olarak sunulmaz. Genel 8 saniye/hover-focus/dedup önceki ortak testlerle regresyonda geçti; bu fazda yeni timer uygulanmadı. Kategori CRUD, dosya yükleme, gider dağıtımı/tedarikçi, banka mutabakatı/transfer ve fiskal belge/ödeme emri yoktur. Docker imajları ve Keycloak dış sağlayıcı smoke bu tur yeniden çalıştırılmadı; JWT/yetki suite içinde doğrulandı. İş 08 yeni onay gerektirir.

## 07.1 — Merkezi API hata kodları, TR/EN ve sağ üst bildirimler — 09–10.10.2026

**TAMAMLANDI: 5/5 = %100.** Geliştirmeden önce belirlenen beş test grubu ve [analiz](analysis/PHASE_07_1_API_FEEDBACK.md) tamamlandı. İş 07 Gider/Bütçe ve UI 07 başlamadı. Bu fazın API sözleşmesi [errors.md](api/errors.md), kayıtları docs/progress içindedir.

- [x] **BE merkezi 98 kod kataloğu, güvenli parametreler ve kodlu alan hataları:** 86 mevcut beklenen mesaj taşındı; code/message/parameters/severity, fieldIssues ve legacy errors birlikte korunur. 9 yeni ErrorContract testi 400/401/403/404/409/500/binding, Bearer challenge, güvenli 500 ve parametre reddini doğruladı. HTTP durumları korunur; bütün hatalar 404'e çevrilmez.
- [x] **Tam .NET regresyonu: 252/252 başarılı, atlanan 0; 47 unit + 205 integration.** PostgreSQL gerektiren persistence/CRUD/rol/scope/finans testleri ayrı buildingflow_tests veritabanında gerçek PostgreSQL ile çalıştı. Authentication ve yeni ErrorContract testleri store/metadata izolasyonu kullanır; 205 testin tamamı PostgreSQL testi olarak gösterilmez. Ana DB'ye migration/EnsureCreated/rollback/veri temizliği yapılmadı.
- [x] **Frontend 59/59 başarılı, atlanan 0.** Mevcut 51 senaryoya 8 yeni test: bütün 98 BE/FE kodu ve sözlük eşliği, TR/EN parametre/alan çevirisi, bozuk/bilinmeyen kod–status–parametre redaksiyonu, severity, dedup/dört kayıt sınırı, enjekte saatle 8 saniye/remaining pause/dispose, dil değişiminde gruplanan alan hataları, gerçek merkezi async transport ve refresh/body sırasında Abort sessizliği. Parametreli requiredMax'in genel max template'e yanlış eşleşmesi testle yakalanıp düzeltildi.
- [x] **Gerçek tarayıcı TR/EN ve 390×844 kabulü:** boş daire formunda yerel doğrulama tek sağ üst error bildiriminde toplandı; aria-invalid/alan açıklaması/ilk odak ve form korundu. Yönetim türü kilit uyarısı ayrı ikon/status ile gösterildi. Bildirim odağında 8 saniyeden uzun süre korundu; kapatma paneli kapatmadı. Radix dismissable/focus branch düzeltmesi gerçek tarayıcıda doğrulandı. Kodlu gerçek API 404, retry, dil değişimi ve iki aynı GET'te tek bildirim görüldü. Mobil top=16/right=16, width=358 px ve yatay taşma yok; viewport sıfırlandı. Otomatik kapanma tarayıcıda görüldü; kesin süre/hover–focus remaining hesabı timer testindedir. Başarılı finans/ilişki mutation'ı yapılmadı.
- [x] **Kalite ve koruma:** npm run typecheck ve son yol haritası güncellemesinden sonra npm run build başarılı; lisans metadata kontrolü 312 npm/NuGet paket sürümünde PASS. Yeni dependency/asset yok, npm ci/dotnet restore gerekmedi. Başlangıç SHA kontrolünde .env, manifest/lockfile ve bütün migration/snapshot dahil 19 dosya aynı; 115 yerel Markdown bağlantısı ve git diff --check başarılı. Yol haritası 07.1 ve STATUS/CHANGELOG/SESSION aynı kabul kaydına bağlandı.

Komutlar: frontend klasöründe `npm test`, `npm run typecheck`, `npm run build`; kökte `python3 scripts/check-licenses.py`; mevcut SDK ve .env'den ayrı test DB bağlantısıyla `dotnet test backend/BuildingFlow.sln --no-restore --verbosity minimal -m:1`. İlk sandbox test soketi/SWC cache erişimi engellendi; gerekli yerel izinle aynı kontroller başarıyla tekrar çalıştı. Backend doğrulama hatasında legacy errors ve UTF-8 response uyumluluğu ilk testte yakalanıp düzeltildi; ardından tam suite geçti. Çalıştırılmamış kontrol başarılı sayılmadı.

Ekran kanıtları: [form TR](screenshots/phase071-form-tr.png), [uyarı EN](screenshots/phase071-warning-en.png), [404 mobil EN](screenshots/phase071-404-mobile-en.png), [404 masaüstü TR](screenshots/phase071-404-desktop-tr.png), [tamamlanan 07.1 yol haritası](screenshots/phase071-roadmap-tr.png). Fotoğraf/mock veri üretimi yok; mevcut React bileşenleri kullanıldı.

Gerçek sınırlar: canlı kullanıcı verisine başarılı create/update/delete/financial işlem gönderilmedi; 400 ve 409 API sözleşmesinin ayrıntıları izole API testleriyle, genel uyarı/404/form sunumu gerçek tarayıcıyla doğrulandı. Full screen-reader ürünü/OS hover ölçümü ayrıca denenmedi; ARIA/focus/role yapısı ve timer işlevi doğrulandı. TraceId/timeout/OpenTelemetry/SSR render exception sınırları ayrı gelecekteki kapsamlardır; bu fazda yeni paket veya DB tablosu eklenmez. Tek agent; yeni skill/alt agent kullanılmadı.

## Çalışma ve ilerleme protokolü — 2026-10-09: belge planı ve doğrulama sonucu

Kapsam yalnız kalıcı çalışma yönergesi ve mevcut takibe bağlı STATUS/CHANGELOG/SESSION belgeleridir; uygulama kaynakları, şema/API ve bağımlılıklar değiştirilmez. Kontroller geliştirmeden önce belirlendi:

- [x] **Mevcut kök AGENTS kurallarının içerik ve sırası korundu.** UI planındaki tek rapor maddesi ortak protokole bağlandı; diğer UI kuralları aynı. MASTER_PROMPT/PHASES frontend/buildingflow-ui-plan altında bulundu, kökte bulunmadığı raporlandı.
- [x] **Gerçek sayaçlar doğrulandı: İŞ 13 faz kaydı / 58–78 adım / %74,36; UI 12 aşama; ilk teknik plan 8 faz.** STATUS/CHANGELOG/SESSION mevcut milestones/TESTING/UI planına bağlı özetlerdir; uygulama yol haritası değiştirilmedi veya yeni faz onaylanmış sayılmadı. Protokol turu **3/3 = %100**, ürün ilerlemesinden ayrı.
- [x] **84 yerel Markdown bağlantısı, zorunlu rapor alanları/dört durum/bilinmeyen ölçümler ve git diff --check geçti.** Başlangıç dosya hashleriyle **315 mevcut dosya aynı; yalnız 7 izinli Markdown farkı** doğrulandı. Uygulama/şema/API/config/.env/bağımlılık kaynaklarına değişiklik yok.

Codex get_usage_limits başta ve teslimde okundu; hesap pencereleri ve Europe/Istanbul reset zamanları SESSION kaydına alındı. Giriş/çıkış/toplam token, kalan context ve oturuma özel kota ortamdan sağlanmadı: **BİLGİ MEVCUT DEĞİL**. Hesap yüzdesinden token/süre türetilmedi; hesap kimliği kaydedilmedi. Bu tur **uygulama testleri, typecheck/build, restore veya lisans taraması yeniden çalıştırılmadı**; yeni paket/asset yok. Faz 06.1'in önceki 243/51 ve 312 lisans sonucu bu belge turunun test sonucu değildir. Gerçek tek-agent/OpenAI Docs kullanımı SKILLS_AND_AGENTS içinde kaydedildi. Sonraki iş/UI fazı başlatılmadan duruldu; yeni kapsam için kullanıcı onayı bekleniyor.

## 06.1 — Tahakkuk kişi geçmişi ve yönetici onaylı taşınma iptali — 2026-10-09

**Uygulandı:** immutable DuesChargeParty ad/rol/tarih/muhatap snapshot'ı; UNIT/TENANT/OWNER açık seçim, tek muhatap ve atomik batch kontrolü. Borç liste/detay/mobil kartında kiracı/sakin/malik; borç geçmişinden aramalı/sayfalı kişi combo ve rol filtreleri. ACCOUNTANT kişisel veri yetkisi genişletilmedi. Legacy borçlara güncel kişi adı backfill edilmedi.

**Uygulandı:** sekiz BaseEntity alanlı beş neden kataloğu, reasonId seçimi, OTHER açıklaması ve neden kod/ad snapshot'ı. Kullanıcı otomatik iptal yerine yönetici onayı seçti: kiracı bitişinde önizleme, açık ve varsayılan kapalı onay, yalnız incelenmiş chargeId/version'ların atomik iptali. Önizlemeden sonra yeni borçlar eski onaya eklenmez. Ödeme geçmişi, terslenmiş ödeme geçmişi, malik/Unit muhatabı ve eski/aynı dönemler korunur. Onaysız bitiş uygun borçları korur.

**Test kanıtı: 243 backend testi = 47 unit + 196 gerçek PostgreSQL integration, 0 hata / 0 atlama.** Yeni domain testleri snapshot değişmezliği, dönem/scope/rol ve cancellation tarihçesini; yeni API/migration testleri apartment/site, sınır günleri, missing/ambiguous rollback, owner/mode replay, kişi düzenleme/silme sonrası tarihçe ve snapshot kaynaklı filtre/özet/seçenekleri doğrular. ACCOUNTANT veri redaksiyonu/arama yan kanalı/kişisel filtre/atama reddi, anonim/sahte rol/üyelik/silinmiş parent; reason/OTHER/version ve composite FK/tek muhatap/history check testleri geçti. Açık onay/onaysız bitiş, stale/foreign/null/duplicate approvals, yeni borçların eski onaya katılmaması, ücret alınmış ve terslenmiş ödeme geçmişi, **eşzamanlı ödeme–onaylı bitiş ve tahakkuk–bitiş** transaction yarış testleri geçti.

**Migration doğrulandı:** yeni DuesPartyHistoryAndCancellationReasons ve snapshot; eski iptal metni/tutar/audit/version JSON karşılaştırması, ilk kurulum/yükseltme/tekrar/boş Down + yeni tarihçede Down reddi. Tüm uygulama tablolarında sekiz NOT NULL alanın model ve gerçek PostgreSQL şema testleri geçti. PaymentMigrationTests eski sürüme geri dönüş denemesinde daha yeni boş modülün önce geri alınmasını hesaba katıp tekrar güncel şemaya yükseltiyor; önceki migration dosyaları değişmedi.

**Frontend: 51/51 test, strict typecheck ve production build geçti.** Yeni dış DTO testleri kişi/charge/property kapsamı, period ilişkisi, role-restricted/legacy/recorded-empty ayrımı, tekrarlı snapshot/comboseçeneği, reason catalog, önizleme tarihi/version/limit ve BigInt toplamını doğruluyor. Onaysız boş liste ve stale onayın reddi, sonuç sayaçları, güvenli TR/EN alan mesajları; mevcut sözlük/ICU/route/auth/API/format/toplu seçim/ödeme/başlatıcı regresyonları geçti. Alan değişiminde DuesWorkspace key ile filtre/form state'i sıfırlanır. Yeni paket/asset yok; **312 npm/NuGet sürümü lisans metadata kontrolü geçti** (restore sonrası tekrar çalıştırıldı).

**Yerel ortam:** yeni migration uygulandı; eski 10 uygulama tablosunda tüm satırların ID/içerik/audit/version/tutar hashleri aynı kaldı (88 mevcut borç). .env/npm manifest/lockfile/önceki migration dahil 15 korunan dosyanın hashleri değişmedi. Backend yeni kodla localhost:5005 üzerinde, health/PostgreSQL Healthy; mevcut frontend localhost:3003. Kullanıcı verisinde başarılı iptal/bitiş/ödeme/tahakkuk kabul mutation'ı yapılmadı.

**Authenticated tarayıcı kabulü tamamlandı:** kullanıcı mevcut hesabıyla giriş yaptı. Gerçek borç listesinde **88 kayıt / 88.000 TRY**, yeni kişi kolonu, borç geçmişi kaynaklı kişi combo ve rol filtresi görüldü. Mevcut eski borçların kişi geçmişi bulunmadığı açıkça gösterildi; güncel ilişkiyle isim doldurulmadı. **Kiracı filtresi 0 kayıt**, tüm roller tekrar **88 kayıt** döndürdü. Sağ borç detayında beş iptal nedeni yüklendi; neden seçilmeden kayıt reddi ve **Diğer → zorunlu açıklama / ikonlu alan hatası / textarea odağı** kaydetmeden doğrulandı. EN tahakkuk panelinde Unit/Tenant/Owner muhatap seçimi, TR karşılıkları ve mevcut toplu seçim kontrolleri görüldü. Mevcut kiracı ilişkisi için bitiş paneli **0 uygun borç / 0 TRY** önizlemesi ve onaysız bitişin borçlara dokunmayacağını anlatan mesajı gösterdi; Vazgeç ile çıkıldı.

**Mobil/klavye:** **390×844**, sayfa scrollWidth **375 ≤ 390**, sağ tahakkuk paneli clientWidth=scrollWidth **373**, sağ kenar **390**; yatay taşma yok. Muhatap alanı ve toplu seçimler mobilde görünür. Escape paneli kapattı ve **Yeni tahakkuk** düğmesine odak döndü. Türkçe korundu, viewport override kaldırıldı. Kanıtlar: [iptal nedeni ve alan hatası](screenshots/phase061-reason-tr.jpg), [kiracı önizlemesi](screenshots/phase061-tenant-preview-tr.jpg), [mobil borç filtreleri](screenshots/phase061-dues-mobile-tr.jpg), [mobil tahakkuk](screenshots/phase061-assessment-mobile-tr.jpg).

**Gerçek manuel sınır:** yerel eski borçlarda snapshot olmadığı için dolu kişi seçenekleri/kişi isimli yeni borç satırı ve uygun borcu bulunan onay kutusunun canlı kabulü bu kullanıcı verisinde denenmedi; bunların snapshot, filtre, açık onay, atomic cancellation ve stale/version davranışı yukarıdaki ayrı PostgreSQL/DTO testleriyle doğrulandı. Başarılı borç iptali, tahakkuk, ödeme veya ilişki bitişi kullanıcı DB'sinde yapılmadı. Mevcut kullanıcı parolası sıfırlanmadı ve hiçbir parola/token/reset bağlantısı dosyaya veya yeni log sistemine yazılmadı. Ayrı test şemaları yalnız ürettikleri geçici veriyi temizler.

Geliştirme öncesi senaryolar [PHASE_06_1_DUES_PARTIES.md](analysis/PHASE_06_1_DUES_PARTIES.md) içinde yazıldı. Yol haritasına ayrı 06.1 uygulama/API/otomatik test kanıtı eklendi; Faz 07 başlamış sayılmaz. Legacy kişisel geçmiş belirsizliği, katalog CRUD ve 500 üstü toplu onay UI sınırları analiz/API belgelerinde açıklandı.

## FE başlatmada sekme tekrar kullanımı — 2026-10-09: geliştirme öncesi plan

- `scripts/dev.mjs` hazır olunca her süreç başlangıcında OS `open` çağırıyor; AuthGate/Keycloak mevcut sekmede yönlendiriyor. macOS Chrome/Safari'de doğru frontend origin veya aynı client + dönüş origin'li yerel Keycloak giriş sekmesi bulunursa URL'sine dokunmadan öne getirilecek. Başka uygulama/port/realm girişi yanlış eşleşmemeli.
- Sekme bulunmazsa normal açılış; kontrol izni/hatası/timeout durumunda kopya sekme açmadan URL ile anlaşılır uyarı. `BUILDINGFLOW_OPEN_BROWSER=false` korunur; açıkça `always` seçilirse yeni sekme açılır. Windows/Linux mevcut açıcı davranışı korunur.
- Saf URL eşleme ve process adapter testleri: frontend/detail/query, Keycloak client/redirect, yanlış origin/port/client, mevcut/yok/izin reddi/timeout/açıcı hatası/false/always. Native macOS script derleme, frontend tests/typecheck/build, lisans ve korunan dosya hashleri. Native tarayıcı otomasyon izni bu testlerle verilmiş sayılmaz; gerçek GUI kabulü yapılmazsa açıkça kaydedilir.

## FE başlatmada sekme tekrar kullanımı — 2026-10-09: uygulama ve test sonucu

- **Neden doğrulandı:** dev.mjs her yeni Next.js sürecinin `Ready in` çıktısından sonra OS `open` çağırıyordu. AuthGate mevcut `window.location` üzerinde Keycloak adapter yönlendirmesi kullanır; yeni pencere/sekme çağrısı yok. CUA envanterinde Chrome'da iki BuildingFlow giriş sekmesi görüldü; URL/state/nonce değerleri rapora veya loglara kopyalanmadı.
- **Uygulandı:** ortak browser-launch.mjs, macOS Chrome/Safari sekmelerinde bu frontend origin'ini veya yapılandırılmış authority/client/redirect origin ile Keycloak girişini eşleştirir. Eşleşen sekme/pencere öne getirilir; URL değiştirilmez, giriş/form state korunur. Başka port/client/realm ve bozuk/tekrar query parametreleri eşleşmez. Yoksa normal OS açıcı; izin/hata/timeout/beklenmedik çıktı durumunda yeni sekme açmadan Türkçe uyarı. `false` kontrolü kapatır, `always` açıkça yeni sekme ister; Windows/Linux açıcıları korunur. OIDC/PKCE/auth politikası değişmedi.
- **44/44 frontend testi geçti, 0 başarısız/atlanan:** 8 yeni senaryo URL/scope/client/redirect eşlemesi, bozuk/tekrar parametreler; aynı native programın kontrollü Application çiftleriyle Chrome/Safari tab/window odağı ve URL koruması; spawn adapter'da mevcut/yok/izin reddi/signal/timeout/error/unknown output, false/always ve platform davranışı. Gizli OIDC değerleri mesajlara dökülmez. Bu kontrollü testler gerçek tarayıcı otomasyon entegrasyon testi değildir.
- **Typecheck, production build, 312 npm/NuGet sürümü lisans metadata ve whitespace kontrolleri başarılı.** Native JXA programı `osacompile -l JavaScript` ile macOS engine üzerinde derlendi; Chrome ve Safari/CocoaStandard yerel resmi scripting dictionary'lerinde kullanılan tab/window özellikleri doğrulandı. Script derleme tarayıcıya komut göndermez. Yeni paket/asset eklenmedi; .env/npm manifest/lockfile/önceki migration koruma hashleri aynı. Backend/DB/Keycloak ayarı değişmedi ve bu tur backend suite tekrar çalıştırılmadı.

**Gerçek sınır:** native canlı tarayıcıya JXA komutu gönderilmedi ve macOS Automation izni verilmedi/değiştirilmedi. İlk gerçek `npm run dev` sırasında OS izin isteyebilir; kullanıcı izin verirse mevcut sekme kontrolü yapılır, reddederse kopya açılmadan terminal uyarısı kalır. Açık Chrome/Safari sekmesini bulma davranışı kontrol çiftleriyle doğrulandı; gerçek OS GUI kabulü tamamlanmış diye tiklenmedi. Codex iç tarayıcısı/Opera/Edge gibi diğer tarayıcılar bu native tespit kapsamında değildir; bu kullanımda `BUILDINGFLOW_OPEN_BROWSER=false` mevcut sekmeyi korur. Kullanıcının çalışan FE süreci veya giriş formu bu tur durdurulmadı; launcher değişikliği bir sonraki başlatmada devreye girer.

## Toplu tahakkuk seçim deneyimi — 2026-10-09: geliştirme öncesi plan

Kullanıcının Yeni tahakkuk görselindeki seçim akışı: tüm arama sonuçlarını (sayfa ötesi) seç, bu sayfayı seç/çıkar, seçilenlerin tamamını temizle ve tek tek çıkar. Mevcut tek transaction ile 1–500 açık UUID'ye tahakkuk API'si korunur; toplu seçim ayrı kayıt/ödeme oluşturmaz.

- Native tests: çok sayfalı 500'e kadar tüm sonuç, mevcut başka filtre seçimleriyle dedup/atomik union, 500 aşımında mevcut seçimi koruma; count/page/disabled/duplicate değişiminde partial selection olmaması, abort ve network error.
- Tarayıcı: gerçek 48 bölümü tek tıkla seçme, 1250 TRY → 60000 TRY önizleme; sayfa ve tek öğe çıkarma/geri seçme; filtreli seçim, filtre/sayfa değişiminde mevcut seçim korunması, temizleme, tarih/alan/odak/iptal, TR/EN ve 390 px. Kullanıcı DB'sine tahakkuk kaydetmeden doğrulanır.
- Typecheck/build, sözlük/test/lisans/hash/whitespace; şema/backend auth veya sözleşme değişmediği için önceki 221 backend testi yeniden geçmiş sayılmaz. Sonuçlar tamamlanınca kaydedilir.

## Toplu tahakkuk seçim deneyimi — 2026-10-09: tamamlandı

- [x] **Tüm sonuçları seç:** mevcut aramanın bütün sayfaları okunur; görünür ilk sayfa ile sınırlı değildir. Diğer filtrelerdeki seçimler korunur, UUID ile tekrarsız birleştirilir. **5 yeni native test; 48/248/500 sonuç, atomik birleşim, 500 aşımı, değişen sayfa/count, tekrar UUID, pasif bölüm, abort ve network error doğrulandı. Toplam frontend suite: 36/36, 0 başarısız/atlanan.** Yarım kalan okuma mevcut seçimi değiştirmez; okuma iptal edilebilir ve sırasında kaydetme engellenir.
- [x] **Sayfa ve seçim yönetimi:** bu sayfayı seç/çıkar, seçilenlerin tamamını temizle ve tek bölüm çıkarma; görünür seçim ikonları, canlı sayaç, sayı sıralı bölüm etiketleri ve bölüm sayısını gösteren kayıt düğmesi. **Tarayıcıda 48 bölüm × 1.250,00 TRY = 60.000,00 TRY; bir sayfa çıkarınca 38 / 47.500,00 TRY, tek bölüm çıkarınca 47 / 58.750,00 TRY doğrulandı.** Sayfayı yeniden seçme, sayfa değiştirme, temizleme ve arama filtrelerinde seçim korunması çalıştı. “1” aramasıyla 14, “2” aramasıyla tekrarsız toplam 26, filtresiz tümünü seçmeyle 48 bölüm görüldü. Bu denemelerde kayıt düğmesine basılmadı.
- [x] **TR/EN, mobil ve odak:** EN sayfa seçimi 10 bölüm / TRY 12,500.00; TR 390×844 ekranında sayfa genişliği **390/390**, panel **373/373**, yatay taşma yok ve uzun içerik dikey kaydırılır. **2026-11 dönemi / 2026-11-05 vadesi** formda korundu. Vazgeç/Escape sonrası Yeni tahakkuk odağı; borç detayından açılan tahsilatı iptal edince ilgili **Detay** düğmesine dönüş doğrulandı. Dues/Payments TanStack hücre callback ve kolonları sabitlendi; panel açılışında satır düğmeleri gereksiz yeniden oluşturulmaz.
- [x] **Kalite ve koruma:** typecheck ve production build, **312 npm/NuGet sürümü lisans metadata** ve whitespace kontrolleri geçti. Yeni dependency/font/asset yok. **13 korunan .env/npm/önceki migration dosyası SHA-256 ile aynı.** API/DB/auth sözleşmesi toplu seçim için değişmedi; Faz 06'nın **221 backend test sonucu** yukarıdaki frontend kontrolünden önce gerçek ayrı PostgreSQL ile elde edildi. Frontend sonrasında backend suite tekrar çalıştırılmış sayılmaz. Yol haritası **05: 5/5**, **06: 4/4**; yalnız doğrulanmış toplu seçim görevi tik/koyu test kanıtıyla eklendi.

**Tahsilat için ek salt-okunur tarayıcı kabulü:** ilk ödeme kabulünden sonra kullanıcı alanında **40 gerçek borç / 40.000 TRY** görüldü. Daire 10'un 2026-10 dönemi **1.000 TRY** borç ayrıntısı ve boş ödeme geçmişi → Ödeme kaydet → tek sağ tahsilat paneli; seçilen borç **1.000 TRY** dağıtımını doldurdu. **1.000,01 TRY** bakiye aşımı/açıklama hatası istemcide gösterildi; **123,45 TRY** kısmi ödeme önizlemesi kuruşuyla korundu. Form kaydedilmeden kapatıldı. Son odak düzeltmesinden sonra borç → ödeme → Vazgeç → ilgili Detay düğmesi yeniden doğrulandı. Kullanıcının yeni borçları değiştirilmedi veya temizlenmedi; eski 80 satırlık hash karşılaştırması güncel 40 borç için koruma kanıtı olarak kullanılmaz. Canlı tahsilat kaydetme/iptal tarayıcıda yapılmadı; bu finans işlemleri ayrı PostgreSQL API/domain testlerinde doğrulandı.

**Ekran kanıtları:** [Toplu seçim masaüstü](screenshots/dues-bulk-selection-desktop.jpg), [390 px mobil](screenshots/dues-bulk-selection-mobile.jpg). **Sınır:** bütün sayfaların GET okumaları tek DB snapshot değildir; count/page/UUID değişimi seçimi reddeder, aynı sayıda değişimlerin tamamını algılama garantisi yoktur. Asıl tek kayıt transaction'ı bütün açık UUID'leri, etkinliği, yetkiyi ve dönem tekrarlarını sunucuda tekrar doğrular. Sonuç veya birleşim 500'ü aşıyorsa kullanıcı aramayı daraltır; sessiz kırpma yapılmaz.

## 06 — Ödeme ve borç kapatma: manuel tahsilat kapsamı tamamlandı — 2026-10-09

- [x] **Payment/PaymentAllocation + kısmi/tam bakiye:** alınmış CASH/BANK_TRANSFER, tek Property/Unit'in 1–100 borcuna açık dağıtım; TRY/two-decimal toplam eşliği ve bakiye/version kontrolü. PaidAmount, OPEN/PARTIALLY_PAID/OVERDUE/PAID/CANCELLED ve kalan bakiye özetleri. **44 domain/unit testinin tamamı geçti**; 8 yeni ödeme domain senaryosu tutar/toplam/kapsam/iptal ve ödemeli borç iptal engelini doğrular.
- [x] **Atomik kayıt/replay/iptal ve yetki:** Property row lock + güncel charge version, UUID/fingerprint ve aktif normalize banka referansı tekilliği; gerekçeli tek tersleme, borç bakiyesi/audit/version geri açılır, ödeme/dağıtım geçmişi tutulur. **177 integration testinin tamamı gerçek ayrı PostgreSQL DB/şemalarıyla geçti.** Dört finans rolü, anonim/yetersiz/sahte rol, property/unit scope, invalid/future/duplicate/overpay, rollback, concurrent replay/competing payments/cancel race, filtered summary/pagination/literal search, historical Unit ve silinmiş üyelik/property kapsanır. ACCOUNTANT kişi API'sinde 403 kalır. Tam/kısmi ödeme ve iptal sonrası bakiye/API geçmişi ayrı test DB'de doğrulandı.
- [x] **Migration ve zorunlu tablo sözleşmesi:** yeni PaymentsAndAllocations migration/snapshot; eski PaidAmount=0 backfill ve audit/tutar/version koruması, tüm application tablolarının sekiz NOT NULL ortak alanı, composite scope FK/check, ilk kurulum/yükseltme/tekrar/boş Down/re-Up ve finans geçmişinde Down reddi. **`has-pending-model-changes --no-build` başarılı.** Önceki migration SHA-256 dosya özetleri değişmedi. Yerel migration uygulandı, yalnız bu workspace'in API'si yeniden başlatıldı ve PostgreSQL health **200**.
- [x] **Ortak UI ve frontend sözleşmeleri:** ayrı kalıcı Tahsilatlar menüsü/alan seçimi, aynı sağ ManagementPanel form/detay; RecordList desktop/mobil, filtre/gerçek KPI, borç detayında ödeme ve tahsilat geçmişi. Güncel receipt GET ve scope/history/money/allocations DTO doğrulaması, kesin BigInt kuruş toplamı; belirsiz aynı içerikte UUID retry, version/409 yeniden yükleme, cancelled replay uyarısı. **31 frontend native test geçti, 0 başarısız/atlanan**; 8 yeni ödeme testi exact cents/bounds, scope/calendar/method, duplicate/allocation total, reversal history, paginated summary, debt status/balance, OIDC paths ve safe TR/EN problems kapsar. **Typecheck ve production build geçti.**
- [x] **Tarayıcı kabulü, kullanıcı verisine yazmadan:** `/payments` → alan seçimi → seçili alan menüsü ve doğru URL; gerçek sıfır tahsilat/borç görünümü. TR boş form ilk bölüm alanına odak, seçili dairede borç yok mesajı, havale referansı/açıklama/borç seçim hataları; **2026-10-08 tarihi yöntem/diğer alan değişiminde korundu.** İptal/Escape açan düğmeye odak döndürdü. EN ekran/form/alan hataları ve tarih aralığı 400 ikonlu güvenli hata; temizleyle iyileşme. **1280×900 desktop sağ panel 512 px ve sağ kenar=1280**, **390×844 mobil sayfa scrollWidth=clientWidth=390, panel scrollWidth=clientWidth=373 ve dikey kaydırma**. Türkçe geri seçildi ve viewport override kaldırıldı.
- [x] **Koruma/lisans/dokümantasyon:** `.env`, npm manifest/lockfile ve önceki migration hashleri korundu; yeni dependency/font/ikon paketi/harici asset yok, LICENSE/NOTICE korunur. **312 npm/NuGet sürümü lisans metadata kontrolü geçti.** Anonim `/payments` ve property payment SSR HTML'lerinde incelenen gerçek alan adı ve ödeme DTO alanları yok. API sözleşmesi/core/migration/frontend kuralları/README/AGENTS/skill-agent kaydı güncellendi. Yol haritası iş **06** dört görevi **tik/koyu test kanıtıyla** gösterir; UI06 ayrı kayıttır.

**Komut kanıtı:** `.env` üzerinden ayrı `BUILDINGFLOW_TEST_DATABASE=buildingflow_tests` bağlantısıyla `dotnet test backend/BuildingFlow.sln --no-restore --verbosity minimal`: **221/221 = 44 unit + 177 integration, 0 atlanan/başarısız**. `npm test`: **31/31**; `npm run typecheck`, `npm run build`, EF pending-model check, `python3 scripts/check-licenses.py`, readonly SSR/health ve whitespace/hash kontrolleri. Sandbox MSBuild pipe/Docker/local bağlantı engelleri yetkili çalıştırmada tekrarlandı; engellenmiş ilk deneme başarılı test sayılmaz. İlk liste projection hatası düzeltildi ve bütün backend suite tekrar geçti. Migration geri alma testi eski aşamanın history guard'ında durduğunda güncel şemaya yeniden yükseltilir; eski uygulanmış migration değiştirilmez.

**Yerel veri kanıtı sınırı:** migration sonrasında alınan sekiz eski tabloya ait **80 satırın** sayı/veri özetleri API yeniden başlatma ve tarayıcı kabulü sonrası eşleşti. İlk Docker baseline isteği sandbox'ta engellendiği için bu tur yerelde upgrade öncesi/sonrası tam hash karşılaştırması kanıtı yoktur. Yerelde eski aidat satırı bulunmuyordu; eski borç/audit backfill koruması ayrı gerçek PostgreSQL migration testinde doğrulandı. Kullanıcı DB'sinde başarılı tahsilat/iptal/silme mutation veya parola reseti yapılmadı.

**Gerçek teslim sınırları:** UI'da mevcut APARTMENT ile alan/daire seçimi, boş durum ve form/locale/responsive kabulü yapıldı. İlk turda borç kaydı yoktu; borçtan ödeme paneli geçişi ve dağıtım önizlemesi yukarıdaki toplu seçim turunda gerçek mevcut borçla salt-okunur doğrulandı. Tam dağıtım kaydetme ve receipt detay/iptal canlı kullanıcı DB'sinde yapılmadı; iş akışı ayrı PG API/domain, dış veri sözleşmesi ve typecheck/build ile doğrulandı. İki bina tipinin scope'u API testindedir. Docker imajları ve 17 Keycloak smoke bu tur yeniden çalıştırılmadı; JWT/yetki regresyonları 221 suite içindedir. Build mevcut kök lockfile nedeniyle Turbopack root uyarısı verir, başarılıdır. Online kart/POS sağlayıcısı, gerçek banka transferi/iadesi, banka mutabakatı, PDF/fiskal makbuz, avans, dağıtım düzenleme ve otomatik bildirim eklenmedi; tahsilat detayı uygulama içi kayıttır.

**Ekranlar:** [Tahsilat masaüstü ortak sağ panel](screenshots/phase06-payment-form-desktop.jpg), [mobil İngilizce alan hataları](screenshots/phase06-payment-form-mobile.jpg). [API sözleşmesi](api/payments.md) ve [migration akışı](MIGRATIONS.md).

## 06 — Ödeme ve borç kapatma: geliştirme öncesi plan — 2026-10-09

Manuel CASH/BANK_TRANSFER tahsilatı; tek Property/Unit'in 1–100 borcuna açık tutarlarla dağıtım. TRY/two-decimal toplam dağıtımla eşit; bakiye aşımı, iptal borcu ve eski charge version reddedilir. PaidDate 2000–Europe/Istanbul bugün, havalede normalize edilmiş referans zorunlu. UUID/fingerprint replay ve aktif havale referansı tekilliği; Property kilidi altında atomik kayıt/iptal. Gerekçe/version ile ödeme iptali dağıtımı geri alır; kayıtlar korunur. İade burada muhasebe kaydının terslenmesidir, bankaya transfer yapılmaz; sağlayıcılı online ödeme/fiskal belge yoktur.

- Domain: kısmi/tam/geri açılan bakiye, hassasiyet, toplam/kapsam, iptal ve tekrar iptal, ödenmiş borç iptal engeli.
- Gerçek PG API: dört finans rolü, 401/403/sahte JWT/kapsam/üyelik, pasif parent; hatalı veri/foreign charge/Unit, atomik rollback, replay/değişmiş fingerprint/referans; eşzamanlı ödeme ve iptal yarışı; sayfa dışı özet/tarih/arama ve soft-delete geçmişi.
- Migration: tüm uygulama tablolarının sekiz ortak NOT NULL kolonu, composite FK/check/index; eski aidatta paid_amount=0 backfill; model/snapshot eşliği, kurulum/upgrade/tekrar ve ödeme geçmişi varsa Down reddi.
- Frontend: ayrı Tahsilatlar menüsü/alan seçimi, mevcut ortak tablo/mobil kart/sağ ManagementPanel; Unit + borç seçimi, toplam/bakiye, tarih/yöntem/referans, belirsiz retry UUID, detay/iptal/borç geçmişi; TR/EN, alan hatası/odak, 390 px, OIDC return whitelist ve unknown DTO/decimal testleri.
- Kullanıcı DB'sinde başarılı ödeme/iptal mutation yapılmaz. Testler yalnız ayrı test DB/şemalarını temizler. Restore sonrası lisans, .env/npm hash koruma; gerçek sonuçlar doğrulanınca roadmap tiklenir.

## Ortak sağ yönetim paneli — 2026-10-09: geliştirme öncesi kontrol planı

Kullanıcı talebi: Kişilerdeki sağ panel formatı diğer kayıt formlarında da aynı olsun. Yönetim alanı oluşturma/düzenleme, bina/blok, daire, kişi, kişi-daire ilişkisi oluşturma/bitiş ve aidat tahakkuk/detay ekranları aynı ortak paneli kullanacak; silme onayı mevcut ortak onay diyaloğudur.

- Masaüstünde sağ kenar/tam yükseklik/aynı genişlik; 390 px mobilde tam genişlik, uzun içerikte dikey kaydırma ve yatay taşma olmaması.
- Oluşturma ve düzenleme formu alanları, tür kilidi, zorunlu alan hatası/ilk odak, tarihler, Vazgeç/Escape ve açan düğmeye odak dönüşü.
- Tahakkukta bölüm seçimi, form tarih/tutar/önizleme; kişi ilişkisinde rol/tarih; iptal veya boş form doğrulaması kullanıcı verisine mutation göndermemeli.
- `npm test`, typecheck/build, bağımlılık/lisans ve whitespace kontrolü. Backend/API/DB değişmiyor; çalıştırılmayan iş testleri yeniden geçmiş sayılmayacak.

## Ortak sağ yönetim paneli — 2026-10-09: tamamlandı

- [x] **ManagementPanel ortak sunumu:** yönetim alanı oluşturma/düzenleme, bina/blok, daire, kişi, ilişki oluşturma/bitiş, tahakkuk ve borç detayı sağdan açılır. **512 px masaüstü / tam yükseklik, 390 px mobil tam genişlik**, ortak başlık/alt eylem ve içerik kaydırması. Silme onayı mevcut ortak merkez diyaloğunda kalır.
- [x] **Tarayıcı kabulü:** yeni yönetim alanı **dört zorunlu alan hatası**, Escape sonrası açan düğmeye odak; mevcut apartman düzenlemede **tür kilidi ve prefill**, iptal sonrası odak. Daire formu sağda ve Vazgeç odağı doğrulandı. Tahakkuk boş form hataları, **1 bölüm + 12,35 TRY önizleme + 2026-10-30 vade** ve mobil değer korunması; tamamı kaydetmeden iptal edildi. Yeni alan paneli **TR/EN**, mobil **390×844 / yatay taşma yok / dikey kaydırma**. Panel açıkken sayfa veya kayıt değişikliği kaydedilmedi.
- [x] **Kalite:** `npm test` **23/23**, typecheck ve production build başarılı; **312 paket sürümü lisans metadata** ve whitespace kontrolü. Yeni paket/asset yok, `.env` ve frontend manifest/lockfile hash'leri korundu. Odak ilk mount yerine **her açılışta** yakalanır; kalıcı Dialog ile yeniden açılan form odağı düzeltildi.

**Kabul sınırı:** yerelde tek mevcut APARTMENT ve sıfır aidat kaydı olduğundan bina/blok ile borç detayı ayrı canlı kayıt üzerinde açılmadı; ortak EntityEditor/ManagementPanel bağlantısı typecheck/build ile doğrulandı. Kişi/ilişki mutation, backend/Keycloak/DB testleri yeniden çalıştırılmadı; API/şema/auth sözleşmesi değişmedi. Aynı mevcut Next.js çoklu lockfile uyarısı build'i engellemedi. Tarayıcıdaki eski route modülü bir kez yenilenerek güncel panel doğrulandı.

**Ekran kanıtı:** [Yeni yönetim alanı ortak sağ panel](screenshots/management-panel-property-desktop.jpg). UI06 yol haritasına tikli/koyu ortak panel ve test kanıtı eklendi; iş fazı 06 ödeme hâlâ planlıdır.

## UI Aşama 06 — 2026-10-08: geliştirme öncesi test planı

Kapsam görsel planın Daireler/Bağımsız Bölümler ve Kişiler aşamasıdır. İş fazı 06 ödeme/tahsilat bu çalışmanın kapsamına dahil değildir. Referanslar `Daire Yönetim Paneli.png` ve `BuildingFlow Sakinler Yönetim Paneli.png`; mevcut Property/Building/Unit/Person/UnitOccupancy sözleşmeleri korunur.

- Gerçek toplamlar ve seçili yönetim alanı/bina kapsamı; apartmanda doğrudan daireler, diğer türlerde bina seçimi.
- Sunucu araması/sıralaması/sayfalaması; boş sonuç, temizle, loading/error/retry ve pasif üstte yazma kilidi.
- Masaüstü kompakt TanStack tablosu, mobil kartlar ve 390 px genişlikte taşmasız görünüm.
- Sağ detay/düzenleme paneli: güncel yetkili GET, scope doğrulaması, Escape/kapat/iptal/odak, zorunlu alan hataları ve mevcut version/409 davranışının korunması.
- Kişi detayında iletişim ve gerçek malik/kiracı/sakin ilişkileri; tek rol/doluluk tahmini yapılmaması, ACCOUNTANT'a kişi menüsü açılmaması.
- TR/EN metin/ARIA, tarih/sayı formatı; anonim SSR'da kişi/bölüm verisi bulunmaması.
- Frontend native test, typecheck, production build; bağımlılık/.env/backend değişmezliği ve lisans metadata kontrolü. Çalıştırılanlar teslimde kaydedilir.

Kullanıcı kayıtları kabul testi için değiştirilmez; form doğrulaması ve iptal okunabilir/veri yazmayan akışlarla incelenir. API veya DB şeması değiştirilmez. Doluluk, oda planı, aile/TC bilgileri ve dışa aktarım mevcut sözleşmede bulunmadığından referanstan uydurulmaz.

## UI Aşama 06 — 2026-10-09: mevcut API kapsamındaki görsel yenileme tamamlandı

- [x] **Daireler:** lacivert başlık alanı, gerçek bölüm/kat/tür özeti, kompakt shadcn/TanStack tablosu, mobil kartlar, ikonlu aktif/pasif durumu, sunucu araması/sıralaması/sayfalaması ve temizle. **48 mevcut daire**, kat azalan sıralaması, ikinci sayfa, sıfır sonuç/1. sayfaya dönüş ve temizleme tarayıcıda doğrulandı. Kat sayısı kayıtlı metadata; doluluk tahmin edilmez.
- [x] **Detay ve CRUD sunumu:** Unit/Person güncel GET ile sağ panel; dış response shape ve property/building/person scope kontrolü. Daire detayının numara/kat/alanı, detaydan düzenleme ve güncel değerler doğrulandı. Escape/iptal sonrası odak ilk düğmeye döner. Boş daire formu **3**, boş kişi formu **2** alan hatası verdi; ilk hatalı alana odak ve geçersiz e-posta hatası doğrulandı. Silme/version/409 mantığı aynı EntityEditor/DeleteEntity üzerinde korunur.
- [x] **Kişiler ve ilişkiler:** gerçek **2 kişi** liste/mobil kart ve detay iletişim alanları incelendi; mevcut **Malik** ilişkisi bölüm ve tarihiyle göründü. Ayrı ilişki sayfası ve yeni ilişki sağ panelinde **Malik/Kiracı/Sakin** seçenekleri doğrulandı. Başlangıç/bitiş alanı değiştirildikten sonra rol ve birincil iletişim değişimi tarihlerinin korunmasını doğruladı; form iptal edildi. Kişiye tek rol atanmaz, Person login hesabı değildir. Yönetici rol kümesi korunur; ACCOUNTANT kişi ekranına eklenmedi.
- [x] **Responsive / TR–EN / hata:** **390×844** mobilde Unit tablosu gizli, 10 kart görünür; sayfa `scrollWidth=clientWidth=375` (390 px viewport, scrollbar dahil), panelde `scrollWidth=clientWidth=388`. Kişi panelinde de taşma yok. TR/EN başlık, arama/sıralama/ARIA ve İngilizce zorunlu alan hataları incelendi. Bulunmayan yönetim alanında ikonlu **404/güvenli hata ve Yeniden dene** çalıştı. Geçici viewport kaldırıldı, Türkçe geri seçildi.
- [x] **Otomatik doğrulama:** **23 frontend native test**, 0 başarısız/atlanan. Yeni 5 sözleşme testi altı Unit türü/doğrudan apartman, scope, negatif kat/alan/version, kişi iletişim/count ve çoklu rol/geçmiş/geçersiz tarihleri doğrular. **`npm run typecheck` ve `npm run build` geçti.** Anonim units/people SSR HTML'lerinde incelenen gerçek alan adı ve iş DTO alanları yok; API/PostgreSQL health **200**.
- [x] **Koruma ve lisans:** `.env`, npm manifest/lockfile ve backend kaynak dosyalarının tur başındaki SHA-256 değerleri değişmedi. Yeni paket/font/fotoğraf/ücretli template yok. **312 npm/NuGet sürümünün lisans metadata kontrolü** ve whitespace kontrolü geçti; LICENSE/NOTICE korundu. Kullanıcı FE/API süreçleri durdurulmadı; tarayıcıda veri yazan başarılı işlem veya silme yapılmadı.
- [x] **Yol haritası:** genel bakışta ayrı **UI 06** kaydı ve üç tamamlanmış görev **tik/koyu test kanıtıyla** gösterilir. İş fazı **06 ödeme/tahsilat** planlı kalır. Görsel plan, frontend kuralları ve skill/agent kaydı güncellendi; sonraki UI aşamasına geçilmedi.

**Komutlar:** frontend `npm test`, `npm run typecheck`, `npm run build`; kökte `python3 scripts/check-licenses.py`, `git diff --check`; readonly SSR/health ve CUA kabul kontrolleri. 23 test frontend testidir; önceki 196 .NET/17 Keycloak sonucu bu tur yeniden çalıştırılmış sayılmaz. Backend/auth/persistence kodu değişmedi. Son build kökteki boş `package-lock.json` nedeniyle otomatik Turbopack root uyarısı verdi; build başarılı oldu, bu mevcut kök dosya silinmedi/değiştirilmedi.

**Gerçek sınırlar:** site/bina seçimi ve pasif üst kayıt dalı kaynak koddan incelendi; bu tur mevcut apartmanla tarayıcı kabulü yapıldı. Yeni gerçek create/update/delete, iki oturum 409 ve beşten fazla ilişkide panel sayfalaması manuel tekrarlanmadı. Bu mutation kuralları önceki 03/04 backend kanıtlarıdır. Doluluk KPI'sı, oda planı (3+1), kişi bazında bina/daire/rol filtreleri, aile/TC/doğum bilgileri, notlar ve export mevcut DTO/servislerde olmadığı için eklenmedi. Light/Dark/System ve global lacivert sidebar/shell ayrı UI aşamalarıdır. Docker imajları bu UI turunda yeniden derlenmedi.

**Ekran kanıtları:** [yeni daire ekranı](screenshots/ui06-units-desktop.png), [daire detay paneli](screenshots/ui06-unit-details-desktop.png), [mobil kartlar](screenshots/ui06-units-mobile.png), [mobil İngilizce form/alan hataları](screenshots/ui06-unit-form-mobile.png), [kişi araması boş görünümü](screenshots/ui06-people-empty-desktop.png). Kişi iletişim bilgisi içeren görüntü kaydedilmedi; mevcut kişiler yalnızca okunarak doğrulandı.

## Yerel API port çakışması — 2026-10-08: düzeltildi

Neden: doğrulama sırasında açık bırakılan BuildingFlow API, 5005’i kullanırken kullanıcı aynı başlatma komutunu tekrar çalıştırdı. Bu checkout’ın executable yolu doğrulandı ve süreç TERM ile kapatıldı. `scripts/dev.sh api` port doluyken Kestrel’i başlatmadan Türkçe uyarı ve durdurma komutu verir. Yeni `api-stop` tüm port dinleyicilerinin **bu workspace’in BuildingFlow.Api apphost executable’ı** olduğunu doğrulamadan hiçbirini kapatmaz; yalnızca kontrollü TERM kullanır. VS Code **BuildingFlow: Stop BE** görevi eklendi.

- [x] **`bash -n scripts/dev.sh` geçti.**
- [x] **İzole PATH fixture:** yabancı executable reddedildi, geçici süreç hayatta kaldı; dolu portta `api` komutu dotnet/Kestrel başlamadan Türkçe hata döndürdü. Geçici fixture yalnız kendi oluşturduğu süreci temizledi.
- [x] **Gerçek kullanıcı komutu `bash scripts/dev.sh api` başarılı; `/health` HTTP 200 ve PostgreSQL Healthy.**
- [x] **Gerçek `api-stop` portu boşalttı; tekrar stop hatasız “çalışan API yok; port hazır” döndürdü. Teslimde API kapalı, 5005 kullanıcının terminalinde başlatmaya hazır.**

Değişiklik çalıştırma scripti, VS Code görevi ve README/test/agent kaydıyla sınırlıdır. API iş kodu, frontend, şema, .env ve parolalar değiştirilmedi; önceki 196 backend/18 frontend testleri bu küçük script turunda yeniden çalıştırılmış sayılmaz. Manifest/lockfile ve ortam dosyası hash kontrolü; restore sonrası lisans metadata kontrolü ve diff whitespace doğrulandı.

## 05 — Aidat ve borçlandırma — 2026-10-08: tamamlandı

Kullanıcının güncel devam talebiyle **iş yol haritasındaki 05** uygulandı. UI modernizasyonu analizinin sonraki aşamaları başlatılmadı. Önce domain/API/şema/migration, rol/kapsam, mükerrer/rollback, iptal ve frontend kabul senaryoları belirlendi; ilk sonuçlarda bulunan EF liste projection ve eski downgrade test beklentisi düzeltilip kontroller yeniden çalıştırıldı.

- [x] **Domain:** DuesAssessment/DuesCharge; aylık YYYY-MM dönem (2000+), TRY, pozitif/en fazla iki ondalık tutar, vade ve açıklama. Hatalı tutar yuvarlanmaz. Borç Unit’e aittir; malik/kiracı otomatik sorumlu yapılmaz. OPEN/OVERDUE Europe/Istanbul iş gününden hesaplanır; vade günü OPEN. Gerekçe + version ile iptal bakiyeyi sıfırlar ve tutar/geçmişi korur. **36 unit test**, 0 başarısız/atlanan; DuesDomainTests yeni 8 test içerir.
- [x] **Toplu tahakkuk / API:** Apartman doğrudan daireleri ve bina içi bölümler; 1–500 açıkça seçilen aktif Unit, doğru Property/Building uyumu, decimal toplam ve trusted audit. Aynı UUID/içeriğin ardışık/eşzamanlı replay’i; değişen içerik 409. Aynı dönem mükerreri veya kısmi örtüşen seçim tüm işlemi geri alır. **DuesApiTests**: dört finans rolü, scope/anonim/sahte rol/RESIDENT, muhasebecinin Person erişiminin kapalı kalması, 37.05 toplam, idempotency ve eşzamanlı mükerrer senaryoları geçti.
- [x] **İptal / finans geçmişi:** boş gerekçe 400, eski version/ikinci iptal 409; iptal sonrası yeni dönem tahakkuku, sıfır bakiye ve değişmez oluşturma audit’i. Silinmiş Unit’in borcu görünür; soft-delete/etkinlik ile finans geçmişini gizleme ve finans geçmişi olan Property’yi silme reddedilir. IgnoreQueryFilters history join’lerinde aktif üyelik/Property silinme kapsamı açık korunur; kapsam kaldırılması/soft-delete Property sızıntı testleri geçti.
- [x] **Gerçek PostgreSQL:** iki finans tablosunda sekiz NOT NULL BaseEntity kolonu; numeric(10,2), partial unique/check/index ve Unit/Assessment/Property composite FK. **Farklı Property’deki gerçek Unit’e bağlantı** ve charge Property/Unit’ini beraber değiştirip başka assessment altında bırakma DB tarafından reddedildi. İlk kurulum, eski veri/audit/version korunarak yükseltme, tekrar migration, veri bulunan Down reddi ve model/snapshot uyumu geçti. Tüm application tablolarını tarayan model/gerçek şema kontrolleri yeni tabloları da kapsadı. **160 integration test**, 0 başarısız/atlanan; ayrı `buildingflow_tests` DB ve geçici test şemaları kullanıldı, yalnız test verisi temizlendi.
- [x] **Liste / özet:** literal arama, kararlı server sayfalama/sıralama, Unit/dönem/vade/durum; list/count/özet tek RepeatableRead snapshot’ında. Sayfa boyutu 1 iken bile üç kaydın 37.05 toplamı doğrulandı; sadece filtreye uyan kayıtlar hesaba katılır. OPEN/OVERDUE/CANCELLED ve aktif bölüm seçenekleri test edildi.
- [x] **Frontend:** kalıcı ayrı Aidat / Borçlar menüsü ve alan seçimi; shadcn Dialog / TanStack RecordList tablo-mobil kart. İki dilli başlık/alan/ARIA, durum ikonları, alan hataları, başarı/uyarı ve version/409 güncel kayıt yükleme. Açık bölüm seçimi, kuruş bazlı BigInt önizlemesi, gönderim kilidi, belirsiz sonuçta aynı içerik için aynı UUID; dış response shape/scope doğrulaması. **18 native frontend testi**, 0 başarısız/atlanan: önceki 11 regresyon + 7 yeni finance sözleşme testi. **Typecheck ve production build geçti.** Sözlük/ICU taraması artık gerçek useTranslations namespace değişkenlerini izler; finance alan hataları merkezi güvenli allowlist’e eklendi.
- [x] **Tarayıcı kabulü:** ana menüden alan seçimi ve apartmanda gerçek 48 daire; sahte blok yok. Boş form üç alan hatası ve ilk geçersiz alana odak; iki bölüm × 12,35 TRY = **24,70 TRY**. Dönem 2026-11 / vade 2026-11-30, sonraki açıklama değişimi ve bölüm seçimlerinin ikinci seçim sayfasında korunması doğrulandı. Vazgeç ile çıkıldı; kullanıcı verisine tahakkuk yazılmadı. TR→EN başlık/filtre/tutar ve İngilizce dialog; **390 px mobil** sayfa/form, yatay taşma yok (dialog’da scrollWidth=clientWidth=390). Geçici viewport geri alındı; Türkçe ve oturumlu aynı rota yenilemesi korundu.
- [x] **Yerel çalışma / lisans:** migration ayrı PG testleri sonrası yerel DB’ye uygulandı; önce/sonra altı mevcut tablodaki **80 satırın tüm alan/audit/version anonim özetleri aynı**. FE **3003**, güncel API **5005**. Servis smoke ve **17 gerçek Keycloak kontrolü** geçti; auth testi geçici hesabını kaldırdı, mevcut kullanıcının parolası değişmedi. `.env`, npm manifest/lockfile değişmezlik kontrolü ve restore sonrası **312 paket sürümü lisans metadata kontrolü** geçti. Yeni paket, font, görsel veya ücretli template eklenmedi; LICENSE/NOTICE korundu.
- [x] **Gizlilik / teslim:** anonim aidat SSR HTML’sinde incelenen gerçek alan adı veya finans kayıt/fingerprint verisi yok. Core/API/migration/README/agent kayıtları güncellendi; 05’in dört uygulanmış görevi yol haritasında **tik ve koyu test kanıtıyla** tamamlandı olarak gösterilir. **06 ödeme/tahsilat** planlı kaldı.

**Çalıştırılan komutlar:** `bash scripts/test.sh` (cached .NET 10 SDK ile), frontend `npm test`, `npm run typecheck`, `npm run build`; `bash scripts/db.sh update`, `bash scripts/smoke.sh`, `bash scripts/test-auth.sh`, `python3 scripts/check-licenses.py`, `git diff --check`. Build ilk sandbox denemesinde SWC kullanıcı önbelleğine erişemedi; izinli tekrar production build başarılı oldu. Atlanan test başarılı kabul edilmedi.

**Gerçek sınırlar:** bu faz TRY ve bir Unit/dönemde tek iptal edilmemiş aidat; bulk işlemde aynı tutar kullanır. Kısmi ödeme/tahsilat dağıtımı, makbuz, gecikme faizi, otomatik dönem üretimi ve bildirim sonraki kapsam. Ana yerel veritabanında kullanıcıya test borcu bırakmamak için tarayıcıdan gerçek oluşturma/iptal gönderilmedi; bu mutation’lar ayrı PostgreSQL API testlerinde doğrulandı. Borçlu satırın dolu mobil kartı ve iptal dialog’unda iki tarayıcı oturumu yarışı ayrıca manuel çalıştırılmadı. FE/API/Worker Docker imajları bu tur yeniden build edilmedi; host build/test ve çalışan altyapı smoke doğrulandı.

**Kanıtlar:** [tahakkuk formu ve tarih/seçim önizlemesi](screenshots/phase05-assessment-preview.jpg), [mobil İngilizce borç ekranı](screenshots/phase05-dues-mobile.jpg), [Türkçe borç ekranı](screenshots/phase05-dues-desktop.jpg). Ekranlarda kullanıcıya ait alan adı/bölüm numaraları görülebilir; kişi iletişim verisi yoktur.

## Menülerin ana listede görünmesi — 2026-10-08: tamamlandı

- [x] **Daireler ve yetkili kullanıcı için Kişiler, genel bakış ve `/properties` dahil her yönetim sayfasında görünür. Seçili alan yokken ayrı `/units` ve `/people` sayfaları erişilebilen yönetim alanlarını seçtirir; seçim doğrudan ilgili modüle gider. İki modülün alan seçimi tarayıcıda doğrulandı.**
- [x] **Seçili alanın menü bağlantıları kendi alanına gider; apartmanda bina/blok menüsü gizli kalır. Mevcut 48 daire görüldü. Aktif menü, TR/EN başlıklar, `/people` yenilemesinde oturum/dil/rota ve 390 px mobil görünüm doğrulandı (scrollWidth=clientWidth=390). Geçici viewport geri alındı. Kullanıcı kayıtları değiştirilmedi.**
- [x] **Yeni yerel modül rotaları OIDC dönüş whitelist'inde kabul edildi; bozuk/dış/token içeren adresler reddedildi. 11 frontend testi (0 başarısız/atlanan), typecheck ve production build geçti. Package.json/lockfile SHA-256 değişmedi; yeni paket/asset yok. 312 sürüm lisans metadata kontrolü ve git diff --check geçti. Anonim `/people` HTML'de incelenen gerçek property/person ad ve ID'leri yok.**

**Malik sorusu:** mevcut domain/API sözleşmesi ve gerçek ilişki formu incelendi. Form seçenekleri **Malik (OWNER), Kiracı (TENANT), Sakin (RESIDENT)**, varsayılan **Malik**; kişi birden fazla role ve daire birden fazla malike sahip olabilir. Form Vazgeç ile kapatıldı, yeni ilişki yazılmadı. Bu tur backend/DB/migration/.env kodu değişmedi; önceki backend/Keycloak/Docker testleri yeniden çalıştırılmış sayılmaz.

**Ekran kanıtları:** [ana listedeki menüler](screenshots/persistent-menus-desktop.png), [390 px mobil menüler](screenshots/persistent-menus-mobile.png). Ana liste screenshot'unda kullanıcıya ait yönetim alanı adı/şehir ve sayılar bulunur; kişi iletişim bilgisi içermez.

## Frontend standartları — mesajlar, ardından TR/EN: tamamlandı — 2026-10-08

- [x] **Ortak success/error/warning/info MessageBanner ve notification sağlayıcısı; tür başlığı, farklı ikon/semantic renk, erişilebilir duyuru, kapatma ve retry/409 eylemleri. Alan hatası ikonla alan altında kalır; hata yalnızca toast içine taşınmaz.**
- [x] **next-intl TR/EN sözlükleri, cookie locale ve dil seçici; mevcut URL/OIDC dönüş rotaları korunur. Menü, genel bakış/yol haritası, yönetim ve kişi formları, durum/alan hataları, tarih/sayı formatı seçili dile uyar. Kullanıcı verileri/enum/ID çevrilmez.**
- [x] **Bilinmeyen API hata gövdeleri güvenli yerel fallback kullanır; bilinen domain/validation mesajları seçili dilde görünür. Hata/uyarı/success birbirinden metin ve ikonla ayırt edilir.**
- [x] **Otomatik sözlük anahtar/placeholder eşliği, locale whitelist/fallback, mesaj türü/hata mapping, dönüş rotası ve format regresyonları; typecheck/build, paket/lisans kontrolü. Tarayıcı TR↔EN, form zorunlu alanları, retry/uyarı/başarı, mobil ve yenilemede dil/rota kabulü.**

Bu kutular gerçek uygulama ve aşağıdaki test sonuçlarıdır. Kapsam mesaj/dil ve kullanıcının eklediği ayrı menü/sayfa düzenidir; RHF/Zod/Query, tema ve diğer planlı 17 standart bu başlıkla otomatik tamamlanmaz.


**Çalıştırılan doğrulamalar:** 11 native frontend testi (0 başarısız/atlanan); npm typecheck ve production build; 17 gerçek Keycloak auth-smoke kontrolü; servis smoke; frontend Docker imajı derleme ve LICENSE/NOTICE kopyalama; anonim yeni units HTML kontrolü; 312 npm/NuGet sürümü lisans metadata kontrolü. .NET/domain/persistence ve migration kodu bu tur değiştirilmedi; önceki 171 backend testi bu tur yeniden çalıştırılmış sayılmaz.

**Tarayıcı kabulü:** TR→EN genel bakış/yol haritası ve yönetim listesi; dört İngilizce required alan mesajı, dört ikon ve ilk alan odağı; API başarı bildirimi; gerçek duplicate bina 409 uyarısı (form değeri kaldı), 404 güvenli hata/yeniden deneme ve apartman blok adresinde info + Daireleri aç. Apartmanda blok menüsü gizli, gerçek mevcut 48 daire ayrı sayfada görüntülendi. Kurgusal Site → Bina → Unit → Kişiler → ayrı kişi ilişkisi sayfası; 01–02 Ekim geçmiş ilişkisi, tarih input sonrası checkbox değişimi ve kayıt doğrulandı. İngilizce 10/01/2026–10/02/2026 → Türkçe 01.10.2026–02.10.2026; yenilemede aynı person adresi ve dil korundu. İngilizce çıkış ekranı ve Keycloak ui_locales=en ile özel giriş metinleri görüldü.

**Mobil:** 390×844, kişi ilişki tablosu gizlenip 348 px kart gösterildi; document scrollWidth=clientWidth=390, yatay taşma yok. Geçici viewport geri alındı. Kanıtlar: [masaüstü menüler](screenshots/standards-menus-tr.png), [mobil menüler/kart](screenshots/standards-menus-mobile.png). Ekranlar yalnızca kurgusal test kişisini içerir.

**Fixture temizliği:** yalnızca bu tur oluşturulan BF Standards Menu Test 20261008 (46c52bcd-5bdb-4956-846f-93bc83b72ce6), BF Test Block, BF-U1 ve Standard Test Person kullanıcı API'siyle soft-delete edildi. İlişki baştan geçmiş ve bitmiş tarihlerle oluşturuldu; geçmiş fiziksel silinmedi. Var olan yönetim alanı, 48 dairesi, .env ve geliştirme hesabı/parolası değiştirilmedi.

**Gerçek sınırlar:** RHF/Zod/Query, dark theme, permission grant, machine-readable backend error code/traceId ve önceki yönetim tablolarının mobil kart dönüşümü hâlâ planlıdır. Yeni bina seçimi/kişi listeleri RecordList ile mobil kart sunar; mevcut HierarchyRows tablosu kendi container'ında yatay kayabilir. Native takvim düğmesi tarayıcı/işletim sistemi dilini kullanabilir. Oluşturma/düzenleme/silme form dialog'ları korunur; ayrılanlar ana yönetim ekranlarıdır. Bilinmeyen backend title/alan mesajı güvenli fallback olur. Üretim SMTP bu kapsamda ayarlanmadı.

## 04 — Kişiler ve daire ilişkileri: tamamlandı — 2026-10-08

Senaryolar geliştirmeden önce bu bölümde planlandı; aşağıdaki kutular gerçek çalıştırma sonuçlarıdır.

- [x] **Person/UnitOccupancy domain ve audit:** yönetim alanına özel kişi, ad/soyad, isteğe bağlı telefon/e-posta; TC kimlik numarası yok, Person giriş hesabı değildir. **7 yeni domain testi** normalize/atomic validation, inclusive tarihler, rol/ID ve bir kez bitiş kaydını doğruladı.
- [x] **18 yeni API/gerçek PostgreSQL testi:** yönetici CRUD/trusted audit/version, anonim 401, RESIDENT/ACCOUNTANT ve standart Role/resource_access sahte yükseltmesi 403, aktif üyelik ve cross-property/person/unit 404. Silinmiş üyelik/property history sorgusunda da korunur; pasif üstte yeni ilişki reddi, validation/literal arama/server pagination, tarih/composite FK kısıtları doğrulandı.
- [x] **İlişki bütünlüğü:** aynı kişi/daire/rol inclusive çakışma, bir dairede çakışan birincil iletişim; ortak malik/çoklu rol, eşzamanlı bounded çift kayıt (1 Created/1 Conflict), bitiş geçmişi ve soft-delete sonrası isim saklama geçti. Devam eden/gelecekte biten ilişki Person/Unit silme ve pasife almayı engeller.
- [x] **Migration:** yeni PeopleAndUnitOccupancies/snapshot; bütün uygulama tablolarında sekiz zorunlu kolon model/gerçek PostgreSQL kontrolü geçti. Ayrı test şemasında ilk kurulum/yükseltme/tekrar, boş şemada geri alma; kişi ve bitmiş ilişki verisi bulunan Down reddi ve veri/geçmiş korunması doğrulandı. Eski uygulanmış migration değiştirilmedi.
- [x] **Tarayıcı kabulü:** geçici apartman ve doğrudan daire → Kişileri yönet → boş liste → zorunlu ad/soyad hatası/ilk odağı → kişi oluşturma → aktif malik ve 01 Ekim başlangıçlı sakin → 07 Ekim bitişi/Bitti/geçmiş. Tarih alanı başka alan değişiminde geri dönüyordu; input event ile düzeltildi, tekrar kaydedilen 01–07 Ekim aralığı ekranda doğrulandı. İlk eski API derlemesinde gerçek error/retry görünümü görüldü; güncel API başlatılınca retry başarılı oldu.
- [x] **Responsive/oturum/gizlilik:** masaüstü tablo ve 390×844 mobil kart/kişi modalı, Vazgeç, yatay taşma yok; viewport geri alındı. Sayfa yenileme kişi rotasını korudu. Anonim HTML'de test kişi/ad/e-posta/property adı yok. Kanıt: screenshots/people-desktop.png, people-mobile.png, people-create-mobile.png.
- [x] **Son regresyon:** **171 .NET testi (28 unit + 143 integration), 0 başarısız/atlanan**; **3 native frontend testi** (canonical/legacy/people dönüş whitelist, dış/bozuk dönüş reddi, TR/EN date-only aynı gün); **17 gerçek Keycloak kontrolü**, servis smoke, npm typecheck/build ve **262 paket sürümü lisans metadata kontrolü** geçti. FE/API Docker imajları derlendi; yerel kullanım şu an host FE 3003/API 5005 üzerinden.
- [x] **Belge/yol haritası:** core, people API, migrations, frontend conventions, README ve skill/agent kaydı güncel; yalnızca doğrulanmış 04 görevleri ekranda koyu/tikli.

Yerel migration uygulandı. Önce/sonra 5 property, 3 building, 3 unit, 5 membership sayısı aynıydı; building/unit/membership satır parmak izleri eşleşti, property parmak izi farklıydı. Migration Property UPDATE içermez; kullanıcı aynı anda veri değiştirebildiği için bütün yerel satırların birebir değişmezliği iddia edilmez. Ana veritabanı geri alınmadı. Sonraki UI testi yalnızca kendi oluşturduğu BF04 UI Test 20261008 property ID'sine sınırlandırıldı; fixture temizliği ad+ID guard ve transaction ile **soft-delete** yaptı, kayıt/geçmiş fiziksel silinmedi. Bu temizlik kullanıcı API'sindeki devam eden ilişki engelini test etme sonucu değildir; engel ayrı PostgreSQL API testinde doğrulandı. .env ve mevcut hesap parolası değiştirilmedi.

Kalan sınırlar: Person birden çok property arasında ortak kimlik eşleştirme yapmaz; TC kimlik numarası, otomatik login daveti/bildirim, hisse yüzdesi, toplu import, geçmiş ilişki düzeltme/geri yükleme ekranı yok. Birincil iletişim ilişkide işarettir; gönderim servisi değildir. Tam TR/EN ekran/i18n, RHF/Zod/Query ve dark theme geçişleri hâlâ planlı; bu fazda yeni bağımlılık eklenmedi. Node built-in testleri için TS import destekli Node 24+ kullanın. Üç tarayıcı kanıtı yalnızca kurgusal test kişisini içerir; manuel kontroller 171 otomatik test sayısına eklenmez.

## Frontend kodlama kuralları — 2026-10-08: belge doğrulaması tamamlandı

Kullanıcı tarafından verilen 17 başlık, [Frontend Convention](frontend-rules.md) içinde Next.js çalışma kuralları olarak repo içine kaydedildi. Bu tur yönerge/dokümantasyon kapsamındadır; bütün i18n/form/state/theme paketlerini kurmak veya mevcut ekranları topluca dönüştürmek kapsamda değildir.

- [x] **Kök ve frontend AGENTS.md ortak belgeyi zorunlu okuma olarak bağladı.** Next.js'in oluşturduğu yönerge bloğu SHA-256 karşılaştırmasıyla byte düzeyinde aynı kaldı; iki AGENTS dosyası toplamı varsayılan 32 KiB sınırının altında.
- [x] **17 numaralı başlık doğrulandı.** Mevcut uygulama ve planlı paket/entegrasyonlar ayrı durum tablosunda; henüz kurulu olmayan next-intl/RHF/Zod/Query/Zustand/lint altyapısı tamamlanmış gösterilmedi.
- [x] **Mevcut sözleşmelerle belge incelemesi tamamlandı.** Gerçek SITE_ADMIN dahil roller, aktif üyelik/backend yetkisi, Property/Building/Unit, ilişki geçmişiyle tür kilidi, soft-delete, version ve ProblemDetails korundu; örnek ADMIN/STAFF veya error code çalışan sözleşme gibi aktarılmadı.
- [x] **35 yerel Markdown bağlantısı çözümlendi; `git diff --check` geçti.** Bu tur öncesi/sonrası package.json ve lockfile SHA-256 değerleri aynı; `python3 scripts/check-licenses.py` **262 paket sürümünün lisans metadata kontrolünü geçti** ve envanteri güncelledi.

Bu tur uygulama kodu, bağımlılık veya DB şeması değiştirilmedi; frontend build/typecheck, .NET/Keycloak/servis ve tarayıcı testleri yeniden çalıştırılmadı. Aşağıdaki sayılar önceki uygulama teslimlerinin sonuçlarıdır; belge doğrulamasına eklenmedi.

04 kişi/ilişki fazının teslim tarihindeki doğrulama: **2026-10-08**. **171 .NET testi (28 unit + 143 integration), 0 başarısız, 0 atlanan; 3 frontend testi; 17 gerçek Keycloak kontrolü.** Frontend typecheck/build, servis smoke ve 262 paket sürümü lisans kontrolü geçti. Başarılı otomatik test ve manuel inceleme ayrı belirtilir. Henüz uygulanmayan işin kutusu boş kalır. Atlanan test başarı sayılmaz.

## 00 — Proje temeli: tamamlandı

- [x] **11 domain testi:** zorunlu alanlar, Site/Block/Apartment ilişkileri, boş değerler, case-insensitive tekrarlar, pozitif metrekare ve bodrum katı.
- [x] **2 persistence testi:** EF mapping/query filters/Restrict ve gerçek PostgreSQL üzerinde migration, audit, immutable oluşturma alanları, soft-delete, concurrency ve unique constraint.
- [x] **Build ve kurulum:** .NET solution ve frontend build; önceki ilk kurulumda Docker imajları/smoke kontrolleri geçti. Bu iterasyonda Keycloak ve yeni Mailpit başlatıldı; yerel servis smoke kontrolü tekrar geçti. Tüm Docker imajları yeniden build edilmedi.
- [x] **Yerel çalışma ve proje kaydı:** VS Code görevleri, 3003 varsayılan frontend portu ve tarayıcı açma scripti; ilk iskelet commit'i 1913a61.

## 01 — Giriş, çıkış ve şifre yenileme: tamamlandı

- [x] **9 API kimlik/CORS testi:** anonim 401; geçerli imzalı token 200; hatalı issuer, audience, imza, süresi dolmuş ve malformed token 401; izinli/izinsiz CORS origin.
- [x] **17 gerçek Keycloak protokol kontrolü:** anonim API reddi, yanlış parola, Authorization Code + S256 PKCE, gerçek JWT ile API 200, frontend CORS preflight, token yenileme, logout yönlendirmesi, kapalı oturumun refresh reddi, reset isteği, Mailpit teslimi, linkten yeni şifre formu, şifre tekrarı uyuşmazlığı, zayıf şifre, başarılı değiştirme, kullanılmış linkin reddi, eski şifrenin reddi, yeni şifreyle giriş.
- [x] **Tarayıcı:** anonim ziyaretçinin seçilen temaya yönlenmesi, geliştirme hesabıyla giriş, kullanıcı adı ve yol haritası, logout sonrası /login, Şifremi unuttum formu.
- [x] **Responsive inceleme:** iki sütunlu masaüstü ve tek sütunlu mobil görünüm. Kanıt: screenshots/login-desktop.jpg, screenshots/login-mobile.jpg ve screenshots/password-reset.jpg.
- [x] **Frontend:** npm run typecheck ve npm run build başarılı; npm audit 0 bilinen açık bildirdi.
- [x] **Lisans metadata kontrolü:** 143 npm/NuGet paket sürümü tanımlı lisansla doğrulandı. Yeni keycloak-js Apache-2.0, MVC.Testing MIT, Mailpit MIT; ayrıntılar licenses.md.
- [x] **Dokümantasyon:** README, AGENTS.md, SKILLS_AND_AGENTS.md, lisans envanteri; tamamlanan ekran görevleri tikli ve test kanıtı koyu.

Tarihsel .NET sonuçları: 01 **22 başarılı**; 02 teslim **56 başarılı**; zorunlu şema kontrolü sonrası **57**, kurulum/yükseltme kontrolleriyle **60** (11 domain + 3 persistence + 3 migration + 9 kimlik/CORS + 34 site API). Hepsinde **0 başarısız, 0 atlanan**. Güncel 03 sonucu üstte ve aşağıdaki teslim kaydındadır. UI kontrolleri bu sayılara eklenmedi.

02 migration yerel veritabanına uygulandı; backend yeni kodla **5005**, frontend **3003** üzerinde yeniden başlatıldı. Eski PostCSS önbelleği nedeniyle UI bağımlılıkları kurulduktan sonra dev süreci yeniden başlatılarak tarayıcı kontrolü yapıldı.

## Tekrarlama komutları

Docker Desktop açık; .NET 10 SDK, Node.js/npm ve Python 3 kurulu olmalı. Proje kökünde:

```bash
bash scripts/dev.sh infrastructure
bash scripts/test.sh
python3 scripts/check-licenses.py
```

Frontend:

```bash
cd frontend/buildingflow-web
npm ci
npm run typecheck
npm test
npm run build
npm audit
```

API'yi başka terminalde başlat, ardından proje kökünde:

```bash
bash scripts/dev.sh api
# API hazır olduktan sonra ayrı terminal:
bash scripts/test-auth.sh
```

Test wrapper yalnızca kendisinin oluşturduğu geçici buildingflow realm hesabını değiştirir, sonunda siler; uygulama kullanıcısının parolasına dokunmaz. Üretilen test e-postaları yerel Mailpit'te kalabilir. Gerçek kişiye mail gönderilmez. DB testleri ayrı buildingflow_tests veritabanında çalışır. Normal dotnet test komutunda BUILDINGFLOW_TEST_DATABASE verilmezse PostgreSQL testi atlanır.

## Üretim ve ilave doğrulamalar

- [ ] Gerçek SMTP sağlayıcısında TLS, gönderen adresi ve gerçek posta kutusuna teslim. Yerelde yalnızca Mailpit doğrulandı.
- [ ] 10 dakikalık reset linki süresinin dolmasını bekleyerek manuel kontrol. Keycloak realm ayarı 600 saniye; otomatik test tek kullanımlılığı doğrular.
- [ ] Üretim HTTPS, secure cookie, gerçek origin/redirect URI ve Keycloak production modu.
- [ ] Container SBOM/telif dağıtımı ve kuruluşun ticari araç lisansı uygunluğu.

Frontend AuthGate hassas veri için güvenlik sınırı değildir. Yalnızca statik yol haritası ve genel sağlık bilgisi SSR'da var. Yönetim alanı/bina/bölüm verisi client-side yetkili API ile alınır; anonim SSR içine konmaz; yetki backend'de uygulanır. Logout refresh oturumunu kapatır; daha önce verilen access JWT kendi geçerlilik süresi bitene kadar API tarafından kabul edilebilir. Anlık token iptali bu fazda uygulanmadı.

## 02 — İlk site akışı: tamamlandı

SUPER_ADMIN global erişimi; diğer kullanıcılar aktif SiteMembership kaydıyla erişir. Oluşturma Site + creator membership tek transaction; audit JWT sub üzerinden alınır. shadcn/ui + TanStack Table kullanılır. Güncelleme/silme ve üyelik atama UI/API'si bu fazın kapsamında değildir.

- [x] **34 yeni site API testi, gerçek PostgreSQL:** anonim create/list/detail 401; 7 rol/claim varyantı 403; eksik sub 403; geçerli SUPER_ADMIN 201/Location; creator membership ve güvenilir audit; 8 boş/uzun alan, eksik 4 alan, geçersiz pagination/sort/search; kapsam dışı detay 404; forged scope query reddi; pasif/silinmiş üyelik reddi; soft-delete; kararlı sıralama; arama/pagination içinde erişim kapsamı; literal %/_ araması.
- [x] **Persistence regresyonu:** mevcut 2 test geçti. SiteMembership migration'ı ayrı `buildingflow_tests` veritabanında rastgele, fixture'a ait şemaya uygulanır ve yalnızca bu şema temizlenir. Tracked üyelikle parent soft-delete, Restrict ilişkiler ve audit/concurrency korundu.
- [x] **Tarayıcı oluşturma akışı:** geliştirme hesabıyla giriş → boş liste → formda 4 zorunlu alan hatası ve ilk alana odak → hata düzeltme → geçerli formda çift tıklama → yalnızca 1 kayıt, başarı bildirimi ve listede güncelleme → detayda ad/şehir/ilçe/adres eşleşmesi.
- [x] **TanStack Table kontrolleri:** Kadıköy araması kayıt döndürdü; bulunmayan arama boş sonuç gösterdi; Adres sütunu açıldı; ad sıralaması descending oldu; sayfa boyutu 20 seçildi. Tarayıcı testi tek kayıtla yapıldı; çok sayfalı kararlılık/kapsam backend testleriyle doğrulandı.
- [x] **Detay ve oturum:** detay yenilenince aynı adres/kayıt korundu. Bulunmayan GUID kullanıcıya 404 erişim mesajı gösterdi. Anonim detay HTML'inde test kaydının adı/adresi bulunmadı. sessionStorage'da sadece whitelist yerel dönüş rotası var, token/parola yok.
- [x] **Responsive inceleme:** 390×844 mobil tablo ve modal; sayfa genişliği viewport'u aşmadı; hatalı alan düzeltilince aria-invalid temizlendi; Kapat/Vazgeç çalıştı. Ekran görüntüleri: screenshots/sites-desktop.jpg, sites-create.jpg, site-detail.jpg, sites-mobile.jpg, sites-create-mobile.jpg.
- [x] **Frontend son kod:** `npm run typecheck` ve `npm run build` başarılı; `npm audit --audit-level=high` **0 bilinen açık**.
- [x] **Kimlik/servis regresyonu:** yeni backend ile `bash scripts/test-auth.sh` **17 kontrol** geçti; `bash scripts/smoke.sh` servis sağlığı, anonim/geçersiz JWT ve worker kontrolleri geçti.
- [x] **Lisans ve belge kaydı:** `python3 scripts/check-licenses.py` **262 npm/NuGet paket sürümü** doğruladı. shadcn MIT lisansı/NOTICE korunur; MPL-2.0 build bağımlılığı resmî lisansla kaydedildi. README, API belgesi, AGENTS, SKILLS_AND_AGENTS ve ekran yol haritası güncel.

Yerel UI testi sadece kendi oluşturduğu `Çınar Yaşam Sitesi · UI Test` kaydını ve üyeliğini temizler; mevcut kullanıcı/veri korunur. Test ekran görüntülerindeki kayıt bu geçici örnektir. Formun gönderim kilidi hızlı çift tıklama içindir; genel ağ tekrarı/idempotency henüz kapsamda değil. Container FE/API imajları bu fazda yeniden derlenmedi; host frontend build ve .NET test build doğrulandı.

## Zorunlu tablo alanları — 2026-10-07

Kullanıcı tüm yeni uygulama tablolarında sekiz BaseEntity kolonunu zorunlu tutuyor. İlişki/üyelik tabloları dahildir. Bu kontrol yeni entity'leri modelden otomatik bulur; yalnızca bugün var olan tablo adlarına sabitlenmez.

- [x] **Model sözleşmesi:** ModelHasExpectedRelationalMapping tüm mapped entity'lerin BaseEntity kalıtımını, sekiz kolonun ad/NOT NULL/CLR türünü, id PK ve version concurrency token şartını doğruladı.
- [x] **Gerçek PostgreSQL:** EveryApplicationTableHasRequiredAuditColumns; migration sonrası sites, blocks, apartments, site_memberships tablolarının sekiz kolonu, SQL türleri ve NOT NULL şartı information_schema üzerinden doğrulandı.
- [x] **Regresyon:** scripts/test.sh ayrı buildingflow_tests veritabanında **57 başarılı / 0 başarısız / 0 atlanan**; audit/soft-delete/concurrency kontrolleri geçti. Bağımlılık veya uygulama şeması değişmedi; lisans kontrolü **262 paket** için tekrar geçti.

## Migration ve kurulum — 2026-10-07

- [x] **Migration helper:** db.sh list iki kaynak migration'ı gösterdi; status yerel DB'de ikisinin de uygulanmış olduğunu gösterdi; check migration'sız model değişikliği bulmadı; sql artifacts/database/buildingflow.sql üretti. Hatalı Add adı çıkış 2 ile dosya oluşturmadan reddedildi; bash -n başarılı.
- [x] **Model/snapshot testi:** CurrentModelHasNoUnrecordedSchemaChanges, EF HasPendingModelChanges ile son modelin migration snapshot'ıyla uyumunu doğruladı.
- [x] **Gerçek PostgreSQL kurulum/yükseltme:** FreshInstallationAndRepeatedApplicationKeepExistingData ve UpgradeFromInitialFoundationPreservesExistingData geçti. Her test kendi geçici şemasını ve migration geçmişini kullandı/temizledi; ad, version, oluşturma/değişiklik audit'i ve etkinlik korunurken eksik migration uygulandı. Tekrar MigrateAsync aynı geçmiş/veri durumunu korudu.
- [x] **Sonuç ve kayıt:** scripts/test.sh **60 başarılı / 0 başarısız / 0 atlanan**; lisans kontrolü **262 paket** için geçti, yeni bağımlılık yok. MIGRATIONS.md, README, AGENTS, agent durumu ve VS Code görevleri güncellendi. Helper update veya geçerli add komutuyla bu turda uygulama DB/kodu değiştirilmedi.

Üretilen idempotent SQL artifact'i incelendi; SQL dosyası bu turda PostgreSQL'e uygulanmadı. Gerçek ilk kurulum/yükseltme/tekrar testleri EF migrator üzerinden çalıştı. Üretim dağıtım pipeline/bundle ve gerçek DBA SQL uygulaması henüz doğrulanmadı.

## 03 — Başlangıç planı ve kapsam değişikliği

Kodlamadan önce CRUD, alan validasyonu, yetki/üyelik, arama/sayfalama, version/parent yarışları, rollback, gerçek PostgreSQL, responsive arayüz ve OIDC dönüş rotası senaryoları belirlendi. İlk Site → Blok → Daire kapsamı, kullanıcının talebiyle ortak Property/Building/Unit modeline genişletildi. Nihai sonuçlar aşağıdaki 03 teslim kaydındadır; bu revizyonda gerçek şema değişikliği için yeni migration eklendi.

## Her kodlama turunda tamamlanma kontrolü

- [ ] İlgili domain/API/UI senaryoları ve negatif vakalar belirlendi.
- [ ] Etkilenen testler çalıştırıldı; komut, sonuç ve atlananlar kaydedildi.
- [ ] Yeni bağımlılık/asset/sürüm lisansı doğrulandı; envanter güncellendi.
- [ ] Ekran yol haritası gerçek test kanıtıyla güncellendi.
- [ ] Skill/agent durumu ve çalıştırma yönergeleri güncellendi.
- [ ] Kullanıcıya somut sonuç ve kalan sınırlar bildirildi.

Bu liste sonraki kodlama turlarında yeniden uygulanır; boş kutular tamamlanmış fazların sonucu değildir.

## 03 — 2026-10-08: Yönetim Alanları, tamamlandı

Kullanıcı talebiyle ortak model Property → opsiyonel Building → Unit uygulandı. APARTMENT doğrudan Unit içerir (building_id NULL); SITE/RESIDENCE/MIXED içindeki Unit bir Building gerektirir. İlk dört PropertyType: APARTMENT, SITE, RESIDENCE, MIXED. Unit türleri APARTMENT/OFFICE/SHOP/VILLA/WAREHOUSE/WORKSHOP; kat mevcut floor alanıdır. Daire sayısı gerçek kayıt sayısıdır; kat sayısı metadata olarak girilir, sahte blok veya otomatik boş daire üretilmez.

- [x] **Domain — 17 test:** trim, aynı ebeveynde tekrar, değişiklikten önce bütün alanların doğrulanması, negatif kat, pozitif ve iki ondalıklı alan, dört üst tür ve doğru hiyerarşi. Domain EF/HTTP bağımlılığı almadı.
- [x] **API/persistence — ayrı gerçek PostgreSQL:** CRUD, trusted audit, version, soft-delete, ad/numara tekrarında 409, silinen numaranın tekrar kullanılması, arama/literal %/_, whitelist sort, kararlı sayfalama ve gerçek çocuk sayıları. Sekiz NOT NULL ortak kolonun model ve PostgreSQL sözleşmesi geçti.
- [x] **Yetki ve negatif vakalar:** anonim 401, üç yazma rolü, yetersiz/sahte rol ve eksik sub, aktif üyelik kapsamı, yanlış/kapsam dışı ebeveyn 404. Doğrudan apartman rotalarında okuma/yazma ayrımı doğrulandı. Rol vakaları otomatik API testleridir; tarayıcı kabulü geliştirme hesabıyla yapıldı.
- [x] **Ortak model:** dört PropertyType oluşturuldu; apartmanda doğrudan daire CRUD ve building_id NULL, sahte Building oluşmadığı doğrulandı. SITE/RESIDENCE/MIXED doğrudan Unit ve APARTMENT Building yazımını reddetti. Altı UnitType karma yapı testlerinde geçti; apartmanda OFFICE reddedildi, body ile Property türü değiştirilemedi.
- [x] **Veritabanı son koruması:** yanlış Property/Building eşleşmesini composite FK reddetti. Çocuk koleksiyonu yüklenmeden doğrudan apartmandaki A1/a1 tekrarını PostgreSQL partial unique index reddetti; tek geçerli satır kaldı.
- [x] **Concurrency ve işlem bütünlüğü:** eski version, dolu üst kayıt ve pasif üstte yazma 409; eşzamanlı duplicate ve üst kayıt pasife alma/silme yarışları, transaction rollback ve kısmi veri bırakmama. Alt yazım parent version/audit alanını da aynı transaction içinde güncelledi.
- [x] **Migration — gerçek PostgreSQL:** `20261007210343_UnifiedPropertyModel` rename/backfill ile ID/audit/version/üyelik/ilişkileri korudu. unit.property_id eski block.site_id'den dolduruldu. İlk kurulum, ilk sürümden yükseltme, tekrar uygulama ve model/snapshot kontrolü geçti. Güvenli Down/Up geçmişi korudu; yeni tür/doğrudan daire varsa veri kaybına yol açacak Down reddedildi.
- [x] **Mevcut yerel veri:** yeni migration uygulandı. Migration öncesi/sonrası mevcut 6 satırın iş/audit/version/durum/ebeveyn alanlarını kapsayan parmak izi birebir eşleşti. `.env` ve geliştirme hesabının parolası korunur. Mevcut kayıtların türü SITE olarak backfill edilir; adından tür tahmin edilmez.
- [x] **Tarayıcı — apartman:** dört tür seçimi → ABC Apartmanı / 8 kat → doğrudan Daireler; blok bölümü yok. Daire 1, bodrum katı ve 120,25 m² oluşturuldu; hızlı çift tıklama tek kayıt verdi. 1A olarak düzenleme/pasife alma, gerçek 8 kat / 1 daire özeti, üst kaydı pasife alma ile alt ekleme engeli ve yeniden etkinleştirme doğrulandı.
- [x] **Tarayıcı — bina/karma yapı:** bina adı tekrarında 409 ve dolu üst kaydın silme reddi; City Center → Ofis Kulesi / 12 kat → Ofis 401. Oluşturma/düzenleme/silme akışları ve tipe uygun tablo başlıkları incelendi. Canonical nested sayfa yenilemesi ve OIDC sonrası dönüş rotası korundu.
- [x] **Tarayıcı — iki oturum:** aynı bölüm iki sekmede düzenlendi; ikinci sekme kaydetti, ilk sekmenin eski version'ı 409 aldı. **Güncel kaydı yükle** yeni katı gösterdi; tekrar düzenleme başarıyla kaydedildi. Kanıt: [unit-concurrency-conflict.png](screenshots/unit-concurrency-conflict.png).
- [x] **Responsive ve anonim SSR:** 390×844 mobil apartman listesi/formunda sayfa viewport'u aşmadı, iptal çalıştı. Anonim detay HTML'inde test apartmanının adı/adresi yoktu. Kanıt: [masaüstü](screenshots/apartment-desktop.png), [mobil](screenshots/apartment-mobile.png), [mobil form](screenshots/apartment-unit-form-mobile.png), [karma yapı](screenshots/mixed-building-desktop.png).
- [x] **Son otomatik sonuç:** `bash scripts/test.sh` **121 başarılı (17 unit + 104 integration), 0 başarısız, 0 atlanan**. `bash scripts/test-auth.sh` **17 başarılı**. `npm run typecheck` ve `npm run build` başarılı; canonical ve eski yönlendirme rotaları derlendi.
- [x] **Servis ve lisans:** `bash scripts/smoke.sh` web/API/live/OpenAPI, anonim/geçersiz JWT, Keycloak, Mailpit, Redis, RabbitMQ, MinIO, Grafana ve worker kontrollerini geçti. MinIO IPv4 `127.0.0.1:9000` üzerinden kontrol edilir; Java'nın IPv6 9000 servisi durdurulmadı. `python3 scripts/check-licenses.py` **262 paket sürümü** doğruladı; 03'te yeni paket/asset eklenmedi.
- [x] **Belge/ekran kaydı:** ana mimari, API sözleşmesi, migration, README, AGENTS, skill/agent durumu ve yol haritası güncellendi; 03 görevleri test kanıtlarıyla tikli ve koyu, sıradaki adım 04.

UI kabulü yalnızca bu turda oluşturulan geçici kayıtları, bölüm → bina → yönetim alanı sırasıyla API üzerinden **soft-delete** etti; audit geçmişi kalır, kullanıcının mevcut kaydı değişmedi. Ekran görüntüleri bu geçici örnekleri gösterir. Test wrapper ayrı hesabını temizledi; mevcut kullanıcının parolasını değiştirmedi.

**Kalan sınırlar:** Section/etap ve ayrı Floor entity/UI, ilişkili Property tür dönüşümü, üyelik atama ekranı, kişi/aidat/ödeme sonraki kapsamdır. floorCount metadata'dır; sıralı kat/daire üretmez. FE/API/worker Docker imajları bu fazda yeniden build edilmedi; host FE build ve .NET test build doğrulandı. Üretim SMTP/HTTPS, dağıtım pipeline ve imaj lisans/SBOM çalışmaları üstte planlıdır.

## 03 ek değişiklik — 2026-10-08: İlişkisiz yönetim alanında tür değiştirme, tamamlandı

Kullanıcı, ilişki kurulmamış yönetim alanının türünü düzenlemek istiyor. Bina/blok veya bağımsız bölüm geçmişi olmayan kayıtta dört tür arasında değişim açılır. Aktif, pasif veya soft-delete edilmiş alt kayıt ilişki sayılır; geçmiş taşınmaz. Oluşturanın PropertyMembership kaydı bu yapısal kısıta dahil değildir. SUPER_ADMIN ve version kontrolü korunur. Şema değişikliği yok; boş migration eklenmez.

- [x] **Domain:** boş kayıtta tür değişimi ve metadata, pasif bina/daire ilişkisi olan kayıtta ret; geçersiz tür/kat sayısında alanlar atomik olarak korundu. 4 yeni vaka, toplam 21 unit testi geçti.
- [x] **Gerçek PostgreSQL/API:** dört hedef türde değişim; ID/create audit/üyelik korundu, version arttı, yeni tipe uygun çocuk eklenebildi. Aktif/pasif/soft-delete edilmiş bina ve doğrudan daire geçmişi değişimi 409 ile engelledi; görünür sayılar sıfır olsa da canChangeType false kaldı. Aynı tür/atlanmış type ile normal düzenleme çalıştı.
- [x] **Negatif vakalar ve yarış:** boş/bilinmeyen/nümerik/küçük harf type 400; anonim 401, SITE_ADMIN/MANAGER/RESIDENT 403, eski version 409. Önce bina eklendiğinde stale tür düzenlemesi reddedildi; önce tür değiştiğinde stale doğrudan daire eklemesi concurrency ile rollback oldu ve yetim kayıt oluşmadı.
- [x] **Tarayıcı:** geçici boş Site → Apartman / 8 kat → Site değişimleri kaydedildi; sayfa yenilemeden Daireler / Binalar-Bloklar tablosu doğru değişti. Apartmandan çıkınca kat sayısı temizlendi. Bina eklenince tür seçimi açıklamayla kilitlendi; bina soft-delete sonrası da kilit korundu. Geçici bina ve üst kayıt soft-delete ile temizlendi; mevcut kullanıcı kaydı değişmedi. Kanıt: [düzenleme](screenshots/property-type-edit.png), [kaydedilmiş apartman](screenshots/property-type-after.png), [ilişki kilidi](screenshots/property-type-locked.png).

**Sonuç:** `bash scripts/test.sh` **146 başarılı (21 unit + 125 integration), 0 başarısız, 0 atlanan**. İlk denemede yeni test helper çağrıları derleme hatası verdi; düzeltildikten sonra tüm suite geçti. `npm run typecheck` / `npm run build` ve lisans kontrolü **262 paket sürümü** başarılı. Yeni API ile `bash scripts/test-auth.sh` **17 kontrol**, `bash scripts/smoke.sh` bütün servis/anonim/geçersiz JWT/worker kontrollerini geçti; geçici kimlik hesabı wrapper tarafından silindi. Yeni bağımlılık/asset/şema değişikliği yok; migration eklenmedi. API yeni kodla yerelde yeniden başlatıldı. Ana mimari, API sözleşmesi, AGENTS, README, skill/agent kaydı ve ekran yol haritası güncellendi.

## 2026-10-08 — Ayrı yönetim menüleri: doğrulanan senaryolar

- [x] **Genel bilgiler, Bina/Blok, Daire/Bağımsız Bölüm ve Kişiler ayrı sayfalardan yönetilir; apartmanda Bina/Blok menüsü yoktur.**
- [x] **Kişi–daire ilişkisi kişi listesinde iç içe açılmaz; ayrı kişi sayfasına gider. Site dairelerinde önce bina seçilir; sahte blok yaratılmaz.**
- [x] **Yeni canonical rotalar, doğrudan bağlantı ve OIDC dönüş whitelist'i doğrulanır; mevcut ve legacy rotalar korunur.**
- [x] **Menü aktif durumu, TR/EN, mobil görünüm, erişilemeyen kayıtta 404/yeniden deneme ve boş listeler tarayıcıda doğrulandı. Kişiler menüsü mevcut yönetici rol kümesini korur; negatif backend rol testleri önceki 04 kanıtıdır.**

## BAKIM — Sabit menüler, sayaç okumaları ve tüketim özeti (11.10.2026)

İlişkili İŞ10 kayıt13/14; yeni faz/görev paydası yok. **Önceden belirlenen kabul planı; aşağıdaki sonuçlar bu tur çalıştırıldı:**

- Sabit sol/üst/alt menü; uzun sayfada yalnız orta içerik kayar, son kayıt/eylem erişilebilir. Klavye scroll, içeriğe atla, rota değişimi ve aktif menü; mobilde erişilebilir menü paneli ve kapanış odağı.
- 1440px masaüstü, 820×1180 tablet ve390px mobil: ana belge yatay/dikey taşmaz; üst/alt çerçeve değişmez. Mobil tüm modüller menüden erişilir, kapsam/rol kuralları korunur.
- Okumalar sağ paneli geniş masaüstü/tablet, mobil tam genişlik; kapatma/ESC/odak ve aç/kaydetmeden vazgeç davranışı.
- Raporun tür bazlı tüketim toplamları, üç ondalık BigInt hassasiyeti (büyük toplamlar dahil), başlangıç hariç kuralı, TR/EN ve gider/fatura açıklaması; m³/kWh/TRY birbirine toplanmaz.
- FE test/typecheck/build, lisans kontrolü, backend/.env/manifest/lock/Next config koruma hash eşliği. Kullanıcı DB'sinde mutation yapılmaz.

**Sonuç:101/101 FE (hata/atlama0), `npm run typecheck`, `npm run build`, `python3 scripts/check-licenses.py`331 sürüm ve `git diff --check` geçti.** İki yeni report testi büyük sayı/üç ondalık/TR-EN ve tür/baseline/gider ayrımını korur. İlk build SWC kullanıcı önbelleği sandbox engeline takıldı; izinli tekrar ve son build başarılı. `next-env.d.ts` otomatik üretimi başlangıç hashine geri alındı; backend/.env/manifest/lock/kullanıcının Next farkları dahil196/196 korunan dosya eşleşti. Yeni bağımlılık yok. Bu FE değişikliğinde .NET testleri tekrar çalıştırılmadı; önceki464.NET bu tur sonucu değildir.

**Girişli gerçek Chrome kabulü:**1440×900 masaüstü sol248px, header86px, footer64px; ana scroll yüksekliği7039px olan uzun sayfada içerik496.5px kayarken header y0/footer y836/sidebar y0 sabit kaldı. Raporda orta scroll706.5px, aynı çerçeve korunur. Tablet820×1180 header86/footer64; mobil390×844 header105/footer64; belge viewport boyutunda, yatay taşma0. Mobil rapor scroll414.5→2029.5px, header y0/footer y780 değişmedi. Mobil menüde tüm modüller, yönetim alanı kapsamlı geçiş ve yeni rotada scroll0; rol tabanlı menü koşulları değişmedi. Skip-link odağı workspace-content; ESC odağı açan Okumalar düğmesine döndü. Okumalar masaüstü896×900,tablet820×1180,mobil390×844; iç yatay taşma0, mobil kartlar, form açıp kaydetmeden Vazgeç doğrulandı. TR/EN miktar/para/menü metinleri; canlı veri yalnız başlangıç okumaları içerdiğinden nonzero tüketim otomatik testiyle doğrulandı. Kullanıcı iş kayıtlarında mutation/DB yazımı yapılmadı. Gerçek Safari/iPadOS kabulü yapılmadı.

Ekran kanıtları: [Masaüstü sabit çerçeve ve rapor](screenshots/maintenance-fixed-frame-reports-desktop.png), [mobil sabit çerçeve](screenshots/maintenance-fixed-frame-mobile.png), [mobil sol menü](screenshots/maintenance-navigation-mobile.png), [896px Okumalar](screenshots/maintenance-readings-desktop.png), [tablet Okumalar](screenshots/maintenance-readings-tablet.png), [mobil Okumalar](screenshots/maintenance-readings-mobile.png).

Uygulama içi mevcut00 Shell ve10 Sayaçlar/Raporlar doğrulama metinleri aynı görev kimliğiyle güncellendi; yeni tik/faz/sayaç eklenmedi,83/83 korunur. Tüketim m³/kWh, giderTRY; tarife olmadığı için otomatik tüketim maliyeti eklenmedi. Faturanın mevcut Expense olarak kaydı gerçekleşme ayı giderine girer; aynı faturayı tekrar kaydetmeme açıklaması eklendi; fatura numarası tekilliği veya otomatik eşleştirme bu bakımda eklenmedi. Tek agent, ek skill yok.

## UI03 referans sol/üst menü — kabul planı (11.10.2026)

Profil açılır menüsünde mevcut Keycloak çıkışı, gerçek oturum adı/rolü; menü araması ve Cmd/Ctrl+K; masaüstü daraltma/mobil panel; TR/EN ve mevcut rol/kapsam bağlantıları. Test/typecheck/build çalıştırılacak. Görsel referanstaki fotoğraf ve bildirim sayısı gerçek veri olmadığından üretilmez.

## UI03 sol/üst menü teslimi — 12.10.2026

**Onaylanan dar kapsam 3/3 adım, %100.00 tamamlandı.** Koyu lacivert248px sol menü, mavi aktif satır,58px üst bar, menü arama dialogu, gerçek oturum adı/rolü ve profil içinde Keycloak çıkışı. Masaüstü daraltma; mobil mevcut sol Dialog ve sabit çerçeve korunur. Yeni paket/DB/API değişikliği yok; tek agent, ek skill yok.

**Bu tur101/101 FE, typecheck/build,331 lisans kontrolü geçti.** Tarayıcı profil menüsünde Çıkış yap görünür; aramada Rapor yalnız Raporlar sonucunu verdi;390×844 mobil panel tüm menüleri gösterdi, yatay taşma0 ve son header58px doğrulandı. Kullanıcının oturumu kapatılmadı; gerçek logout redirect bu tur yeniden çalıştırılmadı. Rol koşulları kod incelemesiyle korundu; ayrı rollerle tarayıcı kabulü yapılmadı. Backend testleri çalıştırılmadı. İlk son kontrol komutu yanlışlıkla kökten çalıştırıldığı için package.json bulunamadı; doğru frontend klasöründen tekrar başarılı.

Görsel birebir eşlik sınırlamaları: referans fotoğraf/logosu yerine Lucide bina işareti ve çizim; avatar gerçek oturum baş harfleri. Bildirim sayısı uydurulmaz; merkez henüz mevcut değildir. API'si bulunmayan menüler Planlandı açıklamalı disabled. Üst arama mevcut modülleri bulur, kişi/daire verilerinde global API araması değildir. UI03 geniş planın breadcrumb/global veri araması tamamlandı sayılmaz; yalnız kullanıcının bu tur istediği menü kapsamı teslim edilir. İş yol haritası83/83 %100.00 korunur; yeni görev/tik yok. UI toplam ilerlemesi BİLGİ MEVCUT DEĞİL.

Kanıt: [Profil ve menü](screenshots/ui03-navigation-profile.png). Sonraki adım: yeni kullanıcı talebi; diğer ekranlara geçilmedi.

Kota kaynağı Codex usage/rateLimitsByLimitId.codex, teslim öncesi okuma:300dk kullanılan%43/kalan%57, reset12.10.2026 04:04:09 Europe/Istanbul;10080dk kullanılan%38/kalan%62, reset18.10.2026 09:28:33. Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Son build tekrarında SWC native cache sandbox erişim engeli oluştu; izinli `npm run build` tekrarı uygulandı. İlk başarılı build kanıtı ayrı korunur. Son izinli build başarılı; son sözlük/erişilebilir ad değişikliklerinden sonra101/101 FE tekrar geçti.

## BAKIM bayraklı dil seçimi — 12.10.2026

01/03 inceleme tamamlandı,02/03 uygulama devam ediyor. Kabul: TR/EN bayrak+ad, uygulama login ve üst bar ortak seçim, Keycloak login dilinin aynı host üzerinde callback sonrası korunması, geçersiz tercihin güvenli fallback. Yeni paket yok; auth/token/yetki değişmez. Ayrı auth hostname için host cookie ortak değildir; bu yerel kurulum aynı hostname/farklı port kullanır.

## BAKIM bayraklı dil teslimi — 12.10.2026

**3/3 %100.00: LanguageSwitcher TR/EN bayraklı; /login ve ana ekran aynı HttpOnly uygulama dil cookie'sini kullanır. Keycloak tema menüsünde bayrak, script ile yalnız whitelist dil tercihi cookie'si; aynı hostname/farklı port yerel girişte dil ana ekrana taşınır.** Kimlik bilgisi/token/auth URL saklanmaz, auth politikası değişmedi. Ayrı auth hostname entegrasyonu bu host cookie mekanizmasıyla desteklenmez; dağıtımda ayrıca ele alınmalıdır.

**102/102 FE, typecheck/build,331 lisans ve git diff --check başarılı.** Tarayıcı /login English→ana ekran en; gerçek Keycloak giriş menüsünde English seçimi→ana ekran en doğrulandı. Kullanıcı parolası girilmedi/oturum kapatılmadı; yeniden parola gönderimli login testi yapılmadı. Test sonrası dil Türkçe'ye döndürüldü. Yeni paket/DB/backend yok; tek agent, skill yok. Çalışma ağacındaki önceki değişiklikler korundu. Geçersiz tercih için whitelist/fallback testi eklendi.

Dosyalar: LanguageSwitcher.tsx, i18n/{locale,actions,request}.ts, globals.css, standards.test.mjs; Keycloak template.ftl/theme.properties/resources/js/language-preference.js; TESTING ve progress kayıtları. İş yol haritası83/83 %100.00 korunur; UI toplam yüzdesi BİLGİ MEVCUT DEĞİL. Sonraki adım yeni kullanıcı kapsamı.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Teslim kota okuması12.10.2026 Codex usage/rateLimitsByLimitId.codex:300dk kullanılan%60/kalan%40 reset12.10.2026 04:04:09;10080dk kullanılan%41/kalan%59 reset18.10.2026 09:28:33 Europe/Istanbul. Ekran kanıtları docs/screenshots/language-keycloak-en.png ve language-workspace-en.png.

Son build sandbox SWC önbellek erişim engelinden sonra izinli tekrar ile başarılı tamamlandı.

## BAKIM yol haritası başlangıcı — 12.10.2026

01/02 inceleme ve uygulama tamamlandı; tüm details başlangıçta kapalı. Kabul: sayfa yenilemede açık bölüm0, tıklama ile aç/kapat korunur; typecheck/build. Yeni paket yok; görev83/83 sayacı değişmez.

## BAKIM yol haritası teslimi —12.10.2026

**2/2 %100.00 tamamlandı. Roadmap.tsx ilk00–03 bölümlerindeki varsayılan open kaldırıldı;15 bölüm kapalı başlangıç.** Tarayıcı yeni ana sayfada açık details0→tıklama1→kapatma0; ekran docs/screenshots/roadmap-collapsed.png. Typecheck ve build başarılı; build sandbox SWC önbellek engeli sonrası izinli tekrar geçti. Yeni test eklenmedi/npm test bu tek sunum değişikliğinde tekrar çalıştırılmadı; paket/lock değişmedi. Önceki kullanıcı değişiklikleri korunur. İş83/83 %100.00; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, skill yok. Sonraki işlem yeni talep. Giriş/çıkış/toplam token ve kalan context ayrı ayrı BİLGİ MEVCUT DEĞİL.

## BAKIM menü görselleri —12.10.2026

01/02 tamamlandı: sidebar dekoratif bina görseli kaldırıldı; Next devIndicators:false. Kabul: masaüstü/mobil dekoratif görsel yok, Next geliştirme düğmesi yok; typecheck/build. Paket değişikliği yok.

## BAKIM menü görselleri teslimi —12.10.2026

**2/2 %100.00 tamamlandı. WorkspaceShell.tsx alt bina dekorasyonu/House import kaldırıldı, globals.css kullanılmayan dekorasyon stili kaldırıldı, next.config.ts devIndicators:false.** Mevcut config ayarları korunur. Typecheck/build geçti; ilk typecheck dev restart sırasında rota tipleri eksik, ilk build SWC sandbox cache engeli: build izinli tekrar, typecheck tekrar başarılı. Tarayıcı dekorasyon0/Next dev düğmesi0; ekran docs/screenshots/sidebar-clean.png. Mobil aynı navigation JSX kaynağı, ayrı mobil kabul bu tur çalıştırılmadı. npm test bu görsel/config değişikliğinde tekrar çalıştırılmadı, yeni paket/lock değişimi yok. İş83/83 %100.00 korunur; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, ek skill yok. Sonraki işlem yeni kullanıcı talebi.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim12.10.2026:300dk kullanılan%73/kalan%27 reset12.10.2026 04:04:09;10080dk kullanılan%43/kalan%57 reset18.10.2026 09:28:33 Europe/Istanbul.

## BAKIM kişi profili ve iletişim —12.10.2026

Onaylı kapsam5 adım:1 inceleme tamamlandı;2 kişi modeli/migration,3 kişi listesi/form/detay,4 hesap eşleştirme/bildirim/arama/aidat geçmişi,5 gerçek PG ve FE kabul. Yeni yerli gerçek TC11, yerli tüzel VKN10; yabancı gerçek pasaport, yabancı tüzel ülke+kuruluş ID; telefon/email zorunlu. Eski kişi profilleri bilinmeyen/null korunur. SMS sağlayıcı bağlı olmadığı için kapalı; kişi aynı Property aktif giriş hesabına açıkça eşleştirilir. Test: dört kombinasyon ve eksikler, nullable/legacy/up/down/şema/version/negatif scope, mesajUUID retry/hesap alıcı izolasyonu, ilişkiler ve geçmiş kişi tahakkuk snapshot korunması, TR/EN/form/list/arama/typecheck/build.

## BAKIM kişi profili ve iletişim teslimi — 12.10.2026

**Onaylı kapsam 5/5 adım = %100.00 tamamlandı.** Mevcut İŞ04 (05/14), İŞ06.1 (08/14) ve İŞ09 (12/14) ile ilişkilidir. Yeni faz/backlog veya görev paydası oluşturulmadı; kişi görevinin açıklama ve kanıtı aynı kimlikle güncellendi. İş yol haritası **83/83 = %100.00**; bütün UI planının adım yüzdesi **BİLGİ MEVCUT DEĞİL**. Tek agent; ek skill veya alt agent yok.

- **Kişi profili:** gerçek/tüzel, yerli/yabancı; ülke, kombinasyona uygun kimlik, doğum tarihi, cinsiyet, adres. Yerli gerçek kişide T.C. numarası (11 rakam, ilk rakam 0 değil); yerli tüzelde vergi numarası (10 rakam); yabancı gerçek kişide pasaport; yabancı tüzelde ülke ve kuruluş kimlik numarası zorunlu. Telefon ve e-posta yeni POST/PUT'ta zorunlu. Tüzelde kurum adı kullanılır; doğum tarihi/cinsiyet null. Kullanıcı bu kapsamı, yabancı kuruluş alanlarını ve önceki TC toplamama kuralının değişmesini onayladı. Resmî kimlik doğrulama servisi yok; yalnız biçim kontrolü yapılır.
- **Liste ve detay:** gerçek daire, blok, site/apartman, kişi türü, ilişki rolü, uyruk, telefon, e-posta kolonları; birden fazla daire/rol birlikte gösterilir. İsim/şirket/telefon/e-posta sunucuda aranır. Legacy profil bilgileri null kalır, güncellemede kullanıcı eksikleri tamamlar. TanStack masaüstü tablo, tablet/mobil kart ve ortak sağ panel kullanılır.
- **İletişim:** aynı Property'deki aktif üyelik hesapları arama/sayfalama ile seçilir; hesap veya yetki oluşturulmaz. Kalıcı PersonMessage, başlık/mesaj, UUID/fingerprint replay, kişi version ve Property kilidi; alıcının JWT sub + aktif üyelik kapsamındaki bildirim kutusu ve okundu kaydı. SMS kullanıcı kararıyla sağlayıcı bağlanana kadar kapalı. Bildirim uygulama içidir; SMS, e-posta veya OS push değildir. Hesap seçenekleri mevcut subject kimliğini gösterir; Keycloak ad/directory entegrasyonu yok.
- **Arama/aidat:** `tel:` bağlantısı cihazın arama uygulamasını açar; bu tur çağrı başlatılmadı. Kişi aidat geçmişi DuesChargeParty snapshot filtresinden okunur. Legacy borç sonradan kişiye atanmaz; ACCOUNTANT kişi verisi yetkisi kazanmaz.
- **Persistence:** `20261011223446_PersonProfilesAndMessages` source/Designer/snapshot; people üzerinde 8 nullable yeni kolon, profil check, PersonMessage BaseEntity ve Property/Person composite FK + UUID index. Ortak sekiz NOT NULL kolon korunur. Profil veya mesaj geçmişi varsa Down ilk drop öncesinde reddedilir. Yerel update sonrası **22 eski tablonun mevcut alan/row/audit/version hashleri eşleşti**. Kimlik verisi backfill edilmedi; eski uygulanmış migration değiştirilmedi.

**Son otomatik sonuç: 481/481 .NET (109 unit + 372 integration), 104/104 FE; hata ve atlama 0.** Typecheck, production build, EF model/migration eşliği, 331 lisans ve `git diff --check` geçti. Yeni paket veya lock değişikliği yok; restore sonrası lisans kontrolü çalıştı. İlk tam backend çalışmasında 3 eski migration fixture testi başarısız oldu; gerçek migration adına bağlama ve eski Down sonrası model sorgusundan önce son şemayı geri yükleme düzeltildi, sonraki tam çalışma geçti. İlk geçici SDK derlemesi yerine kurulu .NET SDK kullanıldı; sandbox SWC önbellek engeli izinli build tekrarıyla çözüldü. Yanlış cwd'deki npm denemesi başarılı kontrol sayılmadı.

**Ayrı PostgreSQL kanıtı:** dört profil kombinasyonu, eksik kontak/kimlik/ülke, gelecekte DOB, yanlış hesap, version/ilişki/rol/scope; hesap seçenekleri, SMS reddi, aynı/farklı UUID içerikleri, alıcı izolasyonu, okundu ve üyelik iptali sonrası kutunun gizlenmesi. İlk kurulum/yükseltme/tekrar, boş Down/re-Up, dolu Down reddi, model ve bütün BaseEntity tablolarında kolon kontrolü geçti.

**Gerçek Chrome kabulü:** TR/EN alanları ve zorunlu telefon/e-posta; boş kayıtta 5 invalid alan ve ilk Ad odağı. Dört kombinasyonda doğru kimlik etiketi, tüzelde DOB/cinsiyetin gizlenmesi. 390×844 sağ panel genişliği 390, iç taşma 0; 820×1180 tablo sarmalayıcısı computed display `none`, kart görünümü, body taşma 0. Mesaj panelinde SMS açıklaması, arama bağlantısı, snapshot aidat metni; ESC kapanış. İngilizce bildirim kutusunun boş durumu yüklendi. Geçici sekmeler kapandı, viewport sıfırlandı ve dil Türkçe'ye döndürüldü.

Kullanıcının iş kayıtlarında form save veya mesaj gönderimi yapılmadı. Gerçek kişiye SMS/bildirim gönderilmedi; başarılı gönderim/teslim/readAt kanıtı ayrı fixture API testlerindedir. Tarayıcıda başarılı mesaj mutation'ı ve gerçek telefon görüşmesi kabulü yapılmış sayılmaz. Ekran kanıtları boş yeni formu gösterir; masaüstü görseli mevcut kişisel veri içeren arka planı dışarıda bırakır. API5005 `/health/live` 200 ve FE3003 çalışır. Docker uygulama imajları bu tur derlenmedi.

Değişen kaynaklar: Domain Person/PersonMessage; Application People/PersonMessages/ErrorCatalog; Infrastructure PeopleStore/PeopleConfigurations/PersonMessageStore/DbContext/DI ve üç migration dosyası; API PeopleEndpoints/Program. FE EntityEditor/RecordDetails/PeopleWorkspace/PersonPage/types/management-contracts/AuthGate; yeni PersonProfileDetails/PersonAccountPicker/PersonActionsPanel/NotificationInbox/messages; TR/EN, error registry ve mevcut roadmap kanıtı. Domain/API/migration/legacy fixture testleri ve FE person-profiles testi. AGENTS, core, people, MIGRATIONS, frontend-rules ve progress/test kayıtları güncellendi. Önceki kullanıcı farkları ve staging korundu; commit/push yapılmadı.

Kanıtlar: [API](api/people.md), [yabancı tüzel form](screenshots/person-foreign-legal-form.png), [mobil form](screenshots/person-profile-mobile.png). Sonraki işlem yeni kullanıcı kapsamıdır.

Son kota okuması: 12.10.2026 07:39:22 Europe/Istanbul, Codex usage / rateLimitsByLimitId.codex. 300 dk kullanılan %50/kalan %50, reset 12.10.2026 12:21:12; 10080 dk kullanılan %55/kalan %45, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**; çıkış token: **BİLGİ MEVCUT DEĞİL**; toplam token: **BİLGİ MEVCUT DEĞİL**; kalan context: **BİLGİ MEVCUT DEĞİL**; oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

## BAKIM görsel taslak — Yeni kişi formu (12.10.2026)

**2/2 %100.00 tamamlandı:** kullanıcının ekran görüntüsü incelendi; imagegen skill'i ve built-in image_gen ile iki sütunlu/gruplu sağ panel taslağı üretildi. Kimlik, iletişim, bildirim hesabı; son görselde hesap eşleştirmesi isteğe bağlı olarak düzeltildi. Başlık, zorunlu alanlar ve alt kaydet/vazgeç görsel olarak kontrol edildi. Görsel: docs/analysis/design/person-create-proposal-v1.png. Bu yalnız görsel öneridir; uygulama kodu değiştirilmedi, typecheck/build/API testleri bu görsel turunda yeniden çalıştırılmadı. Yeni paket yok, roadmap tıkları ve83/83 sayacı korunur. Sonraki adım kullanıcının görsel hakkındaki talebidir. Giriş token/çıkış token/toplam token/kalan context: her biri BİLGİ MEVCUT DEĞİL.

## BAKIM onaylı kişi formu görseli —12.10.2026

01/03 inceleme tamamlandı;02/03 uygulama devam ediyor. Kullanıcı iki sütunlu sağ panel görselini, cinsiyet ve uyruk ikonlarını onayladı. Konum entegrasyonu kullanıcı kararıyla sonraya bırakıldı; adres manuel, geolocation izin/policy veya dış geocoder değişikliği yapılmaz. Kabul:768px panel/390px tek sütun, gruplar/ikonlar, ülke adı+bayrak ve aynıISOcode, birleşik hesap arama/ESC/odak/sayfalama, dört kişi kombinasyonu/required/version/PUTkoruma,TR/EN,typecheck/build/mevcutFEtestler. Yeni paket/backend/DBdeğişikliği yok.


### 2026-10-12 — BAKIM / UI06 kişi formu tasarımı teslimi

**Tamamlandı: 3/3 adım, bu tur %100,00.** İş yol haritası 83/83 (%100,00), UI planı toplam 12 aşama; mevcut UI06 kapsamına sunum iyileştirmesi, yeni faz/tik eklenmedi. Kişi formu 768px gruplu iki sütun, mobilde tek sütun ve sabit eylem alanı; cinsiyet/uyruk ikonları, bayraklı ülke adları ve tek aranabilir hesap seçicisi eklendi. Konum entegrasyonu kullanıcının kararıyla sonraya bırakıldı, adres manuel. API/veritabanı/bağımlılık değişmedi.

**Kanıt:** typecheck başarılı; frontend 104/104 test başarılı; üretim derlemesi mobil CSS düzeltmesi sonrası tekrar başarılı; git diff --check başarılı; paket manifest/lock değişikliği yok. Chrome masaüstü ve390×844 mobil: dört kimlik varyantı, zorunlu alanlarda5 hata/ilk alana odak, hesap popover Escape/odak dönüşü doğrulandı. Mobil son ölçüm388px içerik/388px scrollWidth, footer görünür. Görseller docs/screenshots/person-form-design-desktop.png ve person-form-design-mobile.png. Gerçek kişi kaydı/mesaj gönderimi yapılmadı.

Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL. Codex hesap son okuma: 300dk kullanılan%93/kalan%7, reset12.10.2026 12:21:12 Europe/Istanbul; 10080dk kullanılan%62/kalan%38, reset18.10.2026 09:28:33. Hesap kotası bu turun token tüketimi değildir.

## PROTOKOL — AkaDev AI FAZ 03 ortak çekirdek — 12.10.2026

**Bu tur 4/4 adım = %100.00:** mevcut kaynak analizi, hazırlık/metadata kontrolü,
izinli kurulum, statik kabul/teslim. Yedi ortak Skill ve yedi repo içi Codex bağlantısı;
kök/frontend Claude import girişleri, salt okunur audit istisnası ve güncel katalog.
Tek Agent kullanıldı; ayrı Agent veya MCP kurulmadı.

**Statik kabul:** yedi YAML manifesti, ad/açıklama, proje içi referanslar ve bağlantı
hedefleri, Claude importları, izinli değişiklik kapsamı, kaynak/manifest/lockfile/CI/roadmap
SHA256 koruması ve git diff --check doğrulandı. PyYAML mevcut olmadığından hazır
quick_validate.py kullanılamadı; YAML Ruby/Psych ile ayrıştırıldı.
Uygulama testleri, restore/install, servis/DB ve runtime keşif/tetikleme denenmedi;
geçmiş 481.NET/104FE sonuçları bu tur yeniden çalıştırılmış sayılmaz.
Ürün/UI yol haritası ve resmi iş paydası korunur. FAZ 04 başlatılmadı.
Token/context ölçümleri: BİLGİ MEVCUT DEĞİL; hesap kotası tur tüketimi değildir.

## BAKIM: ortak PostgreSQL 18.6 — 12.10.2026

Tek bağımsız `../postgres-18.6` konteyneri, `projects` veritabanı,
`buildingflow` uygulama şeması ve `buildingflow_keycloak` kimlik şeması.
Yerel host `localhost:5436`; Docker host `postgres-18.6:5432`.
Bağlantı Search Path ile şema seçer; uygulanmış EF migration dosyaları korunur.
API/Worker/migration/Keycloak harici `shared-postgres-net` ağına katılır.
Yerel Compose PostgreSQL oluşturmaz; eski volume’ler geri dönüş için saklanır.
Yeni ürün fazı veya roadmap tıkı eklenmedi. Doğrulama sonuçları ortak README’de.

Ortak DB bakım kabulü:4/4=%100.00 (bağımsız kurulum,412 tablo veri koruması,
üç proje bağlantıları,gerçek doğrulama). PostgreSQL18.6 üzerinde372 integration
+109unit=481test, hata/atlama0;331lisans kontrolü geçti. Test DB/rol temizlendi.
API /health200; üç Keycloak realm discovery200. TraceFlow ve AkaAI güncel
Java paketleri derlendi. Eski PGvolume'leri korunur; yeni şema yazımları nedeniyle
geri dönüşte kesim sonrası değişiklikler ayrıca korunmalıdır. Yeni ürün fazı/tik yok.
Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL.

## Ortak Keycloak yerel bakım teslimi — 13.10.2026

Tek `keycloak-26.7.5` sunucusu ve `projects.keycloak` şeması; üç realm ve
özgün kullanıcı/istemci/rol kimlikleri korunur. Ortak yönetim localhost8080;
TraceFlow mobil issuer güncel LAN192.168.2.95:8080 üzerinden.
Eski Keycloak konteynerleri/şemaları geri dönüş için korunur. Uygulama
şemaları değişmez. Kaynak ve ayrıntılı operasyon/test kanıtı:
`../keycloak-26.7.5/README.md` (AkaAI kökünden ../../keycloak-26.7.5).
LDAP/federationID, DB rol izolasyonu, üç API için kendi token kabulü ve
iki diğer realm token retleri; özel reset SPI anonim401/kendi204/diğer403.
BuildingFlow17gerçek auth kontrolü, AkaAI10yerel/token testi,35frontend
auth/güvenlik testi ve TypeScript geçti. Tam AkaAI frontend166/167;
mevcut İngilizce compact sayı testinde1.3M/1.3m farkı kaydedildi.
Test fixture'ları temizlendi. Domain/roadmap ve on-prem AkaAI değişmedi.

## BAKIM — TraceFlow Gateway / MinIO port ayrımı — 13.10.2026

TraceFlow Gateway9000 ile çakışan MinIO host portu9030'a taşındı.
.env/.env.example ve Compose fallback hizalandı; Docker içi minio:9000
bağlantısı ve mevcut veri volume'u korunur. README ve iletişim API rehberi
güncellendi. GEÇTİ: MinIO9030/minio/health/live200, BuildingFlow5005/health200,
TraceFlow9000/actuator/health200 (IPv4 ve localhost). 9000 yalnız Java Gateway
dinliyor; MinIO9030 Docker eşlemesi doğrulandı. TraceFlow gerçek Chrome
S256 PKCE parola girişi ve originalclient code exchange geçti. IntelliJ
servislerinde eski Keycloak IP ayarı bulundu; yeniden başlatma kullanıcıda
bekliyor, mevcut API token401 sonucu başarı sayılmadı. Ürün fazı/yol haritası değişmedi; yeni bağımlılık yok.


## MOBİL Faz 04 — 13.10.2026

Kullanıcı onayıyla ayrı mobil uygulamaya OIDC/PKCE + SecureStore ve hesap/rol/mülk bağlamı. Yeni GET /api/v1/mobile/session; mevcut Person.LinkedUserId/PropertyMembership/UnitOccupancy okunur, şema/migration yok. Keycloak yalnız buildingflow-mobile public client; basic scope ve API audience, S256 zorunlu. Development issuer explicit allowlist, Production kuralları korunur. Ana API5005 durdurulmadı; mobil geliştirmeAPI5006 ayrı süreç. Mevcut kullanıcı parolaları/iş verileri değişmedi.
Bu tur45/45 seçili integration, hata/atlama0; full regresyon yeniden çalıştırılmadı. Mobil TypeScript/lint8/8, üç platform export, ExpoDoctor21/21; backend331 lisans. Chrome geçici fixture PKCE giriş/API session/profil/çıkış ve yenileme kontrolü. Native cihaz/Keychain kabulü yapılmadı; ExpoGo OAuth desteklemez, development build gerekir. Önceki npm28 audit uyarısı açık. Ürün83/83/UI roadmap sayacı değişmedi; mobil plan buildingflow-mobile-prompts/PHASES.md'dir. Faz05 başlatılmadı; token tüketimi/kalan context BİLGİ MEVCUT DEĞİL.
Sözleşme docs/api/mobile-session.md; ayrıntı ayrı mobil docs/mobile/PHASE04.md.


### Mobil Faz04 devam — kullanıcı görsel referansı / native build
13.10.2026: kullanıcı BuildingFlow Mobil Uygulama Tanıtımı.png referansını tüm UI fazları için onayladı. Ayrı mobil DESIGN_REFERENCE.md + AGENTS güncellendi; login lacivert hero/tipografi/Ionicons, alt menu çizgi ikonlarına geçti. TypeScript/lint8/8 ve üç platform export başarılı;690 mobil üst seviye lisans metadata belirsiz0. Geçici fixture remote logout sonrası uygulama login'e döndü; girişte Vazgeç popup'ı kapattı. Fixture hesabı ve yerel credential dosyaları temizlendi.
iOS development build0hata/1Expo-script-uyarısı ile iPhone17Pro simulator'a kuruldu/açıldı; bu oturumdaki CUA Simulator'a erişemediği için native OAuth/Keychain görsel kabulü DOĞRULANAMADI. Fiziksel/Android kabulü açık, mağaza yayını yok. LAN104→6 değişimi gitignored mobil env ve ayrıAPI5006 issuer listesine uygulandı, Metro8081devclient açık. Ana API5005 ve iş/UI roadmap korunur; Faz05 başlamadı. Ayrıntı ayrı mobil docs/mobile/PHASE04.md.

## MOBİL Faz06 — 14.10.2026

Kullanıcı mobil API önkoşulu eksiğinin backend'de tamamlanmasını onayladı. Yeni mobile/sync command/pull, actor-scope idempotence receipts, version409 ve tombstone; migration20261014062447_MobileRequestSync yeni iki tabloyla yerel dev DB'ye uygulandı. Existing domain/UI83/83 sayacı değiştirilmedi. Mobile talep formu/photo sonraki Faz12'de; yalnız altyapı ve profil sync paneli eklendi.

Bu tur52/52 seçili integration,0atlama; backend build0hata/0uyarı ve EF model check geçti. Mobil26/26, TypeScript/lint/üç platform export; iPhone şifreli SQLite nativeQueueReplay fixturetrue (HTTP simülasyonu, live business write yok). API5006/Metro8081 güncel LAN192.168.2.95; test modu kapalı. Main API5005 korunur. Gerçek uçak modu/soğuk açılış ve Android native UI kabulü açık. Yeni dependency yok; önceki npm28audit uyarısı devam ediyor. Kapsam docs/api/mobile-sync.md ve ayrı mobil docs/mobile/PHASE06.md. Commit/push/yayın yok. Token tüketimi/kalan bütçe BİLGİ MEVCUT DEĞİL.


## BAKIM — ortak BuildingFlow marka simgesi — 14.10.2026
Kullanıcı referansındaki üç bina işareti SVG olarak yeniden çizildi. WorkspaceShell markası yeni BuildingFlowMark bileşeni ve src/brand/buildingflow-mark.json kullanır; public/çıkış ve Keycloak giriş teması aynı buildingflow-logo.svg çıktısını kullanır. Mobil canonical kaynak buildingflow-mobile/mobile/assets/brand/mark.json; marka değişikliklerinde merkez aynası aynı üretimden yenilenir. Eski PNG varlıkları korunur; yeni bağımlılık yok.
Bu bakım3/3=%100: referans/varlıklar, kod bağlantıları, doğrulama/belge. Merkez typecheck ve Next production build geçti; ayrı127.0.0.1:3004 önizlemede SVG complete=true. Ana3003 Docker web yeniden dağıtılmadı. Mobil26/26 test, TypeScript/lint, iOS build/install/launch ve üç platform export; ekran kanıtları buildingflow-mobile/docs/mobile/brand-*.png. Fiziksel telefon görsel kabulü ve Android runtime henüz yok. Backend iş testleri bu marka bakımı nedeniyle yeniden çalıştırılmadı. İş/UI yol haritası değişmedi, Mobil Faz07 başlatılmadı. Ayrıntı kardeş mobil repo docs/mobile/BRAND.md.
Token/context: BİLGİ MEVCUT DEĞİL. Son Codex usage okuması14.10.2026:300dk kullanılan%12/kalan%88;10080dk kullanılan%64/kalan%36. Hesap kotası bu turun token tüketimi değildir.


## BAKIM — MinIO yerine SeaweedFS4.48 — 14.10.2026
Tamamlandı3/3=%100: kaynak/lisans ve veri incelemesi; S3/servis/yedek refactoru; gerçek test ve yerel geçiş. API/worker/web ve mobil API yeni depoya bağlı; MinIO SDK/konteyneri kaldırıldı. Legacy volume/0600 kaynak ayarı korunur; kaynakta0nesne.96/96seçili.NET,104/104FE,8/8izolebackup/restore;24negatifpreflight;329lisans metadata; Dockerbuild/smoke başarılı. Aynı servis datasetlerinde gerçek kullanıcı dosyası/mesaj yazılmadı; yeniDBmigration yok. Kanıt SEAWEEDFS_MIGRATION.md ve artifacts/storage/migration-report.json. Faz07fotoğrafAPI/UI henüz tamamlanmadı; kullanıcı onaylı akış mobilPHASE07.md'de, önce bu refactor istendi. İş/UI roadmap paydası korunur.
Token/context: BİLGİ MEVCUT DEĞİL. Son Codexusage okumasında300dk kullanılan%84/kalan%16,10080dk%75/kalan%25; hesap kotası bu turun token tüketimi değildir.


## MOBİL Faz 07 — profil ve merkezi fotoğraf — 14.10.2026
Kullanıcı onaylı profil düzenleme, isteğe bağlı kamera/galeri/kırpma/önizleme ve kaldırma uygulandı. Merkez own-account API; özel SeaweedFS4.48 upload/download, SHA256, EXIF metadata temizliği, UUID makbuzu/version yarış koruması. Yeni iki BaseEntity tablo ve composite FK/Down veri koruması; migration20261014162556 yerelde uygulandı. Mobil SQLCipher schema4 metadata ve AES-GCM fotoğraf/komut dosyaları; sabitUUID retry,403purge/409conflict, makbuz sonrası indirme hatası tekrar yazım üretmez. Onaylı hukuki metin yok; teknik tercih rıza sayılmadı.
Bu tur27/27 seçili gerçek PostgreSQL/SeaweedFS integration test,0atlama; mobil36/36/lint/typecheck/üç platform export. iPhone build0hata/1toolchainuyarı; install/launch ve SQLCipher/AES-GCM dosya round-trip/AAD/yanlış anahtar/cleanup fixture geçti. Test modu kapatıldı. Kullanıcı fotoğrafı/canlı profil yazımı veya görsel inceleme yapılmadı (kullanıcı bakmana gerek yok dedi). Merkez API/worker/web Dockerbuild/deploy ve5005/5006/8081/3003-login200;329lisansmetadata. Önceki signed-out UI iyileştirmesi bu turda dağıtıldı;104/104FE testi önceki UI turunda geçmişti.
Faz08 başlatılmadı; gerçek kamera/galeri/uçak modu ve Android native UI kabulü açık. Ayrıntı docs/api/mobile-profile.md ve mobil docs/mobile/PHASE07.md. İş/UI roadmap sayacı değişmedi; commit/push/yayın yok. Token tüketimi/kalan context BİLGİ MEVCUT DEĞİL.


## MOBİL Faz08 — Ev sahibi — 14.10.2026
Kullanıcı onayıyla own-unit owner snapshot API ve mobil ana sayfa/Borçlar/Duyurular/Talepler bağlandı. Aktif linkedPerson/OWNEROccupancy/üyelik/property/unit/building; yönetici claim’i bypass değildir. Minimal kiracı adı/ilişki tarihleri; sözleşme/rent modeli bulunmadığı açıkça gösterilir. UNIT+own-owner borçlar; tenant/başkamalik kişiselborcu yok. Duyuru PUBLISHED/ALL_MEMBERS, talepler own-account/OWNER/unit. Sınırlar50charge/10tenant/20duyuru-talep, morebayrakları; totals tüm yetkili satırlar. Yeni backend entity/migration yok; mobil SQLCipher schema5scope/TTL/403purge,4→5profil taslağı koruması.
Bu tur59/59 seçili gerçek izolePostgreSQL integration,0atlama;41/41mobil, lint/typecheck/üç platformexport; build0hata/0uyarı. API/workerDockerbuild/deploy;5005/5006health200,anonimowner401,Expo8081status200. Yeni dependency yok. Görsel/gerçek telefonuçakmodu/Androidruntime kontrolü bu tur yapılmadı; gerçek iş kaydı/OWNER rol ataması yok. Faz09başlatılmadı; ödeme/tahsilat işlem geçmişi11,talepformu12kapsamında. İŞ/UI roadmap sayacı/commit/push/yayın yok. API docs/api/mobile-owner.md ve ayrı mobilPHASE08.md. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## MOBİL Faz09 — Kiracı — 14.10.2026
Kullanıcı onaylı Kiracı ana/finans/duyuru/talep ekranları: ortak owner snapshot servisinin roleTENANTscope’u; yalnız linkedownTenant/aktifOccupancy+üyelik/unit/property/building. UNIT+ownTenantborç; owner/coTenantkişiselborç hariç. Minimal Allocationtarih/tutar/terslenme,bankref/payerPII yok. Kira/kontrat/onlineödeme modeli yokluğu dürüstçe gösterilir. Native1–2000metinli talep, mevcut Faz06SQLCipherqueue/stableUUID/idempotentAPI/SyncPanel; webreadonly, fotoğraf/kategori12kapsamında. Schema5user/unit/rolecache/generationguard/24h/403purge. Merkezi migration/dependency yok.
Bu tur63/63izolePostgreSQLintegration,45/45mobil,lint/typecheck/üçplatformexport; .NETbuild0hata/0uyarı. API/workerDockerbuild/deploy;5005/5006health200,anonimtenant401,Metro200. Kişisel canlıtalep/borç/rol yazımı, görsel ve fizikselUI/uçakmodu kontrolü yok. Faz10başlatılmadı; Commit/push/yayın ve İŞ/UIroadmap sayacı korunur. Sözleşme docs/api/mobile-tenant.md; ayrı mobilPHASE09.md. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## BAKIM — BuildingFlow klasör ve yol adı düzeltmesi — 15.10.2026
Kullanıcı klasör ve tüm ilgili yerel proje yollarını düzeltmeyi açıkça onayladı. Merkez kökü /Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow. Mobil başlatıcı, IDE module/modules.xml, root lock metadata, README/analiz bağlantıları, mobilFaz08–09merkez referansları, sharedPostgreSQL prepare-env ve Keycloak compose/theme sync kaynak yolları güncellendi. IntelliJ eski proje penceresi kapatılıp yeni BuildingFlow penceresi açıldı; yeniden üretilen eski IDE-only ayarı artifacts/ide-rename-backup altında korundu. Kaynak/Git değişiklikleri taşındı; reset/commit/push veya işverisi/rol/parola değişikliği yok. Mevcut gerçek GitHub remote hedefi yeniden adlandırılmadı.
Yeni yolda .NETbuild0hata/0uyarı, mobileTypeScript ve shell/Python syntax geçti. Merkez5005/mobil5006health,web3003/login,KeycloakrealmOIDC veExpo8081status200. Docker veri hacmi adları önce/sonra aynı; API/worker/web/SeaweedFSgateway/core/rabbitmq/mailpit yeni compose yoluyla ayağa kaldırıldı. Keycloak tema mount yeni yola taşındı; yönetici/parola bootstrap çalıştırılmadı. Yeni ürün fazı başlamadı; iş testleri isim/yol bakımı için yeniden çalıştırılmadı. Codex proje etiketi/kayıt yolu için desteklenen güncelleme aracı yok; Codex UI otomasyonu güvenlik nedeniyle kapalıdır. Kullanıcı yeni klasörü Codex proje kaydında açmalıdır. Token/kalancontext BİLGİ MEVCUT DEĞİL.


## Mobil Faz10 — Yönetici deneyimi — 15.10.2026

Kullanıcı sıradaki mobil fazı onayladı.4işpaketi: kapsam/yetki,API+mobil,hedefli doğrulama,belgeleme/çalıştırma. Kod ve otomatik doğrulama teslim edildi; gerçek iPhone/Android küçük-büyük ekran/rol/offline kabulü açık (In review). Merkezin ayrı İŞ/UI/AI sayacı değişmedi; Faz11başlatılmadı.

MobileManager application/store/endpoint; gerçek rol+Property aktif üyelik (SUPER_ADMIN mevcut global scope), RepeatableRead,50satırlık deterministic pagination. Bina/daire/aktif ilişki, tüm bina borç/gecikmiş toplamı, duyuru/talep özetleri; PII-minimal sakin listesi, ACCOUNTANT'a kişi/yönetim verisi açılmaz. Mobil yönetici hero/kartlar/sekme listeleri, context/sectionsonsayfa ayrımlı24saatşifrelicache,403purge/late-responseguard. Readonly kritik kapsam; finans/duyuru/talep mutation yok.

Bu tur48/48mobil;20/20gerçekayrıPG Manager/Owner/Tenant/Session integration0skip; geçiciDB/roletemizlendi. TypeScript başarılı,lint0hata1öncekiuseOwnercleanupuyarısı,3platformexport,merkezbuild0hata0uyarı. Yeni dependency/migration/nativebinary yok; canlı kişisel/finans/rol mutation,commit/push/mağaza yayını yok. Mobil API5006/Metro8081 yeni kodla çalıştırıldı. Önceki kullanıcı talimatıyla görsel inceleme yapılmadı. Detay docs/api/mobile-manager.md ve kardeş mobil docs/mobile/PHASE10.md. Token/context/tur tüketimi:BİLGİ MEVCUT DEĞİL.


## Mobil Faz11 — Aidat/borç/işlem geçmişi — 15.10.2026

Kullanıcı Faz11'i onayladı. Bu tur4işpaketi: finans sözleşmesi,API+mobil,hedefli doğrulama,belge/çalıştırma. Kod ve otomatik kontroller teslim edildi; cihaz/UI kabulü ve online sağlayıcı açık (In review). Faz12başlamadı; merkezin ayrı İŞ/UI/AI sayacı değişmedi.

MobileFinance DTO/query/store/endpoint; OWNER/TENANT gerçek ilişki+aktif üyelik/parent+ownrole/person/UNIT borç filtresi; yönetici/muhasebe gerçek JWTrolü+Property kapsamı.50kayıtsayfalama,chargeId yetki kontrolü,repeatableread,tüm dönem summary ve minimal allocation/reversal geçmişi. PII/banka/ödeme toplamı dönmez. Online ödeme sağlayıcısı mevcut değil; ödeme başlatma/mutation veya başarı üretilmedi.

Mobil ortak finans ekranı,detay/geçmiş geçişi,paraBigInt/ortakparseCharge,24saat/sonsayfa/detailcache,403purge,geçyanıtguard. Yönetici+finans ortak useScopedRead. Yeni dependency/migration/nativebinary yok; canlı finans/rol/kişisel veri yazımı ve commit/push/yayın yok.

Bu tur54/54mobil;42/42gerçekizolePGintegration(MobileFinance6+MobileManager/Owner/Tenant/Session+PaymentApi),8/8PaymentDomain0skip; geçiciDB/roletemizlendi. TypeScript,lint0hata1öncekiuyarı,3platformexport,merkezbuild0hata0uyarı;7GitHubscript testi. Görsel/gerçektelefonofflinekabulü yapılmadı. Kanıt docs/api/mobile-finance.md ve kardeş mobil docs/mobile/PHASE11.md. Token/context/tur tüketimi:BİLGİ MEVCUT DEĞİL.

## Mobil Faz12 — Talep/arıza, fotoğraf ekleri ve offline outbox — 15.10.2026

MobileRequest kategori/öncelik, MobileRequestPhoto (STAGED→CURRENT→RETIRED→PURGED), fotoğraf yükleme/indirme endpoint'leri ve bakım; owner snapshot'ta kategori/öncelik/fotoğraf sayısı. Mobil şifreli taslak, fotoğraf işi kuyruğu (üst komut gönderilmeden claim edilmez), OWNER+TENANT formu ve eşitleme paneli.

Bu tur: **109/109 unit, 401 integration geçti / 22 atlandı** (ayrı geçici PostgreSQL 18.6 + SeaweedFS 4.48 S3; atlananlar ortamda olmayan Keycloak/RabbitMQ vb. fixture'lar). Yeni: MobileRequestPhotoTests 4 (kategori/legacy/DELETE doğrulaması; fotoğraf biçim/413/scope/ACCOUNTANT/anonim; eşzamanlı replay, 3 sınırı, yabancı erişim, silme ve purge; üyelik iptali) ve MobileRequestMigrationTests 1 (backfill, check, composite FK, dolu Down reddi). MobileTenantTests owner snapshot metadata doğrulaması. Mobil **62/62** (yeni requests.test.mjs 8), TypeScript, lint 0 hata/1 önceki uyarı, üç platform export. Gerçek cihaz uçak modu/kamera kabulü yapılmadı. Ayrıntı docs/api/mobile-sync.md ve mobile/docs/mobile/PHASE12.md. Token/context tüketimi: BİLGİ MEVCUT DEĞİL.
