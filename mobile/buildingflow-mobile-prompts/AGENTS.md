# BuildingFlow Mobile — Agent Talimatları

## Çalışma biçimi
- Tek seferde tek onaylı faz. Fazlar `PHASES.md` içinde tanımlıdır.
- Faz 01 salt okunur analizdir. Dosya yazma/değiştirme, kurulum veya otomatik biçimlendirme yoktur.
- Diğer fazlarda yalnızca ilgili fazın kapsamı. Kullanıcı onayı olmadan sonraki faza geçme.
- Başlamadan önce `git status --short` ve repo kurallarını incele; mevcut kullanıcı değişikliklerini koru.
- Her adımda Türkçe konsol ilerleme bilgisi ver; komut ve test sonuçlarını doğru aktar.

## Mobil ürün ilkeleri
- BuildingFlow için ayrı ve tutarlı marka dili; TraceFlow varlıklarını/arayüzünü kopyalama.
- Ev sahibi, kiracı ve yönetici iş akışları rol bazında anlaşılır; aynı kullanıcının çoklu rol/mülk senaryosu desteklenebilir.
- Mobil öncelikli düzen; erişilebilir kontrast, okunaklı yazı, büyük dokunma alanı, net geri bildirim.
- Boş, yükleniyor, hata, çevrimdışı, bekleyen senkronizasyon ve güncellik durumları tasarlanmalı.
- Fotoğraf izni bağlamsal ve isteğe bağlı akışlarla istenir; kullanıcıya fotoğrafın kullanım amacı ve silme/değiştirme imkânı anlatılır. Hukuki metin uydurulmaz.

## Veri ve güvenlik ilkeleri
- Mobil istemci hiçbir zaman veritabanına doğrudan bağlanmaz.
- Her cihaz/kullanıcı bağlamı yerel SQLite kullanabilir; bunun tek başına birden fazla cihazda ortak veri sağlamadığı açıkça belirtilir.
- Merkezi API'ye senkronizasyon yalnızca Faz 01'de ürün sahibi kararı ve backend sözleşmesi doğrulandıktan sonra uygulanır.
- Tokenlar güvenli platform deposunda tutulur. Hassas yerel veri için şifreleme, yedekleme, hesap değiştirme ve cihaz kaybı davranışları doğrulanır.
- Yerel işlem kuyruğunda işlem kimliği, durum, deneme sayısı, zaman damgası ve çakışma sonucu izlenir. Sunucu tarafı idempotency desteklenmeden tekrar deneme tasarlanmaz.
- Sadece yetkili kullanıcının verisi önbelleğe alınır. Loglarda kişisel veri bulunmaz.

## Kalite kapısı
- Fazın kabul ölçütlerini karşıla.
- Uygun birim/UI/integrasyon testlerini ve lint/typecheck/build kontrollerini çalıştır.
- UI değişikliklerinde mümkünse iOS ve Android boyutlarında görsel kontrol yap.
- Her faz sonu Türkçe rapor: kapsam, değişen dosyalar, doğrulama sonuçları, bilinen sorunlar ve sonraki adım.

## Öncelikli kalite ve token protokolü (v2 ek)
- Önce `TOKEN_OPTIMIZATION.md`, `PHASE_EXECUTION_PROTOCOL.md` içindeki kısa yürütme kurallarını uygula; ilgili riskte `LICENSE_POLICY.md`, `SECURITY_STANDARDS.md`, `CODING_STANDARDS.md`, `PERFORMANCE_RULES.md` başlıklarına başvur.
- Fazların iş mantığını, mevcut kodu ve mevcut dosyaları koru; eski kurallarla çelişirse daha güvenli, dar kapsamlı ve açık onay gerektiren kural geçerlidir.
- `MASTER_PROMPT.md` ve `PHASES.md` dosyalarını her alt adımda yeniden okumak zorunda değilsin; yalnız ilgili bölümü oku.
- Yeni dependency eklemeden lisans kontrolü ve eşdeğer mevcut çözüm araştırması yap.
- Aynı işlev için mükerrer method, servis, component veya veri erişim katmanı oluşturma.
- Testleri hedefli çalıştır; tüm suite'i yalnız risk gerektirirse çalıştır.
- Türkçe, kısa, gerçek ilerleme ve test sonucu bildir; token sayısı uydurma.
