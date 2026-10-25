# BF-LATE FAZ 00 — Mevcut tahakkuk ve tahsilat analizi

Tarih: 23.10.2026. Kapsam: gecikme tazminatı (KMK m.20) için mevcut sistem envanteri.
Bu fazda kod değiştirilmedi. Akış kimliği: **BF-LATE** (8 faz: 00–07). İŞ/UI/TEKNİK/MOBİL sayaçlarına eklenmez.

## 1. Mevcut yapı envanteri

| Alan | Mevcut kaynak | Not |
|---|---|---|
| Yönetim alanı | `Domain/Property.cs`, `properties` | "Yönetim alanı" = `Property`. Tenant/scope sınırı `PropertyMembership` (aktif üyelik) ve `SUPER_ADMIN`. |
| Tahakkuk | `DuesAssessment` (toplu istek) → `DuesCharge` (bölüm başına borç) | Aylık dönem (`YYYY-MM`), TRY, `DueDate`, `Amount` değişmez, `PaidAmount`, `IsCancelled`. Borç **Unit**'e aittir. |
| Muhatap | `DuesChargeParty` | Tahakkuk anındaki malik/kiracı snapshot'ı; kişi borcu yok (06.1). |
| Borç durumu | `DuesCharge.Status(today)` | OPEN / PARTIALLY_PAID / OVERDUE (`DueDate < today`) / PAID / CANCELLED. Bakiye = `Amount − PaidAmount`. |
| Tahsilat | `Payment` + `PaymentAllocation` | Aynı Property+Unit; dağıtım **operatör tarafından açıkça** seçilir (borç başına tutar+version). Toplam = ödeme tutarı; fazla ödeme/avans **yok**. |
| Ödeme iptali | `PaymentStore.CancelAsync` | Ödeme silinmez, `IsCancelled`; dağıtımlar kalır, `DuesCharge.ReversePayment` ile `PaidAmount` geri alınır. Ayrı iade kaydı yok. |
| Borç iptali | `DuesStore.CancelAsync` | Ödemeli borç iptal edilemez; katalog nedenli tek iptal. |
| Eşzamanlılık | `SELECT … FOR UPDATE` Property satırı, `version` concurrency token, UUID `requestId` + SHA256 fingerprint replay | Finans komutları Property kilidiyle seri. |
| API | `/api/v1/properties/{id}/dues`, `/payments` | Rol: SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT + aktif üyelik. ACCOUNTANT kişi verisi göremez. |
| Hata | `ErrorCatalog` + FE `error-codes` registry + TR/EN | Yeni kodlar buraya eklenir. |
| Rapor | `ReportStore` aylık özet | Gelir = iptal edilmemiş `Payment.PaidDate` toplamı; tahakkuk gelir değil. |
| Muhasebe | **Yok** | Hesap planı/yevmiye modülü bulunmuyor. Gelir/gider raporu `Payment`/`Expense` üzerinden. |
| Mobil | `MobileFinanceStore`, `MobileOwnerStore`; mobil `src/finance` (cache.ts SQLite/offline snapshot) | Salt okunur borç/ödeme özeti; sunucu hesaplar, mobil hesaplamaz. |
| Online ödeme | **Yok** | Gerçek transfer/sağlayıcı/callback yok (AGENTS.md 06). |
| Bildirim | `MessageOutbox` + RabbitMQ worker (e-posta), SMS dispatch, `MobileNotices` (duyuru/talep okunma) | Borç/vade bildirimi yok. |
| Zamanlanmış iş | `BuildingFlow.Worker` `BackgroundService`'ler | Finans job'ı yok. |
| Ortak alan | `BaseEntity` + `BaseEntityConfiguration.Configure` | id/version/audit/is_deleted/is_enabled zorunlu; `AuditInterceptor` otomatik. |
| Versiyonlu parametre örneği | `PropertyBudget` + `BudgetRevision` | Insert-only revizyon kalıbı yeniden kullanılacak. |

**Mevcut gecikme/faiz hesabı:** kodda yok (`grep gecikme|penalty|interest|late_fee` yalnız ilgisiz eşleşme). Çakışan yapı oluşmaz.

## 2. Tahsilat dağıtımı (kritik bulgu)

