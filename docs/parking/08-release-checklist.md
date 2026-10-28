# OTOPARK-08 yayın kontrolü

Bu belge mevcut #80 ve roadmap kaydının yayın kanıtıdır; yeni bir faz/backlog değildir.

| Kapı | Son durum | Kanıt / gereksinim |
|---|---|---|
| Faz07 kabulü | TAMAMLANDI | Kullanıcı Faz08 onayı; #79completed |
| Tam backend regresyonu | TAMAMLANDI |817/817entegrasyon,413/413unit,0fail/skip; önceki başarısızlar kabul edilmez |
| Web test/typecheck/build | TAMAMLANDI |276/276,tip/üretimderlemesi; son kaynak |
| Mobil check/export | TAMAMLANDI | Node-forge yaması sonrası154/154, tip/lint, iOS/Android/web export; gerçek cihaz değildir |
| Web/NuGet audit | TAMAMLANDI | Güncel duyurularda0vulnerability; kaynak hatası yok |
| Mobil audit | **BLOKE** | Zod/Expo zinciri3high; ayrıca6moderate/5low. Güvenlik kapısı değiştirilmedi |
| License/SBOM | KISMİ |329/871metadata,1116bileşenSBOM ve97/97/19OSenvanteri; native/tam binary ve hedef dağıtım yükümlülükleri ayrıca |
| API/web/worker Docker | TAMAMLANDI (yerel) | Son source3imaj/non-root,SSR/CSP/JWT/CORS/kapalıpasswordgrant;smoke ve klavye geçti |
| Gerçek yerel E2E | TAMAMLANDI (sınırlı kapsam) | Test OIDC S256, alan/daire/politika/park yeri, misafir oluştur/giriş/çıkış, bakım oluştur/iptal, rapor1/1/1,CSV; üretim TLS/native değildir |
| Erişilebilirlik | TAMAMLANDI (sınırlı) |TR/EN/koyu,375/1440px,panel/Escape;Home/End/oklar/tekTabdurağı geçti.WCAGsertifikası/scanneriddiası yok |
| Tenant/rol/KVKK | TAMAMLANDI (kayıtlı kapsam) |Muhasebeci200negatif testle üretildi;rol/UUIDdüzeltmeleri817finaltestte geçti |
| Migration/rollback | TAMAMLANDI (izole DB) | Migrations/model/8alan; terminal maskeleme Downkoruması, eski migration değişmedi |
| Üretim HTTPS/gerçek native/CI | BEKLIYOR | Hedef ortam/cihaz ve ayrıca yetkilendirilmiş CI/deploy gerektirir; önceki sonuçlar yeni koşu sayılmaz |
| Kullanıcı final kabulü | BEKLIYOR | #80açık; engeller kapanmadan go-live onayı istenmez |

## Tekrar üretim

Mevcut bağımlılıklarla `npm test`, `npm run typecheck`, `npm run build` (web); `npm run check`, `npm run build -- --clear` (mobil). `python3 scripts/check-licenses.py`, `node mobile/buildingflow-mobile/scripts/release/licenses.mjs`; `python3 scripts/parking-sbom.py --output /tmp/parking.cdx.json`. Yeni paket kurma ihtiyacı yoktur.

Backend yalnız ayrı PostgreSQL ve fixture S3/RabbitMQ/Mailpit ile: `dotnet test backend/BuildingFlow.sln --no-restore -m:1 --disable-build-servers --filter 'FullyQualifiedName!~ContactNativeHost&FullyQualifiedName!~PhoneContactBrowserHost'`. İki opt-in iletişim host'u kapsamdan açıkça dışlanır, native kabulü olarak sayılmaz. TEST_DATABASE/SMTP/broker/storage sırları çıktı/rapora yazılmaz. İlgili environment değişkenleri scripts/test.sh içinde belgelenmiştir; bu script mevcut servis başlatma/yapılandırma etkileri incelenmeden çalıştırılmaz. Bu tur testler bağımsız PG/broker ve yalnızfixtureS3bucket kullandı.

Audit: web ve mobil `npm audit --audit-level=high --json`; mobil `--package-lock-only`; NuGet `dotnet list backend/BuildingFlow.sln package --vulnerable --include-transitive --format json`. Audit düzeltmesi force/downgrade ile uygulanmaz. Son karar yalnız rapordaki gerçek sonuçtur.

## Güvenli geri dönüş

Henüz push/commit/PR yok; worktree değişiklikleri önceki05–07ile birlikte korunur. Worker'ı kapat/yazımları durdur, kurulu migration sürümünü kontrol et. Maskelenmiş veri varsa Down reddi aşılmaz; eski binary satırları gösterebilir, ileri düzeltme gerekir. Node-forge eski1.4.0'a dönüş açık duyuruyu geri getirir. Kaynak değişikliği veya ortam onayı gate'i kendiliğinden başarılı yapmaz. Canlı DB/.env/IdP/servis ve mevcut kullanıcılar değiştirilmedi.

İnceleme için test sekmesi ve6ayrıDockerfixture açık bırakıldı; regresyon/SQLprovaDB/rol ve testMailpit temizlendi. Varsayılan otoparkworker işlemleri kapalıdır. Modelde değişiklik yok; üretilen idempotentSQL yeniDByeikiuygulama/49migration geçti.
