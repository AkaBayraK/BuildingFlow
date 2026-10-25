# BF-PAY FAZ 00 — Mevcut proje keşfi ve boşluk analizi

Tarih: 23.10.2026. Akış kimliği **BF-PAY** (13 faz: 00–12). Kaynak: `BuildingFlow-Payment-Phases/` (MASTER_PROMPT, PHASES). İŞ/UI/TEKNİK/MOBİL sayaçlarına eklenmez.
Bu faz salt okunurdur; kod, migration ve bağımlılık değiştirilmedi. Gecikme tazminatı (BF-LATE, commit edilmemiş) mevcut kod olarak kabul edildi.

## 1. Yapılan işlemler

- `BuildingFlow-Payment-Phases/AGENTS.md`, `MASTER_PROMPT.md`, `PHASES.md` ve 13 faz dosyası okundu.
- Kök `AGENTS.md`, `docs/progress/STATUS.md`, `docs/api/payments.md`, `docs/api/late-penalties.md`, `docs/refactor/late-penalty/REPORT.md` incelendi.
- Domain: `DuesAssessment`/`DuesCharge`/`DuesChargeParty`, `Payment`/`PaymentAllocation`, `LatePenalty*` sınıfları okundu.
- Uygulama/altyapı: `Dues.cs`, `Payments.cs`, `LatePenalties.cs`, `MobileFinance.cs`, `DuesStore`, `PaymentStore`, `LatePenaltyEngine`, `LatePenaltyStore.PreviewAsync`, `MobileFinanceStore`, `MobileFinanceQueries.ResidentCharges`, EF konfigürasyonları, `Program.cs` politikaları, `ReleaseSecurity` hız sınırı, `ErrorCatalog` ve FE `error-codes.ts` incelendi.
- Web `features/dues`, `features/payments`, `features/late-penalties`; mobil `src/finance`, `FinancePanel.tsx`, yüklü Expo modülleri (expo-web-browser mevcut) incelendi.
- Test altyapısı: `PostgresFact` + `BUILDINGFLOW_TEST_DATABASE`, izole PostgreSQL betik kalıbı (`scripts/test-phone-otp.py`).

## 2. Mevcut yapı envanteri

| Alan | Mevcut kaynak | Ödeme akışı için anlamı |
|---|---|---|
| Tenant / yönetim alanı | `Property` + aktif `PropertyMembership`; `SUPER_ADMIN` global | Tüm yeni kayıtlar `property_id` taşır; kapsam dışı 404. |
| Roller | Keycloak: SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT; sakin bağlamı OWNER/TENANT (mobil oturum) | SystemAdmin = SUPER_ADMIN. Ayrı yetki modeli kurulmaz. |
| Borç | `DuesAssessment` (toplu) → `DuesCharge` (Unit başına) | **Tür alanı yok**; yalnız aylık aidat. Unit+dönem için iptal edilmemiş tek borç (partial unique index). |
| Borç sorumlusu | `DebtorType` UNIT/TENANT/OWNER + `DuesChargeParty` snapshot | Sakin yalnız `ResidentCharges` sorgusunun gösterdiği borcu görür/öder. |
| Tahsilat | `Payment` (CASH/BANK_TRANSFER) + `PaymentAllocation` (PRINCIPAL/PENALTY) | Avans/fazla ödeme yok; toplam = dağıtım toplamı. |
| Gecikme tazminatı | `LatePenaltyEngine` (kesinleştirme, yeniden kurma), `Payment.AllocatePenalty`, `PreviewAsync` TBK m.100 önerisi | Online ödemede aynı motor kullanılmalı; öneri mantığı ikinci kez yazılmamalı. |
| Kilit / idempotency | Property `FOR UPDATE`, `version`, UUID + SHA256 fingerprint | Online ödeme de aynı kilidi kullanmalı. |
| Makbuz | Yok (uygulama içi tahsilat belgesi = ödeme detayı) | Numaralı makbuz eklenecek; mali/fiskal belge değildir. |
| Muhasebe | **Yok** | Minimum değişmez tahsilat defteri (ledger) mutabakat için gerekli. |
| Banka hesabı / IBAN | **Yok** | Yeni. |
| Ödeme sağlayıcı / webhook | **Yok** | Yeni; gerçek sağlayıcı hesabı/sözleşmesi yok → yalnız sandbox. |
| Worker | `LatePenaltyWorker` (6 saatte bir) kalıbı | Mutabakat job'ı aynı kalıpla. |
| Hız sınırı | Anonim IP 30/dk, kimlikli okuma/yazma | Webhook anonim uç bu sınırın içinde. |
| Mobil | `GET /mobile/finance`, SQLite önbellek, `expo-web-browser` | Hosted checkout için yeni paket gerekmez. |
| Hata sözleşmesi | `ErrorCatalog` + FE registry + TR/EN | Yeni kodlar üç yerde birlikte. |

