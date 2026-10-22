# BuildingFlow — Çoklu Yönetim Alanı, Malik/Kiracı Web + Mobil Fazları

## FAZ 00 — Analiz, etki haritası (KOD DEĞİŞTİRME)
- Mevcut Person filtrelerinin yönetim alanı bağımlılığını bul; tenant boundary, mevcut ilişkiler, Keycloak account-person eşleştirmesi, web/merkez ekranları, mobil SQLite/sync ve ödeme akışlarını incele.
- İlgili DB tabloları, API rotaları, frontend sayfaları, mobil ekranları ve testleri tablo halinde çıkar.
- Veri taşıma, mükerrer Person birleştirme (otomatik birleştirme yapma), rollback ve geçiş risklerini raporla.
- `docs/refactor/multi-property/00-analysis.md` oluştur. Kod değiştirme; onay bekle.

## FAZ 01 — Domain model ve migration tasarımı
- Mevcut tenant kapsamı içinde tekil Person, Unit -> Building/Block -> ManagementArea hiyerarşisi.
- Ownership, LeaseContract + LeaseParty, Occupancy ilişkilerini mevcut modellere adapte et; ilişki başlangıç/bitiş, status, verifiedBy, audit, soft-delete, concurrency alanları.
- Aynı kişi birden çok unit ve area ile; bir unit çok malik/ortak kiracı ile ilişkilendirilebilir. Geçerlilik tarihi ve örtüşme politikası tasarla.
- İlişki tiplerini ayrı domain kavramları olarak koru, UI için read model oluştur. Migration script + dry-run + rollback tasarla, production'a uygulama.
- Domain testleri ve backward compatibility testleri.

## FAZ 02 — Backend ilişkiler ve kişi listesi API
- Yetkili kapsamda yönetim alanı seçmeden kişiler listesi: pagination, search, sorting, masking, filtering. SystemAdmin tüm yetkili tenant'lar; manager yalnız yetkili kapsam; malik/kiracı yalnız kendi profili.
- Person detail -> portfolio (management area, building, unit, ownership/lease/occupancy), ilişki CRUD yalnız yetkili yönetici.
- ManagementArea -> Buildings -> Units bağımlı seçim API'leri. Birim ilişkisi oluştururken server-side validasyon ve audit.
- Liste sorgularında N+1, duplicate ve cross-tenant leakage testleri.

## FAZ 03 — Kimlik, çoklu rol ve aktif bağlam
- Keycloak user -> verified Person bağı; global admin/manager rollerini unit bazlı owner/tenant/occupant ilişkilerinden ayır.
- `GET /me/portfolio` benzeri mevcut standartlara uygun endpoint: yalnız yetkili alanlar/birimler ve eylem yetkileri.
- Active context = {managementAreaId, unitId, capacity/relationshipId}; server her eylemde doğrular. Seçim değiştiğinde cache/query invalidation.
- Başka unitId, revoked relationship, inactive lease, cross-tenant IDOR negatif testleri.

## FAZ 04 — Merkez yönetim ekranı
- Kişiler listesi açıldığında yönetim alanı seçimi isteme; yetkili kişi listesi ve isteğe bağlı filtreler.
- Kişi detayında `Bağımsız Bölüm İlişkileri`: portföy tablosu, yönetim alanı -> blok/bina -> daire seçimi, ilişki tipi, geçerlilik, belge ve doğrulama durumu.
- Malik, kiracı ve fiili sakin ilişkilendirme için ayrı domain iş akışları; geçmiş ilişkileri sonlandırma; soft delete.
- Aynı kişi için birden fazla yönetim alanı, aynı sitede birden çok daire, ortak malik/kiracı senaryolarını UI testleriyle doğrula.

## FAZ 05 — Malik/Kiracı merkez web portalı
- Aynı giriş ile `Portföyüm`: yönetim alanlarına göre gruplu daire kartları, seçili daire ve aktif kapasite/rol.
- `Tüm Dairelerim` özeti ve `Seçili Daire` detay modları; aidat, ödeme, belgeler, duyurular, talep/arıza ve sözleşme görünürlüğü permission matrix'e göre.
- Malik ve kiracı kendi ilişkilerini görüntüler; yalnız izin verilen profil/talep/belge işlemlerini yapar; ilişkiyi kendileri onaysız değiştiremez.
- Seçili daire değiştirilince stale veri gösterme; role-specific eylemler, empty/loading/403.

## FAZ 06 — Mobil portföy ve navigasyon
- Mobil teknoloji/repo keşfine göre mevcut navigasyonu refactor et. Login -> yetkili `Portföyüm` -> alan -> daire -> rol seçimi; tek ilişki varsa güvenli varsayılan seçim.
- Her ekranda seçili alan/daire göstergesi ve hızlı değiştirme; tek account, çok area/unit, çok ilişki.
- Yönetici + malik + kiracı birleşik rollerini ayrı çalışma bağlamları olarak göster; mobil tablar role/capability driven.
- Tasarım: premium, sade, büyük dokunma alanları, açık durum, erişilebilir, platform geri navigasyonu; aynı anda iki dairenin verisi karışmasın.

## FAZ 07 — Mobil SQLite, offline ve sync
- Per-account scoped cache: authorized portfolio, area/unit metadata, izinli belgelerin metadata'sı, offline talepler. Hassas veriyi minimize et; şifreleme ve secure storage kontrolü.
- Offline modda son doğrulanmış yetkili portföyün güvenli, sınırlı görünümü; oturum süresi/erişim iptali politikası. Düz metin şifre saklama yok.
- Offline talep oluşturma -> pending queue -> idempotent sync -> server reauthorization; revoked access conflict handling. Ödeme, sözleşme onayı ve güvenlik hassasiyetli işlemler online.
- Logout/account switch -> veri izolasyonu ve uygun cache temizliği; migration/version tests.

## FAZ 08 — İş modüllerinin bağlam entegrasyonu
- Aidat/tahakkuk/borç görüntüleme, online ödeme, belge, duyuru, arıza/talep, bildirim, anket ve raporlar için unit/area/role scoping.
- Birden fazla daire için toplu portföy görünümü; finansal toplamların açık kapsam etiketi ve mükerrer sayım koruması.
- Borç yükümlülüğünü salt UI rolünden türetme; yönetim planı/sözleşme/kanuni sorumluluk kurallarını mevcut finans modülüne uygun modelle.
- Bildirim tıklanınca doğru unit context'e geçiş; yetki yeniden kontrolü.

## FAZ 09 — Güvenlik, regresyon ve rollout
- Unit/area tenant isolation, IDOR, role escalation, stale offline permission, KVKK masking, audit, concurrency, migration dry-run ve rollback testleri.
- API integration, frontend e2e, mobile offline/online/switch account, payment-online-only ve erişilebilirlik kontrolleri.
- Feature flag ile kademeli geçiş, backfill raporu, gözlemlenebilirlik, eski API uyumluluğu ve release checklist.
- Gerçek prod deploy/migration/ödeme tetiklemeden önce kullanıcı onayı iste.
