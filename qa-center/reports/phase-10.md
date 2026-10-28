# [QA FAZ 10 | KAYIT 11/14] Demo/test verileri ve temizleme — GitHub #106

```text
[BUILDINGFLOW]
[FAZ QA 10 | KAYIT 11/14] Demo/test verileri
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR
[İLERLEME] FAZ 10 PHASES maddeleri 3/5 = %60,00 (finans demo verisi ve rol değiştirerek müşteri demosu kısmi)
```

## Ne yapıldı
- `QaDataBatch` + `QaDataRecord` (BaseEntity), migration `QaCenterData` (temiz HEAD üzerinde üretildi; paralel tedarikçi modeli karışmadı), Down guard, demo temizlenemez DB check'i.
- `QaDataService`: sentetik SITE, bloklar, daireler, malik/kiracı kişileri ve ilişkiler; her satır parti koduyla kayıt defterine. Dry-run ön izleme, birebir onay metni, version, bağımlılık sırası, tek transaction soft-delete; parti ve kayıt defteri audit için kalır.
- Web: **Test Verileri** (DEMO/TEST ikonlu seçim, oluştur, ekranda aç, önizle) ve **Temizle/Sıfırla** (sayılar, engeller, onay metni, Verileri Temizle) sekmeleri etkin.

## Testler (bu tur, izole PostgreSQL, guard GEÇTİ)
- `QaDataApiTests`: 35 kayıt (1+2+8+12+12), `@example.test`, replay/409, geçersiz tür/boyut 400, yanlış onay 409, eski version 409, sıra doğrulaması, temizlik sonrası soft-delete + kayıt defteri korunur, ikinci temizlik 409; DEMO korunur, finans (gider) ve parti dışı kişi blokajı, blokajda hiçbir satır değişmez; SITE_ADMIN 403.
- `QaDataMigrationTests`: upgrade/tekrar, audit kolonları, demo CLEANED check reddi, tekil kayıt, Down guard.
- Toplam (QaData + QaCenter + QaAi + ErrorContract + envanter + Persistence): **33/33**. Web 308/308 + typecheck. Commit edilen ağaç ayrıca tek başına derlendi: backend 0 hata/0 uyarı, pending model yok, web tsc temiz + 303/303.

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| Sentetik site/blok/daire/kişi/borç/ödeme/makbuz | KISMİ: site/blok/daire/kişi/ilişki var; borç/ödeme/makbuz yok (finans kaydı silinmez kuralı; demo finansı için karar gerekli) |
| Ekranlardan görüntüleme, rol değiştirerek demo | KISMİ: "Ekranda aç" ile mevcut ekranlar; rol değiştirme demosu için demo kullanıcıları (FAZ 05 realm yöntemi) eklenmedi |
| test_run_id, tenant, ortam etiketi | PASS (`code`, `runCode`, `environment`, yönetim alanı) |
| Dry-run, açık onay, bağımlılık sırası, rollback | PASS |
| Demo ve audit korunması, prod guard | PASS (DEMO 409 + DB check; kayıt defteri kalır; QaCenter Production'da kapalı) |

Yerel geliştirme veritabanına veri yazılmadı. Yeni paket yok.

[TOKEN] BİLGİ MEVCUT DEĞİL.
