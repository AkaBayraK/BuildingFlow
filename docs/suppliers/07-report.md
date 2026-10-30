# TEDARİKÇİ FAZ07 — Periyodik bakım ve planlama

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #97](https://github.com/AkaBayraK/BuildingFlow/issues/97). Plan FAZ07, kayıt08/13, resmi2/2=%100,00. Tedarikçi akışı8/13=%61,54. Sonraki FAZ08'dir.

**Tekrarlı görev planları, takvim, hatırlatma, tatil/çakışma kuralları.** Tedarikçinin alan+hizmet kapsamında haftalık/aylık/3 aylık/6 aylık/yıllık bakım planı, en fazla iki yıl ve 120 tarih. Ay adımı başlangıç tarihinden hesaplanır (31 Ocak → 28 Şubat → 31 Mart). Tenant tatil takvimi ve her zaman iş günü dışı hafta sonu; sonraki/önceki iş günü veya tarihi koru kuralı. Önizleme asıl ve kaydırılmış tarihi, çakışmayı gösterir; aynı Property+hizmette aktif planın planlı tarihiyle çakışma veya dönem dışına kayma 409. Hatırlatma ekranda "Yaklaşan bakımlar" listesidir (tarih − hatırlatma günü ≤ bugün, Europe/Istanbul); e-posta/push FAZ10.

**Tamamlanma kanıtı, gecikme ve sözleşme ilişkisi.** Her tarih mevcut Planlama ve Bakımlar modülünde PLANNED kayıt açar; böylece FAZ06 iş emri kaynağı olur. Tamamlama tarih ≤ bugün ve kanıt notu ister; isteğe bağlı kanıt iş emri yalnız bu tarihten açılmış VERIFIED/CLOSED iş emridir, açık iş emri varken tamamlama/atlama 409. Gecikme günü kaydedilir; geciken tarihler rozetle gösterilir. Sözleşmeye bağlı planda sözleşme ACTIVE ve dönem sözleşme içinde olmalıdır. Plan sonlandırma kalan tarihleri atlar ve bakım kayıtlarını iptal eder. UUID replay, version/kilit, eşzamanlı tamamlama 200+409, immutable revizyon ve DB trigger korumaları vardır.

Değişen kaynaklar: Domain `SupplierMaintenance.cs`, Application `SupplierMaintenance.cs`, `SupplierMaintenanceConfigurations.cs`, `SupplierStore.Maintenance.cs`, `SupplierMaintenanceEndpoints.cs`, migration `20261030061301_SupplierMaintenanceSchedules`, ErrorCatalog/DI/Program/HierarchyStore; web `maintenance-model.ts`, `SupplierMaintenance.tsx`, sekme/stil, TR/EN ve hata kodu kaydı; unit/entegrasyon/migration/web testleri.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| Tam backend regresyonu | **Integration924/928 + Unit469/469**; 2 opt-in canlı OIDC atlandı; 2 test paylaşılan PostgreSQL "53200 out of shared memory" ile düştü |
| O iki test sınıfı + CommunicationApiTests tekrar koşumu | **19/19 geçti** |
| FAZ07 entegrasyon/migration + unit | **4/4 + 5/5** |
| Web test + typecheck + production build | **336/336, GEÇTİ, GEÇTİ** |
| Kapsam kapısı | **GEÇTİ, yeni testsiz kalem 0** (API 450) |
| Lisans | **329 PASS**, yeni paket yok |

Loglar `/tmp/bf-sup07-{a,b,full,rerun,webbuild}.log`. Gerçek tarayıcıda ekran kontrolü ve canlı OIDC E2E yapılmadı; migration yerel veritabanına uygulanmadı. Tatil eklemek/kaldırmak mevcut planları yeniden hesaplamaz (yalnız yeni planlar). Plan oluşturulduktan sonra düzenlenemez; değişiklik için sonlandırıp yeni plan açılır.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
