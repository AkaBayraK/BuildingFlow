# 06 — Tahsilat ve borç kapatma API sözleşmesi

2026-10-09. Alınmış nakit veya banka havalesinin kaydı: Payment → PaymentAllocation → DuesCharge. Bir tahsilat aynı Property ve Unit'in 1–100 borcuna açık tutarlarla dağıtılır; apartmanda sahte bina gerekmez. Bu modül bankadan para çekmez veya transfer başlatmaz.

## Yetki / kapsam

Keycloak JWT ve SUPER_ADMIN / SITE_ADMIN / MANAGER / ACCOUNTANT gerekir. SUPER_ADMIN dışındaki kullanıcı için aktif, silinmemiş PropertyMembership zorunludur. Anonim 401, yetersiz/sahte rol 403, bulunmayan veya kapsam dışı kayıt 404. ACCOUNTANT kişisel kayıt API'sine erişim kazanmaz. Pasif Property'de geçmiş okunur; yeni tahsilat ve iptal 409 olur. Aynı UUID'nin aynı içerik replay'i bir yazma işlemi değildir; daha önceki kaydın güncel durumunu döndürür.

Kök: `/api/v1/properties/{propertyId}/payments`

| Yöntem / yol | Davranış |
| --- | --- |
| GET kök | Sayfalı liste, filtrelenmiş tüm kayıtlara ait özet |
| GET `/{paymentId}` | Güncel version, ödeme/dağıtım ve varsa iptal geçmişi |
| POST kök | Atomik kayıt ve borç bakiyesini düşürme; yeni 201 + Location, replay 200 |
| PUT `/{paymentId}/cancel` | Gerekçe/version ile bir kez tersleme; ödeme ve dağıtım satırları korunur |

## Tahsilat oluşturma

```json
{
  "requestId": "d4ca0da3-eb8e-4da9-b0ea-c18416f9e34c",
  "unitId": "fe1bdfdd-962b-4b4c-aa29-ec132341c662",
  "amount": "100.25",
  "currency": "TRY",
  "paidDate": "2026-10-09",
  "method": "BANK_TRANSFER",
  "reference": "BF-EXAMPLE-001",
  "description": "Alınan havale",
  "allocations": [
    {"chargeId": "490a2896-3479-4ff6-87c5-a38d56e12d2a", "amount": "100.25", "version": 1}
  ]
}
```

Kimlikler ve tutarlar kurgusal örnektir; güncel borç GET/listesinden gerçek kimlik/version alınır. `amount` ve dağıtım tutarları ondalık string, pozitif, en fazla **99999999.99** ve en fazla iki ondalıktır; API noktayı kullanır ve yuvarlayarak kabul etmez. TRY desteklenir. Toplam, dağıtımların tam toplamına eşit olmalıdır; sıfır/fazla bakiye, dağıtılmamış avans ve farklı Unit/Property borcu kabul edilmez. Aynı borç listede bir kez bulunur.

`paidDate`: date-only, 2000 veya sonrası; **Europe/Istanbul bugününden ileri tarih reddedilir**. Yöntem CASH / BANK_TRANSFER; havalede trim sonrası 1–100 karakter referans zorunludur ve büyük harfe normalize edilir. CASH referansı opsiyoneldir. Açıklama trim sonrası 1–500 karakter.

Property satırı FOR UPDATE ile kilitlenir; her seçilen borcun version'ı ve kalan bakiyesi kontrol edilir. Payment/Allocation kayıtları, PaidAmount artışı ve audit/version tek transaction'da kaydedilir. Bir hata tüm dağıtımı geri alır. Aynı Property'de tahakkuk/borç iptali/ödeme/ödeme iptali aynı parent kilidini kullanır. Eski version veya bakiye aşımı 409; farklı/olmayan bölüm ya da borç 404. Sonradan pasif/soft-delete edilen Unit'in geçmiş borcu tahsil edilebilir; aktif Property ve üyelik kontrolü kaldırılmaz.

UUID + normalize fingerprint mükerrer istekleri korur: bölüm/tarih/yöntem/referans/açıklama, tutar ve **borç version'ları dahil** sıralanmış dağıtım aynıysa replay; aynı UUID farklı içerikte 409. BANK_TRANSFER için Property + normalize referans, iptal edilmemiş kayıtlarda DB partial unique index ile tektir. CASH'e otomatik referans tekilliği uygulanmaz. İptal edilmiş havale referansı düzeltme kaydında kullanılabilir; eski kayıt saklanır.

Frontend çift gönderimi kilitler; ağ/5xx belirsizliğinde otomatik mutation retry yapmaz. Aynı form içeriğiyle elle tekrar gönderim aynı UUID'yi kullanır; içerik/version değişirse yeni UUID oluşur. Form kapatılıp yeniden açılınca yeni UUID olur; mükerrer nakit girişi UUID sınırının dışında manuel kontrol gerektirir. Önce liste kontrol edilir. Replay edilen ödeme önceden iptal edilmişse UI yeni ödeme kaydedildi diye başarı göstermez.

