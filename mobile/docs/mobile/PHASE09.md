# Faz 09 — Kiracı deneyimi

14.10.2026. Kullanıcı Faz09’u onayladı. Yalnız kiracı kapsamı uygulandı; Faz10 başlamadı. Kullanıcının önceki talimatı nedeniyle ekran/screenshot ile görsel inceleme yapılmadı. BuildingFlow referansındaki lacivert başlık, okunabilir kartlar ve çizgi ikon yönü korundu.

## Uygulanan akış

- Hesabım’da gerçek yetkili Kiracı bağlamı seçilince ana sayfa ve Borçlar/Duyurular/Talepler kiracı verisini gösterir. Kullanıcıya kendiliğinden rol/kişisel kayıt eklenmez. Aktif daire adres/bina/numara/kat/alan; yalnız kendi kiracı ilişkisinin adı ve başlangıç/bitişi.
- UNIT aidatları ve bu hesaba bağlı kiracı kişisine açıkça atanmış tahakkuklar. Malikin/diğer kiracının kişisel borcu hariçtir. Kalan/gecikmiş özet, son50listeden bağımsız tüm yetkili satırları kapsar. Dönem/vade/tutar/ödenen/kalan ve gerçek durum gösterilir.
- Görüntülenebilir charge’lara mahsup edilen merkez PaymentAllocation geçmişi: tutar/tarih/terslenme. Ödeme toplamı, banka referansı, açıklama, payer/kişisel veri dönmez; bu kayıt kullanıcının bankadan ödeme yaptığına ilişkin kanıt olarak sunulmaz. Son50mahsup kaydı ve morePayments bayrağı.
- Yönetimin yalnız PUBLISHED/ALL_MEMBERS duyuruları ve kendi hesabının aynı TENANT+daire bağlamındaki talepleri. Diğer kişi veya OWNER bağlamı talepleri hariç.
- Native mobilde metinli bakım/arıza formu:1–2000karakter, seçili daire/TENANT scope, sabit operationId+recordId. Faz06 SQLCipher outbox ve idempotent API kullanılır. Offline kaydetme, response-loss replay, bekleyen/gönderildi/hata/çakışma/yeniden deneme mevcut SyncPanel’da izlenir. UI kaydetmeyi merkez başarısı gibi göstermez. Fotoğraf/kategori/talep yönetim durumları Faz12 kapsamındadır. Web önizlemesi bu fazda talep göndermez; native kuyruk gereksinimi açıklanır.
- Kira bedeli/sözleşme/belge modeli ve online ödeme sağlayıcısı mevcut değildir. Kira/sözleşme kaydı yokluğu açıkça gösterilir; ilişki tarihi sözleşme tarihi değildir, ödeme düğmesi/fake receipt yok.

## Ortak sözleşme ve cache

Yeni GET `/api/v1/mobile/tenant?propertyId=<uuid>&unitId=<uuid>` mevcut owner okuma servisi üzerinden ortak scope parametresiyle çalışır; veri erişimi/hook/cache/UI kopyalanmadı. JWT sub → linked aktif Person → geçerli TENANT Occupancy → aktif PropertyMembership/Property/Unit/Building doğrulaması. SUPER_ADMIN claim’i kiracı bağı yerine geçmez. RepeatableRead snapshot; anonim401, yetkisiz/sona ermiş bağlam403. Kendi talepleri mevcut POST `/api/v1/mobile/sync/commands` ve sunucu UUID/fingerprint/version kurallarını kullanır.

OwnerSnapshot’a payments/morePayments additive alanları eklendi. OWNER yanıtı boş payment listesiyle önceki davranışı korur; eski owner cache için parser boş liste varsayar. TENANT için history alanları zorunlu ve whitelisted. SQLCipher schema5tablosu tarihsel olarak owner_snapshots adıyla kalır, user+property+unit+rolecontextKey ayrımı OWNER/TENANT çakışmasını engeller; yeni migration yok. Aynı hesaptaki rol/daire değişiminde panel key’i ve generation guard geç yanıtı engeller.24saatTTL, backward-clock koruması,403scopepurge ve çıkış temizliği korunur. Web yalnız RAM. Offline ekran sonasOf tarihini ve bakiyenin değişmiş olabileceğini gösterir.

## Doğrulama

- Mobil45/45 test; TypeScript/lint hata/uyarı0; iOS/Android/Web export başarılı. Yeni testler TENANT history contract, para/status/foreign-role guard, paymentprivacywhitelist, aynıdaireOWNERcacheayrımı/TTL/purge, tenantrequestscope/validation ve cevabı kaybolan kaydın aynıUUIDreplay’i.
- Merkez63/63 seçili MobileTenant/Owner/Session/Sync/Authentication/ReleaseSecurity integration;0atlama. Gerçek ayrı PostgreSQL fixture oluşturulup temizlendi. UNIT+ownTENANT borç/aktif-ve-terslenmişAllocation, owner/coTenant borç ve banka/kimlik/iletişim ayrımı, diğerunit/property/OWNERendpoint/forgedSUPER_ADMIN/anonim; iptal üyelik ve sona ermiş TENANT ilişkisinin snapshot ve requestreplay’ini engellemesi. Önceki ev sahibi regresyonları geçti.
- Merkez build0hata/0uyarı, API/workerDockerbuild ve yerel deploy. Mobil API5006/ana API5005 health200, tenant anonim401; Metro8081/status200. Normal mobil başlatıcı LAN192.168.2.95. Yeni dependency veya merkez DBmigration yok; native binary değişmedi.
- Bu tur gerçek kullanıcı hesabında talep/borç/rol yazımı yapılmadı. Gerçek iPhone form/uçak modu/Android arayüz kabulü ve görsel inceleme yapılmadı; otomatik SQLite/API tests uçtan uca kullanıcı etkileşimi yerine sunulmaz. Önceki audit/native sınırları korunur.

Merkez: kardeş BuildingFlow/docs/api/mobile-tenant.md. Commit/push/yayın ve merkez İŞ/UI yol haritası sayacı değişikliği yok.
