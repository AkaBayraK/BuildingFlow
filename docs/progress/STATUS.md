## QA FAZ 09 — AkaAI AI Test Asistanı — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#105.** AkaAI deposu değişmeden mevcut entegrasyon API'si: kapalı şema + allowlist plan, onaysız koşum yok, açıklama sonucu değiştirmez, bütçe/503 fallback, 4 hata kodu; web AI Test Asistanı sekmesi. **Integration27/27 (5 injection/uydurma plan reddi), web307/307+typecheck+build, lisans329 PASS.** Gerçek AkaAI uçtan uca bağlantı yapılandırma bekliyor. Rapor `qa-center/reports/phase-09.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 08 — Diğer modüller — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#104.** Modül paketleri ilk koşum 281/302; 20 FAIL paylaşılan PG bağlantı sınırı (53300) flaky, tek başına tekrar PASS; ParkingMigrationTests retention testi QaCenterRuns sonrası `migrations[^2]` varsayımıyla kırılmıştı, isimle bağlandı (4/4 PASS). Yeni `qa_coverage_gate.py` ratchet + CI; paralel tedarikçi kodundaki 2 testsiz endpoint'i yakaladı. Rapor `qa-center/reports/phase-08.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 07 — Mobil ve offline-first — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#103.** Mobil check 160/160 + build PASS; `eas.json` `qa` profili; mobil rol kapıları envanteri. iOS simülatörde mevcut dev build kuruldu ama Metro paketine bağlanamadı (BLOKE, kanıt `artifacts/qa/qa-mobile-ios/`); Android SDK yok. Rapor `qa-center/reports/phase-07.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 06 — Finans E2E — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#88, ürün eksikleri #89.** Yeni testler: eşzamanlı çift tıklama tek intent + UUID replay; sırası bozuk webhook başarılı ödemeyi geri alamaz. Finans paketleri izole PG Integration121/121+Unit57/57, KVKK 0. 3DS, çift taraflı muhasebe ve web/mobil online ödeme istemcisi ürün eksiği (BLOKE). Rapor `qa-center/reports/phase-06.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 04/05 — Web otomasyonu ve kimlik/KVKK — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#85, #86, bulgu #87.** CDP tarayıcı motoru (yeni paket yok, geçici SUPER_ADMIN, kullanıcı silindi): tam koşum PASS81/FAIL10/SKIPPED10; düzeltme sonrası alt küme kalan FAIL yalnız #87. Düzeltilen: `/properties/{id}/sms` OIDC dönüş whitelist'i, test.sh `PersonRefactor__*` sızıntısı (guard FEATURE_FLAG_LEAK). Geçici `bf-qa-*` realm 11/11 (realm izolasyonu 401), KVKK log taraması 13/13 + gerçek loglar 0; kişi/alan/rol paketleri Integration244/244+Unit28/28. Web301/301, lisans329 PASS. Web↔mobil BLOKE. Raporlar `qa-center/reports/phase-04.md`, `phase-05.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 03 — Test Merkezi UI ve orkestrasyon — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#84.** QaRun/QaRunStep + `QaCenterRuns` migration (Down guard), SUPER_ADMIN `CanManageQaCenter`, `QaCenter:Enabled` varsayılan kapalı/Production yasak, sıralı runner (timeout, iptal, INTERRUPTED), 3 salt okunur senaryo, 4 hata kodu; web `/system-settings/qa-center` ikonlu sekmeler, başlat/durdur/yeniden dene, polling izleme. **Integration31/31+Unit156/156 (guard GEÇTİ), web300/300+typecheck+build, lisans329 PASS.** Migration yerel DB'ye uygulanmadı; canlı UI/responsive kanıtı BLOKE (onay bekliyor). Rapor `qa-center/reports/phase-03.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 02 — Ekran/API keşfi ve test matrisi — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**#82, bulgular #83.** Çalışma zamanı endpoint dökümü (`QaEndpointInventoryTests`, public prefix dışında işaretsiz route FAIL) ve `scripts/qa_inventory.py`: **API398 (yetkili385/anonim5/public8), web55 sayfa, mobil9 ekran, tablo108; web tam eşleşme131, mobil20, tüketicisiz12.** Testler: envanter1/1, eşleştirici11/11, deterministik çıktı; lisans329 PASS. Mobil ekran içi rol dalları kısmi (FAZ07). Yeni paket/migration yok; push yok. Rapor `qa-center/reports/phase-02.md`. Token: BİLGİ MEVCUT DEĞİL.

## QA FAZ 01 — İzole test altyapısı ve güvenlik — 28.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**Test ve Kalite Merkezi akışı (qa-center/, 14 faz), FAZ01 / #81.** `scripts/qa_guard.py` test.sh içinde her koşumdan önce Production ortamı, loopback dışı/ana/test dışı DB, loopback dışı S3/Mailpit/Keycloak/RabbitMQ/SMTP/sandbox URL ve gerçek SMS OTP'yi reddeder. **Guard 15/15, izole smoke Integration52/52+Unit14/14 atlanan0, Production negatif kanıtı exit1/0 test, lisans329 PASS.** Ayrı Keycloak test realm'i ve QA mobil profili yok (kısmi madde). Yeni paket/migration yok; push yok. Rapor `qa-center/reports/phase-01.md`. Ürün yol haritası değişmedi. Token: BİLGİ MEVCUT DEĞİL.

## BAKIM main Otopark — onaylı yerel devreye alma — 28.10.2026

Kullanıcı dört migration ve API yeniden başlatmayı açıkça onayladı. **ADIM03/03 TAMAMLANDI; bakım3/3=%100,00.** Yerel main/.env DB üzerinde EF Core `database update 20261027205401_ParkingRetentionRedaction --no-build` başarılı: ParkingFoundation, ParkingRequestIds, ParkingMaintenanceClosures, ParkingRetentionRedaction uygulandı. Son `migrations list --no-build`: **bekleyen0**. Yalnız bu projeye ait API güvenli `api-stop` ile durduruldu ve standart `bash scripts/dev.sh api` ile başlatıldı. **localhost:5005/health ve /health/live HTTP200; anonim parking/policy HTTP401.** Chrome gerçek localhost:3003 oturumunda Araçlar/Otopark etkin link ve property parking rotası görüldü. Sonrasında kullanıcı başka pencereye geçtiği için özel oturumda parking içerik etkileşimi yapılmadı; veri yazan E2E bu tur çalıştırılmadı. Önceki559 test bu tur tekrar çalışmış sayılmaz. Yeni paket/kod/commit/push yok; mevcut hesap parolası ve kullanıcı kayıtları değiştirilmedi.

Kanıt: `/tmp/bf-main-parking-migrate.log`, `/tmp/bf-main-parking-post-migration.log`, `/tmp/bf-main-parking-api.log`. Önceki BEKLIYOR kaydı tarihsel; bu onayla runtime adımı tamamlandı. Ürün yol haritası32kayıt152/163=%93,25; ürün fazı/yayın kabulü değişmedi. Sonraki: kullanıcı localhost:3003 menüsünden Otopark ekranını kullanabilir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex 28.10.2026:300dk kullanılan%99/kalan%1 reset28.10.2026 16:43:32;10080dk%15/%85 reset04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM — main Otopark entegrasyonu — 28.10.2026

Kullanıcı isteğiyle çalışma gerçek yerel `main` dalına taşındı. Otopark backend/web/mobil kodu mevcut Ortak Alanlar ile birleştirildi; navigation/routes/OIDC dönüş kapsamı, servis ve endpoint kayıtları, AuthProvider/cache ve EF snapshot birlikte korundu. Git indeksine bu işlemde add/reset yapılmadı; mevcut kullanıcı değişiklikleri korunur. Yeni bağımlılık eklenmedi; parking kaynak dalındaki aynı node-forge sürüm düzeltmesi taşındı.

**Adım 1/3 kaynak birleştirme TAMAMLANDI; adım 2/3 doğrulama TAMAMLANDI; adım 3/3 yerel runtime BEKLIYOR. Tur 2/3=%66,67.** BAKIM ürün fazı değildir, faz sayaçlarına eklenmez. Birleşik ürün yol haritası 32 kayıt,152/163=%93,25; bu bakım nedeniyle yeni görev işaretlenmedi. **Main .NET build 0 hata/0 uyarı; EF pending-model kontrolü başarılı; web typecheck +296/296 test + üretim build; mobil check (typecheck/lint/160 test); ayrı geçici PostgreSQL'de Otopark/Ortak Alanlar 71/71 entegrasyon +32/32 unit başarılı, atlanan yok.** Test DB/rolü temizlendi. Kanıtlar `/tmp/bf-main-parking-*` log ve TRX dosyaları; önceki dal testleri bu tur çalıştırılmış sayılmadı.

Yerel DB read-only migration sorgusu: CommonArea dört migration uygulanmış, ParkingFoundation/ParkingRequestIds/ParkingMaintenanceClosures/ParkingRetentionRedaction bekliyor. Gerçek DB değişmedi, API yeniden başlatılmadı; tarayıcıda gerçek main oturumuyla uçtan uca kullanım doğrulanmadı. Menü kaynakta artık etkin bağlantı; kullanıcıdaki çalışan API'nin hazır olduğu iddia edilmez. Plan MASTER_PROMPT madde10 gereği yerel migration onayı son adımda istenir. Commit/push/deploy yok.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex 28.10.2026: 300dk %93 kullanılan/%7 kalan, reset 28.10.2026 16:43:32;10080dk %14/%86, reset 04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## ORTAK ALANLAR FAZ08 — 27.10.2026 — DEVAM EDIYOR

OA-08/#58, plan08/08 kullanıcı tarafından onaylandı. Tur0/3=%0,00: kapsam, kalite kontrolleri, yayın raporu. Tam backend/web/mobil regresyonu, güvenlik/lisans/SBOM, izoleDocker-smoke ve performans; gerçekOIDC/native/remoteCI ayrı kapıdır. Mevcut Git indeksi ve kullanıcı verisi korunur; yayın/commit/push yok.

## ORTAK ALANLAR FAZ07 — 27.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**OA-07 / #57, plan07/08.** Yönetici dönem takvimi, sayfalı/ikonlu durum listesi, UUID/version kontrollü düzenleme ve gerekçeli iptal; RepeatableRead rapor API'si. Union/bakım/iptal, null payda ve mevcut saat/kapasite sınırı açık. Sakin salt okunur ve gizli not yasağı korundu. **İzolePG/API42/42+unit13/13;web280/280+typecheck+build GEÇTİ.**375pxTR/ENkoyu,1200pxTR izoleUI;90→60dk düzenle/iptal→0dk ve readonly rol kanıtı. Yeni paket/entity/migration yok, Git indeksi/önceki çalışmalar korundu. Mevcut adapter yalnız Sandbox, gerçek sağlayıcılar planda; rezervasyon-ücret ChargeId bağı yok, referans ücret tahsilat değildir. CanlıE2E/native/gerçeködeme/yük/pentest/CI yok; başarılı sayılmadı. Commit/PR/push/deploy yok.

Tur3/3=%100,00;faz6/7=%85,71 kullanıcı kabulü açık; önceki fazlara devam onayı6/8=%75,00. Ürün roadmap28kayıt,145/155=%93,55; mobil15/UI/TEKNİK sayaçları aynı. #57 açık;FAZ08 başlatılmadı. Sonraki kullanıcı kabulü ve yeni faz için talimat. [Rapor](../common-areas/07-report.md).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage27.10.2026/rateLimitsByLimitId.codex:300dk%65kullanılan/%35kalan reset27.10.2026 12:00:53;10080dk%42/%58 reset02.11.2026 15:28:41 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## ORTAK ALANLAR FAZ06 — 27.10.2026 — TEKNİK TESLİM / KABUL BEKLİYOR

**OA-06 / #56, plan06/08.** Mobil portföy bağlamlı liste/detay/salt okunur saatler, online yönetici alan/rezervasyon/iptal, şifreli sınırlı cache ve otomatik gönderilmeyen UUID taslağı; web/mobil saf sözleşme ortaklaştırıldı. ACCOUNTANT yazmaz;403/404 temizleme ve purge hatasında açık ret. **Mobil152/152+check+üç platform export; web274/274+typecheck+build;871mobil lisans metadata GEÇTİ.**375/430px izole UI rol/409/offline kanıtı var; gerçek cihaz SQLCipher/biyometri ve canlı auth/API E2E yok. Backend/DB/paket/migration/commit/push/deploy değişmedi. Mevcut Git indeksi korundu. Native blok atama ve fotoğraf yönetimi merkezde; gelişmiş takvim/rezervasyon düzenleme/ödeme yok.

Tur3/3=%100,00 teknik; faz6/7=%85,71 kullanıcı kabulü açık; önceki fazlar için devam onayı5/8=%62,50. Ürün roadmap27kayıt,144/154=%93,51. Mobil15faz/UI sayaçları korunur. #56 açık; FAZ07 başlatılmadı. Sonraki: kullanıcı kabulü ve canlı/native kabul; yalnız yeni kullanıcı talimatıyla FAZ07. [Rapor](../common-areas/06-report.md).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage27.10.2026/rateLimitsByLimitId.codex:300dk%13kullanılan/%87kalan reset 27.10.2026 12:00:53;10080dk%33/%67 reset02.11.2026 15:28:41 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

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

## BF-PAY FAZ 08 — 24.10.2026 — TAMAMLANDI (tarayıcı kabulü BEKLIYOR)

**[AKIŞ BF-PAY | 13 faz, 00–12] FAZ 08 resmi görev 9/10 = %90,00; akış 9/13 faz = %69,23 (00–08).** Web: Tahsilatlar sekmeleri (online ödemeler, defter, mutabakat, banka ve ödeme ayarları), makbuz, sistem ayarlarında sağlayıcı kataloğu. Web 260/260, typecheck, OnlinePayment entegrasyon 9/9 + unit 14/14. `npm run build` eşzamanlı iletişim doğrulama çalışmasının Turbopack kökü dışındaki `shared/` içe aktarımı yüzünden kırık. Yeni paket/migration yok, commit yok. Sonraki: FAZ 09 mobil, onay bekler. Token/kota: BİLGİ MEVCUT DEĞİL. [Rapor](../payment/08-web.md).

## BF-VERIFY FAZ05 — Adım02 TAMAMLANDI / Adım03 DEVAM EDIYOR

Mobil ortak doğrulama kartı hemlegacyHesabım hemcanonicalçalışmaalanında;strictDTOweb/mobilpaylaşıldı. Çevrimiçiscopewhitelist,IdP AIA/S256/aynısubject,epoch/late-response/UUID,CANCELchallenge,offline/background/logouttemizliği veprofilrefresholayı eklendi. Yeni bağımlılık yok. **Mobilcheck143/143+3platformexport;ilgiliweb18/18+typecheck;nativeAPItestprojesibuild0hata0uyarı.** BaşlangıçnativeAPI, eşzamanlıOnlinePayment.csIbanalan/sınıfçakışması yüzünden durdu;BAKIMolarakyalnız2çağrıglobaltypeilebelirtildi,ödemeişkuralıdeğişmedi. GeçiciiPhone17Prosimülatörüoluşturuldu,mevcutdevbinaryyalnızona kuruldu. GerçeknativeOIDC/API/PG/SMS-testdoublekabulüsürüyor;henüztamamlandıdeğil. Faz2/3=%66,67;plan4/8=%50,00. FAZ06başlatılmadı.

## BF-VERIFY FAZ05 — 23.10.2026 — DEVAM EDIYOR

Kullanıcı FAZ05/#48’i başlattı;liste6/08,3resmiadım. OrtakAuthProvider/profileRequest,profil ve canonicalPersonModelScreen doğrudan incelendi;Expo57/RN0.86.3 veversioneddocs doğrulandı;başlangıçprofile/registration testleri geçti. iOS26.5simülatörleri veöncedenderlenmişsimulatorapp mevcut;henüzbootedcihaz yok. #48Inprogress;04kapalıDone,03gerçekSMSaçık. Adım01 TAMAMLANDI,02 DEVAM EDIYOR;faz1/3=%33,33,plan4/8=%50,00. Üretim/nativeayar/dependency/canlıveri değişmedi;diğer çalışmalar korunur. SonCodexusage:300dk%65kullanılan/%35kalan,reset23.10.2026 21:42:33;10080dk%25/%75,reset30.10.2026 11:03:51 Europe/Istanbul. Giriş/çıkış/toplam token vekalancontext:BİLGİ MEVCUT DEĞİL.

## BF-VERIFY FAZ04 — 23.10.2026 — TAMAMLANDI

**#47 Closed(completed),Project3 Done;12/12 madde ve son uzak okuma doğrulandı. Faz3/3=%100,00;bu tur3/3=%100,00;tamamlanan BF-VERIFY00/01/02/04=4/8=%50,00.** Kullanıcı kararıyla03gerçekSMSaçıkken04uygulandı;#46açık kalır. Ortak/account paneli:maskeli taze e-posta/telefon,eski doğrulanmış/yeni bekleyen hedef,IdP VERIFY_EMAIL/UPDATE_EMAIL,sabit güvenli callback,kodSEND/VERIFY/CANCEL,version/UUID/limit/süre/deneme,409/429/outage veodak yenilemesi. İsim/fotoğraf/Usersyetkisi genişlemedi. **Son web250/250,typecheck/build;gerçekPG/APIbrowser1/1;API32/32+unit16/16;gerçekKeycloak/Mailpit12/12grup.** TR/EN,375/1440px,3tema,klavye,çifttıklama1istek ve gerçek mobilcontactAPI→web yenileme doğrulandı. Fizikselmobil/gerçekoperatörSMS iddiası yok. Yeni üretimAPI/migration/paket yok;test köprüsü yalnız test projesinde,opt-in ve loopback. [Rapor](../refactor/contact-verification/04-report.md).

Eşzamanlı Next16.4.3 manifest/lock,BF-LATE,quality/Dockerfile/.nvmrc veindex korundu;bu faz install/restore yapmadı. Canlı.env/DB/realm/hesap/servis değişmedi,commit/push/deploy yok;yalnız kendi test kaynakları temizlendi. Sonraki:FAZ05/#48 mobilUI,yeni kapsam onayı bekler;bu tur başlatılmadı. FAZ03 gerçekSMS Netgsm hesabı/gönderici/OTP paketi ve yetkili test alıcısı sağlanınca ayrıca tamamlanır.

Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL. Son kaynak23.10.2026 Codex usage/rateLimitsByLimitId.codex:300dk%52kullanılan/%48kalan,reset23.10.2026 21:42:33;10080dk%24/%76,reset30.10.2026 11:03:51 Europe/Istanbul.Hesap kotası sohbet tüketimi değildir.

## BF-VERIFY FAZ04 — Adım02 sonucu / Adım03 DEVAM EDIYOR

Ortak AccountPage iletişim paneli,strict DTO,maskeli hedef,ikonlu durum,IdP e-posta aksiyonu,sunucu version/challenge/attempt/retry,süre/iptal/409 veodak yenileme uygulandı. İsim/fotoğraf yazarı değişmedi. İlk tarayıcıHTTP fixture: gönderim,yanlışkod4deneme,Enter/odak,başarılıdoğrulama GEÇTİ. İlk tam web249/249 ve typecheck/build geçti;son dar düzeltmeler sonrası kontroller tekrar edilecek. GerçekAPI/izolePG tarayıcı köprüsü hazırlanıyor. Resmi01/02 TAMAMLANDI,03 DEVAM EDIYOR;faz2/3=%66,67,plan3/8=%37,50. #47açık/In progress;03gerçekSMSaçık. BF-LATE/eşzamanlı değişiklikler bu kapsamda değildir ve korunur.

