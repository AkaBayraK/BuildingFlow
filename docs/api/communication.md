# Duyurular ve belgeler — İŞ 08

Temel yol `/api/v1/properties/{propertyId}`. JWT `sub`, SUPER_ADMIN veya aktif yönetim alanı üyeliği zorunludur; silinmiş Property hiçbir rol için açılmaz. Yazım SUPER_ADMIN/SITE_ADMIN/MANAGER ve aktif Property gerektirir. ACCOUNTANT/RESIDENT yalnız kendilerine açık içeriği okur; kişi/daire kayıtları giriş hesabı veya hedef kitle değildir.

## Duyurular

| İstek | Davranış |
| --- | --- |
| GET `/announcements?page=1&pageSize=10&search=&status=&category=` | Filtreli/sayfalı liste; pageSize1–100, search≤200; literal `%/_` kaçırılır. `categoryCounts` kategori filtresinden bağımsız, görünür kayıtlar üzerinden altı kategoriyi sabit sırayla döner |
| GET `/announcements/{id}` | Kapsamlı detay |
| POST `/announcements` | `title`1–200, düz metin `content`1–10000, `audience`ALL_MEMBERS/MANAGEMENT, opsiyonel `category` GENERAL/MEETING/DUES/MAINTENANCE/URGENT/OTHER (yoksa GENERAL); DRAFT oluşturur |
| PUT `/announcements/{id}` | Aynı alanlar + `version`; yalnız taslak düzenlenir. `category` gönderilmezse mevcut kategori korunur (eski istemciler) |
| PUT `/announcements/{id}/publish` | `{version}`; DRAFT → PUBLISHED; UTC server publishedAt |
| PUT `/announcements/{id}/archive` | `{version}`; PUBLISHED → ARCHIVED; içerik/yayın tarihi korunur |

Yönetici tüm durum/hedefleri görür. Diğer aktif üyeler yalnız PUBLISHED+ALL_MEMBERS görür; taslak, MANAGEMENT ve arşiv için detay404/liste görünmezdir. İçerik HTML olarak işlenmez. Güncelleme ve ek işlemleri aynı Property kilidi/version ile sıralanır. Yayınlanmış duyuru içeriği ve ekleri yeniden yazılmaz. Mail/SMS gönderilmez; İş09 kapsamıdır.

## Belgeler

| İstek | Davranış |
| --- | --- |
| GET `/documents?page=1&pageSize=10&search=&announcementId=` | Yetkili arşiv/duyuru eki listesi |
| POST `/documents` | Multipart: tek `file`, UUID `requestId`, `audience`; ek için ayrıca `announcementId`,`announcementVersion` |
| GET `/documents/{id}/download` | JWT ile yetkili indirme; attachment, no-store, nosniff; kapsam dışı/silinmiş404 |
| DELETE `/documents/{id}?version=` | Metadata soft-delete; aktif Property, doğru version; ek için duyuru DRAFT olmalı |

Dosya en çok **10 MiB**. PDF `%PDF-`, JPEG `FF D8 FF`, PNG8byte imzası; MIME ve uzantı uyuşmalıdır. SVG/HTML/ZIP desteklenmez. Dosya adında kontrol karakteri veya `/`/`\` kabul edilmez; sunucu UUID ile `properties/{propertyId:N}/documents/{documentId:N}` üretir. Hiçbir dosya adı path anahtarı yapılmaz; bucket/anahtar/kimlik bilgisi veya public URL DTO’da dönmez. Gerçek dosya tipinin güvenli/zararsız olduğu sadece imzadan kanıtlanmaz; dosya taraması üretim kalite kapsamındadır. İndirme inline çalıştırılmaz.

Duyuru eki aynı Property’ye composite FK ile bağlıdır, en çok10 ek; görünürlük üst duyurudan okunur. Ek metadata audience alanı bağımsız görünürlük izni açmaz. Standalone MANAGEMENT dosyaları yöneticiye, ALL_MEMBERS dosyaları aktif üyelere açıktır. Bir duyuru ekini genel arşivden değiştirmek yerine duyuru taslağını kullanın.

UUID + SHA256/normalize metadata/parent version fingerprint: aynı istek200+replayed, farklı içerik veya silinmiş istek409. Yeni yükleme201. Upload içerik imzası server’da; download boyut ve SHA256 server’da ve FE’de doğrulanır. FE dosyayı ortak yetkili istemciyle Blob olarak alır; geçici object URL sonunda iptal edilir. İstek token’ı query/localStorage’a konmaz.

Dosya bucket’ı sabit özel `buildingflow-documents`; değiştirilecekse önceden tüm metadata/nesne anahtarları için kontrollü taşıma gerekir. SeaweedFS S3 sağlayıcısı her işlemde bucket politikasını kontrol eder; beklenmeyen politika503, var olan operatör politikasını sessizce değiştirmez. Dosya deposu kesintisi503 `BF_STORAGE_UNAVAILABLE`; boyut413 `BF_DOCUMENT_TOO_LARGE`; diğer validasyon400, version/state409. Ham SDK/exception mesajı API’ye taşınmaz.

Metadata DB transaction’ına kaydedilir; S3 deposu ile dağıtık transaction yoktur. Başarısız kayıt için yalnız bu yeni upload’ın benzersiz nesnesi temizlenir. SaveChanges sonrası commit sonucu belirsizse okunabilir bir nesneyi yanlışlıkla silmemek için otomatik temizleme yapılmaz; özel nesne artık bakım işi olabilir. Silme nesne içeriğini yok etmez; finans/duyuru geçmişini koruyan metadata soft-delete’idir. Kalıcı retention/orphan temizliği bu fazda yoktur. Duyuru/belge geçmişi bulunan Property silinemez.

## Yerel çalışma ve migration

`bash scripts/dev.sh api` ObjectStorage endpoint/access key/secret değerlerini mevcut .env'den belleğe alır. Yerel gateway http://127.0.0.1:9040; Docker API http://seaweedfs-s3:8333. İç filer/master/volume portları yalnız internal storage-data ağındadır. Mobil anahtar/public nesne URL'si almaz. Varsayılan API readiness DB'yi kontrol eder; kimlikli S3 round-trip ayrı integration testleridir.


## Dashboard ortak fotoğraf tüketicisi —20.10.2026

Dashboard mevcut standaloneALL_MEMBERS JPEG belge upload/list/download sözleşmesini kullanır. `buildingflow-dashboard-banner.jpg` tam adlı en yeni uygun belge ortak arka plandır. Yönetici yazımı,aktifüyelik,UUIDfingerprint/replay ve özelSHA256/boyut doğrulamalı indirme aynen korunur;ACCOUNTANTyazamaz. Yeniendpoint/tablo/izin yok. Belgelerden son görselin silinmesi önceki uygun görsele dönüş sağlar;otomatik nesne/geçmişsilme yok. Diğer oturumlar sayfaaçılışı/Yenile ile yeni görseli alır. [Ayrıntı](../navigation-refactor/DASHBOARD_HERO.md).
