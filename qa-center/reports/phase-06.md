# [QA FAZ 06 | KAYIT 07/14] Finans: tahakkuk, borçlandırma, ödeme, muhasebe — GitHub #88

```text
[BUILDINGFLOW]
[FAZ QA 06 | KAYIT 07/14] Finans E2E
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#88 açık; ürün eksikleri #89)
[İLERLEME] FAZ 06 PHASES maddeleri 3/7 = %42,86 (4 madde ürün eksikleri nedeniyle BLOKE/KISMİ)
```

## Bu tur
- `OnlinePaymentApiTests`'e iki zorunlu senaryo eklendi: **DuplicateTapsOpenExactlyOneIntent** (5 eşzamanlı istek → 1 intent 201, 4× 409 BF_PAYMENT_INTENT_ACTIVE, aynı UUID replay aynı intent, DB'de tek intent) ve **OutOfOrderFailureNoticeCannotUndoASettledPayment** (başarı sonrası geç gelen imzalı "failed" ve tekrar "succeeded" → durum SUCCEEDED, tek Payment, tek ledger).
- Finans paketleri izole PostgreSQL'de (guard GEÇTİ, run `qa-20261028T182709Z-7422c4`): **Integration 121/121, Unit 57/57, atlanan 0** — OnlinePayment, Payment, Dues, DuesParty, LatePenalty, MobileFinance, Report, Dashboard, Expense ve migration testleri. Konsol logunda KVKK taraması 0 bulgu.

## Zorunlu ödeme zinciri ↔ kanıt
| Zincir halkası / hata senaryosu | Durum | Kanıt (test) |
|---|---|---|
| Alan > daire > ilişki > tahakkuk > doğru borçlu | PASS | DuesApiTests, DuesPartyApiTests (snapshot borçlu, mükerrer engel) |
| Payment intent + idempotency key | PASS | ResidentPaysDues..., DuplicateTaps... (UUID replay) |
| Sandbox sağlayıcı | PASS | ResidentPaysDuesWithPenaltyAndExtraChargeThroughVerifiedSandbox |
| **3D Secure** | **BLOKE** | Ürün özelliği yok (#89) |
| Webhook imza, zaman sınırı, tekrar | PASS | WebhooksAreSignedTimeBoundAndIdempotent (sahte, bozuk, eski imza 400; 3× tekrar tek Payment/ledger) |
| Sırası bozuk webhook | PASS | OutOfOrderFailureNotice... (yeni) |
| Duplicate tap / race | PASS | DuplicateTaps... (yeni) |
| Ret, iptal, kısmi, fazla tutar, chargeback | PASS | PartialPaymentsFailuresCancellationAndChargeback... (500.01 > borç → BF_PAYMENT_PARTIAL_AMOUNT) |
| Değişen borç → review, iade çift onay | PASS | ChangedDebtGoesToReviewAndRefundNeedsSecondApprover |
| Makbuz numarası, ledger işareti | PASS | LedgerSignsReceiptNumbers..., PaymentApiTests (BF-YYYY-NNNNNN) |
| **Çift taraflı muhasebe** | **BLOKE** | Tek taraflı ledger + mutabakat kalemleri var; yevmiye yok (#89) |
| Rol/tenant sızıntısı | PASS | ResidentScopeAndManagementRolesAreEnforced |
| **Web merkez / mobil iOS-Android ödeme başlatma** | **BLOKE** | API var, istemci ekranı yok (#89); mobil otomasyon FAZ 07 |
| Timeout / bağlantı kopması / belirsiz durum | KISMİ | Sağlayıcı kesintisi kaydı (OnlinePaymentStaysClosed...ProviderOutageIsRecorded); istemci tarafı timeout istemci yokluğunda test edilemez |
| Trace/log/ekran videosu | KISMİ | Test logları + KVKK taraması; video FAZ 11 |

Başarısız/belirsiz ödemenin borcu kapatamadığı ve tekrar olayların ikinci tahsilat/ledger üretmediği testlerle doğrulandı. Gerçek ödeme, gerçek kart veya canlı sağlayıcı kullanılmadı.

## PHASES kontrol listesi
1 aidat/tahakkuk/mükerrer **PASS** · 2 web merkez ödeme **BLOKE** · 3 mobil sandbox ödeme **BLOKE** · 4 intent/3DS/webhook/imza/idempotency **KISMİ** (3DS yok) · 5 timeout/ret/duplicate/out-of-order/retry **PASS** (istemci timeout hariç) · 6 kısmi/fazla/iade/bakiye/makbuz/muhasebe/mutabakat **KISMİ** (çift taraflı yok) · 7 trace/log/video **KISMİ**.

Yeni paket yok; lisans 329 PASS. Ürün kodu değişmedi (yalnız test eklendi).

[TOKEN] BİLGİ MEVCUT DEĞİL.
