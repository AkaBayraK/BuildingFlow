# BF-VERIFY FAZ 01 — Ortak model, API ve kalıcılık teslimi

Tarih:22.10.2026. Görev:[#44](https://github.com/AkaBayraK/BuildingFlow/issues/44). Plan sırası02/08.
Kullanıcı **ilk fazdan başlayabilirsin** mesajıyla ilk uygulama fazını yetkilendirdi. FAZ02–07 başlamadı.
Durum: **TAMAMLANDI — #44 Closed(completed), Project3 Done;12/12 madde tamam,son okumayla doğrulandı.**

## Yapılanlar — kabul maddesi bazında

1. **P01.A1 — ADR ve sözleşme:** [01-contract.md](01-contract.md) otorite/durum/değer/null/DTO,eski v1 istemci semantiği,aday/kanıt farkı,kilit sırası,UUID replay ve rollout sınırını belirliyor. Native e-posta gönderimi02,OTP03,UI04–05;bu faz yalnız ortak backend temelidir. D1–D3 politikasını onaysız devreye almaz.
2. **P01.A2 — Hesap düzeyinde model:** `Domain/AccountPhoneVerification.cs` hesap başına tek kanıt/aday satırı;Property/Person/Unit kimliği yok. E-posta kaydı yerelde kopyalanmadı,`IKeycloakAdmin.GetAsync(sub)` otorite. Doğrulama kişi-hesap/daire yetkisi üretmez. HTTP'de doğrulandı boolean'ı yazılamaz.
3. **P01.A3 — Değer yaşam döngüsü:** Telefonun mevcut doğrulanmış değeri ve yeni adayı ayrı. Aynı normalize değerde isim/biçim/fotoğraf kaydı kanıtı veya contact version'ı sıfırlamıyor. Yeni aday eski kanıtı koruyor;eski doğrulanmış değere dönüş adayı iptal ediyor;açık numara kaldırma ikisini de kaldırıyor. E-posta adresi değişim workflow'u plan gereği FAZ02'dedir;bu fazda uygulanmış sayılmaz.
4. **P01.A4 — Eski yazım yolları:** `MobileProfileStore.SaveCore` içinde telefon gerçekten değiştiğinde ortak transaction/kilit altında `ObserveProfileChangeAsync` çağrılıyor. Eski `/account/profile/commands` ve `/mobile/profile/commands` payload/yanıt şekilleri değişmedi;eski istemci yeni numaraya eski kanıtı taşıyamıyor. İsim veya yalnız yönetici fotoğraf işlemi adayı/kanıtı değiştirmiyor. İlk kayıt numarası kanıtsız olduğundan salt GET'te UNVERIFIED;GET veri yazmıyor.
5. **P01.A5 — Ortak API ve yarış/replay:** `Application/ContactVerification.cs`,`Infrastructure/ContactVerificationStore.cs`,`Api/ContactVerificationEndpoints.cs`;iki GET ve iki aday POST aynı store. Kimlik JWT sub'dan;üyelik/alan seçimi yok. Profil ile aynı `profile:owner`→`owner:operation` kilit sırası. Stale version/farklı içerik409;normalize eşdeğer UUID tekrarında tek etki. Eski makbuz yeni adayı geri yazmaz,güncel durumu okur. Mevcut BF_USER_INVALID(phone)/BF_MOBILE_SYNC_CONFLICT kodları ve TR/EN sözleşmesi kullanılır. Bilinmeyen JSON alanları reddedilir;gövde4KiB,JSON415 limitleri var.
6. **P01.A6 — Kalıcılık:** `ContactVerificationConfiguration.cs` ortak BaseEntity mapping,sekiz NOT NULL audit/version alanı,owner unique index,proof/pending çiftleri için check constraint. Migration `20261022142952_AccountPhoneVerificationFoundation.cs`+Designer+snapshot,yalnız yeni tablo;eski veriye verified backfill yok. Down herhangi bir satır varsa (soft-delete dahil) veri silmez,reddeder.
7. **P01.A7 — Gerçek PostgreSQL:** Yeni `ContactVerificationMigrationTests` önceki migration'dan telefonlu eski profile yükseltme,tekrar uygulama,boş tablo Down/yeniden Up,unique/check constraint ve dolu/soft-delete Down korumasını test ediyor. Mevcut `MigrationTests` ilk kurulum/yükseltme/idempotent SQL ve `PersistenceTests` tüm uygulama tablolarının model+gerçek DB sekiz alan/audit sözleşmesini bu yeni entity dahil taradı.
8. **P01.A8 — Kanıt ≠ gönderim:** E-posta UNKNOWN/available=false olduğunda sahte onay yok;eski JWT onayına güvenilmiyor. Telefon DTO'sunda `canSendCode=false`,`deliveryStatus=NOT_REQUESTED`,`challengeId=null`. Aday hazırlama SMS/e-posta göndermez. OTP challenge/taşıma gerçek faz03'te;buradaki domain Confirm yalnız trusted handler/test yüzeyidir,HTTP endpoint'i yoktur.

## Değişen kaynaklar

- Yeni: `backend/src/BuildingFlow.Domain/AccountPhoneVerification.cs`.
- Yeni: `backend/src/BuildingFlow.Application/ContactVerification.cs`.
- Yeni: `backend/src/BuildingFlow.Infrastructure/ContactVerificationConfiguration.cs`,`ContactVerificationStore.cs`.
- Yeni: `backend/src/BuildingFlow.Api/ContactVerificationEndpoints.cs`.
- Bağlandı: API `Program.cs`,infra `DependencyInjection.cs`,eski `MobileProfileStore.cs`.
- Yeni migration+Designer ve mevcut model snapshot'ı;önceden uygulanmış migration'lar değiştirilmedi.
- Testler: `AccountPhoneVerificationTests.cs`,`ContactVerificationTests.cs`,`ContactVerificationMigrationTests.cs`.
- Belgeler: account/mobile-profile API,bu ADR/rapor,prompt güncel yetkisi,TESTING/progress/skill kullanım kaydı.

## Yeni endpointler ve sınırları

| Uç | İşlem |
| --- | --- |
| GET `/api/v1/account/contact-verification` | Kendi ortak durumu |
| GET `/api/v1/mobile/contact-verification` | Aynı durum/otorite |
| POST `/api/v1/account/contact-verification/phone` | Yalnız telefon adayı hazırlama |
| POST `/api/v1/mobile/contact-verification/phone` | Aynı komut/replay/versiyon |

Bu uçlar sahiplik onayı üretmez,gönderim yapmaz. E-posta durumu gerçek IdP kaynağını okur;erişim yoksa UNKNOWN. Web/mobil ekranları bu fazda değiştirilmedi.

## Bu tur gerçek doğrulama

Komutlar (`--no-restore`):

```text
dotnet build backend/BuildingFlow.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --verbosity minimal
dotnet ef migrations add AccountPhoneVerificationFoundation --project src/BuildingFlow.Infrastructure --output-dir Migrations --no-build
dotnet test backend/BuildingFlow.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter '<aşağıdaki seçili sınıf/metotlar>' --logger 'console;verbosity=minimal'
```

Filtre: `AccountPhoneVerificationTests|ContactVerificationMigrationTests|ContactStateIsShared|LegacyProfileWrites|ContactEndpointsReject|NewUserPhone|NewUserProfile|BuildingFlow.IntegrationTests.PersistenceTests|BuildingFlow.IntegrationTests.MigrationTests` (FullyQualifiedName ile).

| Kontrol | Sonuç |
| --- | --- |
| Çözüm build | GEÇTİ,0 uyarı/0 hata |
| Yeni domain normalizasyon/kanıt yaşam döngüsü | 6/6 GEÇTİ |
| Gerçek izole PostgreSQL/API/migration/şema/eski kullanıcı uyumluluğu | 14/14 GEÇTİ,0 atlanan |
| Eklenen yönetici photo-only regresyonu sonrası son çalıştırma | Yine14/14 entegrasyon+6/6 domain GEÇTİ |
| Model-migration farkı | GEÇTİ,CurrentModelHasNoUnrecordedSchemaChanges |
| Gerçek başlangıç/yükseltme/tekrar ve bütün tablo alanları | GEÇTİ,seçili MigrationTests/PersistenceTests içinde |
| Web/mobil test/build,cihaz veya gerçek SMTP/SMS | ÇALIŞTIRILMADI;bu fazda istemci UX/gönderim kodu değişmedi |

Test verisi yalnız ayrılmış fixture'larda. Sandbox ilk testte VSTest socket iznini engelledi;izinli tekrar paylaşılan PostgreSQL'de53300 bağlantı kapasitesine takıldı. Bu başarısız ortam denemeleri başarı sayılmadı. Aynı mevcut PostgreSQL imajı (`--pull=never`) ile yalnız bu faza ait konteynerde testler geçti;imajın diğer proje init scriptleri test konteynerinde boş init diziniyle maskelendi,hiçbir proje şeması/kimlik bilgisi kullanılmadı. Bu konteynerin ilk bootstrap hatası da ürün hatası değildir;son başarılı sonuç yukarıdadır.

## Güvenlik, kaynak ve lisans

- Anonim GET/POST401;başka hesap verisi okunmuyor;bilinmeyen `verified` alanı400;geçersiz telefon400;eşzamanlı version0 komutları1 başarı+1 çatışma;pasif kanıt403 kanıtlandı.
- Eski profil telefon değişimi yeni hedefe kanıt taşımıyor;eski kanıt korunuyor;isim/aynı numara/yönetici fotoğraf kaldırma kanıtı sıfırlamıyor. Numara kaldırma kanıtı kaldırıyor.
- Yeni log/telemetri/OTP/link/token deposu yok;makbuz sonucu yalnız komut türü tutuyor. E-posta/telefon yalnız kendi JWT hesabında okunur;API mevcut no-store hattındadır.
- Yeni paket/SDK/asset/font/ikon/imaj indirme yok;mevcut bağımlılıklar ve BCL. Manifest/lock değiştirilmedi. Servis koşulu/gerçek SMS maliyeti kararı sonraki fazdadır.
- Eşzamanlı BF-MULTI portföy/mobil çalışmaları kaynak ve Git indeksinde ilerledi. Başlangıç SHA256 karşılaştırması bunları fark etti;bu turda bütün ağacın değişmediği iddia edilmez. Kendi değişikliklerimiz yukarıdaki dosyalarla sınırlı;diğer kaynaklar sıfırlanmadı/geri alınmadı. Ortak DI/Program/snapshot mevcut içerik korunarak genişletildi.
- Canlı uygulama/Keycloak/DB/.env değiştirilmedi;development AutoMigrate başlatılmadı. Bu çalışmadan commit/push/deploy yapılmadı;başka eşzamanlı çalışmanın Git değişikliği sahiplenilmez.

## Kapanış ve sonraki faz

Teknik kabul8/8 ve4 kapanış maddesi ayrıntılı işlem/kanıtlarıyla tamamlandı. GitHub GraphQL son okumada #44 CLOSED/COMPLETED,12/12 madde tamam,Project3 Done doğrulandı. #43 de Closed/Done;#45–#50 açık/Backlog. Eksik gerçek SMS/required-action/ekran işi bu fazın kapsamı değildir;ilgili #45–#50 görevleri açık kalır. FAZ02 için yeni başlatma onayı gerekir.

Resmi adım3/3 tamamlandı:faz%100,00;plan2/8=%25,00. Geçici iki konteyner,özel bağlantı dosyaları ve yalnız bizim oluşturduğumuz boş test DB/rolü temizlendi. Ürün İŞ/UI/MOBİL görevleri bu backend temelinin tamamlanmasıyla tam doğrulama ürünü olarak işaretlenmez;İŞ19'un mevcut kanıt kaynağı TESTING'e BF-VERIFY01 ek kanıtı bağlanır.
