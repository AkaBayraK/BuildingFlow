# BF-MULTI FAZ 09 — Güvenlik, regresyon ve kademeli geçiş

22.10.2026. GitHub [#41](https://github.com/AkaBayraK/BuildingFlow/issues/41). Plan 10 faz; listede 10/10 (son faz). **Teknik teslim, onay bekliyor.** Prod deploy, gerçek migration, bayrak değeri, `.env`, Keycloak ve canlı DB değişikliği yapılmadı; bunlar ayrıca kullanıcı onayı ister. #42 (mükerrer kişi/doğrulama/belge) kullanıcı kararı bekliyor.

## 1) Bu fazdaki değişiklikler
- Mobil `src/notices/contracts.ts` `noticeContextSwitch` + `src/components/NoticeScreen.tsx`: bildirim bağlantısı `contextKey` taşıyorsa uygulama yalnız güncel yetki kopyasında bulunan bağlama geçer, sonra mevcut akış sunucuda yeniden doğrular; iptal edilmiş bağlamda "Bu bildirimin dairesine artık erişiminiz yok." Test: `tests/notices.test.mjs` +1.
- Belgeler: bu rapor, TESTING, progress.

## 2) Güvenlik regresyonu (BF-MULTI genel)
Mevcut negatif test kapsamı (FAZ 01–08, ayrı PostgreSQL): tenant/alan/daire izolasyonu (yabancı müşteri/alan/blok/daire/kişi 404), IDOR (başka ilişki id'si, yanlış kapasite, başka daire), rol yükseltme (malik/kiracı/sakin/muhasebeci yönetici uçlarında 403; daire bağlamı MANAGE_* vermez), eski/iptal yetki (bitmiş, PENDING, REJECTED ilişki, pasif sözleşme, sonlandırılmış hesap bağı 404; mobilde 24 sa/1 sa yetki kopyası, iptal bağlam kuyruğu `blocked`), KVKK maskeleme (dizin son 4 hane, portföy/sözleşmede başka kişi yok, kod tek gösterim, önbellekte isim yok), audit (BaseEntity audit, değişmez makbuz tabloları), eşzamanlılık (tek sözleşme yarışı API+DB, version/409, UUID replay). Bu fazda ek eksik bulunmadı; yeni negatif test yalnız mobil bildirim geçişi.

## 3) Migration kontrolü
BF-MULTI migration'ları (`20261022083050_LeaseContractsAndRelationVerification`, `20261022085436_LeaseContractExclusivityAndOperations`, `20261022091337_CentralPersonLinkCodes`) için izole şemada yükseltme, tekrar uygulama, LEGACY backfill, boşta Down ve dolu Down reddi `LeaseContractPersistenceTests` ile; idempotent SQL iki kez uygulama `check-idempotent-sql.sh` ile. Sonuçlar §6.

## 4) Yeni modelde kişiye bağlı tahakkuk — tasarım (kodlanmadı)
Sorun: `DuesChargeParty` borçlu snapshot'ı yalnız legacy `Person` (`people`) satırına FK verir; native `CentralPerson` borçlu olamaz. Öneri:
1. Ekleyici `dues_charge_parties.central_person_id` (nullable, composite FK `(tenant_id, central_person_id)` → `persons`) + `relation_kind/relation_id` snapshot'ı (UnitOwnership/UnitTenancy id); check: tam olarak biri (legacy `person_id` veya `central_person_id`) dolu.
2. Tahakkuk servisi native ilişkilerden muhatap seçer (06.1 kuralları aynen: dönem başı ilişki, tek ilişki/Unit, belirsizse atomik ret); legacy borçlara backfill yok.
3. Okuma: `MyUnitStore.Charges` legacy eşleşmeye ek olarak `central_person_id = bağlam kişisi` koşulu.
4. Down: dolu kolonda reddet. Testler: native kişi borcu yalnız kendi bağlamında, legacy değişmez.
Risk: finans geçmişi; ayrı faz/onay ile.

## 5) Kademeli geçiş planı (yalnız plan)
**Bayrak sırası** (hepsi bugün kapalı): 1) `PersonRefactor:Enabled` (okuma uçları) → 2) `PersonRefactor:AssignmentsEnabled` + yönetici görevleri ve hesap bağları hazır → 3) `PersonRefactor:SessionContextsEnabled` (ENFORCED; tüm yöneticiler için görev+hesap bağı ön koşul) → 4) `PersonRefactor:UnitRelationWritesEnabled` (yalnız legacy geçmişi olmayan alanlar) → 5) mobil `EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED`, `PersonRefactor:MobileAuthorizationEnabled` → 6) `EXPO_PUBLIC_PERSON_SYNC_ENABLED`, `PersonRefactor:MobileSyncEnabled`. Her adım tek müşteri/alan için `PersonRefactor:ManagementModes:{tenant}` LEGACY→SHADOW→ENFORCED ile.
**Legacy→canonical:** alan başına `person-transition/preview` (dry-run hash) → kullanıcı onayı → `apply` (aynı hash, kilitli) → `comparison` drift 0. Hesap bağları: legacy `LinkedUserId` doğrulanmış bağları yönetici onaylı backfill ile `person_account_links` (veya kullanıcıya FAZ 03 kodu). Mükerrer kişiler #42 kararıyla.
**Backfill raporu:** alan başına kişi/ilişki sayısı, geçersiz profil, eşleşmeyen ilişki, hash; uygulamadan önce kullanıcıya sunulur.
**Gözlemlenebilirlik:** mevcut route şablonu/status metrikleri; yeni uçlar (`/api/v2/me/*`, lease, link) 403/404/409 oranları ve link kilidi sayısı izlenir; kişisel veri etiketlenmez.
**Eski API uyumu:** v1 uçlar değişmedi; v2 alanları ekleyici.
**Rollback:** bayrakları ters sırayla kapat; dolu şemada Down çalıştırma (guard reddeder), forward fix.
**Yayın kontrol listesi:** quiesced yedek + izole restore provası; `bash scripts/db.sh sql` incelemesi; migration önce staging; idempotent SQL; lisans PASS; audit/secret kapıları (aşağı); bayrak sırası; kabul listesi.

