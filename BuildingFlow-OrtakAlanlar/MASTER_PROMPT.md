# BuildingFlow — Ortak Alanlar MASTER PROMPT

## Kullanıcı kararı — 26.10.2026: yönetici yazar, diğer kullanıcılar görüntüler

- Ortak alan tanımı ve rezervasyon oluşturma/düzenleme/iptal gibi yönetim işlemleri yalnız yetkili SUPER_ADMIN/SITE_ADMIN/MANAGER ve backend kapsam kontrolüyle yapılır.
- Yönetici dışındaki kullanıcılar web/merkez ve mobil ekranlarda yalnız yetkili oldukları ortak alanları ve rezervasyon saatlerini salt okunur görür. Rezervasyon oluşturma/değiştirme/iptal veya rezervasyon talebi gönderme yetkileri yoktur; ACCOUNTANT yönetici sayılmaz.
- Örnek: kapalı halı saha detayında tarih seçilince dolu/rezerve, boş ve bakım nedeniyle kapalı saat aralıkları gösterilir. Başka kullanıcıların ad/telefon/açıklama/ödeme bilgileri takvimde yayımlanmaz. Salt zaman ve kullanım durumu görünür.
- Okuma Property/tenant ve varsa blok görünürlüğünü izler; istemcide düğmeyi gizlemek yeterli değildir, yazma uçları backend'de de reddedilir. Web ve mobil aynı sunucu yetkisini/verisini kullanır.
- Offline görüntü son güncelleme zamanıyla eski önbellek olarak belirtilir; boş saat garantisi vermez. Yönetici kesin rezervasyon/iptal işlemi yalnız online sunucuda tamamlanır.
- Bu karar görseldeki yönetici dışı “Rezervasyon Yap/Onayla” akışının yerine geçer. FAZ02 şu an yalnız katalog sağlar; saat/rezervasyon motoru FAZ04, web görünümü FAZ03 ve ilgili entegrasyon, mobil FAZ06 kapsamındadır. Bu kayıt sonraki fazı başlatmaz.


Bu dosyayı Codex veya Claude Code'a ver. Önce repo içindeki AGENTS.md, CLAUDE.md, mimari belgeler, mevcut ortak alan/rezervasyon/planlama-bakım modülleri ve test altyapısını oku. İlk faz yalnızca analizdir; mevcut kodu değiştirme. Sonraki fazlara kullanıcı onayıyla sırayla geç. Bir faz bitmeden diğerine geçme.

## Zorunlu ilkeler
- Var olan .NET backend, Next.js/TypeScript/Tailwind/shadcn UI frontend, PostgreSQL, Keycloak ve mobil/offline mimarisini yeniden kullan; varsayımla yeni servis/stack ekleme.
- SOLID/DRY/KISS, tutarlı DTO, validation, pagination, optimistic concurrency, migration rollback, soft-delete, audit, i18n ve erişilebilirlik.
- Kullanıcıya yönetim alanı/blok/daire bağından türeyen rollerle erişim ver. İstemciden gelen tenant/area ID'ye güvenme; sunucuda yetkilendir. IDOR, CSRF, XSS, injection, rate limit, PII loglama, timezone, yarış durumu ve rezervasyon çakışmalarını test et.
- Mobilde kullanıcılar için salt okunur önbellek, yalnız yönetici için uygun işlemlerde offline taslak destekle; rezervasyon kesinleştirme, uygunluk, ücret/ödeme ve onay yalnızca online/sunucuda. Offline kesin rezervasyon vaadi verme.
- Fotoğraflar yetkili depoda, güvenli dosya türü/boyut doğrulaması, telif ve kullanım hakkı kontrolüyle saklanmalı. Görsel referans assets/ortak-alan-ekran-akisi.png; bunu aynen uygulamaya gömme zorunluluğu yok; özgün arayüz bileşenleri geliştir.
- Ticari/SaaS/yeniden dağıtım kullanımına uygun lisans ve dolaylı bağımlılıkları ilk eklemeden önce doğrula. SPDX/SBOM ve dependency/license taraması yap. Belirsiz, copyleft/AGPL/SSPL veya ek ticari koşul içeren bileşenleri hukuk onayı olmadan ekleme. Mevcut bağımlılıklar için risk raporla; sıfır risk garantisi verme.
- Her fazda yalnızca ilgili testler, lint, typecheck, build, güvenlik ve lisans kontrolü; son fazda tam regresyon ve E2E. Test çalıştırılmadıysa geçti yazma.
- Her işlemde Türkçe konsol durumu: [FAZ XX][ADIM a/b] Başladı/Bitti/Engellendi, değişen dosyalar, test sonucu, kalan risk.
- GitHub repo: AkaBayraK/BuildingFlow. Her fazın mevcut Issue'sunu bul; işaretli checklist'i gerçek kanıtla güncelle; commit/PR/test çıktısı/ekran görüntüsü/karar/risk bilgilerini ayrıntılı yorumla. Yalnızca tüm kabul kriterleri ve testler sağlanıp kullanıcı onayı verildiğinde Issue'yu completed olarak kapat. GitHub yetkisi yoksa komut ve taslak üret, başarılı işlem iddiasında bulunma.
- Her faz sonunda dur ve sonraki faz için kullanıcı onayı bekle. Mevcut iş akışlarını bozan refactor yapma; gerekli değişiklikleri raporla.

## Fazlar
1. Mevcut repo analizi, domain/rol/mimari haritası, lisans envanteri, tasarım eşlemesi; kod değişikliği yok.
2. Alan tipleri, yönetim alanı/blok görünürlüğü, kullanım kuralları, ücret parametreleri, migration ve domain/API.
3. Premium kurumsal yönetim UI: kart/listeler, detay, görsel galeri, durum, arama, filtre, erişilebilirlik, responsive ve i18n.
4. Rezervasyon motoru: slot, kota, kişi sınırı, onay, idempotency, çakışma, timezone, transaction ve yetki.
5. Bakım/planlama entegrasyonu, kullanıma kapatma, duyurular/bildirim ve çift kayıt önleme.
6. Mobil kullanıcı akışı: yönetim alanı seçimi, liste, detay, salt okunur rezervasyon takvimi; yönetici işlemleri rol ile ayrılır, offline önbellek güncelliği/senkron çatışma.
7. Yönetici takvimi, onaylar, iptal, raporlama, ücretli kullanım ve ödeme entegrasyonu (varsa mevcut sağlayıcı üzerinden; ödeme yeni baştan geliştirilmez).
8. Tam test, erişilebilirlik, güvenlik, lisans/SBOM, performans, Docker smoke, dokümantasyon, demo ve release kararı.

## GitHub Issue kapanış şablonu
- Tamamlanan maddeler (dosya ve commit/PR referansları)
- API ve DB migration değişiklikleri
- UI ekran görüntüleri (gerçek çalışan ekran)
- Test komutları, sayıları, sonuçları ve CI bağlantısı
- Güvenlik/KVKK/lisans tarama sonuçları ve açık riskler
- Geri alma planı
- Kullanıcı kabul/onay durumu

Kapanış kuralı: Checklist %100 + test kanıtı + açık kritik/yüksek bulgu yok + kullanıcı onayı. Aksi halde Issue açık kalır.