## BF-LATE FAZ 00–07 — 23.10.2026 — TAMAMLANDI

**[AKIŞ BF-LATE | 8 faz] 8/8 = %100,00.** Property bazlı uygula anahtarı (kayıt yoksa kapalı), tarihsel oran politikası (taslak/onay, ≤%5, kanuni dışı oran dayanak+belge+ikinci onay), gün/30 basit tazminat motoru, kesinleşen tahakkuk/kalem, ay sonu Worker job'ı, tahsilatta PRINCIPAL/PENALTY dağıtımı, geriye dönük ödeme/iptal yeniden hesabı, web Aidat sekmeleri ve ödeme formu, mobil ayrı tazminat gösterimi, yıllık rapor. Migration `20261023125757_LatePenalties`. Kullanıcı kararları: K1 gün/30; K2 operatörün girdiği dağıtım uygulanır (TBK m.100 öneri); K3 yalnız kanuni %5, farklı oran/yöntem kapalı. Bildirim kanalı ve muhasebe modülü mevcut değil. Test özeti TESTING.md. Commit yok. Token/kota: BİLGİ MEVCUT DEĞİL.

## BF-VERIFY FAZ04 — 23.10.2026 — DEVAM EDIYOR

Kullanıcı sıradaki fazı açıkça yetkilendirdi: #47, sıra5/08,3 resmi adım. FAZ03 gerçek SMS teslimi açıkken bu faza geçiş kullanıcı kararıdır;03 tamamlanmış sayılmaz. Ortak /account ekranı ve doğrudan API/auth bağımlılıkları incelendi;başlangıç account/contact testleri7/7 geçti. Adım01 TAMAMLANDI;02 DEVAM EDIYOR. Faz1/3=%33,33;plan3/8=%37,50. Yeni paket/canlı ayar/servis değişikliği yok. Son kota okuması23.10.2026 Codex usage:300dk%83kullanılan/%17kalan,reset16:03:51;10080dk%13/%87,reset30.10.2026 11:03:51 Europe/Istanbul. Giriş/çıkış/toplam token ve kalan context:BİLGİ MEVCUT DEĞİL.

[KULLANIM] Son kaynak:Codex usage23.10.2026;hesap kotasıdır. 300dk%80kullanıldı/%20kaldı,reset23.10.2026 16:03:51;10080dk%12/%88,reset30.10.2026 11:03:51 Europe/Istanbul.
Giriş token: BİLGİ MEVCUT DEĞİL.
Çıkış token: BİLGİ MEVCUT DEĞİL.
Toplam token: BİLGİ MEVCUT DEĞİL.
Kalan context: BİLGİ MEVCUT DEĞİL.

BF-VERIFY03 son ek kontrol (23.10.2026):başlık sonrası takılan HTTP gövdesine ortak süre/boyut sınırı eklendi;yalnız etkilenen taşıyıcı grubu3/3 geçti. Önceki32PG/API+16unit sonucu ayrıca korunur.

## BF-VERIFY FAZ03 — 23.10.2026 — YEREL KABUL TAMAM / GERÇEK SMS AÇIK

Kullanıcı Netgsm/platform maliyetini ve6hane/5dk/5deneme/60sn,hesap+hedef5saat/IP20saat/TRcep politikasını onayladı. Hesap şu anda yok;sağlayıcısız testleri açıkça istedi. **Son tekrar32/32 gerçek izole PG/API/yerelHTTP+16/16 unit,0skip;Worker build0hata0uyarı;web kanıt sözlüğü9/9.** Aynı bekleyen telefon+isim kaydı kodu korur;yanlışkod/replay/race/limit/expiry/cancel/UNKNOWN,JWT,şema ve migration ilk/yükseltme/tekrar/idempotentSQL/Down koruması test edildi. Netgsm taşıyıcı,yeni PhoneOtpChallenge tablosu ve20261023082825migration+Designer+snapshot;ortak account/mobile komutu,ayrı HMAC/şifreleme,mevcut makbuz/kilit/worker kullanıldı. **#46 açık/In progress;10/14 madde tamam,4açık(A1gerçek gönderici,A9gerçek teslim,K2/K4).** Resmi01/03 ve02/03 tamam;03/03 gerçek SMS içinBLOKE:Netgsm alt hesabı/OTP paketi/başlık ve yetkili test alıcısı yok. Faz2/3=%66,67;tamamlanan plan3/8=%37,50;bu tur2/3=%66,67. MOCK/yerel test gerçek operatör teslimi sayılmadı. [Rapor](../refactor/contact-verification/03-report.md). FAZ04 başlatılmadı.

PhoneEnabled defaultfalse;canlı/env/DB/servis/keycloak ayarı veya deployment yapılmadı.Yeni paket/SDK/imaj/sürüm yok;geçici yalnız-kendi test DB/container/env temizlendi.Kaynaklar commit/push edilmedi.Quality/Dockerfile/.nvmrc/lisans/progress eşzamanlı değişiklikleri korundu.

## BF-VERIFY FAZ03 — 23.10.2026 — DEVAM EDIYOR

Kullanıcı FAZ03'ü başlattı;D4 onaylı:6hane/5dk/5deneme/60sn;hesap ve hedef5/saat,IP20/saat,Türkiye+90cep. D3:Netgsm,platform hesabı seçildi;gerçek hesap henüz yok. Kullanıcı sağlayıcı olmadan yerel testlerin yapılmasını açıkça istedi. Mevcut telefon domain regresyonu6/6 geçti. Netgsm HTTP sözleşmesi ve yerel OTP kabulü uygulanıyor;gerçek teslim ayrı kanıt olarak açık kalır. Faz0/3=%0,00;plan3/8=%37,50. Başka değişiklikler korunur.



Son kota okuması: Codex usage /22.10.2026. 300dk kullanılan%98/kalan%2, reset22.10.2026 22:19:52;10080dk kullanılan%93/kalan%7, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.
Giriş token: BİLGİ MEVCUT DEĞİL.
Çıkış token: BİLGİ MEVCUT DEĞİL.
Toplam token: BİLGİ MEVCUT DEĞİL.
Kalan context: BİLGİ MEVCUT DEĞİL.

## BF-VERIFY FAZ02 — 22.10.2026 — TAMAMLANDI

**#45 Closed(completed), Project3 Done;12/12 madde ve son uzak okuma doğrulandı. Faz3/3=%100,00; BF-VERIFY plan3/8=%37,50.** D1/D2 onaylı; EmailEnabled defaultfalse, hesap bazlı VERIFY_EMAIL/tek davet, davet hata sonucu, yönetici e-posta bypass koruması ve native UPDATE_EMAIL pending/re-auth/eski adrese bildirim tamam. **12/12 gerçek izole IdP/SMTP grubu;8/8 politika;15/15 JWT/adaptör;20/20 web (son sözlük değişimi sonrası11/11 tekrar);19/19 ilgili mobil;web typecheck/build ve .NET0hata0uyarı.** 17 belge/50 yerel bağlantı ve JSON/Python/shell/diff geçti. Geçici test kaynakları temizlendi. Yeni paket/şema/migration yok;canlı realm/.env/DB/hesap/servis değiştirilmedi. Bu çalışma commit/push/deploy yapmadı;eşzamanlı kaynak/index korundu. [Rapor](../refactor/contact-verification/02-report.md). Sonraki:#46/FAZ03;henüz yetkili değil. D3/D4 sağlayıcı/OTP kararı ilgili fazda;gerçek gönderim kapsamı ayrıca netleşir. Üretim FAZ07;bildirim SMTP/DB atomik değil,kalıcı retry yok;güvenli hata izleme/kurtarma kapısı açık. Ortak web/mobil doğrulama UX'i FAZ04/05.

## BF-VERIFY FAZ02 — 22.10.2026 — Doğrulama tamam / kapanış kontrolü

D1/D2 onaylı. #45 uygulama ve zorunlu kabulü tamamlandı;3 resmi adımın son kapanış kontrolü sürüyor. Kapalı varsayılan EmailEnabled, yeni hesap bazlı VERIFY_EMAIL, tek davet/delivery sonucu, yönetici e-posta bypass koruması, native UPDATE_EMAIL re-auth/pending ve eski adrese bildirim. **12/12 gerçek izole Keycloak26.7.5/Mailpit grubu;8/8 politika;15/15 JWT/adaptör;20/20 web;19/19 ilgili mobil;web typecheck/build ve .NET0hata0uyarı.** Paket/SDK/şema/migration yok. Canlı realm/.env/kullanıcı/servis değişmedi;test kaynakları temizlendi. [Rapor](../refactor/contact-verification/02-report.md). Faz2/3=%66,67 (kapanış adımı devam ediyor);tamamlanan plan2/8=%25,00. FAZ03 başlatılmadı.

## BF-VERIFY FAZ 02 — Adım 01 sonucu / Adım 02 DEVAM EDIYOR

D1/D2 kullanıcı yanıtı: önerilen politika onaylı. Yeni hesapta e-posta zorunlu/telefon doğrulaması isteğe bağlı, eski doğrulanmış adres korunur, kontrollü geçiş ve yetkili destek; canlı enforcement açılmaz. Gerçek kurulu sürüm salt okunur kc.sh --version ile26.7.5. Mevcut API davet hatasını yutuyor ve yönetici email alanını doğrudan yazabiliyor. Kapalı varsayılan EmailEnabled politikası ve native akış/bildirim testleri uygulanıyor. İnceleme1/3=%33,33; plan2/8=%25,00.


## BF-VERIFY FAZ 02 — 22.10.2026 — DEVAM EDIYOR

Kullanıcı sıradaki fazı başlattı: #45, liste 3/08, resmi adım 01/03 inceleme. D1/D2 açık politika yanıtı beklenirken yalnız ilgili hesap/Keycloak kodları ve izole test imkânları inceleniyor. Bu tur henüz test çalıştırılmadı; önceki fazın testleri bu turun kanıtı değildir. Canlı ayarlar/veriler ve eşzamanlı çalışmalar korunur. Faz 0/3=%0,00; tamamlanan plan 2/8=%25,00.
## BF-MULTI Ek 04 — 22.10.2026 — Mükerrer kişi engeli — TAMAMLANDI (teknik)

Kullanıcı kararı: aynı kimlik no (T.C./vergi/yabancı), aynı cep telefonu veya aynı e-posta ile ikinci kişi kaydedilemez — yeni modelde müşteri, eski ekranda yönetim alanı kapsamında; 409 `BF_PERSON_DUPLICATE_IDENTITY|PHONE|EMAIL`, diğer kişi sızdırılmaz, kilit altında. DB benzersiz indeksi bilinçli eklenmedi (geçiş ve bilinmeyen mevcut veri). Unit 302/302, kişi suite'leri 41/41, tam regresyon (bağlantı kotası sonrası yeniden koşu dahil) yalnız bilinen S3 testi kırmızı; web 237/237. #42'de doğrulama eylemi ve kanıt belgesi açık. Commit yok. [Ek](../refactor/multi-property/ek-04-duplicates.md). Token/kota: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 09 — 22.10.2026 — Güvenlik, regresyon, geçiş — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 09 | 10 faz, liste 10/10] %100,00; BF-MULTI planı 10/10 teslim = %100,00 (FAZ 09 onay bekliyor).** Mobil bildirim → yetkili bağlama geçiş; güvenlik regresyonu; geçiş/rollout planı, kabul listesi, native kişi tahakkuk tasarımı (kodlanmadı). Backend etkilenen sınıflar 228/1 bilinen S3/5 atlandı, unit 301/301, web 237/237, mobil 131/131, idempotent SQL PASS. **Yayın kapısı kapalı:** mobil audit 24 (18 high), SeaweedFS anahtar rotasyonu kanıtsız. Prod/migration/bayrak işlemi yok; commit yok. [Rapor](../refactor/multi-property/09-report.md). Token/kota: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 08 — 22.10.2026 — İş modülleri bağlam entegrasyonu — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 08 | 10 faz, liste 9/10] %100,00; plan 9/10 = %90,00.** Bağlam doğrulamalı belge indirme, mükerrer saymayan toplu finans özeti, web talep oluşturma/geçmiş, mobil seçili daire özeti. Native kişiye bağlı tahakkuk ve canonical bildirim derin bağlantısı kapsam dışı (belgeli). **Engel:** paralel iletişim doğrulama işinin migration'sız entity'si backend DB testlerini durduruyor. Web 237/237, mobil 130/130, unit 295/295. Migration/paket yok; commit yok. [Rapor](../refactor/multi-property/08-report.md). Token/kota: BİLGİ MEVCUT DEĞİL.

## BF-VERIFY FAZ 01 — 22.10.2026 — Ortak doğrulama temeli — TAMAMLANDI

Kullanıcı ilk uygulama fazını başlattı;#44 Closed(completed)/Project3 Done,12/12 madde tamam son okumayla doğrulandı.Faz3/3=%100,00;BF-VERIFY planı2/8=%25,00;#45–#50 açık/Backlog.Hesap bazlı AccountPhoneVerification,web/mobil ortak GET ve aday POST,IdP e-posta otoritesi,eski profil yazım adaptörü;DTO geriye uyumlu. Yeni migration20261022142952 yalnız tek tablo,backfill yok,dolu Down korumalı. **Build0hata;14/14 gerçek izole PostgreSQL/API/migration/şema+6/6 domain,0atlanan.** Son tekrar photo-only korumasını da kapsar.Kaynakta SMS/e-posta gönderimi veya canlı zorunluluk açılmadı;istemci UX sonraki fazlarda. Canlı .env/DB/Keycloak/servis değişmedi;bu tur commit/push yok. Eşzamanlı BF-MULTI/mobil/indeks farkları korundu;genel ağacın değişmediği iddia edilmez. [Rapor](../refactor/contact-verification/01-report.md) · [ADR](../refactor/contact-verification/01-contract.md). Sonraki:#45/FAZ02,e-posta/onboarding;henüz yetkilendirilmedi.Geçici yalnız-kendi test kaynakları temizlendi.

## BF-VERIFY FAZ 00 — 22.10.2026 — Prompt, görev ve analiz teslimi — TAMAMLANDI

Kullanıcının **önce prompt, görevler ve analiz fazı** kapsamı uygulandı. Kanonik plan [PHASES.md](../../buildingflow-contact-verification-prompts/PHASES.md):8 faz(00–07),her faz3 resmi adım. Prompt paketi12 dosya+analiz/matris2 dosya;65 kabul+32 senaryo. GitHub #43–#50,Project3'e tekil bağlı;#43 Closed(completed)/Done son okumayla doğrulandı;diğer7 görev açıktır. **14 belge/49 yerel bağlantı/faz-task-pano eşliği GEÇTİ;19897 başlangıç kaynak dosyası hash aynı.** Uygulama testi/build/restore/migration/servis/gerçek gönderim yapılmadı;uygulama kodu/manifest/lock/.env/canlı ayarlar değişmedi;commit/push yok. Ürün/İŞ/UI/MOBİL/BF-MULTI tiki değişmedi. D1 e-posta/aktivasyon,D2 eski hesap,D3 gerçek SMS sağlayıcı/maliyet,D4 OTP politikası ilgili fazların karar kapısıdır;FAZ00 eksikliği değildir. [Ayrıntılı analiz ve kanıt](../refactor/contact-verification/00-analysis.md). Sonraki mesaj: kullanıcı FAZ01'i başlattı;güncel kapsam üst kayıttadır.

## BF-MULTI FAZ 07 — 22.10.2026 — Mobil offline önbellek — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 07 | 10 faz, liste 8/10] %100,00; plan 8/10 = %80,00.** Mobil şema 9 `portfolio_cache` (yalnız durum/yetenek kodları), çevrimdışı son doğrulanmış portföy, durum rozeti ve yetenek bazlı talep bölümü; iptal bağlam kuyruğu mevcut `blocked` davranışıyla. Mobil check 129/129, build PASS. Backend/paket yok; commit yok. [Rapor](../refactor/multi-property/07-report.md). Token/kota: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 06 — 22.10.2026 — Mobil portföy — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 06 | 10 faz, liste 7/10] Adım = %100,00; plan 7/10 = %70,00.** Canonical mobil: Portföyüm (alan→daire→rol çipleri, seçili bağlam göstergesi, görevler ayrı), çoklu bağlamda otomatik seçim yok. Mobil check 128/128, build PASS. Backend/migration/paket yok; commit yok. [Rapor](../refactor/multi-property/06-report.md). Token/kota: BİLGİ MEVCUT DEĞİL.

## BAKIM — 22.10.2026 — Hesap iletişim bilgileri — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** E-posta aslında Hesap bölümündeydi; Kişisel bilgilerde telefonun yanına salt okunur taşındı, kaynak mevcut giriş hesabı. Kullanıcının ek isteğiyle bakım kapsamına yeni kullanıcı cep telefonu da alındı; tek bakım adımı olarak tamamlandı. İsteğe bağlı `CreateUserRequest.Phone`, mevcut ortak MobileProfile'a ilk kayıt olarak ve üyelikle aynı yerel transaction'da kaydediliyor; mevcut profil üzerine yazılmaz. Hesap sahibi web/mobilde okur ve günceller. Keycloak telefon attribute'ı, Person kaydı, PHOTO rızası ve dizin telefon okuma yetkisi açılmadı. **27/27 izole PostgreSQL/S3 fixture entegrasyonu;235/235 FE; typecheck/build PASS.** Gerçek bileşenli fixture1440/375px: readonly e-posta doğru bölümde, telefon girişi/biçimleme ve yatay taşma0; canlı kullanıcı veya gerçek karşılama e-postası işlemi yapılmadı. Yeni paket/entity/migration yok; ilgili contract/security/verify/docs prosedürleri uygulandı. İŞ yol haritası24 kayıt,141/151=%93,38; ürün/UI/BF-MULTI fazı veya görevi tiklenmedi. Başlangıç indeksi/manifest/lock/next-env ve diğer devam eden işler korundu. Engel yok; sonraki kapsam yeni kullanıcı talebidir.

[Dosyalar,komutlar ve görseller](../navigation-refactor/ACCOUNT_CONTACT.md). Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex,22.10.2026 teslim okuması:300dk kullanılan%53/kalan%47,reset22.10.2026 15:39:36;10080dk kullanılan%71/kalan%29,reset27.10.2026 13:32:07 Europe/Istanbul.

