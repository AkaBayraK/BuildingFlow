# İŞ10 — Raporlar, sayaçlar ve gözlemlenebilirlik

11.10.2026 kullanıcı Faz09 hızlı kontrolü ardından Faz10 geliştirmesini onayladı. İş yol haritasındaki kayıt13/14, dört resmi görev korunur. Tek agent; yeni skill yok.

## Kabul sınırları ve kararlar

1. Sayaçlar: yönetim alanı + isteğe bağlı aynı alandaki bağımsız bölüm; WATER/GAS m³, ELECTRICITY kWh. Tarihli, üç ondalıklı, sıfır veya pozitif kümülatif endeks. Aynı sayaç/tarih tek. İlk okuma başlangıçtır, tüketim bilinmez; sonraki tüketim önceki endeksten farktır. Araya ekleme/düzeltme iki komşuyla monotonluk kontrolü alır. Gerekçeli version kontrollü düzeltme immutable revizyonda önceki değerleri korur. UUID tekrar aynı içerikle tek sonuç, farklı içerikle409. Sayaç değişimi yeni sayaçtır; otomatik borç, katsayı/tarife veya bölüştürme yok.
2. Raporlar: tek yetkili Property ve YYYY-MM dönemi; ay sonundaki TENANT/RESIDENT ilişkisinden dolu bölüm sayısı (OWNER tek başına doluluk değildir), borç döneminden tahakkuk/bakiye, tahsilat paidDate ve gider occurredDate. Tüketim daha sonraki okuma tarihinin ayına yazılır; aylık oranlama değildir. Kişi adı/kimliği rapora girmez; ACCOUNTANT kişi yetkisi kazanmaz. Para string, tüketim üç ondalıklı; boş veri sıfır ve bilinmeyen tüketim ayrı.
3. Prometheus/Grafana: düşük cardinality istek/süre/hata, worker başarısızlık/işlem ölçümleri; veri kaynağı ve gerçek dashboard provision edilir.
4. Loki/OpenTelemetry: güvenli yapılandırılmış olaylar + trace/correlation kimliği; ham URL/query/body/token ve kişi verisi telemetriye eklenmez. OTLP pipeline ve Loki veri kaynağı. Trace deposu isteğe bağlı; bu teslimde Tempo zorunlu değil.

## Doğrulama planı

Önce negatif API/yetki, çapraz Property FK, parent lock/version/UUID replay, iki komşu endeks, düzeltme geçmişi, gerçek PostgreSQL ilk kurulum/yükseltme/tekrar/Down veri koruması. FE TR/EN/runtime DTO/OIDC menü/BigInt tüketim ve mobile ortak panel. Gözlemlenebilirlik configuration ve gerçek pipeline smoke; lisans ve korunan dosya eşliği. Çalıştırılan kanıtlar TESTING.md'ye kaydedilir; doğrulanmayan görev işaretlenmez.

## Teslim sonucu — 11.10.2026

**Dört resmi görev uygulanıp doğrulandı:4/4=%100.00.** Gerçek kanıt [TESTING.md](../TESTING.md), işaretler mevcut [yol haritasında](../../frontend/buildingflow-web/src/features/roadmap/milestones.ts). Son397 .NET/82 FE,typecheck/build,11 canlı pipeline kontrolü ve331 lisans geçti; TR/EN/512px sağ panel/390px mobil kullanıcı verisine yazmadan incelendi. 23 korunan dosya,17 eski tablo veri hash eşliği; yeni migration ve schema/model eşliği. Ayrıntılı sınırlar ve çalıştırma [API](../api/meters-reports.md) ve [OBSERVABILITY](../OBSERVABILITY.md). Otomatik borç/Tempo/tarihsel bakiye snapshot/üretim yayın hazırlığı teslim kapsamı değildir. İŞ11 onaylanmadı.
