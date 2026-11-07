# AI Asistan — FAZ 0 + FAZ 1 (yönetici özet asistanı)

Salt okunur yönetim özeti. Asistan yeni veri yolu açmaz: araçlar mevcut servisleri (Dashboard, Gider/Bütçe, Bakım, Talep yönetimi) aynı rol ve aktif Property üyeliği kontrolüyle çağırır. Yazma aracı, sohbet geçmişi, serbest SQL ve doğrudan DB erişimi yoktur. Analiz: "BuildingFlow AI Asistan Ekranı Analizi" (01.11.2026).

## Yetki

| Uç | Politika | Roller |
| --- | --- | --- |
| `GET /api/v1/assistant/status`, `GET /api/v1/assistant/prompts`, `POST /api/v1/assistant/chat` | `CanUseAssistant` | SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT |
| `GET/POST /api/v1/assistant/admin/prompts`, `PUT/DELETE /api/v1/assistant/admin/prompts/{id}` | `CanManageAssistantPrompts` | SUPER_ADMIN, SITE_ADMIN |

Anonim 401, RESIDENT/OWNER/TENANT/SECURITY 403. Servis rolü ayrıca doğrular. `OPEN_REQUESTS` aracı talep yönetimi politikasını izler: ACCOUNTANT'ın araç listesinde ve hazır sorularında görünmez, kodla istenirse 400 döner; günün özetinde talep sayıları ACCOUNTANT'a gelmez. Kişi verisi hiçbir rol için asistandan okunmaz.

Not: analiz belgesindeki `/api/assistant` kısaltması, projenin bütün uçlarında olduğu gibi `/api/v1/assistant` olarak uygulandı.

## `GET /status`

```json
{"enabled":true,"mode":"RULES","quota":{"dailyLimit":30,"usedToday":2,"remaining":28},"tools":["DAILY_SUMMARY","DUES_COLLECTION","BUDGET_VARIANCE","UPCOMING_MAINTENANCE"],"canManagePrompts":false,"readOnly":true}
```

`mode`: `AKAAI` (model yapılandırılmış) veya `RULES` (kural tabanlı). Asistan model olmadan kapanmaz. `Cache-Control: no-store`.

## `GET /prompts`

Aktif ve rolün araçlarına bağlı hazır sorular, `sortOrder` + `code` sırasıyla: `id, code, category, toolKey, titleTr, titleEn, textTr, textEn, sortOrder, isEnabled, version`.

## `POST /chat`

İstek (bilinmeyen alan 400):

```json
{"propertyId":"<uuid>","promptCode":"OVERDUE_DUES","locale":"tr"}
{"propertyId":"<uuid>","message":"Bu ay geciken aidat ne kadar?","locale":"en"}
```

- `promptCode` ile `message`'tan tam olarak biri; `message` 1–500 karakter, kontrol karakteri yok; `locale` `tr`/`en` (varsayılan `tr`). Hatalı istek `BF_VALIDATION_FAILED` + alan kodu `BF_ASSISTANT_QUESTION_INVALID` (`message`/`promptCode`).
- Property erişilemez, silinmiş veya pasifse 404; bu durumda ve doğrulama hatasında kota tüketilmez.
- Kota: kullanıcı (`sub`) başına Europe/Istanbul iş günü; varsayılan 30, `Assistant:DailyRequestLimit` (1–1000). Sayaç audit tablosundan okunur ve kullanıcı başına advisory lock altında ayrılır; eşzamanlı istekler sınırı aşamaz, süreç yeniden başlasa da korunur. Aşımda 429 `BF_ASSISTANT_BUDGET`.

Yanıt:

```json
{"status":"ANSWERED","tool":"DUES_COLLECTION","source":"RULES","modelFallback":false,
 "answer":"Vadesi geçmiş 1 borç kaydında toplam 100,00 TL gecikmede. …",
 "sources":[{"tool":"DUES_COLLECTION","key":"overdueAmount","value":"100.00","kind":"MONEY","dimension":null,"from":"2026-10-01","to":"2026-11-01","href":"/properties/<uuid>/dues"}],
 "quota":{"dailyLimit":30,"usedToday":1,"remaining":29},"readOnly":true}
```