## BF-MULTI FAZ 05 — 22.10.2026 — Malik/kiracı web portalı — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 05 | 10 faz, liste 6/10] Adım 7/7 = %100,00 (bu faz); BF-MULTI planı 6/10 faz teslim = %60,00 (FAZ 00–04 onaylı, FAZ 05 onay bekliyor).** Project 3: #37 In progress. `/workspaces/portfolio` Portföyüm (Tüm Dairelerim / Seçili Daire), yetenek tabanlı ikonlu sekmeler, `contextKey` ile bayat veri koruması; yeni salt okunur `/api/v2/me/units/{unitId}/{dues,payments,announcements,documents,leases}` (her istekte bağlam yeniden çözülür). **Backend 84 geçti/0 başarısız/7 S3 atlandı, unit 295/295; web 234/234, typecheck, build; fixture 1440/375 px.** Migration yok; canlı DB/.env/Keycloak/bayrak değişmedi; yeni paket yok; commit yok. #42 (mükerrer kişi) kullanıcı kararı bekliyor. [Rapor](../refactor/multi-property/05-report.md). Sonraki: kullanıcı onayı → FAZ 06 (#38).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap/oturum kotası: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 04 — 22.10.2026 — Merkez Kişiler ekranı — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 04 | 10 faz, liste 5/10] Adım 7/7 = %100,00 (bu faz); BF-MULTI planı 5/10 faz teslim = %50,00 (FAZ 00–03 onaylı, FAZ 04 onay bekliyor).** Project 3: #36 In progress. Kişiler listesi alan seçmeden yetkili dizinden (ikonlu ilişki çipleri, maskeli kimlik); detayda daire başına portföy, alan→blok→daire seçimi, doğrulama rozeti; malik/sakin ayrı form, kiracılık kira sözleşmesi paneliyle (oluştur/ortak kiracı/bağla/bitir, 409 yeniden yükleme); tek gösterimlik hesap bağlama kodu ve bağı sonlandırma. **Web 228/228, typecheck ve build PASS; fixture 1440/375 px taşma 0, konsol hatası 0.** Backend/şema değişmedi; yeni paket yok; commit yok. Açık karar: mükerrer kişi birleştirme ve kanıt belgesi ayrı iş. [Rapor](../refactor/multi-property/04-report.md). Sonraki: kullanıcı onayı → FAZ 05 (#37).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap/oturum kotası: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 03 — 22.10.2026 — Kimlik, çoklu rol ve aktif bağlam — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 03 | 10 faz, liste 4/10] Adım 7/7 = %100,00 (bu faz); BF-MULTI planı 4/10 faz teslim = %40,00 (FAZ 00–02 onaylı, FAZ 03 onay bekliyor).** Project 3: #35 In progress. Yeni: yönetici onaylı tek kullanımlık hesap↔kişi bağlama kodu (HMAC, 72 sa, ortak deneme kilidi, kimlik ek kontrolü, bağ sonlandırma), `GET /api/v2/me/portfolio` (daire başına kapasite/durum/yetenek, yönetim bağlamı ayrı, fingerprint), `GET /api/v2/me/active-context` (sunucu doğrulamalı bağlam). Bağlamlar artık yalnız VERIFIED/LEGACY ve etkin sözleşmeli kiracılıktan. `BF_PERSON_LINK_UNAVAILABLE` (ErrorCatalog + FE TR/EN). Migration `20261022091337_CentralPersonLinkCodes`. **Entegrasyon 223/224 (S3 baseline); unit 295/295; web 221/221 + typecheck.** Canlı DB/.env/Keycloak/bayrak değişmedi; yeni paket yok; commit yok. [Rapor](../refactor/multi-property/03-report.md). Sonraki: kullanıcı onayı → FAZ 04 (#36).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap/oturum kotası: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 02 — 22.10.2026 — Backend ilişkiler ve kişi listesi API — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 02 | 10 faz, liste 3/10] Adım 7/7 = %100,00 (bu faz); BF-MULTI planı 3/10 faz teslim = %30,00 (FAZ 00–01 onaylı, FAZ 02 onay bekliyor).** Project 3: #34 In progress. Yeni: `GET /api/v2/people` (alan seçimsiz, yetkili kapsamlı, maskeli dizin), kişi portföyü endpoint'i, blok seçimi + bina filtreli daire seçimi, kira sözleşmesi API'si (oluştur/taraf ekle/bağla/bitir; VERIFIED aktör, makbuz, version). Kullanıcı kararı: dairede aynı dönemde tek sözleşme — domain + daire kilitli DB trigger; `BF_LEASE_OVERLAP`, `BF_LEASE_PARTY_OUTSIDE` (ErrorCatalog + FE TR/EN). Migration `20261022085436_LeaseContractExclusivityAndOperations`. **Entegrasyon 172/172; unit 293/293; regresyon 74/75 (S3 baseline); web 221/221 + typecheck.** Canlı DB/.env/Keycloak/bayrak değişmedi; yeni paket yok; commit yok. [Rapor](../refactor/multi-property/02-report.md). Sonraki: kullanıcı onayı → FAZ 03 (#35).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap/oturum kotası: BİLGİ MEVCUT DEĞİL.

## BF-MULTI FAZ 01 — 22.10.2026 — Domain model ve migration tasarımı — TEKNİK TESLİM, ONAY BEKLİYOR

**[FAZ BF-MULTI 01 | 10 faz, liste 2/10] Adım 7/7 = %100,00 (bu faz); BF-MULTI planı 2/10 faz teslim = %20,00 (FAZ 00 onaylı, FAZ 01 onay bekliyor).** GitHub Project 3'e #32–#41 eklendi; #32 Done, #33 In progress. BF-PERSON canonical modeli üzerine ekleyici: `LeaseContract` + `UnitTenancy.lease_contract_id` (ortak kiracı/yenileme), dört tabloda doğrulama alanları (LEGACY/PENDING/VERIFIED/REJECTED, doğrulayan hesap, zaman, aynı Property kanıt belgesi), yönetici yazımı VERIFIED, legacy aktarım ve mevcut satırlar LEGACY (uydurma doğrulayan yok), saf `PersonPortfolio` read model (endpoint yok). Migration `20261022083050_LeaseContractsAndRelationVerification`, dolu Down reddi. **Unit 292/292; ilgili entegrasyon 81/81 + 4/4; regresyon 85/86 (S3 baseline).** Canlı DB/.env/Keycloak/bayrak değişmedi; yeni paket yok; commit yok. [Rapor](../refactor/multi-property/01-report.md). Sonraki: kullanıcı onayı → FAZ 02 (#34).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap/oturum kotası: BİLGİ MEVCUT DEĞİL.

## BAKIM — 22.10.2026 — Üst ve alt menü oval köşeleri — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** Ortak workspace-header/footer masaüstünde18px,≤650px ekranlarda16px oval köşeli; küçük dış boşluk ve semantik tema sınırı. Menü açılır panelleri kırpılmıyor; radius için overflow:hidden eklenmedi. Yalnız globals.css sunumu değişti; auth/API/kayıt/menü davranışı değişmedi. **221/221 FE, typecheck ve build geçti;1440/320/390px CUA fixture: hesap ve gizlilik menüsü açılıyor, yatay taşma yok, browser error/warn0.** Yeni test, bağımlılık, font/ikon/görsel paketi yok. Backend/mobil testleri bu CSS turunda çalıştırılmadı. Kaynak planlar frontend/buildingflow-ui-plan/MASTER_PROMPT.md ve PHASES.md okundu; tarihsel analiz adımı yeniden başlatılmadı. Mevcut frontendShellAndLogo sunum görevinin ek kanıtıdır; ürün/UI fazı veya görev tiki değiştirilmedi. İŞ planı 24 kayıt, 141/151 = %93,38. Git indeksi/manifest/lock/next-env ve önceki kullanıcı farkları korundu. Engel yok; sonraki kapsam yeni kullanıcı talebidir.

[Değişiklik ve görseller](../navigation-refactor/ROUNDED_MENUS.md). Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex,22.10.2026 teslim okuması:300dk kullanılan%22/kalan%78,reset22.10.2026 15:39:36;10080dk kullanılan%66/kalan%34,reset27.10.2026 13:32:07 Europe/Istanbul.

## BAKIM — 22.10.2026 — Yetkili kullanıcı fotoğrafı — TAMAMLANDI

**Adım01/01; bakım ve bu tur1/1=%100,00.** Kullanıcının açık onayıyla yetkili yöneticinin diğer hesaplara fotoğraf ekle/değiştir/kaldır eylemi; hedef sahibinin güncel PROFILE_PHOTO rızası, mevcut rol/üyelik sınırı, ortak mobil/web pointer/version, actor+target UUID replay, JPEG256KiB/40MP kaynak sınırı ve metadata temizliği korunur. İlk fotoğrafta mevcut dizinden geçerli ad/soyad, mevcut profilde alan koruması. Yeni paket/tablo/migration yok. **24/24 gerçek izole PostgreSQL/özel test bucket entegrasyonu;221/221 FE, typecheck/build; masaüstü/dar ekran fixture+odak/rıza/kaldır kanıtı.** Canlı Keycloak/telefon kabulü çalıştırılmadı. [Dosyalar,komutlar,görseller ve koruma raporu](../navigation-refactor/USER_PHOTO_EVIDENCE.md). İŞ yol haritası24 kayıt,141/151=%93,38; mevcut İŞ19 kanıtı bağlandı, yeni görev/faz tiki yok. Engel yok; sonraki kapsam kullanıcı talebidir.

Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL. 300 dk: kullanılan %8, kalan %92; reset 22.10.2026 15:39:36 Europe/Istanbul. 10080 dk: kullanılan %64, kalan %36; reset 27.10.2026 13:32:07 Europe/Istanbul. Kaynak: Codex usage rateLimitsByLimitId.codex, 22.10.2026 teslim okuması.

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

## BAKIM —21.10.2026 — [BUILDINGFLOW] Yaklaşan önemli tarihler, Planlama & Bakımlar, Duyuru kategorileri — TAMAMLANDI

**Dashboard "Yaklaşan Önemli Tarihler" kartı aidat son ödeme tarihlerini ve açık planları tarih sırasıyla birleştirir (en çok4, aynı gün aidat önce, "N gün kaldı"/"Bugün" rozeti, "Tümünü Gör").** Yeni Planlama & Bakımlar ekranı (`/properties/{id}/maintenance`, Operasyon menüsü, CalendarCheck2): Bakım/Temizlik/Kontrol/Tatbikat/Diğer ikonlu tür çipleri ve sayıları, arama, tarih aralığı, durum, tablo+mobil kart, sağ panelde oluştur/görüntüle/düzenle/sil, 409 güncel veri bandı. "Planlı bakım" KPI'si artık gerçek30 günlük sayıyı gösterir. Duyurular: Genel/Toplantı/Aidat/Bakım/Acil/Diğer kategorisi, ikonlu rozet, sayılı filtre çipleri ve formda ikonlu seçim. [API](../api/maintenance.md), [Duyurular](../api/communication.md). Yeni paket/font/ikon seti yok (mevcut lucide).

**Backend:** `maintenance_plans` tablosu (BaseEntity, Property FK Restrict, check constraint), `announcements.category` (default GENERAL, mevcut kayıtlar backfill, check constraint). Migration `20261021103545_PlanningAndAnnouncementCategories` yalnız ekleme; Down veri kaybını engeller. `BF_PLAN_INVALID` ErrorCatalog/error-codes/TR-EN apiErrors eşliği. Plan geçmişi olan Property silinemez. Eski istemcinin kategori göndermeyen düzenlemesi kategoriyi korur.

**Test:** Unit281/281, ilgili integration66/66 (7 skip: S3 depolama gerektiren mevcut belge testleri), FE201/201, typecheck/üretim build PASS. Yeni: plan domain3, Maintenance API3 kapsamlı senaryo (CRUD/version/soft-delete/rol/ACCOUNTANT salt okuma/pasif parent/filtre/sayım/upcoming/kategori), migration upgrade/backfill/Down guard1, FE model/dashboard birleştirme/menü6. Test API fixture ile 1440/375px dashboard, liste, panel, duyuru ekranları taşma0/console error0. Fixture canlı OIDC/API kabulü değildir. Önceki tam koşuda baseline'da da kırmızı olan MobileCanonicalSync fotoğraf testi bu işten bağımsızdır.

Commit/push/deploy yok. Tek agent. Token giriş/çıkış/toplam/kalan context: her biri BİLGİ MEVCUT DEĞİL. Kota bilgisi BİLGİ MEVCUT DEĞİL. Görev: GitHub issue #31.

## BAKIM — 21.10.2026 — MET Norway — TAMAMLANDI

**Kullanıcı onaylı MET Norway hava tahmini bağlandı:** kişisel İstanbul varsayılanı, İstanbul/Ankara/İzmir/Artvin seçimi, sıcaklık/koşul ikonu, sağlayıcı güncelleme zamanı, otomatik kontrol ve ayrı Hava durumunu yenile düğmesi. Şehir/hesap değişiminde istek iptal edilir; eski şehrin sıcaklığı gösterilmez. Görünür sayfada cache zamanına göre süreli kontrol, gizli sayfada sorgu yok; bu davranış kod incelemesiyle doğrulandı, dakikalar süren otomatik süre dolumu tarayıcıda ayrıca beklenmedi.

**Lisans:** açıkça onaylanan veri bağlantısı CC BY4.0 koşullarıyla; MET Norway kaynak ve lisans bağlantıları görünür, seçili zaman/yuvarlama açıklaması var. Yeni npm/NuGet paketi, sağlayıcı ikonu, font veya görsel eklenmedi; paket envanteri değişmedi. Kaynak/ücretsiz kullanım/servis koşulları resmî sitelerden bu tur doğrulandı. API uygulama adıyla tanımlanır; isteğe bağlı Weather:ContactUrl gerçek HTTPS iletişim bağlantısını ekler. Uydurma iletişim bilgisi yok. Yeni konum izni yok, tarayıcı IP/koordinat/tokenı sağlayıcıya gönderilmez; yaklaşık şehir merkezleri kullanılır. Ücretli abonelik/API anahtarı gerektirmez; hizmet SLA garantisi vermez.

**Sunucu:** Application DTO/arayüz + Infrastructure MetWeatherProvider + yetkili API endpoint. Sabit HTTPS hedef, yönlendirme kapalı,8sn timeout,1MiB gövde sınırı; dört şehir cache'i, şehir başına tek istek, Expires/Last-Modified/304 ve Retry-After. Bekleme içinde Yenile sağlayıcıya tekrar yük bindirmez. Hata/bozuk veri normal UNAVAILABLE veya son başarılı veriyle STALE; ham exception/sağlayıcı yanıtı render edilmez. Kişisel hava verisi Property finans kapsamı değildir; CanManageDues rol/sub ve mevcut no-store/rate limit korunur. Model tahmini anlık istasyon ölçümü diye gösterilmez. Tablo/migration/DB yazımı yok.

**Bu tur test:** `npm test` **195/195**, fail/skip0; `npm run typecheck` ve `npm run build` **PASS**. Backend **11/11**, fail/skip0:6 sağlayıcı parser/cache/singleflight/304/backoff/bozuk-birim-sıcaklık testleri ve5 API testi (dört finans rolü, anonim/resident/sahte rol,400 katalog kodu, no-store). API test fixture'ı yalnız ayrı geçici PostgreSQL DB/rolünde çalıştı ve kendine ait kaynakları temizledi; hava testlerinde dış HTTP sahteydi. Ayrı gerçek MET isteği İstanbul için **HTTP200**, Celsius/90 kayıt, Expires/Last-Modified ile başarılı; bu bir canlı OIDC/tam üretim oturumu testi değildir.

**UI fixture:** İstanbul18°C, Yenile düğmesi API'yi tekrar çağırdı; İzmir24°C'ye geçiş, Ankara STALE uyarısı, Artvin UNAVAILABLE/— görünümü, İstanbul'a dönüş başarılı. 320px viewport=document320, yatay taşma yok; tarayıcıerror0. Bu sıcaklıklar örnek fixture verisidir. [Görsel](../navigation-refactor/DASHBOARD_MET_WEATHER.jpg), [makine kanıtı](../navigation-refactor/DASHBOARD_MET_WEATHER_EVIDENCE.json), [tam teslim/önceki seçenekler](../navigation-refactor/DASHBOARD_WEATHER.md), [API sözleşmesi](../api/dashboard.md), [lisans kaydı](../licenses.md). Gerçek cihaz/üretim dağıtımı yapılmadı. Fixture/sekme kapatıldı, viewport sıfırlandı.

Değişen dosyalar: Weather.cs, MetWeatherProvider.cs, WeatherEndpoints.cs, Program.cs'nin yalnız servis/endpoint kaydı; DashboardHero/WeatherLocation/weather.ts, CSS, TR/EN, fixture, WeatherApiTests/MetWeatherTests/weather.test.mjs ve mevcut belge kaynakları. Başlangıç Git indeksi/manifest/lock ve next-env korundu; diff check PASS. Önceki sistem durumu/diğer kullanıcı farkları korunur. Tek agent akadev-contract/security/license/verify/docs; yeni agent/MCP kurulmadı. **Bu tur 1/1=%100,00**; ürün/UI yol haritası/faz işaretleri değişmedi, tüm yol haritasının yeni hesaplanan yüzdesi BİLGİ MEVCUT DEĞİL.

Giriş token'ı: BİLGİ MEVCUT DEĞİL. Çıkış token'ı: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context kapasitesi: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex teslim okuması:300dk kullanılan%0/kalan%100, reset22.10.2026 03:18:13 Europe/Istanbul;10080dk kullanılan%47/kalan%53, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası bu sohbetin token tüketimi değildir.

Engel yok; onaylanan MET entegrasyonu tamamlandı. Sonraki adım yeni kullanıcı talebi; başka faza geçilmedi.

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

## BAKIM —21.10.2026 — Dashboard doluluk halkası — DEVAM EDIYOR

Türdağılımı kartı yerine referans doluluk halkası/gerçekdolu-boş sayı/alanınbölümlistesine bağlantı. Beklemedeverisi mevcutAPIde yok;uydurma0/segment yok,belirsiz — satırı. Kabul:sıfır/tam/karmaoran hesapları,FEtest/typecheck/build,desktop/mobile/alanseçimfixture. Bu tur0/1=%0,00;sidebar8/8korunur. Yeni paket/API/DB yok.

## BAKIM —21.10.2026 — Referans sol menü — TAMAMLANDI

**Solmenü referans lacivert/cyan yuvarlatılmış çerçeve,maviaktif,arama,ikonlu ana ve çizgili altmenülerle uyarlandı.** Mevcutroute/rol/flag/disabled/APARTMENT ve profilkorundu;altalanseçimi geri eklenmedi. [Rapor](../navigation-refactor/SIDEBAR_REFERENCE.md),[görsel](../navigation-refactor/SIDEBAR_REFERENCE_DESKTOP.jpg). WorkspaceShell,CSS,TR/EN,sidebar-layout helper/test ve mevcut testkaynak eşliği değişti;backend/API/DB/paket/asset yok.

**186/186FE,0fail/skip;typecheck/build PASS.** Yeni2test192kombinasyonda öğe/route/izin eşliği;fixture1440x1000,76pxrail,KİŞİarama,Finansaktiflik,375/320drawer,arama sonrası içerikodağı/locktemizliği ve consoleerror0 PASS. CanlıOIDC/gerçekcihaz/backend testleri bu tur koşulmadı. Başlangıçindeks/manifest/lock/next-envkorundu,diffcheckPASS;fixture/sekme kapalı,viewportreset. Bu tur1/1=%100,00;sidebar8/8=%100,00korunur;ürünroadmap değişmez. Tekagentverify/docs;sonraki kullanıcıincelemesi.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage21.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%29/kalan%71 reset21.10.2026 16:14:02;10080dk kullanılan%36/kalan%64 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## BAKIM —21.10.2026 — Referans sol menü — DEVAM EDIYOR

Referans lacivert/cyan çerçeve,maviaktif,dikey ikon/satırlar,arama ve iç içe altmenü sunumu uygulanır. Mevcutroute/rol/flag/APARTMENT sınırı korunur;önce kaldırılan altalanseçimi geri eklenmez,profilkartı korunur. Kabul:rol/route öğe eşliği,FEtest/typecheck/build ve desktop/rail/mobile fixture. Bu tur0/1=%0,00;sidebar8/8korunur. Yeni paket/asset/API yok.

## BAKIM —21.10.2026 — Sidebar alt alan seçimi — TAMAMLANDI

