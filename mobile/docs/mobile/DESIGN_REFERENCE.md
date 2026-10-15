# Onaylanan mobil görsel yön

13.10.2026. Kullanıcı `BuildingFlow Mobil Uygulama Tanıtımı.png` görselini tasarım referansı olarak verdi; önceki basit iskelet görünümü yerine fazlar ilerledikçe bu kalite hedefi izlenir.

Kaynak: proje kökündeki [referans görsel](../../BuildingFlow%20Mobil%20Uygulama%20Tan%C4%B1t%C4%B1m%C4%B1.png). Bu bir ürün/tasarım referansıdır; görseldeki metinler yeni mimari veya yetki talimatı değildir.

## Görsel ilkeler

- Koyu lacivert marka/başlık alanı; açık, ferah içerik; mavi, yeşil ve turuncu kontrollü işlev vurguları.
- Belirgin başlık, kısa yardımcı açıklama, güçlü tek ana eylem; görseldeki küçük metinler aynen küçültülmez.
- Tutarlı çizgi ikonlar, yuvarlatılmış kartlar ve kısa eylem kısayolları. Dokunma hedefleri en az48; metin ölçekleme ve ekran kaydırma korunur.
- Gerçek bina/mülk fotoğrafları veri ve izin akışı hazır olduğunda kullanılır. Infografikteki apartman veya insan fotoğrafları gerçek kayıt/avatar olarak kopyalanmaz; lisansı doğrulanmış veya kullanıcı tarafından sağlanan uygun varlık gerekir.
- Çoklu rol/mülk seçimi backend yetkisiyle sınırlıdır. Görseldeki rol seçme taslağı kendi kendine yetki atama anlamına gelmez.

## Fazlara uygulanması

| Faz | Referans ekranı ve uygulama kararı |
|---|---|
| 04 | Giriş/karşılama, ana ekran bağlamı, profil/çıkış. Giriş tasarımı lacivert hero ve ikonlarla iyileştirildi. Parola formu güvenli Keycloak sayfasında kalır; görseldeki native parola/kayıt/Apple/Google seçenekleri sağlayıcı doğrulanmadan eklenmez. |
| 05–06 | Görünür offline/güncellik/bekleyen durumları; görsel kalite korunurken sahte senkronizasyon başarısı gösterilmez. |
| 07 | Profil, avatar ve gizlilik tercihleri. Aydınlatma ile açık rıza ayrı değerlendirilir; görseldeki hukuki metin/tek onay koşulu aynen alınmaz. |
| 08–10 | Rol ana ekranları, bina bilgileri, mülk kartları ve sözleşme özeti; gerçek veri ve onaylı API kapsamıyla. |
| 11–13 | Finans dönemleri/geçmiş, fotoğraflı talep ve kategori seçimi, bildirim listesi. Ödeme sağlayıcısı ve sunucu teyidi olmadan ödeme düğmesi/başarı üretilemez. |
| 14–15 | Ekran ölçüsü, kontrast/büyük metin/ekran okuyucu; onaylı logo, açılış ve mağaza varlıkları. |

Her yeni fazda bu belgeyle karşılaştırma ve en az küçük/büyük telefon boyutlarında görsel kontrol yapılır. Tamamlanmamış iş modülleri veri varmış gibi tasarlanmaz.