## 3. Mevcut → yeni bileşen eşlemesi

| Kavram (faz dokümanı) | Karar |
|---|---|
| Borç türü (aidat + ek ödeme) | `DuesAssessment/DuesCharge.Kind` DUES/EXTRA + `Category`; yalnız DUES dönem tekilliği |
| PaymentMethod | Mevcut enum'a `ONLINE_CARD`; yöntem aç/kapat `PropertyPaymentSettings` |
| BankAccount | Yeni `PaymentBankAccount` (IBAN değişmez, değişim = yeni hesap + onay) |
| ProviderConfiguration (katalog) | Kod sahipli adaptör kataloğu + `PaymentProvider` global aç/kapat (SUPER_ADMIN) |
| MerchantBinding | Yeni `PaymentMerchantAccount` (yönetim alanı, opsiyonel blok, gizli anahtar yalnız referans) |
| PaymentIntent / Transaction | Yeni `PaymentIntent` + `PaymentIntentLine` (borç rezervasyonu) |
| Allocation | **Mevcut** `PaymentAllocation` (PRINCIPAL/PENALTY) yeniden kullanılır |
| Receipt | Yeni `PaymentReceipt` (yönetim alanı + yıl sıralı numara) |
| Refund | Yeni `PaymentRefund` (çift onay) |
| Reconciliation | Yeni `PaymentReconciliationItem` + `PaymentLedgerEntry` + `PaymentProviderEvent` (webhook gelen kutusu) |

## 4. Boşluklar ve öncelik

1. **Kritik:** aidat dışı borç türü yok → ek ödemeler tahsil edilemiyor (kullanıcı talebi).
2. **Kritik:** online ödeme sağlayıcısı, webhook, durum makinesi yok.
3. **Yüksek:** banka hesabı/IBAN ve sakinlere ödeme yönergesi yok.
4. **Yüksek:** numaralı makbuz ve değişmez tahsilat defteri yok.
5. **Orta:** mutabakat, iade, ters ibraz süreçleri yok.
6. **Orta:** mobil ödeme başlatma yok; offline sınırı tanımlı değil.

## 5. Riskler ve karar kapıları

| Risk | Etki | Karar / kapı |
|---|---|---|
| Gerçek sağlayıcı (iyzico/PayTR/banka POS) sözleşmesi, üye işyeri ve sandbox anahtarı yok | Canlı veya gerçek sandbox entegrasyonu yapılamaz | **BLOKE (kullanıcı/hukuk):** sağlayıcı seçimi ve sözleşme. Bu turda yalnız sağlayıcıdan bağımsız arayüz + yerel sandbox adaptörü. |
| 6493 sayılı Kanun / BuildingFlow'un parayı kendi hesabında toplaması | Lisanssız ödeme hizmeti riski | Varsayılan model: para doğrudan yönetim alanının kendi üye işyeri/hesabına gider; BuildingFlow fon tutmaz. Değişiklik hukuki onay ister. |
| Komisyonun ödeyene yansıtılması | Tüketici/kart mevzuatı | Kapalı: yalnız `ABSORBED` (yönetim üstlenir). |
| PCI DSS | Kart verisi kapsamı | Kart verisi hiç alınmaz/saklanmaz; yalnız hosted checkout. Kapsam sağlayıcıyla teyit edilecek. |
| Yeni paket yasağı (16.10.2026 kararı) | SDK eklenemez | HTTP adaptörü + platform API'leri; yeni paket yok. |
| Eşzamanlı başka işler (BF-VERIFY, Next 16.4.3) | Ortak dosya çakışması | Ortak dosyalarda küçük, yerinde değişiklik; commit yok. |
| Ek ödemelerde gecikme tazminatı | Hukuki kapsam | KMK m.20 ortak gider ve avans paylarını kapsar; ek ödeme türü de aynı motorla işler (FAZ 01 ADR). |

## 6. Testler

Bu fazda kod değişmediği için test çalıştırılmadı; mevcut sonuçlar BF-LATE raporundadır ve yeniden çalıştırılmış gibi gösterilmez.

## 7. Geri alma

Yalnız bu belge eklendi; silinmesi yeterlidir.

## 8. FAZ 01 önerisi

Borç türü, online ödeme kavramları, durum makinesi, yetki matrisi ve kabul kriterlerini ADR olarak yaz; mevcut `Payment`/`PaymentAllocation`/`LatePenaltyEngine` yeniden kullanılır.