**Sidebar/drawer footer Yönetim alanı seçim bağlantısı kaldırıldı.** WorkspaceShell.tsx içinde tek ortak render ve kullanılmayan import kaldırıldı;profil,ana Yönetim Alanları menüsü ve Dashboardheaderseçimi korunur. **184/184FE,typecheck/üretim build,diffcheck PASS.** Bu küçük kaldırma için yeni test yazılmadı;tarayıcı/backend testleri bu tur koşulmadı. Başlangıçindeks/manifest/lock/next-env korundu;build üretimi exactimport kontrolüyle geri alındı. Yeni paket/API/DB yok. Bu tur1/1=%100,00;sidebar8/8korunur. Tekagent verify/docs;sonraki kullanıcıincelemesi.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage21.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%3/kalan%97 reset21.10.2026 16:14:02;10080dk kullanılan%33/kalan%67 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —21.10.2026 — Sidebar alt alan seçimi — DEVAM EDIYOR

Kullanıcı talebiyle sidebar/drawer footer Yönetim alanı bağlantısı kaldırılır;profil ve Dashboard headerseçimi korunur. Bu tur0/1=%0,00;sidebar8/8korunur. Kabul:FEtest/typecheck/build; yeni paket/API yok.

## BAKIM —20.10.2026 — Dashboard sütun grafiği — TAMAMLANDI

**Gelir/gider trendi referanstaki gibi yan yana mavigelir/pembegider sütunlarına dönüştü.** Son6/3ay seçimi mevcut6ayAPI verisini dilimler;backend/tutarhesabı değişmedi. Yuvarlak eksen,klavye/hover/touch kesinTRYtooltip ve ekranokuyucu tutar tablosu korunur. TrendChart.tsx/trend-bars.ts,DashboardPage,globals.css,TR/EN ve trend-bars.test.mjs güncellendi/eklendi. Yeni paket/font/asset yok.

**184/184FE,0fail/skip;typecheck/üretim build PASS.** Yeni2geometri testi:sıfır/küçük/büyüktutar ekseni,240–1100px/3–6ay çakışmayan ikilisütun. GerçekReact/testAPIfixture1440px6ay12sütun,3ay6sütun/3tablosatırı,TabilekesinTRY,320pxchart262/doc320taşma0,consoleerror0 PASS. [Görsel](../navigation-refactor/DASHBOARD_TREND_BARS.jpg). Bu fixturecanlıOIDC/APIkabulü değildir;backendintegration bu saltfrontend turunda tekrar koşulmadı. Başlangıçindeks/manifest/lock/next-env ve diğer kullanıcıfarkları korundu;diffcheckPASS;fixture/sekme kapalı,viewportreset. Tekagent mevcutverify/docs;bu tur1/1=%100,00,sidebar8/8korunur;yeni faz yok.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%97/kalan%3 reset20.10.2026 23:36:32;10080dk kullanılan%31/kalan%69 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir. Sonraki:kullanıcıincelemesi.

## BAKIM —20.10.2026 — Dashboard sütun grafiği — DEVAM EDIYOR

Referansa göre yan yana mavigelir/pembegider sütunları,son6/3ay yerel seçim,gerçek veri ve erişilebilir tutarlar korunur. Yeni paket/API yok. Kabul:geometri testleri,FEtypecheck/build,desktop/mobile fixture ve klavye/filtre. Bu tur0/1=%0,00;sidebar8/8 korunur.

## BAKIM —20.10.2026 — Dashboard fotoğraflı banner — TAMAMLANDI

**Alan combosu headerda dil önüne taşındı;yalnız Dashboard filtrelenir. Fotoğraflı banner,önplan alan/marka/gerçeközet ve Europe/Istanbul saat/tarih paneli uygulandı.** Yönetici fotoğrafı mevcut özelbelgeupload ile ALL_MEMBERS olarak ortak kaydeder;ikinci üye aynıgörseli alır. Yeniendpoint/tablo/migration/bağımlılık yok;mevcut özgünkonutasset yeniden kullanıldı. Hava sağlayıcısı yok:sıcaklıkverisi yok durumu. [Teslim/sözleşme/geri alma](../navigation-refactor/DASHBOARD_HERO.md),[masaüstü](../navigation-refactor/DASHBOARD_HERO_DESKTOP.jpg),[mobil](../navigation-refactor/DASHBOARD_HERO_MOBILE.jpg).

**182/182FE,23/23izolePGstorageintegration(17communication+6dashboard),0fail/skip;typecheck/build PASS.** Yeni5FEsenaryo ve1ortakfotoğrafintegration. GerçekReactfixture1440/375/320,pickerönceliği,48→geri96,eskikart/fotoğraftemizliği,UIJPEGhazırlama/save/BlobSHA,consoleerror0 PASS. FixtureAPIUIakışı gerçekpersistenceyerine sayılmaz;ikinciüye/replay/yetki/alan kanıtı gerçekizolebackendtestidir. TestDB/rol/fixturebucket temiz,fixture/sekme kapalı,viewport reset. CanlıOIDC/gerçekcihaz/tamhavaservisi kabulü DOĞRULANAMADI. Bu tur1/1=%100,00;sidebar8/8=%100,00 korunur;İŞ20bakım/gerçekUATgörevleri tiklenmedi. Ürün genel yolharitası yüzdesi BİLGİ MEVCUT DEĞİL.

Başlangıç indeks/manifest/lock/proje/migration/lisans/next-env korundu;build üretimi yalnız exactimportfarkıyla geri alındı. Diğer kullanıcı farkları korunur;commit/push/deploy/reset yok. Üretimbackend değişmedi;backend yalnızCommunicationApiTests ekidir. Frontend DashboardArea/area-selection/DashboardHero/banner,DashboardPage,WorkspaceShell,SessionControls,return-path,CSS,TR/EN,fixture/test vephotoasset;API/frontend/lisans/progress belgeleri güncellendi. OIDC dönüşüne yalnız `/dashboard?property=tekUUID` allowlist eklendi,negatif parametre testleri var. Tekagent contract/security/verify/docs/license;imagegen yalnız okundu,üretim yapılmadı.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%82/kalan%18 reset20.10.2026 23:36:32;10080dk kullanılan%29/kalan%71 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir. Sonraki:kullanıcıincelemesi,yeni kapsam kendiliğinden açılmaz.

## BAKIM —20.10.2026 — Dashboard fotoğraflı banner — DEVAM EDIYOR

Güncel onay: alan combosu headerda dil öncesine,yalnız Dashboard seçimi;fotoğraflı hero/özet/saat. Fotoğraf alan üyelerine ortak olacak. Mevcut özel StoredDocument yükleme/indirme ve ALL_MEMBERS görünürlüğü yeniden kullanılacak; yeni backend/migration/paket yok. Yeni UUID ile immutable görsel dokümanı,son geçerli banner kullanılır. SaatEurope/Istanbul;hava servisi yoksa veri yok durumu. Bu tur0/1=%0,00;sidebar8/8 korunur. Kabul:model/negatif/yarış/FEtypecheck-build,gerçekReactfixture320/375/1440,mevcutstorageyetkiintegration.

## BAKIM —20.10.2026 — Dashboard kompakt KPI — TAMAMLANDI

**Dashboard özet kutuları daha kompakt:** globals.css içinde desktop padding18→14px,mobil14→12px,gap12→8px,ikon44→32px (yalnız KPI),ikonSVG20→17px,değer22→20px;mobil değer17px kalır. Kolon/veri/API korunur. **Typecheck ve üretim build PASS;1440/375px gerçek üretimCSS+temsiliKPI markup önizlemesi PASS**, desktop kart136,5px,padding14px,gap8px,ikon32px,değer20px;mobilpadding12px,ikon32px;docscrollWidth viewport ile eşit. [Görsel](../navigation-refactor/DASHBOARD_COMPACT.jpg). Bu statik stil kabulüdür; gerçek kimlikli Dashboard/API kabulü değildir. Unit/integration bu yalnız CSS turunda tekrar koşulmadı; yeni test yazılmadı. Diff/başlangıçindeks/manifest/lock/next-env koruması PASS. Fixture/sekme kapalı,viewport reset. Yeni bağımlılık/asset yok;genel dashboard-icon diğer kullanımları korunur. Bu tur1/1=%100,00;sidebar8/8=%100,00 değişmez. Tekagent verify/docs;iptal edilen header alan seçicisi başlatılmadı.

Token giriş/çıkış/toplam/kalancontext:her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%44/kalan%56 reset20.10.2026 23:36:32;10080dk kullanılan%22/kalan%78 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —20.10.2026 — Dashboard kompakt KPI — DEVAM EDIYOR

Özet kutularında boşluk/ikon/değer boyutu azaltılır; veri ve kolon düzeni korunur. Kabul:typecheck/build ve gerçekCSS ile masaüstü/mobil kart ölçümü. Bu tur0/1=%0,00;sidebar8/8=%100,00 korunur. Yeni bağımlılık yok.

## BAKIM —20.10.2026 — Dashboard header alan seçimi — İPTAL EDİLDİ

Kullanıcı talebiyle çalışma iptal edildi. Yalnız mevcut kaynaklar incelendi ve başlangıç kayıtları yazıldı; seçici için uygulama kodu/test/bağımlılık/API değişikliği yapılmadı. Tamamlanma iddiası yok; bu iş sürdürülmeyecek. Önceki sağ üst profil düzeltmesi ve sidebar8/8 teslimi korunur.

## BAKIM —20.10.2026 — Dashboard header alan seçimi — DEVAM EDIYOR

Dil öncesi yönetim alanı combobox; kullanıcı düzeltmesi: yalnız Dashboard filtrelenir, fotoğraf sonraya bırakılır. Mevcut yetkili aktif alan API listesi/UUID ve dashboard endpointi kullanılır. Diğer modül kapsamları korunur. Kabul: seçim/deep-link/geri-ileri/gecikenyanıt/rol-boş-hata,header keyboard/mobile;FE test/typecheck/build. Bu tur0/1=%0,00;sidebar8/8=%100,00 korunur.

## BAKIM —20.10.2026 — Sağ üst profil — TAMAMLANDI

**Sağ üst header kullanıcı adı/rolü/profil menüsü masaüstünde geri getirildi.** SessionControls profileInSidebar parametresi ve desktop gizleme CSS'i kaldırıldı; WorkspaceShell normal SessionControls kullanır. Sidebar footer aynı provider ile korunur. Bu tur WorkspaceShell.tsx,AuthGate.tsx,globals.css ve ilgili teslim/progress belgeleri değişti; yeni paket/auth/API/DB değişimi yok. **177/177FE,typecheck/build PASS;1440px fixture header ad/rol ve Hesap/API/Çıkış menüsü PASS.** Gerçek OIDC/çıkış çalıştırılmadı; mobil davranış bu tur yeniden kabul edilmedi. [Görsel](../navigation-refactor/HEADER_PROFILE_RESTORED.jpg). Başlangıç indeks/manifest/lock/next-env korundu, fixture/sekme kapalı. Bu tur1/1=%100,00; sidebar8/8=%100,00 değişmez. Tekagent verify/docs; yeni faz yok.

Token giriş/çıkış/toplam/kalancontext: her biri BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%27/kalan%73 reset20.10.2026 23:36:32;10080dk kullanılan%20/kalan%80 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —20.10.2026 — Sağ üst profil — DEVAM EDIYOR

Kullanıcı talebiyle header profil bilgisi masaüstünde yeniden gösterilir. Mevcut ortak provider/menu kullanılır; footer korunur. Bu tur0/1=%0,00; sidebar8/8=%100,00 korunur. Kabul: typecheck/build,desktop görünür ad/rol ve menü etkileşimi. Yeni bağımlılık/auth değişimi yok.

## SIDEBAR FAZ07.1 — Aşama08 —20.10.2026 — TAMAMLANDI

**IMPLEMENTATION, TEST_RESULTS ve LICENSE_CHECK teslim edildi; geri alma planı ve açık manuel QA listesi hazır.** [Teslim](../navigation-refactor/IMPLEMENTATION.md), [test özeti](../navigation-refactor/TEST_RESULTS.md), [lisans kontrolü](../navigation-refactor/LICENSE_CHECK.md). Bu tur yalnız belge; bağlantı/sayaç/yönerge/kaynak-dependency/indeks koruması ve diff check PASS. Uygulama testleri tekrar koşulmadı; Aşama07'nin177/177FE,277/277unit,83/83seçiliintegration ve typecheck/build sonuçları tarihli kanıt olarak aktarıldı. Yeni paket/asset/API/DB/auth/rol değişimi yok. Gerçek OIDC/iş ekranı/offline ağ/native/ekranokuyucu/aktif reduced-motion kabulü DOĞRULANAMADI; mevcut scopedSMS dönüş kısıtı kayıtlı.

**SIDEBAR8/8=%100,00 teknik teslim; bu tur1/1=%100,00.** İŞ07.1 hata altyapısı ve ayrı UI/ürün roadmap sayaçları değiştirilmedi; bütün ürün yol haritası güncel yüzdesi BİLGİ MEVCUT DEĞİL. Yeni faz başlatılmaz. Mevcut staged/unstaged/untracked kullanıcı farkları korundu; commit/push/deploy/reset/clean yok. Tek agent phase/docs/license; yeni skill/MCP/agent yok.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codex usage20.10.2026 rateLimitsByLimitId.codex:300dk kullanılan%21/kalan%79, reset20.10.2026 23:36:32;10080dk kullanılan%19/kalan%81, reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet token tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama08 —20.10.2026 — DEVAM EDIYOR

Onaylı son aşama: IMPLEMENTATION, TEST_RESULTS, LICENSE_CHECK; geri alma ve manuel QA. Bu tur yalnız belge, yeni uygulama testi yok. Sidebar7/8=%87,50; bu tur0/1=%0,00. Tek agent phase/docs/license; uygulama/indeks/bağımlılıklar korunur.

## SIDEBAR FAZ07.1 — Aşama07 —20.10.2026 — TAMAMLANDI

**177/177 frontend,277/277 unit,83/83 seçili izole PostgreSQL integration; typecheck/build PASS.** Lint scripti yok. Fixture modül/geri-ileri/doğrudan ikinci alan/mobile drawer smoke PASS; gerçek kimlikli iş ekranları/OIDC ve ağ kesintisinde offline kabulü DOĞRULANAMADI. [Rapor](../navigation-refactor/STAGE_07.md). Bu tur uygulama kodu değişmedi; DB/rol fixture temizlendi, sunucu/sekme kapalı. Başlangıç indeks/manifest/lock/next-env/.env korundu. Aşama07 1/1=%100,00; SIDEBAR7/8=%87,50; ürün roadmap değişmez. Aşama08 onayı beklenir.

Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%14/%86 reset20.10.2026 23:36:32;10080dk%18/%82 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama07 —20.10.2026 — DEVAM EDIYOR

Yalnız Aşama07: FE test/typecheck/build, mevcut komut envanteri, izole backend yetki/alan regresyonu ve UI navigasyon smoke. Lint scripti yok. Canlı OIDC/gerçek tenant ve offline emülasyon kabulü fixture sonucundan ayrı tutulur. Sidebar6/8=%75,00; bu tur0/1=%0,00. Aşama08 başlamaz; yeni paket/backend/auth/DB şeması değişimi yok.

## SIDEBAR FAZ07.1 — Aşama06 —20.10.2026 — TAMAMLANDI

**Ortak profil/rol footer'a, mevcut alan seçimi modül girişlerine bağlandı.177/177 FE,typecheck/build ve desktop/rail/mobile fixture PASS.** Stale subject guard; accountChanged eşliği; yeni yetki/hesap state'i yok. Font bakımı ortak12px. Sidebar6/8=%75,00; Aşama06 1/1, birleşik tur2/2=%100,00. GerçekOIDC/çıkış/fotoğraf/cihaz kabulü yapılmadı. [Teslim](../navigation-refactor/STAGE_06.md). Aşama07 onayı bekler; genel roadmap değişmez. Başlangıç indeks/dependency/next-env hashleri korundu; fixture kapalı. Tekagent phase/verify/docs/security.

Token giriş/çıkış/toplam/kalancontext:BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%99/%1 reset20.10.2026 18:32:07;10080dk%15/%85 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama06 —20.10.2026 — DEVAM EDIYOR

Profil/rol mevcut oturumdan footer'a; alan bağlamı mevcut property/module seçim akışına bağlanır. Yeni yetki/hesap değiştirme yok. Sidebar5/8=%62,50; bu tur0/1=%0,00. Aşama07 başlamaz.

## BAKIM —20.10.2026 — Menü font eşliği — TAMAMLANDI

**Sidebar13px/üst arama12px farkı ortak --shell-menu-font-size:12px tokenıyla giderildi.** globals.css değişti; typecheck/build PASS. İzole tarayıcıda17desktop+17mobil bağlantı, planlı öğe ve üst arama computed12px doğrulandı. Unit suite bu stil düzeltmesinde yeniden koşulmadı. Fixture/sekme kapalı; dependency/next-env/indeks başlangıcı korundu. Bu tur1/1=%100,00; sidebar5/8=%62,50. [Görsel](../navigation-refactor/MENU_FONT_FIX.jpg). Token giriş/çıkış/toplam/kalancontext:BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%68/%32 reset20.10.2026 18:32:07;10080dk%11/%89 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## BAKIM —20.10.2026 — Menü font eşliği — DEVAM EDIYOR

Kullanıcının düzeltmesi: sidebar13px/üst arama12px farkı ortak12px token ile eşitlenecek. Bu tur0/1=%0,00; sidebar5/8=%62,50 korunur. Yeni faz/paket yok.

## SIDEBAR FAZ07.1 — Aşama05 —20.10.2026 — TAMAMLANDI

**Drawer pathname/desktop geçişinde kapanış, aktif-link/main/trigger odak yönetimi ve reduced-motion kuralları uygulandı.173/173 FE,typecheck/build PASS.** İzoleReact/Radix320/375/768/1024/1440px taşma0; focus-trap/Escape/backdrop/sonlink/back-forward/programatikrota/scrolllock PASS. Kimlik/router/header profili fixture verisidir; gerçekauth/cihaz/ekranokuyucu ve aktif reduced-motion runtime kabulü yapılmadı. [Teslim](../navigation-refactor/STAGE_05.md), [kanıt](../navigation-refactor/STAGE_05_EVIDENCE.json), [TESTING](../TESTING.md). Fixture sunucusu/sekmesi kapalı, viewport reset. Sidebar5/8=%62,50; bu tur1/1=%100,00. Aşama06 onayı bekler; diğer roadmap sayaçları değişmez. Yeni paket/API/DB/auth değişimi yok; indeks/manifest/next-env başlangıç hashleri korunur.

Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%58/%42 reset20.10.2026 18:32:07;10080dk%9/%91 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama05 —20.10.2026 — DEVAM EDIYOR

Yalnız Aşama05 onaylandı: mobile drawer rota/viewport kapanışı, focus/keyboard/ARIA/reduced-motion ve320/375/768/1024/geniş ekran kabulü. Önceki farklar/indeks korunur. Sidebar4/8=%50,00; bu tur0/1=%0,00. Yeni paket/auth/API/DB yok; Aşama06 başlamaz.

## SIDEBAR FAZ07.1 — Aşama04 —20.10.2026 — TAMAMLANDI

