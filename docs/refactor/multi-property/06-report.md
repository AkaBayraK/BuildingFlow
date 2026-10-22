# BF-MULTI FAZ 06 — Mobil portföy ve navigasyon

22.10.2026 (Europe/Istanbul). GitHub [#38](https://github.com/AkaBayraK/BuildingFlow/issues/38). Plan 10 faz; bu faz listede 7/10. **Teknik teslim, onay bekliyor; FAZ 07 başlatılmadı.** Kullanıcı isteğiyle düşük tüketimli, en küçük doğru değişiklik.

## 1) Değişiklikler (`mobile/buildingflow-mobile/`)
- `src/auth/contracts.ts`: `initialContext` — canonical modda yalnız hatırlanan veya **tek** bağlam kendiliğinden açılır; birden çoksa kullanıcı seçer. Legacy modda eski davranış (ilk bağlam) aynen.
- `src/auth/AuthProvider.tsx`: çevrimiçi ve çevrimdışı açılışta `initialContext` kullanılır.
- `src/person-authorization/model.ts`: `portfolioGroups` — alan → daire gruplama, aynı dairedeki roller birlikte, yönetim görevleri ayrı.
- `src/person-authorization/PersonModelScreen.tsx`: düz liste yerine **Portföyüm**: üstte seçili alan/daire/rol göstergesi, alan kartları içinde daire başına rol çipleri (48 pt dokunma alanı, erişilebilir etiket ve `selected` durumu), "Yönetim görevlerim" ayrı. Talep/eşitleme bileşenleri mevcut haliyle seçili bağlama bağlı kalır.
- `tests/person-portfolio.test.mjs` (2 test).

## 2) Endpointler
Yeni endpoint yok. Mobil canonical mod mevcut `/api/v2/mobile/authorization` imzalı yetki kopyasını kullanır (FAZ 03 `/me/portfolio` ile aynı sunucu bağlam kaynağı; çevrimdışı okunabilir). Cache/kuyruk ayrımı mevcut `contextKey` (`tenant:kind:id`) ile; bağlam değişiminde eşitleme iptal edilir (mevcut `choose`).

## 3) Yetki/KVKK
Seçim yetki vermez; sunucu her eylemde bağlamı doğrular (mevcut). Yeni veri saklanmaz.

## 4) Migration
Yok.

## 5) Testler
`npm run check` (typecheck + lint + test) **128/128 geçti**; `npm run build` (expo export) **geçti**. Backend değişmedi, backend testi çalıştırılmadı. Cihaz/simülatör görsel kontrolü yapılmadı.

## 6) Riskler
1. Mobil kartlarda portföy durumu (kiraya verilmiş vb.) ve sunucu yetenek listesi gösterilmiyor; `/me/portfolio` çevrimiçi zenginleştirmesi sonraki iş (FAZ 07 önbellek tasarımıyla).
2. Canonical bayraklar (`EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED`) kapalı; legacy davranış değişmedi.
3. Gerçek cihazda erişilebilirlik/görsel kabul yapılmadı.

## 7) Geçiş koşulu
Kullanıcı onayıyla FAZ 07 (#39, mobil SQLite/offline/sync).
