# İŞ 08 — Duyurular ve dosya yönetimi

Onay: 10.10.2026. İş yol haritası 11/14; resmi dört görev mevcut milestones.ts kaynağından alınır. UI planındaki 08 ile karıştırılmaz.

Duyuru yönetim alanına aittir: başlık, düz metin içerik, ALL_MEMBERS/MANAGEMENT hedefi; DRAFT → PUBLISHED → ARCHIVED. Yönetici roller taslakları ve geçmişi yönetir. Aktif üyeler yalnız yayınlanan ve kendilerine açık kayıtları okur. Kişi/daire ilişkisi giriş hesabı olmadığı için kiracı/malik hedefi türetilmez. Bildirim gönderimi sonraki İş09 kapsamıdır.

Belgeler özel MinIO bucket’ında saklanır; DB yalnız BaseEntity audit/version + dosya metadata’sını tutar. PDF/JPEG/PNG, dosya imzası ve uzantı/MIME uyumu, en çok 10 MiB. Sunucu nesne anahtarı üretir; API hiçbir açık dosya URL’si veya kimlik bilgisi döndürmez. Duyuru eki aynı yönetim alanına composite FK ile bağlanır ve duyuru görünürlüğünü devralır.

Yükleme UUID + içerik özeti ile tekrar denemeye dayanır. Yazımlar Property kilidi ve version kontrolüyle sıralanır. Veritabanı ile nesne deposu dağıtık transaction oluşturmaz: yükleme ardından metadata kaydedilir; başarısız kayıt için yalnız bu işlemde oluşturulan nesne temizlenir. Başarısız temizlikte erişime kapalı nesne kalabilir. Silme metadata soft-delete’idir; özel nesne saklama/kalıcı temizlik politikası sonraki bakım kapsamıdır.

Kabul: rol/üyelik, taslak/yayın/arşiv görünürlüğü, version yarışları, tekrar istek, farklı property ekinin reddi, MIME/imza/boyut kontrolü, yetkili indirme ve anonim MinIO erişiminin engeli, ayrı PostgreSQL migration kurulum/yükseltme/idempotence/BaseEntity, TR/EN ve merkezi bildirimin korunması. Kaynak/dependency lisansları doğrulanır; mevcut veri ve uygulanmış migration’lar korunur.

## Teslim — 10.10.2026

**Dört resmi görev tamamlandı; 319 .NET/76 FE, typecheck/build ve316 lisans metadata kontrolü geçti.** Gerçek özel MinIO, ayrı PostgreSQL ve kullanıcı verisini kaydetmeden TR/EN sağ panel/mobil kabulü [TESTING.md](../TESTING.md) içinde kaydedildi. Belirsiz commit sonrasında yanlış nesne silmeyi önlemek için otomatik temizleme uygulanmaz. Mail/SMS, kalıcı nesne temizliği ve finans eki ayrı gelecek kapsamdır; İŞ09 başlatılmadı.
