# BF-MULTI FAZ 08 — İş modüllerinin bağlam entegrasyonu

22.10.2026. GitHub [#40](https://github.com/AkaBayraK/BuildingFlow/issues/40). Plan 10 faz; listede 9/10. **Teknik teslim, onay bekliyor; FAZ 09 başlatılmadı.** Düşük tüketimli, en küçük doğru değişiklik. Online ödeme sağlayıcısı eklenmedi (FAZ 00 kararı).

## 1) Değişiklikler
- Backend `MyUnit.cs`, `MyUnitStore.cs`, `SessionContextEndpoints.cs`:
  - `GET /api/v2/me/units/{unitId}/documents/{documentId}/download` — VIEW_DOCUMENTS, bağlam her istekte yeniden çözülür; yalnız yayınlanmış ALL_MEMBERS duyuru eki; boyut+SHA256 doğrulamalı, `no-store`, `nosniff`, attachment.
  - `GET /api/v2/me/finance-summary` — Tüm Dairelerim toplu finans: VIEW_DUES olan malik/kiracı bağlamları, açık kapsam listesi (alan, daire, rol), **aynı borç birden çok rolde görünse bile bir kez sayılır** (`sharedCharges`).
- Web `features/my-portfolio`: talep oluşturma + geçmiş (mevcut canonical eşitleme uçları `/api/v1/mobile/sync/commands|requests`, `contextKind/contextId` ile; sunucu bağlamı yeniden doğrular; ikonlu kategori çipleri; aynı içerikte aynı `operationId`), belge indirme düğmesi, Tüm Dairelerim finans özeti kartı (kapsam etiketi + ortak borç notu). TR/EN anahtarlar.
- Mobil: seçili dairede aidat (kalan/gecikmiş) ve son 3 duyuru başlığı, yeteneğe göre, yalnız çevrimiçi, yanıt `contextKey` ile eşleşmeli (`unitSummary`, `meRequest`); kalıcı önbelleğe yazılmaz.
- Testler: backend `MyUnitTests` +1; web `my-portfolio.test.mjs` +2; mobil `person-portfolio.test.mjs` +1.

## 2) Kapsam dışı / belgelenen
- **Yeni modelde doğrudan oluşturulmuş kişiye bağlı tahakkuk:** finans borçlu tarafı (`DuesChargeParty`) yalnız legacy `Person`'a bağlanabiliyor; native kişide yalnız borçlusu belirtilmemiş UNIT borçları görünür. Kişiye bağlı tahakkuk finans modeli değişikliği (yeni taraf modeli + migration) gerektirir — FAZ 09 geçiş kararıyla.
- **Bildirim → bağlam geçişi:** mevcut mobil bildirim altyapısı legacy bağlam anahtarıyla çalışıyor; canonical bildirim derin bağlantısı yok. Bu fazda eklenmedi.
- Anket/rıza: mevcut legacy akışa dokunulmadı.

## 3) Yetki/KVKK
Tüm yeni okumalar bağlamı sunucuda yeniden çözer (404/403). Toplu özet kişi verisi içermez. Belge indirme tarayıcıda saklanmaz.

## 4) Migration
Yok.

## 5) Testler
| Komut | Sonuç |
| --- | --- |
| Backend `MyUnitTests|MyPortfolioTests|MobileCanonicalSync` (ayrı PostgreSQL, sapma öncesi) | **15 geçti / 1 başarısız** — başarısız: S3 gerektiren bilinen `MobileCanonicalSync` fotoğraf testi |
| Backend genişletilmiş regresyon (ikinci koşu) | **ÇALIŞTIRILAMADI**: paralel iletişim doğrulama işinin `AccountPhoneVerification` entity'si migration'sız (`db.sh check`: bekleyen model değişikliği) → tüm DB testleri başlangıçta durdu. Bu iş benim değil; dokunulmadı |
| Backend unit | **295/295** |
| Web `npm test` / typecheck / build | **237/237** / PASS / PASS |
| Mobil `npm run check` / build | **130/130** / PASS |

## 6) Riskler
1. Paralel iş migration'ı eklenene kadar backend entegrasyon testleri ve API açılışı (AutoMigrate/pending check) bloke.
2. Native kişiye bağlı tahakkuk ve canonical bildirim derin bağlantısı yok (yukarıda).
3. Talep fotoğrafı web'de yok (mobilde var).

## 7) Geçiş koşulu
Kullanıcı onayıyla FAZ 09 (#41).
