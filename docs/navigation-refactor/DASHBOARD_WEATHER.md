# BAKIM — 21.10.2026 — Kişisel şehir ve hava servisi seçenekleri

Artvin, eski `DashboardHero` kodunun `Property.city` alanını göstermesinden geliyordu. Kullanıcının konumu okunmuyor, canlı sıcaklık servisi de bulunmuyordu. Şehir artık yönetim alanı adresinden bağımsız kişisel tercihtir; ilk değer kullanıcı talebiyle İstanbul. Şehir adına basılınca mevcut Dialog/Input/Button ile değiştirilir. Tercih yalnız tarayıcıda, kullanıcı subject'iyle ayrılmış localStorage anahtarında tutulur; auth token saklanmaz, GPS/IP konumu okunmaz. Hatalı kayıt/engellenmiş depolama güvenli varsayılan veya yalnız sayfada kullanılacak tercih ile ele alınır. Property kaydı değişmez.

Mevcut banner Yenile eylemi yalnız dashboard/belge verisini yeniler; canlı hava getiriyormuş gibi yeni bir düğme eklenmedi. Sıcaklık —°C ve “Canlı hava servisi bağlı değil” olarak dürüstçe gösterilir. Kullanıcı canlı servis **seçeneklerinin hazırlanmasını** onayladı; sağlayıcı bağlantısı veya abonelik henüz onaylanmadı.

## Karşılaştırma (resmî kaynaklar, 21.10.2026)

| Seçenek | Kullanım ve maliyet | Uygulama koşulları |
|---|---|---|
| MET Norway Locationforecast — öneri | Ücretsiz açık meteorolojik tahmin verisi; CC BY 4.0 / NLOD, atıf gerekir. SLA yok. | Uygulama/iletişim bilgisini tanımlayan User-Agent; sunucu proxy/cache; Expires ve Last-Modified/If-Modified-Since kurallarına uyum. İl merkezi koordinatı yeterli; kesin kullanıcı konumu gerekmez. |
| Open-Meteo | Ücretsiz API ticari olmayan kullanım içindir; ticari uygulama için ücretli abonelik ve API anahtarı gerekir. Veri CC BY 4.0, atıf gerekir. | Anahtar yalnız sunucuda, aynı şehir için cache, ücret/kota seçimi. |

Kaynaklar: [MET koşulları](https://api.met.no/doc/TermsOfService), [MET lisansı](https://api.met.no/doc/License), [MET ücretsiz veri](https://www.met.no/en/free-meteorological-data), [Locationforecast dokümanı](https://api.met.no/weatherapi/locationforecast/2.0/documentation), [Open-Meteo ticari kullanım/fiyatlandırma](https://open-meteo.com/en/pricing). Sağlayıcıya ait ikon veya görsel eklenmeyecek; mevcut lucide ikonları yeterli. Hava servisi kütüphanesi gerekli değil: mevcut sunucu platformu HTTP API'leri yeterli.

## Seçim sonrası önerilen davranış — henüz uygulanmadı

Kullanıcı şehir seçer; İstanbul varsayılandır. Sunucu seçili şehir merkezi için güncel model tahminini alır, cache başlıklarına uyar. Görünür sayfada süreli yenileme + manuel Yenile + son güncelleme zamanı; manuel düğme sağlayıcı cache kurallarını atlamaz. Hata halinde son veri eski olarak işaretlenir; veri yoksa — gösterilir. Bu anlık bir istasyon ölçümü gibi tanıtılmaz. Kullanıcının kesin konumu, IP tabanlı geolocation, yeni paket veya sağlayıcı ikonları kullanılmaz. MET tanımlama için gerçek uygulama/iletişim adresi ayrıca netleştirilir; hayali adres yazılmaz.

Kök AGENTS.md “Yeni lisans yükü yok” kararı nedeniyle veri lisansı/atıf yükümlülüğü kullanıcı seçimi olmadan etkinleştirilmedi. Bu dosya uygulanan sağlayıcı entegrasyonu veya yeni faz/backlog değildir.

## Kullanıcı onayı sonrası uygulama — 21.10.2026

MET Norway entegrasyonu onaylandı ve artık uygulanmıştır; yukarıdaki “henüz uygulanmadı/bağlanmadı” ifadeleri önceki seçenek hazırlama turunun tarihsel durumudur. API sözleşmesi [dashboard.md](../api/dashboard.md), veri lisansı/atıf kaydı [licenses.md](../licenses.md). Canlı GET İstanbul için HTTP200, Celsius/90 tahmin kaydı ve Expires/Last-Modified doğrulandı. Canlı veri bir istasyon ölçümü diye sunulmaz.

İlk destek İstanbul/Ankara/İzmir/Artvin şehir merkezleridir; serbest şehir metni yerine seçim kullanılır. Daha önce başka şehir kaydedilmişse sessizce yanlış şehir tahmini verilmez, seçim istenir. Bu kapsamdaki sabit yaklaşık koordinatlar uygulamaya ait sayısal sabitlerdir; dış geocoding servisi/konum izni yoktur.

**Kabul:** frontend195/195, typecheck/build PASS; backend11/11 (6 sağlayıcı+5 API),0fail/skip. [Makine kanıtı](DASHBOARD_MET_WEATHER_EVIDENCE.json), [görsel](DASHBOARD_MET_WEATHER.jpg). UI gerçek React/örnek verili fixture, canlı sağlayıcı GET ayrı doğrulandı; gerçek OIDC/üretim dağıtımı/gerçek cihaz testi değildir. Otomatik timerın dakika dolumu ayrıca beklenmedi; kod/visibility kapıları incelendi. Başlangıç Git indeksi/manifest/lock/next-env korundu; migration ve mevcut kullanıcı verisi değişmedi.
