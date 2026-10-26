# BuildingFlow kod grafiği ve bağımlılık analizi (26.10.2026)

## 1. Graphify durumu

- **Kurulu:** `graphifyy` 0.9.95, pipx ile `~/.local/bin/graphify` ve `graphify-mcp` olarak. `~/.local/bin` PATH'te olmadığı için `graphify` komutu kabukta doğrudan bulunmuyor; tam yolla çalışıyor.
- **Claude Code entegrasyonu:** `~/.claude/settings.json` içinde `graphify hook-guard search` ve `hook-guard read` PreToolUse hook'ları aktif. `~/CLAUDE.md` içinde graphify kuralları var.
- **MCP:** `graphify-mcp` ikili dosyası kurulu, ancak `~/.claude.json` içinde MCP sunucusu olarak kayıtlı değil.
- **Proje içi:** BuildingFlow deposunda `graphify-out/`, `.graphifyignore` veya git hook yok. Proje bu yüzden henüz graphify'a bağlanmamış durumda.
- Yeni bir şey kurulmadı.

## 2. Üretilen çıktı (kaynak dışı)

Konum: `~/Desktop/Projects/BuildingFlow-graphify/` (yaklaşık 117 MB). Depo `git status` ile temiz kaldı.

| Grafik | Düğüm | Kenar | Topluluk | Kaynak commit |
|---|---|---|---|---|
| backend (.NET, 501 dosya) | 6.740 | 28.610 | 224 | ba3e5bdb |
| frontend/buildingflow-web (340 dosya) | 2.148 | 10.043 | 76 | ba3e5bdb |

Her klasörde `graph.json`, `graph.html` (etkileşimli görünüm) ve `GRAPH_REPORT.md` bulunur. Çıkarım `--code-only` ile yapıldı. Yalnız yerel AST kullanıldı, LLM'e kod gönderilmedi ve topluluklar adlandırılmadı ("Community N").

Sorgu örneği:
`~/.local/bin/graphify query "AccessScope nerede kullanılıyor" --graph ~/Desktop/Projects/BuildingFlow-graphify/backend/graphify-out/graph.json`

## 3. Backend bulguları

**Katman yönü temiz.** csproj referansları Domain ← Application ← Infrastructure ← Api/Worker şeklinde. Grafikte Domain'den başka bir projeye giden tek bir kenar bile yok. Application'dan Infrastructure/Api/Worker yönündeki 334 kenarın tamamı `dispatches_to / INFERRED`. Bunlar arayüz metodundan implementasyona çıkarılmış kenarlar, gerçek referans değil.

Projeler arası sembol kenarları:

| Kaynak → Hedef | Kenar |
|---|---|
| Infrastructure → Application | 1.493 |
| Infrastructure → Domain | 1.165 |
| Api → Application | 381 |
| Application → Domain | 118 |
| Api → Infrastructure | 14 |
| Worker → Application / Infrastructure | 12 / 9 |

**Merkez (god) düğümler:** `AccessScope` (322), `BuildingFlowDbContext` (236), `Property` (231), `PagedRows` (136), `BaseEntity` (126), `HierarchyConflictException` (112), `RequestValidationException` (99), `Person` (89), `WebsiteSite` (86), `ICurrentUserService` (85).
- `AccessScope` yetki/kapsam sınırının tek noktası. Buradaki her değişiklik geniş etki alanı demek ve `graphify affected "AccessScope"` ile ölçülmeli.
- `BuildingFlowDbContext` grafikte en yüksek aradalık (betweenness 0.071) değerine sahip ve yaklaşık 90 topluluğu birbirine bağlıyor. Tek DbContext'li modular monolith için bu beklenen bir durum, ama modül sınırlarının veri katmanında birleştiğini gösteriyor.

**En yoğun dosyalar (sembol sayısı):** `Domain/OnlinePayment.cs` (188), `Application/OnlinePayments.cs` (147), `Domain/Website.cs` (93), `Domain/Survey.cs` (92), `Domain/LatePenalty.cs` (88). Bölme için ilk adaylar online ödeme dosyaları.

**Import döngüsü:** tespit edilmedi.

**Boşluk:** 1.434 izole sembol var. Bunlar çoğunlukla DTO/record alanları (`ConstructionYear`, `FloorCount` gibi), AST'de bağlantısı görünmeyen alanlar. Hata olarak değerlendirilmemeli.

## 4. Frontend bulguları

**Genel akış sağlıklı:** features → components (1.823), features → services (543), features → lib (316), app → features (111).

**Import döngüsü:** dosya düzeyinde tespit edilmedi.

**Feature'lar arası bağlantı (378 kenar).** Karşılıklı bağımlı çiftler var. Dosya döngüsü değiller, ama modül sınırı zayıf:
- `people ↔ dues` (14 / 11), `dues ↔ payments` (5 / 9), `dues ↔ late-penalties` (8 / 5), `people ↔ hierarchy` (6 / 20), `online-payments → dues` ve `payments → online-payments`.

**Ortak yardımcılar feature içinde kalmış:**
- `features/people/useRows.ts` (`useRows`, `useSearch`) dues, payments, online-payments, person-center, navigation, users ve hierarchy tarafından kullanılıyor.
- `features/person-center/model.ts` (`uuid`, `dateOnly`) my-portfolio (43 kenar) ve session-workspaces tarafından kullanılıyor.
- Bunlar `lib/` veya `services/` altına taşınmaya uygun adaylar.

**Ters yönlü bağımlılıklar (paylaşılan katman → feature):**
- `components/…/Shell` → `features/navigation` (20), `features/dashboard` (7), `features/system-status`
- `auth/SessionProfileProvider` → `features/account` (6); `AuthGate` → `features/people` (NotificationInbox), `features/theme`
- `services/management-contracts.ts` → `features/people` tipleri; `services` → `features/property-details` doğrulayıcısı
- `components/FilterChips.tsx` → `features/requests` (`Tone` tipi)
- Bunlar küçük, ama "paylaşılan katman feature'ı tanımaz" kuralına aykırı.

**Merkez düğümler:** `errorMessage()` (251, aradalık 0.103), `Button` (222), `MessageBanner` (185), `useAuthorizedApi()` (182), `useNotification()` (112). Bunlar hata/bildirim sözleşmesinin (07.1) tüm ekranlara yayıldığını doğruluyor.

**Uyarı:** Parser `src/features/website/WebsiteEditor.tsx` dosyasında 1. satırda sözdizimi hatası raporladı ve yalnız 1 sembol çıkarabildi. Dosya normal görünüyor (`"use client"` ve importlar), bu yüzden büyük olasılıkla tree-sitter sınırı. Bu dosyanın bağımlılıkları grafikte eksik.

## 5. Öneriler (uygulanmadı)

1. `useRows/useSearch` ile `uuid/dateOnly` yardımcılarını ortak `lib/` altına taşımak.
2. `Shell`, `AuthGate` ve `services` içindeki feature importlarını prop/slot veya ortak tip dosyasıyla tersine çevirmek.
3. `OnlinePayment.cs` ve `OnlinePayments.cs` dosyalarını alt dosyalara bölmek.
4. Graphify'ı projeye kalıcı bağlamak isterseniz iki yol var: (a) depo içinde `graphify-out/` (kural gereği commit'lenir), (b) bu harici klasör. İkisi de sizin onayınızı gerektirir.
5. İsteğe bağlı: `~/.local/bin` dizinini PATH'e eklemek, `graphify-mcp` sunucusunu Claude Code'a kaydetmek.
