# BuildingFlow güncel çalışma durumu

Güncelleme: 2026-10-12 (Europe/Istanbul). Bu dosya mevcut takip kaynaklarının tarihli özetidir; görevlerin ve test kanıtlarının ikinci kaynağı değildir. Çalışma kuralı: [AGENTS.md](../../AGENTS.md).

## Aktif tur

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
