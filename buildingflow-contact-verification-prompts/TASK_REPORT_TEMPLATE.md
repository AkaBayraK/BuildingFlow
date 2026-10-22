# BF-VERIFY — Görev teslim ve kapanış şablonu

Bu şablonu ilgili GitHub görevinin **Yapılanlar / Doğrulama / Eksikler / Kapanış** bölümlerine gerçek sonuçlarla doldur.
Boş alan veya yer tutucu sonuç gerçek kanıt değildir. Genel “kodlandı/test edildi” açıklamasıyla kapatma.

## Kimlik ve kapsam
- BF-VERIFY FAZ NN / sıra N/08, GitHub issue URL ve Project 3 öğesi.
- Kullanıcının yetkilendirdiği kapsam, bu fazın bağımlılıkları, değişen kapsam varsa kullanıcı kararı.
- Resmi adım: 3/3 (ancak gerçekten bittiyse); faz ve refactor planı ilerlemesi ayrı.

## Yapılanlar — ayrıntılı maddeler
1. **Kabul maddesi PNN.A1:** Hangi davranış eklendi/değişti, neden; gerçek dosya yolları ve ilgili semboller.
2. **API/DTO:** Etkilenen uçlar, alan/null/durumlar, auth actor/scope; önce-sonra davranış.
3. **Veri:** Entity/migration/snapshot, mevcut veri ve audit/version koruması; rollback, yeni değer/backfill kararı. Yoksa “değişmedi”.
4. **İstemciler:** Mobil, merkez/yönetim, eski sürüm ve çapraz cihaz etkisi; açık hata/boş/çevrimdışı/güncellik davranışı.
5. **Güvenlik:** Yanlış hesap/kod/hedef, replay/süre/limit/version, secret/PII koruması; yetki genişlemesi varsa açık kapsam.
6. **Bağımlılık/lisans:** Gerçek manifest/lock farkı; yeni yük yoksa bunu yaz. Gerçek servis seçimi/lisans/ücret onayı ayrı.
7. **Diğer kabul maddeleri:** Her maddeyi aynı açıklıkta ayrı işlem ve kanıta bağla; sırf yer tutucu var diye tikleme.

## Doğrulama

| Kabul / CV kimliği | Komut veya etkileşim | Ortam/fixture | Gerçek sonuç | Kanıt dosyası |
| --- | --- | --- | --- | --- |
| PNN.A1 / CVxx | Gerçekte çalıştırılan | İzole/test/canlı yetki | GEÇTİ / BAŞARISIZ / DOĞRULANAMADI / UYGULANMAZ | Yol ve gerekçe |

- Uygulama testi çalıştırılmadıysa açıkça yaz. Önceki tur sonucunu yeniden çalıştırılmış gibi sunma.
- Gönderim kabulü ≠ teslim ≠ kullanıcı sahiplik onayı. MOCK yalnız test kanıtıdır.
- Zorunlu kontrol atlandıysa veya başarısızsa kabul maddesi açık kalır.

## Eksikler, engeller ve kalan iş
- Her açık madde, nedeni, etkisi ve çözüm için gereken veri/karar/ortam.
- Eksik yoksa ancak inceleme ve kanıt sonrası “Yok” yazılır.
- Zorunlu gerçek SMS/SMTP/cihaz/ortam kanıtı eksikken task kapanmaz.

## Kapanış
- Güncel görev yeniden okundu mu; tüm kapsam ve kapanış checkbox'ları gerçek kanıtla tamam mı?
- Faz raporu ve TESTING/STATUS/SESSION/CHANGELOG bağlandı mı; başka planların görevleri korunuyor mu?
- Kaynak/manifest/lock koruma sonucu; commit/push durumu.
- Şartlar sağlanınca issue → Closed (completed), Project 3 → Done. Sonrasında iki durum yeniden okunur.
- Tamam değilse açık durum ve sonraki çözüm adımı; kullanıcıdan gereksiz kapanış onayı isteme.
- Sonraki faz ayrıca başlatılmadıysa dur.

## Kaynak kullanımı
Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL.
Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
Güvenilir veri sağlanırsa gerçek değerle değiştir. Codex usage varsa hesap genelindeki her pencerenin kullanılan/kalan yüzdesi, süresi, reset Europe/Istanbul ve okuma kaynağı; kota sohbet token sayısı değildir.