- Sistem otomatik mahsup yapmaz; her ödeme satırı hangi borca ne kadar gittiğini `payment_allocations` içinde saklar.
- `UNIQUE(payment_id, charge_id)`: bir ödeme bir borca tek satır dağıtır.
- Ödeme tarihi gelecekte olamaz; geçmiş tarihli ödeme serbesttir (geriye dönük tahsilat mümkün).
- Gecikme tazminatı ayrı izlenecekse dağıtım satırı **tür** (ANA PARA / GECİKME) taşımalıdır; unique anahtar `(payment_id, charge_id, kind)` olur.

## 3. Önerilen tasarım (FAZ 01–07)

1. `LatePenaltySwitch` (insert-only): Property bazında **"Gecikme tazminatı uygula"** açık/kapalı, yürürlük tarihi ve gerekçe. Kayıt yoksa **uygulanmaz** (kullanıcı kararı). Geriye dönük değiştirilemez.
2. `LatePenaltyPolicy`: Property bazında tarihsel oran dönemi (valid_from/valid_to, aylık oran, yöntem, hukuki dayanak/referans, onay belgesi, DRAFT→APPROVED→RETIRED). Onaylı dönemler çakışamaz; onaylı kayıt değişmez, yalnız bitiş tarihiyle emekliye ayrılır.
3. Anahtar açık ama onaylı politika yoksa **kanuni varsayılan %5/ay (KMK m.20)** kullanılır; sıfır oran varsayılmaz.
4. `LatePenaltyAccrual` + `LatePenaltySegment`: borç başına bitişik tarih aralıklı kesinleşmiş tazminat, dönem/oran/gün kalem kalem. Tekrar çalıştırma aynı aralığı ikinci kez üretmez.
5. Hesap motoru `Domain` içinde saf fonksiyon: basit tazminat, yalnız kalan ana para, tazminata tazminat yok, oran/anahtar sınırlarında aralık bölme, decimal + kontrollü yuvarlama.
6. Tahsilat: mevcut `Payment` genişletilir; dağıtım satırına `kind`. Gecikme dağıtımı tazminat bakiyesini kapatır. İptalde ters işlem; geriye dönük ödeme etkilenen tahakkukları yeniden kurar (ödenmiş tahakkuk varsa 409).
7. Web: mevcut Aidat ekranında tazminat sütunu/detayı, ödeme dialoğunda ayrı tazminat tutarı, yönetim alanı ayarlarında politika sekmesi.
8. Mobil: sunucunun verdiği ana para/tazminat ayrımı ve detay; offline'da "güncel değil" etiketi; mobil hesap motoru yok.

## 4. Hukuki karar matrisi (onay gerekir)

| # | Konu | Seçenekler | Öneri / geçici varsayılan |
|---|---|---|---|
| K1 | Gün esası | A) aylık oran × gün / 30; B) aylık oran × gün / ilgili ayın gün sayısı; C) başlamış her ay için tam ay | **A (gün/30)** yaygın uygulama; yapılandırılabilir strateji, politika başına seçilir. Onay gelene kadar varsayılan A, belgede "onay bekliyor". |
| K2 | Kısmi ödeme mahsubu | A) mevcut açık dağıtım korunur, sistem TBK m.100 sırasını (önce tazminat, sonra ana para) **öneri** olarak doldurur; B) TBK m.100 sırası zorunlu | **A** — mevcut algoritma değişmez, operatör kararı kayıtlı. |
| K3 | %5'ten farklı oran | A) yalnız daha düşük oran, hukuki dayanak+belge+ikinci kişi onayı ile; B) hiç izin yok; C) üst sınırsız | **A** — %5 üstü reddedilir. |
| K4 | Gecikme başlangıcı | A) vade tarihinin ertesi günü; B) vade ayının bitimi | **A** |
| K5 | Yuvarlama | Her tahakkuk satırı 2 ondalık, MidpointRounding.AwayFromZero | Öneri |
| K6 | Anahtar açılmadan önceki gecikme | A) yalnız anahtarın yürürlük tarihinden sonraki günler; B) geriye dönük | **A** |

## 5. Riskler ve sınırlar

- Muhasebe/hesap planı ve online ödeme sağlayıcısı mevcut değil; FAZ 03'te bu parçalar "mevcut değil" olarak belgelenecek, sahte entegrasyon yazılmayacak.
- Paralel `AccountPhoneOtp` migration'ı çalışma ağacında; yeni migration onun üzerine eklenecek, dokunulmayacak.
- Fazla ödeme/avans modeli yok; tazminat dağıtımı da bakiye ile sınırlı kalır.
