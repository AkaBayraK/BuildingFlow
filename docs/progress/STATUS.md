# BuildingFlow güncel çalışma durumu

Güncelleme: 2026-10-10 (Europe/Istanbul). Bu dosya mevcut takip kaynaklarının tarihli özetidir; görevlerin ve test kanıtlarının ikinci kaynağı değildir. Çalışma kuralı: [AGENTS.md](../../AGENTS.md).

## Aktif tur

**Onaylı kapsam: İŞ 09 — Asenkron mail, SMS ve bildirim; kullanıcı 10.10.2026 başlatmayı onayladı.** Sağlayıcı kararı: SMTP e-posta, SMS sağlayıcısı daha sonra. Önceki İŞ08 teslimi korunur. [Analiz](../analysis/PHASE_09_MESSAGING.md).

| Alan | Durum |
| --- | --- |
| Akış / faz | İŞ09, kayıt12/14 |
| Aktif adım | 01/04 — Mesaj sözleşmeleri ve yayıncı |
| Durum | DEVAM EDIYOR |
| Tamamlanan / toplam | 0/4 |
| Faz ilerlemesi | %0.00 |
| Engel | Yok; gerçek SMTP gönderimi için sağlayıcı ayarları gelecekte gerekecek |
| Sonraki işlem | Sürümlü sözleşme ve RabbitMQ yayıncı testleri |

| Adım | İş | Durum |
| --- | --- | --- |
| 01/04 | Mesaj sözleşmeleri ve yayıncı | DEVAM EDIYOR |
| 02/04 | Outbox ve tüketiciler | BEKLIYOR |
| 03/04 | Retry, dead-letter ve idempotency | BEKLIYOR |
| 04/04 | SMTP sağlayıcı entegrasyonu ve testler; SMS ertelendi | BEKLIYOR |

## İş yol haritası özeti

Kaynak: [milestones.ts](../../frontend/buildingflow-web/src/features/roadmap/milestones.ts); kabul kanıtı [TESTING.md](../TESTING.md). **14 faz kaydı:** 00–11 ve onaylı ek 06.1 / 07.1. Kimlik 06.1 listedeki 8. kayıttır; faz kimliği sıra sayısı değildir. Son tamamlanan **İŞ 08: 4/4 = %100.00**; önceki teslimler korunur. İŞ09 kullanıcı tarafından onaylandı; adım01 başladı.

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
| 09 | Asenkron mail, SMS ve bildirim | 0/4 | DEVAM EDIYOR | %0.00 |
| 10 | Raporlar, sayaçlar ve gözlemlenebilirlik | 0/4 | BEKLIYOR | %0 |
| 11 | Yayın hazırlığı ve kalite | 0/4 | BEKLIYOR | %0 |

**Adım bazlı genel iş ilerlemesi: 71 / 83 = %85.54.** Bu oran mevcut yol haritasının tamamlanan adımlarını sayar; fazların ortalaması veya üretime hazırlık ölçüsü değildir. UI ve protokol adımları bu paydaya eklenmedi. Kaynakta görev sayısı değişince özet ve oran aynı turda yeniden hesaplanır; geçmiş teslim kanıtları korunur.

## Ayrı planlar ve belge konumları

- UI planı: [MASTER_PROMPT.md](../../frontend/buildingflow-ui-plan/MASTER_PROMPT.md), [PHASES.md](../../frontend/buildingflow-ui-plan/PHASES.md); **12 aşama (01–12)**. Kök dizinde bu iki dosya yoktur. UI 01 analiz ve UI 06 uygulama kayıtları [UI_ANALYSIS_REPORT.md](../analysis/UI_ANALYSIS_REPORT.md) ile [UI_STAGE06_REPORT.md](../analysis/UI_STAGE06_REPORT.md) içindedir. UI 06 ayrı kartta **4/4** kabul görevi gösterir; önceki UI 02–05'in tüm kapsamları teslim edilmiş sayılmaz. Bütün UI planının adım bazlı yüzdesi: **BİLGİ MEVCUT DEĞİL** (tüm aşamalar için resmi adım paydası yok).
- İlk teknik plan: [mimari kaynak](../BUILDINGFLOW_PROJECT_CORE.md), bölüm 20, **8 faz**. Tarihsel Messaging/Storage/Observability adları güncel iş 05/06/07 ile eş tutulmaz; iş yol haritasındaki altyapı temeli tüm sağlayıcı entegrasyonlarının bittiği anlamına gelmez.
- Test kanıtları [TESTING.md](../TESTING.md), bu turun değişiklikleri [CHANGELOG.md](CHANGELOG.md), devam bağlamı ve kullanım okuması [SESSION.md](SESSION.md) içinde tutulur.