- `status`: `ANSWERED` veya `UNSUPPORTED` ("henüz cevaplayamıyorum"; `tool` null, kaynak kartı yok).
- `source`: cevabın metnini kim kurdu — `AKAAI` veya `RULES`. `modelFallback=true`: model yapılandırılmış ama ulaşılamadı ya da cevabı doğrulamayı geçemedi; metin kural tabanlıdır.
- Kaynak kartları (`sources`) araçtan gelir; `kind`: `MONEY` (string, iki ondalık), `COUNT`, `PERCENT`, `DATE` (`yyyy-MM-dd`), `DAYS`. `dimension`: kategori kodu (talep kategorisi, gider kategorisi, bakım türü) veya dönem (`yyyy`/`yyyy-MM`). `href` yalnız ilgili ekranın uygulama içi yolu.

### Araç seçimi

- Hazır soru kodu doğrudan tek araca bağlıdır; model araç seçmez.
- Serbest soru yalnız model yapılandırılmışsa yanıtlanır: model kapalı JSON `{"tool":"ANAHTAR"}` / `{"tool":null}` döndürür ve yalnız rolün araç allowlist'inden biri kabul edilir. Bilinmeyen anahtar, null veya şema dışı cevap `UNSUPPORTED` olur. Anahtar kelime eşleştirmesi yoktur; RuleBased modda serbest soru her zaman `UNSUPPORTED`.

### Araç kataloğu

| Araç | Kaynak servis | Modele/karta giden değerler |
| --- | --- | --- |
| `DAILY_SUMMARY` | DashboardService, MaintenanceService.Upcoming | bölüm/dolu/boş sayısı, geciken ve 30 günde vadesi gelecek tutar/kayıt sayısı, bu ay tahsilat, açık talep sayıları (yönetici rolleri), 30 gün bakım sayısı |
| `DUES_COLLECTION` | DashboardService | geciken/yaklaşan toplamlar, bu ay ve önceki ay tahakkuk/tahsil/vadesi gelmemiş/geciken ve oran, yıl toplamı |
| `BUDGET_VARIANCE` | ExpenseService.SummaryAsync (bu ay) | gelir, gider, bütçe, net, bütçeyi aşan kategori sayısı ve aşım tutarı, bütçesiz gider kategorileri |
| `UPCOMING_MAINTENANCE` | MaintenanceService.Upcoming (en çok 10 kayıt) | 30 gün bakım sayısı, ilk tarih, süren sayı, türe göre dağılım; plan başlığı/yer/sorumlu yok |
| `OPEN_REQUESTS` | IRequestManagementStore.ListAsync (RECORDED + IN_PROGRESS, en eski ilk, en çok 5×100) | durum sayıları, yüksek öncelik, 7 günden eski, en eski bekleme günü, kategoriye göre sayı; talep metni ve daire yok. 500'den fazla açık talepte `requestsSampleSize` kartı örneklem boyutunu söyler |

### Model sınırı (FAZ 0 kararları)

- Model: AkaAI, QA Merkezi'nden ayrı `Assistant:AkaAi` yapılandırması (`BaseUrl`, `TokenUrl`, `ClientId`, `ClientSecret`, `Model`, `TimeoutSeconds`). Secret yalnız ortam değişkeni (`Assistant__AkaAi__ClientSecret`) veya user-secrets ile verilir; repo/appsettings içinde yoktur. OAuth client_credentials; `X-Actor-Ref` kullanıcı kimliğinin SHA-256 özetinin ilk 32 hanesidir.
- Modele giden yük: sistem kuralları, `<soru>` içinde soru metni (hazır soruda katalog metni) ve `<veri>` içinde yalnız araç anahtarı, tarih aralığı ve `key/value/kind/dimension` dizisi. Kişi adı, telefon, e-posta, daire–kişi eşleşmesi, talep metni, bakım başlığı/sorumlusu, Property adı/kimliği gönderilmez. Kullanıcının serbest sorusuna kendisi kişisel veri yazarsa bu metin AkaAI'ye iletilir ama saklanmaz; ekran bunu uyarır.
- Cevap kapalı şema `{"answer":"…"}` (1–1200 karakter). Cevaptaki her sayı araç sonucundaki bir değere (veya tarih parçasına) karşılık gelmelidir; araçta olmayan sayı, ek alan, boş metin veya JSON dışı cevap reddedilir ve kural tabanlı metin döner (`modelFallback=true`). `<soru>`/`<veri>` içindeki talimatlar yok sayılır; asıl koruma çıktı doğrulaması ve yazma aracı olmamasıdır.
- Model hatası (bağlantı, token, zaman aşımı, şema) 503 üretmez; cevap kural tabanlıdır.

