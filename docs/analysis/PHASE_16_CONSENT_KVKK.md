# İŞ16–18 — KVKK aydınlatma, açık rıza ve fotoğraf rızası analizi

16.10.2026 kullanıcı talebiyle salt okunur analiz yapıldı. Kod yazılmadı, build/test çalıştırılmadı. Kapsam üç faza (İŞ16–18) ve 18 numaralı task'a bölündü. Her faz ayrı kullanıcı onayıyla başlar; onay gelmeden kodlama yapılmaz. Tamamlanan her task'ın altına "Yapılanlar" notu (değişen dosyalar, migration, test komutu ve sonucu, kalan sınır) bu belgeye ve TESTING.md'ye yazılır.

Yol haritası notu: İŞ12–15'in kodu yerelde henüz commit edilmemiş (210 değişiklik) ve `milestones.ts` / `messages/*.json` o işte de değişiyor. Çakışma olmasın diye İŞ16–18 task'ları `milestones.ts`'e `planned` olarak **İŞ16 onayı geldiğinde, ilk commit'le** eklenir. İş yol haritası toplamı 106 → 124 görev olur; yüzde yeni kapsam nedeniyle düşer, gerileme değildir.

## Talep özeti

1. Mobilde fotoğraf eklerken (profil fotoğrafı ve talep/arıza fotoğrafı) açık rıza onayı alınmalı; onay yoksa fotoğraf yüklenemez.
2. Merkezde (web) bir **Açık Rıza** menüsü olmalı: metinler yazılır, sürümlenir, kimin neyi onayladığı görülür.
3. KVKK kapsamında **birden fazla** rıza/metin olabilir (aydınlatma metni, profil fotoğrafı rızası, talep fotoğrafı rızası, ileride başkaları).
4. TraceFlow'da geliştirilen açık rıza kodlaması ve ekranları hem merkez (API + web) hem mobil tarafa uyarlanır.

## Mevcut durum (kaynakla doğrulandı)

**BuildingFlow**

- Fotoğraf yükleyen iki akış var, ikisi de **yalnız mobilden**:
  - Profil fotoğrafı: `MobileProfile` / `MobileProfilePhoto` (Domain/MobileProfile.cs), `POST /api/v1/mobile/profile/commands` `photoAction=REPLACE` + `photoBase64` (docs/api/mobile-profile.md). Mobil: `src/components/ProfileEditor.tsx` → `src/profile/picker.ts selectPhoto`.
  - Talep/arıza fotoğrafı: `MobileRequest.PhotoId` / `MobileRequestPhoto` (Domain/MobileRequests.cs), `POST /api/v1/mobile/sync/commands` `PhotoAction=REPLACE` (docs/api/mobile-requests.md). Mobil: `src/components/RequestForm.tsx` → `selectPhoto(camera,false)`.
- Rıza/aydınlatma için hiçbir tablo, uç, ekran yok. `docs/api/mobile-profile.md`: "Hukuki metin/consent endpoint'i yok". Mobil `ProfileEditor.tsx` gizlilik penceresinde "Onaylanmış hukuki metin henüz yayınlanmadı… görüntüleme tercihi hukuki açık rıza kaydı değildir" yazıyor; "Bu cihazda fotoğrafımı göster" anahtarı yalnız cihaz tercihi.
- Merkez web fotoğraf **yüklemiyor**; yalnız talep fotoğrafını gösteriyor (`features/requests/RequestsWorkspace.tsx RequestImage`, SHA256 doğrulamalı). Profil fotoğrafı merkezde hiç görünmüyor.
- Fotoğraf saklama: özel SeaweedFS (S3) `IFileStorage` / `S3FileStorage`; STAGED → CURRENT → RETIRED → PURGED durumları ve `ProfilePhotoMaintenance` arka plan temizliği hazır. Rıza geri çekilince fotoğrafı silmek için bu mekanizma yeniden kullanılabilir.
- Web menüsünde "Yönetim" başlığı altında **Tanımlar / Kullanıcılar / Ayarlar** pasif (planlı) düğmeler var (`components/WorkspaceShell.tsx`); Açık Rıza menüsünün doğal yeri burası.
- Backend'de PDF üretim ve HTML temizleme (sanitizer) kütüphanesi yok. Mobilde HTML gösterecek `react-native-webview` yok. Web'de sanitizer paketi yok.
- Mobil yerel veritabanı şema sürümü 7 (`src/offline/database.ts SCHEMA_VERSION`); İŞ15 v8'i planlamıştı, bu iş bir sonraki sürümü alır.

**TraceFlow (Java/Spring + React web + Expo mobil)** — `services/masterdata-service/.../consent`, `web-next/src/pages/consent-texts`, `mobile/src/components/profile/consent-*`, analiz `docs/design/profile-photo-consent/analiz.md`

- `consent_text`: tür + sürüm numarası + başlık + HTML gövde; her yayın yeni ve **değişmez** satır (v1, v2…), (tür, sürüm) benzersiz.
- `user_consent`: kullanıcı, tür, onaylanan metin id/sürüm, onaylayan adı, onay zamanı, geri çekme zamanı/nedeni (`WITHDRAWN` / `SUPERSEDED`), PDF nesne anahtarı/boyutu. Kullanıcı+tür için tek aktif satır (kısmi benzersiz indeks). Geri çekmede satır silinmez, PDF silinir.
- Durum: `NONE` / `ACTIVE` / `OUTDATED` (eski sürümü onaylamış, güncel metni görmemiş).
- Onayda metin + "… tarihinde … tarafından elektronik ortamda okunmuş ve onaylanmıştır" satırı PDF'e basılır, depoya yazılır; işlem geri dönerse PDF silinir, geri çekmede commit sonrası silinir.
- HTML jsoup ile temizlenir: script, img, stil yok; yalnız `https`/`mailto` bağlantı (PDF üretiminde SSRF ve istemcide script engeli).
- Kurallar: rıza yoksa veya metin eskiyse fotoğraf yüklenemez (`PHOTO_CONSENT_REQUIRED`); merkez, mobil kullanıcı adına fotoğraf yükleyemez (`AVATAR_SELF_ONLY`); kapsam dışı yönetici rıza ve fotoğrafı göremez.
- Web: Ayarlar → Açık Rıza Metinleri (sürüm geçmişi, yeni sürüm yayın diyaloğu, sandbox iframe önizleme), kullanıcı formunda rıza durumu + PDF indirme.
- Mobil: fotoğraf eklemeden önce tam ekran metin + "okudum, onaylıyorum" kutusu; metin revize edilmişse uygulama açılışında güncel metin kapı ekranı (onay ya da geri çekme).
- **Bilinen eksikleri:** tek tür (`PROFILE_PHOTO`, enum + DB check), metin sistem geneli, çevrimdışı rıza yok, aydınlatma metni ayrı değil, idempotency anahtarı yok.