## Kalan gerçek sınırlar

- Faz 06.1 eski borçlara bilinmeyen kişi geçmişi üretmez. Katalog CRUD, 500'ün üzerindeki toplu onay ve canlı kullanıcı verisinde başarılı finans mutation'ı teslimin kabul kapsamı değildir; ayrı PostgreSQL testleri ile salt-okunur tarayıcı kabulü ayrıdır.
- FE sekme tekrar kullanımı Chrome/Safari native adapter testleriyle doğrulandı; canlı macOS Automation izni/GUI kabulü yapılmış sayılmaz.
- Online ödeme/gerçek banka transferi, üretim SMTP/HTTPS, tam gözlemlenebilirlik, yedekleme/CI ve gelecekteki iş fazları planlıdır. Ayrıntılı kapsam/sınırlar TESTING ve API belgelerinden okunur.
- 07.1 yalnız mesaj sözleşmesi ve sunum altyapısıdır; domain/DB/migration, kimlik politikası ve finans hesaplama kuralları değişmez.

## İŞ 07 teslim kanıtı

**292 .NET (59 unit + 233 integration), 67 FE; hata/atlama0.** Typecheck/build, EF model eşliği/idempotent SQL, 312 lisans, 18 korunan dosya ve 12 eski DB tablosu hash eşliği. Yerel migration uygulandı, BE5005 Healthy, FE3003 dev süreci korunur. TR/EN/1280×900 sağ512px, 390×844 mobil ve boş/sıfır plan kabulü kaydetmeden tamamlandı. Detay [TESTING.md](../TESTING.md), [API](../api/expenses.md). Kategori CRUD/dosya eki/banka mutabakatı/ödeme emri yok; kullanıcı DB'sinde başarılı finans mutation yapılmadı.

## İŞ 08 teslim kanıtı

**319 .NET (68 unit +251 integration), 76 FE; hata/atlama0.** Gerçek ayrı PostgreSQL/özel geçici MinIO, migration/model/Down/tekrar/idempotent SQL; TR/EN sağ512px ve390px mobil panel kabulü, son typecheck/build,316 lisans ve19 korunan dosya kontrolü geçti. İki yeni BaseEntity tablosu ve Minio7.0.0 SDK; Duyurular/Belgeler menüleri yerel FE3003'te, API5005 Healthy. Yerel tekrar migration öncesi/sonrası15 eski tablo hash'i aynı; ilk canlı upgrade öncesi hash kanıtı alınamadı, ayrı PG yükseltme testi vardır. Kullanıcı DB'sinde başarılı duyuru/dosya mutation yapılmadı. Mail/SMS İŞ09, dosya taraması/retention ve finans eki gelecekteki kapsam; Docker imajı bu tur derlenmedi. Detay [TESTING.md](../TESTING.md), [API](../api/communication.md).

Son kullanım okuması: Codex usage, 10.10.2026 11:41:45 Europe/Istanbul; 300dk kullanılan%19/kalan%81, reset10.10.2026 16:35:15; 10080dk kullanılan%97/kalan%3, reset15.10.2026 22:03:08. Giriş/çıkış/toplam token, kalan context ve oturum tüketimi: **BİLGİ MEVCUT DEĞİL**. Hesap kotası sohbet tüketimi değildir.