**Altı semantik grup ve görünür menülerde TR/EN case/aksan/Unicode/çoklu kelime arama, boş sonuç ve kapanışta query temizliği uygulandı.173/173 FE,typecheck/build PASS;320rol/flag/alan kombinasyonunda enabled hedefler başlangıçla aynı.** Routesuz mükerrer Kullanıcılar placeholderı kaldırıldı; çalışan route/yetki korundu. Sidebar4/8=%50,00; bu tur1/1=%100,00. GerçekUI DOĞRULANAMADI: oturum yok; frontend geçici başlatıldı ve Keycloak girişine ulaşıldı. Kimlik bilgisi girilmedi; test sekmesi ve kendi geçici frontend kapatıldı. API/DB/.env/rol/paket değişmez. Başlangıç staged/unstaged farklar ve manifest/lock/next-env hashleri korunur. [Teslim](../navigation-refactor/STAGE_04.md), [kanıt](../TESTING.md). Aşama05 kullanıcı onayı bekler; diğer İŞ/UI sayaçları değişmez.

Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage20.10.2026:300dk%20kullanılan/%80kalan reset20.10.2026 18:32:07;10080dk%3/%97 reset27.10.2026 13:32:07 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama04 —20.10.2026 — DEVAM EDIYOR

Yalnız Aşama04 onaylandı: gerçek route/izinleri koruyarak altı grup, yetkili menülerde Türkçe uyumlu arama ve boş sonuç. Sidebar3/8=%37,50; bu tur0/1=%0,00. Önceki staged/unstaged farklar korunur; indeks değişmez. Yeni paket/API/DB yok; Aşama05 başlamaz.

## SIDEBAR FAZ07.1 — Aşama03 —19.10.2026 — TAMAMLANDI

**272/76px navy/blue sidebar, aktif sol vurgu/focus/disabled, mevcut Radix dar mod tooltip ve geniş mod grup aç/kapat uygulandı. UI tercihi yalnız sürümlü collapsed/expanded; storage hata fallback'i var.169/169 FE,typecheck/build PASS; Chrome dar/geniş, yenilemede tercih, klavye tooltip/focus/Escape ve grup görünürlüğü PASS.** Sidebar3/8=%37,50; bu tur1/1=%100,00. Yeni paket/API/DB yok. Başlangıç next-env ve manifest/lock hashleri korunur. İŞ07.1 ve genel roadmap sayaçları değişmez. [Teslim](../navigation-refactor/STAGE_03.md), [kanıt](../TESTING.md). Aşama04 için kullanıcı onayı beklenir; tüm mobil/cihaz/ekranokuyucu kabulü yapılmadı.

Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage19.10.2026:300dk%31/%69 reset20.10.2026 00:50:09;10080dk%99/%1 reset25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama03 — 19.10.2026 — DEVAM EDIYOR

Yalnız Aşama03 onaylandı: navy/blue tasarım, geniş/dar mod, tooltip/grup davranışı ve kişisel veri içermeyen UI tercihi. Sidebar2/8=%25,00; bu tur0/1=%0,00. Yeni paket/API/DB yok; Aşama04 başlamaz.

## SIDEBAR FAZ07.1 — Aşama02 — 19.10.2026 — TAMAMLANDI

**Tipli menü şeması ve mevcut rol/flag/property eşlemesi WorkspaceShell'e bağlandı.167/167 FE, typecheck/build PASS; kaynak/dependency/başlangıç next-env hash koruması PASS.** Sidebar2/8=%25,00; bu tur1/1=%100,00. Aşama03 onayı beklenir. Backend üyelik/yetki son otorite; yeni route/paket/migration yok. Canlı UI/cihaz/backend kabulü koşulmadı. Mevcut scopedSMS OIDC dönüş kısıtı değiştirilmedi. İŞ07.1 hata mesajları ve genel roadmap sayaçları değişmez. [Teslim](../navigation-refactor/STAGE_02.md), [kanıt](../TESTING.md).

Tekagent phase/contract/verify/docs/license. İki mevcut next-env farkı ve kullanıcı plan klasörü korunur; commit/push yok. Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Codexusage19.10.2026:300dk%17/%83 reset20.10.2026 00:50:09;10080dk%97/%3 reset25.10.2026 00:15:34 Europe/Istanbul; hesap kotası sohbet tüketimi değildir.

## SIDEBAR FAZ07.1 — Aşama02 — 19.10.2026 — DEVAM EDIYOR

Kullanıcı yalnız Aşama02 geçişini onayladı. Tipli navigasyon şeması, mevcut rol/bayrak/property koşulları ve görünürlük/nested active/duplicate testleri. Sidebar 1/8=%12,50; bu tur0/1=%0,00. Aşama03 başlamaz; backend/API/DB ve paket değişmez. Başlangıç next-env dosyalarındaki iki mevcut değişiklik korunur.

## WEBSITE FAZ09 —19.10.2026 — Teknik teslim / yayın kabulü açık

**Resmi2/2=%100,00; bu tur2/2=%100,00; WEBSITE9/10=%90,00; İŞ21 10/13=%76,92.** T21.9 teknik doğrulamayla tamamlandı; FAZ06/T21.6,T21.10 ve T21.12 açık. Kaynak BuildingFlow-WebsiteManagement-Prompt-Paketi/PHASES.md; kayıt10/10. Yeni faz yok; üretim onayı verilmedi.

Website CI/test/preflight/standalone Docker/health/SBOM/NOTICE, isteğe bağlı compose profili ve dev.shwebsite eklendi. Türkçe işletim ve yönetici rehberleri hazır. Kanıt75/75PGstorageintegration,277/277unit,161/161admin,22/22website;typecheck/build/HTTPsmoke/DockerPASS. npmaudit0;329lisansmetadata;230lockSBOM/148NOTICE;7kökdosya incelemesi açık. Tarayıcıtemelsemantik/klavye/320/390PASS. FullUAT/Lighthouse/OSgüvenlik/gerçekDNS-TLS/EİDS yapılmadı.

