## TEDARİKÇİ FAZ10 — 30.10.2026 — TAMAMLANDI

- Tedarikçi uyarıları (sözleşme, SLA, bakım) ve kullanıcı tercihleri (`SupplierAlertPreferences`); performans raporu.
- Tedarikçiler ekranına ikonlu Uyarılar/Performans görünümleri, CSV ve yazdır/PDF; yol haritası SUP-10.

## TEDARİKÇİ FAZ09 — 30.10.2026 — TAMAMLANDI

- Backend: mülk bazlı tedarikçi iş emri listesi ve saha notu (`SupplierWorkOrderNotes`).
- Mobil: "Tedarikçi işleri" ekranı, SQLCipher şifreli kopya ve işlem kuyruğu (şema 10), şifreli fotoğraf dosyası, çakışma/yeniden deneme/temizlik; yol haritası SUP-09.

## TEDARİKÇİ FAZ08 — 30.10.2026 — TAMAMLANDI

- Tedarikçi faturası, mükerrer numara engeli, onayda Gider kaydı, ödendi/mutabakat, gerekçeli iptal/düzeltme ve audit (`20261030064630_SupplierInvoices`); faturaya bağlı gider Gider ekranından iptal edilemez.
- Tedarikçi detayında ikonlu "Faturalar" sekmesi; yol haritası SUP-08.

## TEDARİKÇİ FAZ07 — 30.10.2026 — TAMAMLANDI

- Tedarikçi periyodik bakım planı, tenant tatil takvimi, iş günü kaydırma ve çakışma kontrolü, takvim/hatırlatma, tamamlanma kanıtı ve gecikme (`20261030061301_SupplierMaintenanceSchedules`).
- Tedarikçi detayında ikonlu "Bakım planları" sekmesi; yol haritası SUP-07.

## TEDARİKÇİ FAZ06 — 30.10.2026 — TAMAMLANDI

- Talep/arıza ve bakım planından iş emri; durum makinesi, SLA, gerekçeli red/iptal, özel fotoğraf kanıtı, SITE_ADMIN doğrulama/kapanış ve immutable geçmiş (`20261030052649_SupplierWorkOrders`).
- Supplier entegrasyon testleri tam route metniyle yazıldı; ilan listesi, form challenge ve birincil alan adı için anonim HTTP testi eklendi; kapsam tabanı 230→190.
- Yol haritası SUP-06.

## TEDARİKÇİ FAZ05 — 30.10.2026 — TAMAMLANDI

**#95; resmi2/2=%100,00; akış6/13=%46,15.** Çoklu davet/teklif, kesin KDV ve TRY/USD/EUR ayrı karşılaştırma; ilk girenden ayrı alan yetkili onay/red, immutable audit, normalize UUID/version/kilit, tek gerçek DRAFT iş emri. Son koşum **Integration63/63+Unit21/21**, önceki geniş142/142+30/30; **web322/322+typecheck+production build**, EF model/snapshot aynı, lisans329 PASS. Gerçek React/sentetik API workflow ve375/768/masaüstü taşma0; canlı OIDC/API browserE2E değildir. Beş tablo migration canlıya uygulanmadı. Rapor `docs/suppliers/05-report.md`; kaynak `docs/api/suppliers.md`. Yeni paket/commit/push/deploy yok; diğer QA/güvenlik çalışması ve kullanıcı verisi korundu. Sonraki FAZ06 mevcut kullanıcı yetkisiyle. Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL.

Ürün yol haritası:37 kayıt,157/168=%93,45. Codex usage/rateLimitsByLimitId.codex30.10.2026 okuması:300dk kullanılan%5/kalan%95,reset30.10.2026 13:12:31;10080dk kullanılan%63/kalan%37,reset04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.


## 29.10.2026 — Tedarikçi FAZ04

Özel belge/revizyon ve sözleşme API/UI, SupplierDocumentsContracts migration, immutable geçmiş ve mahremiyet koruması eklendi. Test koşumunda yerel QaCenter bayrağı temizliği düzeltildi; .env korunur. [Kabul kanıtı](../suppliers/04-report.md).

## 29.10.2026 — Tedarikçi FAZ03

SupplierAreaServices migration;çoklu alan/hizmet ve kategori API/UI;yetkili alan filtreleme,atama/version/UUID ve doluDown koruması. EF ilişki sorgusu ve roadmap çeviri namespace düzeltildi.[Kanıt](../suppliers/03-report.md).

## 28.10.2026 — Tedarikçi FAZ02

Yetkili workspace API ve `/suppliers` liste/form/detay,ikonlu durum,TR/EN,OIDC dönüşü.409 tekbildirim ve panelodak düzeltildi. [Kanıt](../suppliers/02-report.md).

## 28.10.2026 — Tedarikçi FAZ01

Tenant katalog ve7BaseEntity tablo, SupplierFoundation eklemeli migration, yönetici scoped API, version/UUID replay, iki hata kodu TR/EN. Integration29+Unit2 ve web301/typecheck/build doğrulandı; yeni bağımlılık yok. [Rapor](../suppliers/01-report.md).

## BAKIM main Otopark — onaylı yerel devreye alma — 28.10.2026

Kullanıcı dört migration ve API yeniden başlatmayı açıkça onayladı. **ADIM03/03 TAMAMLANDI; bakım3/3=%100,00.** Yerel main/.env DB üzerinde EF Core `database update 20261027205401_ParkingRetentionRedaction --no-build` başarılı: ParkingFoundation, ParkingRequestIds, ParkingMaintenanceClosures, ParkingRetentionRedaction uygulandı. Son `migrations list --no-build`: **bekleyen0**. Yalnız bu projeye ait API güvenli `api-stop` ile durduruldu ve standart `bash scripts/dev.sh api` ile başlatıldı. **localhost:5005/health ve /health/live HTTP200; anonim parking/policy HTTP401.** Chrome gerçek localhost:3003 oturumunda Araçlar/Otopark etkin link ve property parking rotası görüldü. Sonrasında kullanıcı başka pencereye geçtiği için özel oturumda parking içerik etkileşimi yapılmadı; veri yazan E2E bu tur çalıştırılmadı. Önceki559 test bu tur tekrar çalışmış sayılmaz. Yeni paket/kod/commit/push yok; mevcut hesap parolası ve kullanıcı kayıtları değiştirilmedi.

Kanıt: `/tmp/bf-main-parking-migrate.log`, `/tmp/bf-main-parking-post-migration.log`, `/tmp/bf-main-parking-api.log`. Önceki BEKLIYOR kaydı tarihsel; bu onayla runtime adımı tamamlandı. Ürün yol haritası32kayıt152/163=%93,25; ürün fazı/yayın kabulü değişmedi. Sonraki: kullanıcı localhost:3003 menüsünden Otopark ekranını kullanabilir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex 28.10.2026:300dk kullanılan%99/kalan%1 reset28.10.2026 16:43:32;10080dk%15/%85 reset04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM — main Otopark entegrasyonu — 28.10.2026

Kullanıcı isteğiyle çalışma gerçek yerel `main` dalına taşındı. Otopark backend/web/mobil kodu mevcut Ortak Alanlar ile birleştirildi; navigation/routes/OIDC dönüş kapsamı, servis ve endpoint kayıtları, AuthProvider/cache ve EF snapshot birlikte korundu. Git indeksine bu işlemde add/reset yapılmadı; mevcut kullanıcı değişiklikleri korunur. Yeni bağımlılık eklenmedi; parking kaynak dalındaki aynı node-forge sürüm düzeltmesi taşındı.

**Adım 1/3 kaynak birleştirme TAMAMLANDI; adım 2/3 doğrulama TAMAMLANDI; adım 3/3 yerel runtime BEKLIYOR. Tur 2/3=%66,67.** BAKIM ürün fazı değildir, faz sayaçlarına eklenmez. Birleşik ürün yol haritası 32 kayıt,152/163=%93,25; bu bakım nedeniyle yeni görev işaretlenmedi. **Main .NET build 0 hata/0 uyarı; EF pending-model kontrolü başarılı; web typecheck +296/296 test + üretim build; mobil check (typecheck/lint/160 test); ayrı geçici PostgreSQL'de Otopark/Ortak Alanlar 71/71 entegrasyon +32/32 unit başarılı, atlanan yok.** Test DB/rolü temizlendi. Kanıtlar `/tmp/bf-main-parking-*` log ve TRX dosyaları; önceki dal testleri bu tur çalıştırılmış sayılmadı.

Yerel DB read-only migration sorgusu: CommonArea dört migration uygulanmış, ParkingFoundation/ParkingRequestIds/ParkingMaintenanceClosures/ParkingRetentionRedaction bekliyor. Gerçek DB değişmedi, API yeniden başlatılmadı; tarayıcıda gerçek main oturumuyla uçtan uca kullanım doğrulanmadı. Menü kaynakta artık etkin bağlantı; kullanıcıdaki çalışan API'nin hazır olduğu iddia edilmez. Plan MASTER_PROMPT madde10 gereği yerel migration onayı son adımda istenir. Commit/push/deploy yok.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex 28.10.2026: 300dk %93 kullanılan/%7 kalan, reset 28.10.2026 16:43:32;10080dk %14/%86, reset 04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## 27.10.2026 — OA-07 yönetici takvimi ve raporlar

Yönetici dönem takvimi/sayfalı liste/düzenleme/iptal ve RepeatableRead doluluk raporu eklendi; union/bakım/null payda ve mevcut tanım sınırı. İzolePG/API42+unit13,web280/typecheck/build ve responsive rol/UI kanıtı. Yeni paket/migration yok; gerçek ödeme/CI/liveE2E çalıştırılmadı. [Rapor](../common-areas/07-report.md).

## 27.10.2026 — OA-06 mobil ve offline

Mobil ortak alan liste/detay/salt okunur saat takvimi; online yönetici alan/rezervasyon/iptal; hesap/context sınırlı şifreli cache ve manuel onaylı UUID taslağı eklendi. Ortak saf web/mobil sözleşme ve purge hatasında reddi koruma. Mobil152/152+check+üç export, web274/274+typecheck+build,871lisans metadata ve izoleUI kanıtı. Gerçek native/liveE2E kabul açık; yeni backend/migration/paket yok. [Rapor](../common-areas/06-report.md).

## ORTAK ALANLAR FAZ05 — 26.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**OA-05 / #55, plan 05/08.** FAZ04 kaynak worktree'den main çalışma dosyalarına birleştirildi; Git indeksi ve kaynak worktree korundu. Mevcut bakım planına bağlı kapanış, ortak rezervasyon kilidi/409, UUID, plan tamamlandı/iptal/silme ile kaldırma, kapsamlı duyuru ve uygun legacy hesaplara mevcut kişisel kutu bildirimi uygulandı. Blokla sınırlı bildirim geniş kanala çıkmaz. **164/164 PG/API/regresyon + 10/10 unit; web274/274, typecheck, varsayılan build ve329lisans GEÇTİ,0atlanan.**375px fixture UI'da taşma0; sakinde CLOSED var, yazma/özel not yok. Gerçek auth/API browser E2E, native, SMS/push, yük/pentest çalıştırılmadı. Tek günlük15dk aralık; canonical-only hesapta yetkili duyuru/takvim vardır, kişisel inbox/cihaz teslimi iddiası yok. Yeni migration canlıya uygulanmadı; paket/commit/push/deploy yok. Yalnız kendi test DB/rolü ve13007/13008 fixture temizlendi.

Tur3/3=%100,00 teknik teslim; resmi faz6/7=%85,71 (kullanıcı kabulü açık); kullanıcı bildirimindeki önceki fazlar4/8=%50,00. Ürün yol haritası26kayıt,143/153=%93,46. FAZ06 başlatılmadı; #55 açık. [Rapor](../common-areas/05-report.md).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Son kaynak Codex usage/rateLimitsByLimitId.codex, 26.10.2026:300dk kullanılan%73/kalan%27, reset27.10.2026 01:34:15;10080dk kullanılan%27/kalan%73, reset02.11.2026 15:28:41 Europe/Istanbul. Hesap kotası sohbetin token tüketimi değildir.

## ORTAK ALANLAR FAZ03 — 26.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**OA-03 / Issue #53; plan03/08.** Web yönetici kart/liste/detay/CRUD/409/UUID, salt okunur daire görünümü, yetkili galeri (`20261026131804_CommonAreaPhotos`, canlıya uygulanmadı), TR/EN, ikonlu filtreler. Bu tur: fixture locale tipi ve panel kapanış odağı düzeltildi. **102/102 izole PG/API + 8/8 unit, web 271/271, typecheck, webpack ve Turbopack build, 329 lisans GEÇTİ.** İzole fixture UI kabulü: masaüstü, 375px EN koyu, klavye/odak, 409, salt okunur rol. Gerçek Keycloak/API/MinIO tarayıcı kabulü ve gerçek fotoğraf yükleme UI'ı çalıştırılmadı. Yeni paket yok; commit/push yok. Rezervasyon FAZ04, mobil FAZ06; FAZ04 kullanıcı onayı bekler. [Rapor](../common-areas/03-report.md).

## PROTOKOL — Ortak Alanlar yetki kararı — 26.10.2026

Yönetici alan ve rezervasyon oluşturur/düzenler/iptal eder; yönetici dışı kullanıcı web/merkez ve mobilde yalnız yetkili alanları ve rezervasyon saatlerini okur. Halı saha: dolu/boş/bakım saatleri, kişisel detay yok. MASTER_PROMPT+FAZ03/04/06/07 ve API/teslim raporuna işlendi. Yeni faz/kod/migration/bağımlılık yok; roadmap tiki değişmedi. PROTOKOL2/2=%100,00;11yerelbağlantı eksik0,886kaynak/test/migration/dependency/roadmap dosyası hash aynı,git diff --check GEÇTİ. Uygulama testleri bu tur çalıştırılmadı. Önceki FAZ02 testi yeni kanıt değildir. [Karar](../api/common-areas.md).

## ORTAK ALANLAR FAZ 02 — 26.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**OA-02 / Issue #52; plan02/08.** Alan tür/konum/görünürlük/kurallar/kapasite/saat/referans ücret, yönetim CRUD ve mevcut kullanıcı-daire bağlamından salt okunur katalog; UUID/version/audit/soft-delete, parent geçmiş koruması. Eklemeli20261026125717 migration+Designer+snapshot; canlıya uygulanmadı. **100/100 PostgreSQL/API/regresyon+8/8 domain, web260/260,typecheck,webpack build,329lisans metadata GEÇTİ.** Varsayılan Turbopack build mevcut shared/contact-verification importunda BAŞARISIZ; kaynakları değiştirilmedi. UI/rezervasyon/ödeme/mobil fazı uygulanmadı. Yeni paket/manifest/lock/eski migration/.env/canlı servis değişimi veya commit/push yok; yalnız kendi test DB/rolü temizlendi.

Tur3/3=%100,00 teknik teslim; resmi faz6/7=%85,71(kullanıcı kabulü açık); akışta kabul edilen1/8=%12,50. Mevcut ürün roadmap25 kayıt,142/152=%93,42; OA-02 kanıtı eklendi, sonraki fazlar tiklenmedi. [Rapor](../common-areas/02-report.md). Sonraki FAZ03, yeni kullanıcı onayı bekler; #52 açık, #51 bu tur kapatılmadı. Giriş/çıkış/toplam token ve kalan context: her biri BİLGİ MEVCUT DEĞİL. Codex usage26.10.2026:300dk%41kullanılan/%59kalan,reset20:28:41;10080dk%6/%94,reset02.11.2026 15:28:41 Europe/Istanbul.

## BF-PAY FAZ 08 — 24.10.2026 — Web ödeme yönetim ekranları

- Tahsilatlar modülüne sekmeler: Online ödemeler (detay, REVIEW dağıtımı, iade talebi/farklı yetkili onayı), Defter (ters kayıt, CSV), Mutabakat (günlük özet, kalemler), Banka ve ödeme ayarları (IBAN öner/onayla/kapat, çift onay, üye işyeri, bağlantı testi).
- Makbuz görüntüleme/yazdırma; manuel havalede tahsilat hesabı seçimi; ONLINE_CARD yöntemi. Sistem ayarlarına sağlayıcı kataloğu (SUPER_ADMIN).
- Yeni paket/migration yok. [Rapor](../payment/08-web.md).

## 23.10.2026 — BF-VERIFY04 web iletişim doğrulaması

- Ortak /account paneli:maskeli güncel e-posta/telefon,eski doğrulanmış/yeni bekleyen hedef,IdP VERIFY_EMAIL/UPDATE_EMAIL ve güvenli sabit dönüş;telefonSEND/VERIFY/CANCEL.
- Sunucu sınırı/version,UUID retry,kodun bellekte tutulması,odak/görünürlük yenilemesi,409/429/kesinti ve ortak TR/EN/ikon/tema geri bildirimi. Kişisel e-posta staleJWT yerine güncel contact kaynağından.
- Mevcut sharedUI,transport,profil veUsersyetkisi korundu;test-only gerçek PG/API köprüsü ve tarayıcı fixture’ı. Web250/250,API32+unit16,tarayıcıköprü1,IdP12;typecheck/build geçti. GerçekSMS #46’da açık;canlıdeğişim/commit/push yok. [Rapor](../refactor/contact-verification/04-report.md).

## BF-LATE — 23.10.2026 — Gecikme tazminatı

- Eklendi: LatePenaltySwitch/Policy/Accrual/Segment entity+mapping+migration; LatePenaltyCalculator/Engine/Store; `/late-penalties` API; Worker ay sonu job'ı; payment_allocations kind/accrual_id.
- Değişti: ödeme oluşturma/iptali tazminatı kesinleştirir ve geri alır; borç iptali ödenmiş tazminatı korur; dues yanıtına penaltyBalance; mobil finans penalty/penaltyLines/kind.
- Web: Aidat modülü sekmeleri (gecikme, politikalar, rapor), ödeme formunda tazminat; 13 hata kodu TR/EN. [Rapor](../refactor/late-penalty/REPORT.md).

## BF-VERIFY03 — 23.10.2026 — Yerel OTP uygulaması

- Eklendi:platform Netgsm XML/HTTPS taşıyıcı,RNG/HMAC/HKDF,PhoneOtpChallenge entity/mapping/migration/Designer/snapshot;SEND/VERIFY/CANCEL ortak API ve state DTO.
- Yeniden kullanıldı:MobileOperationReceipt,profil/advisory kilitleri,AES-GCM yardımcı kodu,worker expiry döngüsü;Property kredisine bağlanmadı.
- Korundu:aynı normalize aday/isim/fotoğraf;farklı telefon mevcut kodu iptal eder;VERIFY profile+proof tek transaction.
- Kanıt:32PG/API/yerelHTTP+16unit,9webstandardı;Worker build. Gerçek SMS teslimi eksik,#46açık;PhoneEnabled=false. [Rapor](../refactor/contact-verification/03-report.md).

- BF-VERIFY02 kapanışı: #45 Closed(completed)/Project3 Done son okumada doğrulandı;12/12 madde,3/3 resmi adım.

## BF-VERIFY FAZ02 — 22.10.2026

- Eklendi: kapalı varsayılan yeni hesap e-posta politikası ve ortak Keycloak required-action; davette ACCEPTED/UNAVAILABLE/NOT_REQUESTED.
- Düzeltildi: yönetici/adaptör doğrudan e-posta yazımı bypass'ı; web uyarı ve salt okunur alan,TR/EN.
- Eklendi: projeye ait commit-sonrası eski adrese bildirim eklentisi, tema metinleri ve mevcut API ile derleme/izole IdP test araçları.
- Doğrulandı:12 gerçek IdP/SMTP grubu,8 politika,15 JWT/adaptör,20 web,19 mobil;typecheck/build. Canlı değişiklik/paket/migration yok. [Rapor](../refactor/contact-verification/02-report.md).

