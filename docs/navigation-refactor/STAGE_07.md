# Sidebar FAZ 07.1 — Aşama 07: test ve regresyon

20.10.2026. Kaynak: `buildingflow-sidebar-phase/PHASE_07_1.md`, adım **07/08**. Kullanıcının devam onayı yalnız bu aşamaya uygulandı. Tek agent; phase/verify/security/docs. Uygulama kaynak kodunda bu tur değişiklik yok.

## Çalıştırılan kontroller

| Kontrol | Bu turun sonucu |
|---|---|
| `npm test` (frontend/buildingflow-web) | **177/177 geçti, 0 fail/skip** |
| `npm run typecheck` | **Geçti** |
| `npm run build` | **Üretim derlemesi geçti** |
| Lint | package.json içinde komut yok; çalıştırılmadı |
| `dotnet test backend/tests/BuildingFlow.UnitTests/BuildingFlow.UnitTests.csproj --no-restore --verbosity quiet` | **277/277 geçti, 0 fail/skip** |
| Seçili PostgreSQL integration | **83/83 geçti, 0 fail/skip** |
| `git diff --check` | Teslim kontrolünde doğrulandı |

Integration komutu: `dotnet test backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj --no-restore --verbosity quiet --filter "FullyQualifiedName~AuthenticationTests|FullyQualifiedName~SiteApiTests|FullyQualifiedName~UnitPeopleNavigationTests|FullyQualifiedName~SessionContextTests" --logger "trx;LogFileName=sidebar07.trx" --results-directory /tmp/bf-sidebar07-results`.

Gerçek TRX dağılımı: AuthenticationTests9, SiteApiTests34, UnitPeopleNavigationTests13, SessionContextTests23, filtre alt dizgesiyle eşleşen WebsiteApiTests4. JWT/anonim erişim, rol/üyelik, farklı alan ve context iptali/deep-link sınırları mevcut testlerle doğrulandı. Tam backend suite çalıştırılmadı. Test kimlikleri fixture imzalarıdır; gerçek Keycloak uçtanuca kabulü değildir.

`../postgres-18.6/scripts/test-db.py` ile yalnız rastgele adlandırılan ayrı veritabanı/rol oluşturuldu. Fixture her sınıf için ayrı schema/migration kullandı. finally bloğundaki mevcut cleanup yardımcı betiği yalnız bu test DB/rolünü kaldırdı; özel bağlantı dosyaları da silindi. Mevcut uygulama veritabanı, servis ayarları ve kullanıcı kayıtları değiştirilmedi. Genel scripts/test.sh/ci.sh mevcut servis hazırlığı/kurulum adımları içerdiğinden çağrılmadı. Restore/npm ci/kurulum yapılmadı.

İlk sandbox .NET girişimi MSBuild socket izin hatasıyla sonuçlandı; izinli tekrar 277/277 geçti. Docker okuma ve fixture loopback başlatma da sandbox engeli sonrası gerçek izin mekanizmasıyla tekrarlandı. Üretim build'inin next-env içinde değiştirdiği yalnız iki `.next/dev/types/` importu byte karşılaştırmasıyla başlangıç içeriğine geri alındı.

## Tarayıcı davranış kanıtı

Mevcut test-only fixture, gerçek WorkspaceShell/AuthGate/profile/Radix/CSS kullanır; router/API/Keycloak/PropertyScope test adaptörüdür. İş ekranı içerikleri gerçek API'den yüklenmez.

- **1440px:** Daireler → Kişiler → Aidat/Borçlar → Tahsilatlar → Gider/Bütçe → Raporlar → Web Sitesi Yönetimi → Daireler → Dashboard tıklamaları beklenen hedefi ve tek aktif sidebar bağlantısını korudu.
- **Geri/ileri:** Daireler scoped URL'sine geri dönüş ve Dashboard'a ileri dönüş doğru.
- **Sistem Ayarları:** mevcut planlı düğme disabled; gerçek route yok, etkinleştirilmedi.
- **375px:** drawer aktif Dashboard bağlantısına odaklandı, body scroll lock açıldı. Kişiler tıklamasında drawer kapandı, odak workspace-content'e geçti ve lock kalktı. Escape odağı Menüyü aç düğmesine döndürdü.
- **Doğrudan ikinci alan URL'si:** aktif Daireler href'i ikinci alan UUID'sini taşıdı, footer İkinci Alan gösterdi; document scrollWidth375, yatay taşma yok. Bu UI bağlam kontrolüdür; tenant izolasyon yetkisini integration testleri doğrular.
- Fixture tarayıcı error kaydı: **0**. Sekme kapandı, viewport resetlendi ve kendi fixture sunucusu durduruldu.

[Tarayıcı olayları](STAGE_07_EVIDENCE.json), [mobil görüntü](STAGE_07_MOBILE.jpg). Önceki aşamaların diğer viewport/font/focus sonuçları bu turun yeniden koşulan testi olarak sayılmadı.

## Açık kabul sınırları

**DOĞRULANAMADI:** gerçek kimlikli iş ekranı smoke, canlı tenant/gerçek hesap/Keycloak callback-refresh-logout ve fotoğraf; gerçek ağ kesintisi altında offline shell; native cihaz/ekran okuyucu ve aktif reduced-motion runtime. Tarayıcı aracında offline emülasyonu yok; ağdan bağımsız menü modelinin unit testleri ve fixture etkileşimleri gerçek offline kabulü değildir. Oturumsuz/eskimiş yetkiyle hassas veri erişimi başarılı kabul edilmedi. Mevcut scoped SMS OIDC dönüş allowlist açığı önceki aşamalarda kayıtlıdır; auth kapsamı bu tur genişletilmedi.

## Koruma ve teslim

Başlangıç Git indeksi, mevcut manifest/lock dosyaları, iki next-env dosyası ve .env hashleri aynı. Yeni bağımlılık/lisans/asset/API/migration/rol yok. Ürün roadmap işaretleri değiştirilmedi. Değişenler: bu rapor, JSON/JPG kanıtları, TESTING ve progress STATUS/SESSION/CHANGELOG kayıtları.

**Aşama07 1/1=%100,00; SIDEBAR7/8=%87,50.** Tüm ürün yol haritası bu tur değiştirilmedi; toplam ilerleme yüzdesi BİLGİ MEVCUT DEĞİL. Sonraki Aşama08 dokümantasyon/teslim kullanıcı onayı bekler.

Token giriş/çıkış/toplam/kalan context: her biri **BİLGİ MEVCUT DEĞİL**. Son Codex usage20.10.2026:300dk kullanılan%14/kalan%86, reset20.10.2026 23:36:32;10080dk kullanılan%18/kalan%82, reset27.10.2026 13:32:07 Europe/Istanbul. Kaynak rateLimitsByLimitId.codex; hesap kotası sohbet token tüketimi değildir.
