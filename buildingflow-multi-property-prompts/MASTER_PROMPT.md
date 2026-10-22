# BuildingFlow — Çoklu Yönetim Alanı ve Bağımsız Bölüm Refactor / MASTER PROMPT

Sen mevcut BuildingFlow kod tabanında çalışan kıdemli .NET, Next.js, React Native ve güvenlik mimarısın. Bu dosyayı ve PHASES.md'yi oku. Mevcut uygulamayı sil baştan yazma. Her çalıştırmada SADECE kullanıcının seçtiği fazı uygula; faz bitince dur ve onay bekle.

## Hedef
Tek bir Person ve tek bir kimlik hesabı, farklı management area, bina/blok ve unit'lerde birden fazla OWNER, TENANT, OCCUPANT ilişkisine sahip olabilir. Yönetici/merkez yönetim ekranı, malik-kiracı web portalı ve mobil uygulama aynı yetkili ilişki modelini kullanır. Kişi listesi tenant sınırları ve yetkilendirme gözetilerek yönetim alanı seçimi zorunlu olmadan açılır. Farklı tenant'ların kayıtlarını yetkisiz birleştirme.

## Önce keşfet
Repo yapısı, var olan Person/Unit/ManagementArea/Lease/Ownership, auth/Keycloak, mevcut kullanıcı-kişi eşleştirmesi, backend yetki kuralları, mobil teknoloji ve SQLite altyapısı, mevcut migrations, API ve testleri araştır. İsimleri varsayma. İlgili dosyalara odaklan; tüm projeyi her fazda tekrar tarama. Analiz çıktılarını docs/refactor/multi-property/ altına yaz. Hiçbir gerçek müşteri verisini veya sırrı loglama.

## Bağlayıcı kurallar
- Mevcut işlev, API sözleşmesi ve veri korunur. Breaking change gerekiyorsa uyumluluk adaptörü ve migration planı hazırla; onaysız prod migration çalıştırma.
- Önce bağımlılık ve transitive lisans denetimi: ticari/SaaS/on-prem dağıtım, attribution ve copyleft riskleri. Lisans belirsizliğinde yeni paket ekleme.
- SOLID, DRY, KISS; .NET, TypeScript, React Native mevcut standartları; gereksiz abstraction ve tekrar yok.
- Backend her istekte sunucu tarafında personId + unitId + managementAreaId + ilişki durumu + eylem yetkisini doğrular. İstemciden gelen active context güvenlik kanıtı değildir. IDOR, cross-tenant sızıntısı, KVKK ve audit korumaları zorunlu.
- Tekil Person ile Keycloak identity bağını güvenilir/doğrulanmış süreçle kur; yalnızca ad, e-posta veya telefon eşleşmesine güvenme. Bir kullanıcının sistem yöneticisi/yönetici rolü ile daire bazlı malik/kiracı rolleri ayrıdır.
- Ownership (mülkiyet), LeaseContract/LeaseParty (sözleşme), Occupancy (fiili oturum) ayrı kavramlardır. Aynı kişinin çoklu ilişkisinde mükerrer Person oluşturma. Tarihsel kayıt, soft-delete ve optimistic concurrency koru.
- Yönetim alanı seçimi yalnızca ilişki kurulurken ve yetkili alan filtrelerinde kullanılır; Person global kapsamı mevcut tenancy modeline göre belirlenir. Tenant izolasyonunu kaldırma.
- Malik/kiracı kendi ilişkilerini yalnızca görüntüler; yeni malik/kiracı ilişkisini kendiliğinden oluşturamaz. Başvuru akışı varsa onay/audit gerektirir.
- Mobil SQLite'da yalnızca kullanıcıya yetkili minimal veriyi sakla; uygun şifreleme/secure storage, cihaz kilidi, logout ve erişim iptalinde temizleme. Offline ödeme YOK. Offline kimlik doğrulaması için saklanan parola veya düz metin token KULLANMA; mevcut güvenli oturum politikasını analiz et.
- Offline oluşturulan talep gibi işlemler local queue + idempotency key + retry + conflict policy ile senkronize edilir. İlişki iptal edilince offline queued işlemler tekrar sunucu yetkisinden geçer.
- Her aşamada konsola Türkçe [FAZ xx][ADIM y/z] BAŞLADI/TAMAMLANDI, değişen dosyalar, testler, riskler, kalan işler yaz. Kısa özetle token tasarrufu sağla; yalnız ilgili testleri çalıştır, final fazda kapsamlı test.
- Mock veri ile demo yapılabilir; gerçek veri diye gösterme. Mevcut UI tasarım dilini koru; web ve mobil erişilebilirlik, responsive, loading/empty/error states.

## Her fazın çıktısı
1) Yapılan değişiklikler ve dosya yolları 2) Yeni/etkilenen endpointler 3) Yetki ve KVKK etkisi 4) Migration/rollback durumu 5) Test komutları ve sonuçları 6) Bilinen riskler 7) Sonraki faza geçiş koşulu. Faz sonunda DUR.