[Ayrıntılı rapor](../website-management/phase-09-report.md), [kanıt](../TESTING.md), [açık kabul #30](https://github.com/AkaBayraK/BuildingFlow/issues/30), [FAZ09 #29](https://github.com/AkaBayraK/BuildingFlow/issues/29). GitHub ilgili görev gövdeleri kanıtla güncellendi; #26 açık, #29 incelemede, #30Ready; diğer ilgili teknik fazlar Done. Alakasız PERSON/MOBIL/TraceFlow kartları korunur. .env/hesap/rol/parola/gerçekDB/API/Worker korunur; fixturekaynaklar temizlendi; commit/push/deploy yok. Tekagent phase/contract/security/verify/license/docs; yeni paket/MCP/skill yok.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalancontext: BİLGİ MEVCUT DEĞİL. Sonquota teslimde güncellenir; hesapkotası bu sohbet tüketimi değildir. Sonraki: #30gerçek kabul girdileri ve kullanıcı incelemesi.

## WEBSITE FAZ09 —19.10.2026 — DEVAM EDIYOR

Kullanıcı FAZ09 ve GitHub proje3 görev güncellemesini onayladı. Kaynak BuildingFlow-WebsiteManagement-Prompt-Paketi/PHASES.md, kayıt10/10,2resmi madde,0/2=%0,00. Teknik test/operasyon/CI/Docker/SBOM ve Türkçe yönetici/işletim belgeleri; açık gerçek yayın kabulü ayrı GitHub issue'da izlenecek. Mevcut #20–29 korunup yapılanlar içine yazılacak; unrelated PERSON/MOBIL/TraceFlow kartlarına dokunulmaz. Yeni paket/hesap/.env/gerçekDB değişimi yok; tek agent phase/contract/security/verify/license/docs.

## BAKIM —19.10.2026 — Website tek giriş düzeni — TAMAMLANDI

**Bu tur2/2=%100,00.** Üst menüde tek Hesabıma Giriş; mükerrer hero portal kartı/footer/mobil menü girişleri kaldırıldı. Rol seçmeden site/all akışına geçilir; backend aynı siteye ait güncel yönetim/malik/kiracı/sakin bağlamlarını döndürür, yetki genişletmez. Eski management/resident deep linkleri uyumludur. Yeni sekme target blank+noopener+noreferrer; asıl website sekmesi korunur. Hero alanı ve mobil logo/giriş/menü ölçüleri düzenlendi; mevcut optimize WebP'ler doğrudan sunularak Next internal image404 giderildi. CMS LOGIN_CARD geçmişi silinmedi, renderdan çıkarıldı.

**32/32 izole integration+8/8 seçili unit,161/161 yönetim+18/18 website,ikiFE typecheck/build/HTTP smoke PASS.** Tarayıcı1280/390/320 tekCTA, taşma0;390pxkırık görsel0; gerçek tıklama yeni sekme açtı ve Keycloak Code/S256 giriş ekranına ulaştı. Parola/kullanıcı bilgisi girilmedi; kimlikli callback/refresh/logout ve gerçek yayın kabulü yapılmadı. Asıl3004yeni görünüm tarayıcıda doğrulandı. Geçici DB/rol temizlendi;13004izole sunucusu kapatıldı;3004asılsekme teslim görünümü olarak açık. .env/hesap/rol/parola/gerçekDB/API korunur. Yeni paket/restore/npmci/lisansenvanter değişimi yok. Tekagent phase/contract/security/verify/docs/license; yeniSkill/MCP yok. WEBSITE8/10=%80,00; İŞ21 9/13=%69,23 korunur, FAZ09 başlamadı.

[Sözleşme](../api/website-login.md), [kanıt](../TESTING.md). Token giriş/çıkış/toplam/kalancontext: BİLGİ MEVCUT DEĞİL. Son Codex kullanım19.10.2026:300dk %61 kullanılan/%39 kalan,reset19.10.2026 19:43:31;10080dk %88/%12,reset25.10.2026 00:15:34 Europe/Istanbul; hesap kotası sohbet tüketimi değildir.

## BAKIM —19.10.2026 — Website tek giriş düzeni — DEVAM EDIYOR

Kullanıcı rol bazlı mükerrer giriş düğmelerini kaldırıp tek profesyonel giriş ve yeni sekme istedi. Bu tur2adım,0/2=%0,00; faz sayaçları değişmez. Üst menü tek ortak Hesabıma Giriş; tekrar eden hero giriş kartı/footer/mobile menü linkleri kaldırılır. Ortak site/all rotası backend güncel yetkili bağlamları birleştirir; önceki management/resident deep link'leri uyumlu kalır. Keycloak/scope kapıları ve .env/hesaplar korunur. Yeni paket yok. Phase/contract/security/verify/docs; tek agent.

## WEBSITE FAZ08 —19.10.2026 — Teknik teslim

**2/2 resmi madde=%100,00; WEBSITE8/10=%80,00; İŞ21 9/13=%69,23.** Bu tur2/2=%100,00. Kaynak BuildingFlow-WebsiteManagement-Prompt-Paketi/MASTER_PROMPT.md ve PHASES.md; ayrı UI planı frontend/buildingflow-ui-plan/ altında. Kök MASTER_PROMPT.md/PHASES.md yok. T21.8 teknik kabulü kanıtla tamamlandı. Yönetim/Sakin linkleri sabit management origin ve exact site/audience dönüş rotasıyla mevcut Keycloak Code+S256 akışına bağlandı. Backend yayımlanmış siteyi mevcut güncel session context'lerine daraltır; audience rol vermez. UI ortak çalışma alanı seçimini kullanır, hesap/site değişiminde önceki kartları temizler; odak/yenileme erişimi tekrar doğrular. Logout bekleyen dönüş path'ini siler.

**32/32 seçili integration(session/yetki/negatif JWT),8/8 seçili unit,161/161 yönetim,18/18 website; ikiFE typecheck/production build ve public HTTPsmoke PASS.** İzole gerçek React UI: yönetim/sakin ayrımı, hesap değişimiyle kart temizliği, iptal sonrası yenileme PASS. Gerçek Keycloak uçtanuca oturum/yenileme kabulü, canlı DNS/TLS ve dağıtım yapılmadı. API/FE session context bayrakları kapalıysa erişim açıklamalı kapalı kalır; gerçek bayrak/.env/hesap/parola/DB/servis değiştirilmedi. Migration ve yeni bağımlılık yok; restore/npmci ve lisans envanteri yeniden üretimi yapılmadı. Geçici test DB/rol temizlendi; UI fixture sunucusu ve sekmesi kapatıldı. Önceki değişiklikler korunur; commit/push yok.

Tek agent; phase/contract/security/verify/docs ve mevcut license prosedürü. [Sözleşme](../api/website-login.md), [kanıt](../TESTING.md). FAZ06 EİDS/hukuki engeli ve FAZ07 canlı DNS/TLS kabulü sürer. FAZ09 başlamadı; sonraki faz için kullanıcı onayı beklenir.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codex usage19.10.2026:300dk %37 kullanılan/%63 kalan,10080dk %85/%15; reset19.10.2026 19:43:31 ve25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

## WEBSITE FAZ08 —19.10.2026 — DEVAM EDIYOR

Kullanıcı FAZ08 onayı verdi. Kayıt09/10; iki resmi madde,0/2=%0,00. Public yönetim/sakin giriş linkleri, sabit yönetim origin ve allowlist dönüş rotası; backend tarafından siteye ve güncel yetkili session context'lerine daraltma. Mevcut Keycloak Code+S256 PKCE korunur; hesap/rol/parola/.env değişmez. Session context bayrakları kapalı ortam açık uyarıyla fail-closed kalır. Yeni paket yok; tekagent phase/contract/security/verify/docs. FAZ09 başlamaz.

## WEBSITE FAZ07 —19.10.2026 — Teknik teslim

**2/2 resmi madde=%100,00; WEBSITE7/10=%70,00; İŞ21 8/13=%61,54.** T21.7 doğrulanmış teknik kapsamla tamamlandı. Kullanıcı FAZ07'yi FAZ06'nın gerçek EİDS/hukuki engeli sürerken açıkça onayladı; T21.6/T21.10 tamamlandı sayılmaz. FAZ08 başlamadı.

Domain yönetimi/DNS TXT/TLS/custom SUPER_ADMIN onayı, güvenli Host çözümleme, canonical/robots/sitemap/JSON-LD, HMAC süreli taslak önizleme ve no-store yayın/restore izolasyonu uygulandı. Yeni migration20261019065513_WebsiteDomains sadece fixture'da uygulandı. Canlı DNS/sertifika/ters vekil, Keycloak ve gerçek dağıtım kabulü yapılmadı; mevcut API/Worker yeniden başlatılmadı, .env/hesap/gerçek DB korunur.

**Faz kanıtı:42/42 seçili PG/storage integration,90/90 seçili unit; tüm277/277 unit;159/159 yönetim;17/17 website.** Typecheck/production build ve HTTP smoke geçti. İzole390px domain paneli/ESC odak ve taşma kontrolü geçti. Bu devam turunda website17test/typecheck/build/smoke tekrar çalıştırıldı; yönetim159test/typecheck/build de tekrar PASS; önceki backend sonuçları tekrar koşuldu sayılmaz. Test UI sunucusu ve sekmesi temizlendi. Yeni paket/restore/npmci yok; tek agent, phase/contract/security/verify/license/docs. [Sözleşme/işletim](../api/website-domains.md), [kanıt](../TESTING.md).

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Son Codex usage19.10.2026:300dk %14 kullanılan/%86 kalan,10080dk %81/%19; reset19.10.2026 19:43:31 ve25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir. Sonraki: canlı DNS/TLS işletim kabulü ve FAZ06 EİDS/hukuki kabulü; FAZ08 için yeni onay beklenir.

## WEBSITE FAZ07 —19.10.2026 — DEVAM EDIYOR

Kullanıcı FAZ07'yi açıkça onayladı; FAZ06 EİDS engeli korunarak ilerlenir. Kaynak BuildingFlow-WebsiteManagement-Prompt-Paketi/{MASTER_PROMPT,PHASES}.md; kayıt08/10,2resmi madde,0/2=%0,00. Domain DNS/TLS/superadmin onayı, güvenlihostçözümleme/SEO ve preview/yayın/rollback izolasyon testleri. Yeni bağımlılık yok; mevcut no-store stratejisi anlık iptal için korunur. Gerçekdomain/DNS/TLSyapılandırması verilmedi; fixture kabulü ile canlı kabul ayrı raporlanacak. Tekagent; phase/contract/security/verify/license/docs. FAZ08 başlamaz.

## WEBSITE FAZ 06 —19.10.2026 — Teknik teslim / gerçek yayın kabulü açık

**Resmi maddeler2/3=%66,67.** İlk madde (isteğe bağlı Unit ve mevcut sahiplik/kiracılığı değiştirmeme) ve ikinci maddenin uygulama/test akışları tamamlandı; üçüncü maddenin EİDS/hukuki kabulü BLOKE. Varsayılan doğrulayıcı kapalıdır: gerçek sağlayıcı/test erişimi ve hukuki kabul olmadan yayın yoktur; yönetici beyanı doğrulama sayılmaz. WEBSITE tamamlanan6/10=%60,00; İŞ21 önceki7/13=%53,85 korunur. T21.6 ve T21.10 done:false, yapılan doğrulamalar aynı yol haritası görevlerine bağlandı. FAZ07 başlamadı.

İlan model/API/UI, özel malik belgesi, max20fotoğraf/sıra, inceleme/red/geriçekme/version/UUID, değişmez olaylar, süre sonu public filtresi+Worker; public liste/detay ve başvuru ekranları eklendi. Başvuruda aktif privacy publication SHA, site/IP/sunucu zamanı bağlı3s–30dk challenge, ham IP'siz HMAC günlük hash,5/saat+100/gün DBkilitli kota, honeypot,180gün anonimleştirme. Admin/public depoları ayrıdır. Server HMAC anahtarı yapılandırılmadığında form kapalı; secret mevcut .env'ye yazılmadı. Proxy gerçek ziyaretçi IP'si aktarmadığı için kotanın paylaşılması ve işletmeye uygun metin/gerçek proxy kabulü T21.10 için açıktır.

**Son kanıt:26/26 seçili PG/storage integration,75/75 seçili unit; ayrıca tüm262/262 unit,157/157 yönetim,13/13 website.**0atlama/0hata. İkiFEtypecheck/productionbuild ve publicHTTPsmoke PASS. Gerçek React bileşenleriyle izole tarayıcı taslakoluşturma/düzenle/ESCodak/390px taşma0 PASS. Yeni migration20261018201251_PropertyListings ve snapshot/designer; ilk kurulum+yükseltme+replay+boş Down+dolu Down guard/audit/compositeFK ve event immutable testleri geçti. Migration yalnız fixture'da; gerçek API/Worker yeniden başlatılmadı. API kullanım için mevcut dev/migration prosedürüyle yeni koddan başlatılmalıdır.

[API/işletim](../api/listings.md), [kanıt](../TESTING.md). Canlı Keycloak/gerçek EİDS/hukuki UAT/gerçek cihaz/Lighthouse/tam backend suite yapılmadı. Test DB/rol/bucket ve13003UI sunucusu temizlendi. Önceki geniş AutoMigration ortam cleanup sorunu bu dar koşunun başarısı sayılmaz. Yeni paket/harici görsel/font/ikon yok; mevcut dirty çalışma korunur, .env/hesap/rol/parola/gerçek DB/servis değişmedi; commit/push yok. Skillphase/contract/security/verify/license/docs; tek agent.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalancontext: BİLGİ MEVCUT DEĞİL. Sonusage19.10.2026 Codex:300dk%41kullanılan/%59kalan,reset19.10.2026 14:06:03;10080dk%70/%30,reset25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir. Sonraki: EİDS sağlayıcı ve hukuki/işletim kabulü; sonraki faza otomatik geçilmez.

## WEBSITE FAZ 06 —18.10.2026 — DEVAM EDIYOR

Kullanıcı sonraki fazı onayladı. WEBSITE kayıt07/10, üç resmi madde; 0/3=%0,00. T21.6 ve bağlı T21.10 kapsamı: opsiyonel Unit ilanı, medya/özel malik belgesi, version/onay/yayın geçmişi, iletişim gizliliği ve KVKK başvuru. Önceki FAZ05 korunur. Gerçek EİDS sağlayıcısı yok: yayın fail-closed; yönetici beyanı doğrulama yerine geçmez. Yeni paket/hesap/gerçek veri değişikliği yok. Testler henüz çalıştırılmadı. Phase/contract/security/verify/license/docs; tek agent. Token giriş/çıkış/toplam/context BİLGİ MEVCUT DEĞİL. Usage300dk%65kullanılan/%35kalan,10080dk%58/%42; hesap kotası sohbet tüketimi değildir.

## WEBSITE FAZ 05 —18.10.2026 — TAMAMLANDI (teknik teslim)

**3/3 resmi madde=%100,00; WEBSITE6/10=%60,00; İŞ21 T21.0–T21.5 ve T21.11:7/13=%53,85.** Yönetim menüsü/alan seçimi; sayfa/blok CRUD/sıra/tema/ayarlar, yetkili içerik/görsel önizlemesi, yayın/geçmiş/restore/askı. Ortak shadcn/TanStack/ManagementPanel/bildirim/TR–EN kullanılır. Medya hak/kişi izni, JPEG/PNG temizliği, UUID/version, same-site FK, aktif yayın referansıyla public indirme ve SHA doğrulayan no-store website proxy eklendi. Migration20261018172114_WebsiteMedia yalnız fixture'da uygulandı; kullanım için API yeniden başlatılmalı.

Kanıt TESTING.md: **155/155 yönetim,10/10 website,253/253 unit,17/17 son seçili PG/storage; typecheck/build/HTTP smoke PASS.** İzole gerçek bileşen tarayıcı testi sayfa/blok/409/ESC/odak/rıza390/1440 PASS. Canlı Keycloak/gerçek cihaz kabulü yapılmadı. Önizleme içerik dökümüdür, birebir premium render değildir. Orphan nesne otomatik temizliği henüz yok. Geniş erken koşunun mevcut AutoMigration cleanup yetki sorunu kayıtlı; tüm suite geçti denmedi. Geçici DB/rol/UI sunucusu temizlendi. Gerçek .env/hesap/veri/çalışan API korunur. Yeni paket yok; phase/contract/security/verify/license/docs, tek agent. Sonraki: FAZ06 ilan yönetimi onayı; başlanmadı.

## WEBSITE FAZ 04 — 18.10.2026 — TAMAMLANDI

**3/3 resmi madde = %100,00; WEBSITE 5/10 = %50,00; İŞ21 T21.0–T21.4: 5/13 = %38,46.** website/ kökünde mockup temelli premium Altunbayrak demo; ortak Residence yayınlı tenantın public içeriklerini sunar. Hero/giriş bağlantısı/özellik şeridi/ilan örnekleri/duyuru/sosyal alan/galeri/iletişim, tenant bazlı yerel logo/görsel ve API tema tokenları. Demo noindex/bant, temsilî fiyat yerine yayın bekliyor; gerçek tenantta demo içerik/görsel yok. İki özgün image_gen görseli WebP; kaynak/prompt website/public/images/README.md. 10/10 test, typecheck/build ve izole HTTP smoke PASS;390/768/1440 taşma0/kırık görsel0, mobil Enter menü/kart PASS. Lighthouse/saha CWV/gerçek cihaz/Keycloak akışı ölçülmedi. Yeni paket/migration/gerçek veri yok. Skill phase/contract/verify/license/docs/imagegen; tek agent. Sonraki: FAZ 05 yönetim ekranları için kullanıcı onayı; uygulanmadı.

## WEBSITE FAZ 03 — 18.10.2026 — TAMAMLANDI

**İki resmi madde 2/2 = %100,00; WEBSITE planı 4/10 = %40,00; İŞ21 T21.0–T21.3: 4/13 = %30,77.** website/ ayrı Next.js uygulaması 3004; güvenli public DTO/siteKey, sunucuda no-store/timeout/redirect reddi, metadata, tema, 404/500 ve temel responsive erişilebilir iskelet. 6/6 test, typecheck/build ve izole HTTP smoke PASS (TESTING.md). Tarayıcı başlangıç ekranı görüldü; gerçek cihaz/klavye/Lighthouse kabulü yapılmadı. Yeni paket/migration/gerçek veri değişimi yok. Mevcut frontend kilitli node_modules setup bağlantısıyla kullanılır. FAZ 04 premium mockup henüz uygulanmadı; sonraki adım kullanıcı FAZ 04 onayı. Skill: phase/contract/verify/license/docs; tek agent. Token/context BİLGİ MEVCUT DEĞİL.

# BuildingFlow güncel çalışma durumu

## PERSON FAZ11 —18.10.2026 — Son teknik teslim / yayın kapıları açık

FAZ10 kullanıcı kabulüyle #15 Done; #16 In review. İki yeni kiracı+sakin/uzaktan malik-çoklu blok HTTP regresyonu ve üç web dosyasında tekrar dene/yenile düğme standardı düzeltmesi tamamlandı. **366/366 seçili izole backend integration,149/149 web,126/126 mobil; web typecheck/izole production build ve mobil üç platform export başarılı.** 30 migration idempotent SQL iki kez geçti; sahte veride yedek/restore8kontrol geçti, fixture kaynakları temizlendi ve canlı servisler korunuyor. Privateconfig3fixture ve releasepreflight29unsafe reddi başarılı. Unit/telemetri/audit/lisans FAZ10 sonuçları yeniden çalıştırılmış sayılmaz.

[Son rapor ve rollout/rollback/UAT listesi](../refactor/person/phase-11-report.md). Teknik6/6=%100; kullanıcı kabulü10/11=%90,91. Mobil29audit kaydı(18high/11moderate), kontrollü anahtar yenileme/geçmiş incelemesi, eksik tam güvenlik/native/lisans kabulü açık: **yayın engelli**. Fixture PostgreSQL17.6 restore başarısı ortak18.6 üretim DR kabulü değildir. Gerçek .env/DB/hesap/rol/servis, bağımlılık ve migration değiştirilmedi; WEBSITE çalışması korunur. Sonraki faz yok; final kullanıcı kabulü beklenir.


## PERSON FAZ10 —18.10.2026 — Doğrulama teslimi / güvenlik kapısı GEÇMEDİ

FAZ09 kullanıcı kabulüyle #14 Done; mevcut #15 In review. **Mobil npm audit29kayıt(18high/11moderate),4kökadvisory ile başarısız.** Web audit0; NuGet7projede bildirilmiş0açık. Mevcut lisans metadata329npm/NuGet ve871mobil lock kaydında geçti; tam binary/container dağıtım/NOTICE kabulü değildir. Fortify/Semgrep/Gitleaks/TruffleHog/OSV/Trivy/Syft erişimi yok; tam SAST/history/native/container taraması DOĞRULANAMADI.

Gerçek runtime özel konfigürasyonu ignore olmasına rağmen HEAD/indexte takipliydi. Disk dosyası ve .env byte'ları korunarak yalnız Git indeksinden çıkarıldı (bilinçli staged deletion). Eski erişim bilgilerini yenileme, geçmiş/klon incelemesi ve düzeltmenin commit'e alınması açık; commit/push/geçmiş değişimi/rotasyon yapılmadı. Dört private runtime yolunun ignored+untracked gate'i ve3regresyon fixture CI'ye eklendi. Mobil high audit gate'i de CI'ye eklendi; bugün exit1 ile kapatır, bulgu bastırılmaz.

**364/364 seçili izole PG/storage integration,244/244unit,126/126mobil,6/6telemetri,23/23seçiliweb;0fail/skip.** Typecheck/lint/mobil export, .NET0hata0uyarı, yeni3fixture, mevcut6CIgate/29unsafe release reddi, shell/diff PASS. Testlerin geçmesi güvenli yayın sonucu değildir. Native MASVS, biyometrinin gerçek anahtara kriptografik bağı, OS/picker/modal/background/screenshot ve artifact lisans doğrulaması açık. Bu faz uygulama/migration/paket sürümü değiştirmedi; shared checkout'taki eşzamanlı WEBSITE kod/model değişimleri korunur. Teknik6/6=%100 çalışma teslimi; kabul edilmiş önceki9/11=%81,82. FAZ11 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-10-report.md), [redacted kanıt](../refactor/person/phase-10-evidence.json).

## PERSON FAZ09 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ08 kullanıcı kabulüyle #13 Done; mevcut #14 In review. Canonical contextKind/contextId ile talep/arıza/fotoğraf CRUD ve ayrı hesap profili mevcut SQLCipher schema8/outbox'a bağlandı. Sunucu güncel kişi/görev/ilişki kapsamını push/replay/pull/photo sırasında ve parent kilitli transaction içinde tekrar doğrular. Legacy kuyruğu/fingerprint uyumu korundu; canonical revoke pending taslağı şifreli/blocked tutar, başka alana göndermez. Merkezi kişi kartı readonly; ödeme/kesin tahakkuk/rol değişimi offline kuyruğunda yoktur. Fotoğraf seçici dönüşünde tekrar doğrulama ve modal gizlilik örtüsü uygulandı.

**363/363 seçili izole PG/storage integration,187/187 unit,126/126 mobil; typecheck/lint, açık+kapalı bayrakla üç platform export, .NET0hata0uyarı, compose config ve329 lisans metadata PASS.** Yeni9 HTTP+7 mobil senaryosu bu toplamlardadır; dar50/50 ayrıca geçti.70 mevcut manifest/proje/migration hash'i aynı; yeni paket/migration yok. Gerçek native SQLCipher/SecureStore/picker/biyometri/background/UI ve Keycloak/production kabulü DOĞRULANAMADI; scanner çalıştırılmadı. Yeni sync bayrakları defaultfalse, gerçek .env/DB/tenant/Keycloak/rol/servis değiştirilmedi. Teknik6/6=%100,00; kabul edilmiş önceki8/11=%72,73. FAZ10 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-09-report.md), [API](../api/mobile-canonical-sync.md).

## PERSON FAZ08 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ07 kullanıcı kabulüyle #12 Done. FAZ08 mevcut #13 In review: minimum canonical kendi kişi/context/capability kopyası, SQLCipher schema8 payload, SecureStore biyometrik cihaz bağı, yönetim1h/sakin24h ve ilişki bitişiyle kısalan süre, saat geri alma/arka plan/çıkış koruması uygulandı. Yeni mod yalnız readonly ekran açar; eski cache/CRUD/sync yolları kapalıdır, mod geçişinde eski taslak/kuyruk korunur. Bayraklar defaultfalse; canlı DB/.env/Keycloak/servis değiştirilmedi. Yeni paket/migration yok.

**354/354 seçili izole PG/storage integration,187/187 unit,119/119 mobil; typecheck/lint, kapalı+açık bayrakla Expo iOS/Android/web export, .NET0hata0uyarı, compose config ve329 lisans metadata başarılı.** Gerçek native SQLCipher/SecureStore/biyometri/background kabulü DOĞRULANAMADI; FaceID yapılandırması yeni binary ister. Offline iptal anında öğrenilemez; readonly lease sonuna kadar bilgi erişimi riski açıkça raporlandı. SAST/CVE/scanner/production kabulü yapılmadı. Teknik6/6=%100,00; kabul edilmiş önceki7/11=%63,64. FAZ09 başlamadı; diğer İŞ/UI/MOBİL sayaçları değişmedi. [Rapor](../refactor/person/phase-08-report.md), [API](../api/mobile-person-authorization.md).

## İŞ21 WEBSITE FAZ 02 — 18.10.2026 — Backend CMS temel API tamamlandı, FAZ 03 onayı bekliyor

FAZ 01 kullanıcı tarafından kabul edildi. Dört yeni tablo (`website_sites`, `website_pages`, `website_sections`, `website_publications`) tek migration `20261018063745_WebsiteCms` ile eklendi; mevcut tablolara dokunulmadı, Down dolu veride reddeder. Yönetim API'si, değişmez yayın snapshot'ı ve anonim salt okunur public API: [docs/api/website.md](../api/website.md). **.NET 0 hata 0 uyarı; 244/244 unit; ayrı test DB'de 587 integration geçti, 0 hata, 37 atlandı (RabbitMQ/S3/SMTP gerektiren mevcut testler); web 148/149 (bilinen `buttons.test.mjs`).** Testler yerel PostgreSQL 16 ile koştu (CI 18.6). Canlı DB/.env/Keycloak/servis değişmedi; yeni paket yok. Tasarımdan sapma: tema ayrı tablo yerine site satırında; medya, alan adı ve public duyuru tabloları ilgili fazlara kaldı; `/api` no-store korunuyor (önbellek kararı FAZ 07). İŞ21 3/13 = %23.08; iş yol haritası 134/151 = %88.74. GitHub #21 Done, #22 In review.

## İŞ21 WEBSITE FAZ 01 — 18.10.2026 — Tasarım tamamlandı (kabul edildi)

FAZ 00 kullanıcı tarafından kabul edildi (GitHub #20 kapatıldı). Domain/DB tasarımı, API kontratı, yetki matrisi, güvenlik/KVKK/tenant kuralları, 11 ADR ve test senaryoları: [design](../website-management/design.md), [api](../website-management/api.md), [security](../website-management/security.md), [adr](../website-management/adr.md), [test-plan](../website-management/test-plan.md). İŞ21 2/13 = %15.38; iş yol haritası 133/151 = %88.08. Kod, DB, servis değişmedi; yeni paket yok. GitHub #21 In review.

## İŞ21 WEBSITE FAZ 00 — 18.10.2026 — Analiz tamamlandı, WEBSITE FAZ 01 onayı bekliyor

WebsiteManagement prompt paketi (`BuildingFlow-WebsiteManagement-Prompt-Paketi/`) ve mockup incelendi; yalnız analiz yapıldı. Rapor: [analysis.md](../website-management/analysis.md). Yol haritasına İŞ21 (T21.0–T21.12; WEBSITE FAZ 00–09 + 3 ek görev) eklendi; T21.0 tamamlandı. İŞ21 1/13 = %7.69; iş yol haritası 132/151 = %87.42 (13 yeni görev nedeniyle düştü). Backend, veritabanı, Keycloak, .env ve servisler değişmedi; yeni paket yok. Web testi 148/149: tek hata `buttons.test.mjs` (PERSON FAZ07 dosyalarında `tryAgain` düğmeleri), bu turdan önce de vardı. Karar bekleyen 5 soru raporun 5. bölümünde.

## PERSON FAZ07 —18.10.2026 — Teknik teslim/kabul bekliyor

FAZ06 kullanıcı onayıyla #11 Done. FAZ07 #12 In review: sunucu kaynaklı görev/daire context'leri, tek alan otomatik açılış/çoklu seçim, salt okunur self, güncel merkez Property/tenant kapısı ve finans içindeki kişi görünürlüğünün göreve daralması tamamlandı. **348/348 seçili izole PG/storage integration,187/187 unit,143/143 frontend; typecheck, açık/kapalı bayrakla production build, .NET0hata0uyarı, compose config ve329 lisans metadata başarılı.** Model/migration/manifest/lock korundu; canlı DB/.env/rol/servis değişmedi. Yeni bayraklar defaultfalse; global merkez ENFORCED kapısını açmak önceden hazırlanmış hesap/görev/scope ister. Eski mobil ve finans projeksiyon geçişi sonraki fazlarda; global users/consents tenant yöneticilerine yeni akışta kapalıdır. Gerçek UI/Keycloak/cihaz ve SAST/CVE/scanner kabulü yapılmadı. Teknik6/6=%100; kabul edilmiş6/11=%54,55. FAZ08 başlamadı; diğer İŞ/UI/mobil sayaçlar değişmedi. [Rapor](../refactor/person/phase-07-report.md), [API](../api/session-workspaces.md).

## PERSON FAZ06 — 18.10.2026

Merkezi kişi listesi/dört detay sekmesi, izinli native ilişki ve görev akışları, minimum hesap DTO, TR/EN ve role/scope kontrolleri tamamlandı. **329/329 seçili izole PG/storage integration,187/187 unit,135/135 frontend; typecheck/production build, .NET0hata0uyarı ve329 lisans metadata geçti.** İki yazıcı koruması ve immutable receipt migration testli. Canlı DB/.env/rol/servis değiştirilmedi; bayraklar defaultfalse. Native ilişkiler eski finans/mobile projekte edilmez; eski kayıtlar kaynak ekranda yönetilir. Gerçek UI/klavye/telefon, SAST/CVE/scanner kabulü yapılmadı. Teknik6/6=%100; mevcut #11 In review/kullanıcı kabulü bekliyor; kabul edilmiş5/11=%45,45. FAZ07 başlamadı; diğer İŞ/UI/mobil sayaçlar değişmedi. [Rapor](../refactor/person/phase-06-report.md), [API](../api/person-center.md).

Güncelleme: 2026-10-18 (Europe/Istanbul). Bu dosya mevcut takip kaynaklarının tarihli özetidir; görevlerin ve test kanıtlarının ikinci kaynağı değildir. Çalışma kuralı: [AGENTS.md](../../AGENTS.md).

## Aktif tur

**İŞ20 Dashboard ve menü düzeni — T20.1–T20.5 TAMAMLANDI, T20.6 Planlı bakım modülü ve T20.7 kabul açık (18.10.2026).** İŞ20 5/7 = %71.43; iş yol haritası 131/138 = %94.93. Sol menü Dashboard ile başlar, Genel bakış en altta; üst çubukta dönen slogan; Dashboard özet API'si, kutucuklar, trend, dağılım ve yaklaşan işlemler. Backend 184/184 birim ve 526/526 entegrasyon, web 132/132, typecheck/build; yeni paket yok. Kanıt: [TESTING.md](../TESTING.md), [PHASE_20_DASHBOARD.md](../analysis/PHASE_20_DASHBOARD.md).

**İŞ19 Hesap bilgilerim ve kullanıcı fotoğrafı — T19.1–T19.5 TAMAMLANDI, T19.6 gerçek giriş kabulü açık (17.10.2026).** İŞ19 5/6 = %83.33; iş yol haritası 126/131 = %96.18. Merkezde Hesap bilgilerim sayfası (fotoğraf, ad/telefon, hesap, alanlar, parola, Rızalarım), üst menüde fotoğraf ve "Ad SOYAD", Kullanıcılar ve kişi ekranında fotoğraf; fotoğraf mobil ile ortak, rıza WEB kanalıyla. Backend 184/184 birim ve 520/520 entegrasyon, web 123/123, typecheck/build; yeni paket yok. Ek 3: yumuşak renkli düğme standardı (A) bütün ekranlarda; web 129/129. Kanıt: [TESTING.md](../TESTING.md), [PHASE_19_ACCOUNT.md](../analysis/PHASE_19_ACCOUNT.md).

**BAKIM ad/soyad kuralı ve bildirim testi — TAMAMLANDI (17.10.2026).** Soyad büyük, ad ilk harfi büyük kuralı API, merkez ve mobilde; mobil ana sayfada "Ad SOYAD"; MobileNoticeTests düzeltildi, tüm backend entegrasyon 504/504. Yol haritası sayacı değişmedi (faz dışı bakım). Kanıt: TESTING.md BAKIM 17.10.2026.

**İŞ18 mobil rıza ekranları — T18.1–T18.5 TAMAMLANDI, T18.6 cihaz kabulü açık (16.10.2026).** İŞ18 5/6 = %83.33; iş yol haritası 121/125 = %96.80. Mobilde rıza ekranı (WebView yok, projeye ait çizici), fotoğraf eklemeden önce rıza, şifreli çevrimdışı rıza kuyruğu (fotoğraftan önce gider), 403'te fotoğraflı işin bekletilmesi, açılış kapısı ve Profil → Rızalarım. Yeni paket yok. İŞ17 ile İŞ18 birlikte yayınlanabilir. Kanıt: TESTING.md İŞ18.

**İŞ17 fotoğrafta rıza zorunluluğu ve kullanıcı rıza API'si — T17.1–T17.5 TAMAMLANDI, T17.6 açık (16.10.2026).** İŞ17 5/6 = %83.33; iş yol haritası 116/125 = %92.80. Mobil rıza uçları, profil/talep fotoğrafında ACTIVE rıza zorunluluğu (403 `BF_CONSENT_REQUIRED`), geri çekmede profil fotoğrafının silinmesi ve talep fotoğrafının yalnız sahibinden gizlenmesi, kişi ve talep ekranında rıza bilgisi. Üç sistem türü özgün taslak v1 metinlerle gelir (`DefaultConsentTexts` migration'ı). Mobil uygulama İŞ18'e kadar değişmedi: o zamana kadar mobilden fotoğraf yüklemesi 403 alır. İŞ18 kullanıcı onayı bekliyor. Kanıt: TESTING.md İŞ17.

**İŞ16 rıza altyapısı ve merkez Açık Rıza menüsü — T16.1–T16.5 TAMAMLANDI, T16.6 açık (16.10.2026).** İŞ16 5/6 = %83.33; iş yol haritası 111/125 = %88.80 (İŞ16–18 ile 18 yeni görev eklendi, yüzde kapsam nedeniyle düştü). İŞ17 ve İŞ18 kullanıcı onayı bekliyor. Kanıt: TESTING.md İŞ16.

**İŞ15 mobil kayıt ve kodla eşleşme — T15.1–T15.5 TAMAMLANDI, T15.6 cihaz kabulü açık (15.10.2026).** İŞ12–14 tamamlandı (kullanıcı onayı). İş yol haritası 105/106 = %99.06. Kanıt: TESTING.md İŞ12–15.

**PROTOKOL: AkaDev AI FAZ 03 — TAMAMLANDI.** 4/4=%100.00; 7 ortak Skill, Codex keşif bağlantıları ve Claude girişleri. Yalnız statik kabul; uygulama ve runtime testleri çalıştırılmadı. Ürün/UI roadmap değişmedi. Kanıt TESTING.md AI FAZ03 kaydı; sonraki faz başlatılmadı.

## Önceki uygulama teslimi

**BAKIM: kişi profili, hesap eşleştirmesi ve iletişim — TAMAMLANDI (12.10.2026).** İlişkili mevcut İŞ04 (kayıt05/14), İŞ06.1 (kayıt08/14), İŞ09 (kayıt12/14); yeni bağımsız ürün fazı yok. Bu tur5/5=%100.00. Gerçek/tüzel, yerli/yabancı profil, kimlik/ülke, doğum/cinsiyet/adres ve zorunlu telefon/email; liste kolonları ve çoklu ilişkiler. Aktif üyelik hesabı seçimi, kalıcı uygulama bildirim kutusu ve mesaj geçmişi; cihaz tel bağlantısı ve snapshot aidat geçmişi. SMS sağlayıcı bağlanana kadar kapalı.

**Son doğrulama:481/481 .NET (109 unit+372 gerçek integration),104/104 FE, typecheck/build, model/migration eşliği,331 lisans.** Yerel migration20261011223446 uygulandı;22 eski tablo veri/audit hashleri eşit. API5005 live200, FE3003. TR/EN, dört tür alanı/required/odak;390px panel ve820px kartCSS doğrulandı. Kullanıcı iş kayıtlarında form save/mesaj gönderimi yapılmadı; yazım ve mesaj teslimleri ayrı fixture'larda doğrulandı. Gerçek SMS/OS push/telefon görüşmesi veya resmî kimlik doğrulama servisi yoktur. Sonraki işlem: yeni kullanıcı kapsamı. Ayrıntı TESTING.md son bölüm ve SESSION.md.

## İş yol haritası özeti

Son okuma 11.10.2026 23:22:59 Europe/Istanbul, kaynak Codex usage / rateLimitsByLimitId.codex: 300 dk kullanılan%29/kalan%71, reset12.10.2026 04:04:09; 10080 dk kullanılan%36/kalan%64, reset18.10.2026 09:28:33. Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Oturum tüketimi: BİLGİ MEVCUT DEĞİL. Hesap kotası sohbet tüketimi değildir.

Kaynak: [milestones.ts](../../frontend/buildingflow-web/src/features/roadmap/milestones.ts); kabul kanıtı [TESTING.md](../TESTING.md). **14 faz kaydı:** 00–11 ve onaylı ek 06.1 / 07.1. Kimlik 06.1 listedeki 8. kayıttır; faz kimliği sıra sayısı değildir. Son tamamlanan **İŞ11: 4/4 = %100.00**; önceki teslimler korunur. İŞ10 ve İŞ11 tamamlandı; yeni faz için kullanıcı onayı beklenir.

| Faz | Başlık | Adımlar | Durum | İlerleme |
| --- | --- | --- | --- | --- |
| 00 | Proje temeli ve yerel çalışma ortamı | 16/16 | TAMAMLANDI | %100 |
| 01 | Giriş, çıkış ve şifre yenileme | 9/9 | TAMAMLANDI | %100 |
| 02 | İlk siteyi oluşturmak ve listelemek | 10/10 | TAMAMLANDI | %100 |
| 03 | Yönetim alanları, binalar ve bağımsız bölümler | 6/6 | TAMAMLANDI | %100 |
| 04 | Kişiler ve daire ilişkileri | 4/4 | TAMAMLANDI | %100 |
| 05 | Aidat ve borçlandırma | 5/5 | TAMAMLANDI | %100 |
| 06 | Ödeme ve borç kapatma | 4/4 | TAMAMLANDI | %100 |
| 06.1 | Tahakkuk kişi geçmişi ve taşınma onayı | 4/4 | TAMAMLANDI | %100 |
| 07 | Gider ve bütçe takibi | 4/4 | TAMAMLANDI | %100.00 |
| 07.1 | Merkezi API hata/uyarı ve TR/EN bildirim | 5/5 | TAMAMLANDI | %100 |
| 08 | Duyurular ve dosya yönetimi | 4/4 | TAMAMLANDI | %100.00 |
| 09 | Asenkron mail, SMS ve bildirim | 4/4 | TAMAMLANDI | %100.00 |
| 10 | Raporlar, sayaçlar ve gözlemlenebilirlik | 4/4 | TAMAMLANDI | %100.00 |
| 11 | Yayın hazırlığı ve kalite | 4/4 | TAMAMLANDI | %100.00 |

**Adım bazlı genel iş ilerlemesi: 83 / 83 = %100.00.** Bu oran mevcut yol haritasının tamamlanan adımlarını sayar; fazların ortalaması veya üretime hazırlık ölçüsü değildir. UI ve protokol adımları bu paydaya eklenmedi. Kaynakta görev sayısı değişince özet ve oran aynı turda yeniden hesaplanır; geçmiş teslim kanıtları korunur.

## Ayrı planlar ve belge konumları

- UI planı: [MASTER_PROMPT.md](../../frontend/buildingflow-ui-plan/MASTER_PROMPT.md), [PHASES.md](../../frontend/buildingflow-ui-plan/PHASES.md); **12 aşama (01–12)**. Kök dizinde bu iki dosya yoktur. UI 01 analiz ve UI 06 uygulama kayıtları [UI_ANALYSIS_REPORT.md](../analysis/UI_ANALYSIS_REPORT.md) ile [UI_STAGE06_REPORT.md](../analysis/UI_STAGE06_REPORT.md) içindedir. UI 06 ayrı kartta **4/4** kabul görevi gösterir; önceki UI 02–05'in tüm kapsamları teslim edilmiş sayılmaz. Bütün UI planının adım bazlı yüzdesi: **BİLGİ MEVCUT DEĞİL** (tüm aşamalar için resmi adım paydası yok).
- İlk teknik plan: [mimari kaynak](../BUILDINGFLOW_PROJECT_CORE.md), bölüm 20, **8 faz**. Tarihsel Messaging/Storage/Observability adları güncel iş 05/06/07 ile eş tutulmaz; iş yol haritasındaki altyapı temeli tüm sağlayıcı entegrasyonlarının bittiği anlamına gelmez.
- Test kanıtları [TESTING.md](../TESTING.md), bu turun değişiklikleri [CHANGELOG.md](CHANGELOG.md), devam bağlamı ve kullanım okuması [SESSION.md](SESSION.md) içinde tutulur.

## Kalan gerçek sınırlar

- Faz 06.1 eski borçlara bilinmeyen kişi geçmişi üretmez. Katalog CRUD, 500'ün üzerindeki toplu onay ve canlı kullanıcı verisinde başarılı finans mutation'ı teslimin kabul kapsamı değildir; ayrı PostgreSQL testleri ile salt-okunur tarayıcı kabulü ayrıdır.
- FE sekme tekrar kullanımı Chrome/Safari native adapter testleriyle doğrulandı; canlı macOS Automation izni/GUI kabulü yapılmış sayılmaz.
- Online ödeme/gerçek banka transferi, Tempo/tam broker trace propagation ve gelecekteki iş fazları planlıdır. Üretim SMTP/HTTPS, CI ve yedekleme için araçlar ve rehber hazır; hedef ortamda go-live yapılmadı. Ayrıntılı kapsam/sınırlar TESTING ve API belgelerinden okunur.
- 07.1 yalnız mesaj sözleşmesi ve sunum altyapısıdır; domain/DB/migration, kimlik politikası ve finans hesaplama kuralları değişmez.

## İŞ 07 teslim kanıtı

**292 .NET (59 unit + 233 integration), 67 FE; hata/atlama0.** Typecheck/build, EF model eşliği/idempotent SQL, 312 lisans, 18 korunan dosya ve 12 eski DB tablosu hash eşliği. Yerel migration uygulandı, BE5005 Healthy, FE3003 dev süreci korunur. TR/EN/1280×900 sağ512px, 390×844 mobil ve boş/sıfır plan kabulü kaydetmeden tamamlandı. Detay [TESTING.md](../TESTING.md), [API](../api/expenses.md). Kategori CRUD/dosya eki/banka mutabakatı/ödeme emri yok; kullanıcı DB'sinde başarılı finans mutation yapılmadı.

## İŞ 08 teslim kanıtı

**319 .NET (68 unit +251 integration), 76 FE; hata/atlama0.** Gerçek ayrı PostgreSQL/özel geçici MinIO, migration/model/Down/tekrar/idempotent SQL; TR/EN sağ512px ve390px mobil panel kabulü, son typecheck/build,316 lisans ve19 korunan dosya kontrolü geçti. İki yeni BaseEntity tablosu ve Minio7.0.0 SDK; Duyurular/Belgeler menüleri yerel FE3003'te, API5005 Healthy. Yerel tekrar migration öncesi/sonrası15 eski tablo hash'i aynı; ilk canlı upgrade öncesi hash kanıtı alınamadı, ayrı PG yükseltme testi vardır. Kullanıcı DB'sinde başarılı duyuru/dosya mutation yapılmadı. Mail/SMS İŞ09, dosya taraması/retention ve finans eki gelecekteki kapsam; Docker imajı bu tur derlenmedi. Detay [TESTING.md](../TESTING.md), [API](../api/communication.md).

Son kullanım okuması: Codex usage, 11.10.2026 15:00:47 Europe/Istanbul; 300 dk kullanılan %91 / kalan %9, reset 11.10.2026 19:29:12; 10080 dk kullanılan %30 / kalan %70, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**. Çıkış token: **BİLGİ MEVCUT DEĞİL**. Toplam token: **BİLGİ MEVCUT DEĞİL**. Kalan context: **BİLGİ MEVCUT DEĞİL**. Oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

## İŞ 09 adım01 kanıtı — 11.10.2026

**345 .NET (88 unit + 257 integration), 76 FE; hata/atlama 0.** 20 sözleşme unit + 6 gerçek RabbitMQ testi yalnız `buildingflow-test-*` namespace'inde; confirm/mandatory/persistent/eşzamanlılık doğrulandı, test kuyruğu kalmadı. Typecheck, 318 lisans ve 21 korunan dosya kontrolü geçti. Çalışma `faz-09-messaging` dalında; main'e merge/push yok. Detay [TESTING.md](../TESTING.md).

## İŞ 09 adım02 kanıtı — 11.10.2026

**355 .NET (88 unit + 267 integration); hata/atlama 0.** Yeni BaseEntity `outbox_messages`/`processed_messages` ve migration; atomik enqueue, SKIP LOCKED relay, tekilleştiren tüketici ve worker host'ları gerçek ayrı PG + `buildingflow-test-*` RabbitMQ'da doğrulandı. Migration yerel ana DB'ye uygulanmadı (kullanıcı onayı bekler). Detay [TESTING.md](../TESTING.md).

## İŞ 09 adım03 kanıtı — 11.10.2026

**369 .NET (88 unit + 281 integration); hata/atlama 0.** TTL retry kuyrukları, deneme limiti, nedenli confirm'li DLQ ve inbox tekrar teslim koruması gerçek `buildingflow-test-*` RabbitMQ + ayrı PG'de doğrulandı. Detay [TESTING.md](../TESTING.md).

## İŞ 09 teslim kanıtı — 11.10.2026

**374 .NET (88 unit + 286 integration), 76 FE; hata/atlama 0.** Sürümlü sözleşmeler, confirm/mandatory yayıncı, BaseEntity outbox/inbox + migration, SKIP LOCKED relay, retry/DLQ ve SMTP e-posta; gerçek ayrı PG, `buildingflow-test-*` RabbitMQ ve geçici Mailpit ile doğrulandı. SMS kapalı, uygulama içi bildirim kanalı yok, iş akışı tetikleyicisi yok; migration yerel ana DB'ye uygulanmadı, Docker imajı derlenmedi. Dal `faz-09-messaging` (5 commit), main'e merge/push yok. Detay [TESTING.md](../TESTING.md), [analiz](../analysis/PHASE_09_MESSAGING.md).

## İŞ10 teslim kanıtı — 11.10.2026

**4/4 resmi görev = %100.00; genel iş 79/83 = %95.18.** Faz09 hızlı tekrar **41/41**; bu tur son regresyon **397 .NET (88 unit +309 integration), 82 FE, hata/atlama 0**, typecheck/build ve **11 canlı gözlemlenebilirlik kontrolü** geçti. Sayaç/okuma/gerekçeli revizyon ve alan/dönem raporları, TR/EN ortak sağ panel ve 390 px mobil kabulü; üç yeni BaseEntity tablo, gerçek ayrı PostgreSQL kurulum/yükseltme/tekrar/Down ve idempotent SQL doğrulaması. **331 lisans**, **23 korunan dosya**, ana DB yükseltmesinde **17 eski tablonun satır hash eşliği**. Yerel API5005/FE3003; SMTP kapalı test workerı ve OTLP14318/Prometheus19090/Loki13100/Grafana3002 çalışıyor. Kullanıcı iş verisinde mutation yok; Docker uygulama imajları derlenmedi. Ayrıntı [TESTING.md](../TESTING.md), [API](../api/meters-reports.md), [gözlemlenebilirlik](../OBSERVABILITY.md). Sayaçtan otomatik borç, endeks sıfırlama/tarife, geçmiş bakiye snapshot ve Tempo deposu yok. İŞ11 başlatılmadı.

## İŞ11 adım01 kanıtı — 11.10.2026

**425 .NET (88 unit + 337 integration), 85 FE; hata/atlama 0.** Production yapılandırma reddi, yalnız tanımlı proxy, sahte forwarded header, HTTP/HTTPS, kimlikli/anonim kodlu 429, imzasız/yabancı JWT; FE nonce CSP production build smoke'u (24/24 nonce'lu script, CSP ihlali yok) ve güvenli locale cookie. Dal `faz-10-11-release`; Faz10 ayrı commit 8ca000d. Push/merge yok. Detay [TESTING.md](../TESTING.md).