## Yanıt / liste

Payment DTO: `id`, `propertyId`, `unitId`, nullable `buildingId`/`buildingName`, `unitNumber`, `requestId`, `amount`, `currency`, `paidDate`, `method`, nullable `reference`, `description`, `status` (RECORDED/CANCELLED), `version`, `createdAt`, nullable `cancellationReason`/`cancelledAt`, `allocations`, `replayed`. Dağıtım DTO: `id`, `chargeId`, `period` (YYYY-MM), `amount`. Para iki ondalıklı string; bölüm/bina adı güncel isimdir, ödeme tarihinde isim snapshot'ı değildir.

Liste parametreleri: `page` 1–1000000, `pageSize` 1–100, `search` ≤200, `unitId`, `chargeId`, `from`/`to` (ödeme tarihi), `status`, `method`, `sortBy` paidDate/amount/createdAt, `sortDirection` asc/desc. Varsayılan paidDate desc, son anahtar UUID. Arama bölüm numarası, bina adı, referans ve açıklamayı kapsar; `%`/`_` literal kabul edilir. `chargeId` borç detayındaki geçmişi getirir.

Yanıt `items/page/pageSize/totalCount/summary`; summary `recordedAmount`, `cancelledAmount`, `recordedCount`, `cancelledCount`, `currency`. Özet yalnız sayfayı değil **filtreye uyan bütün ödemeleri** hesaplar. Charge filtresindeki özet Payment başlığının toplamıdır; borç detayında ilgili dağıtım tutarı ayrıca gösterilir. Liste/count/özet/dağıtımlar aynı RepeatableRead snapshot'ında okunur. İptal kaydı geçerli tahsilat toplamına dahil değildir.

## Borç bakiyesi / iptal

DuesCharge `PaidAmount`, iptal edilmemiş dağıtımların toplamını taşır. Kalan bakiye amount − paid_amount; iptal borcunda 0.00. OPEN tamamen ödenmemiş/vadesi geçmemiş, PARTIALLY_PAID kısmen ödenmiş/vadesi geçmemiş, OVERDUE kalan bakiyeli/vadesi geçmiş, PAID tamamen ödenmiş, CANCELLED gerekçeli iptal. Europe/Istanbul vade günü gecikme sayılmaz. Durum zamanla eskiyen bir DB alanı değildir. `status=OUTSTANDING` ödenmemiş/kısmi/gecikmiş borçları seçer. Özet kalan bakiyeleri kullanır; PAID açık/gecikmiş toplamına eklenmez.

```json
{"reason": "Yanlış tahsilat kaydı", "version": 1}
```

İptal gerekçesi 1–500 karakter; version güncel olmalıdır. Property kilidi altında ödeme bir kez iptal edilir, dağıtımların tutarı borç PaidAmount'larından geri alınır; bakiye ve borç version/audit güncellenir. Ödeme/dağıtım satırları fiziksel/soft-delete edilmez. İkinci iptal ve eski version 409. Ödemesi bulunan borç doğrudan iptal edilemez; önce ilgili tahsilatlar terslenir. **Bu işlem gerçek banka iadesi değildir.**

## Şema / UI / sınır

`20261009040909_PaymentsAndAllocations`: `payments` ve `payment_allocations`, sekiz ortak NOT NULL BaseEntity kolonu; decimal(10,2), ortak audit/version, tarih/yöntem/TRY/toplam/history check'leri, request/ref index'leri ve Unit + Property kapsamını koruyan composite FK'ler. Eski borçlara `paid_amount=0` backfill yapılır; mevcut tutar/audit/version değiştirilmez. Migration/snapshot birlikte kaydedilir. Ödeme veya dağıtım geçmişi/ödenmiş bakiye varsa Down silmeye başlamadan reddedilir; boş kurulumda Down/re-Up test edilmiştir.

Kalıcı **Tahsilatlar** menüsü: `/payments` alan seçimi ve `/properties/{id}/payments` liste/özet/filtre. Yeni tahsilat ve güncel kayıt detayı ortak sağ ManagementPanel; borç detayından ödeme ve tahsilat geçmişi. TR/EN, güvenli ikonlu hatalar, odak/iptal, mobil RecordList ve kesin kuruş toplamı. Kayıt kimliğiyle görüntülenen detay **uygulama içi tahsilat belgesidir**, mali/fiskal makbuz değildir.

Online kart/POS sağlayıcısı, banka hareketi mutabakatı, gerçek para iadesi, PDF/fiskal belge, avans/fazla ödeme, borçlar arası sonradan dağıtım değişikliği, kişi sorumluluğu, gecikme faizi ve otomatik bildirim bu teslimde yoktur. Yanlış kayıt gerekçeli iptal + yeni kayıt ile düzeltilir. Testlerin gerçek kapsamı [TESTING.md](../TESTING.md), kurulum/yükseltme [MIGRATIONS.md](../MIGRATIONS.md).
