# Mobil yönetici — Faz10

15.10.2026. GET `/api/v1/mobile/manager?propertyId=<uuid>&role=<SUPER_ADMIN|SITE_ADMIN|MANAGER>&section=<overview|buildings|units|residents|finance|announcements|requests>&page=<1..10000>`. Salt okunur; mevcut tüketici buildingflow-mobile/mobile/src/manager ve ManagerPanel. Önceki owner/tenant/session sözleşmeleri değişmedi.

JWT sub+seçili gerçek realm role zorunlu. SUPER_ADMIN global aktif Property; SITE_ADMIN/MANAGER aktif üyelik. ACCOUNTANT/OWNER/TENANT bu endpoint'te403; yönetici claim’i üyelik yerine geçmez (global SUPER_ADMIN mevcut sözleşme istisnası).401anonim;403yabancı/revoked/pasif kapsam;400bozuk section/page/UUID. Kullanıcı hataya ham sunucu mesajıyla maruz bırakılmaz.

ManagerSnapshot: userId/contextKey/asOf/propertyName/address/section/page/total/summary/rows. contextKey Property:-:role. Summary: aktif buildings/units, benzersiz güncel aktif occupants kişi sayısı, enabled announcement/request sayıları, tüm iptal edilmemiş tahakkukların balance/overdue TRY string iki ondalık tutarı. RepeatableRead; Europe/Istanbul tarih. Yönetici geçmiş tahakkuklarını görür; bina/daire listesi yalnız aktif parent zinciridir.

Rows(id,title,detail,amount?,state?) bölümüne göre bina adı, daire+blok/kat, sakin adı+daire/OWNER-TENANT-RESIDENT, tahakkuk dönem+daire/vade+kalan/OPEN-PAID-OVERDUE-CANCELLED, duyuru başlık/içerik/DRAFT-PUBLISHED-ARCHIVED veya talep açıklaması/RECORDED. Kimlik/telefon/email/doğum/hesap bağı/payer/banka referansı dönmez. Sakin satırı aktif ilişki olduğundan bir kişi birden çok satır olabilir. Duyuru MANAGEMENT ve taslak içeriği yönetim yetkisinde görünür; talep sahibi hesabının kişisel bilgisi dönmez.

Sayfa1tabanlı,50sabit, stable sort+id tie-breaker; total tüm filtre kapsamından. Snapshot sayfalar arası transaction değildir; eşzamanlı değişimde yenileme gerekebilir. En fazla500000satıra sayfa erişimi; summary bu sınırdan bağımsız. Yeni şema/migration veya paket yok. Yazım, atama, ödeme, yayınlama yok; onay akışı gerektiren mutation eklenmedi.

Mobil şifreli mevcut owner_snapshots tablosunda hesap/context/section ayrımı, bölüm başına yalnız son sayfa,24saatTTL ve403purge; web RAM. Geç yanıt+hesap değişimi korumalı.

Kanıt:15.10.2026 MobileManager/Owner/Tenant/Session seçili20/20integration0skip;48/48mobil,TypeScript,lint0hata1öncekiuyarı,3platformexport; merkezbuild0hata0uyarı. Gerçek telefon UI/ekran/offline kabulü yapılmadı. Mobil rapor: kardeş buildingflow-mobile/docs/mobile/PHASE10.md.
