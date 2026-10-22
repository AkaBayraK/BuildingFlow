# BAKIM — Hesap iletişim bilgileri — 22.10.2026

**TAMAMLANDI;adım01/01,bakım/bu tur%100,00.** Kullanıcı önce kişisel bilgilerde e-postayı, ardından yeni kullanıcı formundaki cep telefonunu sordu. E-posta verisi zaten Hesap bölümündeydi; frontend yerleşimi düzeltildi. Yeni kullanıcıda telefon alanı ve kaydı eksikti; optional alan mevcut ortak profile bağlandı. Geçmiş MASTER_PROMPT/PHASES başlangıcı yeniden başlatılmadı; gerçek UI plan kaynakları frontend/buildingflow-ui-plan altından okundu. Mevcut İŞ19 hesap sunumu/kullanıcı oluşturma sözleşmesinin bakımı; yeni faz/tik yok.

## Değişen uygulama dosyaları

- `frontend/buildingflow-web/src/features/account/AccountPage.tsx`: e-posta telefon yanında,readonly,açıklamalı ve yokluk durumu; eski Hesap satırı kaldırıldı. Kaydet e-postayı yazmaz.
- `src/features/users/UsersWorkspace.tsx`: yalnız yeni kullanıcı formunda isteğe bağlı ortak PhoneInput; biçimleme/doğrulama ve phone JSON alanı; alan olmayan eski kayıt sözleşmesi korunur. Hesap sahibi sonradan kişisel bilgilerinden telefonunu günceller.
- `backend/src/BuildingFlow.Application/Users.cs`, `Infrastructure/UserDirectoryStore.cs`: nullable Phone DTO, mevcut mobil kurallarıyla erken doğrulama; yeni kullanıcı id'sinde parent/profil kilidiyle üyelik+profil transaction'ı. Mevcut verinin üzerine yazma yok; ilk profil ad/soyadı mobilin80 karakter sınırına uyar. UserResponse'a telefon eklenmedi. Dizin kapsamı ve yeni hesap hatasında mevcut Keycloak telafisi korunur. Yeni profil PHOTO rızası vermez, fotoğraf içermez. Dış kimlik sağlayıcı+yerelDB tek transaction olarak sunulmaz.
- `Application/ErrorCatalog.cs`, `messages/tr.json`,`en.json`, `src/services/problem.ts`: mevcut BF_USER_INVALID metnine telefon eklendi; yeni public kod/parametre yok. phone alanı güvenli alan hata allowlist'inde. Yeni i18n açıklamaları.
- `backend/tests/BuildingFlow.IntegrationTests/UserContactTests.cs`:3 gerçek PostgreSQL senaryosu; alan olmadan eski davranış, iki rolün oluşturması, geçersiz numara/ad sınırı/alan/rol reddi, mevcut profile dokunmama ve membership rollback; web/mobil ortak okuma ve owner version'lı telefon güncellemesi, gerçek creator audit'i ve rıza verilmemesi.
- `tests/api-feedback.test.mjs`: phone alanı için TR/EN kodlu geri bildirim, ham iletişim ayrıntısını render etmeme. `tests/fixtures/sidebar-entry.tsx`,`sidebar-api.ts`: AccountPage gerçek bileşeni ve yalnız fixture'a ait örnek iletişim verisi; diğer test akışları korunur.

## Bu turun test/kanıtı

**27/27 backend entegrasyonu,0 atlanan/başarısız:** `dotnet test backend/tests/BuildingFlow.IntegrationTests/BuildingFlow.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~UserManagementTests|FullyQualifiedName~MobileProfileTests|FullyQualifiedName~AccountApiTests'`; sonuç `/tmp/bf-account-contact-results/account-contact.trx`. Dedicated geçici PostgreSQL rol/veritabanı ve test fixture'ının özel S3 bucket'ı; cleanup tamamlandı. Canlı DB/.env/Keycloak hesapları veya gerçek e-posta gönderimi değiştirilmedi.

**235/235 web testi,0 atlanan/başarısız; npm run typecheck/build başarılı.** `git diff --check` temiz. Yeni phone alanı ilk feedback testinde allowlist dışında kaldı; alan eklenerek düzeltildi ve son koşu geçti. UI için uygulama davranışını kopyalayan test eklenmedi.

CUA1440×1000 gerçek bileşen+izole fixture: kişisel bilgilerde E-posta readonly, doğru label/açıklama; yeni kullanıcı cep telefonu alanı ve ortak+90 gruplama görselde doğrulandı.375×844 panel clientWidth=scrollWidth373px; yatay taşma yok. Browser error/warn0. UI'da kaydetmeden Vazgeç ile çıkıldı; kalıcı kayıt davranışı gerçek backend entegrasyonunda doğrulandı. Girişim yapılması başarı sayılmadı; sahte UI/API canlı OIDC kabulü olarak sunulmaz. Mobil kaynak değişmedi/check-build çalıştırılmadı; mobil uyumluluk ortak gerçek API okuma/güncellemesiyle doğrulandı.

[Kişisel bilgiler](ACCOUNT_CONTACT_PERSONAL.png) · [Yeni kullanıcı](ACCOUNT_CONTACT_NEW_USER.png) · [Mobil](ACCOUNT_CONTACT_MOBILE.png)

Yeni bağımlılık/font/ikon/görsel paketi,entity/kolon/migration yok. Ortak repo başka devam eden BF-MULTI işleri içeriyordu; onların değişiklikleri korunur. Git yazma/stage/commit/push yapılmadı; başlangıç indeksi ve FE manifest/lock koruma kontrolü geçti; Next.js next-env.d.ts yalnız beklenen build yol değişimi geri alındı. Geçici sunucu/sekmeler kapandı; viewport sıfırlandı. İŞ yol haritası24 kayıt,141/151=%93,38.

Giriş token:BİLGİ MEVCUT DEĞİL. Çıkış token:BİLGİ MEVCUT DEĞİL. Toplam token:BİLGİ MEVCUT DEĞİL. Kalan context:BİLGİ MEVCUT DEĞİL.

Codex usage/rateLimitsByLimitId.codex,22.10.2026 teslim okuması:300dk kullanılan%53/kalan%47,reset22.10.2026 15:39:36;10080dk kullanılan%71/kalan%29,reset27.10.2026 13:32:07 Europe/Istanbul.