## Audit ve saklama

`assistant_audit_entries` (BaseEntity): `user_id`, `property_id` (FK Restrict), `business_day`, `request_kind` (`PROMPT`/`FREE_TEXT`), `prompt_code`, `tool_key`, `source`, `outcome` (`STARTED`→`ANSWERED`/`UNSUPPORTED`/`FAILED`), `duration_ms`. Soru metni, cevap ve kaynak değerleri **saklanmaz ve loglanmaz**; uygulama logu yalnız `BuildingFlow.Assistant call {Tool} {Source} {Outcome} {DurationMs}` yazar. Sohbet ekranı geçmişi yalnız tarayıcı belleğindedir; sayfa/alan değişince silinir.

## Hazır soru yönetimi

`assistant_prompts` (BaseEntity, global): `code` (`^[A-Z][A-Z0-9_]{1,39}$`, silinmemişler içinde tekil, oluşturulduktan sonra değişmez), `category` (`OVERVIEW`/`FINANCE`/`OPERATIONS`/`REQUESTS`), `tool_key` (5 araç), `title_tr/en` (≤120), `text_tr/en` (≤500), `sort_order` (0–999), `is_enabled`. Seed: 13 soru (`created_by=migration`).

- `POST` `version=0`; aynı içerikli tekrar mevcut kaydı döner, farklı içerikle aynı kod 409 `BF_ASSISTANT_PROMPT_DUPLICATE`.
- `PUT` beklenen `version`; farklıysa 409 `BF_RECORD_CHANGED` (aynı içerik tekrarında mevcut kayıt döner).
- `DELETE ?version=` soft-delete; geçmiş/audit alanları korunur.
- Alan hataları `BF_VALIDATION_FAILED` + `BF_ASSISTANT_PROMPT_INVALID`.

## Migration

`20261101193632_AssistantFoundation`: iki tablo, check constraint'ler, `uidx_assistant_prompts_code` (partial), `idx_assistant_audit_entries_user_id_business_day`, `idx_assistant_audit_entries_property_id`, 13 seed satırı. `Down`, audit kaydı veya yönetici tarafından değiştirilmiş hazır soru varsa durur.

## Web

