# Duyurular ve belgeler — İŞ 08

Temel yol `/api/v1/properties/{propertyId}`. JWT `sub`, SUPER_ADMIN veya aktif yönetim alanı üyeliği zorunludur; silinmiş Property hiçbir rol için açılmaz. Yazım SUPER_ADMIN/SITE_ADMIN/MANAGER ve aktif Property gerektirir. ACCOUNTANT/RESIDENT yalnız kendilerine açık içeriği okur; kişi/daire kayıtları giriş hesabı veya hedef kitle değildir.

## Duyurular

| İstek | Davranış |
| --- | --- |
| GET `/announcements?page=1&pageSize=10&search=&status=` | Filtreli/sayfalı liste; pageSize1–100, search≤200; literal `%/_` kaçırılır |
| GET `/announcements/{id}` | Kapsamlı detay |
| POST `/announcements` | `title`1–200, düz metin `content`1–10000, `audience`ALL_MEMBERS/MANAGEMENT; DRAFT oluşturur |
| PUT `/announcements/{id}` | Aynı alanlar + `version`; yalnız taslak düzenlenir |
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

Dosya bucket’ı sabit özel `buildingflow-documents`; değiştirilecekse önceden tüm metadata/nesne anahtarları için kontrollü taşıma gerekir. MinIO sağlayıcısı her işlemde bucket politikasını kontrol eder; beklenmeyen politika503, var olan operatör politikasını sessizce değiştirmez. Dosya deposu kesintisi503 `BF_STORAGE_UNAVAILABLE`; boyut413 `BF_DOCUMENT_TOO_LARGE`; diğer validasyon400, version/state409. Ham SDK/exception mesajı API’ye taşınmaz.

Metadata DB transaction’ına kaydedilir; MinIO ile dağıtık transaction yoktur. Başarısız kayıt için yalnız bu yeni upload’ın benzersiz nesnesi temizlenir. SaveChanges sonrası commit sonucu belirsizse okunabilir bir nesneyi yanlışlıkla silmemek için otomatik temizleme yapılmaz; özel nesne artık bakım işi olabilir. Silme nesne içeriğini yok etmez; finans/duyuru geçmişini koruyan metadata soft-delete’idir. Kalıcı retention/orphan temizliği bu fazda yoktur. Duyuru/belge geçmişi bulunan Property silinemez.

## Yerel çalışma ve migration

`bash scripts/dev.sh api` MinIO endpoint/access key/secret mevcut .env’den bellekte yükler; parola değiştirmez. Yerel endpoint **127.0.0.1:9000**: localhost IPv6 farklı uygulamaya gidiyordu. Docker API `minio:9000` kullanır. MinIO console `http://127.0.0.1:9001`; sadece yöneticiler için altyapı aracıdır. Varsayılan readiness DB’yi kontrol eder; dosya erişimi ayrı sağlayıcı hata sözleşmesine tabidir.

Migration `20261010043250_AnnouncementsAndDocuments`; iki uygulama tablosunda sekiz zorunlu BaseEntity alanı NOT NULL. Önceki migration’lar korunur. Down duyuru veya belge geçmişi varsa reddedilir; soft-delete edilmiş belge de engeldir. Yerelde `bash scripts/db.sh update` ve test için `bash scripts/test.sh`. Testler ayrı `buildingflow_tests` DB şeması ve fixture başına `buildingflow-test-*` özel bucket kullanır; yalnız bu geçici bucket temizlenir. Ortam verilmezse storage testi atlanır ve geçmiş sayılmaz.

Kanıtlar [TESTING.md](../TESTING.md), faz kararı [analiz](../analysis/PHASE_08_ANNOUNCEMENTS_DOCUMENTS.md), hata sözleşmesi [errors.md](errors.md), lisans [licenses.md](../licenses.md).
