# BuildingFlow güncel çalışma durumu

Güncelleme: 2026-10-11 (Europe/Istanbul). Bu dosya mevcut takip kaynaklarının tarihli özetidir; görevlerin ve test kanıtlarının ikinci kaynağı değildir. Çalışma kuralı: [AGENTS.md](../../AGENTS.md).

## Aktif tur

**Onaylı kapsam İŞ11 — Yayın hazırlığı ve kalite;11.10.2026 kullanıcı onayı.** [Analiz](../analysis/PHASE_11_RELEASE_QUALITY.md). Yerel yayın adayı ve araçlar; hedef üretim ortamına dağıtım yok.

| Alan | Durum |
| --- | --- |
| Akış / faz | İŞ11, kayıt14/14 |
| Aktif adım | Yok — İŞ11 teslim edildi |
| Durum | TAMAMLANDI |
| Tamamlanan / toplam | 4/4 |
| Faz ilerlemesi | %100.00 |
| Engel | Geliştirmede yok; gerçek go-live için dış domain/TLS/SMTP gerekir |
| Sonraki işlem | Kullanıcı kararı: main'e merge/push, hedef ortamda go-live; yeni faz uydurulmaz |

## İş yol haritası özeti

Kaynak: [milestones.ts](../../frontend/buildingflow-web/src/features/roadmap/milestones.ts); kabul kanıtı [TESTING.md](../TESTING.md). **14 faz kaydı:** 00–11 ve onaylı ek 06.1 / 07.1. Kimlik 06.1 listedeki 8. kayıttır; faz kimliği sıra sayısı değildir. Son tamamlanan **İŞ10: 4/4 = %100.00**; önceki teslimler korunur. İŞ10 ve İŞ11 tamamlandı; yeni faz için kullanıcı onayı beklenir.

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

Son kullanım okuması: Codex usage, 11.10.2026 10:12:48 Europe/Istanbul; 300 dk kullanılan %88 / kalan %12, reset 11.10.2026 14:28:33; 10080 dk kullanılan %14 / kalan %86, reset 18.10.2026 09:28:33. Giriş token: **BİLGİ MEVCUT DEĞİL**. Çıkış token: **BİLGİ MEVCUT DEĞİL**. Toplam token: **BİLGİ MEVCUT DEĞİL**. Kalan context: **BİLGİ MEVCUT DEĞİL**. Oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.

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

**4/4 resmi görev = %100.00; genel iş 83/83 = %100.00.** Son `bash scripts/ci.sh` çıkış 0: **426 .NET (88 unit + 338 integration), 85 FE, hata/atlama 0**, typecheck/build, npm audit 0, NuGet audit temiz, 331 lisans, idempotent SQL provası, preflight testi (21 güvensiz varyant reddi). Yedek provası 8/8; smoke 15/15; gözlemlenebilirlik 12/12; anonim SSR 30 rota sızıntı 0; TR/EN ve 375 px mobil. Bulunan ve düzeltilen sorunlar: bozuk idempotent betik, PostgreSQL readiness yarışı, Docker imajında açılmayan HSTS, kırılgan worker log penceresi. Dal `faz-10-11-release` (Faz10 + 4 adım commit'i); main'e merge/push yok. Dış TLS/SMTP/CI ile go-live yapılmadı. Ayrıntı [TESTING.md](../TESTING.md), [RELEASE.md](../RELEASE.md).