## BF-MULTI Ek 04 — 22.10.2026

- Eklendi: kişi kayıt/güncellemede kimlik no, cep telefonu ve e-posta mükerrer engeli (v1 alan, v2 müşteri kapsamı); `BF_PERSON_DUPLICATE_IDENTITY|PHONE|EMAIL`.
- Değişti: eski kişi güncellemesi Property kilidi + transaction; testlerde benzersiz kişi fixture'ları.

## BF-VERIFY FAZ 01 — 22.10.2026 — Ortak iletişim doğrulama altyapısı

- Eklendi:AccountPhoneVerification,ortak ContactVerification DTO/store/API,hesap/mobil GET ve telefon adayı POST;güncel Keycloak e-posta okuması.
- Bağlandı:eski MobileProfileStore telefon yazımı aynı transaction'da kanıt/aday modeline;isim/fotoğraf/aynı numara korunuyor.
- Eklendi:20261022142952 migration+Designer+snapshot;veri backfill'i yok,dolu tablo Down reddi.
- Kanıt:14/14 PostgreSQL/API/migration/şema+6/6 domain ve build geçti;ADR,iki API belgesi ve teslim raporu.
- Gerçek SMS/e-posta flow/istemci ekranı/canlı ayar henüz eklenmedi;yeni bağımlılık yok.
- #44 ayrıntılı işlem/kanıt ve12/12 maddeyle completed kapatıldı;Project3 Done son okumayla doğrulandı. Geçici yalnız-kendi test kaynakları temizlendi.

## BF-MULTI FAZ 09 — 22.10.2026

- Mobil `noticeContextSwitch` (bildirim yalnız yetkili bağlama geçer); 09-report geçiş planı ve kabul listesi.

## BF-MULTI FAZ 08 — 22.10.2026

- Eklendi: `/api/v2/me/units/{u}/documents/{id}/download`, `/api/v2/me/finance-summary`; web portal talepler/indirme/toplu özet; mobil seçili daire özeti.

## BF-VERIFY FAZ 00 — 22.10.2026 — Prompt ve görev paketi

- Eklendi:`buildingflow-contact-verification-prompts/` MASTER_PROMPT,PHASES,START_HERE,TASK_REPORT_TEMPLATE ve8 faz promptu.
- Eklendi:`docs/refactor/contact-verification/00-analysis.md`,32 senaryolu ACCEPTANCE_MATRIX.md;onaylı kapsam ile D1–D4 kararları ayrıldı.
- Açıldı:GitHub #43–#50 ve Project3 öğeleri;ayrıntılı işlem/test/dosya açıklaması ve eksiksiz kabul sonrası completed/Done kapanış kuralı her görevde.
- Güncellendi:TESTING ve STATUS/SESSION;uygulama kodu/DB/ayar/bağımlılık/yolharitası tiki değişmedi. Belge doğrulaması geçti;uygulama testleri çalıştırılmadı.

## BF-MULTI FAZ 07 — 22.10.2026

- Mobil SQLite şema 9 `portfolio_cache`, `portfolioAccess`, AuthProvider portföy okuma/önbellek, ekran durum rozeti ve `CREATE_REQUEST` kapısı; testler güncellendi.

## BF-MULTI FAZ 06 — 22.10.2026

- Mobil canonical Portföyüm gruplama, `initialContext`, `portfolioGroups`; test person-portfolio.test.mjs (2).

## BAKIM — 22.10.2026 — Hesap iletişim bilgileri — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** E-posta aslında Hesap bölümündeydi; Kişisel bilgilerde telefonun yanına salt okunur taşındı, kaynak mevcut giriş hesabı. Kullanıcının ek isteğiyle bakım kapsamına yeni kullanıcı cep telefonu da alındı; tek bakım adımı olarak tamamlandı. İsteğe bağlı `CreateUserRequest.Phone`, mevcut ortak MobileProfile'a ilk kayıt olarak ve üyelikle aynı yerel transaction'da kaydediliyor; mevcut profil üzerine yazılmaz. Hesap sahibi web/mobilde okur ve günceller. Keycloak telefon attribute'ı, Person kaydı, PHOTO rızası ve dizin telefon okuma yetkisi açılmadı. **27/27 izole PostgreSQL/S3 fixture entegrasyonu;235/235 FE; typecheck/build PASS.** Gerçek bileşenli fixture1440/375px: readonly e-posta doğru bölümde, telefon girişi/biçimleme ve yatay taşma0; canlı kullanıcı veya gerçek karşılama e-postası işlemi yapılmadı. Yeni paket/entity/migration yok; ilgili contract/security/verify/docs prosedürleri uygulandı. İŞ yol haritası24 kayıt,141/151=%93,38; ürün/UI/BF-MULTI fazı veya görevi tiklenmedi. Başlangıç indeksi/manifest/lock/next-env ve diğer devam eden işler korundu. Engel yok; sonraki kapsam yeni kullanıcı talebidir.

## BF-MULTI FAZ 05 — 22.10.2026

- Eklendi: `/api/v2/me/units/{unitId}/{dues,payments,announcements,documents,leases}` (`MyUnitStore`); web `features/my-portfolio` ve `/workspaces/portfolio`; `myPortfolio` TR/EN; fixture `/fixture-portfolio`.
- Değişti: OIDC dönüş whitelist'ine `/workspaces/portfolio`; bağlam kabuğu menüsü ve çalışma alanları sayfasına Portföyüm bağlantısı.
- Testler: MyUnitTests (2), my-portfolio.test.mjs (6).

## BF-MULTI FAZ 04 — 22.10.2026

- Eklendi: `features/person-center` UnitPicker, PersonPicker, PortfolioSection, LeasePanel, AccountLink, Badges, relation-style; fixture `/fixture-people`, `/fixture-person`.
- Değişti: Kişiler listesi `/api/v2/people` dizini (alan seçimi zorunlu değil); ilişki formu malik/sakin + bağımlı seçim; kiracılık sözleşme üzerinden; sekmeler ikonlu; TR/EN anahtarlar.
- Testler: person-center.test.mjs +7.

## BF-MULTI FAZ 03 — 22.10.2026

- Eklendi: `POST …/people/{p}/account-link-codes`, `POST …/account-link/end`, `POST /api/v2/me/person-links`, `GET /api/v2/me/portfolio`, `GET /api/v2/me/active-context`; `CentralPersonLinkCode`, `PersonAccountLinkService/Store`, `PortfolioCapabilities`; `BF_PERSON_LINK_UNAVAILABLE`; migration `20261022091337_CentralPersonLinkCodes`.
- Değişti: oturum bağlamları ve mobil yetki PENDING/REJECTED ilişkileri ve pasif sözleşmeli kiracılığı dışarıda bırakır; `account-status` `codeHint`/`codeExpiresAt`.
- Testler: MyPortfolioTests (4), PortfolioCapabilitiesTests (2); eski bağlam testlerinin fixture ilişkileri LEGACY (`TestRelations`).

## BF-MULTI FAZ 02 — 22.10.2026

- Eklendi: `GET /api/v2/people`, `…/people/{person}/portfolio`, `…/person-building-choices`, `…/units/{u}/lease-contracts` (GET/POST, parties, attach, end); `LeaseContractStore`, `LeaseContractOperation`; `BF_LEASE_OVERLAP`, `BF_LEASE_PARTY_OUTSIDE`; migration `20261022085436_LeaseContractExclusivityAndOperations`.
- Değişti: `person-unit-choices` isteğe bağlı `buildingId` ve satırda `buildingId`; `unit-relations` satırında `verificationStatus`, `leaseContractId` (ekleyici).
- Testler: PersonPortfolioApiTests (6), LeaseContractPersistenceTests +1, LeaseContractTests +1.

## BF-MULTI FAZ 01 — 22.10.2026

