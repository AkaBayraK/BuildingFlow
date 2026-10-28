# [QA FAZ 01 | KAYIT 02/14] İzole test altyapısı ve güvenlik — GitHub #81

```text
[BUILDINGFLOW]
[FAZ QA 01 | KAYIT 02/14] İzole test altyapısı ve güvenlik
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#81 açık)
[İLERLEME] FAZ 01 PHASES maddeleri 4/5 = %80,00 (madde 2 kısmi)
```

## Değişen dosyalar
- `scripts/qa_guard.py` (yeni): izole ortam guard'ı ve `test_run_id` üretimi. Yalnız ayar adı ve kural kodu yazar, değer yazmaz.
- `scripts/test_qa_guard.py` (yeni): 15 sentetik pozitif/negatif fixture ve run_id biçimi.
- `scripts/test.sh`: `dotnet test` öncesi guard zorunlu.
- `scripts/ci.sh`: guard regresyon testi CI kapısına eklendi.
- `qa-center/`: paket dosyaları ve `reports/phase-00.md`, `reports/phase-01.md`.

Guard kuralları: `PROD_ENVIRONMENT` (ASPNETCORE/DOTNET_ENVIRONMENT=Production), `TEST_DATABASE_MISSING`, `DATABASE_NOT_LOOPBACK`, `DATABASE_NOT_TEST` (yalnız `buildingflow_test*` / `buildingflow_ci_test`), `DATABASE_IS_MAIN` (POSTGRES_DB ile aynı), `SERVICE_NOT_LOOPBACK` (S3, Mailpit, Keycloak admin, RabbitMQ, SMTP, sandbox ödeme URL'i), `REAL_SMS_PROVIDER` (Netgsm OTP açık).

## PHASES kontrol listesi
| Madde | Durum | Kanıt |
|---|---|---|
| Ayrı DB/storage/queue/test secrets + environment guard | PASS | Geçici `buildingflow_test_<hex>` DB/rol, geçici Mailpit, loopback S3/RabbitMQ; guard GEÇTİ satırı |
| Sandbox ödeme, sahte SMS/e-posta, sentetik kullanıcı, mobil test profili | **KISMİ** | Sandbox ödeme ve Mailpit (`@example.test` dışı alıcı reddi) mevcut; gerçek SMS guard ile engelli; sentetik JWT kullanıcıları testlerde. **Ayrı Keycloak test realm'i ve QA'ya özel mobil profil yok** (yalnız mevcut `development` EAS profili) |
| Prod'a yazma/silme engeli | PASS | `ASPNETCORE_ENVIRONMENT=Production bash scripts/test.sh` → exit 1, `PROD_ENVIRONMENT`, 0 test çalıştı, geçici DB temizlendi; API'de mevcut sandbox+Production başlatma engeli korunuyor |
| CI izole çalıştırma, lisans/dependency taraması | PASS (yerel) | `test_qa_guard.py` 15/15; `check-licenses.py` 329 paket PASS; `check-private-config.py` PASS; yeni bağımlılık yok. GitHub Actions bu tur çalıştırılmadı (push sizde) |
| Entegrasyon smoke + kanıt | PASS | run_id `qa-20261028T172329Z-034106`: Integration 52/52, Unit 14/14, atlanan 0 (OnlinePayment, AuthenticationTests, ReleaseSecurityTests, PersistenceTests) |

Kanıtlar (git dışı): `artifacts/qa/qa-20261028T172329Z-034106/console.log`, `negative-prod-guard.log`, `smoke.trx`. Not: TRX dosyası iki test projesinde aynı adı aldığı için yalnız unit sonucunu tutuyor; integration sonucu console.log'da. FAZ 04'te proje başına ayrı TRX adı kullanılacak.

## Kalan / karar
- Keycloak test realm'i ve QA mobil profili: FAZ 05 (Keycloak) ve FAZ 07'ye (mobil) taşınması önerilir; onay gelirse #81 kapatılır.
- Android SDK yok: BLOKE.

[TOKEN] Giriş/çıkış/toplam token, kalan context: BİLGİ MEVCUT DEĞİL.
