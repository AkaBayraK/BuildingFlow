# BF-MULTI Ek 04 — Mükerrer kişi engeli

22.10.2026. GitHub #42 (kısmi: doğrulama eylemi ve kanıt belgesi hâlâ açık). Kullanıcı kararı: **mükerrer kişi kesinlikle olmayacak**; kayıt/güncellemede cep telefonu, e-posta ve kimlik numarası (gerçek kişi T.C., tüzel vergi no, yabancı kimlik/pasaport no) kontrol edilir — üçü de kesin engel.

## Kural
| Kapsam | Kimlik no | Cep telefonu | E-posta |
| --- | --- | --- | --- |
| Yeni model (`/api/v2/tenants/{t}/people`) | aynı müşteri (tenant), silinmemiş kişiler (pasif dahil); anahtar = tür + uyruk + ülke + büyük harf/trim no | aynı müşteri, etkin kişiler; E.164 normalize | aynı müşteri, etkin kişiler; küçük harf + trim |
| Eski kişi ekranı (`/api/v1/properties/{p}/people`) | aynı yönetim alanı (Property) | aynı alan, etkin kişiler | aynı alan, etkin kişiler |

- Boş telefon/e-posta/kimlik kontrol edilmez. Kişinin kendi kaydı hariç tutulur (güncelleme).
- Çakışmada 409: `BF_PERSON_DUPLICATE_IDENTITY`, `BF_PERSON_DUPLICATE_PHONE`, `BF_PERSON_DUPLICATE_EMAIL` (ErrorCatalog + web error-codes + TR/EN). Yanıt **hangi alanın** çakıştığını söyler; diğer kişinin adı, kimliği, iletişim bilgisi veya id'si dönmez.
- Eşzamanlılık: yeni modelde müşteri satırı, eski modelde Property satırı `FOR UPDATE` kilidi altında kontrol + yazım; aynı anda iki kayıttan yalnız biri geçer (test edildi). Eski güncelleme yolu da artık Property kilidi ve transaction içinde.
- Web kişi formunda mükerrer 409 "kayıt değişti" kilidi gibi davranmaz; kullanıcı alanı düzeltip yeniden kaydedebilir.

## KVKK değerlendirmesi
Kontrol, yönetimin zaten aynı amaçla tuttuğu veride (kişi sicilinin doğruluğu, mükerrer kaydın önlenmesi — KVKK m.4 doğruluk/güncellik ilkesi) yapılır; yeni veri toplanmaz, başka kişinin verisi yanıtta açıklanmaz. Kimlik/iletişim bilgisi yalnız yetkili yönetici rollerine açık mevcut uçlarda işlenir.

## Veritabanı benzersiz indeksi — bilinçli olarak eklenmedi
- Eski modelde aynı insan farklı yönetim alanlarında ayrı kayıt olabiliyor ve `person-transition` bunları yeni modele birer kişi olarak aktarıyor; müşteri genelinde benzersiz indeks bu aktarımı (FAZ 09 geçişi) kırar.
- Canlı veride mevcut mükerrer olup olmadığı bilinmiyor (canlı DB'ye erişilmedi). Benzersiz indeks yerine kilit altında uygulama düzeyi kontrol kullanıldı; geçiş öncesi aşağıdaki **yalnız sayı** veren sorgu kullanıcı onayıyla çalıştırılmalı, sonuç 0 ise ileride indeks eklenebilir.

```sql
-- Yalnız sayı döndürür; kişisel veri yazdırmaz.
SELECT 'legacy_identity' AS kural, count(*) FROM (SELECT 1 FROM people WHERE NOT is_deleted AND identity_number IS NOT NULL GROUP BY property_id, person_kind, citizenship, country_code, upper(btrim(identity_number)) HAVING count(*)>1) x
UNION ALL SELECT 'legacy_email', count(*) FROM (SELECT 1 FROM people WHERE NOT is_deleted AND is_enabled AND email IS NOT NULL GROUP BY property_id, lower(btrim(email)) HAVING count(*)>1) x
UNION ALL SELECT 'canonical_identity', count(*) FROM (SELECT 1 FROM persons WHERE NOT is_deleted GROUP BY tenant_id, kind, citizenship, country_code, upper(btrim(identity_number)) HAVING count(*)>1) x
UNION ALL SELECT 'canonical_email', count(*) FROM (SELECT 1 FROM persons WHERE NOT is_deleted AND is_enabled AND email IS NOT NULL GROUP BY tenant_id, lower(btrim(email)) HAVING count(*)>1) x;
```
(Telefon için normalize eşleşme uygulama kodundadır; SQL'de yalnız ham eşit değerler sayılabilir.)

## Değişen dosyalar
Backend: `Application/ErrorCatalog.cs`, `Application/CentralPeople.cs` (`PersonDuplicates`), `Infrastructure/CentralPeopleStore.cs`, `Infrastructure/PeopleStore.cs`. Testler: `CentralPeopleTests` (yeni senaryo; eski "aynı kimlikle iki kişi oluşur" beklentisi yeni karara göre güncellendi; fixture'lar benzersiz), `PeopleApiTests` (+`LegacyPersonDuplicateTests`, fixture'lar benzersiz), `TestRelations.Tc` (geçerli T.C. üreteci), `UnitTests/PersonDuplicatesTests`. Web: `services/error-codes.ts`, `messages/tr.json`, `messages/en.json`, `features/person-center/hooks.ts`.

## Açık
Mevcut mükerrerlerin birleştirilmesi/pasife alınması (önceki karar: aidat/tahsilat varsa pasif + gerekçe) ve aktarım sırasında aynı kişinin tek kanonik kişiye eşlenmesi — #42'de doğrulama eylemi ve kanıt belgesiyle birlikte ayrıca.
