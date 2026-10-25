# Gecikme tazminatı API'si (BF-LATE)

Kök: `/api/v1/properties/{propertyId}/late-penalties` · Politika: `CanManageDues` + aktif Property üyeliği (SUPER_ADMIN hariç).
Okuma: SUPER_ADMIN, SITE_ADMIN, MANAGER, ACCOUNTANT. Taslak oluşturma/düzenleme/iptal: SUPER_ADMIN, SITE_ADMIN, MANAGER.
Onay, bitiş, uygulama anahtarı ve muafiyet: SUPER_ADMIN, SITE_ADMIN. ACCOUNTANT'a kişi verisi açılmaz (yanıtlarda kişi yok).

## Kurallar

- **Uygula anahtarı:** `late_penalty_switches` insert-only; yürürlük tarihi ≥ bugün ve kesinleşmiş tahakkukların sonrasında olmalı. Kayıt yoksa tazminat **uygulanmaz**.
- **Kanuni varsayılan:** anahtar açık ve onaylı politika yoksa aylık %5 (634 sayılı KMK m.20), gün esası `DAILY_30` (aylık oran × gün / 30; kullanıcı onayı 23.10.2026).
- **Politika:** DRAFT → APPROVED | CANCELLED. Kullanıcı kararı (23.10.2026): yalnız kanuni yöntem — oran tam %5, `DAILY_30`, dayanak `KMK_20`; başka oran/yöntem/dayanak 400 `BF_LATE_PENALTY_INVALID` (şema ileride değerlendirme için daha geniş değerleri tutabilir). Politika kanuni oranın geçerlilik dönemini, referansını ve onayını kaydeder. Örnek: 3.000 TL için 30 gün 150 TL, 60 gün 300 TL. Onaylı dönemler çakışamaz; onaylı kayıt değişmez, yalnız `end` ile kısaltılır. Kesinleşmiş tahakkuk tarihine dokunan onay/kısaltma `BF_LATE_PENALTY_CRYSTALLIZED`.
- **Hesap:** basit tazminat, yalnız kalan ana para; tazminata tazminat yok. Vade ertesi günü başlar. `D` tarihli ödeme `D` gününü kapsar, azalan ana para `D+1`'den işler. Oran/anahtar/ana para/ay (DAILY_ACTUAL_MONTH) değişiminde aralık bölünür. Kalem 6 ondalık, tahakkuk satırı 2 ondalık `AwayFromZero`.
- **Kesinleşme:** `late_penalty_accruals` borç başına çakışmayan tarih aralıkları + `late_penalty_segments`. Ödeme anında ödeme tarihine kadar, ay sonu job'ında (Worker, 6 saatte bir) önceki ay sonuna kadar kesinleşir; tekrar çalıştırma aynı aralığı üretmez (`UNIQUE(charge_id, from_date) WHERE status <> 'SUPERSEDED'`).
- **Geriye dönük ödeme / iptal:** etkilenen ödenmemiş tahakkuklar `SUPERSEDED` olarak saklanır ve aynı aralık yeniden hesaplanır. Etkilenen aralıkta ödenmiş veya muaf tahakkuk varsa `BF_LATE_PENALTY_REBUILD_BLOCKED`.
- **Tahsilat:** mevcut `POST /payments` dağıtım satırına `kind: "PENALTY"` eklenebilir; tutar ödeme tarihindeki tazminat bakiyesini aşamaz (`BF_LATE_PENALTY_BALANCE`). Sistem tutarları otomatik mahsup etmez; `preview` TBK m.100/101 sırasını (en eski borç, önce tazminat sonra ana para) **öneri** olarak döndürür. Tazminat dağıtımı tahakkuk başına satırlara (eskiden yeniye) bölünür; iptal aynı satırları geri alır. Penalty satırı olmayan isteklerin fingerprint'i değişmez.
- **Borç iptali:** tazminatı tahsil edilmiş borç iptal edilemez (`BF_LATE_PENALTY_CHARGE_PAID`); tahsil edilmemiş tahakkuklar `SUPERSEDED` olur.

## Uçlar

| Yöntem | Yol | Açıklama |
|---|---|---|
| GET | `/settings` | Bugünkü durum, anahtar geçmişi, politikalar, kanuni oran, kesinleşme tarihi, kullanıcı yetkileri |
| POST | `/switches` | `{requestId, apply, effectiveFrom, reason}`; aynı UUID+içerik replay 200 |
| POST / PUT | `/policies`, `/policies/{id}` | Taslak `{requestId, validFrom, validTo, monthlyRate, method, legalBasis, legalReference, approvalDocumentId, version}` |
| PUT | `/policies/{id}/approve` · `/cancel` · `/end` | `{version, reason?, validTo?}` |
| GET | `/overdue?page&pageSize&unitId&year&search` | Gecikmiş borçlar, kesinleşmiş + bekleyen tazminat, filtre toplamları |
| GET | `/charges/{chargeId}` | Hesaplama detayı (tahakkuk geçmişi, kalemler, bugüne kadar bekleyen kalemler) |
| GET | `/preview?unitId&paidDate&amount` | Ödeme tarihindeki tazminat ve TBK m.100 önerisi |
| POST | `/accruals/run` | `{through ≤ bugün}` idempotent kesinleştirme |
| PUT | `/accruals/{id}/waive` | `{version, reason}`; yalnız ödenmemiş aktif tahakkuk |
| GET | `/report` | Yıllık tahakkuk/tahsilat/muafiyet/açık + oran ve anahtar geçmişi |

Mobil: `GET /api/v1/mobile/finance` borç satırında `penalty` (kesinleşmiş + bugüne kadar bekleyen), özet `penalty` (kesinleşmiş), tahsilatta `kind`, `chargeId` detayında `penaltyLines`. Mobil hesap yapmaz.

## Mevcut olmayanlar

Muhasebe/hesap planı modülü ve online ödeme sağlayıcısı yoktur; tazminat ayrı rapor ve ayrı dağıtım türüyle izlenir. Sakinlere vade/gecikme bildirimi gönderecek kanal (push tüketicisi, borçlu kişi + rıza bağlı e-posta/SMS akışı) bulunmadığı için bildirim gönderimi eklenmedi.