## İŞ11 adım02 kanıtı — 11.10.2026

`bash scripts/ci.sh` çıkış 0: **426 .NET (88 unit + 338 integration), 85 FE**, typecheck/build, npm audit 0, NuGet audit temiz, 331 lisans, CI kapı negatifleri 6/6, EF model eşliği ve idempotent SQL'in geçici DB'ye iki kez uygulanması. Bozuk idempotent betik bulgusu migration dosyası değiştirilmeden düzeltildi. Workflow GitHub'da çalıştırılmadı. Detay [TESTING.md](../TESTING.md).

## İŞ11 adım03 kanıtı — 11.10.2026

`python3 scripts/test-backup-restore.py` **8/8, iki çalıştırma**: kendi fixture'ının (11 migration + satırlar, gerçek Keycloak realm/kullanıcı, 3 MinIO nesnesi) quiesced yedeği, bozuk/eksik/INCOMPLETE/desteklenmeyen yedek ve path-traversal reddi, yeni internal hedefe restore ile DB parmak izi, Keycloak girişi ve nesne SHA256 eşliği; kalıntı yok, BuildingFlow compose durumu değişmedi. Canlı veri yedeklenmedi/restore edilmedi. Detay [TESTING.md](../TESTING.md), [rehber](../BACKUP_RESTORE.md).

## İŞ11 teslim kanıtı — 11.10.2026

