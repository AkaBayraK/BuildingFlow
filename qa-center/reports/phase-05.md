# [QA FAZ 05 | KAYIT 06/14] Kişi, alan, daire, rol ve KVKK akışları — GitHub #86

```text
[BUILDINGFLOW]
[FAZ QA 05 | KAYIT 06/14] Kişi, alan, daire, rol ve KVKK
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#86 açık)
[İLERLEME] FAZ 05 PHASES maddeleri 4/5 = %80,00 (web↔mobil karşılaştırma BLOKE)
```

## Ne yapıldı
- `scripts/qa-identity.sh` + `qa-identity.mjs` (FAZ 01'den taşınan ayrı test realm'i): realm tanımından kullanıcısız, SMTP'siz, yalnız web/api istemcili geçici `bf-qa-*` realm; SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT, RESIDENT, SECURITY ve MANAGER+RESIDENT kullanıcıları. Koşum `qa-20261028T180603Z-0aca44`: **11/11 PASS** — 7 PKCE S256 girişi ve rol claim'i, QA realm token'ı BuildingFlow API'de **401** (realm izolasyonu), yanlış parola kod almaz, verifier'sız kod değişimi 400, password grant kapalı. Realm silindi (discovery 404). İlk koşumda betik hatası (stdin tüketimi) 5/11 verdi; düzeltilip tekrar koşuldu.
- `scripts/qa_log_scan.py` (13/13 fixture): TCKN (checksum), IBAN, kart (Luhn), JWT, parola, e-posta (test domainleri hariç), TR cep telefonu; yalnız kategori ve satır yazar. Gerçek API, test ve tarayıcı loglarında **0 maskesiz değer**.
- İzole PostgreSQL'de kişi/alan/daire/rol paketleri (People, Hierarchy, Site, PropertyDetails/Type/Model, PersonPortfolio, MyUnit, CentralPeople, UnitPeopleNavigation, PersonRelation, PersonTransition, PersonCode, MobileAuthorization, PersonCenter, PersonProfiles, Authentication): **Integration 244/244, Unit 28/28, atlanan 0** (bayrak sızıntısı düzeltmesinden sonra; öncesinde 1 FAIL, kök nedeni FAZ 04 raporunda).

## TEST_MATRIX eşlemesi
| Satır | Kanıt |
|---|---|
| Yönetim alanları (tek bina/site/blok, kayıt/ilişki) | Hierarchy/Site/Property paketleri PASS |
| Kişiler ve daireler (malik/kiracı/sakin, çoklu daire, ilişki bitirme) | People/PersonRelation/PersonTransition/PersonPortfolio PASS |
| Güvenlik (Keycloak, RBAC, KVKK, log masking) | qa-identity 11/11, Authentication/MobileAuthorization PASS, log taraması 0 |

## Sınırlamalar
- Web ↔ mobil görünüm karşılaştırması **BLOKE**: mobil UI otomasyonu (FAZ 07) ve Android SDK yok. Sözleşme düzeyinde mobil ayrı `/api/v1/mobile/*`, `/api/v2/me/*` uçlarını kullanıyor (envanter).
- `qa-identity.mjs` PKCE oturum kodu `auth-smoke.mjs` ile benzer; ortak modüle çıkarma ayrı bakım işi.

[TOKEN] BİLGİ MEVCUT DEĞİL.