## Eksik / hatalı / riskli noktalar ve kararlar

1. **Birden fazla rıza (karar önerisi):** TraceFlow'daki tek enum yerine **rıza türü kataloğu** (tablo). Sistem türleri seed edilir ve silinemez, kodda bir zorunluluk noktasına bağlıdır: `KVKK_INFORMATION` (aydınlatma), `PROFILE_PHOTO`, `REQUEST_PHOTO`. Yönetici yeni **serbest** tür ekleyebilir (ör. "Kamera kaydı bilgilendirmesi", "Ticari ileti izni"); serbest türler mobilde "Rızalarım" listesinde gönüllü onay/geri çekme olarak görünür, bir özelliği kilitlemez. Yeni bir özelliği kilitleyen tür ise kod değişikliği gerektirir.
2. **Aydınlatma ≠ açık rıza:** KVKK uygulamasında aydınlatma metni ile açık rıza metni ayrı sunulmalı, açık rıza aydınlatmanın içine gömülmemeli. Bu yüzden tür `kind` alanı taşır: `INFORMATION` (okundu bilgisi, geri çekilemez, uygulamayı kilitlemez) ve `EXPLICIT_CONSENT` (özgür iradeyle verilir, her an geri çekilebilir). Fotoğraf rızası reddedilirse kullanıcı fotoğrafsız devam eder; hizmet rızaya bağlanmaz.
3. **Metin kapsamı (KARAR, 16.10.2026):** metinler **sistem geneli ve ortaktır** (SUPER_ADMIN yazar, veri sorumlusu bilgisi metnin içinde). Property bazlı metin yapılmaz.
4. **Değişmezlik ve kanıt:** metin satırı yayından sonra güncellenmez (yalnız pasife alınabilir); gövde SHA-256'sı saklanır. Rıza kaydı onaylanan metin id + sürüm + gövde karması, onaylayan adı, Keycloak `sub`, zaman, kanal (MOBILE/WEB), uygulama sürümü ve işlem UUID'si taşır. IP adresi saklanmaz (gereksiz kişisel veri); istenirse ayrı karar.
5. **Rıza belgesi, PDF kütüphanesi yok (KARAR, 16.10.2026):** kullanıcı kararıyla yeni paket, kütüphane veya font eklenmez (AGENTS.md "Yeni lisans yükü yok" kuralı). TraceFlow'un PDF yaklaşımı (openhtmltopdf + Noto Sans) bu yüzden taşınmaz. Kendi PDF yazıcımız da seçilmedi: Türkçe karakterler (ş, ğ, ı, İ) PDF'in standart fontlarında yok, bunun için font gömmek gerekir, o da font lisansı demektir. **Lisanssız en iyi alternatif:** kanıt veritabanındaki değişmez kayıttır (metin sürümü + gövde SHA-256 + onaylayan + zaman + kanal + işlem UUID). Merkezde her kayıt için kendi kodumuzla üretilen **yazdırılabilir rıza belgesi** sayfası (metin + "… tarihinde … tarafından elektronik ortamda okunmuş ve onaylanmıştır" satırı + kanıt karması, yazdırma CSS'i) bulunur. Gerekirse tarayıcının "PDF olarak kaydet" özelliğiyle PDF alınır. Depoya nesne yazılmadığı için DB ile depo arasında tutarsızlık riski de olmaz.
6. **Metin biçimi, HTML yok (KARAR, 16.10.2026):** HTML temizleyici paketi (HtmlSanitizer/jsoup) ve mobil `react-native-webview` eklenmez. Metin, projeye ait **kısıtlı bir biçimle** yazılır: `# ` / `## ` başlık, boş satırla paragraf, `- ` madde, `1. ` numaralı madde, `**kalın**`, `[metin](https://…)` ve `mailto:` bağlantı. Sunucu (C#) biçimi doğrular (boyut, satır, bağlantı şeması, bilinmeyen sözdizimi düz metin sayılır). Web (React öğeleri) ve mobil (RN `Text`) kendi küçük çeviricileriyle gösterir. Çevirici hiçbir zaman ham HTML üretmez; tüm metin kaçışlıdır. Bu sayede script/XSS ve SSRF riski yapısal olarak ortadan kalkar. Üç çevirici aynı test vektörlerini kullanır.
7. **Zorunluluk sunucuda:** profil `REPLACE` için `PROFILE_PHOTO`, talep `REPLACE` için `REQUEST_PHOTO` rızası **ACTIVE** (güncel sürüm) olmalı; değilse `BF_CONSENT_REQUIRED` (403). `KEEP` ve `REMOVE` rıza istemez (kullanıcı her zaman silebilmeli). Mobil kontrol yalnız deneyimdir.
8. **Metin revizyonu:** yeni sürüm yayınlanınca aktif kayıtlar `OUTDATED` görünür; yeni fotoğraf yüklenemez, mevcut fotoğraf kalır. Mobil açılışta güncel metin kapı ekranı: onaylarsa eski satır `SUPERSEDED` olur, yeni satır açılır; reddederse rıza geri çekilir. Yayın formunda "Bu sürüm yeniden onay gerektirir" seçeneği; yalnız yazım düzeltmesi ise eski onaylar `ACTIVE` kalır (karar önerisi, varsayılan: yeniden onay gerekir).
9. **Geri çekme ve rıza geçmişi (KARAR, 16.10.2026):** her onay, yenileme ve geri çekme ayrı ve silinmez bir satır olarak loglanır ("v2'yi 16.10'da verdi, 20.10'da geri aldı"). Bu geçmişi **yalnız merkez ve sistem görür**. Kişi mobilde yalnız güncel durumu (onaylı / onaysız / güncelleme bekliyor) görür, geçmiş listesini göremez. Profil fotoğrafı geri çekmede kaldırılır (pointer null, `RETIRED`, bakım işi PURGED); zaten merkezde gösterilmiyor. **Talep fotoğrafları silinmez.** Kişinin mobil görünümünden kalkar, merkez görmeye devam eder ve fotoğrafın yanında "rıza 20.10.2026'da geri çekildi" bilgisi görünür. Hukuki not: rıza geri çekildikten sonra fotoğrafı saklamak için başka bir hukuki sebep gerekir (ör. arıza kaydının ispatı/meşru menfaat). Bu sebep aydınlatma metninde yazmalıdır, aksi halde fotoğraf silinmelidir.
10. **Çevrimdışı (BuildingFlow'a özgü, TraceFlow'da yok):** mobil güncel metinleri şifreli önbellekte tutar. Çevrimdışıyken kullanıcı önbellekteki metni onaylarsa rıza komutu (işlem UUID, textId, gövde karması) kuyruğa girer ve **fotoğraf komutundan önce** gönderilir. Sunucu metin değişmişse rızayı `BF_CONSENT_TEXT_OUTDATED` (409) ile reddeder, fotoğraf kuyrukta bekler, kullanıcıya güncel metin gösterilir. Hiç metin önbelleğe alınmamışsa çevrimdışı fotoğraf eklenemez, nedeni ekranda yazar.
11. **Yetki ve kapsam:** metin/tür yönetimi SUPER_ADMIN. Rıza kayıtlarını görme: SUPER_ADMIN tümü; SITE_ADMIN/MANAGER yalnız kendi aktif Property üyelerine bağlı kullanıcılar (İŞ14'teki sızıntı kuralıyla aynı). ACCOUNTANT görmez. Merkez kullanıcı adına rıza veremez ve fotoğraf yükleyemez (`BF_CONSENT_SELF_ONLY`). Talep fotoğrafını gören yönetici rıza durumunu da görür.
12. **Audit:** yayın, tür değişikliği, onay, geri çekme, başarısız deneme ve PDF indirme güvenli olay olarak loglanır (kişi adı/telefon/HTML gövde loga yazılmaz; yalnız kayıt id, tür kodu, sürüm, sonuç). Telemetri etiketleri AGENTS.md kuralına uyar.
13. **Uygulama hesabı olmayan kişiler:** kâğıt üzerinde ıslak imzalı rızayı merkezden tarayıp kişiye bağlama bu kapsamda **yok**; açık nokta olarak kalır.
14. **Saklama süresi:** geri çekilmiş/eskimiş rıza satırlarının ne kadar tutulacağı hukuki karardır; bu iş satırları silmez, süre tanımı açık nokta.
15. **BaseEntity / migration kuralları:** üç yeni tablo da BaseEntity (id, version, audit, is_deleted, is_enabled) ve ortak `BaseEntityConfiguration.Configure`; EF migration + snapshot, dolu tabloda Down reddi, şema sözleşme testleri ayrı PostgreSQL'de.

## Veri modeli (öneri)

| Tablo | Alanlar (BaseEntity alanlarına ek) | Kısıtlar |
|---|---|---|
| `consent_types` | `code` (A–Z, _, max40), `name_tr`, `name_en`, `kind` INFORMATION/EXPLICIT_CONSENT, `is_system`, `feature` (PROFILE_PHOTO/REQUEST_PHOTO/null), `sort_order` | `code` benzersiz; sistem türü silinemez/kodu değişmez |
| `consent_texts` | `consent_type_id`, `text_version`, `title` (max200), `body` (kısıtlı biçim, max50.000), `body_sha256`, `requires_reconsent`, `published_at`, `published_by` | (tür, sürüm) benzersiz; sürüm>0; yayından sonra içerik değişmez |
| `user_consents` | `owner_user_id` (sub, max200), `approver_name`, `consent_type_id`, `consent_text_id`, `text_version`, `body_sha256`, `approved_at`, `revoked_at`, `revoke_reason` WITHDRAWN/SUPERSEDED, `channel`, `app_version`, `operation_id`, `operation_fingerprint`, `evidence_sha256` (metin karması + onaylayan + zaman + işlem UUID üzerinden; depo nesnesi yok) | sahip+tür için tek aktif (kısmi benzersiz, `revoked_at IS NULL`); sahip+işlem UUID benzersiz; (revoked_at null) ⇔ (revoke_reason null); composite FK tür/metin uyumu |

## Faz ve task planı

Task numarası `T<faz>.<sıra>`; kodlama task numarasıyla istenebilir. Önerilen sıra İŞ16 → İŞ17 → İŞ18; İŞ18 mobil ekranları İŞ17 uçlarına bağlıdır. Her faz ayrı onayla başlar.

### İŞ16 [RIZA-MERKEZ] — Rıza altyapısı ve merkez Açık Rıza menüsü

| Task | İş | Dosyalar (öngörülen) | Kabul kriteri | Bağımlılık |
|---|---|---|---|---|
| T16.1 | **Veri modeli ve migration** — `ConsentType`, `ConsentText`, `UserConsent` entity'leri; üç sistem türü seed; kısmi benzersiz indeks ve check'ler. | Domain/Consents.cs, Infrastructure/ConsentConfiguration.cs, BuildingFlowDbContext.cs, Migrations/*_Consents.cs + snapshot | Migration ayrı PG'de ilk kurulum/yükseltme/tekrar geçer; şema sözleşme testi üç tabloyu BaseEntity kuralıyla kabul eder; aynı kullanıcı+türde ikinci aktif satır DB'de reddedilir | — |
| T16.2 | **Kısıtlı metin biçimi ve sürümleme servisi** — paket kullanmadan projeye ait C# biçim doğrulayıcı (başlık, paragraf, madde, kalın, https/mailto bağlantı); gövde karması; yeni sürüm = yeni satır, eski sürüm değişmez. Ortak test vektörleri dosyası. | Application/Consents.cs, Application/ConsentMarkup.cs, docs/api/consent-markup-vectors.json | `<script>`, HTML etiketleri ve `javascript:` bağlantı düz metin olarak kalır/reddedilir (birim testleri); boş metin `BF_CONSENT_TEXT_INVALID`; aynı UUID ile tekrar yayın tek satır üretir; yeni paket yok (csproj değişmez) | 16.1 |
| T16.3 | **Rıza kanıtı ve yazdırılabilir belge** — onayda `evidence_sha256` hesaplanır; merkez için kendi kodumuzla yazdırılabilir rıza belgesi (metin + onay satırı + kanıt karması, print CSS). PDF kütüphanesi ve depo nesnesi yok. | Application/ConsentEvidence.cs, web features/consents/ConsentDocument.tsx | Belgede metin onaylanan sürümle birebir aynı; karma yeniden hesaplanınca eşleşir; tarayıcıdan "PDF olarak kaydet" Türkçe karakterleri doğru basar; yeni paket/font yok | 16.2 |
| T16.4 | **Merkez rıza API'si** — tür listesi/ekle/düzenle/pasife al; metin sürüm geçmişi, güncel metin, yayın; rıza kayıtları sayfalı liste (Property, tür, durum, arama), kişi bazlı **rıza geçmişi** (verdi/yeniledi/geri aldı), belge verisi. ErrorCatalog kodları. | Api/ConsentEndpoints.cs, Application/ErrorCatalog.cs, docs/api/consents.md, FE error-codes + TR/EN | SUPER_ADMIN yazar; SITE_ADMIN/MANAGER yalnız kendi Property üyelerinin kayıtlarını görür, başka site 403/404; ACCOUNTANT 403; anonim 401; sistem türü silinemez 409 | 16.1–16.3 |
| T16.5 | **Web Açık Rıza menüsü** — Yönetim → "Açık Rıza" (pasif "Ayarlar" yerine/altında). Sekmeler: **Rıza türleri**, **Metinler** (sürüm geçmişi, yeni sürüm diyaloğu: başlık + metin alanı + biçim yardımı + kendi çeviricimizle canlı önizleme + "yeniden onay gerekir"), **Rıza kayıtları** (TanStack tablo, filtre, durum rozeti NONE/ACTIVE/OUTDATED/WITHDRAWN, kişi geçmişi zaman çizelgesi, yazdırılabilir belge). Mevcut shadcn/ui ve ikonlar, yeni paket yok, TR/EN, 390 px mobil görünüm. | app/(workspace)/consents/, features/consents/ (ConsentMarkup.tsx dahil), WorkspaceShell.tsx, messages/tr.json + en.json | Yayın sonrası liste yenilenir; önizleme ham HTML basmaz (test vektörleri); package.json değişmez; TR/EN tam; 1280 px ve 390 px ekran görüntüsü; FE testleri geçer | 16.4 |
| T16.6 | **Testler ve kabul** — gerçek ayrı PostgreSQL integration, bağımlılık dosyalarının (csproj/package.json/lock) değişmediği kontrolü, negatif yetki/kapsam, model/migration eşliği, FE test/typecheck/build, lisans; TESTING.md, STATUS, CHANGELOG, roadmap tiki. | backend/tests/…/ConsentApiTests.cs, docs/TESTING.md | Tüm testler 0 hata; kullanıcı DB'sinde gerçek kayıt yazılmadan kabul | 16.1–16.5 |

### İŞ17 [RIZA-FOTO] — Fotoğrafta rıza zorunluluğu ve kullanıcı rıza API'si

| Task | İş | Dosyalar (öngörülen) | Kabul kriteri | Bağımlılık |
|---|---|---|---|---|
| T17.1 | **Mobil rıza API'si** — `GET /api/v1/mobile/consents` (tüm aktif türler: durum, güncel metin, onaylanan sürüm); `POST /api/v1/mobile/consents/commands` (operationId, APPROVE/WITHDRAW, typeCode, textId, bodySha256) idempotent. Mobil uç **geçmiş döndürmez**, yalnız güncel durum ve onaylanan metin. | Api/MobileConsentEndpoints.cs, Application/MobileConsents.cs, docs/api/mobile-consents.md | Aynı UUID tekrarı aynı sonucu, farklı içerik 409; eski textId 409 `BF_CONSENT_TEXT_OUTDATED`; INFORMATION türü geri çekilemez 409; yanıt şemasında geçmiş alanı yok (test) | 16.4 |
| T17.2 | **Profil fotoğrafında zorunluluk** — `REPLACE` için ACTIVE `PROFILE_PHOTO`; değilse 403 `BF_CONSENT_REQUIRED`. KEEP/REMOVE serbest. | Application/MobileProfile.cs, MobileProfileStore.cs | Rızasız, eski sürümlü, geri çekilmiş rıza ile yükleme reddedilir; KEEP/REMOVE çalışır; mevcut 27 profil testi bozulmaz | 17.1 |
| T17.3 | **Talep fotoğrafında zorunluluk** — sync `REPLACE` için ACTIVE `REQUEST_PHOTO`; aynı kurallar. | Application/MobileSync.cs, MobileSyncStore.cs | Rızasız talep fotoğrafı reddedilir, talebin kendisi fotoğrafsız kaydedilebilir; fingerprint/replay sözleşmesi korunur | 17.1 |
| T17.4 | **Geri çekme sonuçları** — tek transaction: rıza WITHDRAWN (satır kalır, geçmişe eklenir); profil pointer null + RETIRED, bakım işi PURGED; talep fotoğrafları **silinmez**, sahibinin mobil sorgularından gizlenir, merkez görmeye devam eder. | MobileConsents.cs, ProfilePhotoMaintenance.cs, MobileSyncStore.cs, RequestManagementStore.cs | Geri çekmeden sonra sahibi profil ve talep fotoğrafını alamaz (404); yönetici talep fotoğrafını görür; rıza satırları silinmez; bakım sonrası profil nesnesi depoda yok (fixture) | 17.2, 17.3 |
| T17.5 | **Merkezde rıza görünürlüğü** — kişi detayı / kullanıcı ekranında bağlı hesabın rıza durumları, geçmişi ve belgesi; talep detayında fotoğraf yanında "fotoğraf rızası v3 · 16.10.2026" ya da "rıza 20.10.2026'da geri çekildi". Merkez adına yükleme yok. | features/people, features/users, features/requests, messages | Kapsam dışı yönetici göremez; TR/EN; ekran görüntüsü | 17.1 |
| T17.6 | **Testler ve kabul** — integration (rıza yok/eski/geri çekilmiş × profil/talep), yetki, idempotency, depo temizliği; TESTING ve API belgeleri. | ConsentEnforcementTests.cs, docs | 0 hata; önceki profil/talep testleri geçer | 17.1–17.5 |

### İŞ18 [RIZA-MOBİL] — Mobil rıza ekranları

| Task | İş | Dosyalar (öngörülen) | Kabul kriteri | Bağımlılık |
|---|---|---|---|---|
| T18.1 | **Şifreli önbellek ve kuyruk (paketsiz)** — yeni paket yok (lisans kararı 16.10.2026): WebView yerine projeye ait ayrıştırıcı; SQLite şema 8'de rıza önbelleği, rıza kuyruğu ve görüldü tabloları. | src/offline/database.ts, src/consents/{contracts,store}.ts | Migration eski veriyi korur; çıkışta rıza önbelleği silinir; package.json/lock değişmez | 17.1 |
| T18.2 | **Rıza ekranı (ConsentSheet)** — tam ekran metin, projeye ait kısıtlı biçim çizicisiyle RN `Text` (WebView/HTML/script yok; bağlantı onaylı açılır), "okudum" kutusu işaretlenmeden onay pasif; Onayla / Vazgeç; erişilebilirlik etiketleri. TraceFlow `consent-sheet.tsx` uyarlaması. | src/consents/{markup.ts, MarkupView.tsx, ConsentSheet.tsx} | Kutu işaretsizken onay yok; ortak 16 vektör birebir; ekran okuyucu etiketleri | 18.1 |
| T18.3 | **Fotoğraf eklemeden önce rıza** — ProfileEditor ve RequestForm'da "Galeriden seç / Fotoğraf çek" önce rıza durumunu kontrol eder; yoksa/eskiyse ConsentSheet açılır, onaydan sonra seçici açılır. Reddedilirse fotoğrafsız devam. Gizlilik penceresindeki "metin yayınlanmadı" yazısı gerçek durumla değiştirilir. | ProfileEditor.tsx, RequestForm.tsx, profile/picker.ts | Rızasız seçici açılmaz; onay sonrası tek dokunuşla devam; ret fotoğrafsız kayda izin verir | 18.2 |
| T18.4 | **Çevrimdışı rıza kuyruğu ve sıralama** — rıza komutu fotoğraf komutundan önce gönderilir; 409 eski metin gelirse fotoğraf bekletilir ve güncel metin gösterilir; metin önbellekte yoksa çevrimdışı fotoğraf eklenemez uyarısı. | src/consents/outbox.ts, sync/engine.ts, profile/engine.ts | Uçak modunda onay + fotoğraf → bağlantı gelince sırayla gider; eski metin senaryosu kullanıcıya yeniden sorulur | 18.3 |
| T18.5 | **Açılış kapısı ve Rızalarım** — girişte INFORMATION türü ilk kez/revizyonda gösterilir (okundu kaydı); OUTDATED açık rıza için kapı ekranı (onayla / geri çek). Profil altında "Rızalarım": tüm türler, durum, sürüm, tarih, PDF görüntüle, geri çek (onaylı uyarıyla). | src/consents/ConsentGate.tsx, app/(tabs)/profile, ProfileEditor.tsx | Revizyonda kapı bir kez açılır; geri çekme sonrası fotoğraf cihazdan ve önizlemeden kalkar; serbest türler listede görünür | 18.2, 17.4 |
| T18.6 | **Testler ve kabul** — mobil birim testleri (kuyruk sırası, durum hesap, önbellek), `npm run check`, `npm run build`, iOS/Android cihaz kabulü (çevrimiçi, uçak modu, revizyon, geri çekme); mobile/docs/mobile/PHASE16.md ve PHASE_STATUS. | tests/consents.test.mjs, docs | check/build 0 hata; cihaz kabulü kullanıcıyla yapılır, yapılmadıysa açık kalır | 18.1–18.5 |

## Kalan açık noktalar (kullanıcı kararı)

- Metin kapsamı: sistem geneli (varsayılan) mi, Property bazlı mı? (karar 3)
- Rıza geri çekilince eski talep fotoğrafları silinsin mi (varsayılan evet) yoksa yalnız yeni yüklemeler mi engellensin? (karar 9)
- Yazım düzeltmesi niteliğindeki sürümde yeniden onay istenmemesi seçeneği kalsın mı? (karar 8)
- PDF kütüphanesi PDFsharp/MigraDoc (varsayılan) uygun mu? (karar 5)
- Kâğıt/ıslak imzalı rıza kaydı ve rıza satırı saklama süresi bu kapsamda yok.

## Yapılanlar

Bu bölüm her task tamamlandığında doldurulur: task, tarih, değişen dosyalar, migration, çalıştırılan komutlar ve sonuçları, kalan sınırlar.

### İŞ16 — 16.10.2026 (kullanıcı onayı: "iş16 yı onaylıyorum kodlamaya geçebilirsin")

Ortam: cihaz VM'inde dotnet ve ağ yok; derleme ve testler bulut kapsayıcısında (.NET 10.0.112, ayrı PostgreSQL `buildingflow_test`) çalıştırıldı, sonra dosyalar cihaz klasörüne küçük yamalarla aktarıldı. Aynı klasörde Kişi Refaktörü (CentralPerson) paralel sürüyor; son aktarımda migration cihazın 13:01 durumunun üzerine üretildi ve paylaşılan dosyalara yalnız rıza satırları eklendi. Yeni paket yok: csproj, package.json ve lock dosyaları değişmedi; `scripts/check-licenses.py` 329 paket PASS.

**T16.1 Veri modeli ve migration — TAMAMLANDI.**
- Dosyalar: `Domain/Consents.cs` (ConsentType, ConsentText, UserConsent; enum'lar ConsentKind, ConsentFeature, ConsentChannel, ConsentRevokeReason), `Infrastructure/ConsentConfigurations.cs`, `BuildingFlowDbContext.cs` (3 DbSet), `Migrations/20261016130236_Consents.cs` + Designer + snapshot (yalnız rıza entity'leri).
- Kısıtlar: kod deseni ve silinmiş dahil tekil kod; feature yalnız sistem türünde ve tekil; (tür, sürüm) ve (tür, operationId) tekil; (text, type) composite FK; kullanıcı+tür başına tek aktif satır kısmi indeksi; hash/tarih/neden check'leri. Seed: KVKK_INFORMATION, PROFILE_PHOTO, REQUEST_PHOTO sabit id ile. Down veri varsa reddeder.
- Kanıt: tüm şema sözleşme testleri (BaseEntity sekiz alan) ve model/migration eşliği geçti; DB ikinci aktif satırı ve başka türün metnini reddediyor (ConsentApiTests).

**T16.2 Kısıtlı metin biçimi ve sürümleme — TAMAMLANDI.**
- Dosyalar: `Application/ConsentMarkup.cs` (projeye ait ayrıştırıcı; HTML yok), `docs/api/consent-markup-vectors.json` (16 ortak vektör), `Application/Consents.cs` (ConsentService, yayın doğrulaması).
- Davranış: başlık/paragraf/madde/numaralı madde/kalın/https-mailto bağlantı; HTML düz metin kalır; `javascript:`/http/kullanıcı bilgili bağlantı LINK hatası; kontrol, bidi ve bozuk surrogate karakterleri CONTROL_CHAR. Kaynak kodda görünmez karakter bırakılmadı (`\u200c`, `\u202e` kaçışları).
- Sürümleme: her yayın yeni satır; aynı operationId + içerik replay, farklı içerik 409; eşzamanlı iki yayın tür kilidiyle 1 ve 2 numarayı alır.

**T16.3 Rıza kanıtı ve yazdırılabilir belge — TAMAMLANDI.**
- PDF kütüphanesi yerine (lisans kararı) yazdırılabilir sayfa: `web features/consents/ConsentDocumentPage.tsx`, rota `/consents/records/{id}`, `globals.css` içinde print kuralı (menü/üst/alt çubuk gizlenir).
- `ConsentEvidence.Compute` "consent-v1" özeti; belge ucu metin ve kanıt özetini her okumada yeniden hesaplar (`bodyIntact`, `evidenceIntact`). Kurcalanmış satırda `evidenceIntact = false` ve sayfada kırmızı uyarı (test + ekran görüntüsü).

**T16.4 Merkez rıza API'si — TAMAMLANDI.**
- Dosyalar: `Api/ConsentEndpoints.cs`, `Infrastructure/ConsentStore.cs`, `Program.cs` (servis + uç kaydı), `DependencyInjection.cs`, `ErrorCatalog.cs` (8 BF_CONSENT_* kodu), web `services/error-codes.ts` + `messages/*.json apiErrors`, `docs/api/consents.md`.
- Yetki: okuma SUPER_ADMIN/SITE_ADMIN/MANAGER (SITE_ADMIN/MANAGER yalnız kendi aktif Property üyeleri, başka kişi 404), yazma yalnız SUPER_ADMIN; ACCOUNTANT/RESIDENT 403, anonim 401; sistem türü silinemez/pasife alınamaz 409.
- Komut: `dotnet test tests/BuildingFlow.IntegrationTests --filter ConsentApiTests` → 18/18; `dotnet test tests/BuildingFlow.UnitTests --filter Consent` → 20/20.

**T16.5 Web Açık Rıza menüsü — TAMAMLANDI.**
- Dosyalar: `app/(workspace)/consents/page.tsx`, `consents/records/[recordId]/page.tsx`, `features/consents/{markup.ts, model.ts, ConsentMarkupView.tsx, ConsentsWorkspace.tsx, ConsentDocumentPage.tsx}`, `components/WorkspaceShell.tsx` (Yönetim → Açık Rıza, arama listesi), `auth/return-path.ts` (/consents ve belge rotası), `messages/tr.json`/`en.json` (`consents` ad alanı, `ui.navConsents`), `tests/consents.test.mjs`.
- Ekran: Rıza türleri (ekle/düzenle/sil, sistem rozeti), Metinler (tür seçimi, sürüm geçmişi, görüntüle, yayın diyaloğu: başlık, metin, biçim yardımı, canlı önizleme, "yeniden onay gerekir", aynı içerikte aynı operationId), Onay kayıtları (arama, tür/durum/alan filtresi, durum rozetleri, kişi geçmişi zaman çizelgesi, belge bağlantısı). Mevcut shadcn/ui, RecordList (TanStack), ManagementPanel ve lucide ikonları; yeni paket yok.
- Kanıt: `npm test` 116/116 (ortak 16 vektör web ayrıştırıcısında birebir, DTO doğrulayıcıları, dönüş yolu, TR/EN anahtar eşleşmesi, hata kodu eşleşmesi), `npm run typecheck` ve `npm run build` temiz. Tarayıcı kabulü: üretim derlemesi + Playwright (Keycloak ve API taklitli, kullanıcı veritabanına yazılmadı) 1280×900 ve 390×844'te altı ekran, yatay taşma 0, menü bağlantısı aria-current=page, yazdırma görünümü. Görüntüler: `/mnt/project-files/analysis/is16-screens/`.

**T16.6 Testler ve kabul — AÇIK.**
- Cihaz klasörünün 13:01 durumu (Kişi Refaktörü migration'ları dahil) + İŞ16 ile kapsayıcıda: tüm backend 170/170 birim, 489/490 entegrasyon; model/migration eşliği temiz. Tek hata `MobileNoticeTests.ConcurrentReadIsIdempotentAndVisibleToAnotherDevice` (eşzamanlı okumada ReadAt mikrosaniye yuvarlama farkı); rıza değişiklikleri geri alınınca da aynı şekilde kalıyor, ayrı izlenmeli.
- Not: kapsayıcı test veritabanında paralel fixture'lar `citext` uzantısını aynı anda kurmaya çalışıp çakıştı; uzantı test veritabanına önceden kuruldu (kod değişikliği değil).
- Migration ilk olarak 20261016123113 adıyla üretildi; aktarım sırasında cihazda Kişi Refaktörü'nün 20261016122659 ve 20261016124132 migration'ları belirdiği için eski dosya kullanılmadı, migration cihazın güncel durumu üzerine 20261016130236_Consents olarak yeniden üretildi (yapı aynı, seed ve Down koruması korundu).
- Açık kalan: gerçek Keycloak oturumuyla kısa tarayıcı kontrolü. (MobileNoticeTests hatası 17.10.2026 BAKIM ile düzeltildi; bkz. TESTING.md.) Cihaz VM'inde dotnet olmadığından suite cihaz klasörünün kopyasında kapsayıcıda çalıştırıldı.

### İŞ17 — 16.10.2026 (kullanıcı onayı: "sonrasında iş17 ye kodlamaya geçebilirsin")

Ortam: İŞ16 ile aynı yöntem. Cihaz klasörünün güncel kopyası kapsayıcıda derlendi ve test edildi (.NET 10, ayrı PostgreSQL `buildingflow_test`); depolama testleri için kapsayıcıda geçici bir SeaweedFS S3 sunucusu çalıştırıldı, böylece İŞ16'da atlanan fotoğraf/depolama testleri de koştu. Yeni paket, tablo veya migration yok; csproj, package.json ve lock dosyaları değişmedi; `scripts/check-licenses.py` 329 PASS.

**T17.1 Mobil rıza API'si — TAMAMLANDI.**
- Dosyalar: `Application/MobileConsents.cs` (DTO'lar, `IMobileConsentStore`, kodlu 403 için `CodedForbiddenException`), `Infrastructure/MobileConsentStore.cs`, `Api/MobileConsentEndpoints.cs`, `Program.cs`, `DependencyInjection.cs`, `Api/ExceptionHandler.cs` (kodlu 403), `ErrorCatalog.cs` (+3 kod: `BF_CONSENT_REQUIRED`, `BF_CONSENT_TEXT_OUTDATED`, `BF_CONSENT_NOT_WITHDRAWABLE`), web `services/error-codes.ts` + `messages/*.json apiErrors`, `docs/api/mobile-consents.md`.
- `GET /api/v1/mobile/consents`: etkin türler, durum (NONE/ACTIVE/OUTDATED/WITHDRAWN), onaylanan sürüm/tarih ve güncel metin. Geçmiş, geri çekme tarihi veya satır kimliği dönmez (test alan adlarını tarar).
- `POST /api/v1/mobile/consents/commands`: APPROVE yalnız en son metni ve aynı gövde karmasını kabul eder (aksi 409 `BF_CONSENT_TEXT_OUTDATED`); eski sürüm onayı aynı transaction'da SUPERSEDED olur; aynı metin tekrar onaylanırsa yeni satır açılmaz. WITHDRAW aydınlatma türünde 409, aktif onay yoksa değişiklik yapmadan başarılı. Tekrar güvenliği mevcut `mobile_operation_receipts` ile (aynı UUID aynı sonuç, farklı içerik 409 `BF_CONSENT_OPERATION_CONFLICT`). Onaylayan adı mobil profildeki ad soyad, kanal MOBILE, İŞ16 kanıt özeti mühürlenir.

**T17.2 Profil fotoğrafında zorunluluk — TAMAMLANDI.**
- Dosyalar: `Infrastructure/ConsentGate.cs` (ortak ACTIVE kontrolü, `consent:{user}` kilidi, sahibe görünürlük filtresi), `Infrastructure/MobileProfileStore.cs`.
- REPLACE için ACTIVE `PROFILE_PHOTO` gerekir; kontrol depolamaya yazmadan önce ve transaction içinde kilitle iki kez yapılır. Rızasız istekte STAGED satır ve nesne oluşmaz (test). KEEP/REMOVE serbest; tamamlanmış işlemin tekrarı önceki sonucu döner.

**T17.3 Talep fotoğrafında zorunluluk — TAMAMLANDI.**
- Dosya: `Infrastructure/MobileSyncStore.cs`. Sync REPLACE için ACTIVE `REQUEST_PHOTO`; aynı iki aşamalı kontrol. Talep fotoğrafsız kaydedilebilir; parmak izi ve eski makbuz uyumu değişmedi (mevcut legacy replay testi geçti).

**T17.4 Geri çekme sonuçları — TAMAMLANDI.**
- Profil: geri çekme ile aynı transaction'da profil işaretçisi boşalır, fotoğraf RETIRED, profil sürümü artar; `ProfilePhotoMaintenance` nesneyi siler ve satırı PURGED yapar (test depoda nesne kalmadığını doğrular). Kilit sırası profil yazımıyla aynı (profil kilidi, sonra rıza kilidi).
- Talep: fotoğraf satırı ve nesnesi silinmez. Geri çekme anına kadar CURRENT olmuş fotoğraflar sahibinin çekiş, push sonucu ve indirme uçlarından gizlenir (404 / photoId null); merkez ve mobil yönetici görünümü görmeye devam eder. Yeniden onay sonrası yüklenen yeni fotoğraf görünür. İŞ17 öncesi yüklenmiş fotoğraflar (rıza kaydı yok) görünür kalır, yalnız geri çekme gizler.
- Rıza satırları silinmez: verdi → yeniledi (SUPERSEDED) → geri aldı (WITHDRAWN) merkezde görünür.

**T17.5 Merkezde rıza görünürlüğü — TAMAMLANDI.**
- Backend: `Application/RequestManagement.cs` + `Infrastructure/RequestManagementStore.cs` talep detayına `photoConsent` (GRANTED/WITHDRAWN/NONE, rıza kaydı, sürüm, onay ve geri çekme tarihi).
- Web: `features/consents/PersonConsents.tsx` (kişi sayfasında bağlı hesabın tür başına durumu, İŞ16 geçmiş penceresi; bağlı hesap yoksa ve kapsam dışıysa açıklama), `features/people/PersonPage.tsx`, `features/consents/ConsentsWorkspace.tsx` (geçmiş penceresi dışa açıldı), `features/requests/{model.ts, RequestsWorkspace.tsx}` (fotoğraf altında rıza notu ve belge bağlantısı, DTO doğrulaması), `messages/tr.json`/`en.json`.
- Merkez adına rıza verme veya fotoğraf yükleme ucu yok. Kapsam dışı yönetici kişi rızasını göremez (İŞ16 404 kuralı).
- Kanıt: `npm test` 117/117 (yeni fotoğraf rızası çözücü testi), typecheck ve build temiz; Playwright 1280 ve 390 px ekran görüntüleri `/mnt/project-files/analysis/is17-screens/`.

**T17.6 Testler ve kabul — AÇIK.**
- `ConsentEnforcementTests` 6/6; mevcut fotoğraf testleri (profil 1, talep 3) önce rıza verecek şekilde güncellendi ve geçti. Tüm backend 170/170 birim, 506/507 entegrasyon; tek hata İŞ16'dan beri bilinen `MobileNoticeTests` (rızayla ilgisiz).
- Not: testler aynı fixture şemasını paylaştığı için sürüm numarası varsayan test kendi rıza türünü oluşturur.
- Açık kalan: gerçek Keycloak oturumuyla merkezde kısa kontrol, İŞ18 sonrası cihazda uçtan uca kabul, MobileNoticeTests hatası.
- Yayın etkisi: kullanıcı mobilden onay verene kadar fotoğraf yüklenemez. Mobil rıza ekranları İŞ18'de; İŞ17 tek başına yayınlanırsa mobil uygulamadaki fotoğraf ekleme 403 alır.

**Ek — varsayılan metinler (16.10.2026, kullanıcı talebi).** Migration `20261016143930_DefaultConsentTexts` KVKK aydınlatma, profil fotoğrafı ve talep fotoğrafı türlerine özgün taslak v1 metin ekler; türün metni varsa dokunmaz. Metinler [default-consent-texts.md](../api/default-consent-texts.md); hukuki gözden geçirme sonrası merkezden yeni sürümle değiştirilir. Karmalar migration'da sabit, `DefaultConsentTextTests` doğrular (1/1); tüm backend 170/170 birim, 507/508 entegrasyon (bilinen MobileNotice hatası).

### İŞ18 — 16.10.2026 (kullanıcı onayı: "diğer faza geçebilirsin")

Ortam: mobil kod kapsayıcıda cihaz klasörünün mobil ağacıyla karma eşleşen kopyasında geliştirildi (`npm run check`, `npm run build`), sonra cihaz klasörüne yama ile aktarıldı. Cihazdaki Kişi Refaktörü değişikliklerine dokunulmadı. **Yeni paket yok:** package.json ve package-lock.json değişmedi; WebView yerine metin projeye ait ayrıştırıcıyla çizilir; karma `expo-crypto` (zaten kurulu) ile alınır; `scripts/check-licenses.py` 329 PASS, dosya değişikliği yok.

**T18.1 Şifreli önbellek ve kuyruk — TAMAMLANDI.**
- Dosyalar: `src/offline/database.ts` (SCHEMA_VERSION 8: `consent_cache`, `consent_jobs`, `consent_seen`, `consent_jobs_due` indeksi), `src/consents/contracts.ts` (sunucu yanıtı doğrulayıcıları: hesap eşleşmesi, tür/durum tutarlılığı, metin–durum eşleşmesi, makbuz–komut eşleşmesi; `wire` gönderim gövdesi), `src/consents/store.ts`.
- Önbellek hesaba bağlı şifreli SQLite'ta; çıkışta veritabanı anahtarı yok edildiği için rıza önbelleği ve kuyruğu da gider. Metin gövdesi önbelleğe alınmadan ve onaya sunulmadan önce SHA-256 karması sunucunun `bodySha256` değeriyle karşılaştırılır; uyuşmayan liste reddedilir.
- Şema 7 → 8 yükseltmesi mevcut talep/profil/bildirim verisini korur; 9 reddedilir (offline/notices/owner testleri güncellendi).

**T18.2 Rıza ekranı — TAMAMLANDI.**
- Dosyalar: `src/consents/markup.ts` (İŞ16 ayrıştırıcısının mobil kopyası; görünmez karakter denetimi Hermes'te `\p{}` gerektirmeyen açık Cc/Cf tablosuyla, `safeHref` `URL` kullanmadan), `src/consents/MarkupView.tsx`, `src/consents/ConsentSheet.tsx`.
- Ortak `docs/api/consent-markup-vectors.json` 16 vektörü mobilde birebir; Cc/Cf tablosu Node'un Unicode 16 verisiyle tüm kod noktalarında karşılaştırıldı.
- Ekran: tam sayfa, tür adı başlık, sürüm/yayın tarihi, durum rozeti, metin kartı. Onay düğmesi `accessibilityRole="checkbox"` kutusu işaretlenmeden pasif. Aydınlatma metninde "Okudum olarak kaydet", açık rızada "Onaylıyorum". Bağlantılar adres gösterilerek onayla açılır. Biçim hatalı metin "güvenli gösterilemiyor" uyarısıyla onaya kapalı.

**T18.3 Fotoğraf eklemeden önce rıza — TAMAMLANDI.**
- Dosyalar: `src/consents/ConsentProvider.tsx` (`useConsentPrompt().ask(feature)`), `src/components/ProfileEditor.tsx`, `src/components/RequestForm.tsx`.
- "Galeriden seç / Fotoğraf çek" önce rıza durumuna bakar; ACTIVE (veya onay kuyrukta) değilse ConsentSheet açılır, onaydan sonra seçici açılır. Vazgeç fotoğrafsız devam eder ("Fotoğraf eklenmedi…"). Talep gönderirken fotoğraf ekliyse rıza yeniden kontrol edilir. Metin yoksa, çevrimdışı ve önbellekte metin yoksa veya metin yayımlanmamışsa açık mesaj verilir, seçici açılmaz.
- Profil gizlilik penceresindeki "metin yayınlanmadı" yazısı gerçek rıza durumuyla değiştirildi.
- Düzeltme (16.10.2026, kullanıcı bildirimi: onaydan sonra "Fotoğraf hazırlanıyor"da kaldı): iOS, rıza sayfası kapanma animasyonu bitmeden açılan fotoğraf seçiciyi göstermiyor ve seçicinin sözü hiç sonuçlanmıyordu. `useConsentPrompt` cevabı artık sayfa tamamen kapandıktan sonra (`Modal onDismiss`, diğer platformlarda ve kaçan olayda kısa gecikme) veriyor; `ConsentSheet` `visible`/`onDismiss` alır. `npm run check` 105/105.

**T18.4 Çevrimdışı rıza kuyruğu ve sıralama — TAMAMLANDI.**
- Dosyalar: `src/consents/engine.ts`, `src/consents/store.ts`, `src/auth/AuthProvider.tsx` (talep eşitlemesinden önce `flushConsents`; whitelist'e iki rıza ucu), `src/profile/useProfile.ts` (profil eşitlemesinden önce flush), `src/sync/engine.ts`, `src/sync/outbox.ts`, `src/profile/engine.ts`, `src/profile/outbox.ts`, `src/components/SyncPanel.tsx`.
- Kuyruk en eskiden başlayarak tek tek gönderilir; aynı tür için gönderilmemiş/çakışmış/başarısız iş yeni kararla değişir. 409 `BF_CONSENT_TEXT_OUTDATED` işi `conflict` yapar ve liste yenilenir, kişiye yeni sürüm sorulur. 401 işi yeniden denemeye alır; 429/5xx `min(120 sn, max(Retry-After, 2 sn × 2^deneme))` ile bekler, 6 deneme bütçesi.
- Sunucu fotoğraflı işi `BF_CONSENT_REQUIRED` 403 ile reddederse iş silinmez: talep işi `blocked`, profil işi `failed`, hata kodu 4031 (rıza bekliyor). Rıza onayı başarıyla gönderilince `releaseHeld` bu işleri yeniden denemeye alır. Bekleyen iş iptal/vazgeç edilebilir; başka 403 eskisi gibi davranır.

**T18.5 Açılış kapısı ve Rızalarım — TAMAMLANDI.**
- Dosyalar: `src/consents/ConsentProvider.tsx` (açılış kapısı), `src/components/ConsentCenter.tsx`, `src/app/_layout.tsx`, `src/app/(tabs)/profile.tsx`.
- Kapı: okunmamış aydınlatma metni ve OUTDATED açık rıza her metin sürümü için bir kez gösterilir ("Daha sonra" görüldü kaydı yazar). Rızalarım: tür başına durum, sürüm, tarih, bekleyen/sorunlu karar notu, Metni oku / Oku ve onayla, Geri çek (etkisini anlatan onayla), Yeniden gönder / Bildirimi kaldır, Rızalarımı yenile, bekleyen fotoğraflı işlem sayısı.
- Geri çekmede ilgili özellik için cihazdaki yerel fotoğraflar (`dropLocalPhotos`) silinir. Kişi geçmiş görmez; geçmiş yalnız merkezdedir (kullanıcı kararı). PDF yerine İŞ16 yazdırılabilir belge merkezde; mobilde metnin kendisi gösterilir.

**T18.6 Testler ve kabul — AÇIK.**
- `tests/consents.test.mjs` 13/13 (ortak vektörler, Cc/Cf tablosu, sözleşme doğrulayıcıları, kuyruk sırası ve değiştirme, 409/401/429 akışları, bekletilen işlerin serbest bırakılması, geri çekmede yerel fotoğraf silme, kapı seçimi). `npm run check` (typecheck, lint, 105/105 test) temiz; `npm run build` (expo export, tüm platformlar) çıkış 0.
- Gerçek API: İŞ17 API'sinin varsayılan metinlerle verdiği gerçek yanıtlar (liste, onay, geri çekme, eski metin 409, profil fotoğrafı 403) mobil motordan geçirildi; karmalar eşleşti, 403'te iş bekletildi ve taslak korundu.
- Tarayıcı önizlemesi (Expo web, taklit oturum, Playwright, 390 px, yatay taşma 0): kutu işaretlenmeden onay pasif, açılış kapısı, onaydan sonra seçici, Vazgeç ile fotoğrafsız devam, onaylı geri çekme. Görüntüler `/mnt/project-files/analysis/is18-screens/`.
- Açık kalan: iOS/Android cihaz kabulü (çevrimiçi, uçak modu, metin revizyonu, geri çekme) kullanıcıyla yapılacak; T16.6/T17.6 kalemleri.
- Yayın etkisi: İŞ17 ile İŞ18 birlikte yayınlanmalı önerisi artık karşılanıyor; mobil uygulama fotoğraf eklemeden önce rıza alıyor.
