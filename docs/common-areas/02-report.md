# Ortak Alanlar FAZ 02 — teknik teslim, kabul bekliyor

26.10.2026 · OA-02 · plan sırası02/08 · [Issue #52](https://github.com/AkaBayraK/BuildingFlow/issues/52). Kullanıcı yalnız FAZ02'yi başlattı; FAZ03 uygulanmadı. [API sözleşmesi](../api/common-areas.md), [kanonik plan](../../BuildingFlow-OrtakAlanlar/MASTER_PROMPT.md).

**Alan tanımı, yönetim CRUD API'si, kullanıcı bağlamından katalog okuma ve eklemeli migration uygulandı.** Tür, fiziksel blok ve bağımsız görünürlük, kullanım şekli, kurallar, kapasite/saat ve referans ücret parametresi bulunur. Bu faz slot/rezervasyon/onay/ödeme veya fotoğraf sağlamaz; menü henüz açılmadı.

## Değişiklikler ve kararlar

- Yeni `CommonArea`, Application DTO/service/store sözleşmesi, EF configuration/store ve API endpoints. Modüler monolith ve mevcut DI/audit/hata sistemi kullanıldı; yeni paket yok.
- Yönetim rolü + PropertyMembership, canonical modda mevcut aktif yönetim görevi/tenant/issuer/subject/hesap bağı kontrolü. Kullanıcı liste/detayları mevcut `IMyPortfolioStore.ResolveAsync` ile kendi aktif OWNER/TENANT/RESIDENT ilişkisine bağlıdır. Blok görünürlüğü sunucudaki Unit üzerinden; yabancı/pasif/süresi dolan bağlam404. Liste ve detay aynı scope'u korur; PII dönmez. Katalog okuma rezervasyon hakkı değildir.
- UUID+normalize fingerprint, Property kilidi, parent ve seçilen Building version koruması. Aynı UUID aynı içerik200/current replay; farklı içerik/aktör veya silinmiş geçmiş409. PUT/DELETE version kontrollü, kör retry yok.
- `20261026125717_CommonAreaDefinitions`: yalnız `common_areas` tablosu/index/FK/check; Designer+snapshot. Sekiz NOT NULL audit alanı, aynı Property Building composite FK. Dolu/soft-delete geçmişte Down ilk drop öncesi reddeder. Eski migrationlar değişmedi.
- Mevcut HierarchyStore Property/Building silmelerine ortak alan geçmişi engeli; diğer iş kuralları değiştirilmedi. Parent pasif olunca kullanıcı okuması kapanır.
- Yeni iki hata kodu ve TR/EN registry eşliği; mevcut ekran yol haritasına yalnız doğrulanmış OA-02 geliştirmesi eklendi. Rezervasyon/UI yapılmış gösterilmedi.
- Kullanım metni düz metindir. Saatler bu sürümde aynı yerel gün içinde dakika hassasiyetinde çift/null; gece aşımı/istisnai saatler ileride. Ücret tanımı tahsilat değildir. Yeni üçüncü taraf takvim/form/depo/ikon/görsel kullanılmadı.

## Bu tur çalıştırılan doğrulamalar

| Kontrol | Gerçek sonuç |
|---|---|
| `dotnet build backend/BuildingFlow.sln --no-restore -m:1 --disable-build-servers --verbosity minimal` | **GEÇTİ; 0 hata/0 uyarı** |
| Ayrı PostgreSQL DB'de `dotnet test ... --no-build --no-restore -m:1 --disable-build-servers --filter 'FullyQualifiedName~CommonArea\|FullyQualifiedName~PersistenceTests\|FullyQualifiedName~MigrationTests.IdempotentScript\|FullyQualifiedName~AuthenticationTests\|FullyQualifiedName~MaintenanceApiTests\|FullyQualifiedName~HierarchyApiTests\|FullyQualifiedName~PropertyTypeChangeTests'` | **100/100 entegrasyon + 8/8 domain, 0 atlanan** |
| Yalnız yeni modülün yukarıdaki toplam içindeki payı | **13 PostgreSQL/API/migration + 8 domain**; toplam108'e tekrar eklenmez |
| Web `npm test` | **260/260, 0 atlanan** |
| Web `npm run typecheck` | **GEÇTİ** |
| Web `npm run build` | **BAŞARISIZ**: mevcut `features/account/contact-verification.ts` → kök dışı `shared/contact-verification.ts` Turbopack çözümleme hatası; HEAD'de de aynı import var |
| Web `npm run build -- --webpack` | **GEÇTİ**, son roadmap/TR/EN değişiklikleri dahil üretim derlemesi |
| `python3 scripts/check-licenses.py` | **GEÇTİ, 329 npm/NuGet sürümü**; envanter içeriği aynı |
| `git diff --check` | **GEÇTİ** |

Gerçek PG kanıtı: UUID eşzamanlı oluşturma tek satır; replay güncel değeri korur; farklı payload409; stale version409; soft-delete/audit; negatif JWT/rol/üyelik/foreign ID; canonical yönetim görevi iptali; OWNER/TENANT bitiş günü dahil ve geçmişte biten ilişki; pasif hesap bağı/Property; aynı blok ve Property-wide görünürlük; gizli detay404. Migration: eski Property satır/audit JSON eşliği, upgrade/repeat, boş Down/reapply, dolu/soft-deleted Down reddi, DB FK/check, model/snapshot eşliği ve tüm uygulama tablolarının zorunlu kolon taraması; idempotent SQL iki uygulama mevcut regresyonda geçti.

Son TRX dosyaları `/tmp/bf-common-results/common-area_net10.0_20261026160510.trx` ve `...160511.trx`; komut/çıktı özetleri `/tmp/bf-common-{tests,build-escalated,web-all,typecheck,web-webpack,web-build,licenses}.log` altındadır. Bunlar yerel kanıttır; CI URL/commit/PR yok.

İlk yeni-test derlemesinde xUnit2031 düzeltildi. Güncel migration eklenmeden önce kalan eski derlemeyle yapılan ilk test koşusu kabul sayılmadı; yeni build ile tekrarlandı. Sonradan eklenen bitiş tarihi fixture'ı değişmez ilişkiyi ikinci kez değiştirmeye çalıştığı için2 test reddedildi; guard kaldırılmadan ayrı expired fixture ile düzeltildi, son100/100 geçti. İlk kök klasördeki npm çağrısı package.json bulunmadığı için çalışmadı; gerçek260/260 web dizininden alınmıştır. Sandbox SWC cache erişim hatası izinli ortamda tekrarlandı; kalan varsayılan build sorunu yukarıdaki mevcut Turbopack importudur.

## Lisans, güvenlik ve yayın sınırları

Manifest/lock/NuGet sürümü, eski migration, .env, canlı DB/Keycloak/hesap/servis değiştirilmedi. Testler mevcut PostgreSQL sunucusunda ayrı rol+geçici test DB ve rastgele fixture şemalarında çalıştı; canlı projeler DB'sine erişmedi. Test DB/rolü teslimde yalnız kendi kaynakları olarak temizlendi. Commit/push/deploy veya canlı migration yok. AutoMigrate açık sonraki API başlatması bekleyen migration'ı uygulayabilir.

Metadata taraması copyleft/container SBOM/pentest veya hukuki uygunluk sertifikası değildir. Mevcut Redis/Grafana/Loki/libvips dağıtım/NOTICE koşulları sürer; yeni lisans yükü yok. Yeni görsel/font veya tasarım PNG'sinin uygulamaya gömülmesi yok. Güvenlik tarayıcısı, canlı yük testi, gerçek browser/native UI testi ve mobil build çalıştırılmadı; bu faz yeni iş ekranı içermez. Varsayılan Turbopack build sorunu açık, mevcut kaynaklarına dokunulmadı.

## Teslim ve devam

Çalışma adımları3/3=%100,00 teknik teslim; kanonik FAZ02 checklist'i6/7=%85,71 (kullanıcı kabulü açık). Onaylanmış önceki analiz1/8=%12,50; bu faz teknik teslimdir, kullanıcı kabulü olmadan tüm akışta tamamlanmış faz sayılmaz. Issue #52 açık bırakılır; #51 bu tur kapatılmaz. Çalışma adımları resmi kabul maddelerinin yerine geçmez.

Rollback boş tabloda hedef bir önceki migration ile; veri varsa ileri düzeltme/izole restore. Kaynak geri alma git diff üzerinden yalnız bu tur dosyalarıyla yapılmalı; kullanıcıya ait izlenmeyen prompt klasörü korunmalı. Sonraki adım yalnız kullanıcı FAZ03'ü onaylarsa web UI. Rezervasyon hakkı/kota/onay/iptal/ücret tahsilatı ve bakım çatışması kendi fazlarında ayrıca belirlenir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Codex usage/rateLimitsByLimitId.codex 26.10.2026:300dk kullanılan%41/kalan%59,reset26.10.2026 20:28:41;10080dk%6/%94,reset02.11.2026 15:28:41 Europe/Istanbul. Hesap kotası sohbetin token tüketimi değildir.

## Teslim sonrası kullanıcı kararı — 26.10.2026

Yönetici ortak alan ve rezervasyon kaydı/düzenleme/iptal işlemlerini yapar; diğer kullanıcılar web/merkez ve mobilde yetkili alanları ve rezervasyon saatlerini salt okunur görüntüler. Halı saha örneğinde dolu/boş/bakım saatleri görünür, başkasının kişisel rezervasyon detayları görünmez. [Kanonik plan](../../BuildingFlow-OrtakAlanlar/MASTER_PROMPT.md) ve ilgili fazlar güncellendi. Bu ek yalnız yetki/UX kararıdır; uygulama kodu veya yeni faz geliştirmesi değildir. Önceki testler yeniden çalıştırılmış sayılmaz.