`/assistant` (Genel Bakış altında, Dashboard'un hemen altında "AI Asistan"; dört okuma rolüne görünür). Alan seçimi Dashboard ile ortak `?property=` (üst çubuktaki seçici). Dashboard'da "Asistana sor" kısayolu. Rozetler: salt okunur, kapsam, kişisel veri gönderilmez, mod, günlük kota. Kaynak kartları tutarı ortak formatter ile gösterir ve ilgili ekrana bağlanır. Hazır soru yönetimi sağ ManagementPanel'dedir (yalnız SUPER_ADMIN/SITE_ADMIN). Mobil uygulama bu fazda yoktur.

## FAZ 2 — Operasyon asistanı (01.11.2026)

Aynı uçlar ve desenler (araç kataloğu, allowlist, kapalı şema, kaynak kartı, kota, audit). Yeni migration `AssistantOperations`: 7 yeni hazır soru (toplam 20), `COMMUNICATION` kategorisi, yeni araç anahtarları ve audit `request_kind='DRAFT'` için check constraint güncellemesi. `Down`, DRAFT audit kaydı veya yöneticinin FAZ 2 araçlarına bağladığı hazır soru varsa durur.

### Takma ad (alias) sözleşmesi

Listelenen kayıtlar modele yalnız takma adla gider: `T#` talep, `W#` iş emri, `C#` sözleşme, `F#` fatura, `A1` anket, `Q#` soru, `O#` seçenek. Kaynak kartı alanları: `ref` (takma ad) ve `label` (kullanıcının görmeye yetkili olduğu gerçek değer; modele **gitmez**). Model cevabındaki takma adlar doğrulamadan sonra backend tarafından `label` ile değiştirilir; sonuçta olmayan takma ad (ör. `T9`) cevabı reddettirir ve kural tabanlı metne düşer. Sayı kontrolü takma adlar çıkarıldıktan sonra yapılır.

### Yeni araçlar (yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER; ACCOUNTANT'a kapalı)

| Araç | Kaynak | Modele/karta giden değerler |
| --- | --- | --- |
| `REQUEST_BACKLOG` | IRequestManagementStore.ListAsync (4 durum sayısı + en eski 5 açık talep) | `requestsByStatus` (RECORDED/IN_PROGRESS/RESOLVED/REJECTED, filtreli liste bağlantısı), `openRequestAge` (gün, kategori, `T#`; etiket "12 numaralı bölüm talebi"/"ortak alan talebi"; `?request=` bağlantısı). **Talep metni modele hiç gitmez:** BuildingFlow'da AkaPrivacy entegrasyonu yok, yeni entegrasyon kurulmadı. |
| `SUPPLIER_WATCH` | SupplierStore.Alerts (WORK_ORDER_OVERDUE, CONTRACT_EXPIRY; tenant tercihleri geçerli) + SupplierStore.AdviceData (onay/ödeme bekleyen faturalar) — Property'nin `PersonPropertyScope` tenant'ları üzerinden, modülün kendi tenant/üyelik/yönetici kontrolüyle | geciken iş emri sayısı ve en geç 5'i (`W#`, gecikme günü), bitmek üzere olan sözleşmeler (`C#`, kalan gün), bekleyen fatura sayısı/toplamı ve en büyük 5'i (`F#`). Tedarikçi adı ve iş emri/sözleşme başlığı yalnız etikette. Uyarı sayfası 100 ile sınırlı; aşımda `supplierAlertsTruncated`. |
| `SURVEY_SUMMARY` | SurveyService.Overview + Results (son tamamlanan, yoksa en yüksek katılımlı anket) | aktif/tamamlanan anket, yanıt/uygun kişi, katılım oranı; soru başına seçenek sayıları (`Q#`/`O#`), puan ortalaması (`SCORE`). **5'ten az yanıtlı soruda dağılım yok** (`surveySmallGroup`); anonim sonuç gizliyse `surveyResultsHidden`; açık uçlu cevapların yalnız sayısı (`surveyTextAnswers`), metni hiçbir yere gitmez. |

Talep tarafında alanda tanımlı bir SLA yoktur; uydurulmadı. Bekleme süresi ve FAZ 1'deki "7 günden eski" sayısı kullanılır. SLA, tedarikçi iş emirlerindeki `DueAt` üzerinden (`SUPPLIER_WATCH`) raporlanır.

### Duyuru taslağı — `POST /api/v1/assistant/announcement-draft`

```json
{"propertyId":"<uuid>","note":"Yarın 10:00-14:00 arası su kesintisi var.","category":"MAINTENANCE","locale":"tr"}
```

- Yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER (ACCOUNTANT/RESIDENT 403). `note` 1–1000, kontrol karakteri yok; `category` duyuru kategorilerinden (varsayılan GENERAL); bilinmeyen alan 400.
- Not, modele veya şablona gitmeden **maskelenir**: e-posta, TR cep/sabit telefon, 11 haneli T.C. kimlik, TR IBAN → `[e-posta]`, `[telefon]`, `[kimlik no]`, `[IBAN]`. Noktalı tarih/saat ve tutarlar maskelenmez.
- Model kapalı şema `{"title","content"}` (≤200/≤2000) döndürmelidir; notta olmayan sayı, iletişim bilgisi, ek alan veya JSON dışı cevap reddedilir → kural tabanlı şablon (`modelFallback=true`). `<not>` içindeki talimatlar yok sayılır.
- Yanıt: `title, content, category, source, modelFallback, masked, quota, saved:false`. **Hiçbir kayıt oluşturulmaz, yayınlanmaz.** Kota tüketir; audit `request_kind=DRAFT`, `tool_key=ANNOUNCEMENT_DRAFT`.
- Web: "Duyuru taslağı olarak aç" taslağı sekmenin `sessionStorage`'ına bir kez yazar ve `/properties/{id}/announcements?assistantDraft=1` açar; duyuru ekranı taslağı okuyup hemen siler ve mevcut "Yeni duyuru" editörünü önceden doldurur. Kaydetme/yayınlama mevcut duyuru akışındadır.

### Derin bağlantılar

Kaynak kartları: `/properties/{id}/requests?status=…`, `/properties/{id}/requests?request={uuid}` (detay paneli açılır), `/properties/{id}/surveys?survey={uuid}` (sonuç paneli açılır), `/suppliers?view=alerts` (tedarikçi Uyarılar görünümü; çalışma alanı seçimi yine kullanıcıdadır). FE okuyucusu yalnız bu kalıpları ve seçili Property'yi kabul eder.

## FAZ 3 — Sakin asistanı (mobil) — VARSAYILAN KAPALI (01.11.2026)

Analizdeki geçit (KVKK danışman görüşü, ayrı rıza türü, yayınlanmış aydınlatma metni) henüz sağlanmadığı için kod teslim edildi ama sakin tarafı **kapalı** gelir:

- Bayrak `Assistant:Resident:Enabled` varsayılan `false`. Kapalıyken bütün sakin uçları 403 `BF_ASSISTANT_RESIDENT_DISABLED`; mobil ekran asistanı göstermez.
- Rıza türü `AI_ASSISTANT` (`EXPLICIT_CONSENT`, sistem türü, feature `AI_ASSISTANT`) migration `AssistantResident` ile **pasif ve metinsiz** eklenir. Hiçbir metin yayınlanmadı. Açmak için: KVKK görüşü → yönetici türü etkinleştirir → mevcut rıza modülünden metin yayınlar → bayrak açılır. Rıza yoksa/geri çekilmişse/yeni metin yeniden onay istiyorsa her çağrı 403 `BF_CONSENT_REQUIRED`; mobil, mevcut rıza akışını (`ConsentSheet`) açar. Geri çekme bir sonraki çağrıda asistanı kapatır.

### Uçlar (`/api/v1/mobile/assistant`, oturum açmış hesap; servis bayrak + rıza + bağlamı her çağrıda doğrular)

- `GET /status` → `{enabled, mode, quota, tools, readOnly}`; `GET /prompts` → yalnız `RESIDENT_*` araçlı aktif hazır sorular (seed 6).
- `POST /chat` `{propertyId, unitId, role: OWNER|TENANT, promptCode|message, locale}`; bilinmeyen alan 400. Bağlam önce mevcut `IMobileFinanceStore` ile doğrulanır: kişinin kendi oturum bağlamında olmayan bölüm, Property veya rol 403 ve **kota tüketmez**.
- Kota ayrı: varsayılan 10/gün (`Assistant:Resident:DailyRequestLimit`, 1–100); audit `request_kind` `RESIDENT_PROMPT`/`RESIDENT_FREE`; yönetici kotası etkilenmez. Soru/cevap saklanmaz.

### Araçlar (yalnız mevcut mobil servisler)

| Araç | Kaynak | Modele giden |
| --- | --- | --- |
| `RESIDENT_BALANCE` | IMobileFinanceStore (charges + payments, sakin borç kapsamı) | kendi toplam/geciken/ödenen/tazminat tutarı, sonraki son ödeme tarihi ve tutarı, son 3 ödeme; Property/bölüm/kişi adı yok |
| `RESIDENT_REQUESTS` | IMobileSyncStore.PullAsync (yalnız kendi açtığı talepler) | durum sayıları, son 5 talebin kategori + son güncellemeden bu yana gün (`R#`); talep metni ve yönetim cevabı yok |
| `RESIDENT_NOTICES` | IMobileNoticeStore (yalnız yayınlanmış duyurular) + CommunicationService.Documents (yetkiliyse) | duyuru/okunmamış sayısı; son 5 duyuru için tarih ve **maskeli, 400 karakterle sınırlı** başlık+metin (`excerpt`, `N#`); belge için yalnız yayın tarihi (`B#`). **Belge gövdesi okunmaz/gönderilmez** (PDF metin çıkarma yok, yeni paket yok) — belge içeriği soru-cevabı sonraki faza bırakıldı. |

Mobil sakin bağlamı olmayan hesaplarda `documentDate` kartı gelmeyebilir (CommunicationService üyelik ister). Kanonik yetki modelinde (`canonicalMode`) mobil istemci asistan yolunu açmaz; mevcut finans/duyuru yollarıyla aynı davranış.

## AI Durumu ekranı (02.11.2026)

`GET /api/v1/system/ai-status` (`CanManageHierarchy`: SUPER_ADMIN/SITE_ADMIN/MANAGER, `no-store`) — TraceFlow "AI Durumu" ekranının karşılığı. Altyapı bileşenleri `/api/v1/system/status` ekranında kalır; burada yalnız asistan/AkaAI hattı izlenir.

- Bileşenler: `assistant-configuration` (Assistant:AkaAi tanımlı mı), `akaai-model` (AkaAI `/readyz` + servis hesabıyla `/api/v1/integrations/models`; seçili `Model` ya da `x_akaai_default` modeli), `prompt-templates` (aktif hazır soru sayısı), `resident-assistant` (FAZ 3 bayrağı; kapalıyken `CONSENT_GATE`), `qa-center-ai` (QaCenter:AkaAi).
- Model çağrısı yapılmaz; AkaAI sonucu 30 sn önbellekte tutulur. Açıklamalar dil bağımsız `detail` kodudur, metni web sözlüğü üretir. Yanıtta token, secret, TokenUrl, tam adres veya AkaAI hata gövdesi yoktur; endpoint yalnız `host:port`.
- AkaAI tanımlı değilse genel durum `DISABLED`, mod `RULES`. `locality=EXTERNAL` ise ekran harici model uyarısı gösterir. İsteğe bağlı `Assistant:AkaAi:ConsoleUrl` yalnız kimlik/sorgu içermeyen http(s) adresse "AkaAI'de yönet" bağlantısı (`…/models`) olur.
- Web: Yönetim > AI Durumu (`/system-settings/ai-status`, Sistem Ayarları'nda giriş kartı), özet kartları, tür filtreleri, 15/30/60 sn otomatik yenileme, son kontroller ve bilgi kartı. TraceFlow'daki PDF dışa aktarma alınmadı.

## Sesli yazışma (06.11.2026)

Yalnız web AI Asistan ekranında; **SUPER_ADMIN, SITE_ADMIN, MANAGER** (politika `CanUseAssistantVoice`; ACCOUNTANT/RESIDENT/OWNER/TENANT/SECURITY 403, anonim 401). Raporlama ekranında AI asistan yoktur; orada yeni asistan eklenmedi.

- **Tarayıcı Web Speech API kullanılmaz** (Chrome sesi tarayıcı sağlayıcısına gönderir). Tarayıcı `MediaRecorder` ile kaydeder (yeni paket yok); ses sunucuya gider, sunucu yapılandırılmış **yerel** STT sağlayıcısına bir kez iletir.
- `GET /api/v1/assistant/voice/status` → `{enabled, reason: "PROVIDER_NOT_CONFIGURED"|null, maxSeconds: 60, maxBytes: 10485760, acceptedTypes}`.
- `POST /api/v1/assistant/transcribe` (multipart): tek dosya alanı + `propertyId`, `durationMs` (1–60000), isteğe bağlı `locale` (`tr` varsayılan/`en`); başka alan 400. MIME (`audio/webm|ogg|mp4|wav|mpeg`, `codecs` parametresi ve `audio/x-wav`/`audio/mp3`/`video/webm` eşdeğerleri kabul) **ve dosya imzası** (EBML, OggS, ftyp, RIFF/WAVE, ID3/MPEG) eşleşmeli; 10 MB üstü 400 `BF_ASSISTANT_VOICE_INVALID`. Sağlayıcı yoksa veya hata verirse 503 `BF_ASSISTANT_VOICE_UNAVAILABLE`. Property erişilemezse 404, kota tüketilmez. Yanıt `{text (≤500, kontrol karakterleri temizlenmiş), quota}`.
- Metin **soru kutusuna eklenir**; kullanıcı görüp düzenler ve mevcut `/chat` akışıyla kendisi gönderir. Transkripsiyon otomatik gönderilmez.
- **Ses ve metin saklanmaz, loglanmaz.** Ses bellekte okunup sağlayıcıya iletilir ve bırakılır. Audit `request_kind=FREE_TEXT`, `tool_key=TRANSCRIBE`, `source=SPEECH` (yönetici günlük kotasını kullanır); migration gerekmedi.
- Ekran: tek anahtar "Sesli yazışma: Açık/Kapalı" (`role=switch`), açıkken mikrofon düğmesi "Dinleniyor 0:12" / "Yazıya çevriliyor…", 60 sn'de otomatik durur. Sağlayıcı yoksa anahtar devre dışı ve neden yazılı. Ayrı anahtar "Cevabı sesli oku" tarayıcının `speechSynthesis`'i ile **cihaz üzerinde** okur. Tercihler yalnız bu tarayıcıda (`localStorage`) tutulur.

### Yapılandırma (`Assistant:Speech`)

| Ayar | Varsayılan | Not |
| --- | --- | --- |
| `BaseUrl` | — | Boşsa sesli yazışma kapalı. Örn. `http://127.0.0.1:8178` |
| `Path` | `/v1/audio/transcriptions` | OpenAI uyumlu yerel sunucu; whisper.cpp `server` için `/inference` |
| `Model` | — | İsteğe bağlı (`model` alanı) |
| `ApiKey` | — | İsteğe bağlı; yalnız env/user-secrets (`Assistant__Speech__ApiKey`) |
| `TimeoutSeconds` | 60 | 5–180 |

Gönderilen alanlar: `file`, `language`, `response_format=json`, isteğe bağlı `model`; beklenen yanıt `{ "text": "..." }`. AkaAI'de (yerel) ses/transkripsiyon ucu bulunmadığı için AkaAI kullanılmadı.

### Yerel whisper.cpp çalıştırma (geliştirme makinesi, kalıcı servis yok)

```bash
brew install whisper.cpp
mkdir -p ~/Desktop/Projects/whisper-models
curl -L -o ~/Desktop/Projects/whisper-models/ggml-large-v3-turbo-q5_0.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q5_0.bin
shasum -a 256 ~/Desktop/Projects/whisper-models/ggml-large-v3-turbo-q5_0.bin   # 394221709cd5ad1f40c46e6031ca61bce88931e6e088c188294c6d5a55ffa7e2
whisper-server -m ~/Desktop/Projects/whisper-models/ggml-large-v3-turbo-q5_0.bin --host 127.0.0.1 --port 8178 -l tr   # Ctrl+C ile durur
```

`.env` (repoya girmez): `Assistant__Speech__BaseUrl=http://127.0.0.1:8178` ve `Assistant__Speech__Path=/inference`; ardından API yeniden başlatılır. Sunucu yalnız 127.0.0.1'i dinler. Tarayıcı kaydı cihazda 16 kHz mono WAV'a çevrilerek gönderilir (whisper.cpp ffmpeg'siz WAV okur); ses diske yazılmaz.