- Eklendi: `LeaseContract`, `VerifiableRecord`/`RelationVerificationStatus`, `UnitTenancy.LeaseContractId`, `PersonPortfolio` read model, migration `20261022083050_LeaseContractsAndRelationVerification` (LEGACY backfill, değişmezlik ve sözleşme aralığı trigger'ları, dolu Down reddi).
- Değişti: Kişi merkezi ilişki yazımı aktör hesabıyla VERIFIED kaydeder, hesapsız aktöre 403; person-transition aktarımı LEGACY yazar.
- Testler: LeaseContractTests, PersonPortfolioTests, LeaseContractPersistenceTests, PersonCenterTests +1.

## BAKIM — 22.10.2026 — Üst ve alt menü oval köşeleri — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** Ortak workspace-header/footer masaüstünde18px,≤650px ekranlarda16px oval köşeli; küçük dış boşluk ve semantik tema sınırı. Menü açılır panelleri kırpılmıyor; radius için overflow:hidden eklenmedi. Yalnız globals.css sunumu değişti; auth/API/kayıt/menü davranışı değişmedi. **221/221 FE, typecheck ve build geçti;1440/320/390px CUA fixture: hesap ve gizlilik menüsü açılıyor, yatay taşma yok, browser error/warn0.** Yeni test, bağımlılık, font/ikon/görsel paketi yok. Backend/mobil testleri bu CSS turunda çalıştırılmadı. Kaynak planlar frontend/buildingflow-ui-plan/MASTER_PROMPT.md ve PHASES.md okundu; tarihsel analiz adımı yeniden başlatılmadı. Mevcut frontendShellAndLogo sunum görevinin ek kanıtıdır; ürün/UI fazı veya görev tiki değiştirilmedi. İŞ planı 24 kayıt, 141/151 = %93,38. Git indeksi/manifest/lock/next-env ve önceki kullanıcı farkları korundu. Engel yok; sonraki kapsam yeni kullanıcı talebidir.

## BAKIM — 22.10.2026 — Yetkili kullanıcı fotoğrafı — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** Kullanıcının açık onayıyla yetkili yöneticinin diğer hesaplara fotoğraf ekle/değiştir/kaldır eylemi; hedef sahibinin güncel PROFILE_PHOTO rızası, mevcut rol/üyelik sınırı, ortak mobil/web pointer/version, actor+target UUID replay, JPEG256KiB/40MP kaynak sınırı ve metadata temizliği korunur. İlk fotoğrafta mevcut dizinden geçerli ad/soyad, mevcut profilde alan koruması. Yeni paket/tablo/migration yok. **24/24 gerçek izole PostgreSQL/özel test bucket entegrasyonu;221/221 FE, typecheck/build; masaüstü/dar ekran fixture+odak/rıza/kaldır kanıtı.** Canlı Keycloak/telefon kabulü çalıştırılmadı. [Dosyalar,komutlar,görseller ve koruma raporu](../navigation-refactor/USER_PHOTO_EVIDENCE.md). İŞ yol haritası24 kayıt,141/151=%93,38; mevcut İŞ19 kanıtı bağlandı, yeni görev/faz tiki yok. Engel yok; sonraki kapsam kullanıcı talebidir.

## BAKIM — 21.10.2026 — Anket sekmelerine ikon ve kalıcı ikon kuralı — TAMAMLANDI

**Anketler durum sekmeleri ikonlu:** Tümü LayoutGrid, Taslak FilePen, Planlandı CalendarClock, Aktif CirclePlay, Tamamlanan CircleCheckBig, Süresi Dolan TimerOff (mevcut lucide-react; yeni paket yok). Aynı ikonlar tablo/kart durum rozetlerinde de gösterilir (`features/surveys/category-style.ts` `stateIcon`). Ortak `FilterChips` türünde `icon` artık zorunlu; ikonsuz çip typecheck'ten geçmez. Kullanıcı kararı AGENTS.md ve frontend-rules.md'ye kalıcı kural olarak yazıldı: sekme, filtre çipi, durum, tür ve kategori seçimleri her zaman ikonludur.

**Bu tur:** FE `npm test` **215/215** (yeni ikon testi 1), `npm run typecheck` ve `npm run build` **PASS**. Test API fixture 1440/375px sekme görünümü kontrol edildi. Commit yok. Aynı kural gereği metin-only kalan SMS (Hesap Settings, Şablonlar FileText, Mesajlar MessageSquareText) ve Rızalar (Türler Tags, Metinler FileText, Kayıtlar ClipboardCheck) sekmelerine de ikon eklendi.

## BAKIM — 21.10.2026 — Anketler (yönetim paneli, oluşturma ekranı, sakin yanıt API'si) — TAMAMLANDI

**İletişim → Anketler ekranı tasarım görseline göre eklendi** (`/properties/{id}/surveys`, yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER, ClipboardList ikonu): Toplam Anket / Toplam Katılım / Devam Eden / Tamamlanan KPI'ları, Tümü/Taslak/Aktif/(Planlandı)/Tamamlanan/Süresi Dolan sayılı sekmeler, tarih aralığı + kategori + arama, katılım çubuklu tablo ve mobil kartlar, satır menüsü, Hızlı İşlemler (yeni, şablondan, kopyala, CSV dışa aktar), Kategori Dağılımı halkası (ikon+etiket+sayı lejantı, 2px boşluk), En Çok Katılım, Son Tamamlanan ve Öneriler. Görseldeki fotoğraflar yerine lucide kategori ikonlu renkli kutucuklar kullanıldı; yeni paket/font/ikon/görsel yok.

**Oluşturma/düzenleme paneli:** başlık, açıklama, ikonlu kategori seçimi, başlangıç/bitiş, hedef kitle (tüm sakinler/malikler/kiracılar), yanıt kuralı (daire başına / hesap başına), anonim anket anahtarı ve KVKK notu; en çok 30 soru (tek seçim, çoklu seçim, 1–5 puan, açık uçlu), 2–10 seçenek, sıralama/silme/zorunlu, sakin önizlemesi; beş TR/EN şablon. Taslak kaydet veya kaydet ve yayınla; yayından sonra sorular kilitli, yalnız bitiş uzatma/kapatma. Detay panelinde sonuç çubukları, puan ortalaması, metin yanıtları ve formül enjeksiyonuna karşı korumalı CSV. [API](../api/surveys.md).

**Backend:** altı tablo (`surveys`, `survey_questions`, `survey_options`, `survey_participations`, `survey_responses`, `survey_answers`; BaseEntity, bileşik Property FK, check constraint). Migration `20261021195918_Surveys` yalnız ekleme; Down anket varsa durur. Sakin yanıtı mobil oturum bağlamıyla (üyelik + bağlı kişi + aktif daire oturumu) doğrulanır; tek yanıt benzersiz ballot anahtarı ve satır kilidiyle korunur. Anonim ankette yanıt satırları katılım/daireye bağlanmaz, audit `anonymous` ve gün başına yuvarlanmış zamanla yazılır (AuditInterceptor `IAnonymousAudit`), 3 yanıttan önce sonuç gösterilmez. Yeni kodlar `BF_SURVEY_INVALID`, `BF_SURVEY_QUESTION_INVALID`, `BF_SURVEY_ANSWER_INVALID`, `BF_SURVEY_STATE`, `BF_SURVEY_ALREADY_ANSWERED`, `BF_SURVEY_NOT_OPEN` ErrorCatalog/error-codes/TR-EN eşli. Anket geçmişi olan Property silinemez.

**Test:** Unit **285/285**; anket integration 7 + migration 1 **PASS**; tam integration koşusunda yalnız S3 gerektiren baseline `MobileCanonicalSync` fotoğraf testi kırmızı (bu işten bağımsız). FE `npm test` **214/214** (yeni `surveys.test.mjs` 7), `npm run typecheck` ve `npm run build` **PASS**, `check-licenses.py` PASS (329 paket, değişiklik yok). Test API fixture ile 1440/1600/375/320px liste, editör, şablon ve sonuç ekranları yatay taşma 0, console error 0; fixture canlı OIDC/API kabulü değildir. `PlanningMigrationTests` içindeki "son migration Planning" beklentisi yeni migration nedeniyle `migrations.Last()` kontrolüne çevrildi.

**Açık:** sakin mobil uygulamasındaki yanıt ekranı yazılmadı (API hazır). Commit/push/deploy yok; önceki kullanıcı farkları korundu. Tek agent. Token giriş/çıkış/toplam/kalan context: BİLGİ MEVCUT DEĞİL.

## BAKIM — 21.10.2026 — Açık/koyu/sistem teması — TAMAMLANDI

**Üst çubukta dil seçiminin yanına Açık tema / Koyu tema / Sistem teması eklendi.** Ortak ThemeProvider tüm bu Next.js uygulamasını kapsar. Varsayılan sistemdir; cihaz prefers-color-scheme değişimini izler. Tercih yalnız buildingflow.theme=light/dark/system olarak tarayıcıda saklanır ve storage olayıyla sekmeler arasında eşitlenir. Saklama engellenirse seçim sekmede uygulanır, geçici tercih açıklaması gösterilir. Auth token/kişi verisi/konum saklanmaz, API/rol/DB değişmez. Ayrı website/Keycloak uygulamalarına tema taşıma bu kapsamda yapılmadı.

RootLayout sabit, girdisi doğrulanan başlangıç betiğini head içinde mevcut proxy'nin request nonce'u ile çalıştırır; ilk boyada class/data-theme/color-scheme seçilir. Yalnız html tema öznitelikleri için suppressHydrationWarning vardır. CSP gevşetilmedi, unsafe-inline script eklenmedi. Tailwind dark varyantı class tabanlıdır; açık tema OS koyu olsa da açık kalır. Koyu semantik palet kart/form/dialog/popover/mesaj/aksiyon/grafik ve eski beyaz yüzeyleri kapsar; markanın lacivert sidebar/fotoğraf yüzeyleri korunur. Mobil başlık yeni tema düğmesine göre yerleştirildi.

**Bu tur:** `npm test` **210/210**,fail/skip0; `npm run typecheck` ve son `npm run build` **PASS**.3 yeni test: üç tercih ve OS önceliği, bozuk/engellenmiş storage, tüm tercih×OS matrisinde ilk boya/runtime eşliği ve kayıtlı girdinin kod olarak çalıştırılmaması. Gerçek production HTTP200 yanıtında tema betiği nonce'u CSP nonce'uyla eşleşti, script-src unsafe-inline yok. Gerçek React fixture: koyu body rgb(12,20,34), kart/form rgb(19,31,49), metin rgb(229,237,247); reload sonrası koyu korunur. İkinci sekmede Açık seçilince ilk sekme açığa geçti. Sistem seçimi mevcut OS light ile eşleşti. OS ayarı test sırasında değiştirilmedi; iki OS durumu birim testinde, canlı değişim dinleyicisi kod incelemesinde doğrulandı.320px viewport=document320, tema düğmesi36×36, taşma0; tarayıcıerror0. Koyu gerçek tahakkuk formu açılıp alan dolduruldu, gerçek tahakkuk oluşturulmadı.

[Görsel](../navigation-refactor/THEME_DARK.jpg), [koyu form](../navigation-refactor/THEME_DARK_FORM.jpg), [makine kanıtı](../navigation-refactor/THEME_EVIDENCE.json). Fixture örnek verili, canlı OIDC/gerçek cihaz testi değildir. Backend testleri bu frontend turunda koşulmadı. Başlangıç Git indeksi/manifest/lock/next-env ve önceki kullanıcı farkları korundu;diffcheckPASS. Yeni paket/font/ikon/görsel bağımlılığı eklenmedi; install/restore olmadığından paket envanteri yeniden yazılmadı. Test sekmeleri ve geçici sunucular kapatıldı, viewport sıfırlandı; test originindeki tercih Sistem'e geri bırakıldı.

Değişen uygulama dosyaları: app/layout.tsx, auth/AuthGate.tsx, app/globals.css, yeni features/theme/theme.ts/ThemeProvider.tsx/ThemeSwitcher.tsx, TR/EN sözlükleri; mevcut fixture sarmalaması ve yeni theme.test.mjs. TESTING/progress/frontend kuralları/teslim kaydı güncellendi. Tek agent mevcut verify/docs ve nonce güvenlik incelemesi. Bu tur **1/1=%100,00**; BAKIM, ürün/UI faz ve roadmap işaretleri değişmedi. Tüm yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL. Engel yok; sonraki adım yeni kullanıcı talebi.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim okuması21.10.2026:300dk kullanılan%55/kalan%45,reset22.10.2026 03:18:13;10080dk kullanılan%55/kalan%45,reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbetin token tüketimi değildir.

## BAKIM — 21.10.2026 — Tek açık ana menü — TAMAMLANDI

**Sidebar tek açık grup davranışına geçti.** Başka ana başlık açıldığında önceki kapanır; açık başlığa tekrar basıldığında tüm gruplar kapanabilir. Aktif alt sayfanın üst grubu rota/alan değişiminde açılır; aktif grup kullanıcı tercihini zorla geçersiz kılmaz. Masaüstü ve mobil aynı state'i kullanır; dar ikon görünümünün mevcut yapısı korunur. WorkspaceShell.tsx içinde closedGroups listesi yerine openGroup kullanıldı. Route/rol/izin ve alt menü öğeleri değişmedi; yeni paket/API/DB yok.

**Bu tur:** `npm test` **207/207**, fail/skip0; `npm run typecheck` ve `npm run build` **PASS**. Gerçek shell/test API fixture1440px: Daireler→Finans, yalnız Finans açık; Aidat linkiyle /dues sonrası aktif Finans'tan Operasyonlar'a geçişte Finans kapanır. Enter ile açık Operasyonlar kapatıldı (açık grup0).375px mobil drawer'da Finans→İletişim sonrası yalnız İletişim açık; Escape kapatır. Masaüstüne dönüşte Finans açıldığında yalnız Finans açık; consoleerror0. [Görsel](../navigation-refactor/SIDEBAR_ACCORDION.jpg). Canlı OIDC/gerçek cihaz/backend testi bu yalnız frontend turunda koşulmadı. Mevcut otomatik testler kullanıldı, bu küçük state düzeltmesine uygulamayı tekrarlayan test eklenmedi.

Mevcut TESTING/progress/uygulama teslim kaynakları güncellendi. Başlangıç Git indeksi, manifest/lock, next-env ve önceki kullanıcı farkları korundu; diffcheckPASS. Fixture/sekme kapatıldı, viewport sıfırlandı. **Bu tur1/1=%100,00**; BAKIM, ürün/UI fazları ve roadmap işaretleri değişmedi. Tüm iş yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL. Tek agent mevcut verify/docs yönergeleri; engel yok. Sonraki adım yeni kullanıcı talebi.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim okuması21.10.2026:300dk kullanılan%25/kalan%75,reset22.10.2026 03:18:13;10080dk kullanılan%51/kalan%49,reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbetin token tüketimi değildir.

## BAKIM — 21.10.2026 — İki sütunlu tahakkuk formu — TAMAMLANDI

**Yeni tahakkuk paneli masaüstünde sağ/sol iki sütuna ayrıldı.** Sol: borç muhatabı, arama/bölüm listesi, toplu seçim, sayfalama ve seçilenler. Sağ: dönem, bölüm başına tutar, vade, açıklama ve toplam. Ortak Kaydet/Vazgeç altta. Panel en fazla1120px, viewport'a uyumlu;1024px altı tek sütun. Mobil tam genişlik korunur. Yalnız AssessmentEditor sunumu değişti; diğer ManagementPanel ekranları, API/finans/muhatap kuralları,500 sınırı ve UUID/retry davranışına dokunulmadı.

Değişen uygulama dosyası: frontend/buildingflow-web/src/features/dues/DuesDialogs.tsx. Mevcut izole fixture'a gerçek AssessmentEditor açma ve48 temsili bölüm verisi eklendi; üretim rotası/auth bypass yok. Yeni paket/font/ikon/görsel bağımlılığı veya migration yok; salt düzen değişikliği için yeni otomatik test yazılmadı.

**Bu tur:** `npm test` **207/207**,fail/skip0; `npm run typecheck` ve `npm run build` **PASS**. Gerçek React/test API fixture1440x1000:panel1120px, sol547,90px/sağ498,10px, yan yana ve taşmasız.48bölüm×1250,00=**60000,00**; seçimi temizleyip3bölüm seçildiğinde **3750,00**.375 ve320px paneller viewport genişliğinde/tek sütun ve taşmasız; tarayıcıerror0. [Görsel](../navigation-refactor/ASSESSMENT_TWO_COLUMNS.jpg). Bu testte gerçek tahakkuk oluşturulmadı; üretim/OIDC/gerçek cihaz/backend testleri bu yalnız sunum turunda çalıştırılmadı. Önceki kullanıcı farkları, başlangıç indeksi/manifest/lock ve next-env korundu;diffcheckPASS. Fixture/sekme kapatıldı, viewport sıfırlandı.

Bu tur **1/1=%100,00**; BAKIM işi, ürün/UI faz ve roadmap işaretleri değişmedi. Tüm iş yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL. Tek agent, mevcut akadev-verify/docs yönergeleri. Engel yok; sonraki adım yeni kullanıcı talebi.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim okuması21.10.2026:300dk kullanılan%14/kalan%86,reset22.10.2026 03:18:13;10080dk kullanılan%49/kalan%51,reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.

## BAKIM — 21.10.2026 — MET Norway — TAMAMLANDI

**Kullanıcı onaylı MET Norway hava tahmini bağlandı:** kişisel İstanbul varsayılanı, İstanbul/Ankara/İzmir/Artvin seçimi, sıcaklık/koşul ikonu, sağlayıcı güncelleme zamanı, otomatik kontrol ve ayrı Hava durumunu yenile düğmesi. Şehir/hesap değişiminde istek iptal edilir; eski şehrin sıcaklığı gösterilmez. Görünür sayfada cache zamanına göre süreli kontrol, gizli sayfada sorgu yok; bu davranış kod incelemesiyle doğrulandı, dakikalar süren otomatik süre dolumu tarayıcıda ayrıca beklenmedi.

**Lisans:** açıkça onaylanan veri bağlantısı CC BY4.0 koşullarıyla; MET Norway kaynak ve lisans bağlantıları görünür, seçili zaman/yuvarlama açıklaması var. Yeni npm/NuGet paketi, sağlayıcı ikonu, font veya görsel eklenmedi; paket envanteri değişmedi. Kaynak/ücretsiz kullanım/servis koşulları resmî sitelerden bu tur doğrulandı. API uygulama adıyla tanımlanır; isteğe bağlı Weather:ContactUrl gerçek HTTPS iletişim bağlantısını ekler. Uydurma iletişim bilgisi yok. Yeni konum izni yok, tarayıcı IP/koordinat/tokenı sağlayıcıya gönderilmez; yaklaşık şehir merkezleri kullanılır. Ücretli abonelik/API anahtarı gerektirmez; hizmet SLA garantisi vermez.

**Sunucu:** Application DTO/arayüz + Infrastructure MetWeatherProvider + yetkili API endpoint. Sabit HTTPS hedef, yönlendirme kapalı,8sn timeout,1MiB gövde sınırı; dört şehir cache'i, şehir başına tek istek, Expires/Last-Modified/304 ve Retry-After. Bekleme içinde Yenile sağlayıcıya tekrar yük bindirmez. Hata/bozuk veri normal UNAVAILABLE veya son başarılı veriyle STALE; ham exception/sağlayıcı yanıtı render edilmez. Kişisel hava verisi Property finans kapsamı değildir; CanManageDues rol/sub ve mevcut no-store/rate limit korunur. Model tahmini anlık istasyon ölçümü diye gösterilmez. Tablo/migration/DB yazımı yok.

**Bu tur test:** `npm test` **195/195**, fail/skip0; `npm run typecheck` ve `npm run build` **PASS**. Backend **11/11**, fail/skip0:6 sağlayıcı parser/cache/singleflight/304/backoff/bozuk-birim-sıcaklık testleri ve5 API testi (dört finans rolü, anonim/resident/sahte rol,400 katalog kodu, no-store). API test fixture'ı yalnız ayrı geçici PostgreSQL DB/rolünde çalıştı ve kendine ait kaynakları temizledi; hava testlerinde dış HTTP sahteydi. Ayrı gerçek MET isteği İstanbul için **HTTP200**, Celsius/90 kayıt, Expires/Last-Modified ile başarılı; bu bir canlı OIDC/tam üretim oturumu testi değildir.

**UI fixture:** İstanbul18°C, Yenile düğmesi API'yi tekrar çağırdı; İzmir24°C'ye geçiş, Ankara STALE uyarısı, Artvin UNAVAILABLE/— görünümü, İstanbul'a dönüş başarılı. 320px viewport=document320, yatay taşma yok; tarayıcıerror0. Bu sıcaklıklar örnek fixture verisidir. [Görsel](../navigation-refactor/DASHBOARD_MET_WEATHER.jpg), [makine kanıtı](../navigation-refactor/DASHBOARD_MET_WEATHER_EVIDENCE.json), [tam teslim/önceki seçenekler](../navigation-refactor/DASHBOARD_WEATHER.md), [API sözleşmesi](../api/dashboard.md), [lisans kaydı](../licenses.md). Gerçek cihaz/üretim dağıtımı yapılmadı. Fixture/sekme kapatıldı, viewport sıfırlandı.

Değişen dosyalar: Weather.cs, MetWeatherProvider.cs, WeatherEndpoints.cs, Program.cs'nin yalnız servis/endpoint kaydı; DashboardHero/WeatherLocation/weather.ts, CSS, TR/EN, fixture, WeatherApiTests/MetWeatherTests/weather.test.mjs ve mevcut belge kaynakları. Başlangıç Git indeksi/manifest/lock ve next-env korundu; diff check PASS. Önceki sistem durumu/diğer kullanıcı farkları korunur. Tek agent akadev-contract/security/license/verify/docs; yeni agent/MCP kurulmadı. **Bu tur 1/1=%100,00**; ürün/UI yol haritası/faz işaretleri değişmedi, tüm yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim okuması:300dk kullanılan%0/kalan%100, reset22.10.2026 03:18:13 Europe/Istanbul;10080dk kullanılan%47/kalan%53, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.

Engel yok; onaylanan MET entegrasyonu tamamlandı. Sonraki adım yeni kullanıcı talebi; başka faza geçilmedi.

## BAKIM —21.10.2026 — [BUILDINGFLOW] Yaklaşan önemli tarihler, Planlama & Bakımlar, Duyuru kategorileri — TAMAMLANDI

**Dashboard "Yaklaşan Önemli Tarihler" kartı aidat son ödeme tarihlerini ve açık planları tarih sırasıyla birleştirir (en çok4, aynı gün aidat önce, "N gün kaldı"/"Bugün" rozeti, "Tümünü Gör").** Yeni Planlama & Bakımlar ekranı (`/properties/{id}/maintenance`, Operasyon menüsü, CalendarCheck2): Bakım/Temizlik/Kontrol/Tatbikat/Diğer ikonlu tür çipleri ve sayıları, arama, tarih aralığı, durum, tablo+mobil kart, sağ panelde oluştur/görüntüle/düzenle/sil, 409 güncel veri bandı. "Planlı bakım" KPI'si artık gerçek30 günlük sayıyı gösterir. Duyurular: Genel/Toplantı/Aidat/Bakım/Acil/Diğer kategorisi, ikonlu rozet, sayılı filtre çipleri ve formda ikonlu seçim. [API](../api/maintenance.md), [Duyurular](../api/communication.md). Yeni paket/font/ikon seti yok (mevcut lucide).

**Backend:** `maintenance_plans` tablosu (BaseEntity, Property FK Restrict, check constraint), `announcements.category` (default GENERAL, mevcut kayıtlar backfill, check constraint). Migration `20261021103545_PlanningAndAnnouncementCategories` yalnız ekleme; Down veri kaybını engeller. `BF_PLAN_INVALID` ErrorCatalog/error-codes/TR-EN apiErrors eşliği. Plan geçmişi olan Property silinemez. Eski istemcinin kategori göndermeyen düzenlemesi kategoriyi korur.

**Test:** Unit281/281, ilgili integration66/66 (7 skip: S3 depolama gerektiren mevcut belge testleri), FE201/201, typecheck/üretim build PASS. Yeni: plan domain3, Maintenance API3 kapsamlı senaryo (CRUD/version/soft-delete/rol/ACCOUNTANT salt okuma/pasif parent/filtre/sayım/upcoming/kategori), migration upgrade/backfill/Down guard1, FE model/dashboard birleştirme/menü6. Test API fixture ile 1440/375px dashboard, liste, panel, duyuru ekranları taşma0/console error0. Fixture canlı OIDC/API kabulü değildir. Önceki tam koşuda baseline'da da kırmızı olan MobileCanonicalSync fotoğraf testi bu işten bağımsızdır.

Commit/push/deploy yok. Tek agent. Token giriş/çıkış/toplam/kalan context: her biri BİLGİ MEVCUT DEĞİL. Kota bilgisi BİLGİ MEVCUT DEĞİL. Görev: GitHub issue #31.

## BAKIM — 21.10.2026 — Kişisel hava durumu şehri — TAMAMLANDI

**Artvin gösteriminin kaynağı Property.city idi. Hava durumu şehri artık yönetim alanından bağımsız kişisel tercihtir; varsayılan İstanbul.** Şehir adı düğmesi mevcut Dialog/Input/Button ile düzenlenir; Unicode/uzunluk/bozuk kayıt doğrulaması ve kullanıcı subject'iyle ayrılmış tarayıcı tercihi vardır. Depolama engelliyse güvenli varsayılan/yalnız sayfada kullanım açıklaması bulunur. GPS/IP konumu okunmaz, Property veya güvenlik başlıkları değişmez. Sıcaklık — ve canlı servisin bağlı olmadığı açıkça gösterilir; mevcut dashboard Yenile düğmesi hava servisi gibi sunulmaz.

Kullanıcı ayrıca **canlı servis seçeneklerini hazırlamayı** onayladı. [Karşılaştırma ve önerilen uygulama](../navigation-refactor/DASHBOARD_WEATHER.md): MET Norway ücretsiz açık veri/atıf/SLA yok, önerilen seçenek; Open-Meteo ücretsiz API ticari değildir, ticari abonelik gerekir. Resmî koşullar 21.10.2026 incelendi. Henüz dış servis, abonelik, API anahtarı, veri lisansı entegrasyonu veya yeni bağımlılık eklenmedi. Seçim sonrası otomatik yenileme + manuel Yenile + son güncelleme önerilir; sağlayıcı Expires/cache kuralları atlanmaz. Canlı sıcaklık uygulandı diye raporlanmaz.

**Bu tur:** frontend `npm test` **193/193**, fail/skip 0; `npm run typecheck` ve `npm run build` **PASS**. İki yeni test kişisel şehir varsayılanı/hesap ayrımı/kalıcılık/engellenmiş depolama/bozuk kayıt/Unicode doğrulamasını kapsar. Gerçek React + izole fixture: İstanbul→İzmir kaydetme, reload sonrası İzmir, ikinci alana geçişte İzmir korunması, İstanbul'a geri dönme başarılı. 320 px viewport=document320, taşma yok; console error0. [Görsel](../navigation-refactor/DASHBOARD_WEATHER.jpg). Canlı OIDC, gerçek cihaz veya backend testi bu yalnız frontend turunda çalıştırılmadı. Başlangıç Git indeksi, manifest/lock ve next-env korundu; diff check PASS. Fixture/sekme kapalı, viewport sıfırlandı.

Değişen uygulama dosyaları: DashboardHero.tsx, yeni WeatherLocation.tsx/weather-location.ts, globals.css, TR/EN sözlükleri ve weather-location.test.mjs. Mevcut TESTING, ilerleme, frontend kuralları ve teslim kaynağı güncellendi. Tek agent; akadev-verify/docs ve sağlayıcı seçeneklerinde akadev-license kaynak incelemesi kullanıldı. Bu tur **1/1 = %100,00**; ürün/UI fazları ve roadmap işaretleri değişmedi, tüm yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage / rateLimitsByLimitId.codex teslim okuması21.10.2026: 300dk kullanılan%80/kalan%20, reset21.10.2026 16:14:02 Europe/Istanbul;10080dk kullanılan%44/kalan%56, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.

Bu turda engel yok. Sonraki adım: kullanıcının canlı sağlayıcı seçimi; henüz entegrasyon onayı verilmedi.

## BAKIM — 21.10.2026 — Aidat tahsilat kartı — TAMAMLANDI

**Referanstaki Aidat Tahsilat Durumu kartı eklendi:** yeşil halka, Bu Ay / Önceki Ay seçimi, Tahsil Edilen / Bekleyen / Geciken tutarları, TR/EN metinleri ve mobil düzen. Mevcut gelir/gider ve doluluk kartları korunur. Seçili yönetim alanını izler; alan değişiminde kart sıfırlanır. **Bu tur 1/1, %100,00.** BAKIM işidir; SIDEBAR 8/8 ve ürün/UI faz işaretleri değiştirilmedi. Tüm yol haritasının bu tur hesaplanan yüzdesi: BİLGİ MEVCUT DEĞİL.

Sözleşme: DashboardSummary.collections iki dönemli ek alandır. Aynı RepeatableRead, Property/aktif üyelik ve dört finans rolü sınırında iptal edilmemiş DuesCharge tahakkukları gruplanır. Toplam = tahsil + vadesi gelmemiş kalan + geciken kalan; dueDate=today gecikmiş değildir. Tahsil edilen PaidAmount güncel mahsuplarıdır, paidDate bazlı aylık nakit tahsilatıyla karıştırılmaz. Seçili ayın güncel bakiyesidir, tarihsel kapanış değildir. Sıfır tahakkukta oran —; eski API alanı yoksa kart açıklamalı olarak kullanılamaz. API üreticisi/FE tüketicisi birlikte güncellendi; migration, yeni paket, izin veya kişi verisi eklenmedi.

**Bu tur doğrulama:** `npm test` **191/191**, sıfır fail/skip; `npm run typecheck` ve `npm run build` **PASS**. `DashboardApiTests` ayrı gerçek PostgreSQL test veritabanında **7/7**, sıfır fail/skip: dört rol, boş dönemler, tahakkuk/nakit dönemi ayrımı, kısmi ödeme, iptal edilen tahakkuk, bugünkü vade, terslenen ödeme ve 401/403/404 sınırları. Test yalnız kendisine ait geçici veritabanı/rolü temizledi. BigInt oran/toplam doğrulama testleri yıl geçişi, bozuk/negatif ondalık, tutarsız toplam ve eski API'yi kapsar.

Gerçek React + test API fixture: Bu Ay **%78**, Önceki Ay **%90**; alan değişince Bu Ay'a döner. 1440 px kart genişliği 360,66 px; 320 px ekranda document=320, kart=288, taşma yok; tarayıcı hata kaydı 0. [Görsel](../navigation-refactor/DASHBOARD_COLLECTION.jpg), [kanıt](../navigation-refactor/DASHBOARD_COLLECTION_EVIDENCE.json), [API sözleşmesi](../api/dashboard.md). Canlı OIDC veya fiziksel cihaz testi çalıştırılmadı. Değişen uygulama dosyaları: Dashboard.cs, DashboardStore.cs, DashboardApiTests.cs, DashboardPage.tsx, model.ts, CollectionCard.tsx, collection.ts, globals.css, TR/EN sözlükleri, fixture ve collection.test.mjs. Paket/lock ve başlangıç Git indeksi korundu; yalnız beklenen next-env.d.ts derleme çıktısı başlangıç içeriğine döndürüldü.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Hesap kota kaynağı: Codex usage / rateLimitsByLimitId.codex, 21.10.2026 teslim okuması. 300 dakika kullanılan %67 / kalan %33, reset 21.10.2026 16:14:01 Europe/Istanbul. 10080 dakika kullanılan %42 / kalan %58, reset 27.10.2026 13:32:07 Europe/Istanbul. Bunlar bu sohbetin token tüketimi değildir.

Engel yok. Sonraki adım: yeni kullanıcı talebi; başka faza geçilmedi.

## BAKIM —21.10.2026 — Dashboard doluluk halkası — TAMAMLANDI

**Tür dağılımı kartı yerine referanstaki doluluk halkası, yüzde, dolu/boş listesi ve alanın bölüm listesi bağlantısı eklendi.** Yalnız apartman birimleri varsa Daire Doluluk Oranı, diğer türlerde Bölüm Doluluk Oranı başlığı kullanılır. Beklemede verisi mevcut API'de yoktur: sayı —, açıklaması erişilebilir; turuncu halka payı veya sıfır uydurulmaz. Sıfır bölümde yüzde de —; tam dolulukta %100 gösterilir. Backend/API/DB/bağımlılık değişmedi.

**188/188 frontend testi, 0 fail/skip; typecheck ve üretim build PASS.** Yeni 2 test: 88/96→%92, sıfır/tam/karma oran ve pending=null; mağaza/ofis için doğru terminoloji. Gerçek Dashboard/test API fixture: ilk alan 86/96→%90, ikinci alan38/48→%79 ve doğru UUID'li liste bağlantısı; tıklama ikinci alanın units sayfasına gitti. 320px document320/kart288, taşma0; consoleerror0. [Görsel](../navigation-refactor/DASHBOARD_OCCUPANCY.jpg), [kanıt](../navigation-refactor/DASHBOARD_OCCUPANCY_EVIDENCE.json). Canlı OIDC/gerçek cihaz/backend testleri bu yalnız frontend turunda koşulmadı.

Değişenler: DashboardPage.tsx, yeni OccupancyCard.tsx/occupancy.ts, globals.css, TR/EN ve occupancy.test.mjs; TESTING/progress/frontend/API belgeleri. Başlangıç indeks/manifest/lock/next-env ve önceki kullanıcı farkları korundu; diff check PASS. Fixture/sekme kapalı, viewport reset. Bu tur1/1=%100,00; sidebar8/8=%100,00 korunur; ürün roadmap değişmez. Tek agent mevcut verify/docs; sonraki kullanıcı incelemesi.

Token giriş/çıkış/toplam/kalan context: her biri BİLGİ MEVCUT DEĞİL. Son usage21.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%45/kalan%55 reset21.10.2026 16:14:01;10080dk kullanılan%38/kalan%62 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## BAKIM —21.10.2026 — Referans sol menü — TAMAMLANDI

**Solmenü referans lacivert/cyan yuvarlatılmış çerçeve,maviaktif,arama,ikonlu ana ve çizgili altmenülerle uyarlandı.** Mevcutroute/rol/flag/disabled/APARTMENT ve profilkorundu;altalanseçimi geri eklenmedi. [Rapor](../navigation-refactor/SIDEBAR_REFERENCE.md),[görsel](../navigation-refactor/SIDEBAR_REFERENCE_DESKTOP.jpg). WorkspaceShell,CSS,TR/EN,sidebar-layout helper/test ve mevcut testkaynak eşliği değişti;backend/API/DB/paket/asset yok.

**186/186FE,0fail/skip;typecheck/build PASS.** Yeni2test192kombinasyonda öğe/route/izin eşliği;fixture1440x1000,76pxrail,KİŞİarama,Finansaktiflik,375/320drawer,arama sonrası içerikodağı/locktemizliği ve consoleerror0 PASS. CanlıOIDC/gerçekcihaz/backend testleri bu tur koşulmadı. Başlangıçindeks/manifest/lock/next-envkorundu,diffcheckPASS;fixture/sekme kapalı,viewportreset. Bu tur1/1=%100,00;sidebar8/8=%100,00korunur;ürünroadmap değişmez. Tekagentverify/docs;sonraki kullanıcıincelemesi.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage21.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%29/kalan%71 reset21.10.2026 16:14:02;10080dk kullanılan%36/kalan%64 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## BAKIM —21.10.2026 — Sidebar alt alan seçimi — TAMAMLANDI

**Sidebar/drawer footer Yönetim alanı seçim bağlantısı kaldırıldı.** WorkspaceShell.tsx içinde tek ortak render ve kullanılmayan import kaldırıldı;profil,ana Yönetim Alanları menüsü ve Dashboardheaderseçimi korunur. **184/184FE,typecheck/üretim build,diffcheck PASS.** Bu küçük kaldırma için yeni test yazılmadı;tarayıcı/backend testleri bu tur koşulmadı. Başlangıçindeks/manifest/lock/next-env korundu;build üretimi exactimport kontrolüyle geri alındı. Yeni paket/API/DB yok. Bu tur1/1=%100,00;sidebar8/8korunur. Tekagent verify/docs;sonraki kullanıcıincelemesi.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage21.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%3/kalan%97 reset21.10.2026 16:14:02;10080dk kullanılan%32/kalan%68 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —20.10.2026 — Dashboard sütun grafiği — TAMAMLANDI

**Gelir/gider trendi referanstaki gibi yan yana mavigelir/pembegider sütunlarına dönüştü.** Son6/3ay seçimi mevcut6ayAPI verisini dilimler;backend/tutarhesabı değişmedi. Yuvarlak eksen,klavye/hover/touch kesinTRYtooltip ve ekranokuyucu tutar tablosu korunur. TrendChart.tsx/trend-bars.ts,DashboardPage,globals.css,TR/EN ve trend-bars.test.mjs güncellendi/eklendi. Yeni paket/font/asset yok.

**184/184FE,0fail/skip;typecheck/üretim build PASS.** Yeni2geometri testi:sıfır/küçük/büyüktutar ekseni,240–1100px/3–6ay çakışmayan ikilisütun. GerçekReact/testAPIfixture1440px6ay12sütun,3ay6sütun/3tablosatırı,TabilekesinTRY,320pxchart262/doc320taşma0,consoleerror0 PASS. [Görsel](../navigation-refactor/DASHBOARD_TREND_BARS.jpg). Bu fixturecanlıOIDC/APIkabulü değildir;backendintegration bu saltfrontend turunda tekrar koşulmadı. Başlangıçindeks/manifest/lock/next-env ve diğer kullanıcıfarkları korundu;diffcheckPASS;fixture/sekme kapalı,viewportreset. Tekagent mevcutverify/docs;bu tur1/1=%100,00,sidebar8/8korunur;yeni faz yok.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%97/kalan%3 reset20.10.2026 23:36:32;10080dk kullanılan%31/kalan%69 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir. Sonraki:kullanıcıincelemesi.

## BAKIM —20.10.2026 — Dashboard fotoğraflı banner — TAMAMLANDI

**Alan combosu headerda dil önüne taşındı;yalnız Dashboard filtrelenir. Fotoğraflı banner,önplan alan/marka/gerçeközet ve Europe/Istanbul saat/tarih paneli uygulandı.** Yönetici fotoğrafı mevcut özelbelgeupload ile ALL_MEMBERS olarak ortak kaydeder;ikinci üye aynıgörseli alır. Yeniendpoint/tablo/migration/bağımlılık yok;mevcut özgünkonutasset yeniden kullanıldı. Hava sağlayıcısı yok:sıcaklıkverisi yok durumu. [Teslim/sözleşme/geri alma](../navigation-refactor/DASHBOARD_HERO.md),[masaüstü](../navigation-refactor/DASHBOARD_HERO_DESKTOP.jpg),[mobil](../navigation-refactor/DASHBOARD_HERO_MOBILE.jpg).

**182/182FE,23/23izolePGstorageintegration(17communication+6dashboard),0fail/skip;typecheck/build PASS.** Yeni5FEsenaryo ve1ortakfotoğrafintegration. GerçekReactfixture1440/375/320,pickerönceliği,48→geri96,eskikart/fotoğraftemizliği,UIJPEGhazırlama/save/BlobSHA,consoleerror0 PASS. FixtureAPIUIakışı gerçekpersistenceyerine sayılmaz;ikinciüye/replay/yetki/alan kanıtı gerçekizolebackendtestidir. TestDB/rol/fixturebucket temiz,fixture/sekme kapalı,viewport reset. CanlıOIDC/gerçekcihaz/tamhavaservisi kabulü DOĞRULANAMADI. Bu tur1/1=%100,00;sidebar8/8=%100,00 korunur;İŞ20bakım/gerçekUATgörevleri tiklenmedi. Ürün genel yolharitası yüzdesi BİLGİ MEVCUT DEĞİL.

Başlangıç indeks/manifest/lock/proje/migration/lisans/next-env korundu;build üretimi yalnız exactimportfarkıyla geri alındı. Diğer kullanıcı farkları korunur;commit/push/deploy/reset yok. Üretimbackend değişmedi;backend yalnızCommunicationApiTests ekidir. Frontend DashboardArea/area-selection/DashboardHero/banner,DashboardPage,WorkspaceShell,SessionControls,return-path,CSS,TR/EN,fixture/test vephotoasset;API/frontend/lisans/progress belgeleri güncellendi. OIDC dönüşüne yalnız `/dashboard?property=tekUUID` allowlist eklendi,negatif parametre testleri var. Tekagent contract/security/verify/docs/license;imagegen yalnız okundu,üretim yapılmadı.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%82/kalan%18 reset20.10.2026 23:36:32;10080dk kullanılan%29/kalan%71 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir. Sonraki:kullanıcıincelemesi,yeni kapsam kendiliğinden açılmaz.

## BAKIM —20.10.2026 — Dashboard kompakt KPI — TAMAMLANDI

**Dashboard özet kutuları daha kompakt:** globals.css içinde desktop padding18→14px,mobil14→12px,gap12→8px,ikon44→32px (yalnız KPI),ikonSVG20→17px,değer22→20px;mobil değer17px kalır. Kolon/veri/API korunur. **Typecheck ve üretim build PASS;1440/375px gerçek üretimCSS+temsiliKPI markup önizlemesi PASS**, desktop kart136,5px,padding14px,gap8px,ikon32px,değer20px;mobilpadding12px,ikon32px;docscrollWidth viewport ile eşit. [Görsel](../navigation-refactor/DASHBOARD_COMPACT.jpg). Bu statik stil kabulüdür; gerçek kimlikli Dashboard/API kabulü değildir. Unit/integration bu yalnız CSS turunda tekrar koşulmadı; yeni test yazılmadı. Diff/başlangıçindeks/manifest/lock/next-env koruması PASS. Fixture/sekme kapalı,viewport reset. Yeni bağımlılık/asset yok;genel dashboard-icon diğer kullanımları korunur. Bu tur1/1=%100,00;sidebar8/8=%100,00 değişmez. Tekagent verify/docs;iptal edilen header alan seçicisi başlatılmadı.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%44/kalan%56 reset20.10.2026 23:36:32;10080dk kullanılan%22/kalan%78 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —20.10.2026 — Sağ üst profil — TAMAMLANDI

**Sağ üst header kullanıcı adı/rolü/profil menüsü masaüstünde geri getirildi.** SessionControls profileInSidebar parametresi ve desktop gizleme CSS'i kaldırıldı; WorkspaceShell normal SessionControls kullanır. Sidebar footer aynı provider ile korunur. Bu tur WorkspaceShell.tsx,AuthGate.tsx,globals.css ve ilgili teslim/progress belgeleri değişti; yeni paket/auth/API/DB değişimi yok. **177/177FE,typecheck/build PASS;1440px fixture header ad/rol ve Hesap/API/Çıkış menüsü PASS.** Gerçek OIDC/çıkış çalıştırılmadı; mobil davranış bu tur yeniden kabul edilmedi. [Görsel](../navigation-refactor/HEADER_PROFILE_RESTORED.jpg). Başlangıç indeks/manifest/lock/next-env korundu, fixture/sekme kapalı. Bu tur1/1=%100,00; sidebar8/8=%100,00 değişmez. Tekagent verify/docs; yeni faz yok.

Token giriş/çıkış/toplam/kalancontext: her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%27/kalan%73 reset20.10.2026 23:36:32;10080dk kullanılan%20/kalan%80 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama08 —20.10.2026 — TAMAMLANDI

**IMPLEMENTATION, TEST_RESULTS ve LICENSE_CHECK teslim edildi; geri alma planı ve açık manuel QA listesi hazır.** [Teslim](../navigation-refactor/IMPLEMENTATION.md), [test özeti](../navigation-refactor/TEST_RESULTS.md), [lisans kontrolü](../navigation-refactor/LICENSE_CHECK.md). Bu tur yalnız belge; bağlantı/sayaç/yönerge/kaynak-dependency/indeks koruması ve diff check PASS. Uygulama testleri tekrar koşulmadı; Aşama07'nin177/177FE,277/277unit,83/83seçiliintegration ve typecheck/build sonuçları tarihli kanıt olarak aktarıldı. Yeni paket/asset/API/DB/auth/rol değişimi yok. Gerçek OIDC/iş ekranı/offline ağ/native/ekranokuyucu/aktif reduced-motion kabulü DOĞRULANAMADI; mevcut scopedSMS dönüş kısıtı kayıtlı.

**SIDEBAR8/8=%100,00 teknik teslim; bu tur1/1=%100,00.** İŞ07.1 hata altyapısı ve ayrı UI/ürün roadmap sayaçları değiştirilmedi; bütün ürün yol haritası güncel yüzdesi BİLGİ MEVCUT DEĞİL. Yeni faz başlatılmaz. Mevcut staged/unstaged/untracked kullanıcı farkları korundu; commit/push/deploy/reset/clean yok. Tek agent phase/docs/license; yeni skill/MCP/agent yok.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codex usage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%21/kalan%79, reset20.10.2026 23:36:32;10080dk kullanılan%19/kalan%81, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama07 —20.10.2026 — TAMAMLANDI

**177/177 frontend,277/277 unit,83/83 seçili izole PostgreSQL integration; typecheck/build PASS.** Lint scripti yok. Fixture modül/geri-ileri/doğrudan ikinci alan/mobile drawer smoke PASS; gerçek kimlikli iş ekranları/OIDC ve ağ kesintisinde offline kabulü DOĞRULANAMADI. [Rapor](../navigation-refactor/STAGE_07.md). Bu tur uygulama kodu değişmedi; DB/rol fixture temizlendi, sunucu/sekme kapalı. Başlangıç indeks/manifest/lock/next-env/.env korundu. Aşama07 1/1=%100,00; SIDEBAR7/8=%87,50; ürün roadmap değişmez. Aşama08 onayı beklenir.

Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%14/%86 reset20.10.2026 23:36:32;10080dk%18/%82 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## 20.10.2026 — SIDEBAR FAZ07.1 Aşama06

Footer ortak profil/rol ve mevcut modül alan seçimi eklendi; stale subject guard ve accountChanged eşliği.177FE/typecheck/build, fixture desktop/rail/mobile PASS. Yeni paket/API/DB/yetki yok. [Teslim](../navigation-refactor/STAGE_06.md).

## 20.10.2026 — BAKIM: Menü font eşliği

Sidebar ve üst arama ortak12px tokenıyla eşitlendi;17desktop/17mobil computed12px ve typecheck/build PASS. Paket/route değişmedi.

## 20.10.2026 — SIDEBAR FAZ07.1 Aşama05

Mobil drawer rota/viewport kapanışı, odak dönüşü ve reduced-motion eklendi.173FE/typecheck/build ve izole beşviewport React/Radix kabulü geçti; gerçekauth/cihaz ve aktif reduced-motion kabulü açık. Yeni paket/auth/API/DB yok. [Teslim](../navigation-refactor/STAGE_05.md).

## 20.10.2026 — SIDEBAR FAZ07.1 Aşama04

Altı semantik grup, tek çalışan Kullanıcılar bağlantısı, görünür menü etiketlerinde Türkçe uyumlu Unicode/çoklu kelime arama ve boş sonuç eklendi.173FE/typecheck/build ve320route matrisi PASS. GerçekUI açık oturum yokluğuyla doğrulanamadı. Yeni paket/API/DB yok. [Teslim](../navigation-refactor/STAGE_04.md).

## 19.10.2026 — SIDEBAR FAZ07.1 Aşama03

Mevcut marka tokenlarıyla272/76px sidebar, aktif sol vurgu/focus ve Radix tooltip; geniş mod grup kontrolü ve yalnız görünüm tercihi eklendi.169FE/typecheck/build ve gerçekChrome etkileşimleri doğrulandı. Yeni paket/API/DB yok. [Teslim](../navigation-refactor/STAGE_03.md).

## 19.10.2026 — SIDEBAR FAZ07.1 Aşama02

Tipli navigasyon sözleşmesi ve merkezi mevcut görünürlük/route eşlemesi eklendi; WorkspaceShell tüketir. Nested active/duplicate ve rol/flag testleri;167/167 FE,typecheck/build PASS. Yeni paket/API/DB yok; [teslim](../navigation-refactor/STAGE_02.md).

## 19.10.2026 — WEBSITE FAZ09

Website CI kapısı/4releaseunit/HTTPSoriginpreflight, standaloneDockerfile/nonroothealth ve isteğe bağlıCompose3004 eklendi. Adminwebsiteoriginbuildarg/dev.shwebsite ve nestedenv/privateconfigDockerignore koruması. CycloneDXlockSBOM ve mevcutNOTICE toplama; Türkçe işletim/yönetici/teslim raporu. T21.9teknikdone; #30açık kabul görevi ve #20–29gövdeleri/durumları eşitlendi. Üretim/kimlikliUAT kabulü yapılmadı; yeni bağımlılık veya migration yok.

## 19.10.2026 — BAKIM: Tek website girişi

Rol bazlı mükerrer girişler tek Hesabıma Giriş bağlantısında birleştirildi; yeni sekme ve opener koruması. Hero portal kartı kaldırıldı, mobil header düzenlendi. Aynı siteye ait tüm yetkili bağlamlar all audience ile mevcut güvenlik modelinde birleştirildi; eski linkler korundu. WebP optimizer404 düzeltildi. Test kanıtı TESTING.md; faz sayaçları değişmedi.

## 19.10.2026 — WEBSITE FAZ08

Public Yönetim/Sakin girişleri site/audience bağlamıyla mevcut Keycloak akışına bağlandı. Authenticated website context listesi mevcut backend scope modelini kullanır; dönüş allowlist ve logout temizliği, ortak UI hesap değişimi/odak yenilemesi eklendi. T21.8 test kanıtı bağlandı; gerçek Keycloak kabulü bekler. Yeni migration/bağımlılık yok.

## 19.10.2026 — WEBSITE FAZ07

Domain entity/migration, DNS TXT/TLS ve SUPER_ADMIN kapıları, admin domain paneli, güvenli Host çözümleme/SEO ve süreli HMAC taslak önizlemesi eklendi. No-store yayın/restore davranışı korundu. T21.7 teknik kabulü TESTING.md kanıtıyla güncellendi; canlı DNS/TLS ve FAZ06 EİDS/hukuki kabulü açık. Yeni bağımlılık yok. Son devam turunda trailing-dot Host normalizasyonu ve object-src none CSP tamamlandı.

## 19.10.2026 — WEBSITE FAZ06 teknik geliştirme

İlan/fotoğraf/değişmez olay/özel yetki belgesi ve başvuru modelleri, migration/API, Web Sitesi Yönetimi ilan ve başvuru panelleri; public liste/detay ve form proxy'si eklendi. EİDS sağlayıcısı olmadan fail-closed yayın kapısı; privacy sürümü/sunucu challenge/DBkilitli kota/180gün Worker anonimleştirmesi ve süre sonu olayları. Kanıt TESTING.md, API docs/api/listings.md. Gerçek yayın ve işletim kabulü açık; roadmap tiklenmedi. Yeni paket veya gerçek veri/hesap değişimi yok.

## 18.10.2026 — WEBSITE FAZ05

Web Sitesi Yönetimi menüsü, sayfa/blok/tema/medya/önizleme/yayın panelleri ve WebsiteMedia migration eklendi. Hak/kişi izni/metadata/aktif referans ve public SHA proxy güvenliği;409 yenileme/odak düzeltmesi. Kanıt TESTING.md; canlı DB migrationı yapılmadı.

## 18.10.2026 — WEBSITE FAZ 04

Ortak premium rezidans şablonu ve kök demo önizlemesi, iki özgün WebP, tenant logo/görsel eşlemesi, güvenli giriş bağlantısı ve mobil kart/menü eklendi. Gerçek tenantlara demo kayıt taşınmaz. 10/10 test ve HTTP/typecheck/build kanıtı TESTING.md.

## 18.10.2026 — WEBSITE FAZ 03

website/ Next.js public frontend iskeleti (3004), yol tabanlı tenant doğrulama, public SSR, metadata/tema/404/hata sayfaları ve fixture testleri eklendi. Yeni paket/harici asset yok; premium tasarım FAZ 04 olarak kaldı.

## 18.10.2026 — İŞ20 Dashboard ve menü düzeni (T20.1–T20.5)

## PERSON FAZ11 —18.10.2026 — Son teknik teslim / yayın kapıları açık

FAZ10 kullanıcı kabulüyle #15 Done; #16 In review. İki yeni kiracı+sakin/uzaktan malik-çoklu blok HTTP regresyonu ve üç web dosyasında tekrar dene/yenile düğme standardı düzeltmesi tamamlandı. **366/366 seçili izole backend integration,149/149 web,126/126 mobil; web typecheck/izole production build ve mobil üç platform export başarılı.** 30 migration idempotent SQL iki kez geçti; sahte veride yedek/restore8kontrol geçti, fixture kaynakları temizlendi ve canlı servisler korunuyor. Privateconfig3fixture ve releasepreflight29unsafe reddi başarılı. Unit/telemetri/audit/lisans FAZ10 sonuçları yeniden çalıştırılmış sayılmaz.

[Son rapor ve rollout/rollback/UAT listesi](../refactor/person/phase-11-report.md). Teknik6/6=%100; kullanıcı kabulü10/11=%90,91. Mobil29audit kaydı(18high/11moderate), kontrollü anahtar yenileme/geçmiş incelemesi, eksik tam güvenlik/native/lisans kabulü açık: **yayın engelli**. Fixture PostgreSQL17.6 restore başarısı ortak18.6 üretim DR kabulü değildir. Gerçek .env/DB/hesap/rol/servis, bağımlılık ve migration değiştirilmedi; WEBSITE çalışması korunur. Sonraki faz yok; final kullanıcı kabulü beklenir.


Sol menü Dashboard ile başlar, Genel bakış en alta taşındı, alttaki BuildingFlow/slogan/sürüm yazısı kaldırıldı; üst çubukta arama ile dil seçici arasında her yüklemede değişen 8 slogan. Yeni `/dashboard`: karşılama, alan seçici, her yüklemede değişen 6 projeye ait SVG arka plan, 8 kutucuk (bölüm, dolu, boş, tahsilat, geciken borç, açık talep, yaklaşan ödeme, planlı bakım boş), gelir-gider trendi, bölüm dağılımı ve yaklaşan işlemler. Yeni API `GET /api/v1/properties/{id}/dashboard`. Planlı bakım modülü T20.6 olarak açık. Yeni paket/görsel yok.

## 17.10.2026 — İŞ19 ek 3: yumuşak renkli düğme standardı

Kullanıcının seçtiği "A · Yumuşak renkli" biçim: `Button` için `view` (açık mavi: görüntüle, yenile, tekrar dene), `edit` (turkuaz), `neutral`/`outline` (gri: vazgeç, kapat, temizle, sayfalama), `warn` (amber: parola, kod yenile, ilişki bitir), `danger` (açık kırmızı: sil, bağı kaldır, geri çek) ve `field` (form benzeri seçim) varyantları; renkler `--action-*` token'larında. Bütün ekranlardaki düğmeler anlamına göre bağlandı; dolu kırmızı yalnız onay penceresinde. Test `buttons.test.mjs`; yeni paket yok.

## 17.10.2026 — İŞ19 ek 2: telefon ve e-posta giriş standardı

Ortak `PhoneInput`/`EmailInput` ve `lib/contact.ts`: telefon +90 ve 3-3-2-2 biçiminde, yönlendiren örnek ve uyarıyla; e-posta küçük harf, e-posta klavyesi ve uyarıyla. Hesap bilgilerim, kişi formu, kullanıcı formu, SMS gönderimi ve mobil profil telefonu bu kurala bağlandı. Test: web 126/126, mobil 108/108, build.

## 17.10.2026 — İŞ19 ek: kişilerde fotoğraf, renkli roller, standart düğmeler

Kişiler listesinde (masaüstü ve mobil kart) ve kişi sayfasında bağlı hesabın fotoğrafı (`linkedPhotoId`). Ortak `RoleBadge`/`ToneBadge`: roller her ekranda aynı renk ve ikonla (Kullanıcılar, Hesap bilgilerim, Kişiler, kişi ilişkileri, daire ayrıntısı). Hesap bilgilerim düğmeleri birincil (mavi) ve silen eylemler kırmızı; rıza durumları renkli rozet. Test: backend 184/184 + 520/520, web 123/123, build.

## 17.10.2026 — İŞ19 Hesap bilgilerim ve kullanıcı fotoğrafı (T19.1–T19.5)

Merkezde "Hesap bilgilerim" (`/account`): profil fotoğrafı ekle/değiştir/kaldır (önce açık rıza, WEB kanalı; tarayıcıda 512 px JPEG), ad/soyad kuralı ve telefon, kullanıcı adı, e-posta, roller, bağlı alanlar, Keycloak ile parola değiştirme, Rızalarım. Sağ üst menüde fotoğraf ve "Ad SOYAD", menüde Hesap bilgilerim. Kullanıcılar listesinde ve kişi sayfasında bağlı hesabın fotoğrafı. API: `/api/v1/account/*`, `/users/{id}/photo`, `/properties/{p}/people/{id}/photo` ([account.md](../api/account.md)). Fotoğraf mobil ile ortak; yeni tablo, migration ve paket yok. Giriş dönüş listesine `/users` ve `/sms` de eklendi. Test: backend 184/184 birim, 520/520 entegrasyon; web 123/123, typecheck/build. T19.6 gerçek giriş kabulüyle açık.

## 17.10.2026 — BAKIM: ad/soyad yazım kuralı, mobil ana sayfa adı, bildirim testi

Soyad büyük harf, ad ve ikinci ad yalnız ilk harfi büyük kaydedilir (Türkçe harf kuralları; yabancı uyruk dil bağımsız; tüzel unvan değişmez): yönetim kişi kaydı, merkez kişi profili, merkez kullanıcı, mobil kayıt/kişi bağlama ve mobil profil. Mobil ana sayfa selamı "Ad SOYAD" gösterir ve girişten hemen sonra adı merkezden okur. Rıza onayından sonra fotoğraf seçicinin iOS'ta açılmaması düzeltildi (16.10). Bildirim okuma zamanı mikrosaniyeye kırpıldı; MobileNoticeTests yeşil. Testler: backend 184/184 + 504/504, web 119/119, mobil 106/106.

## 16.10.2026 — İŞ18 mobil rıza ekranları (T18.1–T18.5)

Mobil `src/consents/`: kısıtlı biçim ayrıştırıcısı ve RN `Text` çizicisi (WebView yok), tam ekran rıza sayfası (kutu işaretlenmeden onay pasif), fotoğraf seçmeden önce rıza (profil ve talep), şifreli SQLite şema 8 rıza önbelleği/kuyruğu, kuyruğun talep ve profil eşitlemesinden önce gönderilmesi, 409 eski metinde yeniden sorma, 403 `BF_CONSENT_REQUIRED` alan fotoğraflı işin bekletilip onayla yeniden denenmesi, açılış kapısı ve Profil → Rızalarım (durum, sürüm, tarih, oku, geri çek). Geri çekmede yerel fotoğraflar silinir. Yeni paket yok. Testler: mobil 105/105 (rıza 13/13), build tamam, gerçek API yanıtlarıyla kontrol.

## 16.10.2026 — İŞ17 ek: varsayılan rıza metinleri

Migration `20261016143930_DefaultConsentTexts`: KVKK aydınlatma, profil fotoğrafı ve talep fotoğrafı için özgün taslak v1 metinler (yalnız metni olmayan türe). Metinler `docs/api/default-consent-texts.md`; merkezden yeni sürümle değiştirilebilir. Test: varsayılan metin 1/1, backend 170/170 birim ve 507/508 entegrasyon.

 zorunluluğu ve kullanıcı rıza API'si (T17.1–T17.5)

`GET /api/v1/mobile/consents` ve `POST /api/v1/mobile/consents/commands` (APPROVE/WITHDRAW, UUID ile tekrar güvenli, geçmiş döndürmez). Profil ve talep fotoğrafı REPLACE için ACTIVE `PROFILE_PHOTO`/`REQUEST_PHOTO` rızası zorunlu (403 `BF_CONSENT_REQUIRED`); KEEP/REMOVE ve fotoğrafsız talep serbest. Geri çekmede profil fotoğrafı RETIRED olup bakım işinde silinir; talep fotoğrafı silinmez, yalnız sahibinin mobil görünümünden gizlenir. Talep detayına `photoConsent`; web kişi sayfasında Açık rıza bölümü, talep detayında rıza sürümü veya geri çekme tarihi. Yeni kodlar `BF_CONSENT_REQUIRED`, `BF_CONSENT_TEXT_OUTDATED`, `BF_CONSENT_NOT_WITHDRAWABLE`. Migration ve yeni paket yok. Testler: rıza zorunluluğu 6/6, tüm backend 170/170 birim ve 506/507 entegrasyon (bilinen MobileNotice hatası), web 117/117, typecheck/build.

## 16.10.2026 — İŞ16 rıza altyapısı ve merkez Açık Rıza menüsü (T16.1–T16.5)

`consent_types`, `consent_texts`, `user_consents` tabloları ve `20261016130236_Consents` migration'ı (üç sistem türü seed). Projeye ait kısıtlı metin biçimi (HTML yok) ve ortak test vektörleri; sürümlü, değişmez metin yayını; SHA-256 rıza kanıtı. `/api/v1/consents` uçları ve 8 BF_CONSENT_* kodu. Web Yönetim → Açık Rıza: türler, metinler (canlı önizleme), onay kayıtları, kişi geçmişi ve yazdırılabilir kanıt belgesi. Yeni paket yok. Testler: rıza 20/20 birim + 18/18 entegrasyon, tüm backend 170/170 + 489/490 (tek hata rıza öncesinde de var), web 116/116.

## 15.10.2026 — BAKIM: BuildingFlowMobil mobil modül olarak depoya eklendi

## 15.10.2026 — İŞ15 mobil kayıt ve kişi kodu eşleşmesi (T15.1–T15.5)

Anonim `POST /api/v1/mobile/registrations` (Keycloak hesabı, kalıcı parola, mobil profil, idempotent, istemci sınırı, isteğe bağlı kod) ve girişli `POST /api/v1/mobile/person-links` (kod + ad/soyad/telefon/e-posta, tek tip ret, 15 dk'da 5 hata kilidi, tek kullanımlık kod, üyelik). Mobilde "Hesap oluştur" ekranı, kodsuz hesap modu ve kişi kodu kartı. Testler: 624/624 .NET, 108/108 web, 91/91 mobil.


## 15.10.2026 — İŞ14 merkez kullanıcı yönetimi (T14.1–T14.5)

Keycloak `buildingflow-admin-api` servis hesabı (view/query/manage-users), Admin REST istemcisi, `/api/v1/users` (liste, oluştur, güncelle, roller, parola e-postası, kişiye bağla/çöz) ve merkez `/users` ekranı. SUPER_ADMIN tüm realm, SITE_ADMIN yalnız kendi alan üyeleri ve sınırlı roller. `KeycloakAdmin__*` yapılandırması compose/dev.sh/production şablonu ve preflight. Testler: 616/616 .NET, 108/108 web.


## 15.10.2026 — İŞ13 SMS modülü (T13.1–T13.5)

Property başına SMS hesabı (şifreli secret), kontör defteri, tipli şablonlar (PERSON_REGISTERED, MANUAL), maskeli mesaj geçmişi ve elle gönderim; MOCK sağlayıcı ve worker `SmsDispatchWorker`. Kişi kaydında ve kod yenilemede kayıt SMS'i; kod ve telefon yalnız AES-GCM şifreli kuyruk alanında, gönderimden sonra silinir. Merkez `/properties/{id}/sms` ekranı. Testler: 603/603 .NET, 107/107 web.


## 15.10.2026 — İŞ12 kişi kodu (T12.1–T12.4)

Kişiye 6 haneli, ardışık/tekrarlı desen içermeyen rastgele kod; HMAC-SHA256 karma ve son iki hane saklanır. POST `/people` ve POST `/people/{id}/code` tek seferlik kod döner (`no-store`). Migration `PersonCodes`, eski kişilere migration sonrası kod, `BF_PERSON_CODE_UNAVAILABLE`. Merkez kişi sayfasında maskeli kod, yenileme ve tek seferlik gösterim. `PersonCode__HmacKey` compose/dev.sh/init-local/production şablonu ve preflight. Testler: 573/573 .NET, 104/104 web.


Ayrı yerel klasörde (`buildingflow-mobile`, GitHub remote yok) duran Expo uygulaması `mobile/` altına taşındı: uygulama `mobile/buildingflow-mobile`, 15 fazlık plan `mobile/buildingflow-mobile-prompts`, faz raporları `mobile/docs`, tasarım `mobile/design`. Göreli bağlantılar korunsun diye klasör düzeni aynen alındı; yalnız `scripts/mobile-api.sh` backend kökünü artık depo kökü olarak bulur. Mobilin kendi `.github` iş akışı kök `quality.yml` içinde `mobile` işi olarak birleştirildi. Mobil git geçmişi (tek "Initial commit") ve `node_modules`/`ios`/`dist`/`.env` taşınmadı. Doğrulama (Linux, Node 22): `npm ci`, `npm run check` (typecheck + lint + **54/54** test), `project:test` **7/7**, `expo export` geçti. Uygulama kodu ve yol haritası değişmedi; yeni ürün fazı açılmadı.

## 11.10.2026 — BAKIM: Genel Bilgiler / Daireler frontend detay alanları


## 11.10.2026 — BAKIM: Daire/kişi bağlantıları ve tablet

Güncel kiracı/malik bilgisi için yetki korumalı bounded batch People read (Application/Store/PeopleEndpoints); daire GET listelerinde tam UnitId filtresi (Hierarchy/Store/direkt+binalı endpoints). Yeni UnitPeople/unit-links, HierarchyRows kolon/kart bağlantıları; PersonPage iletişim özeti, PersonOccupancies ve RecordDetails bölüm bağlantıları; ManagementPage+unit route query, auth return-path/AuthGate, strict management DTO, RecordList tablet breakpoint ve TR/EN. İŞ04 mevcut görev kanıtı güncellendi; yeni faz/payda yok. PeopleApiTests negatifleri +13 UnitPeopleNavigation PG senaryosu +8 FE davranış testi. **464.NET/99FE**, typecheck/build,331 lisans,28/28 koruma; tablet/telefon/masaüstü kabulü ve3 screenshot. API yerelde yeniden açıldı, Chrome tablet önizlemesi açık. Şema/live business mutation/commit/merge/push yok. Belgeler: people/hierarchy/frontend-rules/FE AGENTS/licenses/TESTING/progress. Kanıt [TESTING.md](../TESTING.md).

`features/property-details` ortak alan/seçenek/payload/doğrulama ve DetailInputs/DetailValues; EntityEditor, Property oluştur/detay, Building detay ve HierarchyRows/RecordDetails yeni nullable alanları okur/yazar/gösterir. RecordList TanStack columnVisibility; net alan/oda düzeni ve seçilebilir diğer özellikler, mobil kartlar, TR/EN, scope/DTO doğrulama. Altı yeni anlamlı test ve mevcut İŞ03 roadmap kanıtı; hierarchy/frontend-rules/AGENTS/lisans/ilerleme belgeleri ve dört ekran kanıtı.

**91/91 FE, typecheck/build, 331 lisans, TR/EN ve 390 px tarayıcı kabulü geçti; 149 korunan dosya aynı. Bu tur3/3 = %100.00, genel83/83 değişmedi.** BE/DB/migration ve bağımlılık manifesti değiştirilmedi; önceki451 .NET bu tur yeniden koşulmadı. Canlı başarılı kayıt mutation'ı yok; commit/merge/push yok. İŞ11 incelemesinden resmî MIT action lisans metinleri korunup bağlandı. Ayrıntı [TESTING.md](../TESTING.md).

## 11.10.2026 — BAKIM: açılışta otomatik migration ve apartman/daire kolonları

`DatabaseMigration` (advisory lock, entity-migration uyum kontrolü), API `Database:AutoMigrate`, `db.sh sync`, `dev.sh api` entegrasyonu, 6 yeni otomatik migration testi. `ApartmentAndUnitDetails` elle eklenmiş kolonlara karşı idempotent; 8 eski yükseltme testi `LegacySchema` ile düzeltildi. Ana DB yedeklendi ve migration API açılışında uygulandı. **451 .NET geçti.** Dal `feature/apartment-unit-details`; push/merge yok.

## 11.10.2026 — main birleştirme ve yerel servis güncellemesi

`faz-10-11-release` → `main` (`--no-ff`, push yok). main'de ci.sh 426 .NET / 85 FE geçti. Ana DB'de bekleyen migration yoktu, DB'ye yazılmadı. Worker yeni kodla yeniden başlatıldı; API kullanıcı tarafından yeniden başlatılmıştı. Smoke 15/15, gözlemlenebilirlik 11/11. Gözlemlenebilirlik sayısı 12/12 → 11/11 düzeltildi.

## 11.10.2026 — İŞ11 adım04 ve faz teslimi: kabul ve yayın rehberi

`docs/RELEASE.md` (preflight, HTTPS/proxy, kurulum/migration, geri dönüş, yedek, SMTP, smoke/kabul), `deploy/production.env.example`, `scripts/release-preflight.py` + 21 negatif varyantlı testi (CI'da). HSTS build-zamanından `proxy.ts` çalışma zamanına taşındı. `test-observability.py` boşta worker log penceresi genişletildi. Sürüm etiketi v0.11, roadmap 4/4, AGENTS/README bağlantıları. **ci.sh: 426 .NET, 85 FE, build, audit, 331 lisans; smoke 15/15, gözlemlenebilirlik 11/11, anonim SSR 30 rota sızıntı 0.** **İŞ11 4/4 = %100.00; genel 83/83 = %100.00.** Push/merge yok.

## 11.10.2026 — İŞ11 adım03: yedekleme ve geri dönüş

`scripts/backup_restore.py` (quiesced PG/Keycloak dump + MinIO cold snapshot, SHA256 manifest, yalnız yeni internal hedefe `restore-check`; init-zamanı readiness yarışı düzeltildi) ve `scripts/test-backup-restore.py` fixture provası (gerçek Keycloak/MinIO, negatifler, temizlik ve canlı compose durumu kontrolü); workflow adımı, [BACKUP_RESTORE.md](../BACKUP_RESTORE.md), roadmap adım03. **Prova 8/8 iki kez geçti.** Canlı veri yedeklenmedi/restore edilmedi; .env/migration/manifest değişmedi.

## 11.10.2026 — İŞ11 adım02: CI ve otomatik kontroller

`scripts/ci.sh` sonuç dizinini sıfırlar, CI kapı testlerini (`scripts/test_ci_gates.py`) ve idempotent SQL provasını (`scripts/check-idempotent-sql.sh`) çalıştırır; `check-test-results.py` TRX kapısı; `.github/workflows/quality.yml` SHA-sabit action'larla. Uygulanmış migration'daki sonlandırılmamış ham SQL nedeniyle bozuk idempotent betik `TerminatedSqlMigrationsSqlGenerator` ile düzeltildi + gerçek PG testi; MIGRATIONS/lisans kaydı. **426 .NET, 85 FE, build, audit, lisans geçti.** Eski migration/.env/manifest değişmedi.

## 11.10.2026 — İŞ11 adım01: güvenlik ve erişim

`faz-10-11-release` dalı açıldı; Faz10 ayrı commit. API `ReleaseSecurity` (production HTTPS/CORS/AllowedHosts doğrulaması, yalnız tanımlı proxy, HSTS/güvenli başlıklar/no-store, anonim/kimlikli okuma/yazma kodlu 429), 28 güvenlik testi; FE `proxy.ts` nonce CSP, `security/policy.ts`, güvenli başlıklar ve test edilebilir locale cookie seçenekleri; roadmap adım01 tiki ve TR/EN metin. **425 .NET, 85 FE, typecheck/build geçti.** DB/migration/.env/manifest değişmedi.

## 11.10.2026 — İŞ11 başladı

Kullanıcı onayı; güvenlik/CI/yedek-restore/yayın kabul kapsamı ve test planı hazırlandı. Faz10 değişiklikleri korunur; yeni göreve henüz tik yok.

## 11.10.2026 — İŞ10 adım04 ve faz teslimi

API/worker güvenli OTLP logları, Loki trace kimliği, Prometheus metrik/TCP health, Grafana iki datasource/8 panel ve `scripts/test-observability.py` canlı kontrolü tamamlandı. Sayaç tarih alanı gerçek takvimle doğrulandı; stale409 sonrası güncel okuma yükleme eklendi. TR/EN ve512px/390px sağ panel tarayıcı kabulü, ekran kanıtı, API/OBSERVABILITY/migration/lisans/AGENTS/README/skill-agent/progress belgeleri ve son roadmap4/4 güncellendi.

**397 .NET,82 FE,typecheck/build,11 canlı smoke,331 lisans,23 korunan dosya ve17 eski DB tablo hash eşliği başarılı.** Yeni idempotent SQL üretildi; uygulama Docker imajları derlenmedi, kullanıcı iş verisine mutation yok. İŞ10 **4/4=%100.00**, genel **79/83=%95.18**. Yeni İŞ11 başlatılmadı; dal faz-09-messaging, commit/merge/push yapılmadı.

## 11.10.2026 İŞ10 adım03

OpenTelemetry1.18 SDK; düşük cardinality request/histogram ve worker/retry/DLQ ölçümleri; Collector/Prometheus/blackbox/Loki compose, Grafana datasource/dashboard, license+NOTICE, dev workerDB düzeltmesi. Gerçek pipeline smoke,397.NET ve331 lisans; yerel upgrade17 eski tablo hash korundu.

## 11.10.2026 İŞ10 adım02

Rapor Application/Store/API, scope/RepeatableRead/SQL LAG, doluluk sayımı ve finans tarih kuralları; ReportsWorkspace ayrı menü, TR/EN/DTO ve testler. Kanıt8 entegrasyon+82FE,typecheck/build.

## 11.10.2026 İŞ10 adım01

Sayaç/okuma/revizyon BaseEntity modeli, EF migration + SQL immutable/Down guard, API UUID/version/scope ve ortak sağ panel/TanStack/TR-EN ekranları. TESTING.md:36 PostgreSQL ve80 FE; typecheck/build. Faz09 hızlı tekrar41/41. Paket değişmedi.

# BuildingFlow çalışma değişiklik geçmişi

Bu dosya yalnız yeni çalışma turlarında gerçekten yapılan değişiklikleri ekleyerek kaydeder. Önceki iş fazlarının ayrıntılı değişiklik/test kanıtı [TESTING.md](../TESTING.md) ve ilgili teslim raporlarında korunur; buraya yeniden kopyalanmaz. Güncel durum [STATUS.md](STATUS.md), devam bağlamı [SESSION.md](SESSION.md).

## 2026-10-09 — Çalışma ve ilerleme protokolü

Onaylı kapsam: yalnız yönerge ve takip belgeleri. Uygulama/DB/API geliştirme fazı açılmadı.

| Dosya | Gerçek değişiklik |
| --- | --- |
| [AGENTS.md](../../AGENTS.md) | Mevcut kuralları koruyan zorunlu başlangıç/ara/sonuç raporları, faz ayrımı, adım durumları ve yüzdesi, kaynaklara bağlı takip, gerçek token/kota ve onay sınırı eklendi |
| [UI planı AGENTS.md](../../frontend/buildingflow-ui-plan/AGENTS.md) | Mevcut ilerleme maddesi kök protokolün Türkçe rapor biçimine bağlandı; kapsam kuralları korundu |
| [STATUS.md](STATUS.md) | Aktif tur ve gerçek yol haritasının tarihli özeti; 13 iş fazı kaydı, 58/78 adım, ayrı 12 UI ve 8 teknik faz bilgisi |
| [CHANGELOG.md](CHANGELOG.md) | Bu gerçek belge değişiklik kaydı başlatıldı |
| [SESSION.md](SESSION.md) | Sonraki tur için onay/kapsam, korunacak kurallar, kaynak ve kullanım bağlamı hazırlandı |
| [TESTING.md](../TESTING.md) | Belge kontrolleri uygulamadan önce planlandı; doğrulanmış sonuçlar ve uygulama testlerinin yeniden çalıştırılmadığı kaydedildi |
| [SKILLS_AND_AGENTS.md](../SKILLS_AND_AGENTS.md) | Bu turun gerçek tek-agent/OpenAI Docs kullanımı ve belge doğrulama sonucu kaydedildi |

**TAMAMLANDI: 3/3, %100.** 84 yerel Markdown bağlantısı, eski kök kuralların içerik/sırası, UI kural bağlantısı, iş/UI/teknik sayaçları, zorunlu rapor alanları ve whitespace kontrolü geçti. Başlangıç hashleriyle **315 mevcut dosyanın korunması ve yalnız 7 izinli Markdown farkı** doğrulandı.

Bu tur uygulama kodu, yapılandırma, migration, veritabanı, .env, npm/NuGet bağımlılıkları veya çalışma süreçleri değiştirilmedi. Önceki kapsamlı çalışma ağacı korundu; commit/push yapılmadı. Uygulama testleri/restore/lisans taraması yeniden çalıştırılmadı; önceki test sonuçları bu tura mal edilmedi. Yeni geliştirme fazı başlatılmadan kullanıcı onayı beklenir.

## 2026-10-09 — İŞ 07.1 merkezi API feedback

Onaylı analiz/kodlama: merkezi ErrorCatalog, kod/mesaj/parametre/önem seviyesi ve TR/EN sağ üst bildirim. Beş resmi adım; mevcut iş 07/UI 07 ayrı.

- 1/5 adım doğrulandı; 86 beklenen mesaj merkezi 98 kod kataloğuna taşındı; analize ve TR/EN sözlüklere bağlandı. Backend/FE testleri henüz çalıştırılmadı.

- Adım 04 kaydı (3/5): Backend 9/9 kodlu HTTP/alan/redaksiyon testi; FE 56/56 kod/parametre/TR–EN/timer/dedup testi ve typecheck başarılı. Lisans 312 paket sürümünde PASS. Tam backend regresyonu ve tarayıcı henüz bekliyor.
- Adım 04 kaydı (3/5): Tam .NET regresyonu 252/252 (47 unit, 205 integration, atlanan 0), 56 FE testi ve production build başarılı; 9 yeni HTTP sözleşme testi dahil. Tarayıcı kabulü sürüyor.
- Adım 04 kaydı (3/5): 252 .NET testi ve FE kod/parametre/timer sözleşmeleri geçti; bildirim kapatma/panel odağı düzeltmesi yapıldı. Son typecheck başarılı; yeni yerel oturumla tarayıcı kabulü devam ediyor.
- Adım 05 kaydı (4/5): Tarayıcı TR/EN sağ üst error/warning, gerçek kodlu 404/dedup/retry, 390 px taşmasız görünüm, bildirim odağında duraklama ve kapatınca panelin açık kalması doğrulandı. Son regressyon/build ve belge teslimi sürüyor.
- Adım 05 kaydı (5/5): 252/252 .NET, 59/59 FE, typecheck/build, 312 lisans, TR/EN/mobil tarayıcı ve 19 korunan dosya kontrolü başarılı. İŞ 07.1 5/5 tamamlandı; yeni faz onayı bekleniyor.

### İŞ 07.1 teslim — 10.10.2026

- Backend: Application/ErrorCatalog.cs (98 kod), Properties/Hierarchy/People/Dues/Payments validator'ları ve Infrastructure Hierarchy/People/Dues/PaymentStore beklenen mesajları; Api/ApiProblems.cs, ExceptionHandler.cs, Program.cs tek kodlu ProblemDetails sözleşmesi. ErrorContractTests.cs 9 yeni senaryo. Domain/DB/migration değişmedi.
- Frontend: services/error-codes.ts, problem.ts, authorized-request.ts ve api.ts; feedback Context/Notifications/model/text, MessageBanner/FieldError, ortak ui/dialog.tsx ve globals.css. Code/parametre TR/EN apiErrors/apiFields, locale sonrası gruplu alan çevirisi ve test edilebilir abort/refresh/body transport. Yeni paket kurulmadı; mevcut Radix branch kaynakları kullanıldı.
- Takip: milestones.ts 07.1 beş doğrulanmış görev; 14 faz/63–83 adım. TESTING, analiz, api/errors/README, frontend-rules, kök/frontend AGENTS, SKILLS_AND_AGENTS ve progress kayıtları. Tarayıcı ekran kanıtları (form TR, warning TR/EN, 404 mobil EN/desktop TR ve tamamlanan yol haritası) dokümanlara bağlandı. SESSION içindeki eski sayaç da 63/83 olarak eşitlendi.
- **252 .NET / 59 FE; typecheck/build; 312 lisans; 19 korunan dosya; TR/EN/390 px/focus/dismiss/404/dedup kabulü başarılı.** Sandbox soket/SWC engeli izinli yeniden çalıştırmayla aşıldı. İlk errors JSON, UTF-8/locale template ve bildirim/panel kapanma hataları testle yakalanıp düzeltildi.
- Son teslim kontrolünde nihai yol haritası koduyla üretim derlemesi, 115 yerel Markdown bağlantısı, 19 korunan dosya ve 14 faz/63–83 adım sayacı doğrulandı. Güncel kullanım okuması SESSION.md'ye yazıldı; sonraki faz başlatılmadı.

Yeni faz başlamadı. Kullanıcı finans/ilişki verisinde başarılı mutation yok; canlı 400/409 başarılı mutation kabulü ve full screen-reader ürünü/OS hover testleri yapılmış sayılmaz. Özel token/context sayıları BİLGİ MEVCUT DEĞİL; hesap kota okuması SESSION içindedir.

## 2026-10-10 — İŞ 07 başlangıç

- Kullanıcı Faz 07 kodlama onayı; dört resmi görev ve finans sözleşmesi analiz edildi. Analiz/kabul planı, STATUS/SESSION oluşturuldu/güncellendi. Koruma hashleri alındı; test sonuçları henüz bekliyor.

## 10.10.2026 — İŞ 07, adım 01/04 tamamlandı

Expense/PropertyBudget/BudgetRevision, Application/EF/store/API ve yeni ExpensesAndBudgets migration/snapshot eklendi. Sekiz ortak NOT NULL alan, immutable revizyon, UUID/fingerprint, scope/version/kilit doğrulandı. 292 .NET test (59 unit + 233 integration), 67 FE test ve typecheck geçti. Tarihsel migration testi kendi hedefini açıkça seçiyor; uygulanmış migration dosyaları değişmedi. Finans endpoint sahte rol/JWT kontrolleri son regresyonda geçti. İlerleme 1/4 %25.00; tarayıcı kabulü sürüyor.

## 10.10.2026 — İŞ 07, adım 02–03/04 tamamlandı

Yeni /expenses ve property/expenses menü/alan seçimi, TR/EN gider listesi/filtre/sayfalama/sağ form/detay; kategori bütçe formu/geçmiş ve gerçek aylık tahsilat/gider/plan hesapları eklendi. BigInt locale para formatı, DTO scope/para/tarih/özet/revizyon doğrulaması, 409/replay ve yedi yeni merkezi hata kodu (toplam105). Ortak SummaryCard dar masaüstü kartı seçeneği ekledi. 1280×900 form sağ 512px, ilk alan odağı/tarih-tutar koruma; EN 390×844 panel/sağ üst bildirim ve sıfır plan kabulü doğrulandı. Gider/bütçe kaydedilmedi. Yerel yeni migration/API başlatma ve 12 eski tablo hash eşliği geçti. API/mimari/frontend/AGENTS/migration/README belgeleri aynı sözleşmeye bağlandı. 3/4 %75.00; final frontend derleme ve teslim sürüyor.

## 10.10.2026 — İŞ 07, adım 04/04 teslim

292 .NET (59 unit +233 integration) ve67 FE hata/atlama0; typecheck/build/EF model eşliği/idempotent SQL, 312 lisans, 18 korunan dosya,12 eski tablo veri/audit hash ve yerel API Healthy. API sözleşmesi, kanıt/ekranlar ve AGENTS/standart/README/mimari/migration kayıtları güncellendi. Roadmap dört görev koyu/tikli; faz4/4 %100.00, iş67/83 %80.72. Ana DB'de başarılı finans mutation yok; dosya/kategoriCRUD/banka/ödeme emri gelecekteki sınırlar. İŞ08 ve ayrıUI07 onaylanmadı, başlatılmadı.

## 10.10.2026 — İŞ 08 başladı

Kullanıcı onayıyla duyuru/dosya kapsamı analiz edildi; STATUS/SESSION ve PHASE_08_ANNOUNCEMENTS_DOCUMENTS.md hazırlandı. Henüz uygulama veya yeni test kabulü tamamlanmadı.

### İŞ08 adım01/02 — TAMAMLANDI

Announcement/StoredDocument, EF ortak audit/composite FK ve metadata migration; CommunicationService/Store/Endpoints; özel Minio7.0.0 sağlayıcısı; bağımsız Duyurular/Belgeler menüleri, sağ panel ve TR/EN; ortak transport multipart/blob. 317 .NET,76 FE,typecheck/build,316 lisans geçti. Yerel MinIO IPv4 düzeltmesi; SDK LICENSE container dağıtımına dahil edildi.

### İŞ08 adım03 — TAMAMLANDI

10 ek sınırı ve yayın/yükleme yarışı, beklenmeyen bucket politikasının reddi gerçek MinIO’da doğrulandı; yeni27 test geçti. Migration yerelde uygulandı; API yeni kodlarla yeniden başlatıldı. İlk canlı migration öncesi hash kontrolü Docker socket iznine takıldığı için bu kanıt başarılı sayılmadı; ilk yükseltme ayrı PostgreSQL’de doğrulandı.


### İŞ 08 adım04 — TAMAMLANDI — 10.10.2026

**4/4 resmi görev=%100.00; genel iş71/83=%85.54.** Duyuru ekleri ve özel belge arşivi;9 yeni domain/18 integration/9 frontend testi dahil **319 .NET/76 FE, başarısız/atlanan0**. Son roadmap TR/EN metninden sonra FE test/typecheck/build tekrar geçti;316 lisans,19 korunan dosya SHA256 ve15 eski tablo tekrar-migration hash eşliği doğrulandı. Yerel API5005/FE3003 sağlıklı. TR512px sağ panel/boş form uyarısı, EN390px mobil belge paneli taşmasız; Türkçe/normal viewport geri yüklendi, geçici kabul sekmesi kapandı. Kullanıcı DB'sinde başarılı duyuru/dosya mutation yapılmadı; ilk canlı upgrade öncesi hash eksikliği açıkça TESTING/SESSION'da kaydedildi.

Etkilenen dosyalar: backend Announcement/StoredDocument, Application Communication/ErrorCatalog, Infrastructure CommunicationStore/Configurations/MinioFileStorage/DI/DbContext/migration+snapshot, API CommunicationEndpoints/Program/ExceptionHandler/ApiProblems; iletişim unit/API/migration testleri ve mevcut factory/ExpenseMigrationTests; FE features/communication, bağımsız rota/menüler, ortak authorized-request/useAuthorizedApi, problem/error-codes, TR/EN sözlükler ve communication/api-feedback testleri; scripts/dev.sh/test.sh, Infrastructure/UnitTests csproj ve API/Worker Dockerfile lisans kopyaları. Son teslimde milestones.ts dördüncü görevi, TESTING/progress, API errors/communication, core/MIGRATIONS/README/AGENTS/Convention/skill-agent/lisans kayıtları ve gerçek UI screenshot kanıtları güncellendi. Minio7.0.0 SDK lisansı resmi kaynaktan korundu; FE manifest/lockfile/eski uygulanmış migration kaynakları değiştirilmedi. İŞ09/UI yeni aşama başlatılmadı.


## 10.10.2026 — İŞ09 başladı

Kullanıcı fazı başlatmayı onayladı; SMTP seçildi, SMS sağlayıcı ertelendi. Kök kurallar/progress/mimari/RabbitMQ/Worker ve ayrı UI planı incelendi; PHASE_09_MESSAGING analizi, TESTING ön planı, STATUS/SESSION güncellendi. Uygulama görevi henüz tiklenmedi.

## 2026-10-11 — İŞ 09 adım01: mesaj sözleşmeleri ve RabbitMQ yayıncı

Dal `faz-09-messaging`. Application `Messaging.cs` (3 sürümlü sözleşme, zarf, doğrulama, bounded JSON), Infrastructure `RabbitMessaging.cs` (durable quorum topoloji, confirm+mandatory yayıncı), 20 unit + 6 RabbitMQ integration testi; RabbitMQ.Client 7.2.1 lisans metni/envanter, roadmap adım01 tiki ve TR/EN doğrulama metni, analiz/TESTING/STATUS/SESSION güncellendi. **345 .NET, 76 FE, typecheck, 318 lisans geçti.** DB/migration/.env değişmedi.

## 2026-10-11 — İŞ 09 adım02: Outbox ve tüketiciler

BaseEntity `outbox_messages`/`processed_messages` + migration/snapshot, EfMessageOutbox, OutboxRelay, MessageProcessor, RabbitMessageConsumer, Worker relay/consumer host'ları (FoundationWorker kaldırıldı), compose worker DB bağlantısı, 10 yeni PG+RabbitMQ testi; RabbitMQ test koleksiyonu ve sağlam temizlik; communication migration testi yeni geri alma sırasına uyarlandı. **355 .NET, 318 lisans geçti.** Yerel ana DB/.env değişmedi.

## 2026-10-11 — İŞ 09 adım03: Retry, dead-letter ve idempotency

Gecikmeli TTL retry kuyrukları, deneme limiti, `x-buildingflow-attempt/failure` başlıkları, confirm'li retry/DLQ yayınından sonra ACK, ana kuyruk delivery-limit/at-least-once DLX; 5 RabbitMQ + 9 politika testi, test temizliği retry kaynaklarını kapsar. **369 .NET, 318 lisans geçti.** DB/migration/.env değişmedi.

## 2026-10-11 — İŞ 09 adım04: SMTP e-posta ve faz teslimi

`SmtpOptions`/`SmtpEmailHandler`, kalıcı teslim hatası için doğrudan DLQ, Worker SMTP kaydı, compose worker Mailpit ayarları, `scripts/test.sh` geçici Mailpit; 5 SMTP/uçtan uca test; analiz yapılandırma bölümü, roadmap 4/4. **374 .NET, 76 FE, typecheck, 318 lisans geçti.** Yeni paket, DB değişikliği veya .env değişikliği yok. **İŞ09 4/4 = %100.00.**

## 2026-10-11 — BAKIM: Sabit menüler, geniş Okumalar ve tüketim toplamı

WorkspaceShell/globals.css:100dvh sabit çerçeve, orta scroll, sol/üst/alt menü, mobil320px sol Dialog, kapsam/rol korunan alt kısa yollar ve skip-link. MetersWorkspace: Okumalar sağ paneli896px; diğer form panel genişlikleri korunur. ReportsWorkspace/lib/format.ts: ayrı üç tür toplam kartı, BigInt üç ondalık formatter, fatura/gider açıklaması ve mevcut Expense bağlantısı. TR/EN sözlükleri, iki report testi ve mevcut roadmap doğrulama anahtarları güncellendi; frontend-rules/API açıklaması/TESTING/progress ve altı ekran kanıtı. **101FE,typecheck/build,331lisans,196/196koruma ve TR/EN masaüstü/tablet/mobil kabulü geçti.** API/DB/finans modeli, .env, paket/lock ve mevcut Next config farkları değişmedi; kaynakta otomatik maliyet/tarife yok. .NET tekrar çalıştırılmadı; canlı iş verisi mutation/commit/merge/push yok.

## UI03 sol/üst menü teslimi — 12.10.2026

**Onaylanan dar kapsam 3/3 adım, %100.00 tamamlandı.** Koyu lacivert248px sol menü, mavi aktif satır,58px üst bar, menü arama dialogu, gerçek oturum adı/rolü ve profil içinde Keycloak çıkışı. Masaüstü daraltma; mobil mevcut sol Dialog ve sabit çerçeve korunur. Yeni paket/DB/API değişikliği yok; tek agent, ek skill yok.

**Bu tur101/101 FE, typecheck/build,331 lisans kontrolü geçti.** Tarayıcı profil menüsünde Çıkış yap görünür; aramada Rapor yalnız Raporlar sonucunu verdi;390×844 mobil panel tüm menüleri gösterdi, yatay taşma0 ve son header58px doğrulandı. Kullanıcının oturumu kapatılmadı; gerçek logout redirect bu tur yeniden çalıştırılmadı. Rol koşulları kod incelemesiyle korundu; ayrı rollerle tarayıcı kabulü yapılmadı. Backend testleri çalıştırılmadı. İlk son kontrol komutu yanlışlıkla kökten çalıştırıldığı için package.json bulunamadı; doğru frontend klasöründen tekrar başarılı.

Görsel birebir eşlik sınırlamaları: referans fotoğraf/logosu yerine Lucide bina işareti ve çizim; avatar gerçek oturum baş harfleri. Bildirim sayısı uydurulmaz; merkez henüz mevcut değildir. API'si bulunmayan menüler Planlandı açıklamalı disabled. Üst arama mevcut modülleri bulur, kişi/daire verilerinde global API araması değildir. UI03 geniş planın breadcrumb/global veri araması tamamlandı sayılmaz; yalnız kullanıcının bu tur istediği menü kapsamı teslim edilir. İş yol haritası83/83 %100.00 korunur; yeni görev/tik yok. UI toplam ilerlemesi BİLGİ MEVCUT DEĞİL.

Kanıt: [Profil ve menü](../screenshots/ui03-navigation-profile.png). Sonraki adım: yeni kullanıcı talebi; diğer ekranlara geçilmedi.

Kota kaynağı Codex usage/rateLimitsByLimitId.codex, teslim öncesi okuma:300dk kullanılan%43/kalan%57, reset12.10.2026 04:04:09 Europe/Istanbul;10080dk kullanılan%38/kalan%62, reset18.10.2026 09:28:33. Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Son build tekrarında SWC native cache sandbox erişim engeli oluştu; izinli `npm run build` tekrarı uygulandı. İlk başarılı build kanıtı ayrı korunur. Son izinli build başarılı; son sözlük/erişilebilir ad değişikliklerinden sonra101/101 FE tekrar geçti.

## BAKIM bayraklı dil teslimi — 12.10.2026

**3/3 %100.00: LanguageSwitcher TR/EN bayraklı; /login ve ana ekran aynı HttpOnly uygulama dil cookie'sini kullanır. Keycloak tema menüsünde bayrak, script ile yalnız whitelist dil tercihi cookie'si; aynı hostname/farklı port yerel girişte dil ana ekrana taşınır.** Kimlik bilgisi/token/auth URL saklanmaz, auth politikası değişmedi. Ayrı auth hostname entegrasyonu bu host cookie mekanizmasıyla desteklenmez; dağıtımda ayrıca ele alınmalıdır.

**102/102 FE, typecheck/build,331 lisans ve git diff --check başarılı.** Tarayıcı /login English→ana ekran en; gerçek Keycloak giriş menüsünde English seçimi→ana ekran en doğrulandı. Kullanıcı parolası girilmedi/oturum kapatılmadı; yeniden parola gönderimli login testi yapılmadı. Test sonrası dil Türkçe'ye döndürüldü. Yeni paket/DB/backend yok; tek agent, skill yok. Çalışma ağacındaki önceki değişiklikler korundu. Geçersiz tercih için whitelist/fallback testi eklendi.

Dosyalar: LanguageSwitcher.tsx, i18n/{locale,actions,request}.ts, globals.css, standards.test.mjs; Keycloak template.ftl/theme.properties/resources/js/language-preference.js; TESTING ve progress kayıtları. İş yol haritası83/83 %100.00 korunur; UI toplam yüzdesi BİLGİ MEVCUT DEĞİL. Sonraki adım yeni kullanıcı kapsamı.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL; oturum tüketimi: BİLGİ MEVCUT DEĞİL.

Son build sandbox SWC önbellek erişim engelinden sonra izinli tekrar ile başarılı tamamlandı.

## BAKIM yol haritası teslimi —12.10.2026

**2/2 %100.00 tamamlandı. Roadmap.tsx ilk00–03 bölümlerindeki varsayılan open kaldırıldı;15 bölüm kapalı başlangıç.** Tarayıcı yeni ana sayfada açık details0→tıklama1→kapatma0; ekran docs/screenshots/roadmap-collapsed.png. Typecheck ve build başarılı; build sandbox SWC önbellek engeli sonrası izinli tekrar geçti. Yeni test eklenmedi/npm test bu tek sunum değişikliğinde tekrar çalıştırılmadı; paket/lock değişmedi. Önceki kullanıcı değişiklikleri korunur. İş83/83 %100.00; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, skill yok. Sonraki işlem yeni talep. Giriş/çıkış/toplam token ve kalan context ayrı ayrı BİLGİ MEVCUT DEĞİL.

## BAKIM menü görselleri teslimi —12.10.2026

**2/2 %100.00 tamamlandı. WorkspaceShell.tsx alt bina dekorasyonu/House import kaldırıldı, globals.css kullanılmayan dekorasyon stili kaldırıldı, next.config.ts devIndicators:false.** Mevcut config ayarları korunur. Typecheck/build geçti; ilk typecheck dev restart sırasında rota tipleri eksik, ilk build SWC sandbox cache engeli: build izinli tekrar, typecheck tekrar başarılı. Tarayıcı dekorasyon0/Next dev düğmesi0; ekran docs/screenshots/sidebar-clean.png. Mobil aynı navigation JSX kaynağı, ayrı mobil kabul bu tur çalıştırılmadı. npm test bu görsel/config değişikliğinde tekrar çalıştırılmadı, yeni paket/lock değişimi yok. İş83/83 %100.00 korunur; UI toplam BİLGİ MEVCUT DEĞİL. Tek agent, ek skill yok. Sonraki işlem yeni kullanıcı talebi.

Giriş token: BİLGİ MEVCUT DEĞİL; çıkış token: BİLGİ MEVCUT DEĞİL; toplam token: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim12.10.2026:300dk kullanılan%73/kalan%27 reset12.10.2026 04:04:09;10080dk kullanılan%43/kalan%57 reset18.10.2026 09:28:33 Europe/Istanbul.

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

Kanıtlar: [API](../api/people.md), [yabancı tüzel form](../screenshots/person-foreign-legal-form.png), [mobil form](../screenshots/person-profile-mobile.png). Sonraki işlem yeni kullanıcı kapsamıdır.

Son kota okuması: 12.10.2026 07:39:22 Europe/Istanbul, Codex usage / rateLimitsByLimitId.codex. 300 dk kullanılan %50/kalan %50, reset 12.10.2026 12:21:12; 10080 dk kullanılan %55/kalan %45, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**; çıkış token: **BİLGİ MEVCUT DEĞİL**; toplam token: **BİLGİ MEVCUT DEĞİL**; kalan context: **BİLGİ MEVCUT DEĞİL**; oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

## BAKIM görsel taslak — Yeni kişi formu (12.10.2026)

**2/2 %100.00 tamamlandı:** kullanıcının ekran görüntüsü incelendi; imagegen skill'i ve built-in image_gen ile iki sütunlu/gruplu sağ panel taslağı üretildi. Kimlik, iletişim, bildirim hesabı; son görselde hesap eşleştirmesi isteğe bağlı olarak düzeltildi. Başlık, zorunlu alanlar ve alt kaydet/vazgeç görsel olarak kontrol edildi. Görsel: docs/analysis/design/person-create-proposal-v1.png. Bu yalnız görsel öneridir; uygulama kodu değiştirilmedi, typecheck/build/API testleri bu görsel turunda yeniden çalıştırılmadı. Yeni paket yok, roadmap tıkları ve83/83 sayacı korunur. Sonraki adım kullanıcının görsel hakkındaki talebidir. Giriş token/çıkış token/toplam token/kalan context: her biri BİLGİ MEVCUT DEĞİL.


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


## Mobil Faz12 — Talep, fotoğraf ve offline — 15.10.2026

Yerel main başlangıcı ea13e59; kullanıcı Faz12’yi onayladı. Beş iş paketi: önkoşul/sözleşme, API/metadata, mobil taslak/kuyruk, hedefli doğrulama, belge/runtime. Kod ve otomatik kontroller teslim edildi; gerçek cihaz/UI kabulü açık (In review). Faz13 başlamadı; merkezin ayrı İŞ/UI/AI sayacı değişmedi.

Kategori, öncelik, isteğe bağlı tek fotoğraf, şifreli otomatik taslak, sabit UUID outbox ve çevrimdışı önizleme; sürümlü own-request kaldırma. Mevcut JPEG/S3/PrivateFiles kullanıldı. Legacy fingerprint ve makbuzlar korundu; composite FK, STAGED/CURRENT/RETIRED/PURGED ve yetkili fotoğraf GET eklendi. SQLite6 fotoğraf BLOB tutmaz; scope, hash, TTL, geç yanıt, yetki iptali ve logout temizliği korunur.

60/60 mobil, 83/83 PG+SeaweedFS ve 3/3 PersistenceTests; sıfır atlama. TypeScript, üç platform export, merkez build ve 329 lisans metadata kontrolü geçti. Mobil lint bir önceki uyarıyı korur; native iPhone build bir Expo script uyarısıyla başarılı ve telefona kuruldu. Migration yerelde uygulandı; kontrol edilen dört tablonun eski veri hash’leri aynı. API5006/Metro8081 main mobil modülünden çalışıyor. Canlı iş/rol/fotoğraf mutation’ı, commit/push veya mağaza yayını yok. Gerçek kamera/galeri/offline UI kabulü yapılmadı. Kanıt: docs/api/mobile-requests.md ve mobile/docs/mobile/PHASE12.md. Token/context/tur tüketimi: BİLGİ MEVCUT DEĞİL.


## Mobil Faz13 — Duyurular ve bildirimler — 15.10.2026

Yerel main üzerinde onaylı Faz13: yayınlanmış audience kontrollü duyuru ve yalnız alıcı kişisel mesaj kutusu; merkezde idempotent okundu, hesap bazında version kontrollü içerik vurgusu tercihi. SQLite7 okundu kuyruğu ve mevcut şifreli snapshotlar;24saatTTL,scope/receipt/revocation/çıkış guard. Derin bağlantı seçili doğrulanmış bağlam ve API yetkisinden geçer. APNs/FCM/Expo sağlayıcısı yok: pushAvailable=false; OS izni/token/push teslimi eklenmedi, açık bağımlılık.

67/67 mobil ve102/102 gerçek izole PostgreSQL/SeaweedFS testleri,0skip; TypeScript ve3platform export başarılı, lint1öncekiuyarı. Yeni bağımlılık yok. Mobil API5006 health200,anonimnotices401,Metro8081running. Uygulanmış otomatik ve boş takip migration’ı korunur; son update öncesi/sonrası4tablohashaynı. Down okundu/tercih verisini siler; rollback yapılmadı. Cihaz/UI/OSizin-kabulü yapılmadı; Faz13Inreview,Faz14başlamadı. Commit/push/yayın yok. Ayrıntı mobile/docs/mobile/PHASE13.md ve docs/api/mobile-notices.md.


### Faz13 teslim ve main aktarımı — 15.10.2026

Çalışma sırasında başka bir işlem ortak checkout’ı feature/dev-readable-console-logs dalına geçirip yalnız konsol değişikliğini commit etmişti. Kullanıcı “Main’e aktar; mevcut değişiklikleri koru” onayı verdi. main tabanına göre binary diff ve geçici stash yedeği alındı; main’e geçilip tüm mevcut tracked değişiklikler ve konsol değişikliği uygulandı.66dosyanın önce/sonra SHA256 değeri birebir aynı; untracked Faz12–13 dosyaları korundu. Feature commit ve aktarım stash yedeği silinmedi. Bu işlem yeni commit/merge/push yapmadı; aktif dalmain, kaynakAPI/Metro sağlıklı,gitdiffchecktemiz. Stash bir yedektir; main’de aynı değişiklikler zaten bulunduğundan tekrar uygulanmamalıdır.

Son teslim:67/67mobil,102/102PG+S3,7/7GitHubscript; TypeScript/üçplatformexport geçti;22GitHubkaydı problems=[] ile doğrulandı. Faz13Inreview,push/cihazkabulü açık;Faz14başlamadı. Paket lisans metadata kontrolü başarılı; yeni bağımlılık yok. Kanıtlar /tmp/bf13/ altında; kalıcı ürün/teknik özet mobile/docs/mobile/PHASE13.md.


## Mobil Faz14 — Güvenlik, erişilebilirlik ve performans — 15.10.2026

Kullanıcı Faz14’ü onayladı; yerelmain.4işpaketi:inceleme/audit,darfix,otomatikdoğrulama,belge/takip. Eski mobil/merkez değişiklikleri korundu. Cache/serverkaynağı ve401/403purge,ortakAction48×48/disabledsemantiği,klavye/sarma/kontrast,SQLaggregate veUI LIMIT10,nativeURL4096/UTF8guard,foregroundpoll,AndroidallowBackupfalse uygulandı. Owner okuma ortakuseScopedRead kullanır; eski lint uyarısı giderildi.

75/75mobil,142/142gerçekizolePG+SeaweedFS,0skip;TypeScript/lint0/3platformexport ve329lisansmetadata geçti. Android manifest introspectfalse; yeniAPKkurulmadı. Audit28(18high/10moderate),4kökadvisory; uyumsuzExpo44/Router58otomatikfix uygulanmadı. NativeURLkısmiönlem webdecoder riskini kapatmaz. Gerçek cihaz/OSizin/ekranokuyucu/metinbüyütme/düşükcihaz/backup kabulü açık. Kod teslimiInreview,üretime hazır/Done değil. Faz15başlamadı; commit/push/yayın veya canlıişverisi/rolmutasyonu yok. Kanıt mobile/docs/mobile/PHASE14.md. Giriş/çıkış/toplamtoken,kalancontext/turtüketimi:BİLGİ MEVCUT DEĞİL.


## Mobil Faz15 — Yayın hazırlığı — 15.10.2026

Kullanıcı Faz15’i onayladı; üretim bilgileri için “Henüz hazır değil; eksikleri engel olarak bırak” seçti. Yerelmain’dedev/prodconfig,EAS24.14.0/Node22.23.3 profilleri,mandatorykimlik/sürüm/HTTPS/publiccallback,releasecheck/audit/lisans/kabulgates,splashveTürkçemağazayönergeleri eklendi. GeliştirmeID/callback korunur;proddevTeamIDkoplanmaz. NativeATS/cleartext/backup veoverlay/microphone remove kuralları;varsayımsalüretimhesabı/yayınverisi yok.

86/86mobil,Node22’de86/86,TypeScript/lint0,3platformexport ve izoleprodconfigintrospect geçti. Mobil871lockkayıt/merkez329lisansmetadata;resmiMITsplash57.0.10. Releasecheck gerçekeksikalan/6kabulpending/29audit(18high11moderate) ile exit1:beklenenengelleme. BEtestleri/gerçeknativebinary/cihazkabulü bu tur çalışmadı. Cloud/mağaza/OTA/hesapbağlantısı,commit/push veya canlıişverisimutationı yok. Faz15Inreview,planınhazırlıksonufazı;öncekiaçıkkabuller ve üretimgereksinimleri kapanmadan yayınyok. Kanıt mobile/docs/mobile/PHASE15.md ve mobile/buildingflow-mobile/release/.


## 15.10.2026 — Merkez Talepler ve Arızalar

- Yönetici rolüne açık alan seçimi ve canonical talep menüsü/listesi/detayı; kategori/öncelik/durum/arama/sıralama/sayfalama ve doğrulanmış özel fotoğraf.
- Aynı MobileRequest üzerinde durum/yanıt, sürüm/UUID/replay ve değişmez RequestManagementEvent geçmişi; kaydı/fotoğrafı yönetimden sonra koruma, eski mobil komut/makbuz uyumu.
- Ortak merkez durumunu mobil sync/owner/tenant/manager okumasına taşıma; production/cihaz kabulü yapılmış sayılmaz.
-159PG+fixtureS3,112FE,92mobil ve build/typecheck/lint/export/329lisans kanıtı TESTING.md; GitHub#5Project3. Yeni dependency/altagent/skill/mobilfaz yok.


## BAKIM — yerel uygulama hesabı bilgilerinin kaldırılması —16.10.2026

Kullanıcı talebiyle yerel ve örnek ortam dosyasından sabit geliştirme kullanıcısı/e-postası/parolası kaldırıldı. configure-identity.sh artık uygulama kullanıcısı oluşturmaz, kullanıcı parolası üretmez/saklamaz veya kullanıcıya otomatik SUPER_ADMIN atamaz. Realm/tema/SMTP/web client ve dar yetkili API servis hesabı yapılandırması korunur. README ile IDE shelf içindeki eski hesap dokümantasyonu temizlendi; shelf satır sayıları/prefixleri ve diğer kullanıcı değişiklikleri korundu. Mobil OAuth geliştirme dönüş şeması hesap bilgisi olmadığından değişmedi.

Doğrulama: ilgili beş shell başlatıcısında bash -n ve git diff --check PASS. Geçici fixture içinde mock curl/docker ile kimlik yapılandırması çalıştırıldı; ortam içeriği aynı kaldı, kullanıcı/parola/uygulama-yönetici rolü provision edilmedi ve mevcut altyapı komutları korundu. Ignore edilmiş dosyalar dahil hedefli taramada eski hesap ayar/referansı kalmadı; kaldırılmadan önce okunan tam parolanın depo dosyalarındaki kopya kontrolü de geçti. Sır değeri çıktı/belge/teste yazılmadı. Canlı Keycloak betiği çalıştırılmadı; mevcut kullanıcı hesabı/parolası/rolü değiştirilmedi. Bu konuşmada paylaşılan parolanın hesap tarafında ayrıca değiştirilmesi gerekir; dosyadan silme mevcut parolayı geçersiz kılmaz.

Bakım2/2=%100. Uygulama/frontend/backend tam testleri bu shell/env temizliği için yeniden çalıştırılmadı; yeni bağımlılık/şema/yolharitası fazı/skill/altagent yok. Commit/push/reset/stash yok; yerelmain ve diğer değişiklikler korundu. Token tüketimi ve kalancontext:BİLGİ MEVCUT DEĞİL.


## PERSON FAZ03 —16.10.2026 teknik teslim

Default kapalı PersonRefactor:Enabled altında Property'den bağımsız tenant-scoped CentralPerson CRUD/arama/detay ve salt okunur self-read uygulandı. Issuer+subject/aktif tenant üyeliği/gerçek yönetici rolü; sakin genelkişiler403,kendi maskelikartıreadonly. Ortak validator v1required-contact davranışını korur. UUID/version/replay/parentlock,receiptimmutatability;6yeniBaseEntitytablo ve eklemeli20261016122659migration/Designer/snapshot. Legacy kişiler/finans/mobilmakbuz/veri sınıflandırması değişmedi; gerçektenantseed/backfill yok.

**215/215seçili gerçekPG+fixturestorage integration,150/150unit,112/112FE,0skip;build0hata0uyarı,typecheck/FEbuild ve329lisansmetadata PASS.** Yeni paket yok; önceki migration ve bağımlılık hashleri aynı. Yalnız izole DB migration'ı çalıştı; anaDBupdate/servisrestart/production/görsel/nativekabul yok. Aktif hesapbağı tür/disable koruması,pasif/silinmişscope/futurelink,sharedcontacts/idempotency/concurrency/legacyupgrade doğrulandı.

Teknik6/6=%100;#8Inreview. FAZ04 başlatılmadı; tenant→Property haritası,legacyalias/adapter/backfill,ilişki/görev ve ekran geçişleri sonraki onaylı fazlardır. Ayrıntı docs/api/central-people.md ve docs/refactor/person/phase-03-report.md. Kullanılan yerel phase/contract/verify/security yönergeleri;tekagent. Token/context:BİLGİ MEVCUT DEĞİL. Commit/push/merge/reset/stash yok.


## PERSON FAZ04 —16.10.2026 önkoşul incelemesi

Kullanıcı FAZ03 teslimini kabul edip FAZ04 başlangıcını onayladı. #8 kabul/Done; #9 devam ediyor, müşteri–Property haritası eksik. PHASE-04 adım2 ve FAZ02 D01 gereği önkoşul çözülmeden uygulama başlatılmadı. Hangi alanların aynı müşteriye ait olduğu soruldu; varsayılan tenant veya otomatik alan gruplaması yapılmaz. Kod/DB/migration/bağımlılık/runtime değişmedi; bu tur uygulama testi ve güvenlik taraması çalıştırılmadı. Yeni faz tamamlanmadı:1/6=%16,67; önceki kabul3/11=%27,27. Ayrıntı docs/refactor/person/phase-04-report.md. Kaynak/manifest hash ve belge bağlantısı kontrolü bu belge turunun doğrulamasıdır. Sonraki faz yok; yeni görev açılmadı, mevcut #8/#9 kullanılır. Token/context:BİLGİ MEVCUT DEĞİL.


## PERSON FAZ04 — 17.10.2026 teknik teslim

Kullanıcı, mevcut kişilerin tek Altunbayrak Rezidans'a ait olduğunu belirtti; önceki eşleştirme engeli çözüldü. Ownership/Tenancy/Residency, kesin kesirli pay hesabı, tarih/primary/history kuralları; açık tenant–Property eşlemesi, legacy alias/yerel profil ve dry-run/hash/atomik apply/karşılaştırma uygulandı. V1 tek yazım kaynağı kalır. Kaynak değişimi ve import canonical güncellemesi409 ile reddedilir; canlı cutover yapılmadı.

Ortak checkout'taki otomatik ilişki tablosu ve Consents migrationları korundu. Yeni20261017144809 migration'ı view/trigger/dolu Down koruması ekler. Bu tur ana DB update/backfill, gerçek tenant kurulumu veya servis restart yapılmadı; iki feature bayrağı varsayılan kapalı.

**288/288 seçili gerçek PG entegrasyonu,174/174 birim,117/117 frontend testi;0 başarısız/atlanan.** Son .NET derlemesi0hata/0uyarı;329 paket lisans metadata, önceki migration/paket/lock hashleri ve diff kontrolü başarılı. Bir önceki koşuda mevcut otomatik migration testinin geçici DB temizliği yetki hatası verdi; yalnız manifestteki test kaynakları temizlenip aynı kapsam tekrar başarılı çalıştırıldı. Ayrıntılı hata ve yeniden doğrulama raporda korunur.

Teknik6/6=%100; #8 Done, #9 In review, kullanıcı kabulü bekleniyor. FAZ05 başlamadı; İŞ/UI/mobil yol haritası sayacı değişmedi. Kanıt: docs/refactor/person/phase-04-report.md ve docs/api/person-transition.md. Başka oturumların değişiklikleri korundu; toplam checkout diff'i bu faza ait değildir. Yeni bağımlılık, commit/push/merge/reset/stash yok. SAST/SCA/CVE/secret scanner, görsel/native ve production kabulü çalıştırılmadı.

Token tüketimi ve kalan context: BİLGİ MEVCUT DEĞİL. Son Codexusage:300dk kullanılan%19/kalan%81, reset17.10.2026 22:37:38;10080dk kullanılan%66/kalan%34, reset22.10.2026 10:57:02 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir.


## PERSON FAZ05 —17.10.2026 teknik teslim

Kullanıcı FAZ04'ü kabul ederek FAZ05'i başlattı; #9 Done. Tarihli ManagementAssignment, atama/bitiş/iptal, rol ve tenant/Property kapsamı, UUID/version/replay ve değişmez audit/makbuz uygulandı. SITE_ADMIN yalnız MANAGER/ACCOUNTANT görevlerini yönetir; MANAGER kendine yetki veremez. Görevler daire malikliği/sakinlik veya Keycloak rolü üretmez.

Tenant bazlı LEGACY/SHADOW/ENFORCED v2 kontrolü, exact issuer/account-link/üyelik/doğal kişi/tarih koşulları ve stale legacy shadow reddi vardır. ENFORCED son yönetici kapatmasında kesintisiz yedek görev ve gerçek Keycloak enabled/rolü salt okunur doğrulanır; başarısız doğrulama erişimi kapalı tutar. Eski v1/mobil legacy geçişi bu fazda yapılmadı; tüm uygulamada production enforcement iddiası yoktur.

**314/314 seçili PG/fixture storage integration,187/187 unit,119/119 frontend;0fail/skip.** Son çözüm derlemesi0hata/0uyarı;329 paket lisans metadata, eski migration/paket/lock hashleri, bağlantı ve diff kontrolleri başarılı. Yeni26integration senaryosu ve3unit eklendi. Keycloak devri FakeIdentity ile doğrulandı; gerçek kimlik sunucusu kabulü yapılmadı. Ara predicate derleme hatası ortak EligiblePeople sorgusuyla düzeltildi; son testler geçti. Tüm geçici test kaynakları temizlendi.

Yeni20261017150715ManagementAssignments üç BaseEntity tablo, Designer/snapshot, scope/closure/overlap/history ve audit trigger'ları, dolu Down koruması ekler. Eski migrationlar korundu. Ana DB/görev/tenant seed veya gerçek Keycloak rol/hesap yazımı, servis restart yok. AssignmentsEnabled defaultfalse, tenantmode defaultLEGACY; gerçek .env korunur. AutoMigrate açık sonraki açılış pendingmigration uygulayabilir.

Teknik6/6=%100,00; #10 In review, kullanıcı kabulü bekleniyor. Kabul edilmiş önceki fazlar4/11=%36,36. FAZ06 başlamadı; İŞ/UI/mobil yol haritası sayacı değişmedi. Ayrıntı docs/refactor/person/phase-05-report.md ve docs/api/management-assignments.md. Yeni paket/extension/agent ve commit/push/merge/reset/stash yok. SAST/SCA/CVE/secret scanner, görsel/cihaz/native/production ve frontend typecheck/build bu fazda çalıştırılmadı.

Token tüketimi/kalan context:BİLGİ MEVCUT DEĞİL. Son17.10Codexusage300dk kullanılan%63/kalan%37, reset17.10.2026 22:37:38;10080dk kullanılan%73/kalan%27, reset22.10.2026 10:57:02 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir.

## PERSON FAZ06 — 18.10.2026

Merkezi kişi listesi/dört detay sekmesi, izinli native ilişki ve görev akışları, minimum hesap DTO, TR/EN ve role/scope kontrolleri tamamlandı. **329/329 seçili izole PG/storage integration,187/187 unit,135/135 frontend; typecheck/production build, .NET0hata0uyarı ve329 lisans metadata geçti.** İki yazıcı koruması ve immutable receipt migration testli. Canlı DB/.env/rol/servis değiştirilmedi; bayraklar defaultfalse. Native ilişkiler eski finans/mobile projekte edilmez; eski kayıtlar kaynak ekranda yönetilir. Gerçek UI/klavye/telefon, SAST/CVE/scanner kabulü yapılmadı. Teknik6/6=%100; mevcut #11 In review/kullanıcı kabulü bekliyor; kabul edilmiş5/11=%45,45. FAZ07 başlamadı; diğer İŞ/UI/mobil sayaçlar değişmedi. [Rapor](../refactor/person/phase-06-report.md), [API](../api/person-center.md).

Codex usage (18.10.2026): 300dk kullanılan %28/kalan %72, reset 18.10.2026 05:15:34; 10080dk kullanılan %4/kalan %96, reset 25.10.2026 00:15:34 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir. Giriş/çıkış/toplam token ve kalan context: BİLGİ MEVCUT DEĞİL.

## BAKIM — Yol haritası ICU rota metni — 18.10.2026

`phase19Task2Detail` TR/EN metinlerindeki `/users/{id}/photo` ve `/people/{id}/photo` örnekleri ICU literal quoting ile düzeltildi; `{id}` ekranda aynen görünür, formatter parametresi istenmez. Mevcut standards testi bütün roadmap dinamik anahtarlarını iki dilde parametresiz gerçek translator ile çalıştırır ve iki rota örneğinin korunmasını doğrular. **135/135 frontend testi, typecheck ve üretim build başarılı; diff kontrolü geçti.** Backend/DB/.env/bağımlılık ve faz sayaçları değişmedi; gerçek tarayıcı kontrolü yapılmadı. FAZ07 başlatılmadı. Bakım2/2=%100.

Kota (Codex usage,18.10.2026):300dk kullanılan%31/kalan%69, reset18.10.2026 05:15:34;10080dk kullanılan%5/kalan%95, reset25.10.2026 00:15:34 Europe/Istanbul. Giriş/çıkış/toplam token ve kalan context: BİLGİ MEVCUT DEĞİL.

## PERSON FAZ07 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ06 kullanıcı onayıyla #11 Done. FAZ07 #12 In review: sunucu kaynaklı görev/daire context'leri, tek alan otomatik açılış/çoklu seçim, salt okunur self, güncel merkez Property/tenant kapısı ve finans içindeki kişi görünürlüğünün göreve daralması tamamlandı. **348/348 seçili izole PG/storage integration,187/187 unit,143/143 frontend; typecheck, açık/kapalı bayrakla production build, .NET0hata0uyarı, compose config ve329 lisans metadata başarılı.** Model/migration/manifest/lock korundu; canlı DB/.env/rol/servis değişmedi. Yeni bayraklar defaultfalse; global merkez ENFORCED kapısını açmak önceden hazırlanmış hesap/görev/scope ister. Eski mobil ve finans projeksiyon geçişi sonraki fazlarda; global users/consents tenant yöneticilerine yeni akışta kapalıdır. Gerçek UI/Keycloak/cihaz ve SAST/CVE/scanner kabulü yapılmadı. Teknik6/6=%100; kabul edilmiş6/11=%54,55. FAZ08 başlamadı; diğer İŞ/UI/mobil sayaçlar değişmedi. [Rapor](../refactor/person/phase-07-report.md), [API](../api/session-workspaces.md).

Codex usage (18.10.2026):300dk kullanılan%75/kalan%25, reset18.10.2026 05:15:34;10080dk kullanılan%12/kalan%88, reset25.10.2026 00:15:34 Europe/Istanbul. Hesap pencereleri sohbet token tüketimi değildir. Giriş/çıkış/toplam token ve kalan context: BİLGİ MEVCUT DEĞİL.

## PERSON FAZ08 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ07 kullanıcı kabulüyle #12 Done. FAZ08 mevcut #13 In review: minimum canonical kendi kişi/context/capability kopyası, SQLCipher schema8 payload, SecureStore biyometrik cihaz bağı, yönetim1h/sakin24h ve ilişki bitişiyle kısalan süre, saat geri alma/arka plan/çıkış koruması uygulandı. Yeni mod yalnız readonly ekran açar; eski cache/CRUD/sync yolları kapalıdır, mod geçişinde eski taslak/kuyruk korunur. Bayraklar defaultfalse; canlı DB/.env/Keycloak/servis değiştirilmedi. Yeni paket/migration yok.

**354/354 seçili izole PG/storage integration,187/187 unit,119/119 mobil; typecheck/lint, kapalı+açık bayrakla Expo iOS/Android/web export, .NET0hata0uyarı, compose config ve329 lisans metadata başarılı.** Gerçek native SQLCipher/SecureStore/biyometri/background kabulü DOĞRULANAMADI; FaceID yapılandırması yeni binary ister. Offline iptal anında öğrenilemez; readonly lease sonuna kadar bilgi erişimi riski açıkça raporlandı. SAST/CVE/scanner/production kabulü yapılmadı. Teknik6/6=%100,00; kabul edilmiş önceki7/11=%63,64. FAZ09 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-08-report.md), [API](../api/mobile-person-authorization.md).

## PERSON FAZ09 —18.10.2026 — Devam ediyor
FAZ08 kabul edildi; #13 Done, mevcut #14 In progress. Canonical talep/arıza/fotoğraf ve ayrı hesap profili taslakları mevcut şifreli SQLite/outbox/idempotency altyapısına bağlanıyor. Yetki sunucuda tarihli context kimliğiyle tekrar kontrol edilir. Merkezi kişi salt okunur kalır; ödeme/kesin tahakkuk/rol değişimi offline kuyruğunda yoktur. Yeni paket/migration ve canlı .env/DB/Keycloak/servis değişimi yok. FAZ10 başlamadı. İnceleme/önkoşul/plan3/6=%50,00; uygulama ve test devam ediyor. Geçmiş FAZ08 testleri bu fazın sonucu sayılmaz.

## PERSON FAZ09 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ08 kullanıcı kabulüyle #13 Done; mevcut #14 In review. Canonical contextKind/contextId ile talep/arıza/fotoğraf CRUD ve ayrı hesap profili mevcut SQLCipher schema8/outbox'a bağlandı. Sunucu güncel kişi/görev/ilişki kapsamını push/replay/pull/photo sırasında ve parent kilitli transaction içinde tekrar doğrular. Legacy kuyruğu/fingerprint uyumu korundu; canonical revoke pending taslağı şifreli/blocked tutar, başka alana göndermez. Merkezi kişi kartı readonly; ödeme/kesin tahakkuk/rol değişimi offline kuyruğunda yoktur. Fotoğraf seçici dönüşünde tekrar doğrulama ve modal gizlilik örtüsü uygulandı.

**363/363 seçili izole PG/storage integration,187/187 unit,126/126 mobil; typecheck/lint, açık+kapalı bayrakla üç platform export, .NET0hata0uyarı, compose config ve329 lisans metadata PASS.** Yeni9 HTTP+7 mobil senaryosu bu toplamlardadır; dar50/50 ayrıca geçti.70 mevcut manifest/proje/migration hash'i aynı; yeni paket/migration yok. Gerçek native SQLCipher/SecureStore/picker/biyometri/background/UI ve Keycloak/production kabulü DOĞRULANAMADI; scanner çalıştırılmadı. Yeni sync bayrakları defaultfalse, gerçek .env/DB/tenant/Keycloak/rol/servis değiştirilmedi. Teknik6/6=%100,00; kabul edilmiş önceki8/11=%72,73. FAZ10 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-09-report.md), [API](../api/mobile-canonical-sync.md).

## PERSON FAZ10 —18.10.2026 — Devam ediyor
FAZ09 kullanıcı kabulüyle #14 Done; mevcut #15 In progress. Güvenlik/lisans doğrulaması: web audit0, NuGet7projede bildirilmiş açık yok; mobil audit29kayıt(18high/11moderate),4kökadvisory — güvenlik kapısı GEÇMEDİ. Fortify/Semgrep/Gitleaks/TruffleHog/OSV/Trivy/Syft erişimi yok, kurulum yapılmadı. Gerçek runtime private-s3.json ignore olmasına rağmen HEAD/indexte takipliydi; disk içeriği ve .env korunarak yalnız git rm--cached yapıldı. Bu dosyanın staged deletion'ı bilinçlidir; commit/push/geçmiş değiştirme/anahtar rotasyonu yapılmadı. Geçmişteki erişim bilgileri ve rotasyon açık risk olarak kalır. Dört private runtime yolunun ignored/untracked kontrolü ve3negatif/pozitif fixture CI'ye eklendi. Native cihaz/MASVS ve dağıtım uyumu garanti edilmez. FAZ11 başlamadı. Shared checkout'taki website kaynak/migration değişimleri bu faza ait değildir.

## PERSON FAZ10 —18.10.2026 — Doğrulama teslimi / güvenlik kapısı GEÇMEDİ

FAZ09 kullanıcı kabulüyle #14 Done; mevcut #15 In review. **Mobil npm audit29kayıt(18high/11moderate),4kökadvisory ile başarısız.** Web audit0; NuGet7projede bildirilmiş0açık. Mevcut lisans metadata329npm/NuGet ve871mobil lock kaydında geçti; tam binary/container dağıtım/NOTICE kabulü değildir. Fortify/Semgrep/Gitleaks/TruffleHog/OSV/Trivy/Syft erişimi yok; tam SAST/history/native/container taraması DOĞRULANAMADI.

Gerçek runtime özel konfigürasyonu ignore olmasına rağmen HEAD/indexte takipliydi. Disk dosyası ve .env byte'ları korunarak yalnız Git indeksinden çıkarıldı (bilinçli staged deletion). Eski erişim bilgilerini yenileme, geçmiş/klon incelemesi ve düzeltmenin commit'e alınması açık; commit/push/geçmiş değişimi/rotasyon yapılmadı. Dört private runtime yolunun ignored+untracked gate'i ve3regresyon fixture CI'ye eklendi. Mobil high audit gate'i de CI'ye eklendi; bugün exit1 ile kapatır, bulgu bastırılmaz.

**364/364 seçili izole PG/storage integration,244/244unit,126/126mobil,6/6telemetri,23/23seçiliweb;0fail/skip.** Typecheck/lint/mobil export, .NET0hata0uyarı, yeni3fixture, mevcut6CIgate/29unsafe release reddi, shell/diff PASS. Testlerin geçmesi güvenli yayın sonucu değildir. Native MASVS, biyometrinin gerçek anahtara kriptografik bağı, OS/picker/modal/background/screenshot ve artifact lisans doğrulaması açık. Bu faz uygulama/migration/paket sürümü değiştirmedi; shared checkout'taki eşzamanlı WEBSITE kod/model değişimleri korunur. Teknik6/6=%100 çalışma teslimi; kabul edilmiş önceki9/11=%81,82. FAZ11 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-10-report.md), [redacted kanıt](../refactor/person/phase-10-evidence.json).
- 22.10.2026 BAKIM: Kişiler ekranındaki tek seferlik "Eski kişi kayıtlarını aktar" paneli ve TR/EN metinleri kaldırıldı; yerel transition bayrağı kapatıldı.