**4/4 resmi görev = %100.00; genel iş 83/83 = %100.00.** Son `bash scripts/ci.sh` çıkış 0: **426 .NET (88 unit + 338 integration), 85 FE, hata/atlama 0**, typecheck/build, npm audit 0, NuGet audit temiz, 331 lisans, idempotent SQL provası, preflight testi (21 güvensiz varyant reddi). Yedek provası 8/8; smoke 15/15; gözlemlenebilirlik 11/11; anonim SSR 30 rota sızıntı 0; TR/EN ve 375 px mobil. Bulunan ve düzeltilen sorunlar: bozuk idempotent betik, PostgreSQL readiness yarışı, Docker imajında açılmayan HSTS, kırılgan worker log penceresi. Dal `faz-10-11-release` (Faz10 + 4 adım commit'i); main'e merge/push yok. Dış TLS/SMTP/CI ile go-live yapılmadı. Ayrıntı [TESTING.md](../TESTING.md), [RELEASE.md](../RELEASE.md).

## main birleştirme — 11.10.2026

`faz-10-11-release` main'e birleştirildi; main'de ci.sh **426 .NET / 85 FE** geçti. Ana DB'de 11 migration'ın tamamı zaten uygulanmıştı; DB'ye yazılmadı. API ve worker yeni kodla çalışıyor; smoke 15/15, gözlemlenebilirlik 11/11. Push yapılmadı. Detay [TESTING.md](../TESTING.md).

## BAKIM — otomatik migration — 11.10.2026

Dal `feature/apartment-unit-details`: API açılışta bekleyen migration'ları advisory lock ile uygular (Development varsayılan açık); `db.sh sync` entity değişikliğinden eklemeli migration üretir. `ApartmentAndUnitDetails` ana DB'ye açılışta uygulandı (önce yedek alındı); elle eklenmiş iki kolon hizalandı. **451 .NET** geçti. Girişli ekran kabulü yapılmadı. Ayrıntı [TESTING.md](../TESTING.md), [MIGRATIONS.md](../MIGRATIONS.md).

## BAKIM teslim — frontend detay kolonları — 11.10.2026

**3/3 = %100.00.** Genel Bilgiler ve bina özellikleri, Daireler net alan/oda/kat konumu/ara kat/banyo/mutfak/balkon/ısıtma: form, detay, tablo ve mobil kartlarda; TR/EN ve nullable değer koruması. **91/91 FE**, typecheck/build, **331** lisans; girişli Chrome TR/EN, Sütunlar ve390×844 kabulü, **149/149** korunan dosya hash eşliği. Önceki451.NET bu tur tekrar çalıştırılmadı. Canlı kayıt mutation/commit/merge/push yok. İŞ03 mevcut doğrulama kaydı güncellendi, resmi83/83 değişmedi. Ayrıntı ve ekran kanıtı [TESTING.md](../TESTING.md).

## BAKIM teslim — Daire/kişi bağlantıları ve tablet — 11.10.2026

**3/3=%100.00; genel83/83 korunur.** Güncel Kiracı/Ev sahibi kolonları ve kişi bilgileri+ilişkiler; iki yönlü tam UUID filtreli gezinme. **464.NET (101unit+363integration),99FE, hata/atlama0**, typecheck/build,331 lisans,28/28 korunan dosya. Girişli TR/EN, tablet820×1180/1180×820,390px ve masaüstü1440; taşma0. Chrome iPad önizleme açık; gerçek Safari/iPadOS testi yok. API5005 Healthy/FE3003 çalışıyor, şema veya canlı iş kaydı mutation yok. Commit/merge/push yok. Ayrıntı [TESTING.md](../TESTING.md).

## UI03 referans menü turu — 11.10.2026

Kapsam yalnız sol/üst menü ve profil içinde çıkış. 01/03 inceleme tamamlandı; 02/03 uygulama devam ediyor. Mevcut çalışma ağacı değişiklikleri korunur; yeni paket/API/DB değişikliği yok. Kabul: rol/kapsam, TR/EN, profil çıkışı, menü araması, responsive ve sabit çerçeve.

## UI03 sol/üst menü teslimi — 12.10.2026

**Onaylanan dar kapsam 3/3 adım, %100.00 tamamlandı.** Koyu lacivert248px sol menü, mavi aktif satır,58px üst bar, menü arama dialogu, gerçek oturum adı/rolü ve profil içinde Keycloak çıkışı. Masaüstü daraltma; mobil mevcut sol Dialog ve sabit çerçeve korunur. Yeni paket/DB/API değişikliği yok; tek agent, ek skill yok.

**Bu tur101/101 FE, typecheck/build,331 lisans kontrolü geçti.** Tarayıcı profil menüsünde Çıkış yap görünür; aramada Rapor yalnız Raporlar sonucunu verdi;390×844 mobil panel tüm menüleri gösterdi, yatay taşma0 ve son header58px doğrulandı. Kullanıcının oturumu kapatılmadı; gerçek logout redirect bu tur yeniden çalıştırılmadı. Rol koşulları kod incelemesiyle korundu; ayrı rollerle tarayıcı kabulü yapılmadı. Backend testleri çalıştırılmadı. İlk son kontrol komutu yanlışlıkla kökten çalıştırıldığı için package.json bulunamadı; doğru frontend klasöründen tekrar başarılı.

Görsel birebir eşlik sınırlamaları: referans fotoğraf/logosu yerine Lucide bina işareti ve çizim; avatar gerçek oturum baş harfleri. Bildirim sayısı uydurulmaz; merkez henüz mevcut değildir. API'si bulunmayan menüler Planlandı açıklamalı disabled. Üst arama mevcut modülleri bulur, kişi/daire verilerinde global API araması değildir. UI03 geniş planın breadcrumb/global veri araması tamamlandı sayılmaz; yalnız kullanıcının bu tur istediği menü kapsamı teslim edilir. İş yol haritası83/83 %100.00 korunur; yeni görev/tik yok. UI toplam ilerlemesi BİLGİ MEVCUT DEĞİL.

Kanıt: [Profil ve menü](../screenshots/ui03-navigation-profile.png). Sonraki adım: yeni kullanıcı talebi; diğer ekranlara geçilmedi.

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

Kanıtlar: [API](../api/people.md), [yabancı tüzel form](../screenshots/person-foreign-legal-form.png), [mobil form](../screenshots/person-profile-mobile.png). Sonraki işlem yeni kullanıcı kapsamıdır.

Son kota okuması: 12.10.2026 07:39:22 Europe/Istanbul, Codex usage / rateLimitsByLimitId.codex. 300 dk kullanılan %50/kalan %50, reset 12.10.2026 12:21:12; 10080 dk kullanılan %55/kalan %45, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**; çıkış token: **BİLGİ MEVCUT DEĞİL**; toplam token: **BİLGİ MEVCUT DEĞİL**; kalan context: **BİLGİ MEVCUT DEĞİL**; oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

## BAKIM görsel taslak — Yeni kişi formu (12.10.2026)

**2/2 %100.00 tamamlandı:** kullanıcının ekran görüntüsü incelendi; imagegen skill'i ve built-in image_gen ile iki sütunlu/gruplu sağ panel taslağı üretildi. Kimlik, iletişim, bildirim hesabı; son görselde hesap eşleştirmesi isteğe bağlı olarak düzeltildi. Başlık, zorunlu alanlar ve alt kaydet/vazgeç görsel olarak kontrol edildi. Görsel: docs/analysis/design/person-create-proposal-v1.png. Bu yalnız görsel öneridir; uygulama kodu değiştirilmedi, typecheck/build/API testleri bu görsel turunda yeniden çalıştırılmadı. Yeni paket yok, roadmap tıkları ve83/83 sayacı korunur. Sonraki adım kullanıcının görsel hakkındaki talebidir. Giriş token/çıkış token/toplam token/kalan context: her biri BİLGİ MEVCUT DEĞİL.

## BAKIM onaylı kişi formu görseli —12.10.2026

01/03 inceleme tamamlandı;02/03 uygulama devam ediyor. Kullanıcı iki sütunlu sağ panel görselini, cinsiyet ve uyruk ikonlarını onayladı. Konum entegrasyonu kullanıcı kararıyla sonraya bırakıldı; adres manuel, geolocation izin/policy veya dış geocoder değişikliği yapılmaz. Kabul:768px panel/390px tek sütun, gruplar/ikonlar, ülke adı+bayrak ve aynıISOcode, birleşik hesap arama/ESC/odak/sayfalama, dört kişi kombinasyonu/required/version/PUTkoruma,TR/EN,typecheck/build/mevcutFEtestler. Yeni paket/backend/DBdeğişikliği yok.


### 2026-10-12 — BAKIM / UI06 kişi formu tasarımı teslimi

**Tamamlandı: 3/3 adım, bu tur %100,00.** İş yol haritası 83/83 (%100,00), UI planı toplam 12 aşama; mevcut UI06 kapsamına sunum iyileştirmesi, yeni faz/tik eklenmedi. Kişi formu 768px gruplu iki sütun, mobilde tek sütun ve sabit eylem alanı; cinsiyet/uyruk ikonları, bayraklı ülke adları ve tek aranabilir hesap seçicisi eklendi. Konum entegrasyonu kullanıcının kararıyla sonraya bırakıldı, adres manuel. API/veritabanı/bağımlılık değişmedi.

**Kanıt:** typecheck başarılı; frontend 104/104 test başarılı; üretim derlemesi mobil CSS düzeltmesi sonrası tekrar başarılı; git diff --check başarılı; paket manifest/lock değişikliği yok. Chrome masaüstü ve390×844 mobil: dört kimlik varyantı, zorunlu alanlarda5 hata/ilk alana odak, hesap popover Escape/odak dönüşü doğrulandı. Mobil son ölçüm388px içerik/388px scrollWidth, footer görünür. Görseller docs/screenshots/person-form-design-desktop.png ve person-form-design-mobile.png. Gerçek kişi kaydı/mesaj gönderimi yapılmadı.

Token giriş/çıkış/toplam/context: BİLGİ MEVCUT DEĞİL. Codex hesap son okuma: 300dk kullanılan%93/kalan%7, reset12.10.2026 12:21:12 Europe/Istanbul; 10080dk kullanılan%62/kalan%38, reset18.10.2026 09:28:33. Hesap kotası bu turun token tüketimi değildir.

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

Kesim tamamlandı: tek çalışan PG konteyneri postgres-18.6; sourcevolume'leri
korundu, test/staging DB ve testrolları temizlendi. Backend sağlıkları ve üç
realm discovery200; geçici Java süreçleri kapatıldı. AkaAI18test geçti.
Son kota okuması Codex usage/rateLimitsByLimitId.codex:300dk kullanılan%94,
kalan%6,reset13.10.2026 02:40:40 Europe/Istanbul;10080dk kullanılan%78,
kalan%22,reset18.10.2026 09:28:33. Hesap kotası tur tüketimi değildir.
Giriş/çıkış/toplam token ve kalancontext:BİLGİ MEVCUT DEĞİL.

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

Ortak Keycloak kesimi tamamlandı: yalnız keycloak-26.7.5 aktif; eski üç
Keycloak durdu. Kontrollü test hesabı/istemcileri ve iki Java test süreci
temizlendi. Kalıcı yönetici alias betiği, üç realm ve BuildingFlow sağlık200
son kez doğrulandı. Notice üretiminde belirsiz lisans0.
Son hesap kotası Codex usage/codex:300dk kullanılan%62/kalan%38,10080dk
kullanılan%10/kalan%90. Token giriş/çıkış/toplam ve kalancontext:
BİLGİ MEVCUT DEĞİL. Hesap kotası bu turun token tüketimi değildir.

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


## Merkez Talepler ve Arızalar —15.10.2026

GitHub #5 / Project3, kullanıcı onaylı merkez tamamlaması.4işpaketi:kapsam/sözleşme,API+UI+mobilokuma,otomatiktest,belge/runtime. Teknik kod/test tamam; son runtime kaydı bekleniyor. Tek MobileRequest deposu, manager-only canonical/alanseçimi, filtre/paging/sağdetay/özelfotoğraf; RECORDED→IN_PROGRESS→RESOLVED/REJECTED ve gerekçeli yenidenaçma. UUID/version/kilit/immutablehistory; yönetim geçmişi sonrası mobil içerik/fotoğraf/silme kilidi. Yeni mobil faz yok.159/159PG+S3,112/112FE,92/92mobil;typecheck/lint/build/export,329lisansmetadata PASS. Görsel/cihaz/üretim kabulü açık; Inreview. Token/context/turtüketimi:BİLGİ MEVCUT DEĞİL. Ayrıntı TESTING ve api/request-management.md.


## Merkez Talepler son teslim —16.10.2026

**4/4 işpaketi, %100 teknik teslim.** Son159/159 izole backend (alan silme geçmiş koruması dahil),112/112 frontend,92/92mobil;0fail/skip. Merkez son production build/typecheck, mobil check/lint ve3platform export;329lisansmetadata PASS. Migration20261015203750 yerelde uygulandı;36eski tablonun count/veri/audit/version hashleri birebir aynı. Migration JSON compare testleri de eski talebi korudu. Son migration yeni workflow; uygulanmış eski migration değiştirilmedi.

Merkez API5005 ve mobilAPI5006 health200; yeni merkez liste/detay anonim401, mobile sync anonim401;web3003/requests200,Metro8081running. Ağ değişiminde mobil launcher kapanmıştı; güncelLAN ile yeniden açıldı. Standard dev.sh api başlatıcısı mevcut dotnet-ef resolver hatasında durdu; aynı backend_env ayarlarıyla no-build API doğrudan başlatıldı (`/tmp/bf-requests/local-api.sh`). Model/migration zaten doğrulanıp uygulandığından runtimeAutoMigratefalse. Genel dev.sh resolver düzeltmesi bu merkez işine eklenmedi; yeni yerel başlatmada bu mevcut sınırlama dikkate alınmalıdır.

GitHub https://github.com/AkaBayraK/BuildingFlow/issues/5,Project3Inreview; yalnız bu yeni kayıt düzenlendi. Kanonik ek kabul docs/api/request-management.md ve TESTING.md; mevcut İŞ08iletişim milestone'una bir doğrulanmış görev bağlandı, yeni mobil faz yok. Güncel kaynakta106/107işgörevi=%99,07; bu turun4işpaketi sayacı ayrı tutulur. Görsel/gerçekcihaz/nativebinary/production kabulü açık; tam backend suite yeniden çalıştırılmadı. Commit/push/merge/reset/stash yapılmadı; dalmain ve önceki ortak değişiklikler korundu.

Giriş/çıkış/toplamtoken,kalancontext ve bu turun token tüketimi:BİLGİ MEVCUT DEĞİL. Son16.10.2026Codexusage:300dk kullanılan%0/kalan%100;10080dk kullanılan%47/kalan%53. Hesap pencereleri bu sohbetin token tüketimi değildir.


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


## PERSON FAZ05 —17.10.2026 devam ediyor
Kullanıcı FAZ04'ü kabul ederek FAZ05'i başlattı. #9 Done, #10 In progress; aynı görevler kullanılıyor. Tarihli ManagementAssignment, atama/bitiş/iptal/audit/UUID-version-replay; tenant/Property/issuer/üyelik/rol kapsamı, tenant bazlı LEGACY/SHADOW/ENFORCED v2 kontrolü ve son yönetici koruması uygulanıyor. Yeni eklemeli migration3 tablo ekler; eski migrationlar korunur. Son koşu310/310 seçili integration geçti; nihai SQL closure koruması/negatif durum testleri sürüyor. Ana DB/Keycloak/servis restart yok. FAZ06 başlamadı.


## PERSON FAZ05 —17.10.2026 teknik teslim

Kullanıcı FAZ04'ü kabul ederek FAZ05'i başlattı; #9 Done. Tarihli ManagementAssignment, atama/bitiş/iptal, rol ve tenant/Property kapsamı, UUID/version/replay ve değişmez audit/makbuz uygulandı. SITE_ADMIN yalnız MANAGER/ACCOUNTANT görevlerini yönetir; MANAGER kendine yetki veremez. Görevler daire malikliği/sakinlik veya Keycloak rolü üretmez.

Tenant bazlı LEGACY/SHADOW/ENFORCED v2 kontrolü, exact issuer/account-link/üyelik/doğal kişi/tarih koşulları ve stale legacy shadow reddi vardır. ENFORCED son yönetici kapatmasında kesintisiz yedek görev ve gerçek Keycloak enabled/rolü salt okunur doğrulanır; başarısız doğrulama erişimi kapalı tutar. Eski v1/mobil legacy geçişi bu fazda yapılmadı; tüm uygulamada production enforcement iddiası yoktur.

**314/314 seçili PG/fixture storage integration,187/187 unit,119/119 frontend;0fail/skip.** Son çözüm derlemesi0hata/0uyarı;329 paket lisans metadata, eski migration/paket/lock hashleri, bağlantı ve diff kontrolleri başarılı. Yeni26integration senaryosu ve3unit eklendi. Keycloak devri FakeIdentity ile doğrulandı; gerçek kimlik sunucusu kabulü yapılmadı. Ara predicate derleme hatası ortak EligiblePeople sorgusuyla düzeltildi; son testler geçti. Tüm geçici test kaynakları temizlendi.

Yeni20261017150715ManagementAssignments üç BaseEntity tablo, Designer/snapshot, scope/closure/overlap/history ve audit trigger'ları, dolu Down koruması ekler. Eski migrationlar korundu. Ana DB/görev/tenant seed veya gerçek Keycloak rol/hesap yazımı, servis restart yok. AssignmentsEnabled defaultfalse, tenantmode defaultLEGACY; gerçek .env korunur. AutoMigrate açık sonraki açılış pendingmigration uygulayabilir.

Teknik6/6=%100,00; #10 In review, kullanıcı kabulü bekleniyor. Kabul edilmiş önceki fazlar4/11=%36,36. FAZ06 başlamadı; İŞ/UI/mobil yol haritası sayacı değişmedi. Ayrıntı docs/refactor/person/phase-05-report.md ve docs/api/management-assignments.md. Yeni paket/extension/agent ve commit/push/merge/reset/stash yok. SAST/SCA/CVE/secret scanner, görsel/cihaz/native/production ve frontend typecheck/build bu fazda çalıştırılmadı.

Token tüketimi/kalan context:BİLGİ MEVCUT DEĞİL. Son17.10Codexusage300dk kullanılan%63/kalan%37, reset17.10.2026 22:37:38;10080dk kullanılan%73/kalan%27, reset22.10.2026 10:57:02 Europe/Istanbul. Hesap pencereleri sohbet tüketimi değildir.

## BAKIM — Yol haritası ICU rota metni — 18.10.2026

`phase19Task2Detail` TR/EN metinlerindeki `/users/{id}/photo` ve `/people/{id}/photo` örnekleri ICU literal quoting ile düzeltildi; `{id}` ekranda aynen görünür, formatter parametresi istenmez. Mevcut standards testi bütün roadmap dinamik anahtarlarını iki dilde parametresiz gerçek translator ile çalıştırır ve iki rota örneğinin korunmasını doğrular. **135/135 frontend testi, typecheck ve üretim build başarılı; diff kontrolü geçti.** Backend/DB/.env/bağımlılık ve faz sayaçları değişmedi; gerçek tarayıcı kontrolü yapılmadı. FAZ07 başlatılmadı. Bakım2/2=%100.

## PERSON FAZ09 —18.10.2026 — Devam ediyor
FAZ08 kabul edildi; #13 Done, mevcut #14 In progress. Canonical talep/arıza/fotoğraf ve ayrı hesap profili taslakları mevcut şifreli SQLite/outbox/idempotency altyapısına bağlanıyor. Yetki sunucuda tarihli context kimliğiyle tekrar kontrol edilir. Merkezi kişi salt okunur kalır; ödeme/kesin tahakkuk/rol değişimi offline kuyruğunda yoktur. Yeni paket/migration ve canlı .env/DB/Keycloak/servis değişimi yok. FAZ10 başlamadı. İnceleme/önkoşul/plan3/6=%50,00; uygulama ve test devam ediyor. Geçmiş FAZ08 testleri bu fazın sonucu sayılmaz.

## PERSON FAZ10 —18.10.2026 — Devam ediyor
FAZ09 kullanıcı kabulüyle #14 Done; mevcut #15 In progress. Güvenlik/lisans doğrulaması: web audit0, NuGet7projede bildirilmiş açık yok; mobil audit29kayıt(18high/11moderate),4kökadvisory — güvenlik kapısı GEÇMEDİ. Fortify/Semgrep/Gitleaks/TruffleHog/OSV/Trivy/Syft erişimi yok, kurulum yapılmadı. Gerçek runtime private-s3.json ignore olmasına rağmen HEAD/indexte takipliydi; disk içeriği ve .env korunarak yalnız git rm--cached yapıldı. Bu dosyanın staged deletion'ı bilinçlidir; commit/push/geçmiş değiştirme/anahtar rotasyonu yapılmadı. Geçmişteki erişim bilgileri ve rotasyon açık risk olarak kalır. Dört private runtime yolunun ignored/untracked kontrolü ve3negatif/pozitif fixture CI'ye eklendi. Native cihaz/MASVS ve dağıtım uyumu garanti edilmez. FAZ11 başlamadı. Shared checkout'taki website kaynak/migration değişimleri bu faza ait değildir.
## BAKIM — 23.10.2026 — Node/JVM güvenlik güncellemesi

Node `26.12.1` doğrulandı, web238/238 geçti; Docker/CI24.22.1 ve .nvmrc sabitlendi. JVM kısmı BLOKE: Oracle25.0.5 kurulumu sudo parolası nedeniyle tamamlanmadı; Keycloak JVM21.0.12.1 güncellenmedi. Bu tur2/3=%66,67; ürün fazı/yol haritası tiki değişmedi. Kanıt TESTING.md. Docker rebuild/CI yapılmadı.
