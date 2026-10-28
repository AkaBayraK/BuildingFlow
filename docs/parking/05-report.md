# OTOPARK FAZ05 — Rezervasyon, ziyaretçi ve operasyon

27.10.2026 • Plan05/08 • Issue[#77](https://github.com/AkaBayraK/BuildingFlow/issues/77) • Faz05 kullanıcı kabulü 27.10.2026 tarihinde alındı; #77 completed kapalı.

## Kapsam ve kaynak

Kullanıcı FAZ05'ten devam istedi; ilk dört fazı yeniden uygulamadık. Kaynak `claude/project-thread-9pmvkm` branch'i, `.claude/worktrees/practical-napier-12a6c6`; başlangıç commit'i `ced51cc` (FAZ04). Ana checkout'ta bu kod bulunmuyordu, ayrı Ortak Alanlar çalışması korunmuştur. Bu tur değişiklikleri bu worktree'dedir; main'e birleştirme, commit/push/PR/deploy yapılmadı.

Gerçek plan ana checkout'taki `/Users/abdulkadiraltunbayrak/Desktop/Projects/BuildingFlow/BuildingFlow-Araclar-Otopark/MASTER_PROMPT.md`, `FAZ-01.md`–`FAZ-08.md`; bu pakette PHASES.md yoktur. UI planının MASTER_PROMPT/PHASES dosyaları `frontend/buildingflow-ui-plan/` altındadır (12 UI aşaması); bu sayaç değiştirilmedi. Görsel referans incelendi, üçüncü taraf görsel ürüne kopyalanmadı. Kullanıcı bu tur ayrıca **mevcut yetki korunsun, misafir/geçici park operasyonu tamamlansın** kararını verdi. Sakin rezervasyonu/rol genişlemesi eklenmedi.

## Uygulanan davranış

- Mevcut `guest_vehicles` geçici rezervasyon modeli korunur. Yersiz misafir kaydı kapasite ayırmaz. VISITOR park yeri seçilirse zaman çakışması ve mevcut bakım kontrol edilir. Aynı plaka başka yerde veya yersiz çakışan aktif kayıt açamaz.
- Saatler İstanbul, API parametreleri UTC; `[başlangıç,bitiş)` ve bitişik aralık desteği. PLANNED giriş yalnız kendi saat penceresinde, ARRIVED çıkış ve PLANNED iptal version ile yapılır. Geç çıkış kendiliğinden üretilmez; çıkış yapılmamış araç yer/plakayı korur. Giriş/çıkış manuel kayıt olarak görünür, sensör verisi değildir.
- UUID replay ad/not dahil normalize içeriğin tümünü karşılaştırır; farklı içerik409. Tahsis kotası toplam kesişen kayıt yerine eşzamanlı tepe kullanımını sayar.
- Yeni `ParkingClosure` BaseEntity: aynı Property/Space compositeFK, UUID, gerekçe, tarih aralığı ve tek gerekçeli/version kontrollü iptal; geçmiş silinmez. Tahsis/misafir/kapatma iki yönlü ve aynı Property kilidinde çakışır. Çakışma varsa otomatik rezervasyon/tahsis iptali yoktur.
- `/closures` GET/POST, `/{id}/cancel` POST; yönetici yazar, SECURITY yalnız okur. `/guest-policy` yalnız configured/guestMaxHours ve `/guest-spaces` yalnız VISITOR listesi verir; guard genel politika/yönetim uçlarına erişemez. ACCOUNTANT403, yabancı alan/üyelik404 ve anonim401 korunur.
- Web ikonlu Bakım kapatmaları sekmesi, shadcn panel ve TanStack ortak liste, TR/EN, gerekçeli iptal, 409 sonrası kör tekrar engeli. Harita `isClosed` gösterir; boş yerine Tahsis yok denir. Yetkili okuma path/scope değişince önceki veri temizlenir. İlk policy yüklemesinde yanlış konfigürasyon uyarısı gösterilmez.

## Dosyalar

Domain `Parking.cs`, yeni `ParkingClosure.cs`; Application `Parking.cs`; API `ParkingEndpoints.cs`; Infrastructure `ParkingStore.Operations.cs`, `ParkingStore.Setup.cs`, yeni `ParkingStore.Closures.cs`, `ParkingConfigurations.cs`, DbContext. Yeni migration `20261027104618_ParkingMaintenanceClosures`+Designer/snapshot. Unit/API/migration testleri. Web `features/parking` model/hook/workspace/GuestsTab/style, yeni `ClosuresTab`, TR/EN, parking testleri; yalnız test fixture dosyaları/scripts. API/MIGRATIONS/TESTING/lisans/skill/progress belgeleri ve aynı OTOPARK-05 kimlikli roadmap kanıtı.

## Bu tur çalıştırılan doğrulama

| Kontrol | Sonuç |
|---|---|
| `dotnet build backend/BuildingFlow.sln --no-restore -m:1 --disable-build-servers` | 0hata/0uyarı |
| `dotnet test ... --filter FullyQualifiedName~Parking|...PersistenceTests|...AutoMigrationTests|...ErrorContractTests` (yalnız bu turun geçici PostgreSQL DB/rolü) | **42/42 integration,18/18 unit;0fail/skip** |
| Migration | Önceki sürüm→Up, tekrar, boşDown/reUp, eski Property JSON/audit eşliği, scopeFK/unique/check, iptal geçmişiyle doluDown reddi, model/snapshot ve tüm uygulama tablosu8NOTNULL |
| `npm test` | **275/275;0fail/skip** |
| `npm run typecheck` | **GEÇTİ** |
| `npm run build` | **Son roadmap/loading değişikliği sonrası da GEÇTİ** |
| CUA + `node scripts/parking-fixture.mjs` | İzole623pxTR/EN/koyu tema; oluşturma, gerekçeli iptal,409 form koruması, guard salt okunur; yatay623/623 |
| Kaynak/lisans koruması |93önceki migration dosyası fark0;manifest/lock/csproj/LICENSE/NOTICE fark0; yeni paket/font/görsel/SDK yok |

İlk integration koşusu38geçti/3başarısız, ikinci41geçti/1başarısız: eklenen tarih sorgularındaki offsetli parametreler UTC'ye normalize edilerek son42/42koşu geçti. İlk web koşuları yeni panelin eksik reason/selectSpace çeviri anahtarlarını yakaladı; eklendi, son275/275geçti. İlk sandbox .NET derlemesi iptal edildi; yetkili koşu geçti. İlk sandbox Nextbuild SWC cache erişiminde engellendi; dışarıdaki koşu geçti. Bu hatalar başarılı test sayılmadı. TRX ve günlükler `/tmp/bf-parking05-results`, `/tmp/bf-parking05-utc-tests.log`, `/tmp/bf-parking05-web-tests.log`, `/tmp/bf-parking05-typecheck.log`, `/tmp/bf-parking05-web-build-final.log`.

Görseller: [EN koyu](screenshots/05-maintenance-en-dark.png), [409 panel](screenshots/05-conflict-panel.png), [guard salt okunur](screenshots/05-guard-readonly.png). Fixture verileri yapaydır; canlı auth/API/PostgreSQL browserE2E kanıtı değildir. Tam masaüstü/375px/native/gerçek Keycloak/güvenlikscanner/yük/remoteCI çalıştırılmadı; başarılı gösterilmedi. Bütün backend suite bu tur çalıştırılmadı.

## Güvenlik, lisans ve rollback

Yeni uçlarda Property aktif üyeliği ve backend rolü gerekir. PII log/telemetri/querystring eklenmedi. Şemada8alanNOTNULL ve aynıalanFK korunur. Negatif rol/üyelik/version ve yarış testleri geçti; bu tam pentest/SAST/KVKK hukuki onayı değildir. Saklama/anonimleştirme işi FAZ07'de kalır; bu tur hukuki saklama süresi kararı verilmedi.

Yeni dış bağımlılık ihtiyacı yoktur: takvim/tarih BCL/Intl, kontroller projeye ait kod, ekran mevcut React/Next/shadcn/TanStack/Lucide ile tamamlandı. Mevcut lisans/container dağıtım yükümlülükleri `docs/licenses.md` kapsamındadır; tam binary/SBOM lisans kabulü iddia edilmez.

Migration yalnız izole testte uygulandı; canlıya uygulanmadı. Sonraki API açılışında AutoMigrate açıksa bekleyen migration uygulanabilir; bu tur uygulama servisi açılmadı/yeniden başlatılmadı. Kapatma geçmişi varsa Down ilkDROP'tan önce reddeder. Geri dönüş veri silerek yapılmaz: önceki artifact'e dönmeden önce quiesce/yedek ve ileri düzeltme değerlendirilir. Kamera/ANPR/bariyer/QR/native/otomatik doluluk yok. Mevcut kullanıcı verileri ve .env korunur. Testler yalnız kendi geçici kaynaklarını temizler.

## Kabul ve ilerleme

FAZ05 resmi8madde: analiz, uygulama, kapsamlı yetki değerlendirmesi, ilgili testler, bağımlılık/varlık kontrolü, değişiklik/kanıt kaydı, risk/rollback **teknik olarak tamamlandı7/8=%87,50**; **kullanıcı kabulü bekliyor**. CI/commit/PR yokluğu açık raporlandı, varmış gibi yazılmadı. Tur inceleme/uygulama/doğrulama: **3/3 = %100,00 teknik teslim**. Önceki4/8faz kodlandı bilgisi kullanıcı beyanıdır; bu tur yeniden test edilmiş kabul oranı değildir. Ürün roadmap bu branch'te25kayıt,142/153=%92,81; bu oran üretime hazırlık değildir. Sonraki FAZ06 başlatılmadı;#77 açık tutulur.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Son kaynak Codexusage/rateLimitsByLimitId.codex:300dk%69kullanılan/%31kalan reset27.10.2026 18:39:51;10080dk%58/%42 reset02.11.2026 15:28:41Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

Teslim işlemleri: #77'ye gerçek sonuçlar ve sınırlamalar [yorumla](https://github.com/AkaBayraK/BuildingFlow/issues/77#issuecomment-6440146390) yazıldı, görev açık tutuldu. Bu tur oluşturulan geçici test DB/rolü temizlendi; mevcut servis/veritabanı değiştirilmedi. Yeni belge bağlantıları mevcut; TESTING.md'de bu tur öncesinden kalan 16 bozuk bağlantı kapsam dışı olarak korundu.

27.10.2026 kullanıcı “faz 06 geçebilirsin” talimatıyla Faz05 kabulünü verdi: resmi8/8=%100,00. Tarihsel teknik test ve canlı/native/CI sınırlamaları değişmez; #77 completed kapandı. Sonraki çalışma Faz06/#78'dir.
