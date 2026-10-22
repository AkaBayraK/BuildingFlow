# BF-MULTI FAZ 03 — Kimlik, çoklu rol ve aktif bağlam

22.10.2026 (Europe/Istanbul). GitHub [#35](https://github.com/AkaBayraK/BuildingFlow/issues/35). Plan toplamı 10 faz (FAZ 00–09); bu faz listede 4/10. **Durum: teknik teslim, kullanıcı onayı bekleniyor. FAZ 04 başlatılmadı.** Mevcut `/api/v2/session/contexts`, `/api/v2/mobile/authorization`, `ManagementAccess` ve legacy kişi kodu altyapısı (HMAC hasher, deneme kilidi, kimlik eşleşmesi) temel alındı; yeniden yazılmadı.

## 1) Yapılan değişiklikler ve dosya yolları

| Katman | Dosya | Değişiklik |
| --- | --- | --- |
| Domain | `backend/src/BuildingFlow.Domain/CentralPerson.cs` | `CentralPersonLinkCode` (HMAC özeti, ipucu, 72 sa süre, tek kullanım, iptal) |
| Application | `PersonAccountLinks.cs` (yeni) | `PersonAccountLinkService`: kod üretimi (çakışmada yeniden dene), bağlama (kilit, ortak tek tip ret) |
| Application | `MyPortfolio.cs` (yeni) | `MyPortfolio`, `ActiveContext`, `PortfolioCapabilities` yetenek matrisi ve `contextKey` |
| Application | `PersonCenter.cs`, `ErrorCatalog.cs`, `MobileOnboarding.cs` | `PersonAccountStatus.CodeHint/CodeExpiresAt`; `BF_PERSON_LINK_UNAVAILABLE`; kimlik normalizasyonu paylaşıldı |
| Infrastructure | `PersonAccountLinkStore.cs` (yeni) | Kod üret/bağla/sonlandır; müşteri satırı kilidi |
| Infrastructure | `SessionContextStore.cs` | Bağlamlar yalnız VERIFIED/LEGACY ve etkin sözleşmeli kiracılıkla; `PortfolioAsync`, `ResolveAsync` |
| Infrastructure | `CentralPersonConfigurations.cs`, `PersonCenterStore.cs`, `DependencyInjection.cs` | Mapping, kod ipucu, DI |
| Api | `PersonCenterEndpoints.cs`, `SessionContextEndpoints.cs`, `Program.cs` | Yeni rotalar |
| Migration | `20261022091337_CentralPersonLinkCodes{,.Designer}.cs`, snapshot | Ekleyici |
| Frontend sözleşme | `error-codes.ts`, `messages/tr.json`, `messages/en.json` | `BF_PERSON_LINK_UNAVAILABLE` |
| Test | `MyPortfolioTests.cs` (yeni, 4), `PortfolioCapabilitiesTests.cs` (yeni, 2), `TestRelations.cs` (yeni yardımcı); `SessionContextTests`, `MobileAuthorizationTests`, `MobileCanonicalSyncTests` fixture ilişkileri LEGACY işaretlendi | |
| Belge | bu rapor, `docs/api/person-center.md`, `docs/api/session-workspaces.md`, `docs/api/errors.md`, `docs/TESTING.md`, `docs/progress/*` | |

Kararlar:

- **Hesap ↔ kişi bağı yalnız yönetici onaylı kodla.** Kod HMAC ile saklanır; ad/soyad/telefon yalnız ek kontrol. Legacy akışla aynı 6 hane politikası ve **ortak** 5 deneme/15 dk kilit sayacı (iki akış ayrı ayrı denenerek kilit aşılamaz). Bir kişinin ve bir hesabın müşteri başına tek aktif bağı (mevcut partial unique).
- **Yönetici rolleri ile daire ilişkileri ayrı:** `/me/portfolio` daire bağlamlarını `units`, görev/platform bağlamlarını `workspaces` altında ayrı verir; daire kapasitesi hiçbir yönetim yeteneği vermez.
- **Doğrulama yaptırımı:** FAZ 01'den beri yazılan PENDING/REJECTED ilişkiler artık bağlam/mobil yetki/portföy üretmez; pasif sözleşmeye bağlı kiracılık da üretmez. Üretimde yazım yolları VERIFIED (yönetici) veya LEGACY (geçiş/backfill) yazdığı için mevcut davranış korunur; yalnız doğrudan oluşturulan test fixture'ları LEGACY'ye işaretlendi.

## 2) Yeni/etkilenen endpointler

Yeni: `POST /api/v2/tenants/{t}/people/{p}/account-link-codes`, `POST …/account-link/end`, `POST /api/v2/me/person-links`, `GET /api/v2/me/portfolio`, `GET /api/v2/me/active-context`. Ayrıntı ve önbellek/sorgu geçersizleştirme sözleşmesi `docs/api/session-workspaces.md`.

Etkilenen: `/api/v2/session/contexts*` ve `/api/v2/mobile/authorization` PENDING/REJECTED ilişki ile pasif sözleşmeli kiracılığı artık bağlam olarak göstermez. `account-status` ek alanları `codeHint`, `codeExpiresAt`.

## 3) Yetki ve KVKK etkisi

- Kod yanıtı `no-store`; düz kod DB'de, logda veya sonraki yanıtlarda yok (test DB satırında kod aranır, bulunmaz). İpucu yalnız 2 hane.
- Tüm ret nedenleri tek koddur; endpoint hangi kişinin/kodun var olduğunu sızdırmaz.
- `/me/*` yalnız aktörün bağlı kişileri; başka kişinin adı, kimliği veya ilişki id'si dönmez; kimlik maskeli.
- Aktif bağlam istemciden gelen kimliklerle sadece aranır; yetki her istekte güncel DB'den türetilir. Bağ sonlandırılınca bağlamlar anında kaybolur.
- Kod yalnız yöneticinin yetkili alanında güncel ilişki/görevi olan kişiye üretilir; müşteri dışı kişi 404.

## 4) Migration/rollback durumu

`20261022091337_CentralPersonLinkCodes` (yalnız ekleme): `person_link_codes` tablosu, composite kişi FK, hesap FK'ları, açık kod için kişi ve özet başına partial unique, durum check'i. Dolu tabloda Down hata verir. Canlı DB'ye uygulanmadı; `.env`, Keycloak, bayraklar değişmedi; yeni paket yok. Development AutoMigrate açıksa yerel API bekleyen üç BF-MULTI migration'ını uygulayabilir.

## 5) Test komutları ve sonuçları

Ayrı geçici PostgreSQL (`../postgres-18.6/scripts/test-db.py`), sonunda silindi; S3/RabbitMQ başlatılmadı.

| Komut | Sonuç |
| --- | --- |
| `dotnet test BuildingFlow.sln --filter "MyPortfolio\|PortfolioCapabilities\|LeaseContract\|PersonPortfolio\|PersonCenter\|PersonRelation\|PersonTransition\|SessionContext\|CentralPe\|PersistenceTests\|ErrorContract\|MigrationTests\|MobileAuthorization\|ManagementAssignment\|MobileSession\|PersonCodeApi\|MobileOnboarding\|MobileCanonicalSync\|ReleaseSecurity\|PersonUnitRelationTests"` | Entegrasyon **223/224**, unit **17/17**. Tek kırmızı `MobileCanonicalSyncTests.PhotoIsOptionalConsentProtectedAndBoundToCurrentContext` — S3 başlatılmadı (bilinen baseline, önceki fazlarda 503) |
| `dotnet test tests/BuildingFlow.UnitTests` | **295/295** |
| `npm test` (web) / `npm run typecheck` | **221/221** / PASS |
| `bash scripts/db.sh check` | fark yok |
| `bash scripts/check-idempotent-sql.sh` | PASS (39 migration, iki kez) |

Negatif senaryolar: yanlış ad/telefon, kod tekrar kullanımı (başka hesap), iptal edilen eski kod, süresi dolmuş kod, 5 hatadan sonra doğru kodla kilit, bağlı kişiye yeni kod 409, ilişkisiz kişi 409, yabancı müşteri 404, daire/muhasebe rolleri 403, tanımsız alan 400, aynı hesapla aynı müşteride ikinci kişi; aktif bağlamda başka daire, başka alan, yanlış kapasite, başka kişinin ilişkisi, yabancı müşteri ilişkisi, bitmiş ilişki, PENDING, REJECTED, pasif sözleşme, bağ sonlandırma sonrası erişim (hepsi 404), biçim hataları 400; VERIFIED kararın PENDING'e geri alınamaması; fingerprint değişimi; kapalı bayrak 404.

## 6) Bilinen riskler

1. 6 haneli kod alanı legacy ile aynı; dağıtık çok hesaplı deneme saldırısına karşı ek koruma mevcut kimlikli istek sınırı (`sub` bazlı 429) ve 72 sa süredir. Daha uzun kod istenirse ayrı karar.
2. Kod kullanıcıya kanal dışı (yüz yüze/SMS) iletilmeli; SMS gönderimi bu fazda bağlanmadı.
3. Yetenek matrisi niyet beyanıdır; modül uçları (aidat, belge, duyuru, talep) henüz bu bağlamı kullanmıyor (FAZ 08).
4. Web/mobil istemcilerin önbellek sözleşmesini uygulaması FAZ 05/06/07 işidir.
5. Bayraklar kapalı; `SessionContextsEnabled=true` açılışı tüm yöneticiler için ENFORCED moduna geçer (00-analysis §6.5).

## 7) Sonraki faza geçiş koşulu

Kullanıcı bu raporu onaylarsa FAZ 04 (#36, merkez yönetim ekranı) başlar. Onay gelmeden FAZ 04'e geçilmez.
