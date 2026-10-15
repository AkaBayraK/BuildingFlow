# Faz 08 — Ev sahibi deneyimi

14.10.2026. Kullanıcının “diğer faza geç” onayıyla yalnız Faz08 uygulandı. Faz09 başlatılmadı. Kullanıcı önceki turda görsel incelemeyi istemedi; ekran/screenshot alınmadı. Mevcut BuildingFlow lacivert/açık kart/çizgi ikon yönü ve referans görsel ilkeleri kullanıldı.

## Kullanıcıya görünen davranış

Ev sahibi rolünde ana sayfa ve Borçlar/Duyurular/Talepler sekmeleri gerçek merkez verisine bağlandı. Hesabım’dan mevcut yetkili mülk/rol seçimi korunur; yönetici/muhasebe/kiracı ekranları kendi sonraki fazlarına aittir. Kullanıcıya kendi kendine ev sahibi yetkisi verilmedi, mevcut kişi/hesap kayıtları değiştirilmedi.

- Seçili dairenin adresi, bina adı (varsa), numarası, katı ve brüt alanı.
- Aktif kiracı ilişkisinin yalnız adı ve ilişki başlangıç/bitişi. Kimlik, doğum, iletişim ve hesap bilgileri API’den gelmez. Kira sözleşmesi, kira bedeli ve belge için doğrulanmış ayrı backend modeli yok; ilişki tarihleri sözleşme tarihi gibi gösterilmez.
- Daireye atanmış UNIT tahakkukları ve bu hesaba bağlı malik kişisine açıkça atanmış borçlar; kiracının veya başka malikin kişisel borcu dahil edilmez. Kalan/ödenen tutar, vade, dönem ve gerçek iptal/ödendi/kısmi/gecikmiş/açık durumu. Kalan ve gecikmiş özet tüm yetkili satırlardan hesaplanır; son50kayıt sınırından etkilenmez. Tahsilat işlemi bazındaki geçmiş/online ödeme Faz11’e bırakıldı; sahte ödeme butonu/başarı yok.
- Yalnız PUBLISHED + ALL_MEMBERS duyuruları. Taslak/arşiv/yönetim duyuruları görünmez. Detay içerik gerçek metindir; belge ekleri ve push bu fazda eklenmedi.
- Aynı daire/OWNER bağlamında kendi merkez MobileRequest kayıtları; diğer hesapların talepleri ve diğer rol bağlamı görünmez. İşleniyor/çözüldü gibi backend’de bulunmayan durumlar üretilmez. Talep oluşturma/fotoğraf formu Faz12’dedir.

## API ve önbellek

GET `/api/v1/mobile/owner?propertyId=<uuid>&unitId=<uuid>` JWT sahibinin aktif linked Person + OWNER Occupancy + PropertyMembership + aktif Property/Unit/Building bağlamını tekrar doğrular. Yönetici rolü bu API’de malik bağını ikame etmez. RepeatableRead tek tutarlı okuma; mutation, yeni merkezi entity veya DBmigration yok.

Yanıt userId/contextKey/asOf, Unit, Tenants (10), Charges (50), Announcements/Requests (20) ve more* bayrakları. Daha fazla kayıt varsa sınır açıkça gösterilir; tam listeymiş gibi sunulmaz. Para decimal0.00string; mobil görüntüleme float rounding kullanmaz. Geçersiz sahiplik/rol/tarih/tutar/duplicate/truncated yanıt önbelleği değiştirmez; eski asOf yanıtı reddedilir.

Native SQLCipher schema5: userId+propertyId+unitId+OWNER anahtarlı owner_snapshots. Faz07 profil taslağı kaybolmadan4→5 additive migration. Offline24saat sınırı, hesap/bağlam/generation guard ve çıkış temizliği önceki altyapıyı kullanır.403 seçili cache’i siler; offline kopya yeni yetki vermez. Tarih/çevrimdışı/boş/yükleniyor/hata/yenile durumları görünür. Bakiye çevrimdışıyken eski olabileceği belirtilir. Web yalnız bellekte okur; kalıcı kişisel cache yok. Ön planda30s kontrol ve manuel yenileme.

## Doğrulama

- Mobil41/41 Node/gerçek SQLite test, TypeScript/lint hata/uyarı0; iOS/Android/Web export başarılı. Yeni testler rol/hesap/daire ayrımı, bozuk para/tarih/duplicate, float’sız format, whitelist,24saatexpiry/clock rollback,403cachepurge, eski yanıt ve4→5profil taslağı koruması.
- Merkez59/59 hedefli MobileOwner/MobileSession/MobileSync/Authentication/ReleaseSecurity integration test,0atlama. Gerçek ayrı PostgreSQL database/role/schema oluşturulup temizlendi. Pozitifmalik, başka daire/property, sahteSUPER_ADMIN/anonim, iptal edilen üyelik/OWNER ilişkisi, tenant kişisel borç hariçliği, duyuru/talep/PII ayrımı; son50satırla sınırlanmayan toplam test edildi.
- Merkez build0hata/0uyarı; API/worker Dockerbuild/yerel dağıtım geçti. Ana API5005 ve mobil API5006 health200, anonim owner401, Metro8081/status200. Normal mobil başlatıcı güncel LAN192.168.2.95. Yeni npm/.NET/native bağımlılığı yok; development build yeniden kurulması gerekmedi.
- Ekran açıp görsel inceleme veya gerçek kullanıcı hesabında OWNER ilişkisi/borç/talep oluşturma yapılmadı. Bu tur fiziksel telefon uçak modu/Android runtime kontrolü yapılmadı; önceki native SQLCipher kanıtı yeni kabul yapılmış gibi sunulmaz.

Merkez sözleşmesi: kardeş BuildingFlow/docs/api/mobile-owner.md. Hukuki/sözleşme API’si eksikliği açıkça belirtilir. Commit/push/yayın yok; ayrı merkez İŞ/UI yol haritası sayacı değişmedi.
