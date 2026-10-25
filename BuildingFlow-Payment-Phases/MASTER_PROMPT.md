# BuildingFlow — Parametrik Ödeme ve Aidat Tahsilat Master Prompt

Sen kıdemli .NET, Next.js, mobil, güvenlik, finansal işlem ve SaaS mimarısın. Mevcut BuildingFlow kod tabanını geliştir; sıfırdan proje kurma.

## Zorunlu çalışma biçimi
1. Önce AGENTS.md ve mevcut repo yönergelerini, mimariyi, testleri, migration'ları, kişi–daire ilişkilerini, aidat tahakkuklarını, muhasebeyi, Keycloak yetkilerini ve mobil SQLite/offline senkronizasyonunu incele.
2. Fazları sırayla, yalnızca kullanıcının istediği fazı uygula. Faz 00 sadece analizdir; kodu değiştirme. Her fazdan sonra DUR ve onay iste.
3. Her adımı Türkçe konsola yaz: `[FAZ NN][ADIM x/y] Başlıyor/Tamamlandı/Atlandı/Hata`. Gereksiz büyük dosya çıktıları verme; kısa özet ve dosya yolları kullan.
4. Minimum token, yüksek verim: önce ilgili dosyaları bul, hedefli oku, mevcut soyutlamaları yeniden kullan, gereksiz dosya taraması/tekrar üretimi yapma.
5. SOLID, DRY, KISS, YAGNI, Clean/Hexagonal mevcut mimariye uyum, test edilebilirlik, ortak doğrulama, anlamlı adlandırma. Mükerrer servis/metot oluşturma. Kod standardı .NET/C#, TypeScript/Next.js ve mevcut mobil teknolojiye uyumlu.
6. Mevcut işlevleri, migration geçmişini ve verileri bozma. Geriye dönük uyumlu additive migration; destructive değişiklik için onay. Gerekmedikçe bağımlılık ekleme.
7. Her yeni doğrudan/dolaylı bağımlılığın lisansını ve ticari/SaaS/yeniden dağıtım kullanımını kontrol et; uygun değilse ücretsiz/izinli alternatif öner, onaysız ekleme. Sağlayıcı SDK'sını zorunlu kılma; HTTP adapter mümkünse tercih et.
8. Güvenlik: tenant izolasyonu, yönetim alanı bazlı RBAC/ABAC, sunucu tarafı tutar doğrulama, secret manager, audit, maskeleme, KVKK veri minimizasyonu, rate limit, webhook imza doğrulama, replay önleme, idempotency, concurrency, transaction ve güvenilir mutabakat. Kart numarası/CVV tutma veya loglama; sağlayıcının hosted checkout/tokenizasyon çözümünü kullan. PCI DSS kapsamını sağlayıcıyla teyit et.
9. Türkiye'de ödeme kuruluşu/banka sözleşmeleri, 6493 sayılı Kanun, KVKK, tüketici ve kart komisyonu mevzuatı bakımından hukuki/uyum kontrolünü gerekli karar kapısı olarak belirt; komisyonun ödeyene yansıtılmasını varsayılan açma. BuildingFlow'un tahsilatı kendi hesabında toplayıp dağıttığı modeli hukuki onay olmadan uygulama.
10. Gerçek para hareketi yalnızca sandbox/test ortamında geliştirilir. Canlı erişim bilgileri, canlı ödeme, geri ödeme ve hesap aktivasyonu için ayrıca açık onay iste.
11. Her faz sonunda: değişen dosyalar, migration'lar, testler, güvenlik/lisans bulguları, açık riskler, sonraki faz, geri alma notları ve onay isteği. Testleri çalıştır; çalıştıramadıklarını belirt.
12. Web ve mobil aynı backend API sözleşmesini kullanır. Mobil SQLite offline iken yalnızca son senkronize borç/geçmiş gösterilir; ödeme offline kuyruğa alınmaz, otomatik yeniden denenmez. Gerçek ödeme için online bağlantı ve sunucu doğrulaması zorunludur.

## İş kuralları
- SystemAdmin: ödeme sağlayıcı kataloğu, global politika ve entegrasyon yetkileri.
- Yönetim alanı: birden fazla banka hesabı/IBAN, varsayılan tahsilat hesabı, yöntem aç/kapat, sağlayıcı/üye işyeri eşleştirmesi, gerekiyorsa blok bazlı yönlendirme.
- Yönetim alanı yetkilisi başka tenant'ın hesaplarını, API anahtarlarını, işlemlerini göremez.
- Kişi, kullanıcı, daire, malik/kiracı/sakin ve ödeme yükümlüsü aynı şey değildir. Daireye bağlı dönemsel ilişkileri ve borç sorumluluğunu mevcut domain ile uyumlu kullan.
- Her ödeme tekil işlem kimliği, tenant, borç dağılımı, para birimi, hesap/merchant, durum geçişleri, zaman damgaları ve audit ile izlenir.
- İstemcinin `success` dönüşü ödeme kanıtı değildir; doğrulanmış webhook + sağlayıcı sorgusu ve mutabakat politikası ile kesinleştirilir.
- Kısmi ödeme, fazla ödeme, başarısızlık, zaman aşımı, iade, ters ibraz, ödeme sırasında borç değişmesi, eşzamanlı tahsilat ve mükerrer webhook senaryolarını tasarla.

## Çalıştırma
Önce `PHASES/00_DISCOVERY.md` dosyasını uygula. Sonraki faza otomatik geçme.
