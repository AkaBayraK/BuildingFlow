# BuildingFlow — Araçlar / Otopark MASTER PROMPT

Repo: AkaBayraK/BuildingFlow. Amaç: mevcut modülü bozmadan çoklu site/apartman, blok, daire, kişi, malik/kiracı/sakin ilişkilerine uygun güvenli ve premium otopark yönetimi.

## Değişmez kurallar
1. Önce mevcut repo ve AGENTS.md / MASTER_PROMPT.md / PHASES.md dosyalarını oku; çelişirse mevcut proje standardını ve kullanıcı onayını esas al. Kod tabanını incelemeden teknoloji, dizin, şema veya çalıştırma komutu varsayma.
2. Sadece onaylanan fazı uygula; FAZ-01 yalnız analiz ve rapor, kod değişikliği yok. Her faz bitince dur ve kullanıcı onayı bekle.
3. Minimal değişiklik, düşük token, yüksek kalite; SOLID/DRY/KISS; mükerrer servis/endpoint/tablolar oluşturma; mevcut .NET, Next.js, React Native, Keycloak, PostgreSQL, SQLite ve bildirim altyapısını uygun olduğu ölçüde yeniden kullan.
4. Güvenlik: tenant/yönetim alanı/daire düzeyinde sunucu tarafı yetkilendirme, veri minimizasyonu, KVKK, audit, log maskeleme, yarış durumları, kapasite ve rezervasyon çakışması. Kamera/ANPR/bariyer opsiyonel ve ayrı onay gerektirir.
5. Lisans: yeni paket, font, ikon, fotoğraf, SDK ve transitif bağımlılıklar için SPDX, ticari/SaaS/dağıtım koşulları, NOTICE ve varsa copyleft/network-copyleft yükümlülüklerini doğrula. Belirsiz veya uygunsuz bağımlılığı ekleme. Tasarım referans görseli sadece yönlendirme; lisanssız üçüncü taraf fotoğrafları ürüne kopyalama.
6. Her faz: unit/integration/UI/E2E ilgili testleri gerçekten çalıştır, sonuçları dürüstçe raporla; CI bağlantıları ve commit SHA paylaş. Test başarısız veya çalıştırılamadıysa başarı iddiasında bulunma.
7. GitHub: Her faz için mevcut ilgili Issue'yu kullan, kopya açma. Başlamadan Issue'yu oku. İş listesi ve kabul kriterlerini güncelle; ilerleme yorumlarında değişen dosyalar, commit/PR, test komutları/sonuçları, güvenlik/lisans bulguları, ekran görüntüleri, riskler, rollback ve sonraki adımı ayrıntılı yaz. Tüm checklist + test + CI + kullanıcı onayı tamamlanmadan Issue'yu kapatma. Onaydan sonra completed olarak kapat. Bir fazın Issue'su kapatılmadan sonraki faza geçme.
8. Her adımı Türkçe konsola yaz: [FAZ xx][ADIM yy][DURUM] yapılan iş, test ve kalan token/limit bilgisi yalnız araç gerçekten sağlıyorsa göster. Sahte metrik üretme.
9. Görsel referans: assets/otopark-ui-referans.png. Ekranların tasarım dilini referans al; iş kurallarını görselden çıkarsama.
10. GitHub'a push/PR açma, şema migration çalıştırma, üretim verisi değiştirme gibi etkili işlemlerde önce kullanıcı onayı al; varsayılan olarak ayrı branch üzerinde çalış.

## Her faz için zorunlu rapor şablonu
- Mevcut durum / değişen dosyalar
- Tamamlanan kabul maddeleri [x] ve eksikler [ ]
- API, DB, UI, mobil değişiklikleri
- Test komutları, geçen/kalan/atlanan sayıları, CI URL
- Güvenlik ve KVKK değerlendirmesi
- Lisans/SBOM ve görsel hakları kontrolü
- Commit SHA / PR ve ekran görüntüleri
- Risk, rollback ve açık kararlar
- Kullanıcı onayı bekleniyor mu?

## Başlatma
Yalnız FAZ-01.md dosyasını uygula; mevcut kodu değiştirme, Issue'ya analiz ve test altyapısı tespitlerini yaz, dur ve onay bekle.
