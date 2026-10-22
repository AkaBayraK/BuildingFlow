# BF-MULTI FAZ 05 — Malik / kiracı merkez web portalı

22.10.2026 (Europe/Istanbul). GitHub [#37](https://github.com/AkaBayraK/BuildingFlow/issues/37). Plan toplamı 10 faz (FAZ 00–09); bu faz listede 6/10. **Durum: teknik teslim, kullanıcı onayı bekleniyor. FAZ 06 başlatılmadı.** Mevcut session-workspaces kabuğu (`/workspaces`, ContextShell) ve FAZ 03 `GET /api/v2/me/portfolio`, `GET /api/v2/me/active-context` üzerine kuruldu; yeniden yazılmadı. Mükerrer kişi / doğrulama / kanıt belgesi işi (#42) kullanıcı kararı beklediği için bu fazda ele alınmadı.

## 1) Yapılan değişiklikler ve dosya yolları

| Katman | Dosya | Değişiklik |
| --- | --- | --- |
| Application | `backend/src/BuildingFlow.Application/MyUnit.cs` (yeni) | `IMyUnitStore`, salt okunur daire bölümleri DTO'ları; her yanıt `contextKey` taşır |
| Infrastructure | `backend/src/BuildingFlow.Infrastructure/MyUnitStore.cs` (yeni), `DependencyInjection.cs` | Bağlamı her istekte `ResolveAsync` ile yeniden çözer, yetenek yoksa 403; aidat/tahsilat/duyuru/belge/sözleşme okuma |
| Api | `backend/src/BuildingFlow.Api/SessionContextEndpoints.cs` | `/api/v2/me/units/{unitId}/{dues,payments,announcements,documents,leases}` (`no-store`) |
| Test | `backend/tests/BuildingFlow.IntegrationTests/MyUnitTests.cs` (yeni, 2) | Yetenek, IDOR, bağlam dışı, sayfa sınırı, bitmiş ilişki, bağ sonlandırma, kapalı bayrak |
| Web | `frontend/buildingflow-web/src/features/my-portfolio/{model.ts,MyPortfolio.tsx,section-style.ts}` (yeni) | Portföyüm: Tüm Dairelerim / Seçili Daire modları, rol seçimi, yetenek tabanlı ikonlu sekmeler |
| Web | `src/app/(workspace)/workspaces/portfolio/page.tsx` (yeni), `src/auth/return-path.ts`, `src/components/WorkspaceShell.tsx`, `src/features/session-workspaces/SessionWorkspaces.tsx` | Rota `/workspaces/portfolio` (bayrak kapalıyken `/`'a yönlenir), OIDC dönüş whitelist'i, bağlam kabuğu menüsü ve çalışma alanları sayfasından bağlantı |
| Web | `messages/tr.json`, `messages/en.json` | `myPortfolio` ad alanı + `sessionWorkspaces.portfolio` (eş TR/EN) |
| Web test | `tests/my-portfolio.test.mjs` (yeni, 6); fixture `tests/fixtures/person-center-api.ts`, `sidebar-entry.tsx` | `/fixture-portfolio` temsili Ahmet verisi |
| Belge | bu rapor, `docs/api/session-workspaces.md`, `docs/TESTING.md`, `docs/progress/*` | |

Davranış:

- **Portföyüm:** müşteri/kişi → yönetim alanı → (blok) daire kartları; ikonlu durum (Malik oturuyor, Kiraya verilmiş, Kiracı oturuyor/oturmuyor, Yalnız sakin, Malik/boş) ve kapasite rozetleri; her ilişki için "… olarak aç". Yönetim görevleri ayrı "Yönetim görevlerim" bölümünde, mevcut çalışma alanı sayfasına gider.
- **Seçili Daire:** seçim önce `/me/active-context` ile sunucuda doğrulanır; sekmeler **sunucunun döndürdüğü yeteneklerden** üretilir (Aidat ve borçlar, Tahsilatlar, Duyurular, Belgeler, Kira sözleşmesi, Talep/arıza, İlişkilerim). Aynı dairede birden çok rol varsa ikonlu rol çipleriyle geçiş.
- **Bayat veri koruması:** her seçim `contextKey` ile ayrı bileşen olarak yeniden kurulur (önceki istekler iptal), istek yolu ilişki kimliğini içerir ve yanıttaki `contextKey` seçimle eşleşmezse yanıt reddedilir. Sekmeye dönüşte portföy yeniden okunur; seçili ilişki yeni yanıtta yoksa seçim düşer ve uyarı gösterilir. Bağlam 404 olursa "artık erişilebilir değil" ve geri dönüş.
- **Yalnız görüntüleme:** ilişki ekleme/değiştirme, sözleşme yazımı, ödeme yok; ilişkiler sekmesi bunu açıkça söyler. Talep oluşturma web'de henüz yok (mevcut akış mobil); dürüst bilgi mesajı gösterilir. Belge indirme FAZ 08'e bırakıldı; liste yalnız meta veridir.

## 2) Yeni/etkilenen endpointler

Yeni (hepsi `PersonRefactor:Enabled` + `SessionContextsEnabled`, girişli kullanıcı, `no-store`, sorgu: `propertyId`, `capacity` OWNER/TENANT/RESIDENT, `relationId`, `page`, `pageSize ≤ 50`):

- `GET /api/v2/me/units/{unitId}/dues` — VIEW_DUES; özet + sayfalı borç. Borç sorumluluğu UI rolünden değil, mevcut 06.1 kuralından: borçlusu belirtilmemiş UNIT borcu **veya** bu kapasitede, bu kişiye `LegacyPersonMap` ile eşleşen borçlu `DuesChargeParty` snapshot'ı.
- `GET …/payments` — VIEW_PAYMENTS; yukarıdaki borçlara yapılan tahsilat dağıtımları.
- `GET …/announcements` — VIEW_ANNOUNCEMENTS; aynı alanda PUBLISHED + ALL_MEMBERS.
- `GET …/documents` — VIEW_DOCUMENTS; yayınlanmış ALL_MEMBERS duyurulara ekli belge meta verisi.
- `GET …/leases` — VIEW_LEASE; kiracı yalnız kendi kiracılığının sözleşmesini, kiraya vermiş malik dairedeki sözleşmeleri görür; taraf adı/kimliği yok, yalnız taraf sayısı ve "tarafı mısınız".

Etkilenen: yok (mevcut uçlar değişmedi).

## 3) Yetki ve KVKK etkisi

- Seçim yetki vermez: her istek bağlamı DB'den yeniden çözer (ilişki etkin, VERIFIED/LEGACY, bugün geçerli, sözleşme etkin, hesap bağı aktif); uyuşmazlık 404, yeteneği olmayan bölüm 403.
- Başka dairenin/kişinin ilişki id'si, başka müşterinin dairesi, yanlış kapasite → 404 (test edildi).
- Sözleşme yanıtında karşı tarafın adı/kimliği yok; portföyde başka kişinin verisi yok; FE doğrulayıcısı bu alanları içeren yanıtı reddeder.
- Yanıtlar `no-store`; portal tarayıcı deposuna veri yazmaz.

## 4) Migration/rollback durumu

Migration yok; şema değişmedi (`db.sh check` fark yok). Geri dönüş: yeni uçlar ve sayfa bayraklar kapalıyken erişilemez (`/me/*` 404, sayfa `/`'a yönlenir).

## 5) Test komutları ve sonuçları

| Komut | Sonuç |
| --- | --- |
| `dotnet test BuildingFlow.sln --filter "MyUnit\|MyPortfolio\|SessionContext\|MobileAuthorization\|PersonCenter\|PersonPortfolio\|LeaseContract\|ErrorContract\|MobileFinance\|CommunicationApi\|PortfolioCapabilities"` (ayrı geçici PostgreSQL) | Entegrasyon **84 geçti, 0 başarısız, 7 atlandı** — atlananlar S3 gerektiren `CommunicationApiTests` `[StorageFact]` testleri (nesne deposu başlatılmadı; geçti sayılmaz). Unit filtre **10/10** |
| `dotnet test tests/BuildingFlow.UnitTests` | **295/295** |
| `bash scripts/db.sh check` | fark yok |
| `npm test` / `npm run typecheck` / `npm run build` | **234/234** / PASS / PASS (`next-env.d.ts` eski haline alındı) |
| İzole fixture `/fixture-portfolio` | 1440 px: iki alan gruplu kartlar, durum/kapasite rozetleri, yönetim görevi ayrı; D7 kiracı seçiminde 7 ikonlu sekme, aidat özeti/tablosu. 375 px: D3 sakin rolünde yalnız Duyurular/Talep/İlişkilerim sekmeleri, rol çipleri (Malik/Sakin), `scrollWidth=375`, konsol hatası 0. Temsili veri; canlı OIDC/API değil |

## 6) Bilinen riskler

1. Talep/arıza web'de yalnız bilgi mesajı; oluşturma ve geçmiş FAZ 08.
2. Belge indirme yok (meta veri listesi); yetkili indirme FAZ 08.
3. Legacy (aktarılmış) kişide borç görünürlüğü `LegacyPersonMap` eşleşmesine dayanır; native kişide yalnız UNIT borçları görünür (yeni modelde kişiye bağlı tahakkuk FAZ 08).
4. Seçim sayfa içinde tutulur; yeniden yüklemede Tüm Dairelerim'e dönülür (tarayıcı deposu bilinçli kullanılmadı).
5. Bayraklar kapalı; canlı etki yok.

## 7) Sonraki faza geçiş koşulu

Kullanıcı bu raporu onaylarsa FAZ 06 (#38, mobil portföy ve navigasyon) başlar. Onay gelmeden FAZ 06'ya geçilmez.
