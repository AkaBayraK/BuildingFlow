# BF-LATE FAZ 01–07 — Gecikme tazminatı teslim raporu

Tarih: 23.10.2026. Analiz: [PHASE_00_ANALYSIS.md](PHASE_00_ANALYSIS.md). API: [late-penalties.md](../../api/late-penalties.md).

## Kullanıcı kararları

| # | Karar | Durum |
|---|---|---|
| K0 | "Gecikme tazminatını uygula" seçilirse uygulanır, aksi halde uygulanmaz | Uygulandı: Property bazlı insert-only anahtar, kayıt yoksa kapalı |
| K1 | Gün esası | Kullanıcı onayı: aylık oran × gün / 30 (`DAILY_30`), tek yöntem |
| K2 | Kısmi ödeme mahsubu | Kullanıcı kararı (23.10.2026): operatörün girdiği dağıtım uygulanır; TBK m.100/101 yalnız öneri |
| K3 | %5'ten farklı oran | Kullanıcı kararı: yalnız kanuni örnek (aylık %5, gün/30, tazminata tazminat yok); farklı oran/yöntem kapalı, gerekirse ayrıca değerlendirilecek |

## Fazlar

| Faz | Sonuç |
|---|---|
| 01 | `LatePenaltySwitch`, `LatePenaltyPolicy` (BaseEntity, version, audit, soft-delete yok/insert-only, CHECK'ler); taslak/onay/iptal/kısaltma API'si |
| 02 | Domain `LatePenaltyCalculator` (saf, decimal), `LatePenaltyAccrual` + `LatePenaltySegment`, `LatePenaltyEngine` (kesinleştirme, yeniden kurma), Worker ay sonu job'ı |
| 03 | `PaymentAllocation.kind/accrual_id`; ödeme anında kesinleştirme, tazminat dağıtımı, geriye dönük ödeme ve iptalde yeniden hesap, borç iptali koruması; fingerprint geriye uyumlu |
| 04 | Aidat modülünde ikonlu sekmeler: Borçlar / Gecikme tazminatı / Politikalar / Rapor; hesap detayı + muafiyet paneli, politika editörü ve onay paneli, tarihsel oran çizelgesi, CSV (Excel) ve yazdır/PDF; ödeme formunda ayrı tazminat tutarı ve yalnız tazminat kalan borçlar; TR/EN ve 13 hata kodu |
| 05 | Mobil finans: borç satırında ana borç ve tazminat ayrı, kesinleşmiş özet, tahsilat türü, dönem detayında sunucu hesap kalemleri; çevrimdışı kopya mevcut uyarıyla; mobil hesaplama yok |
| 06 | Yıllık rapor (tahakkuk/tahsilat/muafiyet/açık), oran ve anahtar geçmişi, gecikmiş borç toplamları; tüm değişiklikler kim/ne zaman/gerekçe ile saklanır |
| 07 | Test ve güvenlik: aşağıda |

Migration: `20261023125757_LatePenalties` (+Designer, snapshot). Down, tazminat geçmişi varsa reddeder. Yeni paket yok.

## Test senaryoları (FAZ 07 listesiyle eşleme)

Unit `LatePenaltyTests`: vadesinde ödeme, 1 gün, 30 gün, yıl geçişi, aynı yıl oran değişimi, geçmiş yıl borcu, kısmi ve birden çok kısmi tahsilat, tam ödeme, anahtar kapalı/sonradan açık, gerçek ay yöntemi, yuvarlama sınırları, %5 üstü oran reddi, onay kuralları, dönem çakışması, FIFO tazminat dağıtımı ve fazla tazminat ödemesi reddi, muafiyet geçmişi.

Entegrasyon `LatePenaltyApiTests` / `LatePenaltyMigrationTests`: anahtar varsayılan kapalı ve geçmiş tarih reddi, replay/409, idempotent kesinleştirme ve tekrar çalışan ay sonu job'ı, ödeme+iptal ile tazminatın geri alınması, geriye dönük tahsilat yeniden hesabı ve ödenmiş tahakkuk engeli, eşzamanlı iki tazminat ödemesi, rol/yetki (MANAGER onaylayamaz, ACCOUNTANT taslak açamaz, sahte roller 403), tenant izolasyonu (üye olmayan 404), kanuni dışı oran onayı, çakışma, kesinleşmiş döneme oran, muafiyet, tazminatı ödenmiş borcun iptal engeli, mobil finans tazminat alanları, migration yükseltme/tekrar/Down koruması.

Kapsam dışı/karşılanamayan senaryolar: mükerrer ödeme callback'i ve online ödeme mutabakatı (sağlayıcı yok), malik/kiracı değişimi (borç Unit'e ait; muhatap snapshot'ı 06.1 testlerinde), offline mobil görüntüleme mevcut önbellek testleriyle.

## Bu turun sonuçları

Unit 337/337; gecikme entegrasyon 11/11; tam entegrasyon (bağlantı kotası sonrası yeni DB'de tekrar) 714 geçti / 2 ilgisiz başarısız / 43 ortam nedeniyle atlandı; web 249/249; mobil check 132/132 ve build; EF model check ve lisans kontrolü geçti. Ayrıntı TESTING.md.

## Açık riskler

- Sakinlere vade/gecikme bildirimi gönderilmiyor: push tüketicisi ve borçlu kişiye bağlı rızalı e-posta/SMS akışı yok.
- Muhasebe/hesap planı modülü yok; tazminat ayrı dağıtım türü ve raporla izleniyor.
- Web `npm run build`, paralel iletişim doğrulama işinin izlenmeyen `tests/fixtures/verification-entry.tsx` tip hatasında duruyor; bu çalışmanın dosyaları `tsc` temiz.
- Ödeme ekranının canlı tarayıcı kabulü yapılmadı (yerel Keycloak oturumu gerekiyor).
