# BF-MULTI FAZ 04 — Merkez yönetim ekranı (Kişiler)

22.10.2026 (Europe/Istanbul). GitHub [#36](https://github.com/AkaBayraK/BuildingFlow/issues/36). Plan toplamı 10 faz (FAZ 00–09); bu faz listede 5/10. **Durum: teknik teslim, kullanıcı onayı bekleniyor. FAZ 05 başlatılmadı.** Mevcut `features/person-center` (CenterList, CenterDetail, RelationForms, Choices) ve shadcn ManagementPanel / RecordList / TanStack / FilterChips / ToneBadge kalıpları genişletildi; yeniden yazılmadı. Backend değişmedi (FAZ 02/03 API'leri tüketildi).

## 1) Yapılan değişiklikler ve dosya yolları

| Dosya (`frontend/buildingflow-web/`) | Değişiklik |
| --- | --- |
| `src/features/person-center/CenterList.tsx` | Liste `GET /api/v2/people` dizininden: müşteri seçimi **isteğe bağlı** (Tüm müşteriler), müşteri seçilince isteğe bağlı yönetim alanı filtresi, ilişki türü ikonlu çipler (Tümü/Malik/Kiracı/Sakin), müşteri ve maskeli kimlik kolonları; "müşteri seçiniz" engeli kaldırıldı |
| `src/features/person-center/CenterDetail.tsx` | Sekmeler ikonlu; "Bağımsız bölüm ilişkileri" sekmesinde portföy + ilişki listesi (doğrulama rozeti kolonu), "Kiracılık ekle" ve sözleşmesiz kiracılık için "Sözleşmeye bağla"; hesap sekmesi `AccountLink` |
| `src/features/person-center/RelationForms.tsx` | Malik/sakin formu alan → blok → daire (`UnitPicker`); kiracılık bu formdan çıkarıldı, kira sözleşmesi akışına taşındı |
| `src/features/person-center/Choices.tsx` | İsteğe bağlı (`optional`) seçim ve sorgu parametresi |
| `src/features/person-center/model.ts` | Strict DTO doğrulayıcıları: `directoryRow` (yalnız maskeli kimlik; tam kimlik/telefon/e-posta reddedilir), `unitChoice`, `portfolio`, `leaseContract`, `linkCode`; `relation` doğrulama/sözleşme alanları, `account` kod ipucu |
| Yeni: `UnitPicker.tsx`, `PersonPicker.tsx`, `PortfolioSection.tsx`, `LeasePanel.tsx`, `AccountLink.tsx`, `Badges.tsx`, `relation-style.ts` | Bağımlı seçim; ortak kiracı seçici (aynı müşterinin maskeli dizini); daire başına gruplu portföy; sözleşme oluştur / ortak kiracı ekle / bağla / bitir; tek gösterimlik bağlama kodu ve bağı sonlandırma onayı; kapasite/doğrulama/portföy/sözleşme durumu için tek ikon+ton eşlemesi |
| `messages/tr.json`, `messages/en.json` | `personCenter` altında eş TR/EN anahtarlar; `relations`, `accountHelp`, `relationHelp` metinleri güncellendi |
| `tests/person-center.test.mjs` | 7 yeni sözleşme/kaynak testi, hesap testi ipucu alanlarına uyarlandı |
| `tests/fixtures/person-center-api.ts` (yeni), `sidebar-api.ts`, `sidebar-entry.tsx`, `sidebar-mocks.tsx` | İzole fixture: `/fixture-people`, `/fixture-person` temsili veri (gerçek kişi/API değil), router `push` |

Davranış:

- **Kişiler listesi alan seçmeden açılır**; kapsamı sunucu belirler (SUPER_ADMIN tüm müşteriler, yönetici yetkili kapsam). Yeni kişi oluşturma yalnız müşteri seçiliyken (kişi bir müşteriye aittir).
- **Malik / kiracı / sakin ayrı iş akışları:** malik ve sakin `RelationEditor` ile; kiracılık yalnız `LeasePanel` ile (sözleşme + taraf). Tek aktif sözleşme kuralı ihlali sunucunun `BF_LEASE_OVERLAP` TR/EN mesajıyla gösterilir.
- **409/version:** tüm sözleşme işlemleri `expectedVersion` gönderir; çakışmada "Güncel kaydı yükle" listeyi yeniler ve formu sıfırlar; belirsiz sonuçta aynı içerik aynı `operationId` ile tekrar edilir (kör retry yok).
- **Hesap bağlama:** kod yalnız POST yanıtından bileşen belleğinde bir kez gösterilir, sonra yalnız son iki hane ipucu ve geçerlilik; kod tarayıcı deposuna yazılmaz. Bağı sonlandırma ayrı onay penceresi (`destructive`).

## 2) Yeni/etkilenen endpointler

Yeni backend endpoint yok. Tüketilen: `GET /api/v2/people`, `…/people/{p}/portfolio`, `…/person-building-choices`, `…/person-unit-choices?buildingId`, `…/units/{u}/lease-contracts` (+`/parties`, `/attach`, `/end`), `…/account-link-codes`, `…/account-link/end`, `…/account-status`.

## 3) Yetki ve KVKK etkisi

- Ekran yalnız SUPER_ADMIN/SITE_ADMIN/MANAGER rolünde açılır (mevcut kapı); asıl yetki backend'dedir.
- Listede ve ortak kiracı seçicide tam kimlik, telefon, e-posta görünmez; DTO doğrulayıcısı bu alanları içeren yanıtı reddeder.
- Portföyde başka kişinin adı/kimliği yoktur (sunucu göndermez, istemci de beklemez).
- Bağlama kodu loglanmaz, depolanmaz; yalnız oluşturulduğu anda gösterilir.

## 4) Migration/rollback durumu

Migration yok; backend ve şema değişmedi. Geri dönüş = bu frontend dosyalarını eski sürüme almak; bayraklar (`BUILDINGFLOW_PERSON_CENTER_ENABLED`, `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED`) kapalıyken `/people` eski alan seçimli akışı gösterir.

## 5) Test komutları ve sonuçları

| Komut | Sonuç |
| --- | --- |
| `npm test` | **228/228** (7 yeni person-center testi dahil) |
| `npm run typecheck` | PASS |
| `npm run build` | PASS (`next-env.d.ts` build sonrası eski haline alındı) |
| `node scripts/sidebar-fixture.mjs` + tarayıcı paneli | 1440 px: liste (alan seçimsiz, ikonlu çipler, maskeli kimlik), detay portföyü (Örnek Apartmanı D3 Malik oturuyor / D7 Kiracı oturmuyor; Örnek Sitesi A Blok D5 Kiraya verilmiş / B Blok D12 Kiracı oturuyor, PENDING sakin rozeti), D7 sözleşme paneli (aktif sözleşme + ortak kiracı, biten sözleşme). 375 px: `scrollWidth=375` (yatay taşma yok), hesap sekmesinde kod tek sefer gösterimi, tarayıcı deposunda kod yok, konsol hatası 0. Sekmelerin dördü ikonlu |

Kapsanan UI senaryoları: çok yönetim alanı (iki alan), aynı sitede çok daire (A/B blok), ortak kiracı (aynı sözleşmede iki taraf), tek sözleşme çakışması (sunucu kodu + TR/EN mesajı; fixture yazma çağrısını gerçek sunucu gibi yanıtlamaz). Ortak malik akışı mevcut malik formu + pay alanlarıyla korunur. Fixture temsili veridir; canlı OIDC/API ve gerçek cihaz kabulü değildir. Backend bu fazda değişmediği için backend testleri yeniden çalıştırılmadı (son sonuç FAZ 03: entegrasyon 223/224, unit 295/295).

## 6) Bilinen riskler ve açık kararlar

1. **Mükerrer kişi eşleştirme (FAZ 00 kararı 4) bu fazda yapılmadı.** Doğru uygulama backend'de yönetici onaylı, audit'li bir "kişi birleştirme" işlemi gerektirir: ilişkilerin, sözleşme taraflarının, hesap bağının, görevlerin ve `LegacyPersonMap` kayıtlarının tek kişiye taşınması; geri alınamazlık, borç (`DuesChargeParty`) snapshot'larının korunması. Kapsam büyük ve geri dönüşü zor olduğundan ayrı iş olarak **onayınızı bekliyor**: (a) yalnız öneri listesi (aynı müşteri + aynı kimlik no, salt okunur), (b) tam onaylı birleştirme.
2. **Kanıt belgesi:** backend doğrulama alanında `evidenceDocumentId` var ama ilişki/sözleşmeye belge yükleme/bağlama endpoint'i yok; ekranda belge gösterilmiyor. Ayrı karar/iş.
3. PENDING ilişkiyi VERIFIED/REJECTED yapan yönetici "doğrula/reddet" eylemi için endpoint yok; rozet yalnız görüntülenir.
4. Eski `/properties/{id}/people` ekranı bayraklar kapalıyken çalışmaya devam eder; geçiş FAZ 09'da.
5. Fixture yazma çağrılarını yanıtlamaz; 409/çakışma akışı gerçek API'de kabul testi gerektirir.

## 7) Sonraki faza geçiş koşulu

Kullanıcı raporu ve 6.1/6.2 kararlarını onaylarsa FAZ 05 (#37, malik/kiracı web portalı) başlar. Onay gelmeden FAZ 05'e geçilmez.
