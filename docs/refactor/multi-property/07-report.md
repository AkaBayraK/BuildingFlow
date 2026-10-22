# BF-MULTI FAZ 07 — Mobil offline önbellek ve eşitleme

22.10.2026. GitHub [#39](https://github.com/AkaBayraK/BuildingFlow/issues/39). Plan 10 faz; listede 8/10. **Teknik teslim, onay bekliyor; FAZ 08 başlatılmadı.** Düşük tüketimli, en küçük doğru değişiklik.

## 1) Değişiklikler (`mobile/buildingflow-mobile/`)
- `src/person-authorization/model.ts`: `portfolioAccess` — `/api/v2/me/portfolio` yanıtından yalnız bağlam başına durum kodu ve yetenek kodları (isim/kişisel veri yok); yalnız güncel yetki kopyasındaki bağlamlar; `unitStatusLabels` (Kendisi oturuyor, Kiraya verilmiş…).
- `src/offline/database.ts`: **şema 9** `portfolio_cache` (hesap başına, SQLCipher veritabanı içinde); `writePortfolio`/`readPortfolio` — 24 sa yaş sınırı, okumada iptal edilmiş bağlamlar düşer, başka hesap reddedilir.
- `src/auth/AuthProvider.tsx`: canonical modda çevrimiçiyken `/me/portfolio` okunup önbelleğe yazılır, çevrimdışıyken son doğrulanmış kopya gösterilir; değer hesap kimliğine bağlı, oturum/hesap değişince gösterilmez. Web'de kalıcı önbellek yok.
- `src/person-authorization/PersonModelScreen.tsx`: daire satırında durum rozeti metni; talep/eşitleme bölümü `CREATE_REQUEST` yeteneğine göre açılır (bilgi yoksa mevcut davranış).
- Testler: `offline.test.mjs` (+1, şema 9), şema sürüm beklentileri 8→9 (`notices`, `owner`, `person-authorization` testleri).

Zaten mevcut olup korunanlar: hesap+`contextKey` kapsamlı outbox, idempotent `operationId`, sunucuda yeniden yetki; bağlamı iptal/REJECTED olan kuyruk işleri `blocked` (403) olarak **kendi bağlamında** tutulur ve özet panelinde gösterilir, başka bağlama taşınmaz; çıkışta cihaz verisi temizlenir; çevrimdışı lease 1 sa (yönetim) / 24 sa (malik/kiracı/sakin), parola/düz token saklanmaz. Ödeme, sözleşme onayı ve hesap bağlama yalnız çevrimiçi (mobilde bu eylemler yok).

## 2) Endpoint
Yeni yok; mevcut `GET /api/v2/me/portfolio` tüketildi.

## 3) KVKK
Önbellekte yalnız durum/yetenek kodları; isim, kimlik, başka kişi verisi yok.

## 4) Migration
Mobil SQLite şema 8→9 (ekleyici, `IF NOT EXISTS`); backend migration yok.

## 5) Testler
Mobil `npm run check` (typecheck + lint + test) **129/129**; `npm run build` **geçti**. Backend değişmedi, backend testi çalıştırılmadı. Cihaz kabulü yapılmadı.

## 6) Riskler
1. FAZ 05 `/me/units/*` özetlerinin (aidat/duyuru/belge) mobil önbelleği ve ekranı yok (kapsam tüketim nedeniyle sınırlı tutuldu; FAZ 08 ile).
2. Bayraklar kapalı; legacy davranış değişmedi.
3. Gerçek cihazda çevrimdışı/çevrimiçi kabul gerekiyor.

## 7) Geçiş koşulu
Kullanıcı onayıyla FAZ 08 (#40).