## 6) Kullanıcı kabul listesi (Ahmet senaryosu)
1. Yönetici Kişiler'i alan seçmeden açar, Ahmet'i bulur (maskeli kimlik). 2. Portföy: Örnek Sitesi A Blok D5 Kiraya verilmiş, B Blok D12 Kiracı oturuyor, Örnek Apartmanı D3 Malik oturuyor, D7 Kiracı (ikinci sözleşme). 3. D7'ye çakışan ikinci sözleşme `BF_LEASE_OVERLAP`. 4. Bağlama kodu üret → Ahmet girişte kodla bağlanır → Portföyüm. 5. D5 seçince kira sözleşmesi görünür, kiracı adı görünmez; D3 sakin rolünde aidat sekmesi yok. 6. Talep oluştur (D12) → yönetici görür. 7. Bağı sonlandır → Ahmet'in bağlamları hemen kapanır. 8. Mobil: çoklu bağlamda otomatik seçim yok; bildirim farklı daireye ait ise o daireye geçer, iptal edilmişse reddedilir.

## 7) Yayın kapısı — açık güvenlik bulguları
- Mobil `npm audit`: **24 açık (18 high, 6 moderate)** — FAZ 10'daki 18 high'dan beri kapanmadı; major yükseltme gerektirir, onaysız yapılmadı. **Kapı kapalı.**
- Web `npm audit --omit=dev`: **0**.
- SeaweedFS erişim anahtarı rotasyonu: kanıt yok, açık (gizli değerlere dokunulmadı). **Kapı kapalı.**
- Lisans: `check-licenses.py` PASS (329 paket, yeni paket yok).

## 8) Testler (bu tur)
| Komut | Sonuç |
| --- | --- |
| Backend tam entegrasyon (`dotnet test BuildingFlow.sln`, ayrı geçici PostgreSQL) | İlk koşu 535 geçti / 153 başarısız / 43 atlandı. Başarısızların 142'si `53300 remaining connection slots` (paylaşılan sunucu `max_connections=100`, paralel oturum yükü), kalanı aynı anda bağlantı tükenmesine bağlı eşzamanlılık testleri |
| Etkilenen 30 sınıfın yeniden koşusu (yeni DB, `xUnit.MaxParallelThreads=2`) | **228 geçti / 1 başarısız / 5 atlandı** — başarısız: S3 gerektiren bilinen `MobileCanonicalSyncTests` fotoğraf testi; atlananlar S3/RabbitMQ `[StorageFact]` vb. (geçti sayılmaz) |
| Backend unit | **301/301** |
| `db.sh check` / idempotent SQL | fark yok / **PASS (40 migration, iki kez)** |
| Web `npm test` / typecheck | **237/237** / PASS (bu fazda web kodu değişmedi; build FAZ 08'de PASS) |
| Mobil `npm run check` / build | **131/131**, lint/typecheck temiz / PASS |
| Lisans `check-licenses.py` | PASS (329 paket) |
| `npm audit` web prod / mobil | 0 / **24 (18 high, 6 moderate)** |

## 9) BAKIM — geçiş paneli kaldırıldı (22.10.2026)
Kullanıcı aktarımı uyguladıktan sonra (DB: 4 kişi, 4 kişi eşlemesi, 3 ilişki eşlemesi) "Eski kişi kayıtlarını aktar" paneli Kişiler ekranından kaldırıldı: `features/person-center/TransitionPanel.tsx` silindi, `CenterList.tsx` import/render temizlendi, `personCenter.transition*` TR/EN anahtarları silindi. Backend person-transition uçları kodda duruyor; yerel `.env`'den `PersonRefactor__TransitionEnabled` satırı kaldırıldı ve API yeniden başlatıldı (uç 404). Test: web `npm test` **238/238**, typecheck PASS; tarayıcı `/people` alan seçimsiz 4 kişi, `/properties/{id}/people` eski ekran 4 kişi, konsol hatası 0.
