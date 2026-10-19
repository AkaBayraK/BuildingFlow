# PERSON FAZ11 — Uçtan Uca Regresyon ve Yayına Hazırlık

18.10.2026. Kullanıcı FAZ10 teslimini kabul ederek son fazı başlattı; devam edip bitirme talebiyle teknik teslim tamamlandı. **Teknik adımlar 6/6 (%100); kullanıcı kabulü bekliyor. Kabul edilmiş fazlar 10/11 (%90,91). Yayına hazır sonucu verilmedi.** GitHub: [#16](https://github.com/AkaBayraK/BuildingFlow/issues/16).

## Etki ve gerçekleştirilen değişiklikler

Önceki raporlar, paket talimatları ve mevcut kişi/bağlam/mobil sözleşmeleri incelendi. FAZ10 güvenlik bulguları açık olduğundan bu teslim yalnız mevcut davranışın regresyonu ve yayın hazırlığıdır; aktivasyon, bağımlılık yükseltmesi veya üretim geçişi yapılmadı.

- `backend/tests/BuildingFlow.IntegrationTests/SessionContextTests.cs`: kiracı+sakin birleşiminde iki bağlam ve yönetim yetkisinin bulunmaması; başka bir yönetim üyeliği veya sakinlik olmadan iki farklı blokta iki daire sahibi uzaktan malikin bağlamları için iki HTTP regresyon testi eklendi.
- `frontend/buildingflow-web/src/features/person-center/CenterDetail.tsx`, `Choices.tsx` ve `session-workspaces/SessionWorkspaces.tsx`: tekrar dene/yenile düğmeleri mevcut mavi `view` standardına uyarlandı. İş mantığı ve yetki değişmedi.
- Bu rapor ve mevcut TESTING/progress/Skill kullanım kayıtları güncellendi. Bağımsız yeni backlog veya faz oluşturulmadı.

## Bu fazda çalıştırılan doğrulamalar

| Kontrol | Gerçek sonuç ve sınır |
| --- | --- |
| Seçili backend entegrasyon regresyonu | **366/366**, 0 başarısız/atlanan; ayrı PostgreSQL DB/rol ve fixture storage. Tüm backend suite olduğu iddia edilmez. |
| Web `npm test` | **149/149**, 0 başarısız/atlanan. İlk düğme standardı hataları düzeltildikten sonraki tam sonuç. |
| Web typecheck | `tsc --noEmit` başarılı. |
| Web production derlemesi | İzole kaynak kopyasında `next build --webpack` başarılı; çalışan uygulamanın `.next` dizinine dokunulmadı. İlk sandbox SWC cache erişim hatası izinli tekrar çalıştırmada giderildi. |
| Mobil `npm run check` | **126/126**, typecheck ve lint başarılı. |
| Mobil export | Kişi yetkilendirme ve sync bayrakları açıkken iOS/Android/web export başarılı; native binary/cihaz testi değildir. |
| Migration SQL | Mevcut dotnet-ef DLL ile idempotent SQL üretildi; izole DB'de **30 migration iki kez** başarıyla uygulandı. Canlı DB'ye uygulanmadı. |
| Yedek/restore | `scripts/test-backup-restore.py`: **8 kontrol başarılı**. Sahte veride 2 DB fingerprint, Keycloak realm/kullanıcı giriş kontrolü, 3 nesne SHA256 doğrulandı; bozuk/eksik/path traversal yedekler reddedildi. Fixture kaynakları temizlendi; canlı compose isim/durum/başlangıç zamanları aynı. |
| Özel runtime config | Dört yol için ignored+untracked kapısı ve 3 regresyon fixture başarılı. |
| Release preflight | Geçerli senaryo geçti; 29 güvensiz yapılandırma reddedildi. |

Geçici loglar `/tmp/bf-person11/` altındadır; kalıcı CI arşivi değildir. Unit244/244, telemetri6/6, web audit0/NuGet bildirilmiş0 ve lisans metadata sonuçları **FAZ10 kanıtıdır; FAZ11'de yeniden çalıştırılmış sayılmaz**. Tam CI çalıştırılmadı; mevcut mobile high audit kapısı FAZ10 bulgularıyla başarısızdır. Hiçbir başarısız güvenlik kapısı bastırılmadı.

Yedek provası mevcut fixture aracının PostgreSQL17.6 formatını doğrular. Yerel ortak altyapı PostgreSQL18.6 ve ayrı uygulama/Keycloak şemaları kullanır; bu sonuç üretim18.6 felaket kurtarma sertifikası değildir. Ortak altyapı için [BACKUP_RESTORE](../../BACKUP_RESTORE.md) içindeki güncel ortak PostgreSQL yönergeleri izlenmelidir.

## Senaryo matrisi

| Senaryo | Kanıt / kalan sınır |
| --- | --- |
| Yönetici+malik+sakin | SessionContext HTTP regresyonları; bağlam ve capability ayrımı. |
| Kiracı+sakin | Yeni `TenantAndResidentShareUnitWithoutInventingOwnershipOrManagement`; kişi yönetimi403, yönetim capability'leri false. |
| Uzaktan malik / çoklu daire-blok | Yeni `RemoteOwnerHasSeparateUnitsAcrossBlocksWithoutResidencyOrPropertyMembership`; iki OWNER bağlamı, yönetim alanına erişim yok. |
| Farklı tenant | Mevcut canonical/context HTTP negatif kapsam testleri; yabancı tenant bilgisi açılmaz. |
| Rol iptali offline | Sunucu sonraki push/replay/pull/photo sırasında güncel kapsamı reddeder; mobil cache/outbox sözleşmeleri. Tamamen offline cihaz iptali anında öğrenemez; lease süresi riski devam eder. |
| Çoklu cihaz / duplicate sync | Concurrent HTTP replay/fingerprint ve mobil SQLite/outbox testleri; iki gerçek cihaz üzerinde UAT yapılmadı. |
| Başarısız migration | Mevcut migration/model/Down koruma testleri ve idempotent SQL tekrarı; dolu üretim DB'sinde hata/restore denenmedi. |

API testleri gerçek test veritabanı kullanır; Node SQLite ve mobil port testleri SQLCipher/SecureStore veya fiziksel cihaz kanıtı değildir. Kişi kartı merkezi kaynakta korunur; OWNER/TENANT yönetim kişi listesi/yazma yetkisi kazanmaz. Tüm gerçek kişilerin Altunbayrak Rezidans'a ait olduğu kullanıcı kararı korunur; gerçek tenant/property UUID eşleştirmesi uydurulmadı.

## Kademeli yayın ve geri alma kontrol listesi

Bu liste uygulama talimatı değildir; aşağıdaki kapılar kapanınca yetkili operatör tarafından uygulanacak kabul planıdır. **Bu faz hiçbir bayrağı veya servisi değiştirmedi.**

1. **Giriş kapısı:** FAZ10 mobil açıkları ve geçmişte takipli erişim bilgisi bulgusunu kapat; kontrollü anahtar yenileme/geçmiş-klon incelemesi ve gereken commit incelemesini tamamla. SAST/history secret/native/container ve dağıtım lisans/NOTICE kabulünü belgele. Bulgular açıkken yayın durur.
2. **Envanter ve staging:** Altunbayrak Rezidans için gerçek tenant/property/account-person/görev/ilişki eşleşmelerini yetkili operatör doğrulasın; belirsiz kayıtlar otomatik eşleştirilmesin. Ayrı staging'de önceki sürüm ve yeni sürüm/veri sözleşmesini doğrula. Global bayrakları tenant bazlı canary olarak sunma; gerekirse ayrı staging deployment kullan.
3. **Yedek:** bakım penceresinde tüm ilgili yazıcıları durdur; ortak PostgreSQL18.6'nın güncel backup prosedürü ve SeaweedFS nesne/metadata yedeğini tutarlı aynı pencereye bağla. Erişimi kısıtla, checksum ve sürüm manifestini sakla. Gerçek yedeği yeni izole18.6 hedefe geri yükleyip DB, Keycloak oturumu ve nesne fingerprint'lerini doğrula. Mevcut fixture provası bu adımın yerine geçmez.
4. **Bayrak sırası:** API `PersonRefactor__Enabled`, `TransitionEnabled`, `AssignmentsEnabled`, `UnitRelationWritesEnabled`, `SessionContextsEnabled`, `MobileAuthorizationEnabled`, `MobileSyncEnabled` ilgili `PersonRefactor__` önekiyle başlangıçta false kalır. Belgeli bağımlılıklar sağlandıktan sonra ayrı staging'de önce merkezi kişi/eşleme ve salt okunur bağlam, ardından yönetim yazımları, mobil readonly yetkilendirme, en son mobil sync kabul edilir. Tüm hesapların geçişi hazır olmadan global context açılmaz; legacy ve canonical arasında çift yazıcı oluşturulmaz.
5. **İstemci:** web `BUILDINGFLOW_PERSON_CENTER_ENABLED` / `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED`; mobil `EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED` / `EXPO_PUBLIC_PERSON_SYNC_ENABLED` dağıtım ve build kapsamıyla birlikte doğrulansın. Mobil public bayrak değişimi yeni build/update kabulü gerektirir; FaceID yapılandırması yeni native binary ister. Sunucu kapısı tek güvenlik sınırı olarak korunur.
6. **İzleme ve geri alma:** yetki403/404, duplicate409, senkronizasyon başarısızlığı ve rol iptalini kişisel veri içermeyen mevcut telemetriyle izle. Sorunda önce yeni mobil sync yazımlarını kapat; istemcide başka context'e veya legacy kuyruğa otomatik taşıma yapma. Web/API context bayrakları uyumlu kapatılır; legacy çevrimiçi dönüşte tekrar kimlik doğrulama ve yetki kontrolü gerekir. Bekleyen taslakları koru. Dolu şemada körlemesine Down çalıştırma; eklemeli şemada ileri düzeltme veya onaylı sürümle yeni hedefe restore planı kullan. Veri silen rollback onayı bu teslim kapsamında verilmedi.

## Kullanıcı kabulü — henüz yapılmadı

- [ ] Yönetici tüm yetkili kişileri görür; malik/kiracı kendi bilgisi dışında kişi listesine erişemez ve merkezi kişi kartını değiştiremez.
- [ ] Yönetici+malik+sakin, kiracı+sakin ve uzaktan malikte doğru bağlam seçimi; iki blok/daire ayrımı.
- [ ] Gerçek hesaplarla yabancı tenant negatif erişim ve tarih sonu/rol iptali kontrolü.
- [ ] İki fiziksel cihazda duplicate/retry, offline iptal/lease sonu, yeniden giriş ve kayıpsız bekleyen taslak kontrolü.
- [ ] Native SQLCipher/SecureStore/biyometri, picker/modal/background/screenshot gizlilik kabulü.
- [ ] Ortak PostgreSQL18.6 + Keycloak + nesneler için operatör kontrollü gerçek yedek/izole restore ve geri alma onayı.
- [ ] Güvenlik/lisans bulgularının kapandığı kanıt ve son yayın onayı.

## Açık riskler ve teslim

FAZ10 mobil audit **29 kayıt:18 high/11 moderate,4 kök advisory** (braces, node-forge, decode-uri-component, uuid) kapanmadı. Uyumsuz major yükseltme/audit fix yapılmadı. Daha önce takipli runtime config'in yalnız indeksten çıkarılması geçmişteki bilgileri geçersiz kılmaz; gerçek dosya/.env korundu, rotasyon ve geçmiş incelemesi yapılmadı. Fortify/Semgrep/Gitleaks/TruffleHog/OSV/Trivy/Syft ile eksik tam taramalar, native MASVS/anahtarın biyometriye kriptografik bağı, binary/container NOTICE ve ticari dağıtım kabulü açık. Ayrıntılı kanıt [FAZ10 raporu](phase-10-report.md).

Yeni paket, migration, ürün yetkisi veya DB değişikliği yapılmadı. Eşzamanlı WEBSITE çalışması korunur; depo çapında dirty değişiklikler bu faza mal edilmez. Commit/push/deploy/gerçek yedek/servis yeniden başlatma yapılmadı. Sonraki faz yoktur. Teknik teslim tamamlandı; #16 In review, son kullanıcı kabulü ve yayın kapıları beklenir.
